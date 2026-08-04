import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './playwright',
  testMatch: 'local-demo.spec.ts',
  timeout: 60_000,
  reporter: 'list',
  use: { baseURL: 'http://127.0.0.1:5297', locale: 'fa-IR' },
  projects: [{ name: 'chrome-demo', use: { ...devices['Desktop Chrome'], channel: 'chrome' } }],
  webServer: {
    command: 'dotnet run --project ../../src/Hambaft.Api --no-build --urls http://127.0.0.1:5297',
    url: 'http://127.0.0.1:5297/ready',
    reuseExistingServer: true,
    timeout: 120_000,
  },
})
