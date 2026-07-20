import { test, expect } from '@playwright/test'
import { continueToDashboard, uniqueTitle } from '../fixtures/app'
import { createTaskViaApi } from '../fixtures/api'

test.describe('Global search', () => {
  test('search navigates to an entity', async ({ page }) => {
    const title = await uniqueTitle('Searchable Task')
    await createTaskViaApi(title)

    await continueToDashboard(page)

    const search = page.getByLabel('Search')
    await search.fill(title)
    const results = page.getByLabel('Search results')
    await expect(results.getByText(title)).toBeVisible({ timeout: 20_000 })
    await results.getByRole('option', { name: new RegExp(title) }).click()

    await expect(page).toHaveURL(/#\/tasks\//)
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })
})
