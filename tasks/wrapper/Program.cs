using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using TodayInDestiny2.Tasks;

Console.WriteLine("*** Today in Destiny Task Function Wrapper ***");
if (args.Length < 1)
{
    PrintOptions();
    return;
}
string cmd = args[0];
if (cmd == "refresh")
{
    bool isRunLocalOnly = args.Length > 1 && args[1] == "--local";

    string? dataS3BucketName = null;
    string? cloudFrontDistributionId = null;
    AWSCredentials? creds = null;

    if (!isRunLocalOnly)
    {
        Console.Write("Enter S3 bucket name or leave blank to skip upload: ");
        dataS3BucketName = Console.ReadLine();

        Console.Write("Enter CloudFront distribution ID or leave blank to skip upload: ");
        cloudFrontDistributionId = Console.ReadLine();

        creds = await AssumeRoleAsync("role.lambda.RefreshCurrentActivities");
    }

    Console.WriteLine("Running refresh current activities function...");
    string dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\data");
    await RefreshCurrentActivitiesFunction.RefreshCurrentActivitiesAsync(new(
        DestinyCharacterId: "2305843009301193818",
        Credentials: creds,
        DataS3BucketName: dataS3BucketName,
        CloudFrontDistributionId: cloudFrontDistributionId,
        LocalDataDir: dataDir
    ));
}
else
{
    PrintOptions();
    return;
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

void PrintOptions()
{
    Console.WriteLine("Please specify a valid command i.e.");
    Console.WriteLine("  dotnet run <option>");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  refresh : Refresh current activities");
}