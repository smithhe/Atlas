import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav } from '../fixtures/app'

test.describe('Shell navigation', () => {
  test('primary nav links show expected page titles', async ({ page }) => {
    await continueToDashboard(page)

    const pages: Array<{ nav: string; title: string }> = [
      { nav: 'Dashboard', title: 'Dashboard' },
      { nav: 'Tasks', title: 'Tasks' },
      { nav: 'Team', title: 'Team' },
      { nav: 'Risks & Mitigation', title: 'Risks & Mitigation' },
      { nav: 'Projects', title: 'Projects' },
      { nav: 'Settings', title: 'Settings' },
    ]

    for (const entry of pages) {
      await openNav(page, entry.nav)
      await expect(page.locator('h2.pageTitle', { hasText: entry.title })).toBeVisible()
    }
  })

  test('unknown route shows Not Found and Back to Dashboard', async ({ page }) => {
    await continueToDashboard(page)
    await page.goto('/#/this-route-does-not-exist')
    await expect(page.locator('h2.pageTitle', { hasText: 'Not Found' })).toBeVisible()
    await page.getByRole('link', { name: 'Back to Dashboard' }).click()
    await expect(page).toHaveURL(/#\/dashboard/)
    await expect(page.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible()
  })
})
