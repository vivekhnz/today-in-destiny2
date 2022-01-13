import React, { useEffect, useState } from 'react'
import ActivityBlock from './ActivityBlock';
import { CurrentActivities, DailyActivities, RaidChallengeEntry, WeeklyActivities } from './activities';
import "./styles.css";

interface Props {
    dataSourceUri: string
}

const App: React.FC<Props> = props => {
    const [currentActivities, setCurrentActivities] = useState(new CurrentActivities())
    const [loadingState, setLoadingState] = useState('')
    useEffect(() => {
        async function fetchData() {
            setLoadingState('Loading...');
            try {
                const res = await fetch(`${props.dataSourceUri}/d2/today.json`, { mode: 'cors' });
                const json = await res.json();
                setLoadingState('');
                setCurrentActivities({ ...currentActivities, ...json });
            }
            catch (error) {
                setLoadingState(`An error occured: ${error}`);
                setCurrentActivities(new CurrentActivities());
            }
        }
        fetchData();
    }, []);

    const year = new Date().getFullYear();
    const app = (
        <>
            <div className='header'>
                <div className='headerContent'>
                    <div className='headerLogo'></div>
                    <h1 className='appTitle'>
                        <span className='line1'>Today in</span>
                        <br />
                        <span className='line2'>Destiny 2</span>
                    </h1>
                </div>
            </div>
            <div className='container'>
                {loadingState && <p className='loading'>{loadingState}</p>}
                {renderDailyActivities(currentActivities.dailyActivities)}
                {renderWeeklyActivities(currentActivities.weeklyActivities)}
                <p className='footer'>
                    <span className='line1'>&copy; {year} Vivek Hari</span><br />
                    <span className='line2'>Not affiliated with Bungie</span>
                </p>
            </div>
        </>
    )
    return app
}

export default App

function renderDailyActivities(activities: DailyActivities) {
    const activityInfos: any[] = [
        activities.vanguardStrikes
    ]
    if (activityInfos.filter(x => x).length < 1) {
        return <></>
    }

    return <>
        <h2 className='categoryHeader'>Today</h2>
        <div className='categorySeparator'></div>
        <div className='activityGrid'>
            {activities.vanguardStrikes && <ActivityBlock key='vanguardStrikes'
                type='Daily Modifiers' name='Vanguard Strikes'
                imageUrl={activities.vanguardStrikes.imageUrl}
                modifiers={activities.vanguardStrikes.modifiers} />}
        </div>
    </>
}

function renderWeeklyActivities(activities: WeeklyActivities) {
    const activityInfos: any[] = [
        activities.nightfall,
        activities.raidChallenges.vaultOfGlass,
        activities.raidChallenges.deepStoneCrypt,
        activities.raidChallenges.gardenOfSalvation,
        activities.empireHunt,
        activities.nightmareHunts,
        activities.cruciblePlaylist
    ].concat(activities.nightmareHunts);
    if (activityInfos.filter(x => x).length < 1) {
        return <></>
    }

    function renderRaidChallenge(name: string, entry?: RaidChallengeEntry) {
        if (!entry) {
            return <></>
        }
        return <ActivityBlock key={`raidChallenges.${name}`}
            type={`${name} Challenge`} name={entry.challengeName} imageUrl={entry.imageUrl} />
    }

    return <>
        <h2 className='categoryHeader'>This Week</h2>
        <div className='categorySeparator'></div>
        <div className='activityGrid'>
            {activities.nightfall && <ActivityBlock key='nightfall'
                type='Nightfall Strike' name={activities.nightfall.activityName}
                imageUrl={activities.nightfall.imageUrl} />}
            {activities.empireHunt && <ActivityBlock key='empireHunt'
                type='Empire Hunt' name={activities.empireHunt.activityName}
                imageUrl={activities.empireHunt.imageUrl} />}
            {activities.cruciblePlaylist && <ActivityBlock key='cruciblePlaylist'
                type='Crucible Playlist' name={activities.cruciblePlaylist.activityName}
                imageUrl={activities.cruciblePlaylist.imageUrl} />}
            {renderRaidChallenge('Vault of Glass', activities.raidChallenges.vaultOfGlass)}
            {renderRaidChallenge('Deep Stone Crypt', activities.raidChallenges.deepStoneCrypt)}
            {renderRaidChallenge('Garden of Salvation', activities.raidChallenges.gardenOfSalvation)}
            {activities.nightmareHunts.map(nightmareHunt =>
                <ActivityBlock key={`nightmareHunt.${nightmareHunt.activityName}`}
                    type='Nightmare Hunt' name={nightmareHunt.activityName}
                    imageUrl={nightmareHunt.imageUrl} />)}
        </div>
    </>
}