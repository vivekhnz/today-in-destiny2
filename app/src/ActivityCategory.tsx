import React from 'react'
import styled from 'styled-components'
import { fonts, colors, breakpoints } from './GlobalStyles';

interface Props {
    name: string
}

const ActivityCategory: React.FC<Props> = props => {
    return <>
        <CategoryHeader>{props.name}</CategoryHeader>
        <CategorySeparator />
        <ActivityGrid>
            {props.children}
        </ActivityGrid>
    </>
}
export default ActivityCategory;

const CategoryHeader = styled.h2`
    ${fonts.bebas.bold}
    font-size: 36px;
    color: ${colors.primary};
    margin: 24px 0 0 0;
`
const CategorySeparator = styled.div`
    height: 4px;
    background-image: linear-gradient(-45deg,
        ${colors.secondary} 25%, transparent 25%,
        transparent 50%, ${colors.secondary} 50%,
        ${colors.secondary} 75%, transparent 75%, transparent);
    background-size: 4px 4px;
    margin: 0 0 24px 0;
`
const ActivityGrid = styled.div`
    display: grid;
    grid-template-columns: repeat(1, 1fr);
    gap: 8px;

    @media ${breakpoints.medium} {
        grid-template-columns: repeat(2, 1fr);
    }
    @media ${breakpoints.wide} {
        grid-template-columns: repeat(3, 1fr);
    }
`