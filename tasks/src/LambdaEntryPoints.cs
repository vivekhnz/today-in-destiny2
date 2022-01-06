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
            Credentials: null,
            DataS3BucketName: Environment.GetEnvironmentVariable("TID2_DATA_S3_BUCKET_NAME"),
            CloudFrontDistributionId: Environment.GetEnvironmentVariable("TID2_CLOUDFRONT_DISTRIBUTION_ID"),
            LocalDataDir: null
        ));
        return "Done";
    }
}