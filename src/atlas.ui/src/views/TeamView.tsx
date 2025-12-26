import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, Risk, TeamMember, TeamNote } from '../app/types'
import { NavLink, useLocation, useNavigate, useParams } from 'react-router-dom'
import { Markdown } from '../components/Markdown'
import { Modal } from '../components/Modal'

const TAGS: NoteTag[] = ['Blocker', 'Progress', 'Concern', 'Praise', 'Standup']
const FILTER_TAGS: Array<NoteTag | 'All'> = ['All', 'Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

type MemberTab = 'overview' | 'notes' | 'work-items' | 'risks' | 'growth'

function getActiveTab(pathname: string): MemberTab {
  if (pathname.includes('/notes')) return 'notes'
  if (pathname.includes('/work-items')) return 'work-items'
  if (pathname.includes('/risks')) return 'risks'
  if (pathname.includes('/growth')) return 'growth'
  return 'overview'
}

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
  const location = useLocation()
  const { memberId } = useParams<{ memberId?: string }>()
  const { team, selectedTeamMemberId } = useAppState()
  const selected = useSelectedTeamMember()
  const isFocusMode = !!memberId
  const routeTab = useMemo(() => getActiveTab(location.pathname), [location.pathname])
  const [localTab, setLocalTab] = useState<MemberTab>('overview')
  const activeTab = isFocusMode ? routeTab : localTab

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
              activeTab={activeTab}
              setActiveTab={isFocusMode ? undefined : setLocalTab}
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
  activeTab,
  setActiveTab,
  onEnterFocus,
  onExitFocus,
}: {
  member: TeamMember
  isFocusMode: boolean
  activeTab: MemberTab
  setActiveTab?: (t: MemberTab) => void
  onEnterFocus: () => void
  onExitFocus: () => void
}) {
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { risks } = useAppState()
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

      <div className="tabsBar" role="tablist" aria-label="Member tabs">
        {isFocusMode ? (
          <>
            <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${member.id}`}>
              Overview
            </NavLink>
            <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${member.id}/notes`}>
              Notes
            </NavLink>
            <NavLink
              className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`}
              to={`/team/${member.id}/work-items`}
            >
              Work Items
            </NavLink>
            <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${member.id}/risks`}>
              Risks
            </NavLink>
            <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${member.id}/growth`}>
              Growth
            </NavLink>
          </>
        ) : (
          <>
            <button
              className={`tabBtn ${activeTab === 'overview' ? 'tabBtnActive' : ''}`}
              onClick={() => setActiveTab?.('overview')}
              type="button"
            >
              Overview
            </button>
            <button
              className={`tabBtn ${activeTab === 'notes' ? 'tabBtnActive' : ''}`}
              onClick={() => setActiveTab?.('notes')}
              type="button"
            >
              Notes
            </button>
            <button
              className={`tabBtn ${activeTab === 'work-items' ? 'tabBtnActive' : ''}`}
              onClick={() => setActiveTab?.('work-items')}
              type="button"
            >
              Work Items
            </button>
            <button
              className={`tabBtn ${activeTab === 'risks' ? 'tabBtnActive' : ''}`}
              onClick={() => setActiveTab?.('risks')}
              type="button"
            >
              Risks
            </button>
            <button
              className={`tabBtn ${activeTab === 'growth' ? 'tabBtnActive' : ''}`}
              onClick={() => setActiveTab?.('growth')}
              type="button"
            >
              Growth
            </button>
          </>
        )}
      </div>

      {activeTab === 'overview' ? (
        <>
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
                <button className="btn btnGhost" onClick={() => navigate(`/team/${member.id}/work-items`)}>
                  View all work items
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
        </>
      ) : null}

      {activeTab === 'notes' ? <MemberNotesTab member={member} tags={FILTER_TAGS} /> : null}
      {activeTab === 'work-items' ? <MemberWorkItemsTab member={member} /> : null}
      {activeTab === 'risks' ? <MemberRisksTab memberId={member.id} risks={risks} /> : null}
      {activeTab === 'growth' ? <MemberGrowthTab member={member} /> : null}
    </div>
  )
}

function stripMarkdownHeadings(line: string) {
  return line.replace(/^#{1,6}\s+/, '').trim()
}

function getDerivedTitle(note: TeamNote) {
  const firstNonEmpty = note.text.split('\n').map((l) => l.trim()).find((l) => l.length > 0) ?? '(untitled)'
  return stripMarkdownHeadings(firstNonEmpty)
}

function getPreview(note: TeamNote) {
  const text = note.text.replace(/\s+/g, ' ').trim()
  if (text.length <= 180) return text
  return text.slice(0, 180).trim() + '…'
}

function isActionItemNote(note: TeamNote) {
  const t = note.text.toLowerCase()
  return t.includes('- [ ]') || t.includes('action item') || t.includes('next:') || t.includes('todo')
}

function isOneOnOneNote(note: TeamNote) {
  const t = note.text.toLowerCase()
  return t.includes('1:1') || t.includes('one-on-one') || t.includes('one on one')
}

function isRiskNote(note: TeamNote) {
  if (note.tag === 'Concern' || note.tag === 'Blocker') return true
  const t = note.text.toLowerCase()
  return t.includes('risk') || t.includes('mitigation') || t.includes('watchout') || t.includes('watch-outs')
}

function MemberNotesTab({ member, tags }: { member: TeamMember; tags: Array<NoteTag | 'All'> }) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [tagFilter, setTagFilter] = useState<NoteTag | 'All'>('All')
  const [sortBy, setSortBy] = useState<'Newest' | 'Oldest'>('Newest')
  const [quickFilter, setQuickFilter] = useState<'All' | 'ThisWeek' | 'ActionItems' | 'OneOnOne' | 'Risks'>('All')

  const [selectedNoteId, setSelectedNoteId] = useState<string | undefined>(undefined)
  const [isNewOpen, setIsNewOpen] = useState(false)
  const [newTag, setNewTag] = useState<NoteTag>('Quick')
  const [newText, setNewText] = useState('')

  const [isEditOpen, setIsEditOpen] = useState(false)
  const [editTab, setEditTab] = useState<'Write' | 'Preview'>('Write')
  const [draftText, setDraftText] = useState('')
  const editTextareaRef = useRef<HTMLTextAreaElement | null>(null)
  const editInputMaxHeightPx = 360

  function updateNotes(nextNotes: TeamNote[]) {
    dispatch({ type: 'updateTeamMember', member: { ...member, notes: nextNotes } })
  }

  const selectedNote = useMemo(() => member.notes.find((n) => n.id === selectedNoteId), [member.notes, selectedNoteId])

  useLayoutEffect(() => {
    if (!isEditOpen) return
    if (editTab !== 'Write') return
    const el = editTextareaRef.current
    if (!el) return
    el.style.height = 'auto'
    const next = Math.min(el.scrollHeight, editInputMaxHeightPx)
    el.style.height = `${next}px`
    el.style.overflowY = el.scrollHeight > editInputMaxHeightPx ? 'auto' : 'hidden'
  }, [draftText, editInputMaxHeightPx, editTab, isEditOpen])

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

  const filteredSorted = useMemo(() => {
    const q = query.trim().toLowerCase()
    const now = Date.now()
    const weekMs = 7 * 24 * 60 * 60 * 1000

    function matchesText(n: TeamNote) {
      if (!q) return true
      const hay = [
        n.tag,
        getDerivedTitle(n),
        n.text,
        n.adoWorkItemId ?? '',
        n.prUrl ?? '',
        new Date(n.createdIso).toLocaleDateString(),
      ]
        .join(' ')
        .toLowerCase()
      return hay.includes(q)
    }

    function matchesTag(n: TeamNote) {
      if (tagFilter === 'All') return true
      return n.tag === tagFilter
    }

    function matchesQuick(n: TeamNote) {
      if (quickFilter === 'All') return true
      if (quickFilter === 'ThisWeek') return now - new Date(n.createdIso).getTime() <= weekMs
      if (quickFilter === 'ActionItems') return isActionItemNote(n)
      if (quickFilter === 'OneOnOne') return isOneOnOneNote(n)
      if (quickFilter === 'Risks') return isRiskNote(n)
      return true
    }

    return member.notes
      .filter((n) => matchesTag(n) && matchesQuick(n) && matchesText(n))
      .slice()
      .sort((a, b) => {
        const at = new Date(a.createdIso).getTime()
        const bt = new Date(b.createdIso).getTime()
        return sortBy === 'Newest' ? bt - at : at - bt
      })
  }, [member.notes, query, quickFilter, sortBy, tagFilter])

  return (
    <section className="card subtle" aria-label="Notes tab">
      <div className="cardHeader">
        <div className="cardTitle">Notes</div>
        <div className="row" style={{ marginTop: 0 }}>
          <button
            className="btn"
            onClick={() => {
              setIsNewOpen(true)
              setNewTag('Quick')
              setNewText('')
            }}
          >
            + New
          </button>
        </div>
      </div>

      <div className="pad">
        <div className="tasksFiltersRow" aria-label="Notes filters">
          <label className="field" style={{ gridColumn: '1 / span 2' }}>
            <div className="fieldLabel">Search</div>
            <input
              className="input"
              placeholder="Search notes by title, tag, person, or text…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </label>

          <label className="field">
            <div className="fieldLabel">Tag</div>
            <select className="select" value={tagFilter} onChange={(e) => setTagFilter(e.target.value as NoteTag | 'All')}>
              {tags.map((t) => (
                <option key={t} value={t}>
                  {t === 'All' ? 'Any tag' : t}
                </option>
              ))}
            </select>
          </label>

          <label className="field">
            <div className="fieldLabel">Sort</div>
            <select className="select" value={sortBy} onChange={(e) => setSortBy(e.target.value as 'Newest' | 'Oldest')}>
              <option value="Newest">Newest first</option>
              <option value="Oldest">Oldest first</option>
            </select>
          </label>
        </div>

        <div className="chipRow" aria-label="Notes quick filters">
          <button className={`chipBtn ${quickFilter === 'All' ? 'chipBtnActive' : ''}`} onClick={() => setQuickFilter('All')}>
            All
          </button>
          <button
            className={`chipBtn ${quickFilter === 'ThisWeek' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('ThisWeek')}
          >
            This Week
          </button>
          <button
            className={`chipBtn ${quickFilter === 'ActionItems' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('ActionItems')}
          >
            Action Items
          </button>
          <button
            className={`chipBtn ${quickFilter === 'OneOnOne' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('OneOnOne')}
          >
            1:1
          </button>
          <button
            className={`chipBtn ${quickFilter === 'Risks' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('Risks')}
          >
            Risks
          </button>
        </div>

        <div className="list listCard">
          {filteredSorted.length === 0 ? (
            <div className="muted pad">No notes match your filters.</div>
          ) : (
            filteredSorted.map((n) => (
              <button
                key={n.id}
                className="listRow listRowBtn"
                onClick={() => {
                  setSelectedNoteId(n.id)
                  setIsEditOpen(false)
                  setDraftText('')
                  setEditTab('Write')
                }}
              >
                <div className="listMain">
                  <div className="listTitle">{getDerivedTitle(n)}</div>
                  <div className="listMeta">
                    {new Date(n.createdIso).toLocaleDateString()} • {n.tag}
                    {n.adoWorkItemId ? ` • ADO: ${n.adoWorkItemId}` : ''}
                  </div>
                  <div className="listMeta listMetaWrap listNotesPreview">{getPreview(n)}</div>
                </div>
              </button>
            ))
          )}
        </div>
      </div>

      <Modal
        title={selectedNote ? getDerivedTitle(selectedNote) : 'Note'}
        isOpen={!!selectedNote}
        onClose={() => {
          setSelectedNoteId(undefined)
          setIsEditOpen(false)
          setDraftText('')
          setEditTab('Write')
        }}
        footer={
          selectedNote ? (
            <div className="row" style={{ marginTop: 0 }}>
              {isEditOpen ? (
                <>
                  <button
                    className="btn btnSecondary"
                    onClick={() => {
                      const nextNotes = member.notes.map((x) => (x.id === selectedNote.id ? { ...x, text: draftText } : x))
                      updateNotes(nextNotes)
                      setIsEditOpen(false)
                    }}
                  >
                    Save
                  </button>
                  <button
                    className="btn btnGhost"
                    onClick={() => {
                      setIsEditOpen(false)
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
                    setIsEditOpen(true)
                    setDraftText(selectedNote.text)
                    setEditTab('Write')
                  }}
                >
                  Edit
                </button>
              )}
              <button className="btn btnGhost" onClick={() => navigate(`/team/${member.id}/notes/${selectedNote.id}`)}>
                Open full page
              </button>
            </div>
          ) : null
        }
      >
        {!selectedNote ? null : (
          <>
            <div className="noteMeta">
              <span className={`chip chipTag chipTag-${selectedNote.tag.toLowerCase()}`}>{selectedNote.tag}</span>
              <span className="mutedSmall">{new Date(selectedNote.createdIso).toLocaleString()}</span>
              {selectedNote.adoWorkItemId ? <span className="chip chipGhost">ADO: {selectedNote.adoWorkItemId}</span> : null}
              {selectedNote.prUrl ? <span className="chip chipGhost">PR</span> : null}
            </div>

            {isEditOpen ? (
              <div className="noteEdit">
                <div className="noteEditTabs">
                  <button className={`btn btnGhost ${editTab === 'Write' ? 'noteEditTabActive' : ''}`} onClick={() => setEditTab('Write')}>
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
                <Markdown text={selectedNote.text} />
              </div>
            )}
          </>
        )}
      </Modal>

      <Modal
        title="New note"
        isOpen={isNewOpen}
        onClose={() => {
          setIsNewOpen(false)
          setNewText('')
        }}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button
              className="btn btnSecondary"
              onClick={() => {
                if (!newText.trim()) return
                const next: TeamNote = {
                  id: newId('note'),
                  createdIso: new Date().toISOString(),
                  tag: newTag,
                  text: newText.trim(),
                }
                updateNotes([next, ...member.notes])
                setIsNewOpen(false)
                setNewText('')
              }}
            >
              Create
            </button>
            <button
              className="btn btnGhost"
              onClick={() => {
                setIsNewOpen(false)
                setNewText('')
              }}
            >
              Cancel
            </button>
          </div>
        }
      >
        <div className="fieldGrid">
          <label className="field">
            <div className="fieldLabel">Tag</div>
            <select className="select" value={newTag} onChange={(e) => setNewTag(e.target.value as NoteTag)}>
              {FILTER_TAGS.filter((t) => t !== 'All').map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <div className="fieldLabel">Note</div>
            <textarea className="textarea" value={newText} onChange={(e) => setNewText(e.target.value)} placeholder="Write your note…" />
          </label>
        </div>
      </Modal>
    </section>
  )
}

function MemberWorkItemsTab({ member }: { member: TeamMember }) {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | undefined>(undefined)

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return member.azureItems
    return member.azureItems.filter((a) => `${a.id} ${a.title} ${a.status}`.toLowerCase().includes(q))
  }, [member.azureItems, query])

  const selected = useMemo(() => member.azureItems.find((a) => a.id === selectedId), [member.azureItems, selectedId])

  return (
    <section className="card subtle" aria-label="Work items tab">
      <div className="cardHeader">
        <div className="cardTitle">Work Items</div>
      </div>

      <div className="pad">
        <div className="tasksFiltersRow" aria-label="Work items filters">
          <label className="field" style={{ gridColumn: '1 / span 3' }}>
            <div className="fieldLabel">Search</div>
            <input
              className="input"
              placeholder="Search by id, title, or status…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </label>
        </div>

        <div className="list listCard">
          {filtered.length === 0 ? (
            <div className="muted pad">No work items match your search.</div>
          ) : (
            filtered.map((a) => (
              <button
                key={a.id}
                className="listRow listRowBtn"
                onClick={() => {
                  setSelectedId(a.id)
                }}
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
      </div>

      <Modal
        title={selected ? `${selected.id} — ${selected.title}` : 'Work item'}
        isOpen={!!selected}
        onClose={() => setSelectedId(undefined)}
        footer={
          selected ? (
            <div className="row" style={{ marginTop: 0 }}>
              <button className="btn btnGhost" onClick={() => navigate(`/team/${member.id}/work-items/${selected.id}`)}>
                Open full page
              </button>
            </div>
          ) : null
        }
      >
        {!selected ? null : (
          <div className="kv">
            <div className="kvRow">
              <div className="kvKey">Status</div>
              <div className="kvVal">{selected.status}</div>
            </div>
            <div className="kvRow">
              <div className="kvKey">Time taken</div>
              <div className="kvVal">{selected.timeTaken ?? '—'}</div>
            </div>
            <div className="kvRow">
              <div className="kvKey">Links</div>
              <div className="kvVal">
                {(selected.ticketUrl ?? selected.prUrl ?? selected.commitsUrl) ? (
                  <>
                    {selected.ticketUrl ? (
                      <div>
                        <a href={selected.ticketUrl} target="_blank" rel="noreferrer">
                          Ticket
                        </a>
                      </div>
                    ) : null}
                    {selected.prUrl ? (
                      <div>
                        <a href={selected.prUrl} target="_blank" rel="noreferrer">
                          PR
                        </a>
                      </div>
                    ) : null}
                    {selected.commitsUrl ? (
                      <div>
                        <a href={selected.commitsUrl} target="_blank" rel="noreferrer">
                          Commits
                        </a>
                      </div>
                    ) : null}
                  </>
                ) : (
                  '—'
                )}
              </div>
            </div>
          </div>
        )}
      </Modal>
    </section>
  )
}

function MemberRisksTab({ memberId, risks }: { memberId: string; risks: Risk[] }) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  const linked = useMemo(() => risks.filter((r) => r.linkedTeamMemberIds.includes(memberId)), [memberId, risks])

  return (
    <section className="card subtle" aria-label="Risks tab">
      <div className="cardHeader">
        <div className="cardTitle">Risks</div>
        <button className="btn btnGhost" onClick={() => navigate('/risks')}>
          View all risks
        </button>
      </div>
      <div className="pad">
        {linked.length === 0 ? (
          <div className="muted">No linked risks yet.</div>
        ) : (
          <div className="list listCard">
            {linked.map((r) => (
              <button
                key={r.id}
                className="listRow listRowBtn"
                onClick={() => {
                  dispatch({ type: 'selectRisk', riskId: r.id })
                  navigate('/risks')
                }}
              >
                <span className={`dot dot-${r.severity.toLowerCase()}`} aria-hidden="true" />
                <div className="listMain">
                  <div className="listTitle">{r.title}</div>
                  <div className="listMeta">{r.status}</div>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </section>
  )
}

function MemberGrowthTab({ member }: { member: TeamMember }) {
  const praise = useMemo(() => member.notes.filter((n) => n.tag === 'Praise').slice(0, 5), [member.notes])
  const concerns = useMemo(
    () => member.notes.filter((n) => n.tag === 'Concern' || n.tag === 'Blocker').slice(0, 5),
    [member.notes],
  )

  return (
    <section className="card subtle" aria-label="Growth tab">
      <div className="cardHeader">
        <div className="cardTitle">Growth (draft)</div>
      </div>
      <div className="pad">
        <div className="fieldGrid2">
          <div className="field">
            <div className="fieldLabel">Strengths / praise signals</div>
            <div className="placeholderBox">
              {praise.length === 0 ? 'No praise notes yet.' : praise.map((n) => `• ${new Date(n.createdIso).toLocaleDateString()}: ${getDerivedTitle(n)}`).join('\n')}
            </div>
          </div>
          <div className="field">
            <div className="fieldLabel">Risks / friction signals</div>
            <div className="placeholderBox">
              {concerns.length === 0
                ? 'No concern/blocker notes yet.'
                : concerns.map((n) => `• ${new Date(n.createdIso).toLocaleDateString()}: ${getDerivedTitle(n)}`).join('\n')}
            </div>
          </div>
          <div className="field span2">
            <div className="fieldLabel">Growth plan / experiments (draft)</div>
            <div className="placeholderBox">Add a lightweight plan here later (e.g. goals, experiments, check-ins).</div>
          </div>
        </div>
      </div>
    </section>
  )
}


