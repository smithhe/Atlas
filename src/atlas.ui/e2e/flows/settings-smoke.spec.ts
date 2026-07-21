import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav } from '../fixtures/app'
import { getSettingsViaApi, updateSettingsViaApi } from '../fixtures/api'

test.describe('Settings smoke', () => {
  test('stale days persists after Save and reload', async ({ page }) => {
    const original = await getSettingsViaApi()
    const nextStaleDays = original.staleDays === 11 ? 12 : 11

    try {
      await continueToDashboard(page)
      await openNav(page, 'Settings')
      await expect(page.locator('h2.pageTitle', { hasText: 'Settings' })).toBeVisible()

      const input = page.getByTestId('settings-stale-days')
      await expect(input).toBeVisible()
      await input.fill(String(nextStaleDays))

      const saveWait = page.waitForResponse(
        (r) => r.url().includes('/settings') && r.request().method() === 'PUT' && r.ok(),
        { timeout: 20_000 },
      )
      await page.getByRole('button', { name: 'Save settings' }).click()
      await saveWait

      await page.reload()
      await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
      await expect(page.locator('h2.pageTitle', { hasText: 'Settings' })).toBeVisible()
      await expect(page.getByTestId('settings-stale-days')).toHaveValue(String(nextStaleDays))

      const saved = await getSettingsViaApi()
      expect(saved.staleDays).toBe(nextStaleDays)
    } finally {
      await updateSettingsViaApi(original)
    }
  })
})
