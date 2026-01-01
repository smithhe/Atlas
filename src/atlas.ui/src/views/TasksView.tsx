import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
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
  const listRef = useRef<HTMLElement | null>(null)
  const [listMaxHeightPx, setListMaxHeightPx] = useState<number | undefined>(undefined)

  const projectOptions = useMemo(() => projects.map((p) => p.name).sort((a, b) => a.localeCompare(b)), [projects])
  const riskOptions = useMemo(() => risks.map((r) => r.title).sort((a, b) => a.localeCompare(b)), [risks])

  const [projectFilter, setProjectFilter] = useState<'All' | string>('All')
  const [riskFilter, setRiskFilter] = useState<'All' | string>('All')
  const [priorityFilter, setPriorityFilter] = useState<Priority | 'All'>('All')
  const [stalenessFilter, setStalenessFilter] = useState<'All' | 'Fresh' | 'Warning' | 'Stale'>('All')
  const [durationFilter, setDurationFilter] =
    useState<'All' | '<=30m' | '<=2h' | '<=4h' | '<=1d' | '>1d' | 'Invalid'>('All')
  const [sortBy, setSortBy] =
    useState<'Priority' | 'Project' | 'Risk' | 'Staleness' | 'Estimated Duration' | 'Title'>('Priority')
  const [sortDir, setSortDir] = useState<'Asc' | 'Desc'>('Desc')

  function sortButtonGlyph(category: typeof sortBy) {
    if (sortBy !== category) return '↕'
    return sortDir === 'Asc' ? '▲' : '▼'
  }

  function toggleSort(category: typeof sortBy) {
    if (sortBy === category) {
      setSortDir((d) => (d === 'Asc' ? 'Desc' : 'Asc'))
      return
    }

    setSortBy(category)
    // Reasonable defaults per category.
    if (category === 'Project' || category === 'Risk' || category === 'Title') setSortDir('Asc')
    else setSortDir('Desc')
  }

  useEffect(() => {
    ai.setContext('Context: Tasks', [
      { id: 'suggest-next-task', label: 'Suggest Next Task' },
      { id: 'summarize-week', label: 'Summarize Incomplete Work (week)' },
      { id: 'reprioritize', label: 'Reprioritize suggestions (draft)' },
    ])
  }, [ai.setContext])

  const filtered = useMemo(() => {
    const staleDays = settings.staleDays
    const warnStart = Math.max(1, staleDays - 2)

    return tasks.filter((t) => {
      if (priorityFilter !== 'All' && t.priority !== priorityFilter) return false
      if (projectFilter !== 'All' && (t.project ?? '') !== projectFilter) return false
      if (riskFilter !== 'All' && (t.risk ?? '') !== riskFilter) return false

      const days = daysSince(t.lastTouchedIso)
      const bucket = days >= staleDays ? 'Stale' : days >= warnStart ? 'Warning' : 'Fresh'
      if (stalenessFilter !== 'All' && bucket !== stalenessFilter) return false

      const parsed = parseDurationText(t.estimatedDurationText)
      const mins = parsed?.totalMinutes ?? null
      if (durationFilter === 'Invalid') return mins === null
      if (durationFilter !== 'All') {
        if (mins === null) return false
        if (durationFilter === '<=30m' && mins > 30) return false
        if (durationFilter === '<=2h' && mins > 120) return false
        if (durationFilter === '<=4h' && mins > 240) return false
        if (durationFilter === '<=1d' && mins > 1440) return false
        if (durationFilter === '>1d' && mins <= 1440) return false
      }

      return true
    })
  }, [durationFilter, priorityFilter, projectFilter, riskFilter, settings.staleDays, stalenessFilter, tasks])

  const filteredSorted = useMemo(() => {
    const staleDays = settings.staleDays
    const warnStart = Math.max(1, staleDays - 2)
    const dir = sortDir === 'Asc' ? 1 : -1

    function priorityRank(p: Priority) {
      switch (p) {
        case 'Critical':
          return 4
        case 'High':
          return 3
        case 'Medium':
          return 2
        case 'Low':
          return 1
      }
    }

    function cmp(a: Task, b: Task) {
      if (sortBy === 'Priority') return (priorityRank(a.priority) - priorityRank(b.priority)) * dir
      if (sortBy === 'Project') return ((a.project ?? '').localeCompare(b.project ?? '')) * dir
      if (sortBy === 'Risk') return ((a.risk ?? '').localeCompare(b.risk ?? '')) * dir
      if (sortBy === 'Title') return a.title.localeCompare(b.title) * dir
      if (sortBy === 'Estimated Duration') {
        const am = parseDurationText(a.estimatedDurationText)?.totalMinutes
        const bm = parseDurationText(b.estimatedDurationText)?.totalMinutes
        if (am == null && bm == null) return 0
        if (am == null) return 1
        if (bm == null) return -1
        return (am - bm) * dir
      }

      // Staleness: compare by bucket then by age in days.
      const ad = daysSince(a.lastTouchedIso)
      const bd = daysSince(b.lastTouchedIso)
      const ab = ad >= staleDays ? 3 : ad >= warnStart ? 2 : 1
      const bb = bd >= staleDays ? 3 : bd >= warnStart ? 2 : 1
      if (ab !== bb) return (ab - bb) * dir
      return (ad - bd) * dir
    }

    return filtered
      .map((t, i) => ({ t, i }))
      .sort((a, b) => {
        const c = cmp(a.t, b.t)
        return c !== 0 ? c : a.i - b.i
      })
      .map((x) => x.t)
  }, [filtered, settings.staleDays, sortBy, sortDir])

  const isFocusMode = !!taskId
  const showDetail = isFocusMode || !!selected

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

  // If there are >5 tasks, cap list height to exactly 5 rows so the 6th+ scrolls (regardless of window size).
  useLayoutEffect(() => {
    const el = listRef.current
    if (!el) return
    if (isFocusMode) return
    if (!showDetail) {
      // When the detail pane is hidden, let the list expand to fill available space.
      setListMaxHeightPx(undefined)
      return
    }

    if (filteredSorted.length <= 5) {
      setListMaxHeightPx(undefined)
      return
    }

    const rows = Array.from(el.querySelectorAll<HTMLElement>('.tasksTaskRow')).slice(0, 5)
    if (rows.length === 0) return

    const sumHeights = rows.reduce((sum, r) => sum + r.getBoundingClientRect().height, 0)
    // Include list padding + gaps between rows so the cap is visually correct.
    const style = getComputedStyle(el)
    const paddingTop = Number.parseFloat(style.paddingTop || '0') || 0
    const paddingBottom = Number.parseFloat(style.paddingBottom || '0') || 0
    const rowGap = Number.parseFloat(style.rowGap || style.gap || '0') || 0
    const gaps = rowGap * Math.max(0, rows.length - 1)

    // Small padding so borders don't clip.
    setListMaxHeightPx(Math.ceil(sumHeights + gaps + paddingTop + paddingBottom + 2))
  }, [filteredSorted.length, isFocusMode, showDetail])

  return (
    <div className="page pageFill">
      <h2 className="pageTitle">Tasks</h2>

      <div className={`tasksStack ${isFocusMode ? 'tasksStackFocus' : ''} ${!showDetail ? 'tasksStackNoDetail' : ''}`}>
        {!isFocusMode ? (
          <section className="card tight tasksFiltersRow" aria-label="Task filters">
            <label className="field">
              <div className="fieldLabel">Project</div>
              <div className="fieldInline">
                <select className="select selectCompact" value={projectFilter} onChange={(e) => setProjectFilter(e.target.value)}>
                  <option value="All">All</option>
                  <option value="">(None)</option>
                  {projectOptions.map((p) => (
                    <option key={p} value={p}>
                      {p}
                    </option>
                  ))}
                </select>
                <button type="button" className="btn btnGhost btnIcon" title="Sort by Project" onClick={() => toggleSort('Project')}>
                  {sortButtonGlyph('Project')}
                </button>
              </div>
            </label>

            <label className="field">
              <div className="fieldLabel">Risk</div>
              <div className="fieldInline">
                <select className="select selectCompact" value={riskFilter} onChange={(e) => setRiskFilter(e.target.value)}>
                  <option value="All">All</option>
                  <option value="">(None)</option>
                  {riskOptions.map((r) => (
                    <option key={r} value={r}>
                      {r}
                    </option>
                  ))}
                </select>
                <button type="button" className="btn btnGhost btnIcon" title="Sort by Risk" onClick={() => toggleSort('Risk')}>
                  {sortButtonGlyph('Risk')}
                </button>
              </div>
            </label>

            <label className="field">
              <div className="fieldLabel">Priority</div>
              <div className="fieldInline">
                <select
                  className="select selectCompact"
                  value={priorityFilter}
                  onChange={(e) => setPriorityFilter(e.target.value as Priority | 'All')}
                >
                  <option value="All">All</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
                <button type="button" className="btn btnGhost btnIcon" title="Sort by Priority" onClick={() => toggleSort('Priority')}>
                  {sortButtonGlyph('Priority')}
                </button>
              </div>
            </label>

            <label className="field">
              <div className="fieldLabel">Staleness</div>
              <div className="fieldInline">
                <select
                  className="select selectCompact"
                  value={stalenessFilter}
                  onChange={(e) => setStalenessFilter(e.target.value as any)}
                >
                  <option value="All">All</option>
                  <option value="Fresh">Fresh</option>
                  <option value="Warning">Getting close</option>
                  <option value="Stale">Stale</option>
                </select>
                <button type="button" className="btn btnGhost btnIcon" title="Sort by Staleness" onClick={() => toggleSort('Staleness')}>
                  {sortButtonGlyph('Staleness')}
                </button>
              </div>
            </label>

            <label className="field">
              <div className="fieldLabel">Est. Duration</div>
              <div className="fieldInline">
                <select
                  className="select selectCompact"
                  value={durationFilter}
                  onChange={(e) => setDurationFilter(e.target.value as any)}
                >
                  <option value="All">All</option>
                  <option value="<=30m">≤ 30m</option>
                  <option value="<=2h">≤ 2h</option>
                  <option value="<=4h">≤ 4h</option>
                  <option value="<=1d">≤ 1d</option>
                  <option value=">1d">&gt; 1d</option>
                  <option value="Invalid">Invalid</option>
                </select>
                <button
                  type="button"
                  className="btn btnGhost btnIcon"
                  title="Sort by Estimated Duration"
                  onClick={() => toggleSort('Estimated Duration')}
                >
                  {sortButtonGlyph('Estimated Duration')}
                </button>
              </div>
            </label>

          </section>
        ) : null}

        {!isFocusMode ? (
          <section
            className="list listCard tasksListRow"
            aria-label="Task list"
            ref={(n) => {
              listRef.current = n
            }}
            style={listMaxHeightPx ? { maxHeight: `${listMaxHeightPx}px` } : undefined}
          >
            {filteredSorted.map((t) => {
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
            {filteredSorted.length === 0 ? <div className="muted pad">No tasks match your filters.</div> : null}
          </section>
        ) : null}

        {showDetail ? (
          <section className="tasksDetailRow" aria-label="Task detail editor">
            {selected ? (
              <TaskDetail
                task={selected}
                staleDays={settings.staleDays}
                isFocusMode={isFocusMode}
                onEnterFocus={() => navigate(`/tasks/${selected.id}`)}
                onExitFocus={() => navigate('/tasks')}
                onClose={() => dispatch({ type: 'selectTask', taskId: undefined })}
                projectOptions={projectOptions}
                riskOptions={riskOptions}
              />
            ) : (
              <div className="card pad">
                <div className="muted">Task not found.</div>
              </div>
            )}
          </section>
        ) : null}
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
  onClose,
  projectOptions,
  riskOptions,
}: {
  task: Task
  staleDays: number
  isFocusMode: boolean
  onEnterFocus: () => void
  onExitFocus: () => void
  onClose: () => void
  projectOptions: string[]
  riskOptions: string[]
}) {
  const dispatch = useAppDispatch()
  const stale = daysSince(task.lastTouchedIso) >= staleDays
  const notesRef = useRef<HTMLTextAreaElement | null>(null)
  const notesMaxHeightPx = 280

  function update(patch: Partial<Task>) {
    dispatch({ type: 'updateTask', task: { ...task, ...patch } })
  }

  function formatLastTouched(iso: string) {
    const d = new Date(iso)
    return d.toLocaleString(undefined, {
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })
  }

  const estimateParsed = parseDurationText(task.estimatedDurationText)
  const estimatePreview = estimateParsed ? formatDurationFromMinutes(estimateParsed.totalMinutes) : 'Invalid'
  const actualParsed = parseDurationText(task.actualDurationText ?? '')
  const actualPreview = actualParsed ? formatDurationFromMinutes(actualParsed.totalMinutes) : task.actualDurationText ? 'Invalid' : '—'

  // Auto-grow notes until a cap, then scroll.
  useLayoutEffect(() => {
    const el = notesRef.current
    if (!el) return

    // Reset height so scrollHeight reflects the full content.
    el.style.height = 'auto'
    const next = Math.min(el.scrollHeight, notesMaxHeightPx)
    el.style.height = `${next}px`
    el.style.overflowY = el.scrollHeight > notesMaxHeightPx ? 'auto' : 'hidden'
  }, [notesMaxHeightPx, task.notes])

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Task Detail</div>
          <div className="mutedSmall">
            Last touched: {formatLastTouched(task.lastTouchedIso)}
            {stale ? ` • stale` : ''}
          </div>
        </div>
        <div className="rowTiny">
          {isFocusMode ? (
            <button className="btn btnGhost" onClick={onExitFocus}>
              Exit focus
            </button>
          ) : (
            <>
              <button className="btn btnGhost" onClick={onClose}>
                Close
              </button>
              <button className="btn btnGhost" onClick={onEnterFocus}>
                Focus
              </button>
            </>
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
          <textarea
            ref={notesRef}
            className="textarea textareaAutoGrow"
            value={task.notes}
            onChange={(e) => update({ notes: e.target.value })}
          />
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
