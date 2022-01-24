import React from 'react'
import styled from 'styled-components'
import { fonts, colors, breakpoints } from './GlobalStyles';
import logoMedium from '../images/logo-medium.png'
import logoLarge from '../images/logo-large.png'
import headerBackgroundPattern from '../images/header-bg-pattern.inline.png'

const Header: React.FC = () => {
    return (
        <OuterContainer>
            <InnerContainer>
                <Logo />
                <AppTitle>
                    <Line1>Today in</Line1>
                    <br />
                    <Line2>Destiny 2</Line2>
                </AppTitle>
            </InnerContainer>
        </OuterContainer>
    );
}
export default Header;

const OuterContainer = styled.div`
    padding: 0 12px;
    background-image: url(${headerBackgroundPattern});
    height: 100px;

    @media ${breakpoints.medium} {
        height: 140px;
    }
`
const InnerContainer = styled.div`
    max-width: 1300px;
    margin: 0px auto;
`
const Logo = styled.div`
    background-image: url(${logoMedium});
    background-size: cover;
    width: 54px;
    height: 87px;
    float: left;
    margin-right: 12px;

    @media ${breakpoints.medium} {
        background-image: url(${logoLarge});
        width: 72px;
        height: 124px;
        margin-right: 16px;
    }
`
const AppTitle = styled.h1`
    margin: 0;
    padding-top: 4px;

    @media ${breakpoints.medium} {
        padding-top: 12px;
    }
`
const Line1 = styled.span`
    ${fonts.bebas.regular}
    font-size: 24px;
    color: ${colors.secondary};

    @media ${breakpoints.medium} {
        font-size: 32px;
    }
`
const Line2 = styled.span`
    ${fonts.bebas.regular}
    font-size: 48px;
    color: ${colors.primary};
    line-height: 36px;

    @media ${breakpoints.medium} {
        font-size: 72px;
        line-height: 64px;
    }
`