import { test, expect } from '@playwright/test'
import { continueToDashboard, uniqueTitle } from '../fixtures/app'
import { createRiskViaApi, createTaskViaApi, ensureTeamMember } from '../fixtures/api'

test.describe('Global search', () => {
  test('search navigates to a task', async ({ page }) => {
    const title = await uniqueTitle('Searchable Task')
    await createTaskViaApi(title)

    await continueToDashboard(page)

    const search = page.getByRole('combobox', { name: 'Search' })
    await search.fill(title)
    const results = page.getByLabel('Search results')
    await expect(results.getByText(title)).toBeVisible({ timeout: 20_000 })
    await results.getByRole('option', { name: new RegExp(title) }).click()

    await expect(page).toHaveURL(/#\/tasks\//)
    await expect(page.getByLabel('Task detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })

  test('search navigates to a risk', async ({ page }) => {
    const title = await uniqueTitle('Searchable Risk')
    await createRiskViaApi(title)

    await continueToDashboard(page)

    const search = page.getByRole('combobox', { name: 'Search' })
    await search.fill(title)
    const results = page.getByLabel('Search results')
    await expect(results.getByText(title)).toBeVisible({ timeout: 20_000 })
    await results.getByRole('option', { name: new RegExp(title) }).click()

    await expect(page).toHaveURL(/#\/risks\//)
    await expect(page.getByLabel('Risk detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })

  test('search navigates to a person', async ({ page }) => {
    const member = await ensureTeamMember()

    await continueToDashboard(page)

    const search = page.getByRole('combobox', { name: 'Search' })
    await search.fill(member.name)
    const results = page.getByLabel('Search results')
    await expect(results.getByText(member.name)).toBeVisible({ timeout: 20_000 })
    await results.getByRole('option', { name: new RegExp(member.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    await expect(page).toHaveURL(new RegExp(`#/team/${member.id}`))
    await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible()
    await expect(page.getByText(member.name).first()).toBeVisible({ timeout: 20_000 })
  })
})
