import { test, expect, type Page } from '@playwright/test'
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { continueToDashboard, openNav } from '../fixtures/app'
import { getApiBaseUrl, waitForApiHealthy } from '../fixtures/api'

/**
 * Capture React visual baseline screenshots (Phase 3).
 *
 * Run via scripts/capture-react-baseline.sh (serves React on 5173, not Blazor).
 * Output: docs/migration-screenshots/react-baseline/
 */

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const OUT_ROOT = path.resolve(__dirname, '../../../docs/migration-screenshots/react-baseline')

const VIEWPORTS = [
  { name: 'desktop-1440x900', width: 1440, height: 900 },
  { name: 'tablet-1100x800', width: 1100, height: 800 },
  { name: 'narrow-700x900', width: 700, height: 900 },
] as const

async function shot(page: Page, viewport: (typeof VIEWPORTS)[number], name: string) {
  const dir = path.join(OUT_ROOT, viewport.name)
  fs.mkdirSync(dir, { recursive: true })
  const file = path.join(dir, `${name}.png`)
  await page.screenshot({ path: file, fullPage: true })
  return file
}

async function waitHydrated(page: Page) {
  await expect(page.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
}

async function fetchIds() {
  const base = getApiBaseUrl()
  const [tasks, projects, risks, team] = await Promise.all([
    fetch(`${base}/tasks`).then((r) => r.json()) as Promise<Array<{ id: string }>>,
    fetch(`${base}/projects`).then((r) => r.json()) as Promise<Array<{ id: string }>>,
    fetch(`${base}/risks`).then((r) => r.json()) as Promise<Array<{ id: string }>>,
    fetch(`${base}/team-members`).then((r) =>
      r.json(),
    ) as Promise<
      Array<{
        id: string
        notes?: Array<{ id: string }>
        risks?: Array<{ id: string }>
        azureWorkItems?: Array<{ id: string }>
      }>
    >,
  ])

  const memberWithNote = team.find((m) => (m.notes?.length ?? 0) > 0) ?? team[0]
  const memberWithRisk = team.find((m) => (m.risks?.length ?? 0) > 0)
  const memberWithWorkItem = team.find((m) => (m.azureWorkItems?.length ?? 0) > 0)

  let growthGoal:
    | { memberId: string; goalId: string }
    | undefined
  for (const member of team) {
    const res = await fetch(`${base}/team-members/${member.id}/growth`)
    if (!res.ok) continue
    const growth = (await res.json()) as { goals?: Array<{ id: string }> }
    const goalId = growth.goals?.[0]?.id
    if (goalId) {
      growthGoal = { memberId: member.id, goalId }
      break
    }
  }

  return {
    taskId: tasks[0]?.id,
    projectId: projects[0]?.id,
    riskId: risks[0]?.id,
    memberId: memberWithNote?.id ?? team[0]?.id,
    noteId: memberWithNote?.notes?.[0]?.id,
    teamMemberRisk:
      memberWithRisk && memberWithRisk.risks?.[0]
        ? { memberId: memberWithRisk.id, riskId: memberWithRisk.risks[0].id }
        : undefined,
    workItem:
      memberWithWorkItem && memberWithWorkItem.azureWorkItems?.[0]
        ? { memberId: memberWithWorkItem.id, workItemId: memberWithWorkItem.azureWorkItems[0].id }
        : undefined,
    growthGoal,
  }
}

test.describe('React visual baseline capture', () => {
  test.skip(
    process.env.ATLAS_REACT_BASELINE !== '1',
    'Set ATLAS_REACT_BASELINE=1 to recapture the frozen React baseline (React host only).',
  )
  test.describe.configure({ mode: 'serial' })
  test.setTimeout(300_000)

  test('capture checklist routes and dashboard perf', async ({ page, browser }) => {
    await waitForApiHealthy()
    fs.mkdirSync(OUT_ROOT, { recursive: true })
    const ids = await fetchIds()

    const gaps: string[] = []
    const captured: string[] = []

    for (const viewport of VIEWPORTS) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height })

      await page.goto('/#/')
      await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible({ timeout: 30_000 })
      captured.push(await shot(page, viewport, '01-login-index'))

      await page.goto('/#/login')
      await expect(page.getByRole('heading', { name: 'Login' })).toBeVisible()
      captured.push(await shot(page, viewport, '02-login'))

      await page.goto('/#/setup')
      await expect(page.locator('h2.pageHeaderTitle', { hasText: 'Azure Setup' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '03-setup'))

      await continueToDashboard(page)
      await waitHydrated(page)
      captured.push(await shot(page, viewport, '04-dashboard'))

      await openNav(page, 'Tasks')
      await expect(page.locator('h2.pageTitle', { hasText: 'Tasks' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '05-tasks-list'))

      // Split view: select a row on /tasks (list + detail), URL stays /tasks.
      if (ids.taskId) {
        const row = page.locator('button.tasksTaskRow').first()
        if (await row.count()) {
          await row.click()
          await page.waitForTimeout(400)
          captured.push(await shot(page, viewport, '06-tasks-split'))
        } else {
          gaps.push(`${viewport.name}: tasks split — no task rows to select`)
        }

        // Focus mode: /tasks/:id hides the list.
        await page.goto(`/#/tasks/${ids.taskId}`)
        await waitHydrated(page)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '07-tasks-focus'))
      } else {
        gaps.push(`${viewport.name}: tasks split/focus — no tasks in demo seed`)
      }

      await openNav(page, 'Projects')
      await expect(page.locator('h2.pageTitle', { hasText: 'Projects' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '08-projects-list'))

      if (ids.projectId) {
        await page.goto(`/#/projects/${ids.projectId}?tab=overview`)
        await waitHydrated(page)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '09-projects-detail-overview'))

        await page.goto(`/#/projects/${ids.projectId}?tab=tasks`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '10-projects-tab-tasks'))

        await page.goto(`/#/projects/${ids.projectId}?tab=risks`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '11-projects-tab-risks'))
      } else {
        gaps.push(`${viewport.name}: project detail tabs — no projects in demo seed`)
      }

      await openNav(page, 'Risks')
      await expect(page.locator('h2.pageTitle', { hasText: 'Risks' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '12-risks-list'))

      if (ids.riskId) {
        await page.goto(`/#/risks/${ids.riskId}`)
        await waitHydrated(page)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '13-risks-detail'))
      } else {
        gaps.push(`${viewport.name}: risk detail — no risks in demo seed`)
      }

      await openNav(page, 'Team')
      await expect(page.locator('h2.pageTitle', { hasText: 'Team' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '14-team'))

      if (ids.memberId) {
        await page.goto(`/#/team/${ids.memberId}`)
        await waitHydrated(page)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '15-team-member'))

        await page.goto(`/#/team/${ids.memberId}/notes`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '16-team-member-notes'))

        await page.goto(`/#/team/${ids.memberId}/work-items`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '17-team-member-work-items'))

        await page.goto(`/#/team/${ids.memberId}/risks`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '18-team-member-risks'))

        await page.goto(`/#/team/${ids.memberId}/growth`)
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '19-team-member-growth'))

        // Nested detail routes (checklist: note/work-item/risk/growth detail).
        if (ids.noteId) {
          await page.goto(`/#/team/${ids.memberId}/notes/${ids.noteId}`)
          await waitHydrated(page)
          await page.waitForTimeout(400)
          captured.push(await shot(page, viewport, '20-team-note-detail'))
        } else {
          gaps.push(`${viewport.name}: team note detail — no notes on seeded members`)
        }

        if (ids.workItem) {
          await page.goto(`/#/team/${ids.workItem.memberId}/work-items/${ids.workItem.workItemId}`)
          await waitHydrated(page)
          await page.waitForTimeout(400)
          captured.push(await shot(page, viewport, '21-team-work-item-detail'))
        } else {
          gaps.push(
            `${viewport.name}: team work-item detail (/#/team/:memberId/work-items/:workItemId) — demo seed has no azureWorkItems`,
          )
        }

        if (ids.teamMemberRisk) {
          await page.goto(
            `/#/team/${ids.teamMemberRisk.memberId}/risks/${ids.teamMemberRisk.riskId}`,
          )
          await waitHydrated(page)
          await page.waitForTimeout(400)
          captured.push(await shot(page, viewport, '22-team-member-risk-detail'))
        } else {
          gaps.push(
            `${viewport.name}: team member risk detail (/#/team/:memberId/risks/:teamMemberRiskId) — demo seed has no member risks`,
          )
        }

        if (ids.growthGoal) {
          await page.goto(
            `/#/team/${ids.growthGoal.memberId}/growth/goals/${ids.growthGoal.goalId}`,
          )
          await waitHydrated(page)
          await page.waitForTimeout(400)
          captured.push(await shot(page, viewport, '23-team-growth-goal-detail'))
        } else {
          gaps.push(
            `${viewport.name}: growth goal detail (/#/team/:memberId/growth/goals/:goalId) — demo seed has no growth records (GET growth → 404)`,
          )
        }
      } else {
        gaps.push(`${viewport.name}: team member tabs — no members in demo seed`)
      }

      await openNav(page, 'Settings')
      await expect(page.locator('h2.pageTitle', { hasText: 'Settings' })).toBeVisible({ timeout: 15_000 })
      captured.push(await shot(page, viewport, '24-settings'))

      await page.goto('/#/settings/azure-import')
      await page.waitForTimeout(800)
      captured.push(await shot(page, viewport, '25-settings-azure-import'))

      await page.goto('/#/dashboard')
      await waitHydrated(page)
      const search = page.getByRole('combobox', { name: 'Search' })
      await search.click()
      await search.fill('a')
      await page.waitForTimeout(400)
      captured.push(await shot(page, viewport, '26-shell-search-open'))

      const quickAdd = page.getByRole('button', { name: /quick add/i })
      if (await quickAdd.count()) {
        await quickAdd.first().click()
        await page.waitForTimeout(400)
        captured.push(await shot(page, viewport, '27-shell-quick-add'))
        await page.keyboard.press('Escape')
      } else {
        gaps.push(`${viewport.name}: Quick Add modal — button not found`)
      }
    }

    const context = await browser.newContext({
      viewport: { width: 1440, height: 900 },
    })
    const perfPage = await context.newPage()
    await waitForApiHealthy()

    // Login once so dashboard is reachable, then hard-reload with cache disabled for transfer size.
    await perfPage.goto('/#/')
    await perfPage.getByRole('button', { name: 'Continue' }).click()
    await expect(perfPage.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({ timeout: 30_000 })
    await expect(perfPage.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })

    const client = await perfPage.context().newCDPSession(perfPage)
    await client.send('Network.enable')
    await client.send('Network.setCacheDisabled', { cacheDisabled: true })

    let transferSize = 0
    client.on('Network.loadingFinished', (e) => {
      transferSize += e.encodedDataLength ?? 0
    })

    const start = Date.now()
    await perfPage.reload({ waitUntil: 'networkidle' })
    await expect(perfPage.locator('h2.pageTitle', { hasText: 'Dashboard' })).toBeVisible({ timeout: 30_000 })
    await expect(perfPage.getByRole('combobox', { name: 'Search' })).toBeEnabled({ timeout: 30_000 })
    const ttiMs = Date.now() - start
    await client.detach().catch(() => undefined)

    const perf = {
      route: '/#/dashboard',
      viewport: '1440x900',
      transferSizeBytes: transferSize,
      ttiMs,
      measuredAt: new Date().toISOString(),
      notes:
        'Hard reload of dashboard with CDP Network.setCacheDisabled. TTI = reload start until Dashboard title visible and Search enabled. Transfer size = sum of Network.loadingFinished encodedDataLength.',
    }
    fs.writeFileSync(path.join(OUT_ROOT, 'PERFORMANCE.json'), JSON.stringify(perf, null, 2) + '\n')

    const readme = `# React visual baseline (Phase 3)

Captured from the **React** UI (\`src/atlas.ui\`) with hash routes, against API + Postgres with \`ATLAS_SEED_DEMO=true\`.

## Viewports

| Name | Size |
| --- | --- |
| desktop-1440x900 | 1440×900 |
| tablet-1100x800 | 1100×800 |
| narrow-700x900 | 700×900 |

## Regenerate

\`\`\`bash
bash scripts/capture-react-baseline.sh
\`\`\`

Requires Atlas API on \`:5012\` with demo seed and Postgres.

## Performance (\`/#/dashboard\`)

| Metric | Value |
| --- | --- |
| Transfer size | ${transferSize} bytes |
| TTI (approx) | ${ttiMs} ms |

See \`PERFORMANCE.json\` for raw measurement details. Not a hard exit gate — document variance at Phase 8.

## Gaps / not captured

${
  gaps.length
    ? [...new Set(gaps.map((g) => g.replace(/^[^:]+:\s*/, '')))].map((g) => `- ${g}`).join('\n')
    : '- (none for seed-reachable routes this run)'
}

Also not captured (require fault injection or transient UI):

- Empty-list states (demo seed populates lists)
- Validation / server-error overlays
- AI panel streaming transcript (Phase 7+)
- Hydration overlay freeze frame

Per-viewport gap lines recorded during capture (for debugging):

${gaps.length ? gaps.map((g) => `- ${g}`).join('\n') : '- (none)'}

## Captured files

${captured.map((f) => `- \`${path.relative(OUT_ROOT, f)}\``).join('\n')}
`
    fs.writeFileSync(path.join(OUT_ROOT, 'README.md'), readme)

    await context.close()
    expect(captured.length).toBeGreaterThan(40)
  })
})
