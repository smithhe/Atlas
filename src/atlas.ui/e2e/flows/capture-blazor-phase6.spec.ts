import { test, expect, type Page } from '@playwright/test'
import path from 'node:path'
import fs from 'node:fs'
import { fileURLToPath } from 'node:url'
import { continueToDashboard, openNav, uniqueTitle } from '../fixtures/app'
import { ensureTeamMember } from '../fixtures/api'

/**
 * Phase 6 Blazor Team hub screenshot evidence vs docs/migration-screenshots/react-baseline/.
 * Run: ATLAS_BLAZOR_PHASE6_SHOTS=1 npx playwright test e2e/flows/capture-blazor-phase6.spec.ts
 * (Not in playwright-ported.txt — evidence only.)
 */

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const OUT = path.resolve(__dirname, '../../../../docs/migration-screenshots/blazor-phase6')

const VIEWPORTS = [
  { name: 'desktop-1440x900', width: 1440, height: 900 },
  { name: 'tablet-1100x800', width: 1100, height: 800 },
  { name: 'narrow-700x900', width: 700, height: 900 },
] as const

async function shot(page: Page, viewport: string, name: string) {
  const dir = path.join(OUT, viewport)
  fs.mkdirSync(dir, { recursive: true })
  const file = path.join(dir, `${name}.png`)
  await page.screenshot({ path: file, fullPage: false })
  return file
}

test.describe('Blazor Phase 6 screenshots', () => {
  test.skip(!process.env.ATLAS_BLAZOR_PHASE6_SHOTS, 'Set ATLAS_BLAZOR_PHASE6_SHOTS=1 to capture')

  for (const vp of VIEWPORTS) {
    test(`capture ${vp.name}`, async ({ page }) => {
      test.setTimeout(240_000)
      await page.setViewportSize({ width: vp.width, height: vp.height })

      const member = await ensureTeamMember()

      await continueToDashboard(page)

      await openNav(page, 'Team')
      await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible()
      await expect(page.getByLabel('Team member list')).toBeVisible()
      await shot(page, vp.name, '11-team-list')

      const memberList = page.getByLabel('Team member list')
      await expect(memberList.getByText(member.name)).toBeVisible({ timeout: 20_000 })
      await memberList
        .getByRole('button', { name: new RegExp(member.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) })
        .dblclick()

      await expect(page.getByLabel('Member tabs')).toBeVisible({ timeout: 20_000 })
      await shot(page, vp.name, '12-team-member-overview')

      // Focus mode uses NavLink anchors; list mode uses buttons.
      const tabs = page.getByLabel('Member tabs')
      await tabs.getByRole('link', { name: 'Notes', exact: true }).click()
      await expect(page.getByLabel('Notes tab')).toBeVisible()
      await shot(page, vp.name, '12b-team-member-notes')

      // Create a note and open full detail for baseline shot 20-team-note-detail.
      const noteTitle = await uniqueTitle('Phase6 Note Shot')
      await page.getByRole('button', { name: '+ New' }).click()
      const modal = page.getByRole('dialog', { name: 'New note' })
      await expect(modal).toBeVisible()
      await modal.locator('.fieldLabel', { hasText: 'Title (optional)' }).locator('..').locator('input').fill(noteTitle)
      await modal.locator('.fieldLabel', { hasText: 'ADO work item id' }).locator('..').locator('input').fill('424242')
      await modal
        .locator('.fieldLabel', { hasText: 'PR URL' })
        .locator('..')
        .locator('input')
        .fill('https://dev.azure.com/example/_git/repo/pullrequest/99')
      await modal.locator('.fieldLabel', { hasText: /^Note$/ }).locator('..').locator('textarea').fill('Note body for phase 6 screenshot.')
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
      await noteRow.getByTitle('Open full page').click()
      await expect(page).toHaveURL(new RegExp(`/team/${member.id}/notes/`))
      await expect(page.getByLabel('Note fields')).toBeVisible({ timeout: 20_000 })
      await shot(page, vp.name, '20-team-note-detail')

      await page.goto(`/team/${member.id}/work-items`)
      await expect(page.getByLabel('Work items tab')).toBeVisible({ timeout: 10_000 })
      await shot(page, vp.name, '12c-team-member-work-items')

      await page.goto(`/team/${member.id}/risks`)
      await expect(page.getByLabel('Risks tab')).toBeVisible({ timeout: 10_000 })
      await shot(page, vp.name, '12d-team-member-risks')

      await page.goto(`/team/${member.id}/growth`)
      await expect(page.getByLabel('Growth tab')).toBeVisible({ timeout: 10_000 })
      await shot(page, vp.name, '12e-team-member-growth')
    })
  }
})
