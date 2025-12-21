import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamMember, TeamNote } from '../app/types'
import { useNavigate, useParams } from 'react-router-dom'

const TAGS: NoteTag[] = ['Blocker', 'Progress', 'Concern', 'Praise', 'Standup']

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

export function TeamView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId } = useParams<{ memberId?: string }>()
  const { team, selectedTeamMemberId } = useAppState()
  const selected = useSelectedTeamMember()
  const isFocusMode = !!memberId

  useEffect(() => {
    ai.setContext('Context: Team', [
      { id: 'summarize-patterns', label: 'Summarize patterns (frequent blockers)' },
      { id: 'growth-areas', label: 'Highlight growth areas' },
      { id: 'cite-notes', label: 'Cite specific notes (draft)' },
    ])
  }, [ai.setContext])

  useEffect(() => {
    if (!memberId) return
    dispatch({ type: 'selectTeamMember', memberId })
  }, [dispatch, memberId])

  // If we entered focus mode with an unknown ID, fall back to list view.
  useEffect(() => {
    if (!memberId) return
    const exists = team.some((m) => m.id === memberId)
    if (!exists) navigate('/team', { replace: true })
  }, [memberId, navigate, team])

  return (
    <div className="page">
      <h2 className="pageTitle">Team</h2>

      <div className={`teamGrid ${isFocusMode ? 'teamGridFocus' : ''}`}>
        {!isFocusMode ? (
          <section className="pane paneTeamLeft" aria-label="Team member list">
            <div className="list listCard">
              {team.map((m) => (
                <button
                  key={m.id}
                  className={`listRow listRowBtn ${m.id === selectedTeamMemberId ? 'listRowActive' : ''}`}
                  onClick={() => dispatch({ type: 'selectTeamMember', memberId: m.id })}
                  onDoubleClick={() => navigate(`/team/${m.id}`)}
                >
                  <div className="avatar" aria-hidden="true" />
                  <div className="listMain">
                    <div className="listTitle">{m.name}</div>
                    <div className="listMeta">{m.role ?? '—'}</div>
                  </div>
                  <span className={`dot dot-${m.statusDot.toLowerCase()}`} aria-label={`${m.statusDot} status`} />
                </button>
              ))}
            </div>
          </section>
        ) : null}

        <section className="pane paneTeamCenter" aria-label="Member detail">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a team member.</div>
            </div>
          ) : (
            <MemberDetail
              member={selected}
              isFocusMode={isFocusMode}
              onEnterFocus={() => navigate(`/team/${selected.id}`)}
              onExitFocus={() => navigate('/team')}
            />
          )}
        </section>
      </div>
    </div>
  )
}

function MemberDetail({
  member,
  isFocusMode,
  onEnterFocus,
  onExitFocus,
}: {
  member: TeamMember
  isFocusMode: boolean
  onEnterFocus: () => void
  onExitFocus: () => void
}) {
  const dispatch = useAppDispatch()
  const [quickNote, setQuickNote] = useState('')
  const [tag, setTag] = useState<NoteTag>('Standup')
  const [structured, setStructured] = useState('')
  const [selectedAzureId, setSelectedAzureId] = useState<string | undefined>(member.azureItems[0]?.id)
  const quickNoteRef = useRef<HTMLTextAreaElement | null>(null)
  const structuredRef = useRef<HTMLTextAreaElement | null>(null)
  const noteInputMaxHeightPx = 180

  const selectedAzure = useMemo(
    () => member.azureItems.find((a) => a.id === selectedAzureId),
    [member.azureItems, selectedAzureId],
  )

  function update(patch: Partial<TeamMember>) {
    dispatch({ type: 'updateTeamMember', member: { ...member, ...patch } })
  }

  function addNote(note: TeamNote) {
    update({ notes: [note, ...member.notes] })
  }

  function autoGrow(el: HTMLTextAreaElement | null, capPx: number) {
    if (!el) return

    // Reset height so scrollHeight reflects the full content.
    el.style.height = 'auto'
    const next = Math.min(el.scrollHeight, capPx)
    el.style.height = `${next}px`
    el.style.overflowY = el.scrollHeight > capPx ? 'auto' : 'hidden'
  }

  // Auto-grow note inputs until a cap, then scroll.
  useLayoutEffect(() => {
    autoGrow(quickNoteRef.current, noteInputMaxHeightPx)
  }, [noteInputMaxHeightPx, quickNote])

  useLayoutEffect(() => {
    autoGrow(structuredRef.current, noteInputMaxHeightPx)
  }, [noteInputMaxHeightPx, structured])

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">{member.name}</div>
          <div className="mutedSmall">{member.role ?? ''}</div>
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

      <label className="field">
        <div className="fieldLabel">Current Focus</div>
        <input className="input" value={member.currentFocus} onChange={(e) => update({ currentFocus: e.target.value })} />
      </label>

      <div className="subGrid">
        <section className="card subtle">
          <div className="cardHeader">
            <div className="cardTitle">Performance Notes</div>
          </div>

          <div className="pad teamNotesComposer">
            <div className="field">
              <div className="fieldLabel">Quick note</div>
              <textarea
                ref={quickNoteRef}
                className="textarea textareaAutoGrow textareaAutoGrowSmall"
                placeholder="Quick note…"
                value={quickNote}
                onChange={(e) => setQuickNote(e.target.value)}
              />
              <div className="row">
                <button
                  className="btn btnWide"
                  onClick={() => {
                    if (!quickNote.trim()) return
                    addNote({
                      id: newId('note'),
                      createdIso: new Date().toISOString(),
                      tag: 'Quick',
                      text: quickNote.trim(),
                    })
                    setQuickNote('')
                  }}
                >
                  Add
                </button>
              </div>
            </div>

            <div className="field teamNotesStructuredField">
              <div className="fieldLabel">Structured note</div>
              <textarea
                ref={structuredRef}
                className="textarea textareaAutoGrow textareaAutoGrowSmall"
                placeholder="Structured note…"
                value={structured}
                onChange={(e) => setStructured(e.target.value)}
              />
              <div className="row">
                <button
                  className="btn btnWide"
                  onClick={() => {
                    if (!structured.trim()) return
                    addNote({
                      id: newId('note'),
                      createdIso: new Date().toISOString(),
                      tag,
                      text: structured.trim(),
                      adoWorkItemId: '(placeholder)',
                      prUrl: '(placeholder)',
                    })
                    setStructured('')
                  }}
                >
                  Add
                </button>
                <select className="select" value={tag} onChange={(e) => setTag(e.target.value as NoteTag)}>
                  {TAGS.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>
        </section>

        <section className="card subtle">
          <div className="cardHeader">
            <div className="cardTitle">Azure DevOps Items</div>
          </div>

          <div className="azureGrid">
            <div className="list listCard inner">
              {member.azureItems.length === 0 ? (
                <div className="muted pad">No Azure items (mock).</div>
              ) : (
                member.azureItems.map((a) => (
                  <button
                    key={a.id}
                    className={`listRow listRowBtn ${a.id === selectedAzureId ? 'listRowActive' : ''}`}
                    onClick={() => setSelectedAzureId(a.id)}
                  >
                    <div className="listMain">
                      <div className="listTitle">
                        {a.id} — {a.title}
                      </div>
                      <div className="listMeta">
                        {a.status}
                        {a.timeTaken ? ` • ${a.timeTaken}` : ''}
                      </div>
                    </div>
                    <div className="rowTiny">
                      <span className="chip chipGhost">Ticket</span>
                      <span className="chip chipGhost">PR</span>
                      <span className="chip chipGhost">Commits</span>
                    </div>
                  </button>
                ))
              )}
            </div>

            <div className="card subtle inner">
              <div className="cardHeader">
                <div className="cardTitle">Item Detail Peek</div>
              </div>
              {!selectedAzure ? (
                <div className="muted pad">Select an item.</div>
              ) : (
                <div className="pad">
                  <div className="detailTitle">{selectedAzure.title}</div>
                  <div className="mutedSmall">{selectedAzure.id}</div>
                  <div className="kv">
                    <div className="kvRow">
                      <div className="kvKey">Status</div>
                      <div className="kvVal">{selectedAzure.status}</div>
                    </div>
                    <div className="kvRow">
                      <div className="kvKey">Time taken</div>
                      <div className="kvVal">{selectedAzure.timeTaken ?? '—'}</div>
                    </div>
                    <div className="kvRow">
                      <div className="kvKey">Links</div>
                      <div className="kvVal">Ticket / PR / Git history (placeholders)</div>
                    </div>
                  </div>
                  <button className="btn btnSecondary" onClick={() => {}}>
                    Open in browser
                  </button>
                </div>
              )}
            </div>
          </div>
        </section>

        <section className="card subtle span2" aria-label="Notes history">
          <div className="cardHeader">
            <div className="cardTitle">Notes History</div>
          </div>
          <div className="pad">
            <div className="notesList">
              {member.notes.length === 0 ? (
                <div className="muted">No notes yet.</div>
              ) : (
                member.notes.map((n) => (
                  <div key={n.id} className="noteRow">
                    <div className="noteMeta">
                      <span className={`chip chipTag chipTag-${n.tag.toLowerCase()}`}>{n.tag}</span>
                      <span className="mutedSmall">{new Date(n.createdIso).toLocaleDateString()}</span>
                      {n.adoWorkItemId ? <span className="chip chipGhost">ADO: {n.adoWorkItemId}</span> : null}
                      {n.prUrl ? <span className="chip chipGhost">PR</span> : null}
                    </div>
                    <div className="noteText">{n.text}</div>
                  </div>
                ))
              )}
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}


