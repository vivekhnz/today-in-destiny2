const path = require('path')
const HtmlWebpackPlugin = require('html-webpack-plugin')
const TerserPlugin = require('terser-webpack-plugin')
const ForkTsCheckerWebpackPlugin = require('fork-ts-checker-webpack-plugin')

module.exports = {
    entry: path.resolve(__dirname, 'src', 'index.tsx'),
    output: {
        path: path.resolve(__dirname, 'dist'),
        filename: 'bundle.js'
    },
    module: {
        rules: [
            {
                test: /\.[jt]sx?$/,
                use: 'babel-loader'
            },
            {
                test: /\.png$/,
                type: 'asset/resource'
            },
            {
                test: /\.inline\.png$/,
                type: 'asset/inline'
            },
            {
                test: /\.woff2?$/,
                type: 'asset/resource'
            }
        ]
    },
    resolve: {
        extensions: ['.tsx', '.ts', '.js']
    },
    plugins: [
        new HtmlWebpackPlugin({
            template: path.resolve(__dirname, 'src', 'index.html'),
            favicon: path.resolve(__dirname, 'images', 'favicon.ico')
        }),
        new ForkTsCheckerWebpackPlugin()
    ],
    optimization: {
        minimizer: [
            new TerserPlugin({ extractComments: false })
        ]
    },
    devServer: {
        static: {
            directory: path.resolve(__dirname, '../data'),
            publicPath: '/__data'
        }
    }
}