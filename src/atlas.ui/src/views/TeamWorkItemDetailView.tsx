import { useEffect, useMemo, useState } from 'react'
import { NavLink, useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { AzureItem } from '../app/types'
import { Markdown } from '../components/Markdown'

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

function formatIsoDateLong(iso?: string) {
  if (!iso) return '—'
  // Treat ISO date (YYYY-MM-DD) as a local date to avoid timezone shifting.
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })
}

export function TeamWorkItemDetailView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId, workItemId } = useParams<{ memberId: string; workItemId: string }>()
  const { team, projects } = useAppState()
  const member = useSelectedTeamMember()
  const [newNoteText, setNewNoteText] = useState('')

  useEffect(() => {
    ai.setContext('Context: Team Work Item Detail', [{ id: 'summarize-item', label: 'Summarize this work item' }])
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

  const item = useMemo(() => {
    if (!member || !workItemId) return undefined
    return member.azureItems.find((a) => a.id === workItemId)
  }, [member, workItemId])

  const prLinks = useMemo(() => {
    if (!item) return []
    return (item.prUrls ?? []).filter(Boolean)
  }, [item])

  function updateWorkItem(patch: Partial<AzureItem>) {
    if (!member || !item) return
    const next: AzureItem = {
      ...item,
      ...patch,
    }
    const nextMember = {
      ...member,
      azureItems: member.azureItems.map((a) => (a.id === item.id ? next : a)),
    }
    dispatch({ type: 'updateTeamMember', member: nextMember })
  }

  function addLocalNote() {
    if (!member || !item) return
    const text = newNoteText.trim()
    if (!text) return
    const nowIso = new Date().toISOString()
    const nextNotes = [{ id: newId('win'), createdIso: nowIso, text }, ...(item.localNotes ?? [])]
    updateWorkItem({ localNotes: nextNotes })
    setNewNoteText('')
  }

  return (
    <div className="page">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Work Item</div>
          <div className="mutedSmall">{member?.name ?? ''}</div>
        </div>
        <div className="row" style={{ marginTop: 0 }}>
          <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}/work-items`)}>
            Back to work items
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
      ) : !item ? (
        <div className="card pad">
          <div className="muted">Work item not found.</div>
        </div>
      ) : (
        <>
          <div className="card pad">
            <div className="detailTitle">
              {item.id} — {item.title}
            </div>

            <div className="workItemDetailsGrid" style={{ marginTop: 12 }}>
              <div className="workItemField">
                <div className="workItemFieldKey">Project</div>
                <div className="workItemFieldVal">
                  <select
                    className="select"
                    value={item.projectId ?? ''}
                    onChange={(e) => {
                      const next = e.target.value || undefined
                      updateWorkItem({ projectId: next })
                    }}
                  >
                    <option value="">(None)</option>
                    {projects.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="workItemField">
                <div className="workItemFieldKey">Start date</div>
                <div className="workItemFieldVal">{formatIsoDateLong(item.startDateIso)}</div>
              </div>

              <div className="workItemField">
                <div className="workItemFieldKey">Assigned to</div>
                <div className="workItemFieldVal">{item.assignedTo ?? member.name ?? '—'}</div>
              </div>

              <div className="workItemField">
                <div className="workItemFieldKey">Time taken</div>
                <div className="workItemFieldVal">{item.timeTaken ?? '—'}</div>
              </div>

              <div className="workItemField">
                <div className="workItemFieldKey">Status</div>
                <div className="workItemFieldVal">{item.status}</div>
              </div>

              <div className="workItemField">
                <div className="workItemFieldKey">Links</div>
                <div className="workItemFieldVal">
                  {(item.ticketUrl ?? prLinks[0] ?? item.commitsUrl) ? (
                    <div className="rowTiny" style={{ alignItems: 'center' }}>
                      {item.ticketUrl ? (
                        <a className="btn btnGhost btnIcon workItemLinkPill" href={item.ticketUrl} target="_blank" rel="noreferrer">
                          Ticket
                        </a>
                      ) : null}
                      {item.commitsUrl ? (
                        <a className="btn btnGhost btnIcon workItemLinkPill" href={item.commitsUrl} target="_blank" rel="noreferrer">
                          Commits
                        </a>
                      ) : null}
                      {prLinks.map((url, idx) => (
                        <a key={`${url}-${idx}`} className="btn btnGhost btnIcon workItemLinkPill" href={url} target="_blank" rel="noreferrer">
                          PR {idx + 1}
                        </a>
                      ))}
                    </div>
                  ) : (
                    '—'
                  )}
                </div>
              </div>
            </div>
          </div>

          <section className="card pad" aria-label="Work item notes">
            <div className="cardHeader" style={{ padding: 0, borderBottom: 0, marginBottom: 10 }}>
              <div className="cardTitle">Notes (local)</div>
            </div>

            <label className="field">
              <div className="fieldLabel">Add a note</div>
              <textarea
                className="textarea"
                placeholder="Write a local note (Markdown supported)…"
                value={newNoteText}
                onChange={(e) => setNewNoteText(e.target.value)}
              />
            </label>
            <div className="row" style={{ marginTop: 10 }}>
              <button className="btn btnSecondary" type="button" onClick={addLocalNote}>
                Add note
              </button>
            </div>

            <div style={{ marginTop: 12 }}>
              {(item.localNotes ?? []).length === 0 ? (
                <div className="muted">No local notes yet.</div>
              ) : (
                <div className="list listCard">
                  {(item.localNotes ?? []).map((n) => (
                    <div key={n.id} className="listRow">
                      <div className="listMain">
                        <div className="listMeta">{new Date(n.createdIso).toLocaleString()}</div>
                        <div className="noteBody" style={{ marginTop: 8 }}>
                          <Markdown text={n.text} />
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>

          <section className="card pad" aria-label="Work item history">
            <div className="cardHeader" style={{ padding: 0, borderBottom: 0, marginBottom: 10 }}>
              <div className="cardTitle">History</div>
            </div>
            {(item.history ?? []).length === 0 ? (
              <div className="muted">No history yet.</div>
            ) : (
              <div className="list listCard">
                {(item.history ?? []).map((h) => (
                  <div key={h.id} className="listRow">
                    <div className="listMain">
                      <div className="listTitle">{h.summary}</div>
                      <div className="listMeta">
                        {new Date(h.createdIso).toLocaleString()} • {h.kind}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </>
      )}
    </div>
  )
}



