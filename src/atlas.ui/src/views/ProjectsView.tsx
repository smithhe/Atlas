import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import {
  useAppDispatch,
  useAppState,
  useSelectedProject,
} from '../app/state/AppState'
import type { HealthSignal, Priority, Project, ProjectStatus, Risk, TaskStatus } from '../app/types'

export function ProjectsView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { projectId } = useParams<{ projectId?: string }>()
  const { projects, selectedProjectId } = useAppState()
  const selected = useSelectedProject()
  const isFocusMode = !!projectId

  useEffect(() => {
    ai.setContext('Context: Projects', [
      { id: 'project-summary', label: 'Summarize project status (draft)' },
      { id: 'identify-risks', label: 'Identify risks (draft)' },
    ])
  }, [ai.setContext])

  useEffect(() => {
    if (!projectId) return
    dispatch({ type: 'selectProject', projectId })
  }, [dispatch, projectId])

  // If we entered focus mode with an unknown ID, fall back to list view.
  useEffect(() => {
    if (!projectId) return
    const exists = projects.some((p) => p.id === projectId)
    if (!exists) navigate('/projects', { replace: true })
  }, [navigate, projectId, projects])

  return (
    <div className="page pageFill">
      <h2 className="pageTitle">Projects</h2>

      <div className={`splitGrid splitGridFill ${isFocusMode ? 'projectsSplitFocus' : ''}`}>
        {!isFocusMode ? (
          <section className="pane paneLeft" aria-label="Project list">
            <div className="list listCard">
              {projects.map((p) => (
                <button
                  key={p.id}
                  className={`listRow listRowBtn ${p.id === selectedProjectId ? 'listRowActive' : ''}`}
                  onClick={() => dispatch({ type: 'selectProject', projectId: p.id })}
                  onDoubleClick={() => navigate(`/projects/${p.id}`)}
                >
                  <div className="listMain">
                    <div className="listTitle">{p.name}</div>
                    <div className="listMeta">{p.summary}</div>
                  </div>
                </button>
              ))}
            </div>
          </section>
        ) : null}

        <section className="pane paneCenter" aria-label="Project detail">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a project.</div>
            </div>
          ) : (
            <ProjectDetail
              project={selected}
              isFocusMode={isFocusMode}
              onEnterFocus={(search) => navigate(`/projects/${selected.id}${search}`)}
              onExitFocus={(search) => navigate(`/projects${search}`)}
            />
          )}
        </section>
      </div>
    </div>
  )
}

function ProjectDetail({
  project,
  isFocusMode,
  onEnterFocus,
  onExitFocus,
}: {
  project: Project
  isFocusMode: boolean
  onEnterFocus: (search: string) => void
  onExitFocus: (search: string) => void
}) {
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { tasks, risks, team } = useAppState()
  const [searchParams, setSearchParams] = useSearchParams()
  const tabParam = (searchParams.get('tab') ?? 'overview').toLowerCase()
  const tab: 'overview' | 'tasks' | 'risks' = tabParam === 'tasks' ? 'tasks' : tabParam === 'risks' ? 'risks' : 'overview'
  const isOverviewTab = tab === 'overview'

  const [isEditingOverview, setIsEditingOverview] = useState(false)
  const [draft, setDraft] = useState<Project>(project)

  const [taskQuery, setTaskQuery] = useState('')
  const [taskStatusFilter, setTaskStatusFilter] = useState<TaskStatus | 'All'>('All')
  const [taskPriorityFilter, setTaskPriorityFilter] = useState<Priority | 'All'>('All')
  const [taskAssigneeFilter, setTaskAssigneeFilter] = useState<string | 'All'>('All')
  const [taskDueFilter, setTaskDueFilter] =
    useState<'All' | 'Overdue' | 'Next 7 days' | 'Next 30 days' | 'No due date'>('All')

  const [riskQuery, setRiskQuery] = useState('')
  const [riskSeverityFilter, setRiskSeverityFilter] = useState<'All' | 'Low' | 'Medium' | 'High'>('All')
  const [riskOwnerFilter, setRiskOwnerFilter] = useState<string | 'All'>('All')

  const linkedTasks = useMemo(
    () => tasks.filter((t) => project.linkedTaskIds.includes(t.id)),
    [project.linkedTaskIds, tasks],
  )
  const linkedRisks = useMemo(
    () => risks.filter((r) => project.linkedRiskIds.includes(r.id)),
    [project.linkedRiskIds, risks],
  )
  const members = useMemo(
    () => team.filter((m) => project.teamMemberIds.includes(m.id)),
    [project.teamMemberIds, team],
  )

  const memberById = useMemo(() => new Map(team.map((m) => [m.id, m])), [team])
  const productOwner = useMemo(() => (project.productOwnerId ? memberById.get(project.productOwnerId) : undefined), [
    memberById,
    project.productOwnerId,
  ])

  const currentSearch = useMemo(() => {
    const qs = searchParams.toString()
    return qs ? `?${qs}` : ''
  }, [searchParams])

  const taskAssigneeOptions = useMemo(() => {
    const ids = Array.from(new Set(linkedTasks.map((t) => t.assigneeId).filter(Boolean))) as string[]
    const names = ids.map((id) => ({ id, name: memberById.get(id)?.name ?? id }))
    return names.sort((a, b) => a.name.localeCompare(b.name))
  }, [linkedTasks, memberById])

  const riskOwnerOptions = useMemo(() => {
    const ids = Array.from(new Set(linkedRisks.map((r) => r.ownerId).filter(Boolean))) as string[]
    const names = ids.map((id) => ({ id, name: memberById.get(id)?.name ?? id }))
    return names.sort((a, b) => a.name.localeCompare(b.name))
  }, [linkedRisks, memberById])

  function setTab(next: typeof tab) {
    const nextParams = new URLSearchParams(searchParams)
    nextParams.set('tab', next)
    setSearchParams(nextParams, { replace: true })
  }

  useEffect(() => {
    // Reset edit mode when switching project (prevents "sticky edit" across projects).
    setIsEditingOverview(false)
    setDraft(project)
  }, [project.id])

  useEffect(() => {
    // Leaving the Overview tab cancels edit mode.
    if (!isOverviewTab) setIsEditingOverview(false)
  }, [isOverviewTab])

  useEffect(() => {
    // While editing, keep the URL pinned to the Overview tab (and hide the tabs UI).
    if (!isEditingOverview) return
    if (tab === 'overview') return
    const nextParams = new URLSearchParams(searchParams)
    nextParams.set('tab', 'overview')
    setSearchParams(nextParams, { replace: true })
  }, [isEditingOverview, searchParams, setSearchParams, tab])

  function updateDraft(patch: Partial<Project>) {
    setDraft((d) => ({ ...d, ...patch }))
  }

  function normalizeDraftForSave(input: Project): Project {
    const tags = (input.tags ?? []).map((t) => t.trim()).filter(Boolean)
    const links = (input.links ?? [])
      .map((l) => ({ label: (l.label ?? '').trim(), url: (l.url ?? '').trim() }))
      .filter((l) => l.label && l.url)

    const latestCheckInDateIso = input.latestCheckIn?.dateIso?.trim() ?? ''
    const latestCheckInNote = input.latestCheckIn?.note?.trim() ?? ''
    const latestCheckIn =
      latestCheckInDateIso || latestCheckInNote ? { dateIso: latestCheckInDateIso || new Date().toISOString().slice(0, 10), note: latestCheckInNote } : undefined

    return {
      ...input,
      tags,
      links,
      latestCheckIn,
      lastUpdatedIso: new Date().toISOString(),
    }
  }

  function saveOverviewEdits() {
    const next = normalizeDraftForSave(draft)
    dispatch({ type: 'updateProject', project: next })
    setIsEditingOverview(false)
  }

  function cancelOverviewEdits() {
    setDraft(project)
    setIsEditingOverview(false)
  }

  function formatDateLabel(isoDate?: string) {
    if (!isoDate) return '—'
    // Prefer deterministic formatting for ISO date-only strings (YYYY-MM-DD) to avoid timezone drift.
    const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(isoDate.trim())
    if (m) {
      const year = m[1]
      const monthIdx = Number(m[2]) - 1
      const day = m[3]
      const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'] as const
      const mon = months[monthIdx] ?? ''
      if (mon) return `${mon} ${day}, ${year}`
    }

    const d = new Date(isoDate)
    if (Number.isNaN(d.getTime())) return isoDate
    return d.toLocaleDateString(undefined, { month: 'short', day: '2-digit', year: 'numeric' })
  }

  function healthTone(health?: Project['health']) {
    if (health === 'Green') return 'toneGood'
    if (health === 'Yellow') return 'toneWarn'
    if (health === 'Red') return 'toneBad'
    return 'toneNeutral'
  }

  function statusTone(status?: Project['status']) {
    if (status === 'Active') return 'toneGood'
    if (status === 'Paused') return 'toneWarn'
    if (status === 'Completed') return 'toneNeutral'
    return 'toneNeutral'
  }

  function priorityTone(priority?: Priority) {
    if (!priority) return 'toneNeutral'
    if (priority === 'Low') return 'toneNeutral'
    if (priority === 'Medium') return 'toneWarn'
    if (priority === 'High' || priority === 'Critical') return 'toneBad'
    return 'toneNeutral'
  }

  function taskStatusTone(s?: TaskStatus) {
    if (s === 'Done') return 'toneGood'
    if (s === 'Blocked') return 'toneBad'
    if (s === 'In Progress') return 'toneGood'
    return 'toneNeutral'
  }

  function severityTone(sev: Risk['severity']) {
    switch (sev) {
      case 'High':
        return 'toneBad'
      case 'Medium':
        return 'toneWarn'
      case 'Low':
        return 'toneNeutral'
    }
  }

  const filteredTasks = useMemo(() => {
    const q = taskQuery.trim().toLowerCase()
    const todayIso = new Date().toISOString().slice(0, 10)
    const today = new Date(todayIso).getTime()

    function dueBucket(dueDate?: string) {
      if (!dueDate) return 'No due date' as const
      const due = new Date(dueDate).getTime()
      if (Number.isNaN(due)) return 'All' as const
      if (due < today) return 'Overdue' as const
      const days = Math.floor((due - today) / (1000 * 60 * 60 * 24))
      if (days <= 7) return 'Next 7 days' as const
      if (days <= 30) return 'Next 30 days' as const
      return 'All' as const
    }

    return linkedTasks.filter((t) => {
      if (taskStatusFilter !== 'All' && (t.status ?? 'Not Started') !== taskStatusFilter) return false
      if (taskPriorityFilter !== 'All' && t.priority !== taskPriorityFilter) return false
      if (taskAssigneeFilter !== 'All' && (t.assigneeId ?? '') !== taskAssigneeFilter) return false
      if (taskDueFilter !== 'All' && dueBucket(t.dueDate) !== taskDueFilter) return false
      if (q) {
        const hay = `${t.title} ${t.summary ?? ''}`.toLowerCase()
        if (!hay.includes(q)) return false
      }
      return true
    })
  }, [linkedTasks, taskAssigneeFilter, taskDueFilter, taskPriorityFilter, taskQuery, taskStatusFilter])

  const filteredRisks = useMemo(() => {
    const q = riskQuery.trim().toLowerCase()
    return linkedRisks.filter((r) => {
      if (riskSeverityFilter !== 'All' && r.severity !== riskSeverityFilter) return false
      if (riskOwnerFilter !== 'All' && (r.ownerId ?? '') !== riskOwnerFilter) return false
      if (q) {
        const hay = `${r.title} ${r.description ?? ''}`.toLowerCase()
        if (!hay.includes(q)) return false
      }
      return true
    })
  }, [linkedRisks, riskOwnerFilter, riskQuery, riskSeverityFilter])

  const rollup = useMemo(() => {
    const totalTasks = linkedTasks.length
    const doneTasks = linkedTasks.filter((t) => (t.status ?? 'Not Started') === 'Done').length
    const taskCompletionPct = totalTasks > 0 ? Math.round((doneTasks / totalTasks) * 100) : 0
    const openTasks = linkedTasks.filter((t) => (t.status ?? 'Not Started') !== 'Done')
    const highPriorityOpenTasks = openTasks.filter((t) => t.priority === 'High' || t.priority === 'Critical')
    const openRisks = linkedRisks.filter((r) => r.status !== 'Resolved')
    const atRiskCount = linkedRisks.filter((r) => r.status !== 'Resolved' && (r.severity === 'High' || r.severity === 'Medium')).length
    return {
      totalTasks,
      doneTasks,
      taskCompletionPct,
      openTasks: openTasks.length,
      highPriorityOpenTasks: highPriorityOpenTasks.length,
      openRisks: openRisks.length,
      atRiskCount,
    }
  }, [linkedRisks, linkedTasks])

  const allMemberOptions = useMemo(() => {
    return [...team].sort((a, b) => a.name.localeCompare(b.name))
  }, [team])

  const statusOptions: ProjectStatus[] = ['Active', 'Paused', 'Completed']
  const healthOptions: HealthSignal[] = ['Green', 'Yellow', 'Red']

  const effectiveTab: typeof tab = isEditingOverview ? 'overview' : tab

  return (
    <div className="card pad projectDetailCard" aria-label="Project detail">
      <div className="projectHeader">
        <div className="projectHeaderLeft">
          <div className="projectTitle">{project.name}</div>
        </div>
        <div className="projectHeaderRight">
          {!isEditingOverview ? (
            <div className="pillRow projectHeaderPills" aria-label="Project status summary">
              {project.status ? <span className={`pill ${statusTone(project.status)}`}>Status: {project.status}</span> : null}
              {project.health ? <span className={`pill ${healthTone(project.health)}`}>Health: {project.health}</span> : null}
              {project.targetDateIso ? <span className="pill toneNeutral">Target: {formatDateLabel(project.targetDateIso)}</span> : null}
              {project.priority ? <span className={`pill ${priorityTone(project.priority)}`}>Priority: {project.priority}</span> : null}
            </div>
          ) : null}
          <div className="rowTiny projectHeaderActions" aria-label="Project focus actions">
            {effectiveTab === 'overview' ? (
              isEditingOverview ? (
                <>
                  <button className="btn btnSecondary" onClick={saveOverviewEdits}>
                    Save
                  </button>
                  <button className="btn btnGhost" onClick={cancelOverviewEdits}>
                    Cancel
                  </button>
                </>
              ) : (
                <button
                  className="btn btnGhost"
                  onClick={() => {
                    // Edit is scoped to Overview; pin to overview and hide tabs.
                    if (tab !== 'overview') setTab('overview')
                    setDraft(project)
                    setIsEditingOverview(true)
                  }}
                >
                  Edit
                </button>
              )
            ) : null}
            {isFocusMode ? (
              <button className="btn btnGhost" onClick={() => onExitFocus(currentSearch)}>
                Exit focus
              </button>
            ) : (
              <button className="btn btnGhost" onClick={() => onEnterFocus(currentSearch)}>
                Focus
              </button>
            )}
          </div>
        </div>
      </div>

      {!isEditingOverview ? (
        <div className="tabsBar projectTabsBar" aria-label="Project tabs">
          <button className={`tabBtn ${tab === 'overview' ? 'tabBtnActive' : ''}`} onClick={() => setTab('overview')}>
            Overview
          </button>
          <button className={`tabBtn ${tab === 'tasks' ? 'tabBtnActive' : ''}`} onClick={() => setTab('tasks')}>
            Tasks
          </button>
          <button className={`tabBtn ${tab === 'risks' ? 'tabBtnActive' : ''}`} onClick={() => setTab('risks')}>
            Risks
          </button>
        </div>
      ) : null}

      <div className="projectDetailScroll" aria-label="Project detail scroll region">
        {effectiveTab === 'overview' ? (
          <div
            className={`projectOverviewGrid ${isEditingOverview ? 'projectOverviewGridEditing' : ''}`}
            aria-label="Project overview"
          >
          {isEditingOverview ? (
            <section className="card subtle projectOverviewSection span2" aria-label="Edit overview">
              <div className="cardHeader">
                <div className="cardTitle">Edit Overview</div>
              </div>
              <div className="pad">
                <div className="fieldGrid2">
                  <label className="field">
                    <div className="fieldLabel">Status</div>
                    <select className="select" value={draft.status ?? 'Active'} onChange={(e) => updateDraft({ status: e.target.value as ProjectStatus })}>
                      {statusOptions.map((s) => (
                        <option key={s} value={s}>
                          {s}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="field">
                    <div className="fieldLabel">Health</div>
                    <select className="select" value={draft.health ?? 'Green'} onChange={(e) => updateDraft({ health: e.target.value as HealthSignal })}>
                      {healthOptions.map((h) => (
                        <option key={h} value={h}>
                          {h}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="field">
                    <div className="fieldLabel">Target</div>
                    <input
                      className="input"
                      type="date"
                      value={draft.targetDateIso ?? ''}
                      onChange={(e) => updateDraft({ targetDateIso: e.target.value || undefined })}
                    />
                  </label>
                  <label className="field">
                    <div className="fieldLabel">Priority</div>
                    <select className="select" value={draft.priority ?? 'Medium'} onChange={(e) => updateDraft({ priority: e.target.value as Priority })}>
                      <option value="Low">Low</option>
                      <option value="Medium">Medium</option>
                      <option value="High">High</option>
                      <option value="Critical">Critical</option>
                    </select>
                  </label>

                  <label className="field">
                    <div className="fieldLabel">Product Owner</div>
                    <select
                      className="select"
                      value={draft.productOwnerId ?? ''}
                      onChange={(e) => updateDraft({ productOwnerId: e.target.value || undefined })}
                    >
                      <option value="">(None)</option>
                      {allMemberOptions.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.name}
                        </option>
                      ))}
                    </select>
                  </label>

                  <label className="field span2">
                    <div className="fieldLabel">Tags (comma separated)</div>
                    <input
                      className="input"
                      placeholder="platform, reliability"
                      value={(draft.tags ?? []).join(', ')}
                      onChange={(e) => updateDraft({ tags: e.target.value.split(',').map((x) => x.trim()).filter(Boolean) })}
                    />
                  </label>

                  <label className="field span2">
                    <div className="fieldLabel">Description</div>
                    <textarea className="textarea" value={draft.description ?? ''} onChange={(e) => updateDraft({ description: e.target.value })} />
                  </label>

                  <label className="field span2">
                    <div className="fieldLabel">Summary</div>
                    <textarea className="textarea" value={draft.summary} onChange={(e) => updateDraft({ summary: e.target.value })} />
                  </label>

                  <label className="field">
                    <div className="fieldLabel">Latest check-in date</div>
                    <input
                      className="input"
                      type="date"
                      value={draft.latestCheckIn?.dateIso ?? ''}
                      onChange={(e) => updateDraft({ latestCheckIn: { dateIso: e.target.value, note: draft.latestCheckIn?.note ?? '' } })}
                    />
                  </label>

                  <label className="field span2">
                    <div className="fieldLabel">Latest check-in note</div>
                    <textarea
                      className="textarea"
                      value={draft.latestCheckIn?.note ?? ''}
                      onChange={(e) => updateDraft({ latestCheckIn: { dateIso: draft.latestCheckIn?.dateIso ?? '', note: e.target.value } })}
                    />
                  </label>

                  <div className="field span2">
                    <div className="fieldLabel">Links</div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                      {(draft.links ?? []).map((l, idx) => (
                        <div key={`${l.label}-${idx}`} style={{ display: 'grid', gridTemplateColumns: '1fr 1fr auto', gap: 8 }}>
                          <input
                            className="input inputInline"
                            placeholder="Label"
                            value={l.label}
                            onChange={(e) => {
                              const next = [...(draft.links ?? [])]
                              next[idx] = { ...next[idx], label: e.target.value }
                              updateDraft({ links: next })
                            }}
                          />
                          <input
                            className="input inputInline"
                            placeholder="URL"
                            value={l.url}
                            onChange={(e) => {
                              const next = [...(draft.links ?? [])]
                              next[idx] = { ...next[idx], url: e.target.value }
                              updateDraft({ links: next })
                            }}
                          />
                          <button
                            type="button"
                            className="btn btnGhost btnIcon"
                            title="Remove link"
                            onClick={() => updateDraft({ links: (draft.links ?? []).filter((_, i) => i !== idx) })}
                          >
                            ×
                          </button>
                        </div>
                      ))}
                      <button
                        type="button"
                        className="btn btnGhost"
                        onClick={() => updateDraft({ links: [...(draft.links ?? []), { label: '', url: '' }] })}
                      >
                        + Add link
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </section>
          ) : (
            <>
              <section className="card subtle projectOverviewSection" aria-label="Summary">
                <div className="cardHeader">
                  <div className="cardTitle">Summary</div>
                </div>
                <div className="pad projectOverviewBody">{project.summary}</div>
              </section>

              <section className="card subtle projectOverviewSection" aria-label="Details">
                <div className="cardHeader">
                  <div className="cardTitle">Details</div>
                </div>
                <div className="pad projectDetailsKv">
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Product Owner</div>
                    <div className="projectDetailsVal">{productOwner?.name ?? '—'}</div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Team members</div>
                    <div className="projectDetailsVal">{members.length ? members.map((m) => m.name).join(', ') : '—'}</div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Tags</div>
                    <div className="projectDetailsVal">
                      <div className="pillRow">
                        {(project.tags ?? []).length ? (
                          (project.tags ?? []).map((t) => (
                            <span key={t} className="pill toneNeutral">
                              {t}
                            </span>
                          ))
                        ) : (
                          <span className="muted">—</span>
                        )}
                      </div>
                    </div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Last updated</div>
                    <div className="projectDetailsVal">
                      {project.lastUpdatedIso ? formatDateLabel(project.lastUpdatedIso.slice(0, 10)) : '—'}
                    </div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Links</div>
                    <div className="projectDetailsVal">
                      {(project.links ?? []).length ? (
                        <span className="mutedSmall">{(project.links ?? []).map((l) => l.label).join(' • ')}</span>
                      ) : (
                        '—'
                      )}
                    </div>
                  </div>
                </div>
              </section>

              <section className="card subtle projectOverviewSection" aria-label="Latest check-in">
                <div className="cardHeader">
                  <div className="cardTitle">Latest Check-in</div>
                </div>
                <div className="pad">
                  <div className="mutedSmall">
                    {project.latestCheckIn?.dateIso ? formatDateLabel(project.latestCheckIn.dateIso) : '—'}
                  </div>
                  <div className="projectOverviewBody">{project.latestCheckIn?.note ?? '—'}</div>
                </div>
              </section>

              <section className="card subtle projectOverviewSection" aria-label="Rollup">
                <div className="cardHeader">
                  <div className="cardTitle">Rollup</div>
                </div>
                <div className="pad projectDetailsKv">
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Tasks complete</div>
                    <div className="projectDetailsVal">
                      <span className="pill toneNeutral">
                        {rollup.doneTasks} / {rollup.totalTasks}
                      </span>
                    </div>
                  </div>
                  <div className="ggProgressBar" aria-label="Task completion progress">
                    <div className="ggProgressFill" style={{ width: `${rollup.taskCompletionPct}%` }} />
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Open tasks</div>
                    <div className="projectDetailsVal">
                      <span className="pill toneNeutral">{rollup.openTasks}</span>
                    </div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">High priority</div>
                    <div className="projectDetailsVal">
                      <span className={`pill ${rollup.highPriorityOpenTasks > 0 ? 'toneWarn' : 'toneNeutral'}`}>
                        {rollup.highPriorityOpenTasks}
                      </span>
                    </div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">Open risks</div>
                    <div className="projectDetailsVal">
                      <span className={`pill ${rollup.openRisks > 0 ? 'toneBad' : 'toneGood'}`}>{rollup.openRisks}</span>
                    </div>
                  </div>
                  <div className="projectDetailsRow">
                    <div className="projectDetailsKey">At risk</div>
                    <div className="projectDetailsVal">
                      <span className={`pill ${rollup.atRiskCount > 0 ? 'toneWarn' : 'toneGood'}`}>{rollup.atRiskCount}</span>
                    </div>
                  </div>
                </div>
              </section>
            </>
          )}
          </div>
        ) : null}

        {!isEditingOverview && tab === 'tasks' ? (
        <div className="projectTabBody" aria-label="Project tasks tab">
          <section className="card subtle projectTabSection" aria-label="Task filters">
            <div className="pad projectFiltersRow">
              <label className="field">
                <div className="fieldLabel">Search</div>
                <input className="input" value={taskQuery} onChange={(e) => setTaskQuery(e.target.value)} placeholder="Filter tasks…" />
              </label>
              <label className="field">
                <div className="fieldLabel">Status</div>
                <select
                  className="select selectCompact"
                  value={taskStatusFilter}
                  onChange={(e) => setTaskStatusFilter(e.target.value as TaskStatus | 'All')}
                >
                  <option value="All">All</option>
                  <option value="Not Started">Not Started</option>
                  <option value="In Progress">In Progress</option>
                  <option value="Blocked">Blocked</option>
                  <option value="Done">Done</option>
                </select>
              </label>
              <label className="field">
                <div className="fieldLabel">Priority</div>
                <select
                  className="select selectCompact"
                  value={taskPriorityFilter}
                  onChange={(e) => setTaskPriorityFilter(e.target.value as Priority | 'All')}
                >
                  <option value="All">All</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </label>
              <label className="field">
                <div className="fieldLabel">Assignee</div>
                <select className="select selectCompact" value={taskAssigneeFilter} onChange={(e) => setTaskAssigneeFilter(e.target.value)}>
                  <option value="All">All</option>
                  {taskAssigneeOptions.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.name}
                    </option>
                  ))}
                </select>
              </label>
              <label className="field">
                <div className="fieldLabel">Due</div>
                <select className="select selectCompact" value={taskDueFilter} onChange={(e) => setTaskDueFilter(e.target.value as any)}>
                  <option value="All">All</option>
                  <option value="Overdue">Overdue</option>
                  <option value="Next 7 days">Next 7 days</option>
                  <option value="Next 30 days">Next 30 days</option>
                  <option value="No due date">No due date</option>
                </select>
              </label>
            </div>
          </section>

          <section className="card subtle projectTabSection" aria-label="Linked tasks">
            <div className="cardHeader">
              <div className="cardTitle">Linked Tasks</div>
            </div>
            <div className="projectTable" role="table" aria-label="Project tasks table">
              <div className="projectTableHeader" role="row">
                <div className="projectTh" role="columnheader">
                  Status
                </div>
                <div className="projectTh" role="columnheader">
                  Task
                </div>
                <div className="projectTh" role="columnheader">
                  Priority
                </div>
                <div className="projectTh" role="columnheader">
                  Assignee
                </div>
                <div className="projectTh" role="columnheader">
                  Due
                </div>
              </div>
              {filteredTasks.length ? (
                filteredTasks.map((t) => (
                  <button
                    key={t.id}
                    type="button"
                    className="projectTableRow projectTableRowBtn"
                    role="row"
                    title="Open task in focus mode"
                    onClick={() => navigate(`/tasks/${t.id}`)}
                  >
                    <div className="projectTd" role="cell">
                      <span className={`pill ${taskStatusTone(t.status ?? 'Not Started')}`}>{t.status ?? 'Not Started'}</span>
                    </div>
                    <div className="projectTd" role="cell">
                      <div className="projectCellTitle">{t.title}</div>
                      <div className="mutedSmall">{t.summary ?? ''}</div>
                    </div>
                    <div className="projectTd" role="cell">
                      {t.priority}
                    </div>
                    <div className="projectTd" role="cell">
                      {t.assigneeId ? memberById.get(t.assigneeId)?.name ?? t.assigneeId : '—'}
                    </div>
                    <div className="projectTd" role="cell">
                      {t.dueDate ? formatDateLabel(t.dueDate) : '—'}
                    </div>
                  </button>
                ))
              ) : (
                <div className="muted pad">No tasks match your filters.</div>
              )}
            </div>
          </section>
        </div>
      ) : null}

        {!isEditingOverview && tab === 'risks' ? (
        <div className="projectTabBody" aria-label="Project risks tab">
          <section className="card subtle projectTabSection" aria-label="Risk filters">
            <div className="pad projectFiltersRow projectFiltersRowRisks">
              <label className="field">
                <div className="fieldLabel">Search</div>
                <input className="input" value={riskQuery} onChange={(e) => setRiskQuery(e.target.value)} placeholder="Filter risks…" />
              </label>
              <label className="field">
                <div className="fieldLabel">Severity</div>
                <select
                  className="select selectCompact"
                  value={riskSeverityFilter}
                  onChange={(e) => setRiskSeverityFilter(e.target.value as any)}
                >
                  <option value="All">All</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                </select>
              </label>
              <label className="field">
                <div className="fieldLabel">Owner</div>
                <select className="select selectCompact" value={riskOwnerFilter} onChange={(e) => setRiskOwnerFilter(e.target.value)}>
                  <option value="All">All</option>
                  {riskOwnerOptions.map((o) => (
                    <option key={o.id} value={o.id}>
                      {o.name}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </section>

          <section className="card subtle projectTabSection" aria-label="Linked risks">
            <div className="cardHeader">
              <div className="cardTitle">Linked Risks</div>
            </div>
            <div className="projectTable projectRisksTable" role="table" aria-label="Project risks table">
              <div className="projectTableHeader projectRisksHeader" role="row">
                <div className="projectTh" role="columnheader">
                  Severity
                </div>
                <div className="projectTh" role="columnheader">
                  Risk
                </div>
                <div className="projectTh" role="columnheader">
                  Owner
                </div>
              </div>
              {filteredRisks.length ? (
                filteredRisks.map((r) => (
                  <button
                    key={r.id}
                    type="button"
                    className="projectTableRow projectRisksRow projectTableRowBtn"
                    role="row"
                    title="Open risk in focus mode"
                    onClick={() => navigate(`/risks/${r.id}`)}
                  >
                    <div className="projectTd" role="cell">
                      <span className={`pill ${severityTone(r.severity)}`}>{r.severity}</span>
                    </div>
                    <div className="projectTd" role="cell">
                      <div className="projectCellTitle">{r.title}</div>
                      <div className="mutedSmall">{r.description}</div>
                    </div>
                    <div className="projectTd" role="cell">
                      {r.ownerId ? memberById.get(r.ownerId)?.name ?? r.ownerId : '—'}
                    </div>
                  </button>
                ))
              ) : (
                <div className="muted pad">No risks match your filters.</div>
              )}
            </div>
          </section>
        </div>
      ) : null}
      </div>
    </div>
  )
}


