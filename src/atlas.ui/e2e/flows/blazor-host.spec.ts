import { test, expect } from '@playwright/test'

test.describe('Blazor host', () => {
  test('loads bootstrap shell on 5173', async ({ page }) => {
    await page.goto('/')

    await expect(page.locator('#app')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Atlas' })).toBeVisible({
      timeout: 60_000,
    })
    await expect(page).not.toHaveURL(/#\//)
  })
})
