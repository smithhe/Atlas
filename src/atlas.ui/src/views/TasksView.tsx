import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTask } from '../app/state/AppState'
import type { Priority, Task, TaskStatus } from '../app/types'
import { useNavigate, useParams } from 'react-router-dom'
import { formatDurationFromMinutes, parseDurationText } from '../app/duration'

function daysSince(iso: string) {
  const a = new Date(iso).getTime()
  const b = Date.now()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

function taskStatusTone(s?: TaskStatus) {
  if (s === 'Done') return 'toneGood'
  if (s === 'Blocked') return 'toneBad'
  if (s === 'In Progress') return 'toneGood'
  return 'toneNeutral'
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
  const taskById = useMemo(() => new Map(tasks.map((t) => [t.id, t] as const)), [tasks])

  const [projectFilter, setProjectFilter] = useState<'All' | string>('All')
  const [riskFilter, setRiskFilter] = useState<'All' | string>('All')
  const [statusFilter, setStatusFilter] = useState<TaskStatus | 'All'>('All')
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
      if (statusFilter !== 'All' && (t.status ?? 'Not Started') !== statusFilter) return false
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
  }, [durationFilter, priorityFilter, projectFilter, riskFilter, settings.staleDays, stalenessFilter, statusFilter, tasks])

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
              <div className="fieldLabel">Status</div>
              <div className="fieldInline">
                <select className="select selectCompact" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as TaskStatus | 'All')}>
                  <option value="All">All</option>
                  <option value="Not Started">Not Started</option>
                  <option value="In Progress">In Progress</option>
                  <option value="Blocked">Blocked</option>
                  <option value="Done">Done</option>
                </select>
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
              const depCount = (t.dependencyTaskIds ?? []).filter((id) => taskById.get(id)?.status !== 'Done').length
              const statusLabel = t.status ?? 'Not Started'
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
                      {depCount ? ` • Blocked by ${depCount}` : ''}
                    </div>
                    <div className="listMeta listMetaWrap">
                      {t.dueDate ? `Due ${t.dueDate}` : ' '}
                    </div>
                  </div>
                  <div className="pillRow">
                    <div className="pill">
                      {formatDurationFromMinutes(parseDurationText(t.estimatedDurationText)?.totalMinutes ?? 0)}
                    </div>
                    <div className={`pill ${taskStatusTone(statusLabel)}`}>{statusLabel}</div>
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
  const { tasks, team } = useAppState()
  const stale = daysSince(task.lastTouchedIso) >= staleDays
  const [isEditing, setIsEditing] = useState(false)
  const notesRef = useRef<HTMLTextAreaElement | null>(null)
  const notesMaxHeightPx = 280
  const [addBlockerText, setAddBlockerText] = useState<string>('')

  function update(patch: Partial<Task>) {
    dispatch({ type: 'updateTask', task: { ...task, ...patch } })
  }

  useEffect(() => {
    // Switching selection should default back to view mode (prevents "sticky edit" across tasks).
    setIsEditing(false)
    setAddBlockerText('')
  }, [task.id])

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

  function taskStatusTone(s?: TaskStatus) {
    if (s === 'Done') return 'toneGood'
    if (s === 'Blocked') return 'toneBad'
    if (s === 'In Progress') return 'toneGood'
    return 'toneNeutral'
  }

  const estimateParsed = parseDurationText(task.estimatedDurationText)
  const estimatePreview = estimateParsed ? formatDurationFromMinutes(estimateParsed.totalMinutes) : 'Invalid'
  const actualParsed = parseDurationText(task.actualDurationText ?? '')
  const actualPreview = actualParsed ? formatDurationFromMinutes(actualParsed.totalMinutes) : task.actualDurationText ? 'Invalid' : '—'

  const dependencyOptions = useMemo(() => {
    return [...tasks]
      .filter((t) => t.id !== task.id)
      .sort((a, b) => a.title.localeCompare(b.title))
  }, [task.id, tasks])

  const taskById = useMemo(() => new Map(tasks.map((t) => [t.id, t] as const)), [tasks])
  const memberById = useMemo(() => new Map(team.map((m) => [m.id, m] as const)), [team])
  const assigneeOptions = useMemo(() => [...team].sort((a, b) => a.name.localeCompare(b.name)), [team])
  const assigneeName = useMemo(() => {
    if (!task.assigneeId) return undefined
    return memberById.get(task.assigneeId)?.name ?? task.assigneeId
  }, [memberById, task.assigneeId])

  // Prevent dependency cycles: you can't depend on a task that (directly or transitively) depends on you.
  const tasksThatDependOnMe = useMemo(() => {
    const dependentsById = new Map<string, string[]>()
    for (const t of tasks) {
      for (const dep of t.dependencyTaskIds ?? []) {
        const arr = dependentsById.get(dep) ?? []
        arr.push(t.id)
        dependentsById.set(dep, arr)
      }
    }

    const seen = new Set<string>()
    const queue: string[] = [task.id]
    while (queue.length) {
      const cur = queue.shift()!
      const deps = dependentsById.get(cur) ?? []
      for (const next of deps) {
        if (next === task.id) continue
        if (seen.has(next)) continue
        seen.add(next)
        queue.push(next)
      }
    }
    return seen
  }, [task.id, tasks])

  const dependencyIds = task.dependencyTaskIds ?? []

  const visibleBlockers = useMemo(() => {
    return dependencyIds
      .map((id) => taskById.get(id))
      .filter((t): t is NonNullable<typeof t> => Boolean(t))
      // Only show blockers that are not completed.
      .filter((t) => t.status !== 'Done')
  }, [dependencyIds, taskById])

  const blockerCandidates = useMemo(() => {
    const chosen = new Set(dependencyIds)
    return dependencyOptions
      // Keep the add list focused on "active" tasks only.
      .filter((t) => t.status !== 'Done')
      // Don't offer already-selected blockers.
      .filter((t) => !chosen.has(t.id))
  }, [dependencyIds, dependencyOptions])

  function resolveBlockerIdFromInput(inputRaw: string): string | undefined {
    const input = inputRaw.trim()
    if (!input) return undefined

    // Preferred format from the datalist: "Title [task-id]"
    const m = /\[([^\]]+)\]\s*$/.exec(input)
    if (m?.[1]) return m[1].trim()

    // Allow direct ID entry as a fallback.
    const byId = blockerCandidates.find((t) => t.id === input)
    if (byId) return byId.id

    // As a last resort, accept exact title match (may be ambiguous if titles duplicate).
    const exactTitle = blockerCandidates.find((t) => t.title === input)
    return exactTitle?.id
  }

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

  const statusLabel = task.status ?? 'Not Started'

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
          {!isEditing ? (
            <div className="pillRow">
              <span className={`pill ${taskStatusTone(statusLabel)}`}>{statusLabel}</span>
              <span className="pill toneNeutral">{task.priority}</span>
              <span className="pill toneNeutral">{assigneeName ? `Assignee: ${assigneeName}` : 'Unassigned'}</span>
            </div>
          ) : null}
          <button className="btn btnGhost" type="button" onClick={() => setIsEditing((e) => !e)}>
            {isEditing ? 'Done' : 'Edit'}
          </button>
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
        {isEditing ? (
          <label className="field span2">
            <div className="fieldLabel">Title</div>
            <input className="input" value={task.title} onChange={(e) => update({ title: e.target.value })} />
          </label>
        ) : (
          <div className="field span2">
            <div className="fieldLabel">Title</div>
            <div className="noteDetailReadonly">{task.title || '—'}</div>
          </div>
        )}

        <div className="field">
          <div className="fieldLabel">Status</div>
          {isEditing ? (
            <select className="select" value={statusLabel} onChange={(e) => update({ status: e.target.value as TaskStatus })}>
              <option value="Not Started">Not Started</option>
              <option value="In Progress">In Progress</option>
              <option value="Blocked">Blocked</option>
              <option value="Done">Done</option>
            </select>
          ) : (
            <div className="noteDetailReadonly">
              <span className={`pill ${taskStatusTone(statusLabel)}`}>{statusLabel}</span>
            </div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Priority (required)</div>
          {isEditing ? (
            <select className="select" value={task.priority} onChange={(e) => update({ priority: e.target.value as Priority })}>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
              <option value="Critical">Critical</option>
            </select>
          ) : (
            <div className="noteDetailReadonly">{task.priority}</div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Assignee (optional)</div>
          {isEditing ? (
            <select
              className="select"
              value={task.assigneeId ?? ''}
              onChange={(e) => update({ assigneeId: e.target.value || undefined })}
            >
              <option value="">(None)</option>
              {assigneeOptions.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.name}
                </option>
              ))}
              {task.assigneeId && !memberById.has(task.assigneeId) ? (
                <option value={task.assigneeId}>{task.assigneeId} (unknown)</option>
              ) : null}
            </select>
          ) : (
            <div className="noteDetailReadonly">{assigneeName ?? '—'}</div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Estimated Duration (required)</div>
          {isEditing ? (
            <>
              <input
                className="input"
                placeholder="e.g., 1d, 2h30m"
                value={task.estimatedDurationText}
                onChange={(e) => update({ estimatedDurationText: e.target.value })}
              />
              <div className={`mutedSmall ${estimateParsed ? '' : 'textBad'}`}>Estimated: {estimatePreview}</div>
            </>
          ) : (
            <>
              <div className="noteDetailReadonly">{task.estimatedDurationText || '—'}</div>
              <div className={`mutedSmall ${estimateParsed ? '' : 'textBad'}`}>Estimated: {estimatePreview}</div>
            </>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Confidence</div>
          {isEditing ? (
            <select
              className="select"
              value={task.estimateConfidence}
              onChange={(e) => update({ estimateConfidence: e.target.value as Task['estimateConfidence'] })}
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          ) : (
            <div className="noteDetailReadonly">{task.estimateConfidence}</div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Project (optional)</div>
          {isEditing ? (
            <select className="select" value={task.project ?? ''} onChange={(e) => update({ project: e.target.value || undefined })}>
              <option value="">(None)</option>
              {projectOptions.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
            </select>
          ) : (
            <div className="noteDetailReadonly">{task.project || '—'}</div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Risk (optional)</div>
          {isEditing ? (
            <select className="select" value={task.risk ?? ''} onChange={(e) => update({ risk: e.target.value || undefined })}>
              <option value="">(None)</option>
              {riskOptions.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          ) : (
            <div className="noteDetailReadonly">{task.risk || '—'}</div>
          )}
        </div>

        <div className="field">
          <div className="fieldLabel">Due date (optional)</div>
          {isEditing ? (
            <input className="input" type="date" value={task.dueDate ?? ''} onChange={(e) => update({ dueDate: e.target.value || undefined })} />
          ) : (
            <div className="noteDetailReadonly">{task.dueDate || '—'}</div>
          )}
        </div>

        <div className="field span2">
          <div className="fieldLabel">Actual Duration (optional)</div>
          {isEditing ? (
            <>
              <input
                className="input"
                placeholder="e.g., 1d2h, 45m"
                value={task.actualDurationText ?? ''}
                onChange={(e) => update({ actualDurationText: e.target.value })}
              />
              <div className={`mutedSmall ${task.actualDurationText && !actualParsed ? 'textBad' : ''}`}>Actual: {actualPreview}</div>
            </>
          ) : (
            <>
              <div className="noteDetailReadonly">{task.actualDurationText?.trim() ? task.actualDurationText : '—'}</div>
              <div className={`mutedSmall ${task.actualDurationText && !actualParsed ? 'textBad' : ''}`}>Actual: {actualPreview}</div>
            </>
          )}
        </div>

        <div className="field span2">
          <div className="fieldLabel">Blockers (optional)</div>
          <div className="card tight blockersCard">
            {isEditing ? (
              <div className="blockersAddRow">
                <input
                  className="input blockersAddSelect"
                  list="blockerCandidatesList"
                  placeholder="Add blocker…"
                  value={addBlockerText}
                  onChange={(e) => setAddBlockerText(e.target.value)}
                />
                <datalist id="blockerCandidatesList">
                  {blockerCandidates.map((t) => {
                    const disabled = tasksThatDependOnMe.has(t.id)
                    const label = `${t.title}${t.project ? ` • ${t.project}` : ''}${disabled ? ' • (would create cycle)' : ''} [${t.id}]`
                    return <option key={t.id} value={label} />
                  })}
                </datalist>
                <button
                  type="button"
                  className="btn btnSecondary"
                  disabled={!resolveBlockerIdFromInput(addBlockerText) || tasksThatDependOnMe.has(resolveBlockerIdFromInput(addBlockerText) ?? '')}
                  onClick={() => {
                    const id = resolveBlockerIdFromInput(addBlockerText)
                    if (!id) return
                    if (tasksThatDependOnMe.has(id)) return
                    update({ dependencyTaskIds: Array.from(new Set([...dependencyIds, id])) })
                    setAddBlockerText('')
                  }}
                >
                  Add
                </button>
              </div>
            ) : null}

            <div className="blockersList" role="list" aria-label="Blockers">
              {visibleBlockers.length === 0 ? <div className="mutedSmall">No active blockers.</div> : null}
              {visibleBlockers.map((t) => {
                const suffix = `${t.project ? ` • ${t.project}` : ''}${t.risk ? ` • Risk: ${t.risk}` : ''}`
                const statusLabel = t.status ?? 'Not Started'
                return (
                  <div key={t.id} className="blockersItem" role="listitem">
                    <div className="blockersItemText">
                      <div className="blockersItemTitle">{t.title}</div>
                      <div className="blockersItemMeta">{suffix}</div>
                    </div>
                    <div className="blockersItemRight">
                      <span className="pill blockersStatus">{statusLabel}</span>
                      {isEditing ? (
                        <button
                          type="button"
                          className="btn btnGhost blockersRemoveBtn"
                          onClick={() => update({ dependencyTaskIds: dependencyIds.filter((id) => id !== t.id) })}
                          title="Remove blocker"
                        >
                          Remove
                        </button>
                      ) : null}
                    </div>
                  </div>
                )
              })}
            </div>
          </div>
          {dependencyIds.some((id) => tasksThatDependOnMe.has(id)) ? (
            <div className="mutedSmall textBad">One or more selected dependencies would create a cycle; please remove them.</div>
          ) : null}
          {isEditing && dependencyIds.length ? (
            <div className="rowTiny">
              <button type="button" className="btn btnGhost" onClick={() => update({ dependencyTaskIds: [] })}>
                Clear dependencies
              </button>
            </div>
          ) : null}
        </div>

        <div className="field span2">
          <div className="fieldLabel">Notes</div>
          {isEditing ? (
            <textarea
              ref={notesRef}
              className="textarea textareaAutoGrow"
              value={task.notes}
              onChange={(e) => update({ notes: e.target.value })}
            />
          ) : (
            <div className="noteDetailReadonly" style={{ whiteSpace: 'pre-wrap' }}>
              {task.notes?.trim() ? task.notes : '—'}
            </div>
          )}
        </div>
      </div>

      <div className="row">
        <button className="btn" onClick={() => dispatch({ type: 'touchTask', taskId: task.id, touchedIso: new Date().toISOString() })}>
          Touch / Update Activity
        </button>
      </div>
    </div>
  )
}

// duration parsing lives in app/duration.ts
