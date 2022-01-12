import React, { Fragment, useEffect, useState } from 'react'
import ActivityBlock from './ActivityBlock';
import { Activity, ActivityCategory } from './interfaces';
import "./styles.css";

interface Props {
    dataSourceUri: string
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
                {loadingState && <p className='loading'>{loadingState}</p>}
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
                            <ActivityBlock
                                key={`activity.${activity.type}.${activity.name}`}
                                activity={activity} />
                        )}
                    </div>
                </Fragment>
            )}
        </>
    )
}

export default App