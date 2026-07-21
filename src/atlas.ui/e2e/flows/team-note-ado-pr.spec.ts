import { test, expect } from '@playwright/test'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'
import { ensureTeamMember } from '../fixtures/api'

test.describe('Team note ADO/PR round-trip', () => {
  test('create note with ADO/PR fields and see them after reload', async ({ page }) => {
    const member = await ensureTeamMember()
    await continueToDashboard(page)
    await openNav(page, 'Team')
    await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible()

    const memberList = page.getByLabel('Team member list')
    await expect(memberList.getByText(member.name)).toBeVisible({ timeout: 20_000 })
    await memberList.getByRole('button', { name: new RegExp(member.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    await page.getByLabel('Member tabs').getByRole('button', { name: 'Notes', exact: true }).click()

    await expect(page.getByLabel('Notes tab')).toBeVisible()

    const noteTitle = await uniqueTitle('E2E Note')
    const adoId = '424242'
    const prUrl = 'https://dev.azure.com/example/_git/repo/pullrequest/99'

    await page.getByRole('button', { name: '+ New' }).click()
    const modal = page.getByRole('dialog', { name: 'New note' })
    await expect(modal).toBeVisible()

    await modal.locator('.fieldLabel', { hasText: 'Title (optional)' }).locator('..').locator('input').fill(noteTitle)
    await modal.locator('.fieldLabel', { hasText: 'ADO work item id' }).locator('..').locator('input').fill(adoId)
    await modal.locator('.fieldLabel', { hasText: 'PR URL' }).locator('..').locator('input').fill(prUrl)
    await modal.locator('.fieldLabel', { hasText: /^Note$/ }).locator('..').locator('textarea').fill('Note body with ADO/PR fields.')

    const createWait = page.waitForResponse(
      (r) =>
        r.url().includes(`/team-members/${member.id}/notes`) &&
        r.request().method() === 'POST' &&
        r.ok(),
      { timeout: 20_000 },
    )
    await modal.getByRole('button', { name: 'Create' }).click()
    await createWait

    await expect(page.getByText(noteTitle)).toBeVisible({ timeout: 20_000 })
    const noteRow = page.locator('.memberNotesRow', { hasText: noteTitle })
    await expect(noteRow.getByText(`ADO: ${adoId}`)).toBeVisible()

    // Open note modal via tag chip and verify PR chip.
    await noteRow.getByTitle('Open note').click()
    const noteModal = page.getByRole('dialog').filter({ hasText: noteTitle })
    await expect(noteModal.getByText(`ADO: ${adoId}`)).toBeVisible()
    await expect(noteModal.locator('.chip', { hasText: /^PR$/ })).toBeVisible()
    await noteModal.getByRole('button', { name: 'Open full page' }).click()

    await expect(page).toHaveURL(new RegExp(`#/team/${member.id}/notes/`))
    const noteUrl = page.url()
    await page.reload()
    // Primary path: wait for shell hydration before asserting note fields.
    await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    // Safety net only if a rare bounce still occurs after hydration.
    if (!page.url().includes('/notes/')) {
      await page.goto(noteUrl)
      await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    }
    await expect(page.getByLabel('Note fields').getByText(adoId)).toBeVisible({ timeout: 30_000 })
    await expect(page.getByRole('link', { name: prUrl })).toBeVisible()
  })
})
