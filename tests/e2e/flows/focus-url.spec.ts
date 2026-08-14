import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'
import { createTaskViaApi } from '../fixtures/api'

test.describe('List ↔ detail focus URL', () => {
  test('Focus sets /tasks/:id, survives reload, Exit focus returns to list', async ({ page }) => {
    const title = await uniqueTitle('Focus Task')
    const id = await createTaskViaApi(title)

    await continueToDashboard(page)
    await openNav(page, 'Tasks')

    const list = page.getByLabel('Task list')
    await expect(list.getByText(title)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    const detail = page.getByLabel('Task detail editor')
    await expect(detail.getByText(title)).toBeVisible()
    await detail.getByRole('button', { name: 'Focus' }).click()

    await expect(page).toHaveURL(new RegExp(`/tasks/${id}`))
    await expect(detail.getByText(title)).toBeVisible()

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page).toHaveURL(new RegExp(`/tasks/${id}`))
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })

    await page.getByLabel('Task detail editor').getByRole('button', { name: 'Exit focus' }).click()
    await expect(page).toHaveURL(/\/tasks\/?$/)
    await expect(page.getByLabel('Task list').getByText(title)).toBeVisible()
  })
})
