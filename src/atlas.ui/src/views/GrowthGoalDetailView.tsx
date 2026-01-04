import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAppDispatch, useAppState, useGrowthForMember } from '../app/state/AppState'
import type {
  Growth,
  GrowthGoal,
  GrowthGoalAction,
  GrowthGoalActionState,
  GrowthGoalCheckIn,
  GrowthGoalCheckInSignal,
  Priority,
} from '../app/types'

type Selected =
  | { kind: 'action'; id: string }
  | { kind: 'checkin'; id: string }
  | { kind: 'none' }

function isoDateToday() {
  return new Date().toISOString().slice(0, 10)
}

function isoNow() {
  return new Date().toISOString()
}

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

function goalStatusLabel(status: GrowthGoal['status']) {
  switch (status) {
    case 'OnTrack':
      return 'On Track'
    case 'NeedsAttention':
      return 'Needs Attention'
    case 'Completed':
      return 'Completed'
  }
}

function goalStatusTone(status: GrowthGoal['status']) {
  switch (status) {
    case 'Completed':
      return 'toneGood'
    case 'OnTrack':
      return 'toneGood'
    case 'NeedsAttention':
      return 'toneWarn'
  }
}

function formatShortDate(iso?: string) {
  if (!iso) return '—'
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { month: 'short', day: '2-digit' })
}

function formatLongDate(iso?: string) {
  if (!iso) return '—'
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { month: 'short', day: '2-digit', year: 'numeric' })
}

function priorityLabel(p?: Priority) {
  return p ? `Priority: ${p}` : undefined
}

function actionStateTone(state: GrowthGoalActionState) {
  switch (state) {
    case 'Complete':
      return 'toneGood'
    case 'InProgress':
      return 'toneWarn'
    case 'Planned':
      return 'toneNeutral'
  }
}

function checkInSignalTone(signal: GrowthGoalCheckInSignal) {
  switch (signal) {
    case 'Positive':
      return 'toneGood'
    case 'Mixed':
      return 'toneWarn'
    case 'Concern':
      return 'toneBad'
  }
}

export function GrowthGoalDetailView() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId, goalId } = useParams<{ memberId?: string; goalId?: string }>()
  const { team } = useAppState()
  const growth = useGrowthForMember(memberId)

  const member = useMemo(() => (memberId ? team.find((m) => m.id === memberId) : undefined), [memberId, team])
  const goal = useMemo(() => (goalId ? growth?.goals.find((g) => g.id === goalId) : undefined), [growth?.goals, goalId])
  const goalSafe = useMemo(() => {
    if (!goal) return undefined
    return {
      ...goal,
      actions: goal.actions ?? [],
      checkIns: goal.checkIns ?? [],
      successCriteria: goal.successCriteria ?? [],
    }
  }, [goal])

  const [selected, setSelected] = useState<Selected>({ kind: 'none' })
  const [edit, setEdit] = useState({
    timeframe: false,
    status: false,
    category: false,
    priority: false,
    summary: false,
    successCriteria: false,
  })
  const [actionFilter, setActionFilter] = useState<GrowthGoalActionState | 'All'>('All')
  const [actionSort, setActionSort] = useState<'DueDate' | 'State'>('DueDate')

  useEffect(() => {
    if (!memberId) return
    dispatch({ type: 'selectTeamMember', memberId })
  }, [dispatch, memberId])

  // When the goal changes, reset selection.
  useEffect(() => {
    setSelected({ kind: 'none' })
  }, [goalId])

  function commitGoal(update: (g: GrowthGoal) => GrowthGoal) {
    if (!memberId || !goalId || !growth) return
    const updatedGrowth: Growth = {
      ...growth,
      goals: growth.goals.map((g) => {
        if (g.id !== goalId) return g
        const base: GrowthGoal = {
          ...g,
          actions: g.actions ?? [],
          checkIns: g.checkIns ?? [],
          successCriteria: g.successCriteria ?? [],
          lastUpdatedIso: isoNow(),
        }
        return update(base)
      }),
    }
    dispatch({ type: 'updateGrowth', growth: updatedGrowth })
  }

  function addAction() {
    const id = newId('growth-action')
    commitGoal((g) => ({
      ...g,
      actions: [
        {
          id,
          title: 'New action',
          dueDateIso: undefined,
          state: 'Planned',
          priority: g.priority ?? 'Medium',
          notes: '',
          links: [],
        },
        ...g.actions,
      ],
    }))
    setSelected({ kind: 'action', id })
  }

  function addCheckIn() {
    const id = newId('growth-checkin')
    commitGoal((g) => ({
      ...g,
      checkIns: [
        {
          id,
          dateIso: isoDateToday(),
          signal: 'Mixed',
          note: '',
        },
        ...g.checkIns,
      ],
    }))
    setSelected({ kind: 'checkin', id })
  }

  const selectedAction: GrowthGoalAction | undefined =
    selected.kind === 'action' ? goalSafe?.actions.find((a) => a.id === selected.id) : undefined
  const selectedCheckIn: GrowthGoalCheckIn | undefined =
    selected.kind === 'checkin' ? goalSafe?.checkIns.find((c) => c.id === selected.id) : undefined

  const progress = useMemo(() => {
    const total = goalSafe?.actions.length ?? 0
    const done = goalSafe?.actions.filter((a) => a.state === 'Complete').length ?? 0
    const percent = total === 0 ? 0 : Math.round((done / total) * 100)
    return { total, done, percent }
  }, [goalSafe?.actions])

  const visibleActions = useMemo(() => {
    const actions = goalSafe?.actions ?? []
    const filtered = actionFilter === 'All' ? actions : actions.filter((a) => a.state === actionFilter)
    const sorted = [...filtered].sort((a, b) => {
      if (actionSort === 'State') {
        const order: Record<GrowthGoalActionState, number> = { InProgress: 0, Planned: 1, Complete: 2 }
        return order[a.state] - order[b.state]
      }
      // DueDate (default): undefined last; ascending
      const ad = a.dueDateIso ?? '9999-12-31'
      const bd = b.dueDateIso ?? '9999-12-31'
      if (ad === bd) return a.title.localeCompare(b.title)
      return ad.localeCompare(bd)
    })
    return sorted
  }, [actionFilter, actionSort, goalSafe?.actions])

  if (!memberId || !goalId) {
    return (
      <div className="page">
        <h2 className="pageTitle">Growth Goal</h2>
        <div className="card pad">
          <div className="muted">Missing route params.</div>
        </div>
      </div>
    )
  }

  if (!member || !growth || !goalSafe) {
    return (
      <div className="page">
        <h2 className="pageTitle">Growth Goal</h2>
        <div className="card pad">
          <div className="muted">Goal not found.</div>
          <div className="row">
            <button className="btn btnSecondary" onClick={() => navigate(`/team/${memberId}/growth`)}>
              Back to Growth
            </button>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="page">
      <div className="growthGoalHeader">
        <nav className="growthGoalBreadcrumbs" aria-label="Breadcrumbs">
          <Link className="crumbLink" to="/team">
            Team
          </Link>
          <span className="crumbSep" aria-hidden="true">
            /
          </span>
          <Link className="crumbLink" to={`/team/${memberId}`}>
            {member.name}
          </Link>
          <span className="crumbSep" aria-hidden="true">
            /
          </span>
          <Link className="crumbLink" to={`/team/${memberId}/growth`}>
            Growth
          </Link>
          <span className="crumbSep" aria-hidden="true">
            /
          </span>
          <span className="crumbCurrent">Goal</span>
        </nav>

        <div className="growthGoalHeaderTop">
          <div className="growthGoalHeaderLeft">
            <div className="growthGoalHeaderTitleRow">
              <div className="growthGoalHeaderTitle">Goal: {goalSafe.title}</div>
              <span className={`pill ${goalStatusTone(goalSafe.status)}`}>{goalStatusLabel(goalSafe.status)}</span>
              <span className="pill toneNeutral">{priorityLabel(goalSafe.priority ?? 'Medium')}</span>
              <span className="pill toneNeutral">{`Category: ${goalSafe.category ?? '—'}`}</span>
            </div>
          </div>

          <div className="growthGoalHeaderActions">
            <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}/growth`)} type="button">
              Back to Growth
            </button>
            <button className="btn btnSecondary" onClick={addAction} type="button">
              + Add Action
            </button>
            <button className="btn" onClick={addCheckIn} type="button">
              + Add Check-in
            </button>
          </div>
        </div>
      </div>

      <div className="growthGoalDetailGrid">
        {/* Left column: primary work area */}
        <div className="growthGoalLeft">
          <section className="card subtle" aria-label="Goal overview">
            <div className="cardHeader">
              <div className="cardTitle">GOAL OVERVIEW</div>
              <div className="ggOverviewLastUpdated" title={goalSafe.lastUpdatedIso ? `Last updated: ${goalSafe.lastUpdatedIso}` : undefined}>
                <span className="ggOverviewLastUpdatedLabel">Last updated</span>
                <span className="ggOverviewLastUpdatedValue">
                  {goalSafe.lastUpdatedIso ? formatLongDate(goalSafe.lastUpdatedIso.slice(0, 10)) : '—'}
                </span>
              </div>
            </div>
            <div className="pad">
              <div className="ggMiniGrid">
                <div className="ggMiniCard">
                  <div className="ggMiniHeader">
                    <div className="ggMiniLabel">Timeframe</div>
                    <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, timeframe: !e.timeframe }))} type="button">
                      {edit.timeframe ? 'Done' : 'Edit'}
                    </button>
                  </div>
                  {!edit.timeframe ? (
                    <div className="ggMiniValue ggTimeframeLines">
                      <div>
                        <span className="ggTimeframeKey">Start</span> <strong>{formatLongDate(goalSafe.startDateIso)}</strong>
                      </div>
                      <div>
                        <span className="ggTimeframeKey">Target</span> <strong>{formatLongDate(goalSafe.targetDateIso)}</strong>
                      </div>
                    </div>
                  ) : (
                    <div className="ggMiniEditRow">
                      <label className="field">
                        <div className="fieldLabel">Start</div>
                        <input
                          className="input"
                          type="date"
                          value={goalSafe.startDateIso ?? ''}
                          onChange={(e) => commitGoal((g) => ({ ...g, startDateIso: e.target.value || undefined }))}
                        />
                      </label>
                      <label className="field">
                        <div className="fieldLabel">Target</div>
                        <input
                          className="input"
                          type="date"
                          value={goalSafe.targetDateIso ?? ''}
                          onChange={(e) => commitGoal((g) => ({ ...g, targetDateIso: e.target.value || undefined }))}
                        />
                      </label>
                    </div>
                  )}
                </div>

                <div className="ggMiniCard">
                  <div className="ggMiniHeader">
                    <div className="ggMiniLabel">Status</div>
                    <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, status: !e.status }))} type="button">
                      {edit.status ? 'Done' : 'Edit'}
                    </button>
                  </div>
                  {!edit.status ? (
                    <div className="ggMiniValue">
                      <span className={`pill ${goalStatusTone(goalSafe.status)}`}>{goalStatusLabel(goalSafe.status)}</span>
                    </div>
                  ) : (
                    <label className="field">
                      <div className="fieldLabel">Status</div>
                      <select
                        className="select"
                        value={goalSafe.status}
                        onChange={(e) => commitGoal((g) => ({ ...g, status: e.target.value as GrowthGoal['status'] }))}
                      >
                        <option value="OnTrack">On Track</option>
                        <option value="NeedsAttention">Needs Attention</option>
                        <option value="Completed">Completed</option>
                      </select>
                    </label>
                  )}
                </div>

                <div className="ggMiniCard">
                  <div className="ggMiniHeader">
                    <div className="ggMiniLabel">Priority</div>
                    <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, priority: !e.priority }))} type="button">
                      {edit.priority ? 'Done' : 'Edit'}
                    </button>
                  </div>
                  {!edit.priority ? (
                    <div className="ggMiniValue">
                      <strong>{goalSafe.priority ?? 'Medium'}</strong>
                    </div>
                  ) : (
                    <label className="field">
                      <div className="fieldLabel">Priority</div>
                      <select
                        className="select"
                        value={goalSafe.priority ?? 'Medium'}
                        onChange={(e) => commitGoal((g) => ({ ...g, priority: e.target.value as Priority }))}
                      >
                        <option value="Low">Low</option>
                        <option value="Medium">Medium</option>
                        <option value="High">High</option>
                        <option value="Critical">Critical</option>
                      </select>
                    </label>
                  )}
                </div>

                <div className="ggMiniCard">
                  <div className="ggMiniHeader">
                    <div className="ggMiniLabel">Category</div>
                    <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, category: !e.category }))} type="button">
                      {edit.category ? 'Done' : 'Edit'}
                    </button>
                  </div>
                  {!edit.category ? (
                    <div className="ggMiniValue">{goalSafe.category?.trim() ? <strong>{goalSafe.category}</strong> : <span className="muted">—</span>}</div>
                  ) : (
                    <label className="field">
                      <div className="fieldLabel">Category</div>
                      <input
                        className="input"
                        value={goalSafe.category ?? ''}
                        placeholder="e.g. Leadership"
                        onChange={(e) => {
                          const v = e.target.value.trim()
                          commitGoal((g) => ({ ...g, category: v ? e.target.value : undefined }))
                        }}
                      />
                    </label>
                  )}
                </div>

              </div>

              <div className="ggBlock">
                <div className="ggBlockHeader">
                  <div className="ggBlockTitle">Progress</div>
                </div>
                <div className="ggProgressText">
                  <strong>{progress.percent}%</strong> (based on actions completed) • {progress.done}/{progress.total} complete
                </div>
                <div className="ggProgressBar" aria-hidden="true">
                  <div className="ggProgressFill" style={{ width: `${progress.percent}%` }} />
                </div>
              </div>

              <div className="ggBlock">
                <div className="ggBlockHeader">
                  <div className="ggBlockTitle">Summary</div>
                  <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, summary: !e.summary }))} type="button">
                    {edit.summary ? 'Done' : 'Edit'}
                  </button>
                </div>
                {!edit.summary ? (
                  <div className="ggBlockBody">{goalSafe.summary?.trim() ? goalSafe.summary : <span className="muted">—</span>}</div>
                ) : (
                  <textarea
                    className="textarea"
                    value={goalSafe.summary ?? ''}
                    placeholder="Short paragraph describing the goal and why it matters…"
                    onChange={(e) => commitGoal((g) => ({ ...g, summary: e.target.value }))}
                  />
                )}
              </div>

              <div className="ggBlock">
                <div className="ggBlockHeader">
                  <div className="ggBlockTitle">Success Criteria</div>
                  <button className="linkBtn" onClick={() => setEdit((e) => ({ ...e, successCriteria: !e.successCriteria }))} type="button">
                    {edit.successCriteria ? 'Done' : 'Edit'}
                  </button>
                </div>
                {!edit.successCriteria ? (
                  goalSafe.successCriteria.length === 0 ? (
                    <div className="ggBlockBody muted">—</div>
                  ) : (
                    <ul className="ggBullets">
                      {goalSafe.successCriteria.map((x) => (
                        <li key={x}>{x}</li>
                      ))}
                    </ul>
                  )
                ) : (
                  <textarea
                    className="textarea"
                    value={goalSafe.successCriteria.join('\n')}
                    placeholder={'One bullet per line…\nExample:\n- Clear PR context\n- Fewer review cycles'}
                    onChange={(e) =>
                      commitGoal((g) => ({
                        ...g,
                        successCriteria: e.target.value
                          .split('\n')
                          .map((x) => x.trim().replace(/^-+\s*/, ''))
                          .filter(Boolean),
                      }))
                    }
                  />
                )}
              </div>
            </div>
          </section>

          <section className="growthGoalSection" aria-label="Actions and milestones">
            <div className="growthGoalSectionTitleRow">
              <div className="growthGoalSectionTitle">ACTIONS & MILESTONES</div>
              <div className="ggControlsRow" aria-label="Actions controls">
                <label className="ggControl">
                  <span className="ggControlLabel">Filter</span>
                  <select className="select selectCompact" value={actionFilter} onChange={(e) => setActionFilter(e.target.value as GrowthGoalActionState | 'All')}>
                    <option value="All">All</option>
                    <option value="Planned">Planned</option>
                    <option value="InProgress">In Progress</option>
                    <option value="Complete">Complete</option>
                  </select>
                </label>
                <label className="ggControl">
                  <span className="ggControlLabel">Sort</span>
                  <select className="select selectCompact" value={actionSort} onChange={(e) => setActionSort(e.target.value as 'DueDate' | 'State')}>
                    <option value="DueDate">Due date</option>
                    <option value="State">State</option>
                  </select>
                </label>
              </div>
            </div>
            <div className="card subtle">
              <div className="ggTable" role="table" aria-label="Actions table">
                <div className="ggRow ggHeaderRow" role="row">
                  <div className="ggCell ggCellDone" role="columnheader">
                    Done
                  </div>
                  <div className="ggCell ggCellMain" role="columnheader">
                    Action
                  </div>
                  <div className="ggCell ggCellDue" role="columnheader">
                    Due
                  </div>
                  <div className="ggCell ggCellState" role="columnheader">
                    State
                  </div>
                </div>
                {visibleActions.length === 0 ? (
                  <div className="pad muted">No actions yet. Use “Add Action” to create one.</div>
                ) : (
                  visibleActions.map((a) => {
                    const isSelected = selected.kind === 'action' && selected.id === a.id
                    const done = a.state === 'Complete'
                    const notesPreview = (a.notes ?? '').trim().split('\n')[0]
                    return (
                      <button
                        key={a.id}
                        className={`ggRow ggRowBtn ${isSelected ? 'ggRowSelected' : ''}`}
                        role="row"
                        onClick={() => setSelected({ kind: 'action', id: a.id })}
                        type="button"
                      >
                        <div className="ggCell ggCellDone" role="cell" onClick={(e) => e.stopPropagation()}>
                          <input
                            type="checkbox"
                            checked={done}
                            onChange={(e) =>
                              commitGoal((g) => ({
                                ...g,
                                actions: g.actions.map((x) =>
                                  x.id === a.id ? { ...x, state: e.target.checked ? 'Complete' : 'Planned' } : x,
                                ),
                              }))
                            }
                          />
                        </div>
                        <div className="ggCell ggCellMain" role="cell">
                          <div className="ggPrimary">{a.title}</div>
                          {notesPreview ? <div className="ggRowSubtle">{`Notes: ${notesPreview}`}</div> : null}
                        </div>
                        <div className="ggCell ggCellDue" role="cell">
                          <div className="ggMuted">{formatShortDate(a.dueDateIso)}</div>
                        </div>
                        <div className="ggCell ggCellState" role="cell">
                          <span className={`pill ${actionStateTone(a.state)}`}>{a.state === 'InProgress' ? 'In Progress' : a.state}</span>
                        </div>
                      </button>
                    )
                  })
                )}
              </div>
            </div>
          </section>

          <section className="growthGoalSection" aria-label="Check-ins">
            <div className="growthGoalSectionTitleRow">
              <div className="growthGoalSectionTitle">CHECK-INS</div>
              <div className="mutedSmall">Brief notes on how it’s going • Click to edit on the right</div>
            </div>
            <div className="card subtle">
              <div className="ggTable" role="table" aria-label="Check-ins table">
                <div className="ggRow ggRow3 ggHeaderRow" role="row">
                  <div className="ggCell ggCellDate" role="columnheader">
                    Date
                  </div>
                  <div className="ggCell ggCellMain" role="columnheader">
                    Note
                  </div>
                  <div className="ggCell ggCellSignal" role="columnheader">
                    Signal
                  </div>
                </div>
                {goalSafe.checkIns.length === 0 ? (
                  <div className="pad muted">No check-ins yet. Use “Add Check-in” to add one.</div>
                ) : (
                  goalSafe.checkIns.map((c) => {
                    const isSelected = selected.kind === 'checkin' && selected.id === c.id
                    const preview = c.note.length > 110 ? `${c.note.slice(0, 110)}…` : c.note
                    return (
                      <button
                        key={c.id}
                        className={`ggRow ggRow3 ggRowBtn ${isSelected ? 'ggRowSelected' : ''}`}
                        role="row"
                        onClick={() => setSelected({ kind: 'checkin', id: c.id })}
                        type="button"
                      >
                        <div className="ggCell ggCellDate" role="cell">
                          <div className="ggMuted">{formatLongDate(c.dateIso)}</div>
                        </div>
                        <div className="ggCell ggCellMain" role="cell">
                          <div className="ggPrimary">{preview || '—'}</div>
                        </div>
                        <div className="ggCell ggCellSignal" role="cell">
                          <span className={`pill ${checkInSignalTone(c.signal)}`}>{c.signal}</span>
                        </div>
                      </button>
                    )
                  })
                )}
              </div>
            </div>
          </section>
        </div>

        {/* Right column: contextual editor */}
        <aside className="growthGoalRight" aria-label="Detail editor">
          <section className="card subtle ggEditorCard">
            <div className="cardHeader">
              <div className="cardTitle">DETAIL EDITOR</div>
              <div className="mutedSmall">Selection-based editing</div>
            </div>
            <div className="pad">
              {selected.kind === 'none' ? (
                <>
                  <div className="ggEditorCallout">
                    Click an Action/Milestone or Check-in on the left to edit it here. This keeps the main page scannable without lots of
                    nested cards.
                  </div>
                  <div className="muted">Select an action or check-in from the left to edit it.</div>
                </>
              ) : selected.kind === 'action' ? (
                !selectedAction ? (
                  <div className="muted">Action not found.</div>
                ) : (
                  <div className="fieldGrid">
                    <div className="ggEditorSelectedPillRow">
                      <span className="pill toneNeutral">Selected: Action</span>
                    </div>
                    <label className="field">
                      <div className="fieldLabel">Title</div>
                      <input
                        className="input"
                        value={selectedAction.title}
                        onChange={(e) =>
                          commitGoal((g) => ({
                            ...g,
                            actions: g.actions.map((x) => (x.id === selectedAction.id ? { ...x, title: e.target.value } : x)),
                          }))
                        }
                      />
                    </label>
                    <div className="fieldGrid2">
                      <label className="field">
                        <div className="fieldLabel">State</div>
                        <select
                          className="select"
                          value={selectedAction.state}
                          onChange={(e) =>
                            commitGoal((g) => ({
                              ...g,
                              actions: g.actions.map((x) =>
                                x.id === selectedAction.id ? { ...x, state: e.target.value as GrowthGoalActionState } : x,
                              ),
                            }))
                          }
                        >
                          <option value="Planned">Planned</option>
                          <option value="InProgress">In Progress</option>
                          <option value="Complete">Complete</option>
                        </select>
                      </label>
                      <label className="field">
                        <div className="fieldLabel">Due date</div>
                        <input
                          className="input"
                          type="date"
                          value={selectedAction.dueDateIso ?? ''}
                          onChange={(e) =>
                            commitGoal((g) => ({
                              ...g,
                              actions: g.actions.map((x) =>
                                x.id === selectedAction.id ? { ...x, dueDateIso: e.target.value || undefined } : x,
                              ),
                            }))
                          }
                        />
                      </label>
                      <label className="field">
                        <div className="fieldLabel">Priority</div>
                        <select
                          className="select"
                          value={selectedAction.priority ?? 'Medium'}
                          onChange={(e) =>
                            commitGoal((g) => ({
                              ...g,
                              actions: g.actions.map((x) =>
                                x.id === selectedAction.id ? { ...x, priority: e.target.value as Priority } : x,
                              ),
                            }))
                          }
                        >
                          <option value="Low">Low</option>
                          <option value="Medium">Medium</option>
                          <option value="High">High</option>
                          <option value="Critical">Critical</option>
                        </select>
                      </label>
                    </div>
                    <label className="field">
                      <div className="fieldLabel">Notes (what you’re tracking)</div>
                      <textarea
                        className="textarea"
                        value={selectedAction.notes ?? ''}
                        placeholder="Free-text notes…"
                        onChange={(e) =>
                          commitGoal((g) => ({
                            ...g,
                            actions: g.actions.map((x) =>
                              x.id === selectedAction.id ? { ...x, notes: e.target.value } : x,
                            ),
                          }))
                        }
                      />
                    </label>
                    <label className="field">
                      <div className="fieldLabel">Evidence / Links (optional)</div>
                      <textarea
                        className="textarea"
                        value={(selectedAction.links ?? []).join('\n')}
                        placeholder={'https://…\nhttps://…'}
                        onChange={(e) =>
                          commitGoal((g) => ({
                            ...g,
                            actions: g.actions.map((x) =>
                              x.id === selectedAction.id
                                ? { ...x, links: e.target.value.split('\n').map((s) => s.trim()).filter(Boolean) }
                                : x,
                            ),
                          }))
                        }
                      />
                    </label>
                  </div>
                )
              ) : !selectedCheckIn ? (
                <div className="muted">Check-in not found.</div>
              ) : (
                <div className="fieldGrid">
                  <div className="ggEditorSelectedPillRow">
                    <span className="pill toneNeutral">Selected: Check-in</span>
                  </div>
                  <div className="fieldGrid2">
                    <label className="field">
                      <div className="fieldLabel">Date</div>
                      <input
                        className="input"
                        type="date"
                        value={selectedCheckIn.dateIso}
                        onChange={(e) =>
                          commitGoal((g) => ({
                            ...g,
                            checkIns: g.checkIns.map((x) => (x.id === selectedCheckIn.id ? { ...x, dateIso: e.target.value } : x)),
                          }))
                        }
                      />
                    </label>
                    <label className="field">
                      <div className="fieldLabel">Signal</div>
                      <select
                        className="select"
                        value={selectedCheckIn.signal}
                        onChange={(e) =>
                          commitGoal((g) => ({
                            ...g,
                            checkIns: g.checkIns.map((x) =>
                              x.id === selectedCheckIn.id ? { ...x, signal: e.target.value as GrowthGoalCheckInSignal } : x,
                            ),
                          }))
                        }
                      >
                        <option value="Positive">Positive</option>
                        <option value="Mixed">Mixed</option>
                        <option value="Concern">Concern</option>
                      </select>
                    </label>
                  </div>
                  <label className="field">
                    <div className="fieldLabel">Check-in Note</div>
                    <textarea
                      className="textarea"
                      value={selectedCheckIn.note}
                      placeholder="Short narrative signal about how the goal is going…"
                      onChange={(e) =>
                        commitGoal((g) => ({
                          ...g,
                          checkIns: g.checkIns.map((x) => (x.id === selectedCheckIn.id ? { ...x, note: e.target.value } : x)),
                        }))
                      }
                    />
                  </label>
                </div>
              )}
            </div>
          </section>
        </aside>
      </div>
    </div>
  )
}

