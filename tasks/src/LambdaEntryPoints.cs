using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;

namespace TodayInDestiny2.Tasks;

public class LambdaEntryPoints
{
    public async Task<string> RefreshCurrentActivitiesHandler(Stream stream, ILambdaContext ctx)
    {
        await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
            BungieApiKey: GetRequiredEnvVar("TID2_BUNGIE_API_KEY", "Bungie API key"),
            DestinyMembershipType: GetRequiredEnvVar("TID2_DESTINY_MEMBERSHIP_TYPE", "Destiny 2 membership type"),
            DestinyMembershipId: GetRequiredEnvVar("TID2_DESTINY_MEMBERSHIP_ID", "Destiny 2 membership ID"),
            DestinyCharacterId: GetRequiredEnvVar("TID2_DESTINY_CHARACTER_ID", "Destiny 2 character ID"),
            AwsCredentials: null,
            DataS3BucketName: Environment.GetEnvironmentVariable("TID2_DATA_S3_BUCKET_NAME"),
            CloudFrontDistributionId: Environment.GetEnvironmentVariable("TID2_CLOUDFRONT_DISTRIBUTION_ID"),
            LocalDataDir: null
        ));
        return "Done";
    }

    private string GetRequiredEnvVar(string varName, string friendlyName)
    {
        string? envVarValue = Environment.GetEnvironmentVariable(varName);
        if (envVarValue == null)
        {
            throw new Exception($"{friendlyName} was not specified ({varName}).");
        }
        return envVarValue;
    }
}