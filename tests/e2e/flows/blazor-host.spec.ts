import { test, expect } from '@playwright/test'

test.describe('Blazor host', () => {
  test('loads login shell on 5173', async ({ page }) => {
    test.setTimeout(120_000)

    await page.goto('/')

    // Wait for WASM-rendered Login (not the #app loading shell alone).
    await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible({
      timeout: 90_000,
    })
    await expect(page.getByRole('button', { name: 'Continue' })).toBeVisible()
    await expect(page).not.toHaveURL(/#\//)
  })

  test('hash shim rewrites #/dashboard to path', async ({ page }) => {
    test.setTimeout(120_000)

    await page.goto('/#/dashboard')
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 90_000 })
    await expect(page).not.toHaveURL(/#\//)
    await expect(page.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({
      timeout: 90_000,
    })
  })
})
