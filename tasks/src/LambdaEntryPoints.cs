using System;
using System.IO;
using System.Text.Json;
using Amazon.Lambda.Core;

namespace TodayInDestiny2.Tasks;

public class LambdaEntryPoints
{
    public void RefreshCurrentActivitiesHandler(Stream stream, ILambdaContext ctx)
    {
        Console.WriteLine("Hello from Lambda!");

        var envVars = Environment.GetEnvironmentVariables();
        Console.WriteLine($"Environment Variables: {JsonSerializer.Serialize(envVars)}");
        Console.WriteLine($"Function name: {ctx.FunctionName}");
        Console.WriteLine($"Max mem allocated: {ctx.MemoryLimitInMB}");
        Console.WriteLine($"Time remaining: {ctx.RemainingTime}");
        Console.WriteLine($"CloudWatch log stream name: {ctx.LogStreamName}");
        Console.WriteLine($"CloudWatch log group name: {ctx.LogGroupName}");

        RefreshCurrentActivitiesFunction.RefreshCurrentActivities();
    }
}