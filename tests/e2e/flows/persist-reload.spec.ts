import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'
import { createProjectViaApi, createRiskViaApi, createTaskViaApi } from '../fixtures/api'

test.describe('Persist + reload', () => {
  test('task title survives reload in list and detail', async ({ page }) => {
    const seed = await uniqueTitle('Persist Task')
    const edited = `${seed} edited`
    await createTaskViaApi(seed)

    await continueToDashboard(page)
    await openNav(page, 'Tasks')

    const list = page.getByLabel('Task list')
    await expect(list.getByText(seed)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(seed.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    const detail = page.getByLabel('Task detail editor')
    const titleInput = detail.locator('.field', { hasText: 'Title' }).locator('input')
    if (!(await titleInput.isVisible().catch(() => false))) {
      await detail.getByRole('button', { name: 'Edit' }).click()
    }
    await expect(titleInput).toBeVisible()
    const saveWait = page.waitForResponse(
      (r) => r.url().includes('/tasks/') && r.request().method() === 'PUT' && r.ok(),
      { timeout: 20_000 },
    )
    await titleInput.fill(edited)
    await saveWait
    await detail.getByRole('button', { name: 'Done' }).click()
    await expect(detail.getByText(edited)).toBeVisible()
    await expect(list.getByText(edited)).toBeVisible()

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(list.getByText(edited)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(edited.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()
    await expect(detail.getByText(edited)).toBeVisible()
  })

  test('risk title survives reload in list and detail', async ({ page }) => {
    const seed = await uniqueTitle('Persist Risk')
    const edited = `${seed} edited`
    await createRiskViaApi(seed)

    await continueToDashboard(page)
    await openNav(page, 'Risks & Mitigation')

    const list = page.getByLabel('Risk list')
    await expect(list.getByText(seed)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(seed.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    const detail = page.getByLabel('Risk detail editor')
    const editBtn = detail.getByRole('button', { name: 'Edit' })
    if (await editBtn.isVisible().catch(() => false)) {
      await editBtn.click()
    }
    const titleInput = detail.locator('.field', { hasText: 'Title' }).locator('input')
    await expect(titleInput).toBeVisible()
    const saveWait = page.waitForResponse(
      (r) => r.url().includes('/risks/') && r.request().method() === 'PUT' && r.ok(),
      { timeout: 20_000 },
    )
    await titleInput.fill(edited)
    await saveWait
    await detail.getByRole('button', { name: 'Done' }).click()
    await expect(detail.getByText(edited)).toBeVisible()
    await expect(list.getByText(edited)).toBeVisible()

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(list.getByText(edited)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(edited.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()
    await expect(detail.getByText(edited)).toBeVisible()
  })

  test('project name survives reload in list and detail', async ({ page }) => {
    const seed = await uniqueTitle('Persist Project')
    const edited = `${seed} edited`
    await createProjectViaApi(seed)

    await continueToDashboard(page)
    await openNav(page, 'Projects')

    const list = page.getByLabel('Project list')
    await expect(list.getByText(seed)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(seed.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    const detail = page.getByLabel('Project detail')
    const saveBtn = detail.getByRole('button', { name: 'Save' })
    if (!(await saveBtn.isVisible().catch(() => false))) {
      await detail.getByRole('button', { name: 'Edit' }).click()
    }
    await expect(saveBtn).toBeVisible()
    const nameInput = detail.getByLabel('Edit overview').locator('.fieldLabel', { hasText: 'Name' }).locator('..').locator('input')
    await nameInput.fill(edited)
    const saveWait = page.waitForResponse(
      (r) => r.url().includes('/projects/') && r.request().method() === 'PUT' && r.ok(),
      { timeout: 20_000 },
    )
    await saveBtn.click()
    await saveWait
    await expect(detail.getByText(edited)).toBeVisible({ timeout: 15_000 })
    await expect(list.getByText(edited)).toBeVisible()

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(list.getByText(edited)).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: new RegExp(edited.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()
    await expect(detail.getByText(edited)).toBeVisible()
  })
})
