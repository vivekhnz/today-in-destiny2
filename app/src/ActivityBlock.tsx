import React, { CSSProperties } from 'react'
import { ActivityModifier } from './activities';

interface Props {
    type: string
    name: string
    imageUrl: string
    modifiers?: ActivityModifier[]
}

const ActivityBlock: React.FC<Props> = props => {
    const gradient = 'linear-gradient(45deg, rgba(39, 58, 65, 0.7), rgba(39, 58, 65, 0.45))';
    const blockStyle: CSSProperties = {
        backgroundImage: props.imageUrl && `${gradient}, url('${props.imageUrl}')`
    };
    return (
        <div className='activityBlock' style={blockStyle}>
            <p className='activityType'>{props.type}</p>
            <p className='activityName'>{props.name}</p>
            {props.modifiers && props.modifiers.length > 0 && <>
                <div className='separator'></div>
                <ul className='activityModifiers'>
                    {props.modifiers.map(modifier => renderModifier(modifier))}
                </ul>
            </>}
        </div>
    );
}

function renderModifier(modifier: ActivityModifier) {
    return (
        <li key={modifier.modifierName} title={modifier.description}>
            <img src={modifier.iconUrl} />
            <p>{modifier.modifierName}</p>
        </li>
    );
}

export default ActivityBlock