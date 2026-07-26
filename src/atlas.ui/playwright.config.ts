import { defineConfig, devices } from '@playwright/test'

const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5173'
const reactBaseline = process.env.ATLAS_REACT_BASELINE === '1'

export default defineConfig({
  testDir: './e2e/flows',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  timeout: 60_000,
  expect: { timeout: 15_000 },
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    actionTimeout: 15_000,
    navigationTimeout: 30_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    // Phase 2–7 default: Blazor host. React baseline capture sets ATLAS_REACT_BASELINE=1.
    command: reactBaseline
      ? 'npm run preview -- --host 127.0.0.1 --port 5173'
      : 'dotnet run --project ../frontend/Atlas.Ui/Atlas.Ui.csproj --launch-profile http',
    url: baseURL,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
  },
})
