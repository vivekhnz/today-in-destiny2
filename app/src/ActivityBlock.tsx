import React, { CSSProperties, Fragment, useEffect, useState } from 'react'
import { Activity } from './interfaces';

interface Props {
    activity: Activity
}

const ActivityBlock: React.FC<Props> = props => {
    const activity = props.activity;
    const gradient = 'linear-gradient(45deg, rgba(39, 58, 65, 0.7), rgba(39, 58, 65, 0.45))';
    const imageUrl = props.activity.imageUrl;
    const blockStyle: CSSProperties = {
        backgroundImage: imageUrl && `${gradient}, url('${imageUrl}')`
    };
    return (
        <div className='activityBlock' style={blockStyle}>
            <p className='activityType'>{activity.type}</p>
            <p className='activityName'>{activity.name}</p>
            {activity.modifiers && activity.modifiers.length > 0 && <ul className='activityModifiers'>
                {activity.modifiers.map(modifier => <li key={modifier}>{modifier}</li>)}
            </ul>}
        </div>
    );
}

export default ActivityBlock