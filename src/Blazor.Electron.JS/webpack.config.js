const path = require('path');
const webpack = require('webpack');
const TsconfigPathsPlugin = require('tsconfig-paths-webpack-plugin');

module.exports = (env, args) => ({
    target: 'electron-renderer',
    resolve: {
        extensions: ['.ts', '.js'],
        plugins: [new TsconfigPathsPlugin()]
    },
    devtool: args.mode === 'development' ? 'inline-source-map' : 'none',
    module: {
        rules: [{ test: /\.ts?$/, loader: 'ts-loader' }]
    },
    entry: {
        'blazor.electron': './src/Boot.Electron.ts'
    },
    output: { path: path.join(__dirname, '/dist'), filename: '[name].js' }
});
