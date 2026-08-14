import { test, expect } from '@playwright/test'
import { continueToDashboard } from '../fixtures/app'
import { createProjectViaApi, createTaskViaApi, ensureTeamMember } from '../fixtures/api'

test.describe('Deep-link refresh (Phase 8 nginx SPA fallback)', () => {
  test('refresh /tasks/{id} keeps task detail', async ({ page }) => {
    const title = `Deep Link Task ${Date.now()}`
    const id = await createTaskViaApi(title)
    await continueToDashboard(page)

    await page.goto(`/tasks/${id}`)
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({
      timeout: 20_000,
    })

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page).toHaveURL(new RegExp(`/tasks/${id}`))
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({
      timeout: 20_000,
    })
  })

  test('refresh /team/{id}/notes keeps notes tab', async ({ page }) => {
    const member = await ensureTeamMember()
    await continueToDashboard(page)

    await page.goto(`/team/${member.id}/notes`)
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page.getByLabel('Notes tab')).toBeVisible({ timeout: 20_000 })

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page).toHaveURL(new RegExp(`/team/${member.id}/notes`))
    await expect(page.getByLabel('Notes tab')).toBeVisible({ timeout: 20_000 })
  })

  test('refresh /projects/{id}?tab=tasks keeps tasks tab', async ({ page }) => {
    const name = `Deep Link Project ${Date.now()}`
    const id = await createProjectViaApi(name)
    await continueToDashboard(page)

    await page.goto(`/projects/${id}?tab=tasks`)
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page.getByLabel('Project tasks tab', { exact: true })).toBeVisible({ timeout: 20_000 })

    await page.reload()
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    await expect(page).toHaveURL(new RegExp(`/projects/${id}\\?tab=tasks`))
    await expect(page.getByLabel('Project tasks tab', { exact: true })).toBeVisible({ timeout: 20_000 })
  })
})
