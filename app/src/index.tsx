import React from 'react'
import ReactDOM from 'react-dom'
import "./styles.css";

interface Activity {
    name: string
    description: string
}
interface ActivityCategory {
    category: string
    activities: Activity[]
}

const activityCategories: ActivityCategory[] = [
    {
        category: 'Today',
        activities: [
            {
                name: 'Lost Sectors',
                description: `Legend: K1 Crew Quarters (Exotic Gauntlets)
Master: K1 Communion (Exotic Leg Armor)`
            }
        ]
    },
    {
        category: 'This Week',
        activities: [
            {
                name: 'Nightfall',
                description: 'The Hollowed Lair'
            },
            {
                name: 'Featured Crucible Playlist',
                description: 'Showdown'
            },
            {
                name: 'Raid Challenges',
                description: `Vault of Glass: Strangers in Time
Deep Stone Crypt: Of All Trades
Garden of Salvation: Leftovers
Last Wish: Forever Fight`
            },
        ]
    }
]

function renderActivities(categories: ActivityCategory[]) {
    return (
        <table className='activityCategoryTables'>
            <tbody>
                {categories.map(category =>
                    <>
                        <tr className='categoryHeader'>
                            <td colSpan={2}>{category.category}</td>
                        </tr>
                        {category.activities.map(activity =>
                            <tr>
                                <td>{activity.name}</td>
                                <td>{activity.description}</td>
                            </tr>
                        )}
                    </>
                )}
            </tbody>
        </table>
    )
}

const year = new Date().getFullYear();
const app = (
    <>
        <h1>Today in Destiny 2</h1>
        <p>WARNING: Today in Destiny 2 is a work-in-progress and any data displayed may be incorrect or out-of-date.</p>
        {renderActivities(activityCategories)}
        <p>
            &copy; {year} Vivek Hari<br />
            Not affiliated with Bungie
        </p>
    </>
)

ReactDOM.render(
    app,
    document.getElementById('root')
);