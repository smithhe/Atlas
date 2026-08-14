import { test, expect } from '@playwright/test'
import { continueToDashboard, uniqueTitle } from '../fixtures/app'
import { createRiskViaApi, requestJson, updateRiskViaApi } from '../fixtures/api'

test.describe('Dashboard navigation', () => {
  test('Needs Action open risk navigates to risk detail', async ({ page }) => {
    // Needs Action is capped at 8 (React parity). Resolve every open risk so the
    // newly created one is guaranteed to surface in the top slice.
    const existing = await requestJson<Array<{ id: string; title: string; status: string; severity: string }>>('/risks')
    for (const r of existing.filter((x) => x.status === 'Open')) {
      await updateRiskViaApi(r.id, {
        title: r.title,
        status: 'Resolved',
        severity: (r.severity as 'Low' | 'Medium' | 'High') || 'Medium',
      })
    }

    const title = await uniqueTitle('Dash Needs Risk')
    const id = await createRiskViaApi(title, { status: 'Open', severity: 'High' })

    await continueToDashboard(page)

    const section = page.getByTestId('dashboard-needs-action')
    await expect(section.getByText(title)).toBeVisible({ timeout: 20_000 })
    await section.getByRole('button', { name: new RegExp(title.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')) }).click()

    await expect(page).toHaveURL(new RegExp(`/risks/${id}`))
    await expect(page.getByLabel('Risk detail editor').getByText(title)).toBeVisible({ timeout: 20_000 })
  })
})
