import { test, expect } from '@playwright/test'
import { continueToDashboard } from '../fixtures/app'

test.describe('Login → Dashboard', () => {
  test('Continue opens the dashboard', async ({ page }) => {
    await continueToDashboard(page)
    await expect(page).toHaveURL(/#\/dashboard/)
    await expect(page.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible()
  })
})
