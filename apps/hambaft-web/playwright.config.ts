import { defineConfig, devices } from '@playwright/test'

export default defineConfig({
  testDir: './playwright',
  testIgnore: 'local-demo.spec.ts',
  timeout: 120_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: 'http://127.0.0.1:5173', trace: 'retain-on-failure', locale: 'fa-IR' },
  projects: [{ name: 'chrome', use: { ...devices['Desktop Chrome'], channel: 'chrome' } }],
  webServer: [
    {
      command: 'dotnet run --project ../../src/Hambaft.Api --no-build --urls http://127.0.0.1:5297',
      url: 'http://127.0.0.1:5297/ready',
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npm run dev -- --host 127.0.0.1',
      url: 'http://127.0.0.1:5173',
      reuseExistingServer: true,
      timeout: 120_000,
    },
  ],
})
