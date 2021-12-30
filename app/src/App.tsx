import React, { Fragment, useEffect, useState } from 'react'
import "./styles.css";

interface Props {
    dataSourceUri: string
}

interface Activity {
    name: string
    description: string
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
            <h1>Today in Destiny 2</h1>
            <p>WARNING: Today in Destiny 2 is a work-in-progress and any data displayed may be incorrect or out-of-date.</p>
            {loadingState && <p>{loadingState}</p>}
            {renderActivities(currentActivities)}
            <p>
                &copy; {year} Vivek Hari<br />
                Not affiliated with Bungie
            </p>
        </>
    )
    return app
}

function renderActivities(categories: ActivityCategory[]) {
    return (
        <table className='activityCategoryTables'>
            <tbody>
                {categories.map(category =>
                    <Fragment key={`category.${category.category}`}>
                        <tr className='categoryHeader'>
                            <td colSpan={2}>{category.category}</td>
                        </tr>
                        {category.activities.map(activity =>
                            <tr key={`activity.${activity.name}`}>
                                <td>{activity.name}</td>
                                <td>{activity.description}</td>
                            </tr>
                        )}
                    </Fragment>
                )}
            </tbody>
        </table>
    )
}

export default App