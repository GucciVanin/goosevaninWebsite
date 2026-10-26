import { defineConfig, devices } from '@playwright/test';

// Browser tests (TE5). They run inside the verification container against the live stack. Inside that container
// https://localhost is reached through helpers/forward.cjs (a TCP forwarder to Caddy), so the stack's own
// certificate and host name are used unchanged; the certificate is self-signed, hence ignoreHTTPSErrors.
//
// VERIFY_BROWSERS limits the engines (comma list of chromium, firefox, webkit); the default is all three.
const wanted = (process.env.VERIFY_BROWSERS || 'chromium,firefox,webkit')
  .split(',')
  .map((name) => name.trim())
  .filter(Boolean);

const engines = [
  { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
  { name: 'webkit', use: { ...devices['Desktop Safari'] } },
];

export default defineConfig({
  testDir: './tests',
  outputDir: process.env.VERIFY_BROWSER_OUTPUT ?? '/tmp/verify-browser-output',
  globalSetup: './helpers/global-setup.ts',
  globalTeardown: './helpers/global-teardown.ts',
  fullyParallel: true,
  workers: 2,
  retries: 0,
  timeout: 45_000,
  expect: { timeout: 10_000 },
  reporter: [['./helpers/jsonl-reporter.ts'], ['line']],
  use: {
    baseURL: 'https://localhost',
    ignoreHTTPSErrors: true,
    locale: 'en-US',
    timezoneId: 'UTC',
    trace: 'off',
    screenshot: 'off',
    video: 'off',
  },
  projects: engines.filter((engine) => wanted.includes(engine.name)),
});
