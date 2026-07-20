import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'

test.describe('Risks CRUD', () => {
  test('create and edit a risk', async ({ page }) => {
    await continueToDashboard(page)
    await openNav(page, 'Risks & Mitigation')
    await expect(page.locator('h2.pageTitle', { hasText: 'Risks' })).toBeVisible()

    const title = await uniqueTitle('E2E Risk')
    await page.getByRole('button', { name: 'Add risk' }).click()

    const list = page.getByLabel('Risk list')
    await expect(list.getByText('New risk').first()).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: /New risk/ }).first().click()

    const detail = page.getByLabel('Risk detail editor')
    await expect(detail.getByText(/Risk Detail|New risk/)).toBeVisible()

    const editBtn = detail.getByRole('button', { name: 'Edit' })
    if (await editBtn.isVisible().catch(() => false)) {
      await editBtn.click()
    }

    const titleInput = detail.locator('.field', { hasText: 'Title' }).locator('input')
    await expect(titleInput).toBeVisible()
    await titleInput.fill(title)
    await detail.getByRole('button', { name: 'Done' }).click()

    await expect(detail.getByText(title)).toBeVisible()
    await expect(list.getByText(title)).toBeVisible()
  })
})
