using System;

namespace TodayInDestiny2.Tasks.Wrapper;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("*** Today in Destiny Task Function Wrapper ***");
        if (args.Length != 1)
        {
            PrintOptions();
            return;
        }
        string cmd = args[0];
        if (cmd == "r")
        {
            Console.WriteLine("Running refresh current activities function...");
            RefreshCurrentActivitiesFunction.RefreshCurrentActivities();
        }
        else
        {
            PrintOptions();
            return;
        }
    }

    static void PrintOptions()
    {
        Console.WriteLine("Please specify a valid command i.e.");
        Console.WriteLine("  dotnet run <option>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  r : Refresh current activities");
    }
}