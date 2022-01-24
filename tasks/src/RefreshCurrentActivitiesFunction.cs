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

    public async static Task RefreshCurrentActivitiesAsync(TaskDefinition taskDef)
    {
        Console.WriteLine("Refreshing current activities...");

        var currentActivities = new CurrentActivities();
        using (var bungieService = new BungieService(taskDef.BungieApiKey))
        {
            var getCurrentActivities = bungieService.GetCurrentActivitiesAsync(
                taskDef.DestinyMembershipType, taskDef.DestinyMembershipId);
            var getModifiers = bungieService.GetModifiersAsync();

            using (var activitiesResponse = await getCurrentActivities)
            using (var modifiers = await getModifiers)
            {
                currentActivities = ExtractCurrentActivities(taskDef, activitiesResponse, modifiers);
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

    private static CurrentActivities ExtractCurrentActivities(
        TaskDefinition taskDef, JsonDocument responseDoc, JsonDocument modifiersDoc)
    {
        Console.WriteLine("Extracting current activities from response...");
        var result = new CurrentActivities();

        Dictionary<ulong, TKey> BuildHashLookup<TKey>(Dictionary<TKey, ulong> hashesByKey)
            where TKey : notnull
        {
            Dictionary<ulong, TKey> result = new();
            foreach (var kvp in hashesByKey)
            {
                result[kvp.Value] = kvp.Key;
            }
            return result;
        }
        Dictionary<ulong, TKey> BuildMultiHashLookup<TKey>(Dictionary<TKey, ulong[]> hashesByKey)
            where TKey : notnull
        {
            Dictionary<ulong, TKey> result = new();
            foreach (var kvp in hashesByKey)
            {
                foreach (var hash in kvp.Value)
                {
                    result[hash] = kvp.Key;
                }
            }
            return result;
        }

        var nightfallHashes = new Dictionary<(string Name, string ImageUrl), ulong[]>
        {
            [("The Pyramidion", "https://bungie.net/img/destiny_content/pgcr/strike_the_pyramdion.jpg")] = new ulong[] { 1129066976, 2229749170, 3265488360, 3265488362, 3265488363, 3265488365, 3289589202, 562078030, 642277473, 926940962 },
            [("The Festering Core", "https://bungie.net/img/destiny_content/pgcr/strike_the_festering_core.jpg")] = new ulong[] { 629542775, 685590036, 766116577, 766116580, 766116582, 766116583 },
            [("Savathûn's Song", "https://bungie.net/img/destiny_content/pgcr/strike_savanthuns_song.jpg")] = new ulong[] { 1863334927, 1975064760, 2288451134, 2886394453, 3280234344, 3815730356, 3849697856, 3849697858, 3849697859, 3849697861, 585071442 },
            [("The Inverted Spire", "https://bungie.net/img/destiny_content/pgcr/strike_inverted_spire.jpg")] = new ulong[] { 1801803624, 1801803625, 1801803627, 1801803630, 2599001912, 2599001913, 2599001915, 2599001918, 3050465729, 3368226533, 4054968718, 4259769141, 48090081 },
            [("Warden of Nothing", "https://www.bungie.net/img/destiny_content/pgcr/strike_aries.jpg")] = new ulong[] { 2491790989, 3108813009, 380956400, 380956405, 380956406, 380956407, 557845328, 557845329, 557845330, 557845335 },
            [("A Garden World", "https://bungie.net/img/destiny_content/pgcr/rituals_a_garden_world.jpg")] = new ulong[] { 1002842615, 2322829199, 2533203704, 2533203706, 2533203707, 2533203709, 2688061647, 373475104, 411726442, 936308438 },
            [("Will of the Thousands", "https://bungie.net/img/destiny_content/pgcr/strike_xol.jpg")] = new ulong[] { 2383858990, 272852450, 3132003003, 3907468134, 958578340 },
            [("Exodus Crash", "https://bungie.net/img/destiny_content/pgcr/strike_exodus_crash.jpg")] = new ulong[] { 1282886582, 1357019430, 1792985204, 322277826, 3233498448, 3233498449, 3233498450, 3233498455, 642256373, 68611392, 68611393, 68611394, 68611399 },
            [("The Corrupted", "https://bungie.net/img/destiny_content/pgcr/strike_gemini.jpg")] = new ulong[] { 2416314392, 2416314397, 2416314398, 2416314399, 245243704, 245243705, 245243706, 245243711, 3034843176, 3447375316 },
            [("The Arms Dealer", "https://bungie.net/img/destiny_content/pgcr/strike_the_arms_dealer.jpg")] = new ulong[] { 1358381368, 1358381370, 1358381371, 1358381373, 145302664, 1753547897, 1753547898, 1753547899, 1753547900, 2258250028, 3145298904, 3678597432, 3920643231, 601540706 },
            [("Tree of Probabilities", "https://bungie.net/img/destiny_content/pgcr/rituals_tree_of_probabilities.jpg")] = new ulong[] { 2046332536, 2416546450, 2660931442, 2660931444, 2660931445, 2660931447, 3326586101, 3718330161, 989294159 },
            [("The Insight Terminus", "https://bungie.net/img/destiny_content/pgcr/strike_glee.jpg")] = new ulong[] { 1034003646, 3029388705, 3029388708, 3029388710, 3029388711, 3200108049, 3200108052, 3200108054, 3200108055, 927394522 },
            [("The Hollowed Lair", "https://bungie.net/img/destiny_content/pgcr/strike_taurus.jpg")] = new ulong[] { 1465939129, 1561733171, 1561733172, 1561733173, 1561733174, 3701132453 },
            [("Lake of Shadows", "https://bungie.net/img/destiny_content/pgcr/strike_lake_of_shadows.jpg")] = new ulong[] { 1302909042, 1302909044, 1302909045, 1302909047, 1503474689, 3109193569, 3109193572, 3109193574, 3109193575, 3372160277 },
            [("Broodhold", "https://bungie.net/img/destiny_content/pgcr/strike_virgo.jpg")] = new ulong[] { 135872552, 135872553, 135872554, 135872559, 1391780798, 265186824, 265186829, 265186830, 265186831, 3692509130 },
            [("The Disgraced", "https://bungie.net/img/destiny_content/pgcr/cosmodrome-strike-gantry.jpg")] = new ulong[] { 2136458561, 2136458564, 2136458566, 2136458567 },
            [("Proving Grounds", "https://bungie.net/img/destiny_content/pgcr/nessus_proving_grounds.jpg")] = new ulong[] { 2103025314, 2103025316, 2103025317, 2103025319 },
            [("The Scarlet Keep", "https://bungie.net/img/destiny_content/pgcr/strike_the_scarlet_keep.jpg")] = new ulong[] { 1495545952, 1495545954, 1495545955, 1495545957, 3625752472, 3856436847, 887176537, 887176540, 887176542, 887176543 },
            [("The Devils' Lair", "https://bungie.net/img/destiny_content/pgcr/cosmodrome_devils_lair.jpg")] = new ulong[] { 1203950593, 1203950596, 1203950598, 1203950599 },
            [("The Glassway", "https://bungie.net/img/destiny_content/pgcr/europa-strike-blackbird.jpg")] = new ulong[] { 3812135450, 3812135452, 3812135453, 3812135455 },
            [("Strange Terrain", "https://bungie.net/img/destiny_content/pgcr/strike_nokris.jpg")] = new ulong[] { 13813394, 1701995982, 1794007817, 18699611, 2179568029, 3883876600, 3883876605, 3883876606, 3883876607, 4279557030, 522318687 },
            [("Fallen S.A.B.E.R.", "https://bungie.net/img/destiny_content/pgcr/cosmodrome_fallen_saber.jpg")] = new ulong[] { 3293630128, 3293630130, 3293630131, 3293630133 }
        };
        var empireHuntHashes = new Dictionary<(string Name, string ImageUrl), ulong>
        {
            [("The Warrior", "https://bungie.net/img/destiny_content/pgcr/empire-hunt-chowder.jpg")] = 4173217513,
            [("The Technocrat", "https://bungie.net/img/destiny_content/pgcr/empire-hunt-chili.jpg")] = 5517242,
            [("The Dark Priestess", "https://www.bungie.net/img/destiny_content/pgcr/empire-hunt-gumbo.jpg")] = 2205920677
        };
        var nightmareHuntHashes = new Dictionary<(string Name, string ImageUrl), ulong[]>
        {
            [("Anguish (Omnigul)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_anguish.jpg")] = new ulong[] { 2195531043, 571058904 },
            [("Despair (Crota)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_despair.jpg")] = new ulong[] { 1086094024, 2450170731 },
            [("Fear (Phogoth)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_fear.jpg")] = new ulong[] { 1342492675, 77280912 },
            [("Insanity (The Fanatic)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_insanity.jpg")] = new ulong[] { 2639701103, 66809868 },
            [("Isolation (Taniks)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_isolation.jpg")] = new ulong[] { 1344110078, 3205253945 },
            [("Pride (Skolas)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_pride.jpg")] = new ulong[] { 1907493625, 3821020454 },
            [("Rage (Ghaul)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_rage.jpg")] = new ulong[] { 2055076382, 4098556693 },
            [("Servitude (Zydron)", "https://bungie.net/img/destiny_content/pgcr/nightmare_hunt_servitude.jpg")] = new ulong[] { 1188363426, 1743972305 }
        };
        var cruciblePlaylistHashes = new Dictionary<string, ulong[]>
        {
            ["Clash"] = new ulong[] { 540869524 },
            ["Showdown"] = new ulong[] { 142028034, 1151331757, 1457072306 },
            ["Team Scorched"] = new ulong[] { 1219083526, 3787302650 },
            ["Mayhem"] = new ulong[] { 3847433434 },
            ["Momentum Control"] = new ulong[] { 935998519 }
        };

        var nightfallsByHash = BuildMultiHashLookup(nightfallHashes);
        var empireHuntsByHash = BuildHashLookup(empireHuntHashes);
        var nightmareHuntsByHash = BuildMultiHashLookup(nightmareHuntHashes);
        var cruciblePlaylistsByHash = BuildMultiHashLookup(cruciblePlaylistHashes);

        if (responseDoc.TryGetPropertyChain(out var availableActivities,
            "Response", "characterActivities", "data", taskDef.DestinyCharacterId, "availableActivities"))
        {
            foreach (var activity in availableActivities.EnumerateArray())
            {
                if (!activity.TryGetProperty("activityHash", out var activityHashProp)) continue;

                ulong activityHash = activityHashProp.GetUInt64();
                var modifiers = new ActivityModifier[0];
                if (activity.TryGetProperty("modifierHashes", out var modifierHashesProp))
                {
                    modifiers = (
                        from hash in modifierHashesProp.EnumerateArray()
                        let modifier = GetModifier(modifiersDoc, hash.GetUInt64())
                        where modifier != null
                        select modifier
                    ).ToArray();
                }
                string firstModifierName = modifiers.FirstOrDefault()?.ModifierName ?? string.Empty;

                if (activityHash == 743628305)
                {
                    result.DailyActivities.VanguardStrikes = new ActivityWithModifiers(
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pve_screenshot_2.jpg", modifiers);
                }
                else if (nightfallsByHash.TryGetValue(activityHash, out var nightfall))
                {
                    result.WeeklyActivities.Nightfall = new Activity(nightfall.Name, nightfall.ImageUrl);
                }
                else if (empireHuntsByHash.TryGetValue(activityHash, out var empireHunt))
                {
                    result.WeeklyActivities.EmpireHunt = new Activity(empireHunt.Name, empireHunt.ImageUrl);
                }
                else if (nightmareHuntsByHash.TryGetValue(activityHash, out var nightmareHunt))
                {
                    result.WeeklyActivities.NightmareHunts.Add(new Activity(nightmareHunt.Name, nightmareHunt.ImageUrl));
                }
                else if (cruciblePlaylistsByHash.TryGetValue(activityHash, out string? playlistName))
                {
                    result.WeeklyActivities.CruciblePlaylist = new Activity(playlistName,
                        "https://www.bungie.net/7/ca/destiny/bgs/new_light/media/pvp_screenshot_2.jpg");
                }
                else if (activityHash == 3881495763)
                {
                    result.WeeklyActivities.RaidChallenges.VaultOfGlass = new RaidChallengeEntry(firstModifierName,
                        "https://www.bungie.net/img/destiny_content/pgcr/vault_of_glass.jpg");
                }
                else if (activityHash == 910380154)
                {
                    result.WeeklyActivities.RaidChallenges.DeepStoneCrypt = new RaidChallengeEntry(firstModifierName,
                        "https://bungie.net/img/destiny_content/pgcr/europa-raid-deep-stone-crypt.jpg");
                }
                else if (activityHash == 3458480158)
                {
                    result.WeeklyActivities.RaidChallenges.GardenOfSalvation = new RaidChallengeEntry(firstModifierName,
                        "https://bungie.net/img/destiny_content/pgcr/raid_garden_of_salvation.jpg");
                }
            }
        }

        return result;
    }

    private static ActivityModifier? GetModifier(JsonDocument doc, ulong hash)
    {
        if (doc.TryGetPropertyChain(out var displayProperties, hash.ToString(), "displayProperties")
            && displayProperties.TryGetProperty("name", out var name))
        {
            string? modifierName = name.GetString();
            if (modifierName != null)
            {
                string description = string.Empty;
                if (displayProperties.TryGetProperty("description", out var descriptionProp))
                {
                    description = descriptionProp.GetString() ?? string.Empty;
                }
                string iconUrl = string.Empty;
                if (displayProperties.TryGetProperty("icon", out var iconProp))
                {
                    iconUrl = iconProp.GetString() ?? string.Empty;
                }
                if (!string.IsNullOrWhiteSpace(iconUrl))
                {
                    iconUrl = $"https://bungie.net{iconUrl}";
                }
                return new ActivityModifier(modifierName, description, iconUrl);
            }
        }

        return null;
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