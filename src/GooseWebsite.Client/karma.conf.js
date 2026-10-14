// Karma configuration for the container verification run (TE10): one log line per test, and a headless
// Chrome launcher that works as root inside a container. `npm test` keeps using Angular's default
// configuration; pass `--karma-config=karma.conf.js --browsers=ChromeHeadlessNoSandbox` to use this one.
module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['jasmine'],
    plugins: [
      require('karma-jasmine'),
      require('karma-chrome-launcher'),
      require('karma-spec-reporter'),
    ],
    reporters: ['spec'],
    specReporter: {
      suppressSkipped: false,
      suppressPassed: false,
      suppressErrorSummary: false,
      showSpecTiming: true,
    },
    customLaunchers: {
      ChromeHeadlessNoSandbox: {
        base: 'ChromeHeadless',
        flags: ['--no-sandbox', '--disable-gpu', '--disable-dev-shm-usage'],
      },
    },
    restartOnFileChange: false,
  });
};
