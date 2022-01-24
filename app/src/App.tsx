import React, { useEffect, useState } from 'react'
import styled from 'styled-components'
import { colors, fonts, GlobalStyles } from './GlobalStyles';
import Header from './Header';
import Footer from './Footer';
import ActivityCategory from './ActivityCategory';
import ActivityBlock from './ActivityBlock';
import { CurrentActivities, DailyActivities, RaidChallengeEntry, WeeklyActivities } from './activities';

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

    return (
        <>
            <GlobalStyles />
            <Header />
            <Container>
                {loadingState && <Loading>{loadingState}</Loading>}
                {renderDailyActivities(currentActivities.dailyActivities)}
                {renderWeeklyActivities(currentActivities.weeklyActivities)}
                <Footer />
            </Container>
        </>
    )
}
export default App

const Container = styled.div`
    max-width: 1300px;
    padding: 0 12px;
    margin: 0px auto;
`

const Loading = styled.p`
    margin-top: 24px;
    ${fonts.bender.bold}
    color: ${colors.primary};
`

function renderDailyActivities(activities: DailyActivities) {
    const activityInfos: any[] = [
        activities.vanguardStrikes
    ]
    if (activityInfos.filter(x => x).length < 1) {
        return <></>
    }

    return (
        <ActivityCategory name='Today'>
            {activities.vanguardStrikes && <ActivityBlock key='vanguardStrikes'
                type='Daily Modifiers' name='Vanguard Strikes'
                imageUrl={activities.vanguardStrikes.imageUrl}
                modifiers={activities.vanguardStrikes.modifiers} />}
        </ActivityCategory>
    );
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

    return (
        <ActivityCategory name='This Week'>
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
        </ActivityCategory>
    );
}