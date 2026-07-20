import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'

test.describe('Projects CRUD', () => {
  test('create and edit a project via prompt', async ({ page }) => {
    await continueToDashboard(page)
    await openNav(page, 'Projects')
    await expect(page.locator('h2.pageTitle', { hasText: 'Projects' })).toBeVisible()

    const name = await uniqueTitle('E2E Project')
    page.once('dialog', async (dialog) => {
      await dialog.accept(name)
    })
    await page.getByRole('button', { name: 'Add project' }).click()

    const projectList = page.getByLabel('Project list')
    await expect(projectList.getByText(name)).toBeVisible({ timeout: 20_000 })
    await projectList.getByRole('button', { name: new RegExp(name) }).click()

    const detail = page.getByLabel('Project detail')
    await expect(detail.getByText(name)).toBeVisible()

    // Newly created projects often open in edit mode; otherwise click Edit.
    const saveBtn = detail.getByRole('button', { name: 'Save' })
    if (!(await saveBtn.isVisible().catch(() => false))) {
      await detail.getByRole('button', { name: 'Edit' }).click()
    }
    await expect(saveBtn).toBeVisible()

    const nameInput = detail.getByLabel('Edit overview').locator('.fieldLabel', { hasText: 'Name' }).locator('..').locator('input')
    const edited = `${name} edited`
    await nameInput.fill(edited)
    await saveBtn.click()

    await expect(detail.getByText(edited)).toBeVisible({ timeout: 15_000 })
    await expect(projectList.getByText(edited)).toBeVisible()
  })
})
