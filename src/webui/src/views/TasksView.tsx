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

      <div
        className={`splitGrid tasksSplit ${ai.state.isOpen ? 'tasksSplitAiOpen' : 'tasksSplitAiClosed'} ${
          isFocusMode ? 'tasksSplitFocus' : ''
        }`}
      >
        {!isFocusMode ? (
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
                const lastTouched = new Date(t.lastTouchedIso).toLocaleDateString()
                const notesPreview = (t.notes ?? '').trim().replace(/\s+/g, ' ')
                return (
                  <button
                    key={t.id}
                    className={`listRow listRowBtn ${t.id === selectedTaskId ? 'listRowActive' : ''}`}
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
                        Last touched {lastTouched}
                        {t.dueDate ? ` • due ${t.dueDate}` : ''}
                        {stale ? ` • stale ${daysSince(t.lastTouchedIso)}d` : ''}
                      </div>
                      {notesPreview ? <div className="listMeta listMetaWrap listNotesPreview">{notesPreview}</div> : null}
                    </div>
                    <div className="pill">
                      {formatDurationFromMinutes(parseDurationText(t.estimatedDurationText)?.totalMinutes ?? 0)}
                    </div>
                  </button>
                )
              })}
              {filtered.length === 0 ? <div className="muted pad">No tasks match your filters.</div> : null}
            </div>
          </section>
        ) : null}

        <section className="pane paneCenter" aria-label="Task detail editor">
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
}: {
  task: Task
  staleDays: number
  isFocusMode: boolean
  onEnterFocus: () => void
  onExitFocus: () => void
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
