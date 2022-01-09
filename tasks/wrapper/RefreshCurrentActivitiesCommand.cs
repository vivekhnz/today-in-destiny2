using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace TodayInDestiny2.Tasks.Wrapper;

internal static class RefreshCurrentActivitiesCommand
{
    internal static Command GetCommand()
    {
        var bungieApiKeyArg = new Argument<string>(
            "Bungie API key",
            "A Bungie API key retrieved from the Bungie Developer Portal."
        );
        var membershipTypeArg = new Argument<int>(
            "membership type",
            "The membership type of the Destiny 2 account whose activity availability will be queried."
        );
        var membershipIdArg = new Argument<ulong>(
            "membership id",
            "The membership ID of the Destiny 2 account whose activity availability will be queried."
        );
        var characterIdArg = new Argument<ulong>(
            "character id",
            "The ID of the Destiny 2 character whose activity availability will be queried."
        );
        var s3BucketArg = new Argument<string>(
            "bucket name",
            "The name of the S3 bucket to upload current activities data into."
        );
        var cfDistributionArg = new Argument<string>(
            "distribution id",
            "The ID of the CloudFront distribution to invalidate after uploading data into the S3 bucket."
        );

        var refreshLocalCommand = new Command("local", "Refresh current activities and save to a local JSON file.")
        {
            bungieApiKeyArg, membershipTypeArg, membershipIdArg, characterIdArg
        };
        refreshLocalCommand.SetHandler<string, int, ulong, ulong>(RefreshLocalAsync,
            bungieApiKeyArg, membershipTypeArg, membershipIdArg, characterIdArg);

        var refreshAwsCommand = new Command("aws", "Refresh current activities and save to AWS.")
        {
            characterIdArg, s3BucketArg, cfDistributionArg
        };
        refreshAwsCommand.SetHandler<string, int, ulong, ulong, string, string>(RefreshAwsAsync,
            bungieApiKeyArg, membershipTypeArg, membershipIdArg, characterIdArg, s3BucketArg, cfDistributionArg);

        return new Command("refresh", "Refresh current activities.")
        {
            refreshLocalCommand,
            refreshAwsCommand
        };
    }

    static async Task RefreshLocalAsync(string bungieApiKey,
        int membershipType, ulong membershipId, ulong characterId)
    {
        string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\data");
        await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
            BungieApiKey: bungieApiKey,
            DestinyMembershipType: membershipType.ToString(),
            DestinyMembershipId: membershipId.ToString(),
            DestinyCharacterId: characterId.ToString(),
            AwsCredentials: null,
            DataS3BucketName: null,
            CloudFrontDistributionId: null,
            LocalDataDir: dataDir
        ));
    }

    static async Task RefreshAwsAsync(string bungieApiKey,
        int membershipType, ulong membershipId, ulong characterId,
        string s3BucketName, string cfDistributionId)
    {
        await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
            BungieApiKey: bungieApiKey,
            DestinyMembershipType: membershipType.ToString(),
            DestinyMembershipId: membershipId.ToString(),
            DestinyCharacterId: characterId.ToString(),
            AwsCredentials: await AssumeRoleAsync("role.lambda.RefreshCurrentActivities"),
            DataS3BucketName: s3BucketName,
            CloudFrontDistributionId: cfDistributionId,
            LocalDataDir: null
        ));
    }

    static async Task<AWSCredentials> AssumeRoleAsync(string roleName)
    {
        var iamClient = new AmazonIdentityManagementServiceClient();
        var role = await iamClient.GetRoleAsync(new GetRoleRequest { RoleName = roleName });

        var stsClient = new AmazonSecurityTokenServiceClient();
        var assumeResponse = await stsClient.AssumeRoleAsync(new AssumeRoleRequest
        {
            RoleArn = role.Role.Arn,
            RoleSessionName = "LocalDev_AssumeLambdaRole"
        });

        return assumeResponse.Credentials;
    }
}