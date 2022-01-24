import { createGlobalStyle, css } from 'styled-components'
import BebasNeue_Regular_Woff from '../fonts/bebasneue_regular-webfont.woff'
import BebasNeue_Regular_Woff2 from '../fonts/bebasneue_regular-webfont.woff2'
import BebasNeue_Bold_Woff from '../fonts/bebasneue_bold-webfont.woff'
import BebasNeue_Bold_Woff2 from '../fonts/bebasneue_bold-webfont.woff2'
import Bender_Bold_Woff from '../fonts/bender_bold-webfont.woff'
import Bender_Bold_Woff2 from '../fonts/bender_bold-webfont.woff2'

interface Font {
    family: string,
    woff: string,
    woff2: string,
    weight: string,
    style: string,
    fallbackFamilies: string[]
}

function createFontFace(font: Font) {
    return {
        declaration: css`
            @font-face {
                font-family: '${font.family}';
                src: url(${font.woff2}) format('woff2'), url(${font.woff}) format('woff');
                font-weight: ${font.weight};
                font-style: ${font.style};
            }
        `,
        usage: css`
            font-family: ${[font.family, ...font.fallbackFamilies].map(family => `${family}`).join(', ')};
            font-weight: ${font.weight};
            font-style: ${font.style};
        `
    };
}

const bebasRegular = createFontFace({
    family: 'bebas',
    woff: BebasNeue_Regular_Woff,
    woff2: BebasNeue_Regular_Woff2,
    weight: 'normal',
    style: 'normal',
    fallbackFamilies: ['Segoe UI', 'Helvetica', 'sans-serif']
});
const bebasBold = createFontFace({
    family: 'bebas',
    woff: BebasNeue_Bold_Woff,
    woff2: BebasNeue_Bold_Woff2,
    weight: 'bold',
    style: 'normal',
    fallbackFamilies: ['Segoe UI', 'Helvetica', 'sans-serif']
});
const benderBold = createFontFace({
    family: 'bender',
    woff: Bender_Bold_Woff,
    woff2: Bender_Bold_Woff2,
    weight: 'bold',
    style: 'normal',
    fallbackFamilies: ['Segoe UI', 'Helvetica', 'sans-serif']
});

export const fonts = {
    bebas: {
        regular: bebasRegular.usage,
        bold: bebasBold.usage
    },
    bender: {
        bold: benderBold.usage
    }
}

export const colors = {
    background: '#d6d6d9',
    primary: '#273a41',
    secondary: '#486e7e',
    tertiary: '#93a3ae'
}

export const breakpoints = {
    medium: '(min-width: 760px)',
    wide: '(min-width: 1200px)'
}

export const GlobalStyles = createGlobalStyle`
${bebasRegular.declaration}
${bebasBold.declaration}
${benderBold.declaration}

body, p, ul {
    margin: 0;
}
body {
    font-family: sans-serif;
    background-color: ${colors.background};
}
`