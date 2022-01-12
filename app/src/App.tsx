import React, { Fragment, useEffect, useState } from 'react'
import "./styles.css";

interface Props {
    dataSourceUri: string
}

interface Activity {
    type: string
    name: string
    modifiers?: string[]
}
interface ActivityCategory {
    category: string
    activities: Activity[]
}

const App: React.FC<Props> = props => {
    const [currentActivities, setCurrentActivities] = useState([])
    const [loadingState, setLoadingState] = useState('')
    useEffect(() => {
        async function fetchData() {
            setLoadingState('Loading...');
            try {
                const res = await fetch(`${props.dataSourceUri}/d2/today.json`, { mode: 'cors' });
                const json = await res.json();
                setLoadingState('');
                setCurrentActivities(json);
            }
            catch (error) {
                setLoadingState(`An error occured: ${error}`);
                setCurrentActivities([]);
            }
        }
        fetchData();
    }, []);

    const year = new Date().getFullYear();
    const app = (
        <>
            <div className='header'>
                <div className='headerContent'>
                    <h1 className='appTitle'>
                        <span className='line1'>Today in</span>
                        <br />
                        <span className='line2'>Destiny 2</span>
                    </h1>
                </div>
            </div>
            <div className='container'>
                {loadingState && <p>{loadingState}</p>}
                {renderActivities(currentActivities)}
                <p className='footer'>
                    <span className='line1'>&copy; {year} Vivek Hari</span><br />
                    <span className='line2'>Not affiliated with Bungie</span>
                </p>
            </div>
        </>
    )
    return app
}

function renderActivityBlock(activity: Activity) {
    return (
        <div className='activityBlock'>
            <p className='activityType'>{activity.type}</p>
            <p className='activityName'>{activity.name}</p>
            {activity.modifiers && activity.modifiers.length > 0 && <ul className='activityModifiers'>
                {activity.modifiers.map(modifier => <li key={modifier}>{modifier}</li>)}
            </ul>}
        </div>
    );
}

function renderActivities(categories: ActivityCategory[]) {
    const categoryOrder: string[] = ['Today', 'This Week'];
    const sortedCategories = categories.sort(
        (a, b) => categoryOrder.indexOf(a.category) - categoryOrder.indexOf(b.category));
    return (
        <>
            {sortedCategories.map(category =>
                <Fragment key={`category.${category.category}`}>
                    <h2 className='categoryHeader'>{category.category}</h2>
                    <div className='categorySeparator'></div>
                    <div className='activityGrid'>
                        {category.activities.map(activity =>
                            <Fragment key={`activity.${activity.type}.${activity.name}`}>
                                {renderActivityBlock(activity)}
                            </Fragment>
                        )}
                    </div>
                </Fragment>
            )}
        </>
    )
}

export default App