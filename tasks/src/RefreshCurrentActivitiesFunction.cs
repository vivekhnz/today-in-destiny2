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
        string BungieApiKey,
        string DestinyMembershipType,
        string DestinyMembershipId,
        string DestinyCharacterId,
        AWSCredentials? AwsCredentials,
        string? DataS3BucketName, string? CloudFrontDistributionId,
        string? LocalDataDir);

    public record class Activity(string Type, string Name, string ImageUrl, string[]? Modifiers);
    public record class ActivityCategory(string Category, IEnumerable<Activity> Activities);

    public record class ExtractedActivity(string Category, string Type, string Name, string ImageUrl, string[]? Modifiers = null)
        : Activity(Type, Name, ImageUrl, Modifiers);
    public record class DailyActivity(string Type, string Name, string ImageUrl, string[]? Modifiers = null)
        : ExtractedActivity("Today", Type, Name, ImageUrl, Modifiers);
    public record class WeeklyActivity(string Type, string Name, string ImageUrl, string[]? Modifiers = null)
        : ExtractedActivity("This Week", Type, Name, ImageUrl, Modifiers);

    public async static Task RefreshCurrentActivitiesAsync(TaskDefinition taskDef)
    {
        Console.WriteLine("Refreshing current activities...");

        List<ActivityCategory>? currentActivities = null;
        using (var bungieService = new BungieService(taskDef.BungieApiKey))
        {
            var getCurrentActivities = bungieService.GetCurrentActivitiesAsync(
                taskDef.DestinyMembershipType, taskDef.DestinyMembershipId);
            var getModifiers = bungieService.GetModifiersAsync();

            using (var activitiesResponse = await getCurrentActivities)
            using (var modifiers = await getModifiers)
            {
                var extractedActivities = ExtractCurrentActivities(taskDef, activitiesResponse, modifiers);
                currentActivities = CategorizeCurrentActivities(extractedActivities);
            }
        }

        Console.WriteLine("Serializing current activities to JSON...");
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

    private static IEnumerable<ExtractedActivity> ExtractCurrentActivities(
        TaskDefinition taskDef, JsonDocument responseDoc, JsonDocument modifiersDoc)
    {
        Console.WriteLine("Extracting current activities from response...");

        if (responseDoc.TryGetPropertyChain(out var availableActivities,
            "Response", "characterActivities", "data", taskDef.DestinyCharacterId, "availableActivities"))
        {
            foreach (var activity in availableActivities.EnumerateArray())
            {
                if (!activity.TryGetProperty("activityHash", out var activityHashProp)) continue;

                ulong activityHash = activityHashProp.GetUInt64();
                string[]? modifierNames = null;
                if (activity.TryGetProperty("modifierHashes", out var modifierHashesProp))
                {
                    modifierNames = modifierHashesProp
                        .EnumerateArray()
                        .Select(hash => GetModifierName(modifiersDoc, hash.GetUInt64()))
                        .ToArray();
                }

                ExtractedActivity? extracted = activityHash switch
                {
                    743628305 => new DailyActivity("Daily Modifiers", "Vanguard Strikes",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pve_screenshot_2.jpg", modifierNames),

                    540869524 => new WeeklyActivity("Crucible Playlist", "Clash",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg"),
                    142028034 or 1151331757 or 1457072306 => new WeeklyActivity("Crucible Playlist", "Showdown",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg"),
                    1219083526 or 3787302650 => new WeeklyActivity("Crucible Playlist", "Team Scorched",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg"),
                    903584917 or 1102379070 or 1312786953 or 3847433434 => new WeeklyActivity("Crucible Playlist", "Mayhem",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg"),
                    935998519 or 952904835 => new WeeklyActivity("Crucible Playlist", "Momentum Control",
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg"),

                    1129066976 or 2229749170 or 3265488360 or 3265488362 or 3265488363 or 3265488365 or 3289589202 or 562078030 or 642277473 or 926940962
                        => new WeeklyActivity("Nightfall", "The Pyramidion", "https://bungie.net/img/destiny_content/pgcr/strike_the_pyramdion.jpg"),
                    629542775 or 685590036 or 766116577 or 766116580 or 766116582 or 766116583
                        => new WeeklyActivity("Nightfall", "The Festering Core", "https://bungie.net/img/destiny_content/pgcr/strike_the_festering_core.jpg"),
                    1863334927 or 1975064760 or 2288451134 or 2886394453 or 3280234344 or 3815730356 or 3849697856 or 3849697858 or 3849697859 or 3849697861 or 585071442
                        => new WeeklyActivity("Nightfall", "Savathûn's Song", "https://bungie.net/img/destiny_content/pgcr/strike_savanthuns_song.jpg"),
                    1801803624 or 1801803625 or 1801803627 or 1801803630 or 2599001912 or 2599001913 or 2599001915 or 2599001918 or 3050465729 or 3368226533 or 4054968718 or 4259769141 or 48090081
                        => new WeeklyActivity("Nightfall", "The Inverted Spire", "https://bungie.net/img/destiny_content/pgcr/strike_inverted_spire.jpg"),
                    2491790989 or 3108813009 or 380956400 or 380956405 or 380956406 or 380956407 or 557845328 or 557845329 or 557845330 or 557845335
                        => new WeeklyActivity("Nightfall", "Warden of Nothing", "https://www.bungie.net/img/destiny_content/pgcr/strike_aries.jpg"),
                    1002842615 or 2322829199 or 2533203704 or 2533203706 or 2533203707 or 2533203709 or 2688061647 or 373475104 or 411726442 or 936308438
                        => new WeeklyActivity("Nightfall", "A Garden World", "https://bungie.net/img/destiny_content/pgcr/rituals_a_garden_world.jpg"),
                    2383858990 or 272852450 or 3132003003 or 3907468134 or 958578340
                        => new WeeklyActivity("Nightfall", "Will of the Thousands", "https://bungie.net/img/destiny_content/pgcr/strike_xol.jpg"),
                    1282886582 or 1357019430 or 1792985204 or 322277826 or 3233498448 or 3233498449 or 3233498450 or 3233498455 or 642256373 or 68611392 or 68611393 or 68611394 or 68611399
                        => new WeeklyActivity("Nightfall", "Exodus Crash", "https://bungie.net/img/destiny_content/pgcr/strike_exodus_crash.jpg"),
                    2416314392 or 2416314397 or 2416314398 or 2416314399 or 245243704 or 245243705 or 245243706 or 245243711 or 3034843176 or 3447375316
                        => new WeeklyActivity("Nightfall", "The Corrupted", "https://bungie.net/img/destiny_content/pgcr/strike_gemini.jpg"),
                    1358381368 or 1358381370 or 1358381371 or 1358381373 or 145302664 or 1753547897 or 1753547898 or 1753547899 or 1753547900 or 2258250028 or 3145298904 or 3678597432 or 3920643231 or 601540706
                        => new WeeklyActivity("Nightfall", "The Arms Dealer", "https://bungie.net/img/destiny_content/pgcr/strike_the_arms_dealer.jpg"),
                    2046332536 or 2416546450 or 2660931442 or 2660931444 or 2660931445 or 2660931447 or 3326586101 or 3718330161 or 989294159
                        => new WeeklyActivity("Nightfall", "Tree of Probabilities", "https://bungie.net/img/destiny_content/pgcr/rituals_tree_of_probabilities.jpg"),
                    1034003646 or 3029388705 or 3029388708 or 3029388710 or 3029388711 or 3200108049 or 3200108052 or 3200108054 or 3200108055 or 927394522
                        => new WeeklyActivity("Nightfall", "The Insight Terminus", "https://bungie.net/img/destiny_content/pgcr/strike_glee.jpg"),
                    1465939129 or 1561733171 or 1561733172 or 1561733173 or 1561733174 or 3701132453
                        => new WeeklyActivity("Nightfall", "The Hollowed Lair", "https://bungie.net/img/destiny_content/pgcr/strike_taurus.jpg"),
                    1302909042 or 1302909044 or 1302909045 or 1302909047 or 1503474689 or 3109193569 or 3109193572 or 3109193574 or 3109193575 or 3372160277
                        => new WeeklyActivity("Nightfall", "Lake of Shadows", "https://bungie.net/img/destiny_content/pgcr/strike_lake_of_shadows.jpg"),
                    135872552 or 135872553 or 135872554 or 135872559 or 1391780798 or 265186824 or 265186829 or 265186830 or 265186831 or 3692509130
                        => new WeeklyActivity("Nightfall", "Broodhold", "https://bungie.net/img/destiny_content/pgcr/strike_virgo.jpg"),
                    2136458561 or 2136458564 or 2136458566 or 2136458567
                        => new WeeklyActivity("Nightfall", "The Disgraced", "https://bungie.net/img/destiny_content/pgcr/cosmodrome-strike-gantry.jpg"),
                    2103025314 or 2103025316 or 2103025317 or 2103025319
                        => new WeeklyActivity("Nightfall", "Proving Grounds", "https://bungie.net/img/destiny_content/pgcr/nessus_proving_grounds.jpg"),
                    1495545952 or 1495545954 or 1495545955 or 1495545957 or 3625752472 or 3856436847 or 887176537 or 887176540 or 887176542 or 887176543
                        => new WeeklyActivity("Nightfall", "The Scarlet Keep", "https://bungie.net/img/destiny_content/pgcr/strike_the_scarlet_keep.jpg"),
                    1203950593 or 1203950596 or 1203950598 or 1203950599
                        => new WeeklyActivity("Nightfall", "The Devils' Lair", "https://bungie.net/img/destiny_content/pgcr/cosmodrome_devils_lair.jpg"),
                    3812135450 or 3812135452 or 3812135453 or 3812135455
                        => new WeeklyActivity("Nightfall", "The Glassway", "https://bungie.net/img/destiny_content/pgcr/europa-strike-blackbird.jpg"),
                    13813394 or 1701995982 or 1794007817 or 18699611 or 2179568029 or 3883876600 or 3883876605 or 3883876606 or 3883876607 or 4279557030 or 522318687
                        => new WeeklyActivity("Nightfall", "Strange Terrain", "https://bungie.net/img/destiny_content/pgcr/strike_nokris.jpg"),
                    3293630128 or 3293630130 or 3293630131 or 3293630133
                        => new WeeklyActivity("Nightfall", "Fallen S.A.B.E.R.", "https://bungie.net/img/destiny_content/pgcr/cosmodrome_fallen_saber.jpg"),

                    4173217513 => new WeeklyActivity("Empire Hunt", "The Warrior",
                        "https://bungie.net/img/destiny_content/pgcr/empire-hunt-chowder.jpg"),
                    5517242 => new WeeklyActivity("Empire Hunt", "The Technocrat",
                        "https://bungie.net/img/destiny_content/pgcr/empire-hunt-chili.jpg"),
                    2205920677 => new WeeklyActivity("Empire Hunt", "The Dark Priestess",
                        "https://www.bungie.net/img/destiny_content/pgcr/empire-hunt-gumbo.jpg"),

                    3881495763 => new WeeklyActivity("Vault of Glass Challenge",
                        modifierNames?.FirstOrDefault() ?? string.Empty,
                        "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg"),
                    910380154 => new WeeklyActivity("Deep Stone Crypt Challenge",
                        modifierNames?.FirstOrDefault() ?? string.Empty,
                        "https://bungie.net/img/destiny_content/pgcr/europa-raid-deep-stone-crypt.jpg"),
                    3458480158 => new WeeklyActivity("Garden of Salvation Challenge",
                        modifierNames?.FirstOrDefault() ?? string.Empty,
                        "https://bungie.net/img/destiny_content/pgcr/raid_garden_of_salvation.jpg"),

                    2195531043 or 571058904 => new WeeklyActivity("Nightmare Hunt", "Anguish (Omnigul)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_anguish.jpg"),
                    1086094024 or 2450170731 => new WeeklyActivity("Nightmare Hunt", "Despair (Crota)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_despair.jpg"),
                    1342492675 or 77280912 => new WeeklyActivity("Nightmare Hunt", "Fear (Phogoth)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_fear.jpg"),
                    2639701103 or 66809868 => new WeeklyActivity("Nightmare Hunt", "Insanity (The Fanatic)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_insanity.jpg"),
                    1344110078 or 3205253945 => new WeeklyActivity("Nightmare Hunt", "Isolation (Taniks)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_isolation.jpg"),
                    1907493625 or 3821020454 => new WeeklyActivity("Nightmare Hunt", "Pride (Skolas)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_pride.jpg"),
                    2055076382 or 4098556693 => new WeeklyActivity("Nightmare Hunt", "Rage (Ghaul)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_rage.jpg"),
                    1188363426 or 1743972305 => new WeeklyActivity("Nightmare Hunt", "Servitude (Zydron)",
                        "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_servitude.jpg"),

                    _ => null
                };
                if (extracted is not null)
                {
                    yield return extracted;
                }
            }
        }
    }

    private static string GetModifierName(JsonDocument doc, ulong hash)
    {
        if (doc.TryGetPropertyChain(out var name, hash.ToString(), "displayProperties", "name"))
        {
            return name.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static List<ActivityCategory> CategorizeCurrentActivities(IEnumerable<ExtractedActivity> activities)
    {
        return activities
            .Distinct()
            .GroupBy(activity => activity.Category)
            .Select(grp => new ActivityCategory(
                Category: grp.Key,
                Activities: grp
            ))
            .ToList();
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
        var s3Client = taskDef.AwsCredentials == null
            ? new AmazonS3Client()
            : new AmazonS3Client(taskDef.AwsCredentials);
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
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NotFound" || ex.ErrorCode == "Forbidden")
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
        var cfClient = taskDef.AwsCredentials == null
            ? new AmazonCloudFrontClient()
            : new AmazonCloudFrontClient(taskDef.AwsCredentials);

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