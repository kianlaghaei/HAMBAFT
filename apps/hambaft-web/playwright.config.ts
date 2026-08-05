import { defineConfig, devices } from '@playwright/test'

const apiPort = Number(process.env.HAMBAFT_E2E_API_PORT ?? 5297)
const webPort = Number(process.env.HAMBAFT_E2E_WEB_PORT ?? 5173)

export default defineConfig({
  testDir: './playwright',
  testIgnore: 'local-demo.spec.ts',
  timeout: 300_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: { baseURL: `http://127.0.0.1:${webPort}`, trace: 'retain-on-failure', locale: 'fa-IR' },
  projects: [{ name: 'chrome', use: { ...devices['Desktop Chrome'], channel: 'chrome' } }],
  webServer: [
    {
      command: `dotnet run --project ../../src/Hambaft.Api --no-build --urls http://127.0.0.1:${apiPort}`,
      url: `http://127.0.0.1:${apiPort}/ready`,
      reuseExistingServer: false,
      timeout: 120_000,
    },
    {
      command: `npm run dev -- --host 127.0.0.1 --port ${webPort} --strictPort`,
      url: `http://127.0.0.1:${webPort}`,
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
})
