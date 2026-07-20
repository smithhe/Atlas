import { test, expect } from '@playwright/test'
import { continueToDashboard, uniqueTitle } from '../fixtures/app'

test.describe('Quick Add', () => {
  test('creates a task from Quick Add', async ({ page }) => {
    await continueToDashboard(page)

    const title = await uniqueTitle('Quick Add Task')
    await page.getByRole('button', { name: '+ Quick Add' }).click()

    const modal = page.getByRole('dialog', { name: 'Quick Add' })
    await expect(modal).toBeVisible()
    await modal.getByRole('button', { name: 'Task', exact: true }).click()
    await modal.locator('.fieldLabel', { hasText: 'Title' }).locator('..').locator('input').fill(title)
    await modal.getByRole('button', { name: 'Create' }).click()

    await expect(page).toHaveURL(/#\/tasks\//)
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })
})
