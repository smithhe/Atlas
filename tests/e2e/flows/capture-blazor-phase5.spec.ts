import { test, expect, type Page } from '@playwright/test'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'
import { continueToDashboard, openNav } from '../fixtures/app'

/**
 * Phase 5 Blazor screenshot evidence vs docs/migration-screenshots/react-baseline/.
 * Run: ATLAS_BLAZOR_PHASE5_SHOTS=1 npx playwright test flows/capture-blazor-phase5.spec.ts
 * (Evidence only — skipped unless ATLAS_BLAZOR_PHASE5_SHOTS=1.)
 */

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const OUT = path.resolve(__dirname, '../../../docs/migration-screenshots/blazor-phase5')

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

test.describe('Blazor Phase 5 screenshots', () => {
  test.skip(!process.env.ATLAS_BLAZOR_PHASE5_SHOTS, 'Set ATLAS_BLAZOR_PHASE5_SHOTS=1 to capture')

  for (const vp of VIEWPORTS) {
    test(`capture ${vp.name}`, async ({ page }) => {
      test.setTimeout(240_000)
      await page.setViewportSize({ width: vp.width, height: vp.height })

      await continueToDashboard(page)
      await shot(page, vp.name, '04-dashboard')

      await openNav(page, 'Tasks')
      await expect(page.locator('h2.pageTitle', { hasText: 'Tasks' })).toBeVisible()
      await shot(page, vp.name, '05-tasks-list')
      const taskRow = page.getByLabel('Task list').getByRole('button').first()
      if (await taskRow.isVisible().catch(() => false)) {
        await taskRow.click()
        await expect(page.getByLabel('Task detail editor')).toBeVisible()
        await shot(page, vp.name, '06-tasks-detail')
      }

      await openNav(page, 'Risks & Mitigation')
      await expect(page.locator('h2.pageTitle', { hasText: 'Risks' })).toBeVisible()
      await shot(page, vp.name, '07-risks-list')
      const riskRow = page.getByLabel('Risk list').getByRole('button').first()
      if (await riskRow.isVisible().catch(() => false)) {
        await riskRow.click()
        await expect(page.getByLabel('Risk detail editor')).toBeVisible()
        await shot(page, vp.name, '08-risks-detail')
      }

      await openNav(page, 'Projects')
      await expect(page.locator('h2.pageTitle', { hasText: 'Projects' })).toBeVisible()
      await shot(page, vp.name, '09-projects-list')
      const projectRow = page.getByLabel('Project list').getByRole('button').first()
      if (await projectRow.isVisible().catch(() => false)) {
        await projectRow.click()
        await expect(page.getByRole('region', { name: 'Project detail', exact: true })).toBeVisible()
        await shot(page, vp.name, '10-projects-detail')
      }

      await openNav(page, 'Team')
      await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible()
      await shot(page, vp.name, '11-team')
      const memberRow = page.getByRole('button').filter({ has: page.locator('.listTitle') }).first()
      if (await memberRow.isVisible().catch(() => false)) {
        await memberRow.click()
        await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible()
        await shot(page, vp.name, '12-team-member')
      }

      await openNav(page, 'Settings')
      await expect(page.locator('h2.pageTitle', { hasText: 'Settings' })).toBeVisible()
      await expect(page.getByTestId('settings-stale-days')).toBeVisible()
      await shot(page, vp.name, '13-settings')

      await page.getByRole('combobox', { name: 'Search' }).fill('a')
      await expect(page.getByLabel('Search results')).toBeVisible()
      await shot(page, vp.name, '26-shell-search-open')
      await page.getByRole('combobox', { name: 'Search' }).fill('')

      await page.getByRole('button', { name: '+ Quick Add' }).click()
      await expect(page.getByRole('dialog', { name: 'Quick Add' })).toBeVisible()
      await shot(page, vp.name, '27-shell-quick-add')
    })
  }
})
