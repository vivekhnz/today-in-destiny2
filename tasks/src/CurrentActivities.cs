using System.Collections.Generic;

namespace TodayInDestiny2.Tasks;

public class CurrentActivities
{
    public DailyActivities DailyActivities { get; } = new();
    public WeeklyActivities WeeklyActivities { get; } = new();
}
public class DailyActivities
{
    public ActivityWithModifiers? VanguardStrikes { get; set; }
}
public class WeeklyActivities
{
    public Activity? Nightfall { get; set; }
    public RaidChallenges RaidChallenges { get; } = new();
    public Activity? EmpireHunt { get; set; }
    public List<Activity> NightmareHunts { get; } = new();
    public Activity? CruciblePlaylist { get; set; }
}

public record class Activity(string ActivityName, string ImageUrl);

public record ActivityWithModifiers(string ImageUrl, ActivityModifier[] Modifiers);
public record class ActivityModifier(string ModifierName);

public class RaidChallenges
{
    public RaidChallengeEntry? VaultOfGlass { get; set; }
    public RaidChallengeEntry? DeepStoneCrypt { get; set; }
    public RaidChallengeEntry? GardenOfSalvation { get; set; }
}
public record class RaidChallengeEntry(string ChallengeName, string ImageUrl);