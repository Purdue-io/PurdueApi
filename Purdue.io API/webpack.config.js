const path = require('path');

module.exports = {
    entry: path.join(__dirname, '/Scripts/ts/queryTester.ts'),
    output: {
        filename: 'app.js',
        path: path.join(__dirname, '/Content/script')
    },
    module: {
        rules: [
            {
                test: /\.tsx?$/,
                loader: 'ts-loader',
                exclude: /node_modules/,
            },
        ]
    },
    resolve: {
        extensions: [".tsx", ".ts", ".js"]
    },
};