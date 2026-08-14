import { test, expect, type Page } from '@playwright/test'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'
import { continueToDashboard } from '../fixtures/app'

/**
 * Phase 4 Blazor screenshot evidence vs docs/migration-screenshots/react-baseline/.
 * Run: ATLAS_BLAZOR_PHASE4_SHOTS=1 npx playwright test flows/capture-blazor-phase4.spec.ts
 * (Evidence only — skipped unless ATLAS_BLAZOR_PHASE4_SHOTS=1.)
 */

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const OUT = path.resolve(__dirname, '../../../docs/migration-screenshots/blazor-phase4')

const VIEWPORTS = [
  { name: 'desktop-1440x900', width: 1440, height: 900 },
  { name: 'tablet-1100x800', width: 1100, height: 800 },
  { name: 'narrow-700x900', width: 700, height: 900 },
] as const

async function shot(page: Page, viewport: string, name: string) {
  const dir = path.join(OUT, viewport)
  fs.mkdirSync(dir, { recursive: true })
  const file = path.join(dir, `${name}.png`)
  await page.screenshot({ path: file, fullPage: false })
  return file
}

test.describe('Blazor Phase 4 screenshots', () => {
  test.skip(!process.env.ATLAS_BLAZOR_PHASE4_SHOTS, 'Set ATLAS_BLAZOR_PHASE4_SHOTS=1 to capture')

  for (const vp of VIEWPORTS) {
    test(`capture ${vp.name}`, async ({ page }) => {
      test.setTimeout(180_000)
      await page.setViewportSize({ width: vp.width, height: vp.height })

      await page.goto('/')
      await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible({ timeout: 90_000 })
      await shot(page, vp.name, '01-login-index')

      await page.goto('/login')
      await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible()
      await shot(page, vp.name, '02-login')

      await page.goto('/setup')
      await expect(page.locator('h2.pageHeaderTitle', { hasText: 'Azure Setup' })).toBeVisible()
      await shot(page, vp.name, '03-setup')

      await continueToDashboard(page)
      await shot(page, vp.name, '04-dashboard')

      await page.getByRole('combobox', { name: 'Search' }).click()
      await page.getByRole('combobox', { name: 'Search' }).fill('a')
      await shot(page, vp.name, '26-shell-search-open')
      await page.getByRole('combobox', { name: 'Search' }).fill('')

      await page.getByRole('button', { name: '+ Quick Add' }).click()
      await expect(page.getByText('Quick Add').first()).toBeVisible()
      await shot(page, vp.name, '27-shell-quick-add')
    })
  }
})
