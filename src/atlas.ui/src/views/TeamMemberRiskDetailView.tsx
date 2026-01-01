import { useEffect, useMemo, useState } from 'react'
import { NavLink, useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { TeamMemberRisk } from '../app/types'

function daysSince(iso?: string) {
  if (!iso) return undefined
  const a = new Date(iso).getTime()
  if (Number.isNaN(a)) return undefined
  const b = Date.now()
  return Math.max(0, Math.floor((b - a) / (1000 * 60 * 60 * 24)))
}

function severityClass(sev: TeamMemberRisk['severity']) {
  return sev.toLowerCase()
}

function formatIsoDate(iso: string) {
  // Treat ISO date (YYYY-MM-DD) as a local date to avoid timezone shifting.
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function TeamMemberRiskDetailView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId, teamMemberRiskId } = useParams<{ memberId: string; teamMemberRiskId: string }>()
  const { team, risks, teamMemberRisks } = useAppState()
  const member = useSelectedTeamMember()

  useEffect(() => {
    ai.setContext('Context: Team Member Risk Detail', [
      { id: 'summarize-risk', label: 'Summarize this risk' },
      { id: 'suggest-mitigation', label: 'Suggest mitigation experiments' },
    ])
  }, [ai.setContext])

  useEffect(() => {
    if (!memberId) return
    dispatch({ type: 'selectTeamMember', memberId })
  }, [dispatch, memberId])

  useEffect(() => {
    if (!memberId) return
    const exists = team.some((m) => m.id === memberId)
    if (!exists) navigate('/team', { replace: true })
  }, [memberId, navigate, team])

  const risk = useMemo(() => {
    if (!teamMemberRiskId) return undefined
    const r = teamMemberRisks.find((x) => x.id === teamMemberRiskId)
    if (!r) return undefined
    if (memberId && r.memberId !== memberId) return undefined
    return r
  }, [memberId, teamMemberRiskId, teamMemberRisks])

  useEffect(() => {
    if (!risk) return
    dispatch({ type: 'selectTeamMemberRisk', teamMemberRiskId: risk.id })
  }, [dispatch, risk])

  const linkedGlobalRisk = useMemo(() => {
    if (!risk?.linkedRiskId) return undefined
    return risks.find((r) => r.id === risk.linkedRiskId)
  }, [risk?.linkedRiskId, risks])

  const reviewedDaysAgo = daysSince(risk?.lastReviewedIso)

  const [isEditMode, setIsEditMode] = useState(false)
  const [draft, setDraft] = useState<TeamMemberRisk | undefined>(undefined)

  useEffect(() => {
    // Keep draft in sync with selection when entering/leaving edit mode, or when route changes.
    if (!risk) {
      setIsEditMode(false)
      setDraft(undefined)
      return
    }
    setDraft((prev) => {
      if (!isEditMode) return undefined
      // If editing and route changed to a new risk, reset the draft.
      if (!prev || prev.id !== risk.id) return risk
      return prev
    })
  }, [isEditMode, risk])

  function updateDraft(patch: Partial<TeamMemberRisk>) {
    if (!draft) return
    setDraft({ ...draft, ...patch })
  }

  function beginEdit() {
    if (!risk) return
    setDraft(risk)
    setIsEditMode(true)
  }

  function cancelEdit() {
    setIsEditMode(false)
    setDraft(undefined)
  }

  function saveEdit() {
    if (!draft) return
    dispatch({ type: 'updateTeamMemberRisk', teamMemberRisk: draft })
    setIsEditMode(false)
    setDraft(undefined)
  }

  const view = risk
  const edit = draft

  return (
    <div className="page">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Team Member Risk</div>
          <div className="mutedSmall">{member?.name ?? ''}</div>
        </div>
        <div className="row" style={{ marginTop: 0 }}>
          <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}/risks`)}>
            Back to risks
          </button>
          <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}`)}>
            Back to member
          </button>
        </div>
      </div>

      {memberId ? (
        <div className="tabsBar" role="tablist" aria-label="Member tabs">
          <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${memberId}`} end>
            Overview
          </NavLink>
          <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${memberId}/notes`}>
            Notes
          </NavLink>
          <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${memberId}/work-items`}>
            Work Items
          </NavLink>
          <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${memberId}/risks`}>
            Risks
          </NavLink>
          <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${memberId}/growth`}>
            Growth
          </NavLink>
        </div>
      ) : null}

      {!member ? (
        <div className="card pad">
          <div className="muted">Select a team member.</div>
        </div>
      ) : !view ? (
        <div className="card pad">
          <div className="muted">Risk not found.</div>
        </div>
      ) : (
        <div className="card pad tmRiskCard">
          <div className="tmRiskTopRow">
            <div className="tmRiskTitleBlock">
              {isEditMode && edit ? (
                <label className="field" style={{ margin: 0 }}>
                  <div className="srOnly">Title</div>
                  <input className="input tmRiskTitleInput" value={edit.title} onChange={(e) => updateDraft({ title: e.target.value })} />
                </label>
              ) : (
                <div className="tmRiskTitle">{view.title}</div>
              )}
              <div className="tmRiskSubtitle">
                Team Member Risk {member?.role ? `• ${member.role}` : ''}
              </div>
            </div>
            <div className="tmRiskTopActions">
              {!isEditMode ? (
                <div className={`tmRiskSeverityPill tmRiskSeverity-${severityClass(view.severity)}`}>{view.severity} Severity</div>
              ) : null}
              {isEditMode ? (
                <div className="row" style={{ marginTop: 0 }}>
                  <button className="btn btnSecondary" type="button" onClick={saveEdit} disabled={!edit}>
                    Save
                  </button>
                  <button className="btn btnGhost" type="button" onClick={cancelEdit}>
                    Cancel
                  </button>
                </div>
              ) : (
                <button className="btn btnGhost" type="button" onClick={beginEdit}>
                  Edit
                </button>
              )}
            </div>
          </div>

          {!isEditMode ? (
            <div className="tmRiskSummaryGrid" aria-label="Risk summary metrics">
              <div className="tmRiskMetricCard">
                <div className="tmRiskMetricLabel">RISK TYPE</div>
                <div className="tmRiskMetricValue">{view.riskType}</div>
              </div>
              <div className="tmRiskMetricCard">
                <div className="tmRiskMetricLabel">STATUS</div>
                <div className="tmRiskMetricValue">{view.status}</div>
              </div>
              <div className="tmRiskMetricCard">
                <div className="tmRiskMetricLabel">TREND</div>
                <div className="tmRiskMetricValue">{view.trend}</div>
              </div>
              <div className="tmRiskMetricCard">
                <div className="tmRiskMetricLabel">FIRST NOTICED</div>
                <div className="tmRiskMetricValue">{formatIsoDate(view.firstNoticedDateIso)}</div>
              </div>
              <div className="tmRiskMetricCard">
                <div className="tmRiskMetricLabel">IMPACT AREA</div>
                <div className="tmRiskMetricValue">{view.impactArea}</div>
              </div>
            </div>
          ) : null}

          {!isEditMode || !edit ? (
            <div className="tmRiskBody">
              <div className="tmRiskSectionTitle">Description</div>
              <div className="tmRiskSectionBody">{view.description}</div>

              <div className="tmRiskSectionTitle" style={{ marginTop: 14 }}>
                Current Action / Experiment
              </div>
              <div className="tmRiskSectionBody">{view.currentAction}</div>
            </div>
          ) : (
            <div className="tmRiskEditForm" aria-label="Edit risk form">
              <div className="fieldGrid2" style={{ marginTop: 14 }}>
                <label className="field">
                  <div className="fieldLabel">Severity</div>
                  <select className="select" value={edit.severity} onChange={(e) => updateDraft({ severity: e.target.value as TeamMemberRisk['severity'] })}>
                    <option value="Low">Low</option>
                    <option value="Medium">Medium</option>
                    <option value="High">High</option>
                  </select>
                </label>

                <label className="field">
                  <div className="fieldLabel">Risk type</div>
                  <input className="input" value={edit.riskType} onChange={(e) => updateDraft({ riskType: e.target.value })} />
                </label>

                <label className="field">
                  <div className="fieldLabel">Status</div>
                  <select className="select" value={edit.status} onChange={(e) => updateDraft({ status: e.target.value as TeamMemberRisk['status'] })}>
                    <option value="Open">Open</option>
                    <option value="Mitigating">Mitigating</option>
                    <option value="Resolved">Resolved</option>
                  </select>
                </label>

                <label className="field">
                  <div className="fieldLabel">Trend</div>
                  <select className="select" value={edit.trend} onChange={(e) => updateDraft({ trend: e.target.value as TeamMemberRisk['trend'] })}>
                    <option value="Improving">Improving</option>
                    <option value="Stable">Stable</option>
                    <option value="Worsening">Worsening</option>
                  </select>
                </label>

                <label className="field">
                  <div className="fieldLabel">First noticed</div>
                  <input className="input" type="date" value={edit.firstNoticedDateIso} onChange={(e) => updateDraft({ firstNoticedDateIso: e.target.value })} />
                </label>

                <label className="field span2">
                  <div className="fieldLabel">Impact area</div>
                  <input className="input" value={edit.impactArea} onChange={(e) => updateDraft({ impactArea: e.target.value })} />
                </label>

                <label className="field span2">
                  <div className="fieldLabel">Description</div>
                  <textarea className="textarea" value={edit.description} onChange={(e) => updateDraft({ description: e.target.value })} />
                </label>

                <label className="field span2">
                  <div className="fieldLabel">Current action / experiment</div>
                  <textarea className="textarea" value={edit.currentAction} onChange={(e) => updateDraft({ currentAction: e.target.value })} />
                </label>
              </div>
            </div>
          )}

          <div className="tmRiskFooter">
            <div className="tmRiskFooterLeft">
              <label className="field tmRiskLinkField">
                <div className="fieldLabel">Linked global risk</div>
                <div className="fieldInline">
                  <select
                    className="select selectCompact"
                    value={(isEditMode && edit ? edit.linkedRiskId : view.linkedRiskId) ?? ''}
                    onChange={(e) => {
                      const next = e.target.value || undefined
                      if (isEditMode && edit) {
                        updateDraft({ linkedRiskId: next })
                        return
                      }
                      dispatch({ type: 'updateTeamMemberRisk', teamMemberRisk: { ...view, linkedRiskId: next } })
                    }}
                  >
                    <option value="">(None)</option>
                    {risks.map((r) => (
                      <option key={r.id} value={r.id}>
                        {r.title}
                      </option>
                    ))}
                  </select>
                  <button
                    type="button"
                    className="btn btnGhost btnIcon"
                    title={linkedGlobalRisk ? 'Open in Risks & Mitigation' : 'No linked global risk'}
                    disabled={!linkedGlobalRisk}
                    onClick={() => {
                      if (!linkedGlobalRisk) return
                      dispatch({ type: 'selectRisk', riskId: linkedGlobalRisk.id })
                      navigate('/risks')
                    }}
                  >
                    ↗
                  </button>
                </div>
              </label>
            </div>

            <div className="tmRiskFooterRight">
              <div className="tmRiskFooterControls">
                <button
                  type="button"
                  className="btn btnGhost"
                  onClick={() => {
                    if (isEditMode && edit) {
                      updateDraft({ lastReviewedIso: new Date().toISOString() })
                      return
                    }
                    dispatch({
                      type: 'updateTeamMemberRisk',
                      teamMemberRisk: { ...view, lastReviewedIso: new Date().toISOString() },
                    })
                  }}
                  title="Update last reviewed timestamp"
                >
                  Mark reviewed
                </button>
                <div className="mutedSmall tmRiskLastReviewedText" aria-label="Last reviewed">
                  <span className="tmRiskLastReviewedLabel">Last reviewed:</span>{' '}
                  <span className="tmRiskLastReviewedValue">
                    {reviewedDaysAgo === undefined
                      ? '—'
                      : reviewedDaysAgo === 0
                        ? 'today'
                        : `${reviewedDaysAgo} day${reviewedDaysAgo === 1 ? '' : 's'} ago`}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

