import { useEffect, useMemo } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppState } from '../app/state/AppState'
import { formatDurationFromMinutes, parseDurationText } from '../app/duration'

function daysBetween(iso: string, nowIso: string) {
  const a = new Date(iso).getTime()
  const b = new Date(nowIso).getTime()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

export function DashboardView() {
  const ai = useAi()
  const { tasks, risks, team, settings } = useAppState()
  const nowIso = useMemo(() => new Date().toISOString(), [])

  useEffect(() => {
    ai.setContext('Context: Dashboard', [
      { id: 'suggest-next-action', label: 'Suggest Next Action' },
      { id: 'summarize-week', label: 'Summarize Incomplete Work (week)' },
    ])
  }, [ai.setContext])

  const attention = useMemo(() => {
    const staleDays = settings.staleDays
    const staleTasks = tasks.filter((t) => daysBetween(t.lastTouchedIso, nowIso) >= staleDays)
    const highTasks = tasks.filter((t) => t.priority === 'High' || t.priority === 'Critical')
    const openRisks = risks.filter((r) => r.status !== 'Resolved')
    const noNotes = team.filter((m) => m.notes.length === 0)
    return {
      staleTasks,
      highTasks,
      openRisks,
      noNotes,
    }
  }, [nowIso, risks, settings.staleDays, tasks, team])

  return (
    <div className="page">
      <h2 className="pageTitle">Dashboard</h2>

      <div className="dashboardGrid">
        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Attention Required</div>
            <div className="mutedSmall">Stale threshold: {settings.staleDays}d</div>
          </header>

          <div className="list">
            {attention.highTasks.slice(0, 4).map((t) => (
              <div key={t.id} className="listRow">
                <span className={`dot dot-${t.priority.toLowerCase()}`} />
                <div className="listMain">
                  <div className="listTitle">{t.title}</div>
                  <div className="listMeta">Task • {t.priority}</div>
                </div>
              </div>
            ))}

            {attention.openRisks.slice(0, 3).map((r) => (
              <div key={r.id} className="listRow">
                <span className={`dot dot-${r.severity.toLowerCase()}`} />
                <div className="listMain">
                  <div className="listTitle">{r.title}</div>
                  <div className="listMeta">Risk • {r.status}</div>
                </div>
              </div>
            ))}

            {attention.staleTasks.slice(0, 3).map((t) => (
              <div key={t.id} className="listRow">
                <span className="dot dot-stale" />
                <div className="listMain">
                  <div className="listTitle">{t.title}</div>
                  <div className="listMeta">Task • stale {daysBetween(t.lastTouchedIso, nowIso)}d</div>
                </div>
              </div>
            ))}

            {attention.noNotes.slice(0, 3).map((m) => (
              <div key={m.id} className="listRow">
                <span className="dot dot-team" />
                <div className="listMain">
                  <div className="listTitle">{m.name}</div>
                  <div className="listMeta">Team • no notes yet</div>
                </div>
              </div>
            ))}
          </div>
        </section>

        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Today / This Week</div>
          </header>

          <div className="list">
            {tasks.slice(0, 8).map((t) => (
              <div key={t.id} className="listRow">
                <span className={`dot dot-${t.priority.toLowerCase()}`} />
                <div className="listMain">
                  <div className="listTitle">{t.title}</div>
                  <div className="listMeta">
                    {t.priority}
                    {t.project ? ` • ${t.project}` : ''}
                    {t.risk ? ` • Risk: ${t.risk}` : ''}
                  </div>
                </div>
                <div className="pill">{formatDurationFromMinutes(parseDurationText(t.estimatedDurationText)?.totalMinutes ?? 0)}</div>
              </div>
            ))}
          </div>

          <div className="cardFooter">
            <button className="btn" onClick={() => ai.runAction('suggest-next-action')}>
              Ask AI: What should I work on next?
            </button>
          </div>
        </section>

        <section className="card">
          <header className="cardHeader">
            <div className="cardTitle">Team Pulse</div>
          </header>
          <div className="list">
            {team.map((m) => (
              <div key={m.id} className="listRow">
                <span className={`dot dot-${m.statusDot.toLowerCase()}`} />
                <div className="listMain">
                  <div className="listTitle">{m.name}</div>
                  <div className="listMeta">{m.currentFocus}</div>
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}

// duration formatting now lives in app/duration.ts


