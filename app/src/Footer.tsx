import React from 'react'
import styled from 'styled-components'
import { fonts, colors } from './GlobalStyles';

const Footer: React.FC = () => {
    const year = new Date().getFullYear();
    return (
        <FooterText>
            <Line1>&copy; {year} Vivek Hari</Line1>
            <br />
            <Line2>Not affiliated with Bungie</Line2>
        </FooterText>
    );
}
export default Footer;

const FooterText = styled.p`
    margin: 24px 0;
    ${fonts.bender.bold}
`
const Line1 = styled.span`
    color: ${colors.primary};
`
const Line2 = styled.span`
    color: ${colors.secondary};
`
