import { test, expect } from '@playwright/test'

test.describe('Blazor host', () => {
  test('loads bootstrap shell on 5173', async ({ page }) => {
    test.setTimeout(120_000)

    await page.goto('/')

    // Wait for WASM-rendered content (not the #app loading shell alone).
    await expect(page.getByRole('heading', { name: 'Atlas' })).toBeVisible({
      timeout: 90_000,
    })
    await expect(page.getByText(/Blazor WebAssembly host scaffold/i)).toBeVisible()
    await expect(page).not.toHaveURL(/#\//)
  })
})
