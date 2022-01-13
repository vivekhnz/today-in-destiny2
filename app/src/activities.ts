export class CurrentActivities {
    dailyActivities: DailyActivities = {}
    weeklyActivities: WeeklyActivities = {
        raidChallenges: {},
        nightmareHunts: []
    }
}
export interface DailyActivities {
    vanguardStrikes?: ActivityWithModifiers
}
export interface WeeklyActivities {
    nightfall?: Activity
    raidChallenges: RaidChallenges
    empireHunt?: Activity
    nightmareHunts: Activity[]
    cruciblePlaylist?: Activity
}

export interface Activity {
    activityName: string
    imageUrl: string
}

export interface ActivityWithModifiers {
    imageUrl: string
    modifiers: ActivityModifier[]
}
export interface ActivityModifier {
    modifierName: string
    description: string
    iconUrl: string
}

export interface RaidChallenges {
    vaultOfGlass?: RaidChallengeEntry
    deepStoneCrypt?: RaidChallengeEntry
    gardenOfSalvation?: RaidChallengeEntry
}
export interface RaidChallengeEntry {
    challengeName: string
    imageUrl: string
}