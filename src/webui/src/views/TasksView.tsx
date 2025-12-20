import { useEffect, useMemo, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTask } from '../app/state/AppState'
import type { Priority, Task } from '../app/types'

function formatDuration(days: number, hours: number) {
  const parts: string[] = []
  if (days > 0) parts.push(`${days}d`)
  if (hours > 0) parts.push(`${hours}h`)
  return parts.length ? parts.join(' ') : '—'
}

function daysSince(iso: string) {
  const a = new Date(iso).getTime()
  const b = Date.now()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

export function TasksView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const { tasks, settings, selectedTaskId } = useAppState()
  const selected = useSelectedTask()

  const [projectFilter, setProjectFilter] = useState('')
  const [riskFilter, setRiskFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState<Priority | 'All'>('All')

  useEffect(() => {
    ai.setContext('Context: Tasks', [
      { id: 'suggest-next-task', label: 'Suggest Next Task' },
      { id: 'summarize-week', label: 'Summarize Incomplete Work (week)' },
      { id: 'reprioritize', label: 'Reprioritize suggestions (draft)' },
    ])
  }, [ai.setContext])

  const filtered = useMemo(() => {
    return tasks.filter((t) => {
      if (priorityFilter !== 'All' && t.priority !== priorityFilter) return false
      if (projectFilter.trim() && !(t.project ?? '').toLowerCase().includes(projectFilter.trim().toLowerCase()))
        return false
      if (riskFilter.trim() && !(t.risk ?? '').toLowerCase().includes(riskFilter.trim().toLowerCase()))
        return false
      return true
    })
  }, [priorityFilter, projectFilter, riskFilter, tasks])

  return (
    <div className="page">
      <h2 className="pageTitle">Tasks</h2>

      <div className="splitGrid">
        <section className="pane paneLeft" aria-label="Task list and filters">
          <div className="card tight">
            <div className="fieldGrid">
              <label className="field">
                <div className="fieldLabel">Project</div>
                <input className="input" value={projectFilter} onChange={(e) => setProjectFilter(e.target.value)} />
              </label>
              <label className="field">
                <div className="fieldLabel">Risk</div>
                <input className="input" value={riskFilter} onChange={(e) => setRiskFilter(e.target.value)} />
              </label>
              <label className="field">
                <div className="fieldLabel">Priority</div>
                <select
                  className="select"
                  value={priorityFilter}
                  onChange={(e) => setPriorityFilter(e.target.value as Priority | 'All')}
                >
                  <option value="All">All</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </label>
            </div>
          </div>

          <div className="list listCard">
            {filtered.map((t) => {
              const stale = daysSince(t.lastTouchedIso) >= settings.staleDays
              return (
                <button
                  key={t.id}
                  className={`listRow listRowBtn ${t.id === selectedTaskId ? 'listRowActive' : ''}`}
                  onClick={() => dispatch({ type: 'selectTask', taskId: t.id })}
                >
                  <div className="listMain">
                    <div className="listTitle">{t.title}</div>
                    <div className="listMeta">
                      {t.priority}
                      {t.project ? ` • ${t.project}` : ''}
                      {t.risk ? ` • Risk: ${t.risk}` : ''}
                      {stale ? ` • stale ${daysSince(t.lastTouchedIso)}d` : ''}
                    </div>
                  </div>
                  <div className="pill">{formatDuration(t.durationDays, t.durationHours)}</div>
                </button>
              )
            })}
            {filtered.length === 0 ? <div className="muted pad">No tasks match your filters.</div> : null}
          </div>
        </section>

        <section className="pane paneCenter" aria-label="Task detail editor">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a task to edit.</div>
            </div>
          ) : (
            <TaskDetail task={selected} staleDays={settings.staleDays} />
          )}
        </section>
      </div>
    </div>
  )
}

function TaskDetail({ task, staleDays }: { task: Task; staleDays: number }) {
  const dispatch = useAppDispatch()
  const stale = daysSince(task.lastTouchedIso) >= staleDays

  function update(patch: Partial<Task>) {
    dispatch({ type: 'updateTask', task: { ...task, ...patch } })
  }

  const preview = formatDuration(task.durationDays, task.durationHours)

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div className="detailTitle">Task Detail</div>
        <div className="mutedSmall">
          Last touched: {new Date(task.lastTouchedIso).toLocaleString()}
          {stale ? ` • stale` : ''}
        </div>
      </div>

      <div className="fieldGrid2">
        <label className="field span2">
          <div className="fieldLabel">Title</div>
          <input className="input" value={task.title} onChange={(e) => update({ title: e.target.value })} />
        </label>

        <label className="field">
          <div className="fieldLabel">Priority (required)</div>
          <select
            className="select"
            value={task.priority}
            onChange={(e) => update({ priority: e.target.value as Priority })}
          >
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
            <option value="Critical">Critical</option>
          </select>
        </label>

        <div className="field">
          <div className="fieldLabel">Estimated Duration (required)</div>
          <div className="durationRow">
            <label className="durationField">
              <span className="mutedSmall">Days</span>
              <input
                className="input"
                type="number"
                min={0}
                value={task.durationDays}
                onChange={(e) => update({ durationDays: clampInt(e.target.value) })}
              />
            </label>
            <label className="durationField">
              <span className="mutedSmall">Hours</span>
              <input
                className="input"
                type="number"
                min={0}
                max={23}
                value={task.durationHours}
                onChange={(e) => update({ durationHours: clampInt(e.target.value, 0, 23) })}
              />
            </label>
          </div>
          <div className="mutedSmall">Estimated: {preview}</div>
        </div>

        <label className="field">
          <div className="fieldLabel">Project (optional)</div>
          <input className="input" value={task.project ?? ''} onChange={(e) => update({ project: e.target.value })} />
        </label>

        <label className="field">
          <div className="fieldLabel">Risk (optional)</div>
          <input className="input" value={task.risk ?? ''} onChange={(e) => update({ risk: e.target.value })} />
        </label>

        <label className="field">
          <div className="fieldLabel">Due date (optional)</div>
          <input
            className="input"
            type="date"
            value={task.dueDate ?? ''}
            onChange={(e) => update({ dueDate: e.target.value || undefined })}
          />
        </label>

        <div className="field span2">
          <div className="fieldLabel">Dependencies (optional)</div>
          <div className="placeholderBox">Add dependencies…</div>
        </div>

        <label className="field span2">
          <div className="fieldLabel">Notes</div>
          <textarea className="textarea" value={task.notes} onChange={(e) => update({ notes: e.target.value })} />
        </label>
      </div>

      <div className="row">
        <button className="btn btnSecondary" onClick={() => update({})}>
          Save (mock)
        </button>
        <button className="btn" onClick={() => dispatch({ type: 'touchTask', taskId: task.id, touchedIso: new Date().toISOString() })}>
          Touch / Update Activity
        </button>
      </div>
    </div>
  )
}

function clampInt(value: string, min = 0, max = Number.MAX_SAFE_INTEGER) {
  const n = Number.parseInt(value, 10)
  if (Number.isNaN(n)) return 0
  return Math.max(min, Math.min(max, n))
}


