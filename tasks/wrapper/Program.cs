using System.CommandLine;
using TodayInDestiny2.Tasks.Wrapper;

var rootCommand = new RootCommand("Today in Destiny Task Function Wrapper")
{
    RefreshCurrentActivitiesCommand.GetCommand()
};
return await rootCommand.InvokeAsync(args);