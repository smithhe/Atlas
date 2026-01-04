import { useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppState } from '../app/state/AppState'
import { formatDurationFromMinutes, parseDurationText } from '../app/duration'

function daysBetween(iso: string, nowIso: string) {
  const a = new Date(iso).getTime()
  const b = new Date(nowIso).getTime()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

function isoDateAddDays(isoDate: string, days: number) {
  const d = new Date(`${isoDate}T00:00:00.000Z`)
  d.setUTCDate(d.getUTCDate() + days)
  return d.toISOString().slice(0, 10)
}

function isIsoDateBetweenInclusive(isoDate: string, startIsoDate: string, endIsoDate: string) {
  return isoDate >= startIsoDate && isoDate <= endIsoDate
}

function includesDocsKeyword(text: string) {
  const t = text.toLowerCase()
  return ['docs', 'documentation', 'onboarding', 'runbook', 'playbook'].some((k) => t.includes(k))
}

type DriftTag = 'Risk' | 'Project' | 'Team' | 'Docs'
type DashboardRow = {
  key: string
  dotClass: string
  title: string
  meta: string
  pill?: string
  to?: string
}

type DriftRow = {
  key: string
  tag: DriftTag
  title: string
  detail: string
  to: string
}

export function DashboardView() {
  const ai = useAi()
  const nav = useNavigate()
  const { tasks, risks, team, projects } = useAppState()
  const nowIso = useMemo(() => new Date().toISOString(), [])
  const todayIsoDate = useMemo(() => nowIso.slice(0, 10), [nowIso])

  useEffect(() => {
    ai.setContext('Context: Dashboard', [
      { id: 'suggest-next-action', label: 'Suggest Next Action' },
      { id: 'summarize-week', label: 'Summarize Incomplete Work (week)' },
    ])
  }, [ai.setContext])

  const dashboard = useMemo(() => {
    const staleSoonDays = 7
    const staleDays = 10
    const dueSoonDays = 2

    function normalize(s: string) {
      return s.trim().toLowerCase()
    }

    function taskLinkedToOpenRisk(taskRisk?: string) {
      if (!taskRisk) return undefined
      const needle = normalize(taskRisk)
      if (!needle) return undefined
      const open = risks.filter((r) => r.status === 'Open')
      const exact = open.find((r) => normalize(r.title) === needle)
      if (exact) return exact
      return open.find((r) => normalize(r.title).includes(needle) || needle.includes(normalize(r.title)))
    }

    function priorityToDot(p: string) {
      const q = p.toLowerCase()
      if (q === 'critical') return 'dot-critical'
      if (q === 'high') return 'dot-high'
      if (q === 'medium') return 'dot-medium'
      if (q === 'low') return 'dot-low'
      return 'dot-stale'
    }

    function daysSince(iso?: string) {
      if (!iso) return undefined
      return daysBetween(iso, nowIso)
    }

    function taskWhy(t: (typeof tasks)[number]) {
      const parts: string[] = []
      if (t.risk) parts.push(`Risk: ${t.risk}`)
      if (t.project) parts.push(t.project)
      if (parts.length > 0) return parts.join(' • ')
      return `${t.priority}${t.status ? ` • ${t.status}` : ''}`
    }

    const needsAction: DashboardRow[] = []
    const watchlist: DashboardRow[] = []

    // Tasks
    for (const t of tasks) {
      const ageDays = daysBetween(t.lastTouchedIso, nowIso)
      const isBlocked = t.status === 'Blocked'
      const isHigh = t.priority === 'High' || t.priority === 'Critical'
      const isStaleSoon = ageDays >= staleSoonDays && ageDays < staleDays
      const isStale = ageDays >= staleDays
      const linkedOpenRisk = taskLinkedToOpenRisk(t.risk)

      const dueSoon =
        t.dueDate && isIsoDateBetweenInclusive(t.dueDate, todayIsoDate, isoDateAddDays(todayIsoDate, dueSoonDays))

      const highAndDrifting = isHigh && (isStale || Boolean(dueSoon) || Boolean(linkedOpenRisk))

      const pill = t.dueDate
        ? `due ${t.dueDate === todayIsoDate ? 'today' : t.dueDate}`
        : `${formatDurationFromMinutes(parseDurationText(t.estimatedDurationText)?.totalMinutes ?? 0)}`

      const base: DashboardRow = {
        key: t.id,
        dotClass: priorityToDot(t.priority),
        title: t.title,
        meta: `Task • ${t.status ? `${t.status} • ` : ''}${taskWhy(t)}`,
        pill,
        to: `/tasks/${t.id}`,
      }

      if (isBlocked || highAndDrifting) {
        needsAction.push(base)
      } else if (!isBlocked && isStaleSoon) {
        watchlist.push({
          ...base,
          dotClass: 'dot-stale',
          meta: `Task • stale soon (${ageDays}d) • ${taskWhy(t)}`,
        })
      }
    }

    // Risks
    for (const r of risks) {
      const ageDays = daysBetween(r.lastUpdatedIso, nowIso)
      const base: DashboardRow = {
        key: r.id,
        dotClass: `dot-${r.severity.toLowerCase()}`,
        title: r.title,
        meta: `Risk • ${r.status}`,
        pill: `${ageDays}d`,
        to: `/risks/${r.id}`,
      }
      if (r.status === 'Open') needsAction.push(base)
      else if (r.status === 'Watching') watchlist.push(base)
    }

    // Team
    for (const m of team) {
      const freshnessDays = daysSince(m.activitySnapshot.lastUpdatedIso)
      const hasBaselineSignal = Boolean(m.activitySnapshot.lastUpdatedIso) && m.notes.length > 0
      const missingBaseline = !hasBaselineSignal

      const freshnessText =
        freshnessDays === undefined
          ? 'no update yet'
          : freshnessDays > 7
            ? 'no update this week'
            : `updated ${freshnessDays}d ago`

      const base: DashboardRow = {
        key: m.id,
        dotClass: `dot-${m.statusDot.toLowerCase()}`,
        title: m.name,
        meta: m.currentFocus,
        pill: freshnessText,
        to: `/team/${m.id}`,
      }

      if (m.statusDot === 'Red') needsAction.push(base)
      if (m.statusDot === 'Yellow' || missingBaseline) watchlist.push(base)
    }

    // Sort: needs action first by "most urgent-ish" (blocked tasks, open risks, then everything else)
    const urgencyRank = (row: DashboardRow) => {
      const meta = row.meta.toLowerCase()
      if (meta.startsWith('task') && meta.includes('blocked')) return 0
      if (meta.startsWith('risk') && meta.includes('open')) return 1
      return 2
    }
    needsAction.sort((a, b) => urgencyRank(a) - urgencyRank(b))

    // Commitment View
    const dueToday = tasks.filter((t) => t.dueDate && t.dueDate === todayIsoDate)
    const dueThisWeek = tasks.filter(
      (t) => t.dueDate && t.dueDate !== todayIsoDate && isIsoDateBetweenInclusive(t.dueDate, todayIsoDate, isoDateAddDays(todayIsoDate, 7)),
    )
    const noDue = tasks.filter((t) => !t.dueDate)

    const touchedDesc = [...noDue].sort((a, b) => new Date(b.lastTouchedIso).getTime() - new Date(a.lastTouchedIso).getTime())
    const todayBucket: (typeof tasks)[number][] = []
    const weekBucket: (typeof tasks)[number][] = []

    // Fill today with recently touched first, then high priority.
    for (const t of touchedDesc) {
      if (todayBucket.length >= 4) break
      todayBucket.push(t)
    }
    for (const t of noDue.filter((t) => t.priority === 'High' || t.priority === 'Critical')) {
      if (todayBucket.some((x) => x.id === t.id)) continue
      if (todayBucket.length >= 6) break
      todayBucket.push(t)
    }

    // Fill this week with remaining recently touched, else medium.
    for (const t of touchedDesc) {
      if (todayBucket.some((x) => x.id === t.id)) continue
      if (weekBucket.length >= 6) break
      weekBucket.push(t)
    }
    for (const t of noDue.filter((t) => t.priority === 'Medium')) {
      if (todayBucket.some((x) => x.id === t.id) || weekBucket.some((x) => x.id === t.id)) continue
      if (weekBucket.length >= 8) break
      weekBucket.push(t)
    }

    const commitmentToday = [...dueToday, ...todayBucket].slice(0, 8)
    const commitmentWeek = [...dueThisWeek, ...weekBucket].slice(0, 10)

    // Team pulse (sorted by status then freshness)
    const statusRank: Record<string, number> = { Red: 0, Yellow: 1, Green: 2 }
    const teamPulse = [...team].sort((a, b) => {
      const s = (statusRank[a.statusDot] ?? 9) - (statusRank[b.statusDot] ?? 9)
      if (s !== 0) return s
      const ad = daysSince(a.activitySnapshot.lastUpdatedIso) ?? 999
      const bd = daysSince(b.activitySnapshot.lastUpdatedIso) ?? 999
      return bd - ad
    })

    // Drift signals (early warnings)
    const drift: DriftRow[] = []

    for (const t of tasks) {
      const ageDays = daysBetween(t.lastTouchedIso, nowIso)
      if (ageDays >= staleDays) {
        const textForDocs = `${t.title} ${(t.project ?? '')}`
        const tag: DriftTag = includesDocsKeyword(textForDocs) ? 'Docs' : 'Project'
        drift.push({
          key: `drift-task-stale-${t.id}`,
          tag,
          title: t.title,
          detail: `Task stale (${ageDays}d since touched)`,
          to: `/tasks/${t.id}`,
        })
      } else if ((t.priority === 'High' || t.priority === 'Critical') && !t.dueDate) {
        drift.push({
          key: `drift-task-high-nodue-${t.id}`,
          tag: 'Project',
          title: t.title,
          detail: 'High priority task has no due date',
          to: `/tasks/${t.id}`,
        })
      }
    }

    for (const r of risks) {
      const ageDays = daysBetween(r.lastUpdatedIso, nowIso)
      if (r.status === 'Watching' && ageDays >= 14) {
        drift.push({
          key: `drift-risk-watching-${r.id}`,
          tag: 'Risk',
          title: r.title,
          detail: `Watching ${ageDays}d since update`,
          to: `/risks/${r.id}`,
        })
      }
      if (r.status === 'Open' && ageDays >= 10) {
        drift.push({
          key: `drift-risk-open-stale-${r.id}`,
          tag: 'Risk',
          title: r.title,
          detail: `Open risk not updated recently (${ageDays}d)`,
          to: `/risks/${r.id}`,
        })
      }
    }

    for (const m of team) {
      const freshnessDays = daysSince(m.activitySnapshot.lastUpdatedIso)
      if (freshnessDays !== undefined && freshnessDays > 7) {
        drift.push({
          key: `drift-team-noupdate-${m.id}`,
          tag: 'Team',
          title: m.name,
          detail: 'No update this week',
          to: `/team/${m.id}`,
        })
      }
      if (m.notes.length === 0) {
        drift.push({
          key: `drift-team-nobaseline-${m.id}`,
          tag: 'Team',
          title: m.name,
          detail: 'No baseline notes yet',
          to: `/team/${m.id}`,
        })
      }
    }

    for (const p of projects) {
      if (p.health === 'Yellow' || p.health === 'Red') {
        drift.push({
          key: `drift-proj-health-${p.id}`,
          tag: 'Project',
          title: p.name,
          detail: `Project health: ${p.health}`,
          to: `/projects/${p.id}`,
        })
      }
      const last = p.lastUpdatedIso
      if (last) {
        const ageDays = daysBetween(last, nowIso)
        if (ageDays > 7) {
          drift.push({
            key: `drift-proj-stale-${p.id}`,
            tag: 'Project',
            title: p.name,
            detail: `No recent project update (${ageDays}d)`,
            to: `/projects/${p.id}`,
          })
        }
      }
    }

    // De-dupe drift by key and keep it intentionally short
    const driftTop = drift.slice(0, 10)

    return {
      staleSoonDays,
      staleDays,
      needsAction: needsAction.slice(0, 8),
      watchlist: watchlist.slice(0, 8),
      commitmentToday,
      commitmentWeek,
      teamPulse,
      drift: driftTop,
      taskWhy,
      daysSince,
    }
  }, [nowIso, projects, risks, tasks, team, todayIsoDate])

  return (
    <div className="page">
      <h2 className="pageTitle">Dashboard</h2>

      <div className="dashboardGrid">
        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Attention Required</div>
            <div className="mutedSmall">
              Stale soon: {dashboard.staleSoonDays}d • Stale: {dashboard.staleDays}d
            </div>
          </header>

          <div className="list">
            <div className="dashboardGroup">
              <div className="dashboardGroupHeader">
                <div className="dashboardGroupTitle">Needs Action</div>
                <div className="mutedSmall">Things you should decide/unblock</div>
              </div>
              {dashboard.needsAction.length === 0 ? (
                <div className="dashboardEmpty">Nothing urgent right now.</div>
              ) : (
                dashboard.needsAction.map((row) => (
                  <button
                    key={row.key}
                    className="listRow listRowBtn"
                    onClick={() => row.to && nav(row.to)}
                    type="button"
                  >
                    <span className={`dot ${row.dotClass}`} />
                    <div className="listMain">
                      <div className="listTitle">{row.title}</div>
                      <div className="listMeta">{row.meta}</div>
                    </div>
                    {row.pill ? <div className="pill">{row.pill}</div> : null}
                  </button>
                ))
              )}
            </div>

            <div className="dashboardGroup">
              <div className="dashboardGroupHeader">
                <div className="dashboardGroupTitle">Watchlist</div>
                <div className="mutedSmall">Monitor — no immediate action</div>
              </div>
              {dashboard.watchlist.length === 0 ? (
                <div className="dashboardEmpty">No watch items.</div>
              ) : (
                dashboard.watchlist.map((row) => (
                  <button
                    key={row.key}
                    className="listRow listRowBtn"
                    onClick={() => row.to && nav(row.to)}
                    type="button"
                  >
                    <span className={`dot ${row.dotClass}`} />
                    <div className="listMain">
                      <div className="listTitle">{row.title}</div>
                      <div className="listMeta">{row.meta}</div>
                    </div>
                    {row.pill ? <div className="pill">{row.pill}</div> : null}
                  </button>
                ))
              )}
            </div>
          </div>
        </section>

        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Commitment View</div>
          </header>

          <div className="list">
            <div className="dashboardGroup">
              <div className="dashboardGroupHeader">
                <div className="dashboardGroupTitle">Today</div>
                <div className="mutedSmall">Due today or recently-touched commitments</div>
              </div>
              {dashboard.commitmentToday.length === 0 ? (
                <div className="dashboardEmpty">No items.</div>
              ) : (
                dashboard.commitmentToday.map((t) => (
                  <button key={t.id} className="listRow listRowBtn" onClick={() => nav(`/tasks/${t.id}`)} type="button">
                    <span className={`dot dot-${t.priority.toLowerCase()}`} />
                    <div className="listMain">
                      <div className="listTitle">{t.title}</div>
                      <div className="listMeta">{dashboard.taskWhy(t)}</div>
                    </div>
                    <div className="pill">{t.dueDate ? `due ${t.dueDate === todayIsoDate ? 'today' : t.dueDate}` : `${daysBetween(t.lastTouchedIso, nowIso)}d`}</div>
                  </button>
                ))
              )}
            </div>

            <div className="dashboardGroup">
              <div className="dashboardGroupHeader">
                <div className="dashboardGroupTitle">This Week</div>
                <div className="mutedSmall">Due soon or medium-term commitments</div>
              </div>
              {dashboard.commitmentWeek.length === 0 ? (
                <div className="dashboardEmpty">No items.</div>
              ) : (
                dashboard.commitmentWeek.map((t) => (
                  <button key={t.id} className="listRow listRowBtn" onClick={() => nav(`/tasks/${t.id}`)} type="button">
                    <span className={`dot dot-${t.priority.toLowerCase()}`} />
                    <div className="listMain">
                      <div className="listTitle">{t.title}</div>
                      <div className="listMeta">{dashboard.taskWhy(t)}</div>
                    </div>
                    <div className="pill">{t.dueDate ? `due ${t.dueDate}` : `${daysBetween(t.lastTouchedIso, nowIso)}d`}</div>
                  </button>
                ))
              )}
            </div>
          </div>

          <div className="cardFooter">
            <button className="btn" onClick={() => ai.runAction('suggest-next-action')}>
              Ask AI: What needs my attention?
            </button>
          </div>
        </section>

        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Team Pulse</div>
          </header>
          <div className="list">
            {dashboard.teamPulse.map((m) => {
              const d = dashboard.daysSince(m.activitySnapshot.lastUpdatedIso)
              const freshnessText =
                d === undefined ? 'no update yet' : d > 7 ? 'no update this week' : `updated ${d}d ago`
              const freshnessTone = d === undefined ? 'pillToneBad' : d > 7 ? 'pillToneBad' : d >= 4 ? 'pillToneWarn' : 'pillToneOk'
              return (
                <button key={m.id} className="listRow listRowBtn" onClick={() => nav(`/team/${m.id}`)} type="button">
                  <span className={`dot dot-${m.statusDot.toLowerCase()}`} />
                  <div className="listMain">
                    <div className="listTitle">{m.name}</div>
                    <div className="listMeta">{m.currentFocus}</div>
                  </div>
                  <div className={`pill ${freshnessTone}`}>{freshnessText}</div>
                </button>
              )
            })}
          </div>
        </section>

        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Drift Signals</div>
            <div className="mutedSmall">Early warnings (not a backlog)</div>
          </header>
          <div className="list dashboardDriftList">
            {dashboard.drift.length === 0 ? (
              <div className="dashboardEmpty">No drift signals.</div>
            ) : (
              dashboard.drift.map((d) => (
                <button key={d.key} className="listRow listRowBtn dashboardDriftRow" onClick={() => nav(d.to)} type="button">
                  <span className="dot dot-stale" />
                  <div className="listMain">
                    <div className="listTitle">{d.title}</div>
                    <div className="listMeta">{d.detail}</div>
                  </div>
                  <div className="pill pillTag">{d.tag}</div>
                </button>
              ))
            )}
          </div>
        </section>
      </div>
    </div>
  )
}

// duration formatting now lives in app/duration.ts


