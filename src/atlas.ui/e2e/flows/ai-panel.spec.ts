import { test, expect } from '@playwright/test'
import { continueToDashboard, openAiPanel, openNav, uniqueTitle } from '../fixtures/app'
import { stubAiConversation, STUB_DRAFT } from '../fixtures/ai'

test.describe('AI panel', () => {
  test('shows missing-key guidance against real API', async ({ page }) => {
    await continueToDashboard(page)
    await openAiPanel(page)

    const panel = page.locator('aside[aria-label="AI panel"]')
    await panel.locator('textarea.aiPanelPrompt').fill('Summarize my dashboard.')
    await panel.getByRole('button', { name: 'Send Prompt' }).click()

    await expect(panel.getByText('OpenAI API key required')).toBeVisible({ timeout: 45_000 })
    await expect(panel.locator('code', { hasText: 'OpenAI__ApiKey' }).first()).toBeVisible()
  })

  test('starts a stubbed conversation on multiple views', async ({ page }) => {
    await continueToDashboard(page)
    await stubAiConversation(page)

    const views: Array<{ nav: string; context: RegExp }> = [
      { nav: 'Dashboard', context: /Context:\s*Dashboard/i },
      { nav: 'Tasks', context: /Context:\s*Tasks/i },
      { nav: 'Risks & Mitigation', context: /Context:\s*Risks/i },
      { nav: 'Projects', context: /Context:\s*Projects/i },
      { nav: 'Settings', context: /Context:\s*Settings/i },
    ]

    for (const view of views) {
      await openNav(page, view.nav)
      await openAiPanel(page)
      const panel = page.locator('aside[aria-label="AI panel"]')
      await expect(panel.locator('.aiPanelTitle')).toHaveText(view.context)

      await panel.getByRole('button', { name: 'New session' }).click()
      const prompt = `Hello from ${view.nav} ${Date.now()}`
      await panel.locator('textarea.aiPanelPrompt').fill(prompt)
      await panel.getByRole('button', { name: 'Send Prompt' }).click()

      await expect(panel.getByLabel('AI conversation').getByText(prompt)).toBeVisible({ timeout: 20_000 })
      await expect(panel.getByLabel('AI conversation').getByText(STUB_DRAFT)).toBeVisible({ timeout: 20_000 })
      // Terminal SSE must settle on Completed — not overwrite to Failed via EventSource onerror.
      await expect(panel.locator('.aiPanelMeta')).toContainText(/Status:\s*Completed/i, { timeout: 10_000 })
      await expect(panel.locator('.aiPanelMeta')).not.toContainText(/Status:\s*Failed/i)
    }
  })

  test('Insert Draft writes into a focused task notes editor', async ({ page }) => {
    await continueToDashboard(page)
    await stubAiConversation(page)
    await openNav(page, 'Tasks')

    const title = await uniqueTitle('Draft Target Task')
    await page.getByRole('button', { name: 'Add task', exact: true }).click()
    const list = page.getByLabel('Task list')
    await expect(list.getByText('New task').first()).toBeVisible({ timeout: 20_000 })
    await list.getByRole('button', { name: /New task/ }).first().click()

    const detail = page.getByLabel('Task detail editor')
    const titleInput = detail.locator('.field', { hasText: 'Title' }).locator('input')
    if (!(await titleInput.isVisible().catch(() => false))) {
      await detail.getByRole('button', { name: 'Edit' }).click()
    }
    await expect(titleInput).toBeVisible()
    await titleInput.fill(title)

    const notes = detail.locator('.field', { hasText: 'Notes' }).locator('textarea')
    await expect(notes).toBeVisible()
    await notes.click()

    await openAiPanel(page)
    const panel = page.locator('aside[aria-label="AI panel"]')
    await panel.locator('textarea.aiPanelPrompt').fill('Draft some notes for this task.')
    await panel.getByRole('button', { name: 'Send Prompt' }).click()
    await expect(panel.getByLabel('AI conversation').getByText(STUB_DRAFT)).toBeVisible({ timeout: 20_000 })
    await expect(panel.locator('.aiPanelMeta')).toContainText(/Status:\s*Completed/i, { timeout: 10_000 })
    await expect(panel.locator('.aiPanelMeta')).not.toContainText(/Status:\s*Failed/i)

    await panel.getByRole('button', { name: 'Insert Draft' }).click()
    await expect(notes).toHaveValue(new RegExp(STUB_DRAFT.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')))
  })
})
