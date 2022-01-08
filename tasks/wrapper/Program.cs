using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using TodayInDestiny2.Tasks;

var characterIdArg = new Argument<ulong>(
    "character id",
    "The ID of the Destiny 2 character whose activity availability will be queried.");
var s3BucketArg = new Argument<string>(
    "bucket name",
    "The name of the S3 bucket to upload current activities data into.");
var cfDistributionArg = new Argument<string>(
    "distribution id",
    "The ID of the CloudFront distribution to invalidate after uploading data into the S3 bucket.");

var refreshLocalCommand = new Command("local", "Refresh current activities and save to a local JSON file.")
{
    characterIdArg
};
refreshLocalCommand.SetHandler<ulong>(RefreshLocalAsync, characterIdArg);

var refreshAwsCommand = new Command("aws", "Refresh current activities and save to AWS.")
{
    characterIdArg, s3BucketArg, cfDistributionArg
};
refreshAwsCommand.SetHandler<ulong, string, string>(
    RefreshAwsAsync, characterIdArg, s3BucketArg, cfDistributionArg);

return
    await new RootCommand("Today in Destiny Task Function Wrapper")
    {
        new Command("refresh", "Refresh current activities.")
        {
            refreshLocalCommand, refreshAwsCommand
        }
    }
    .InvokeAsync(args);

async Task RefreshLocalAsync(ulong characterId)
{
    string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\data");
    await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
        DestinyCharacterId: characterId.ToString(),
        Credentials: null,
        DataS3BucketName: null,
        CloudFrontDistributionId: null,
        LocalDataDir: dataDir
    ));
}

async Task RefreshAwsAsync(ulong characterId, string s3BucketName, string cfDistributionId)
{
    await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
        DestinyCharacterId: characterId.ToString(),
        Credentials: await AssumeRoleAsync("role.lambda.RefreshCurrentActivities"),
        DataS3BucketName: s3BucketName,
        CloudFrontDistributionId: cfDistributionId,
        LocalDataDir: null
    ));
}

async Task<AWSCredentials> AssumeRoleAsync(string roleName)
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