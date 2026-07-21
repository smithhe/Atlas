import { test, expect } from '@playwright/test'
import { continueToDashboard, uniqueTitle } from '../fixtures/app'
import { ensureTeamMember } from '../fixtures/api'

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

  test('creates a risk from Quick Add', async ({ page }) => {
    await continueToDashboard(page)

    const title = await uniqueTitle('Quick Add Risk')
    await page.getByRole('button', { name: '+ Quick Add' }).click()

    const modal = page.getByRole('dialog', { name: 'Quick Add' })
    await expect(modal).toBeVisible()
    await modal.getByRole('button', { name: 'Risk', exact: true }).click()
    await modal.locator('.fieldLabel', { hasText: 'Title' }).locator('..').locator('input').fill(title)
    await modal.getByRole('button', { name: 'Create' }).click()

    await expect(page).toHaveURL(/#\/risks\//)
    await expect(page.getByLabel('Risk detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })

  test('creates a team note from Quick Add', async ({ page }) => {
    const member = await ensureTeamMember()
    await continueToDashboard(page)

    const noteTitle = await uniqueTitle('Quick Add Note')
    await page.getByRole('button', { name: '+ Quick Add' }).click()

    const modal = page.getByRole('dialog', { name: 'Quick Add' })
    await expect(modal).toBeVisible()
    await modal.getByRole('button', { name: 'Team note', exact: true }).click()
    await modal.locator('.fieldLabel', { hasText: 'Team member' }).locator('..').locator('select').selectOption({ label: member.name })
    await modal.locator('.fieldLabel', { hasText: 'Title (optional)' }).locator('..').locator('input').fill(noteTitle)
    await modal.locator('.fieldLabel', { hasText: /^Note$/ }).locator('..').locator('textarea').fill('Quick add note body.')
    await modal.getByRole('button', { name: 'Create' }).click()

    await expect(page).toHaveURL(new RegExp(`#/team/${member.id}/notes`))
    await expect(page.getByText(noteTitle)).toBeVisible({ timeout: 20_000 })
  })
})
