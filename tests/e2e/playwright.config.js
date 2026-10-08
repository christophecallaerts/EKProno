import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const here = path.dirname(fileURLToPath(import.meta.url));
const projectFile = path.resolve(here, '../../EKProno.csproj');

// A port of its own, so a `dotnet run` you already have open on 5298 keeps its data.
const port = Number(process.env.E2E_PORT ?? 5299);
const baseURL = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${port}`;

// The app writes its JSON store here instead of App_Data (see `DataStore:Path` in
// Program.cs). globalSetup deletes it, so every run starts from an empty database.
export const dataStorePath = path.join(here, '.artifacts', 'ekprono.e2e.json');

const configuration = process.env.E2E_CONFIGURATION ?? 'Debug';
const webServerCommand = [
  'dotnet run',
  `--project "${projectFile}"`,
  `--configuration ${configuration}`,
  // launchSettings.json pins its own URLs; we want the ones below.
  '--no-launch-profile',
  // Build somewhere of our own. A `dotnet run` or `dotnet watch` the developer already
  // has open holds a lock on bin/, and without this the E2E build fails on MSB3027.
  process.env.E2E_NO_BUILD === 'true'
    ? '--no-build'
    : `--artifacts-path "${path.join(here, '.artifacts', 'build')}"`,
]
  .filter(Boolean)
  .join(' ');

export default defineConfig({
  testDir: './tests',
  globalSetup: './global-setup.js',

  // Each test signs in as its own freshly generated organiser, so nothing is shared and
  // the files can run together.
  fullyParallel: true,
  workers: process.env.CI ? 2 : undefined,

  // A test that only passes on a retry is a bug worth seeing, so never retry locally.
  retries: process.env.CI ? 1 : 0,
  forbidOnly: !!process.env.CI,

  timeout: 30_000,
  expect: { timeout: 10_000 },

  reporter: process.env.CI
    ? [['list'], ['html', { open: 'never' }], ['junit', { outputFile: 'test-results/e2e-results.xml' }]]
    : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL,
    // Kickoffs render in the viewer's zone (FR-009), so pin one or the clock assertions
    // move with whoever runs the suite.
    timezoneId: 'Europe/Amsterdam',
    locale: 'en-GB',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],

  webServer: {
    command: webServerCommand,
    url: `${baseURL}/Account/SignIn`,
    reuseExistingServer: !process.env.CI,
    // A cold `dotnet run` has to restore and build first.
    timeout: 180_000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: `http://127.0.0.1:${port}`,
      DataStore__Path: dataStorePath,
    },
  },
});
