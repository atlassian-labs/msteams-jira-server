// Karma configuration file, see link for more information
// https://karma-runner.github.io/1.0/config/configuration-file.html

module.exports = function (config) {
    config.set({
        basePath: '',
        frameworks: ['jasmine', '@angular-devkit/build-angular'],
        plugins: [
            require('@angular-devkit/build-angular/plugins/karma'),
            require('karma-jasmine'),
            require('karma-chrome-launcher'),
            require('karma-jasmine-html-reporter'),
            require('karma-coverage-istanbul-reporter'),
            require('karma-trx-reporter')
        ],
        client: {
            clearContext: false // leave Jasmine Spec Runner output visible in browser
        },
        coverageIstanbulReporter: {
            dir: require('path').join(__dirname, '../coverage'),
            reports: ['html', 'lcovonly'],
            fixWebpackSourcePaths: true
        },
        reporters: ['progress', 'trx'],
        trxReporter: { outputFile: '../../../tests/MicrosoftTeamsIntegration.Jira.Tests/TestResults/test-results.trx', shortTestName: false },
        port: 9876,
        colors: true,
        logLevel: config.LOG_ERROR,
        autoWatch: true,
        browsers: ['ChromeHeadless'],
        customLaunchers: {
            HeadlessChrome: {
                base: 'ChromeHeadless',
                flags: ['--no-sandbox']
            }
        },
        singleRun: false,
        browserNoActivityTimeout: 120000
    });
};