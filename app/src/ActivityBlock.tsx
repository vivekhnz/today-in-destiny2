import React, { CSSProperties } from 'react'
import styled from 'styled-components';
import { colors, fonts } from './GlobalStyles';
import { ActivityModifier } from './activities';

interface Props {
    type: string
    name: string
    imageUrl: string
    modifiers?: ActivityModifier[]
}

const ActivityBlock: React.FC<Props> = props => {
    return (
        <Container imageUrl={props.imageUrl}>
            <ActivityType>{props.type}</ActivityType>
            <ActivityName>{props.name}</ActivityName>
            {props.modifiers && props.modifiers.length > 0 && <>
                <Separator />
                <ModifierList>
                    {props.modifiers.map(modifier => renderModifier(modifier))}
                </ModifierList>
            </>}
        </Container>
    );
}
export default ActivityBlock

function renderModifier(modifier: ActivityModifier) {
    return (
        <li key={modifier.modifierName} title={modifier.description}>
            <img src={modifier.iconUrl} />
            <p>{modifier.modifierName}</p>
        </li>
    );
}

const gradient = 'linear-gradient(45deg, rgba(39, 58, 65, 0.7), rgba(39, 58, 65, 0.45))';
const Container = styled.div<{ imageUrl: string }>`
    background-color: ${colors.primary};
    background-size: cover;
    background-position: center 25%;
    ${props => props.imageUrl && `background-image: ${gradient}, url('${props.imageUrl}');`}
    padding: 48px 12px 12px 12px;
    border-radius: 3px;
    box-shadow: 0 0 4px 1px rgb(0 0 0 / 25%);
`
const ActivityType = styled.p`
    color: ${colors.tertiary};
    text-transform: uppercase;
    ${fonts.bender.bold}
    font-size: 15px;
    margin-bottom: 0px;
    text-shadow: 0.5px 0.5px 0 ${colors.primary};
`
const ActivityName = styled.p`
    color: white;
    ${fonts.bebas.bold}
    font-size: 36px;
    text-shadow: 1px 1px 2px ${colors.primary};
`
const Separator = styled.div`
    height: 4px;
    background-image: linear-gradient(-45deg,
        ${colors.tertiary} 25%, transparent 25%,
        transparent 50%, ${colors.tertiary} 50%,
        ${colors.tertiary} 75%, transparent 75%, transparent);
    opacity: 0.5;
    background-size: 4px 4px;
    margin: 4px 0;
`
const ModifierList = styled.ul`
    list-style-type: none;
    margin-top: 12px;
    padding: 0;

    > li {
        display: inline-block;
        ${fonts.bender.bold}
        font-size: 12px;
        margin: 0 16px 0 0;
        color: white;
        height: 20px;

        > img {
            width: 20px;
            height: 20px;
            margin-right: 6px;
        }
        > p {
            float: right;
            line-height: 20px;
        }
    }
`