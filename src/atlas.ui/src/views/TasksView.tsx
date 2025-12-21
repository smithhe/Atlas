import { useEffect, useMemo, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTask } from '../app/state/AppState'
import type { Priority, Task } from '../app/types'
import { useNavigate, useParams } from 'react-router-dom'
import { formatDurationFromMinutes, parseDurationText } from '../app/duration'

function daysSince(iso: string) {
  const a = new Date(iso).getTime()
  const b = Date.now()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

export function TasksView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { taskId } = useParams<{ taskId?: string }>()
  const { tasks, settings, selectedTaskId, projects, risks } = useAppState()
  const selected = useSelectedTask()

  const projectOptions = useMemo(() => projects.map((p) => p.name).sort((a, b) => a.localeCompare(b)), [projects])
  const riskOptions = useMemo(() => risks.map((r) => r.title).sort((a, b) => a.localeCompare(b)), [risks])

  const [projectFilter, setProjectFilter] = useState<'All' | string>('All')
  const [riskFilter, setRiskFilter] = useState<'All' | string>('All')
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
      if (projectFilter !== 'All' && (t.project ?? '') !== projectFilter) return false
      if (riskFilter !== 'All' && (t.risk ?? '') !== riskFilter) return false
      return true
    })
  }, [priorityFilter, projectFilter, riskFilter, tasks])

  const isFocusMode = !!taskId

  useEffect(() => {
    if (!taskId) return
    dispatch({ type: 'selectTask', taskId })
  }, [dispatch, taskId])

  // If we entered focus mode with an unknown ID, fall back to list view.
  useEffect(() => {
    if (!taskId) return
    const exists = tasks.some((t) => t.id === taskId)
    if (!exists) navigate('/tasks', { replace: true })
  }, [navigate, taskId, tasks])

  return (
    <div className="page">
      <h2 className="pageTitle">Tasks</h2>

      <div className={`tasksStack ${isFocusMode ? 'tasksStackFocus' : ''}`}>
        {!isFocusMode ? (
          <section className="card tight tasksFiltersRow" aria-label="Task filters">
            <label className="field">
              <div className="fieldLabel">Project</div>
              <select className="select" value={projectFilter} onChange={(e) => setProjectFilter(e.target.value)}>
                <option value="All">All</option>
                <option value="">(None)</option>
                {projectOptions.map((p) => (
                  <option key={p} value={p}>
                    {p}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Risk</div>
              <select className="select" value={riskFilter} onChange={(e) => setRiskFilter(e.target.value)}>
                <option value="All">All</option>
                <option value="">(None)</option>
                {riskOptions.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
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

          </section>
        ) : null}

        {!isFocusMode ? (
          <section className="list listCard tasksListRow" aria-label="Task list">
            {filtered.map((t) => {
              const stale = daysSince(t.lastTouchedIso) >= settings.staleDays
              const days = daysSince(t.lastTouchedIso)
              const activity =
                days >= settings.staleDays ? 'red' : days >= Math.max(1, settings.staleDays - 2) ? 'yellow' : 'green'
              return (
                <button
                  key={t.id}
                  className={`listRow listRowBtn tasksTaskRow ${t.id === selectedTaskId ? 'listRowActive' : ''}`}
                  onClick={() => dispatch({ type: 'selectTask', taskId: t.id })}
                  onDoubleClick={() => navigate(`/tasks/${t.id}`)}
                >
                  <div className="listMain">
                    <div className="listTitle listTitleWrap">{t.title}</div>
                    <div className="listMeta listMetaWrap">
                      {t.priority}
                      {t.project ? ` • ${t.project}` : ''}
                      {t.risk ? ` • Risk: ${t.risk}` : ''}
                    </div>
                    <div className="listMeta listMetaWrap">
                      {t.dueDate ? `Due ${t.dueDate}` : ' '}
                    </div>
                  </div>
                  <div className="pillRow">
                    <div className="pill">
                      {formatDurationFromMinutes(parseDurationText(t.estimatedDurationText)?.totalMinutes ?? 0)}
                    </div>
                    <span
                      className={`activityDot activityDot-${activity}`}
                      title={`Activity: ${days}d ago (stale threshold ${settings.staleDays}d)`}
                      aria-label={`Activity: ${days} days ago`}
                    />
                  </div>
                </button>
              )
            })}
            {filtered.length === 0 ? <div className="muted pad">No tasks match your filters.</div> : null}
          </section>
        ) : null}

        <section className="tasksDetailRow" aria-label="Task detail editor">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a task to edit.</div>
            </div>
          ) : (
            <TaskDetail
              task={selected}
              staleDays={settings.staleDays}
              isFocusMode={isFocusMode}
              onEnterFocus={() => navigate(`/tasks/${selected.id}`)}
              onExitFocus={() => navigate('/tasks')}
              projectOptions={projectOptions}
              riskOptions={riskOptions}
            />
          )}
        </section>
      </div>
    </div>
  )
}

function TaskDetail({
  task,
  staleDays,
  isFocusMode,
  onEnterFocus,
  onExitFocus,
  projectOptions,
  riskOptions,
}: {
  task: Task
  staleDays: number
  isFocusMode: boolean
  onEnterFocus: () => void
  onExitFocus: () => void
  projectOptions: string[]
  riskOptions: string[]
}) {
  const dispatch = useAppDispatch()
  const stale = daysSince(task.lastTouchedIso) >= staleDays

  function update(patch: Partial<Task>) {
    dispatch({ type: 'updateTask', task: { ...task, ...patch } })
  }

  const estimateParsed = parseDurationText(task.estimatedDurationText)
  const estimatePreview = estimateParsed ? formatDurationFromMinutes(estimateParsed.totalMinutes) : 'Invalid'
  const actualParsed = parseDurationText(task.actualDurationText ?? '')
  const actualPreview = actualParsed ? formatDurationFromMinutes(actualParsed.totalMinutes) : task.actualDurationText ? 'Invalid' : '—'

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div className="detailTitle">Task Detail</div>
        <div className="rowTiny">
          <div className="mutedSmall">
            Last touched: {new Date(task.lastTouchedIso).toLocaleString()}
            {stale ? ` • stale` : ''}
          </div>
          {isFocusMode ? (
            <button className="btn btnGhost" onClick={onExitFocus}>
              Exit focus
            </button>
          ) : (
            <button className="btn btnGhost" onClick={onEnterFocus}>
              Focus
            </button>
          )}
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

        <label className="field">
          <div className="fieldLabel">Estimated Duration (required)</div>
          <input
            className="input"
            placeholder="e.g., 1d, 2h30m"
            value={task.estimatedDurationText}
            onChange={(e) => update({ estimatedDurationText: e.target.value })}
          />
          <div className={`mutedSmall ${estimateParsed ? '' : 'textBad'}`}>Estimated: {estimatePreview}</div>
        </label>

        <label className="field">
          <div className="fieldLabel">Confidence</div>
          <select
            className="select"
            value={task.estimateConfidence}
            onChange={(e) => update({ estimateConfidence: e.target.value as Task['estimateConfidence'] })}
          >
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
        </label>

        <label className="field">
          <div className="fieldLabel">Project (optional)</div>
          <select
            className="select"
            value={task.project ?? ''}
            onChange={(e) => update({ project: e.target.value || undefined })}
          >
            <option value="">(None)</option>
            {projectOptions.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </label>

        <label className="field">
          <div className="fieldLabel">Risk (optional)</div>
          <select
            className="select"
            value={task.risk ?? ''}
            onChange={(e) => update({ risk: e.target.value || undefined })}
          >
            <option value="">(None)</option>
            {riskOptions.map((r) => (
              <option key={r} value={r}>
                {r}
              </option>
            ))}
          </select>
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

        <label className="field span2">
          <div className="fieldLabel">Actual Duration (optional)</div>
          <input
            className="input"
            placeholder="e.g., 1d2h, 45m"
            value={task.actualDurationText ?? ''}
            onChange={(e) => update({ actualDurationText: e.target.value })}
          />
          <div className={`mutedSmall ${task.actualDurationText && !actualParsed ? 'textBad' : ''}`}>Actual: {actualPreview}</div>
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

// duration parsing lives in app/duration.ts
