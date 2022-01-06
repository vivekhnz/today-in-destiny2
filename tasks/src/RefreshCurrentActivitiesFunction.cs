using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TodayInDestiny2.Tasks;

public static class RefreshCurrentActivitiesFunction
{
    const string CurrentActivitiesS3FileKey = "d2/today.json";

    public record class TaskDefinition(
        AWSCredentials? Credentials,
        string? DataS3BucketName, string? CloudFrontDistributionId,
        string? LocalDataDir);

    public record class Activity(string Name, string Description);
    public record class ActivityCategory(string Category, List<Activity> Activities);

    public async static Task RefreshCurrentActivitiesAsync(TaskDefinition taskDef)
    {
        Console.WriteLine("Refreshing current activities...");

        var currentActivities = new List<ActivityCategory>
        {
            new("Today", new List<Activity>
            {
                new("Lost Sectors", @"Legend: Aphelion's Rest (Exotic Leg Armor)
Master: Chamber of Starlight (Exotic Helmet)")
            }),

            new("This Week", new List<Activity>
            {
                new("Nightfall", "Lake of Shadows"),
                new("Featured Crucible Playlist", "Clash"),
                new("Raid Challenges", @"Vault of Glass: Ensemble's Refrain
Deep Stone Crypt: The Core Four
Garden of Salvation: A Link to the Chain
Last Wish: Keep Out"),
            })
        };
        string json = JsonSerializer.Serialize(currentActivities, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (!string.IsNullOrWhiteSpace(taskDef.DataS3BucketName)
            && !string.IsNullOrWhiteSpace(taskDef.CloudFrontDistributionId))
        {
            await SaveToS3Async(taskDef, json);
        }

        if (!string.IsNullOrWhiteSpace(taskDef.LocalDataDir))
        {
            string outputDirPath = Path.Combine(taskDef.LocalDataDir, "d2");
            Directory.CreateDirectory(outputDirPath);
            File.WriteAllText(Path.Combine(outputDirPath, "today.json"), json);
        }
    }

    private async static Task SaveToS3Async(TaskDefinition taskDef, string json)
    {
        // skip upload if the existing file's ETag matches the hash of our generated JSON
        string jsonHash = string.Empty;
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(json);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            jsonHash = string.Join("", hashBytes.Select(b => b.ToString("x2")));
        }

        Console.WriteLine("Querying ETag from S3 bucket...");
        var s3Client = taskDef.Credentials == null
            ? new AmazonS3Client()
            : new AmazonS3Client(taskDef.Credentials);
        try
        {
            var metadata = await s3Client.GetObjectMetadataAsync(
                taskDef.DataS3BucketName, CurrentActivitiesS3FileKey);

            // strip surrounding quotes
            string etag = metadata.ETag.Replace("\"", "");

            Console.WriteLine($"Generated JSON hash : {jsonHash}");
            Console.WriteLine($"Existing file ETag  : {etag}");

            if (string.Equals(etag, jsonHash, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Generated JSON hash matches existing file ETag. Skipping upload to S3.");
                return;
            }
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NotFound")
        {
            Console.WriteLine("Existing file was not found.");
        }

        // upload file to S3
        Console.WriteLine("Uploading file to S3...");
        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = taskDef.DataS3BucketName,
            Key = CurrentActivitiesS3FileKey,
            ContentBody = json,
            ContentType = "application/json"
        });

        // invalidate CloudFront cache
        var cfClient = taskDef.Credentials == null
            ? new AmazonCloudFrontClient()
            : new AmazonCloudFrontClient(taskDef.Credentials);

        var invalidationBatch = new InvalidationBatch(
            new Paths
            {
                Quantity = 1,
                Items = new List<string> { $"/{CurrentActivitiesS3FileKey}" }
            },
            $"RefreshCurrentActivities-{jsonHash}");
        await cfClient.CreateInvalidationAsync(new CreateInvalidationRequest(
            taskDef.CloudFrontDistributionId, invalidationBatch));
    }
}