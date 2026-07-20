import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'

test.describe('Tasks CRUD', () => {
  test('create and edit a task', async ({ page }) => {
    await continueToDashboard(page)
    await openNav(page, 'Tasks')
    await expect(page.locator('h2.pageTitle', { hasText: 'Tasks' })).toBeVisible()

    const title = await uniqueTitle('E2E Task')
    await page.getByRole('button', { name: 'Add task', exact: true }).click()

    const list = page.getByLabel('Task list')
    await expect(list.getByText('New task').first()).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: /New task/ }).first().click()

    const detail = page.getByLabel('Task detail editor')
    await expect(detail.getByText('Task Detail')).toBeVisible()

    // Add task auto-opens edit mode; if not, click Edit.
    const titleInput = detail.locator('.field', { hasText: 'Title' }).locator('input')
    if (!(await titleInput.isVisible().catch(() => false))) {
      await detail.getByRole('button', { name: 'Edit' }).click()
    }
    await expect(titleInput).toBeVisible()
    await titleInput.fill(title)
    await detail.getByRole('button', { name: 'Done' }).click()

    await expect(detail.getByText(title)).toBeVisible()
    await expect(list.getByText(title)).toBeVisible()
  })
})
