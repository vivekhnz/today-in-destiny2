export interface Activity {
    type: string
    name: string
    imageUrl: string
    modifiers?: string[]
}
export interface ActivityCategory {
    category: string
    activities: Activity[]
}