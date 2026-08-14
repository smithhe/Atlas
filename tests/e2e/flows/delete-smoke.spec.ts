import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'
import { createTaskViaApi } from '../fixtures/api'

test.describe('Delete smoke', () => {
  test('delete task removes it from the list', async ({ page }) => {
    const title = await uniqueTitle('Delete Task')
    await createTaskViaApi(title)

    await continueToDashboard(page)
    await openNav(page, 'Tasks')

    const list = page.getByLabel('Task list')
    await expect(list.getByText(title)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    const detail = page.getByLabel('Task detail editor')
    await expect(detail.getByText(title)).toBeVisible()

    page.once('dialog', async (dialog) => {
      await dialog.accept()
    })
    await detail.getByRole('button', { name: 'Delete' }).click()

    await expect(list.getByText(title)).toHaveCount(0, { timeout: 20_000 })
  })
})
