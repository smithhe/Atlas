import { test, expect } from '@playwright/test'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { continueToDashboard } from '../fixtures/app'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const OUT = path.resolve(__dirname, '../../../docs/migration-screenshots/blazor-cutover')

test.describe('Blazor cutover performance', () => {
  test.skip(
    process.env.ATLAS_BLAZOR_CUTOVER_PERF !== '1',
    'Set ATLAS_BLAZOR_CUTOVER_PERF=1 to record Blazor cold-load metrics.',
  )

  test('record dashboard transfer size and TTI', async ({ page, browser }) => {
    test.setTimeout(180_000)
    fs.mkdirSync(OUT, { recursive: true })

    await continueToDashboard(page)

    const context = await browser.newContext({ viewport: { width: 1440, height: 900 } })
    const perfPage = await context.newPage()
    await perfPage.goto('/dashboard')
    await expect(perfPage.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({
      timeout: 30_000,
    })
    await expect(perfPage.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })

    const client = await perfPage.context().newCDPSession(perfPage)
    await client.send('Network.enable')
    await client.send('Network.setCacheDisabled', { cacheDisabled: true })

    let transferSize = 0
    client.on('Network.loadingFinished', (e: { encodedDataLength?: number }) => {
      transferSize += e.encodedDataLength ?? 0
    })

    const start = Date.now()
    await perfPage.reload({ waitUntil: 'networkidle' })
    await expect(perfPage.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({
      timeout: 30_000,
    })
    await expect(perfPage.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    const ttiMs = Date.now() - start
    await client.detach().catch(() => undefined)
    await context.close()

    const perf = {
      route: '/dashboard',
      viewport: '1440x900',
      transferSizeBytes: transferSize,
      ttiMs,
      measuredAt: new Date().toISOString(),
      notes:
        'Hard reload of /dashboard with CDP Network.setCacheDisabled. TTI = reload start until Dashboard title visible and Search enabled. Transfer size = sum of Network.loadingFinished encodedDataLength. Blazor WASM host (dotnet run or nginx).',
    }
    fs.writeFileSync(path.join(OUT, 'PERFORMANCE.json'), JSON.stringify(perf, null, 2) + '\n')
  })
})
