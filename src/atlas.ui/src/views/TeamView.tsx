import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamMember, TeamNote } from '../app/types'
import { useNavigate, useParams } from 'react-router-dom'
import { Markdown } from '../components/Markdown'

const TAGS: NoteTag[] = ['Blocker', 'Progress', 'Concern', 'Praise', 'Standup']

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

function startOfDay(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

function isBusinessDay(d: Date) {
  const day = d.getDay()
  return day !== 0 && day !== 6
}

// 0 = today (if business day), 1 = previous business day, etc.
function businessDaysAgo(iso: string) {
  const noteDay = startOfDay(new Date(iso))
  const cursor = startOfDay(new Date())
  if (noteDay.getTime() > cursor.getTime()) return 0

  let count = 0
  while (cursor.getTime() > noteDay.getTime()) {
    cursor.setDate(cursor.getDate() - 1)
    if (isBusinessDay(cursor)) count++
  }
  return count
}

function isCurrentTicketStatus(status: string) {
  const s = status.toLowerCase()
  return s.includes('in progress') || s.includes('blocked') || s.includes('code review') || s.includes('in review')
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
              key={selected.id}
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
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const [quickNote, setQuickNote] = useState('')
  const [tag, setTag] = useState<NoteTag>('Standup')
  const [structured, setStructured] = useState('')
  const currentTickets = useMemo(() => member.azureItems.filter((a) => isCurrentTicketStatus(a.status)), [member.azureItems])
  const recentNotes = useMemo(() => member.notes.filter((n) => businessDaysAgo(n.createdIso) < 5), [member.notes])
  const [selectedAzureId, setSelectedAzureId] = useState<string | undefined>(currentTickets[0]?.id)
  const quickNoteRef = useRef<HTMLTextAreaElement | null>(null)
  const structuredRef = useRef<HTMLTextAreaElement | null>(null)
  const editTextareaRef = useRef<HTMLTextAreaElement | null>(null)
  const noteInputMaxHeightPx = 180
  const editInputMaxHeightPx = 360

  const [editingNoteId, setEditingNoteId] = useState<string | undefined>(undefined)
  const [draftText, setDraftText] = useState('')
  const [editTab, setEditTab] = useState<'Write' | 'Preview'>('Write')

  const effectiveSelectedAzureId = useMemo(() => {
    if (selectedAzureId && currentTickets.some((t) => t.id === selectedAzureId)) return selectedAzureId
    return currentTickets[0]?.id
  }, [currentTickets, selectedAzureId])

  const selectedAzure = useMemo(
    () => currentTickets.find((a) => a.id === effectiveSelectedAzureId),
    [currentTickets, effectiveSelectedAzureId],
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

  // Auto-grow edit textarea when content changes (up to a cap).
  useLayoutEffect(() => {
    if (!editingNoteId) return
    if (editTab !== 'Write') return
    autoGrow(editTextareaRef.current, editInputMaxHeightPx)
  }, [draftText, editInputMaxHeightPx, editTab, editingNoteId])

  function applyTabIndentation(params: {
    value: string
    selectionStart: number
    selectionEnd: number
    outdent: boolean
  }) {
    const { value, selectionStart: start, selectionEnd: end, outdent } = params
    const tab = '\t'

    // No selection: insert (or remove) a single tab at the caret.
    if (start === end) {
      if (outdent) {
        // Remove a single tab immediately before the caret if present.
        const prev = value.slice(Math.max(0, start - 1), start)
        if (prev === tab) {
          const nextValue = value.slice(0, start - 1) + value.slice(start)
          return { value: nextValue, selectionStart: start - 1, selectionEnd: start - 1 }
        }
        return { value, selectionStart: start, selectionEnd: end }
      }

      const nextValue = value.slice(0, start) + tab + value.slice(end)
      const nextPos = start + tab.length
      return { value: nextValue, selectionStart: nextPos, selectionEnd: nextPos }
    }

    // Selection: indent/outdent all lines touched by the selection.
    const lineStart = value.lastIndexOf('\n', start - 1) + 1
    const endIsAtLineStart = end > 0 && value[end - 1] === '\n'
    const lineEnd = endIsAtLineStart ? end : value.indexOf('\n', end)
    const sliceEnd = lineEnd === -1 ? value.length : lineEnd

    const block = value.slice(lineStart, sliceEnd)
    const lines = block.split('\n')

    if (!outdent) {
      const indentedLines = lines.map((l) => tab + l)
      const nextBlock = indentedLines.join('\n')
      const nextValue = value.slice(0, lineStart) + nextBlock + value.slice(sliceEnd)
      const delta = tab.length
      return {
        value: nextValue,
        selectionStart: start + delta,
        selectionEnd: end + delta * lines.length,
      }
    }

    // Outdent: remove one leading tab (or two leading spaces) per line if present.
    let removedTotal = 0
    let removedFirst = 0
    const outdentedLines = lines.map((l, idx) => {
      if (l.startsWith(tab)) {
        removedTotal += 1
        if (idx === 0) removedFirst = 1
        return l.slice(1)
      }
      if (l.startsWith('  ')) {
        removedTotal += 2
        if (idx === 0) removedFirst = 2
        return l.slice(2)
      }
      return l
    })
    const nextBlock = outdentedLines.join('\n')
    const nextValue = value.slice(0, lineStart) + nextBlock + value.slice(sliceEnd)
    return {
      value: nextValue,
      selectionStart: Math.max(lineStart, start - removedFirst),
      selectionEnd: Math.max(lineStart, end - removedTotal),
    }
  }

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

      <label className="field teamCurrentFocusField">
        <div className="fieldLabel">Current Focus</div>
        <input className="input" value={member.currentFocus} onChange={(e) => update({ currentFocus: e.target.value })} />
      </label>

      <div className="subGrid">
        <section className="card subtle span2">
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

        <section className="card subtle span2">
          <div className="cardHeader">
            <div className="cardTitle">Current Tickets</div>
            <button className="btn btnGhost" onClick={() => navigate(`/team/${member.id}/tickets`)}>
              View all tickets
            </button>
          </div>

          <div className="azureGrid">
            <div className="list listCard inner">
              {currentTickets.length === 0 ? (
                <div className="muted pad">No current tickets.</div>
              ) : (
                currentTickets.map((a) => (
                  <button
                    key={a.id}
                    className={`listRow listRowBtn ${a.id === effectiveSelectedAzureId ? 'listRowActive' : ''}`}
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
                  </button>
                ))
              )}
            </div>

            <div className="card subtle inner">
              <div className="cardHeader">
                <div className="cardTitle">Item Detail Peek</div>
                <button className="btn btnSecondary" onClick={() => {}}>
                  Open in browser
                </button>
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
                </div>
              )}
            </div>
          </div>
        </section>

        <section className="card subtle span2" aria-label="Recent notes">
          <div className="cardHeader">
            <div className="cardTitle">Recent Notes</div>
            <button className="btn btnGhost" onClick={() => navigate(`/team/${member.id}/notes`)}>
              View all notes
            </button>
          </div>
          <div className="pad teamNotesHistoryBody">
            <div className="notesList">
              {recentNotes.length === 0 ? (
                <div className="muted">No notes in the last 5 business days.</div>
              ) : (
                recentNotes.map((n) => (
                  <div key={n.id} className="noteRow">
                    <div className="noteMeta">
                      <span className={`chip chipTag chipTag-${n.tag.toLowerCase()}`}>{n.tag}</span>
                      <span className="mutedSmall">{new Date(n.createdIso).toLocaleDateString()}</span>
                      {n.adoWorkItemId ? <span className="chip chipGhost">ADO: {n.adoWorkItemId}</span> : null}
                      {n.prUrl ? <span className="chip chipGhost">PR</span> : null}
                      <div className="noteActions">
                        {editingNoteId === n.id ? (
                          <>
                            <button
                              className="btn btnSecondary"
                              onClick={() => {
                                const nextNotes = member.notes.map((x) => (x.id === n.id ? { ...x, text: draftText } : x))
                                update({ notes: nextNotes })
                                setEditingNoteId(undefined)
                                setDraftText('')
                                setEditTab('Write')
                              }}
                            >
                              Save
                            </button>
                            <button
                              className="btn btnGhost"
                              onClick={() => {
                                setEditingNoteId(undefined)
                                setDraftText('')
                                setEditTab('Write')
                              }}
                            >
                              Cancel
                            </button>
                          </>
                        ) : (
                          <button
                            className="btn btnGhost"
                            onClick={() => {
                              setEditingNoteId(n.id)
                              setDraftText(n.text)
                              setEditTab('Write')
                            }}
                          >
                            Edit
                          </button>
                        )}
                      </div>
                    </div>
                    {editingNoteId === n.id ? (
                      <div className="noteEdit">
                        <div className="noteEditTabs">
                          <button
                            className={`btn btnGhost ${editTab === 'Write' ? 'noteEditTabActive' : ''}`}
                            onClick={() => setEditTab('Write')}
                          >
                            Write
                          </button>
                          <button
                            className={`btn btnGhost ${editTab === 'Preview' ? 'noteEditTabActive' : ''}`}
                            onClick={() => setEditTab('Preview')}
                          >
                            Preview
                          </button>
                        </div>

                        {editTab === 'Write' ? (
                          <textarea
                            ref={editTextareaRef}
                            className="textarea textareaAutoGrow noteEditTextarea"
                            value={draftText}
                            onKeyDown={(e) => {
                              if (e.key !== 'Tab') return
                              e.preventDefault()

                              const el = e.currentTarget
                              const start = el.selectionStart ?? 0
                              const end = el.selectionEnd ?? 0
                              const next = applyTabIndentation({
                                value: draftText,
                                selectionStart: start,
                                selectionEnd: end,
                                outdent: e.shiftKey,
                              })
                              setDraftText(next.value)

                              // Restore selection after React updates the controlled value.
                              requestAnimationFrame(() => {
                                const ta = editTextareaRef.current
                                if (!ta) return
                                ta.selectionStart = next.selectionStart
                                ta.selectionEnd = next.selectionEnd
                              })
                            }}
                            onChange={(e) => setDraftText(e.target.value)}
                          />
                        ) : (
                          <div className="noteText noteBody">
                            <Markdown text={draftText} />
                          </div>
                        )}
                      </div>
                    ) : (
                      <div className="noteText noteBody">
                        <Markdown text={n.text} />
                      </div>
                    )}
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


