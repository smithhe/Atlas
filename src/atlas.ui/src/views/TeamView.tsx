import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useGrowthForMember, useSelectedTeamMember } from '../app/state/AppState'
import type { Growth, GrowthFeedbackTheme, GrowthGoalStatus, NoteTag, Risk, TeamMember, TeamMemberRisk, TeamNote } from '../app/types'
import { isCurrentTicketStatus } from '../app/team'
import { Link, NavLink, useLocation, useNavigate, useParams } from 'react-router-dom'
import { Markdown } from '../components/Markdown'
import { Modal } from '../components/Modal'
import { MemberOverviewTab } from './team/MemberOverviewTab'

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

function memberTabPath(memberId: string, tab: MemberTab) {
  switch (tab) {
    case 'overview':
      return `/team/${memberId}`
    case 'notes':
      return `/team/${memberId}/notes`
    case 'work-items':
      return `/team/${memberId}/work-items`
    case 'risks':
      return `/team/${memberId}/risks`
    case 'growth':
      return `/team/${memberId}/growth`
  }
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
  const memberName = useMemo(() => {
    if (!memberId) return undefined
    return selected?.name ?? team.find((m) => m.id === memberId)?.name ?? memberId
  }, [memberId, selected?.name, team])

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
      {isFocusMode && memberId ? (
        <nav className="pageBreadcrumbs" aria-label="Breadcrumbs">
          <Link className="crumbLink" to="/team">
            Team
          </Link>
          <span className="crumbSep" aria-hidden="true">
            /
          </span>
          {activeTab === 'overview' ? (
            <span className="crumbCurrent">{memberName ?? memberId}</span>
          ) : (
            <Link className="crumbLink" to={`/team/${memberId}`}>
              {memberName ?? memberId}
            </Link>
          )}
          {activeTab !== 'overview' ? (
            <>
              <span className="crumbSep" aria-hidden="true">
                /
              </span>
              <span className="crumbCurrent">
                {activeTab === 'work-items'
                  ? 'Work Items'
                  : activeTab === 'notes'
                    ? 'Notes'
                    : activeTab === 'risks'
                      ? 'Risks'
                      : activeTab === 'growth'
                        ? 'Growth'
                        : 'Overview'}
              </span>
            </>
          ) : null}
        </nav>
      ) : null}

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
              onEnterFocus={() => navigate(memberTabPath(selected.id, activeTab))}
              onExitFocus={() => {
                // Preserve the current focus-mode tab when returning to list mode.
                setLocalTab(routeTab)
                navigate('/team')
              }}
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
  const { risks, teamMemberRisks } = useAppState()

  function update(patch: Partial<TeamMember>) {
    dispatch({ type: 'updateTeamMember', member: { ...member, ...patch } })
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
            <NavLink className={({ isActive }) => `tabBtn ${isActive ? 'tabBtnActive' : ''}`} to={`/team/${member.id}`} end>
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
        <MemberOverviewTab
          member={member}
          onUpdate={update}
          onGoToNotes={() => navigate(`/team/${member.id}/notes`)}
          onGoToWorkItem={(workItemId) => navigate(`/team/${member.id}/work-items/${workItemId}`)}
        />
      ) : null}

      {activeTab === 'notes' ? <MemberNotesTab member={member} tags={FILTER_TAGS} /> : null}
      {activeTab === 'work-items' ? <MemberWorkItemsTab member={member} /> : null}
      {activeTab === 'risks' ? <MemberRisksTab memberId={member.id} teamMemberRisks={teamMemberRisks} risks={risks} /> : null}
      {activeTab === 'growth' ? <MemberGrowthTab member={member} /> : null}
    </div>
  )
}

function stripMarkdownHeadings(line: string) {
  return line.replace(/^#{1,6}\s+/, '').trim()
}

function getDerivedTitle(note: TeamNote) {
  const explicit = note.title?.trim()
  if (explicit) return explicit
  const firstNonEmpty = note.text.split('\n').map((l) => l.trim()).find((l) => l.length > 0) ?? '(untitled)'
  return stripMarkdownHeadings(firstNonEmpty)
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

  const [expandedNoteId, setExpandedNoteId] = useState<string | undefined>(undefined)
  const [selectedNoteId, setSelectedNoteId] = useState<string | undefined>(undefined)
  const [isNewOpen, setIsNewOpen] = useState(false)
  const [newTag, setNewTag] = useState<NoteTag>('Quick')
  const [newTitle, setNewTitle] = useState('')
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
        n.title ?? '',
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
              setNewTitle('')
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

        <div className="chipRow memberNotesQuickFilters" aria-label="Notes quick filters">
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

        <div className="list listCard memberNotesList">
          {filteredSorted.length === 0 ? (
            <div className="muted pad">No notes match your filters.</div>
          ) : (
            filteredSorted.map((n) => (
              <div
                key={n.id}
                className={`listRow memberNotesRow memberNotesRowBtn ${expandedNoteId === n.id ? 'memberNotesRowExpanded' : ''}`}
                role="button"
                tabIndex={0}
                aria-expanded={expandedNoteId === n.id}
                onClick={() => {
                  setExpandedNoteId((prev) => (prev === n.id ? undefined : n.id))
                }}
                onDoubleClick={() => {
                  navigate(`/team/${member.id}/notes/${n.id}`)
                }}
                onKeyDown={(e) => {
                  if (e.key !== 'Enter' && e.key !== ' ') return
                  e.preventDefault()
                  setExpandedNoteId((prev) => (prev === n.id ? undefined : n.id))
                }}
              >
                <div className="listMain">
                  <div className="memberNotesHeaderRow">
                    <div className="memberNotesHeaderLeft">
                      <div className="listTitle memberNotesTitle">{getDerivedTitle(n)}</div>
                      <div className="listMeta memberNotesMeta">
                        {new Date(n.createdIso).toLocaleDateString()}
                        {n.adoWorkItemId ? ` • ADO: ${n.adoWorkItemId}` : ''}
                      </div>
                    </div>

                    <div className="memberNotesRowActions">
                      <button
                        type="button"
                        className={`chip chipTag chipTag-${n.tag.toLowerCase()} memberNotesTagPill`}
                        onClick={(e) => {
                          e.stopPropagation()
                          setSelectedNoteId(n.id)
                          setIsEditOpen(false)
                          setDraftText('')
                          setEditTab('Write')
                        }}
                        title="Open note"
                      >
                        {n.tag}
                      </button>
                      <button
                        type="button"
                        className="btn btnGhost btnIcon"
                        title="Open full page"
                        onClick={(e) => {
                          e.stopPropagation()
                          navigate(`/team/${member.id}/notes/${n.id}`)
                        }}
                      >
                        ↗
                      </button>
                      <button
                        type="button"
                        className="btn btnGhost btnIcon"
                        title={expandedNoteId === n.id ? 'Collapse' : 'Expand'}
                        onClick={(e) => {
                          e.stopPropagation()
                          setExpandedNoteId((prev) => (prev === n.id ? undefined : n.id))
                        }}
                      >
                        {expandedNoteId === n.id ? '▾' : '▸'}
                      </button>
                    </div>
                  </div>

                  {expandedNoteId === n.id ? (
                    <div className="memberNotesExpandedBody">
                      <div className="noteText noteBody memberNotesExpandedMarkdown">
                        <Markdown text={n.text} />
                      </div>
                    </div>
                  ) : (
                    <div className="memberNotesCollapsedMarkdown">
                      <Markdown text={n.text} />
                    </div>
                  )}
                </div>
              </div>
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
                      const nowIso = new Date().toISOString()
                      const nextNotes = member.notes.map((x) =>
                        x.id === selectedNote.id ? { ...x, text: draftText, lastModifiedIso: nowIso } : x,
                      )
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
          setNewTitle('')
          setNewText('')
        }}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button
              className="btn btnSecondary"
              onClick={() => {
                if (!newText.trim()) return
                const title = newTitle.trim()
                const next: TeamNote = {
                  id: newId('note'),
                  createdIso: new Date().toISOString(),
                  lastModifiedIso: new Date().toISOString(),
                  tag: newTag,
                  title: title || undefined,
                  text: newText.trim(),
                }
                updateNotes([next, ...member.notes])
                setIsNewOpen(false)
                setNewTitle('')
                setNewText('')
              }}
            >
              Create
            </button>
            <button
              className="btn btnGhost"
              onClick={() => {
                setIsNewOpen(false)
                setNewTitle('')
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
            <div className="fieldLabel">Title (optional)</div>
            <input
              className="input"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
              placeholder="e.g., 1:1 follow-ups, Standup recap…"
            />
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
  const [quickFilter, setQuickFilter] = useState<'All' | 'Current' | 'Blocked' | 'InReview'>('All')
  const [statusFilter, setStatusFilter] = useState<string>('All')

  function ticketAttentionTone(status: string) {
    const s = status.toLowerCase()
    if (s.includes('blocked')) return 'toneBad'
    if (s.includes('code review') || s.includes('in review') || s.includes('review')) return 'toneWarn'
    return 'toneNeutral'
  }

  const statusOptions = useMemo(() => {
    const uniq = Array.from(new Set(member.azureItems.map((a) => a.status).filter(Boolean)))
    uniq.sort((a, b) => a.localeCompare(b))
    return ['All', ...uniq]
  }, [member.azureItems])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    let items = member.azureItems

    if (quickFilter === 'Current') {
      items = items.filter((a) => isCurrentTicketStatus(a.status))
    } else if (quickFilter === 'Blocked') {
      items = items.filter((a) => a.status.toLowerCase().includes('blocked'))
    } else if (quickFilter === 'InReview') {
      items = items.filter((a) => {
        const s = a.status.toLowerCase()
        return s.includes('code review') || s.includes('in review') || s.includes('review')
      })
    }

    if (statusFilter !== 'All') {
      items = items.filter((a) => a.status === statusFilter)
    }

    if (!q) return items
    return items.filter((a) => `${a.id} ${a.title} ${a.status}`.toLowerCase().includes(q))
  }, [member.azureItems, query, quickFilter, statusFilter])

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
          <label className="field">
            <div className="fieldLabel">Status</div>
            <select className="select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              {statusOptions.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="chipRow memberWorkItemsQuickFilters" aria-label="Work items quick filters">
          <button
            type="button"
            className={`chipBtn ${quickFilter === 'All' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('All')}
          >
            All
          </button>
          <button
            type="button"
            className={`chipBtn ${quickFilter === 'Current' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('Current')}
            title="In progress, blocked, or in review"
          >
            Current
          </button>
          <button
            type="button"
            className={`chipBtn ${quickFilter === 'InReview' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('InReview')}
          >
            In Review
          </button>
          <button
            type="button"
            className={`chipBtn ${quickFilter === 'Blocked' ? 'chipBtnActive' : ''}`}
            onClick={() => setQuickFilter('Blocked')}
          >
            Blocked
          </button>
        </div>

        <div className="focusTicketsList" role="list">
          {filtered.length === 0 ? (
            <div className="muted pad">No work items match your search.</div>
          ) : (
            filtered.map((a) => (
              <button
                key={a.id}
                type="button"
                className={`focusTicketRow focusTicketRowBtn ${ticketAttentionTone(a.status)}`}
                onClick={() => {
                  navigate(`/team/${member.id}/work-items/${a.id}`)
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
    </section>
  )
}

function MemberRisksTab({
  memberId,
  teamMemberRisks,
  risks,
}: {
  memberId: string
  teamMemberRisks: TeamMemberRisk[]
  risks: Risk[]
}) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState<TeamMemberRisk['status'] | 'All'>('All')
  const [createModalOpen, setCreateModalOpen] = useState(false)
  const [createDraft, setCreateDraft] = useState<TeamMemberRisk | null>(null)

  const severityOptions: TeamMemberRisk['severity'][] = ['Low', 'Medium', 'High']
  const trendOptions: TeamMemberRisk['trend'][] = ['Improving', 'Stable', 'Worsening']
  const statusOptions: TeamMemberRisk['status'][] = ['Open', 'Mitigating', 'Resolved']

  function todayIsoDate() {
    // ISO date (YYYY-MM-DD) in local time.
    const d = new Date()
    const yyyy = d.getFullYear()
    const mm = String(d.getMonth() + 1).padStart(2, '0')
    const dd = String(d.getDate()).padStart(2, '0')
    return `${yyyy}-${mm}-${dd}`
  }

  function openCreateModal() {
    setCreateDraft({
      id: newId('tmr'),
      memberId,
      title: '',
      severity: 'Medium',
      riskType: '',
      status: 'Open',
      trend: 'Stable',
      firstNoticedDateIso: todayIsoDate(),
      impactArea: '',
      description: '',
      currentAction: '',
      linkedRiskId: undefined,
      lastReviewedIso: undefined,
    })
    setCreateModalOpen(true)
  }

  function closeCreateModal() {
    setCreateModalOpen(false)
    setCreateDraft(null)
  }

  function saveCreateModal() {
    if (!createDraft) return
    const title = createDraft.title.trim()
    if (!title) return

    const next: TeamMemberRisk = {
      ...createDraft,
      title,
      riskType: createDraft.riskType.trim(),
      impactArea: createDraft.impactArea.trim(),
      description: createDraft.description.trim(),
      currentAction: createDraft.currentAction.trim(),
      memberId,
      linkedRiskId: createDraft.linkedRiskId || undefined,
    }

    dispatch({ type: 'addTeamMemberRisk', teamMemberRisk: next })
    closeCreateModal()
    navigate(`/team/${memberId}/risks/${next.id}`)
  }

  const memberRisks = useMemo(() => {
    const q = query.trim().toLowerCase()
    return teamMemberRisks
      .filter((r) => r.memberId === memberId)
      .filter((r) => (statusFilter === 'All' ? true : r.status === statusFilter))
      .filter((r) => {
        if (!q) return true
        const hay = [r.title, r.riskType, r.impactArea, r.description, r.currentAction].join(' ').toLowerCase()
        return hay.includes(q)
      })
  }, [memberId, query, statusFilter, teamMemberRisks])

  return (
    <section className="card subtle" aria-label="Risks tab">
      <div className="cardHeader">
        <div className="cardTitle">Risks</div>
        <button className="btn btnGhost" type="button" onClick={openCreateModal}>
          Add risk
        </button>
      </div>
      <div className="pad">
        <div
          className="tasksFiltersRow"
          aria-label="Risks filters"
          style={{ gridTemplateColumns: 'minmax(0, 1.6fr) minmax(0, 1fr)', marginBottom: 12 }}
        >
          <label className="field">
            <div className="fieldLabel">Search</div>
            <input
              className="input"
              placeholder="Search by title, type, impact area, or text…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
            />
          </label>
          <label className="field">
            <div className="fieldLabel">Status</div>
            <select
              className="select"
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as TeamMemberRisk['status'] | 'All')}
            >
              <option value="All">All</option>
              <option value="Open">Open</option>
              <option value="Mitigating">Mitigating</option>
              <option value="Resolved">Resolved</option>
            </select>
          </label>
        </div>

        {memberRisks.length === 0 ? (
          <div className="muted">No member risks match your filters.</div>
        ) : (
          <div className="list listCard">
            {memberRisks.map((r) => {
              const linkedGlobal = r.linkedRiskId ? risks.find((x) => x.id === r.linkedRiskId) : undefined
              return (
              <button
                key={r.id}
                className="listRow listRowBtn"
                onClick={() => {
                  navigate(`/team/${memberId}/risks/${r.id}`)
                }}
              >
                <span className={`dot dot-${r.severity.toLowerCase()}`} aria-hidden="true" />
                <div className="listMain">
                  <div className="listTitle">{r.title}</div>
                  <div className="listMeta">
                    {r.status} • {r.trend}
                    {linkedGlobal ? ` • linked: ${linkedGlobal.title}` : ''}
                  </div>
                </div>
              </button>
            )})}
          </div>
        )}
      </div>

      <Modal
        title="Add team member risk"
        isOpen={createModalOpen}
        onClose={closeCreateModal}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button className="btn btnSecondary" type="button" onClick={saveCreateModal} disabled={!createDraft?.title.trim()}>
              Add
            </button>
            <button className="btn btnGhost" type="button" onClick={closeCreateModal}>
              Cancel
            </button>
          </div>
        }
      >
        {!createDraft ? null : (
          <div className="fieldGrid">
            <label className="field" style={{ gridColumn: '1 / -1' }}>
              <div className="fieldLabel">Title</div>
              <input
                className="input"
                value={createDraft.title}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, title: e.target.value } : d))}
                placeholder="What could go wrong?"
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Severity</div>
              <select
                className="select"
                value={createDraft.severity}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, severity: e.target.value as TeamMemberRisk['severity'] } : d))}
              >
                {severityOptions.map((x) => (
                  <option key={x} value={x}>
                    {x}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Status</div>
              <select
                className="select"
                value={createDraft.status}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, status: e.target.value as TeamMemberRisk['status'] } : d))}
              >
                {statusOptions.map((x) => (
                  <option key={x} value={x}>
                    {x}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Trend</div>
              <select
                className="select"
                value={createDraft.trend}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, trend: e.target.value as TeamMemberRisk['trend'] } : d))}
              >
                {trendOptions.map((x) => (
                  <option key={x} value={x}>
                    {x}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">First noticed</div>
              <input
                className="input"
                type="date"
                value={createDraft.firstNoticedDateIso}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, firstNoticedDateIso: e.target.value } : d))}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Type</div>
              <input
                className="input"
                value={createDraft.riskType}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, riskType: e.target.value } : d))}
                placeholder="e.g., Burnout, Delivery, Role clarity"
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Impact area</div>
              <input
                className="input"
                value={createDraft.impactArea}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, impactArea: e.target.value } : d))}
                placeholder="e.g., Quality, Schedule, Team health"
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Link to global risk (optional)</div>
              <select
                className="select"
                value={createDraft.linkedRiskId ?? ''}
                onChange={(e) =>
                  setCreateDraft((d) => (d ? { ...d, linkedRiskId: e.target.value ? e.target.value : undefined } : d))
                }
              >
                <option value="">(None)</option>
                {risks.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.title}
                  </option>
                ))}
              </select>
            </label>

            <label className="field" style={{ gridColumn: '1 / -1' }}>
              <div className="fieldLabel">Description</div>
              <textarea
                className="textarea"
                value={createDraft.description}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, description: e.target.value } : d))}
                placeholder="What are you observing? What evidence supports it?"
              />
            </label>

            <label className="field" style={{ gridColumn: '1 / -1' }}>
              <div className="fieldLabel">Current action</div>
              <textarea
                className="textarea"
                value={createDraft.currentAction}
                onChange={(e) => setCreateDraft((d) => (d ? { ...d, currentAction: e.target.value } : d))}
                placeholder="What are you doing next to mitigate?"
              />
            </label>
          </div>
        )}
      </Modal>
    </section>
  )
}

function MemberGrowthTab({ member }: { member: TeamMember }) {
  const dispatch = useAppDispatch()
  const growth = useGrowthForMember(member.id)
  const navigate = useNavigate()

  const SKILL_LEVEL_OPTIONS = ['Awareness', 'Beginner', 'Developing', 'Intermediate', 'Advanced', 'Expert'] as const

  const [skillModalOpen, setSkillModalOpen] = useState(false)
  const [skillModalIndex, setSkillModalIndex] = useState<number | null>(null)
  const [skillDraft, setSkillDraft] = useState<{ label: string; from: string; to: string }>({ label: '', from: '', to: '' })

  const [themeModalOpen, setThemeModalOpen] = useState(false)
  const [themeDraft, setThemeDraft] = useState<GrowthFeedbackTheme | null>(null)
  const [themeIsNew, setThemeIsNew] = useState(false)

  const [focusModalOpen, setFocusModalOpen] = useState(false)
  const [focusDraftText, setFocusDraftText] = useState('')

  function baseGrowth(): Growth {
    const g =
      growth ?? {
        id: newId('growth'),
        memberId: member.id,
        goals: [],
        skillsInProgress: [],
        feedbackThemes: [],
        focusAreasMarkdown: '',
      }
    return {
      ...g,
      memberId: member.id,
      goals: g.goals ?? [],
      skillsInProgress: g.skillsInProgress ?? [],
      feedbackThemes: g.feedbackThemes ?? [],
      focusAreasMarkdown: g.focusAreasMarkdown ?? '',
    }
  }

  function normalizeLines(items: string[]) {
    const cleaned = items.map((x) => x.trim()).filter(Boolean)
    // Keep order but remove duplicates.
    return Array.from(new Set(cleaned))
  }

  function parseSkillText(input: string) {
    const raw = input.trim()
    if (!raw) return { label: '', from: '', to: '' }

    const arrow = '→'
    const hasArrow = raw.includes(arrow)
    const [leftRaw, rightRaw] = hasArrow ? raw.split(arrow, 2) : [raw, '']
    const left = (leftRaw ?? '').trim()
    const to = (rightRaw ?? '').trim()

    const colonIdx = left.indexOf(':')
    if (colonIdx >= 0) {
      const label = left.slice(0, colonIdx).trim()
      const from = left.slice(colonIdx + 1).trim()
      return { label, from, to }
    }

    // Fallback: treat the whole left side as the label.
    return { label: left, from: '', to }
  }

  function formatSkillText(skill: { label: string; from: string; to: string }) {
    const label = skill.label.trim()
    const from = skill.from.trim()
    const to = skill.to.trim()

    if (!label) return ''
    if (from && to) return `${label}: ${from} → ${to}`
    if (from) return `${label}: ${from}`
    if (to) return `${label} → ${to}`
    return label
  }

  function commitGrowth(patch: Partial<Growth>) {
    const g = baseGrowth()
    dispatch({
      type: 'updateGrowth',
      growth: {
        ...g,
        ...patch,
        memberId: member.id,
        goals: patch.goals ?? g.goals,
        skillsInProgress: patch.skillsInProgress ?? g.skillsInProgress,
        feedbackThemes: patch.feedbackThemes ?? g.feedbackThemes,
        focusAreasMarkdown: patch.focusAreasMarkdown ?? g.focusAreasMarkdown,
      },
    })
  }

  function openEditSkill(idx: number) {
    const g = baseGrowth()
    const text = g.skillsInProgress[idx] ?? ''
    setSkillModalIndex(idx)
    setSkillDraft(parseSkillText(text))
    setSkillModalOpen(true)
  }

  function openAddSkill() {
    setSkillModalIndex(null)
    setSkillDraft({ label: '', from: '', to: '' })
    setSkillModalOpen(true)
  }

  function saveSkill() {
    const nextText = formatSkillText(skillDraft)
    if (!nextText) return
    const g = baseGrowth()
    const skills = [...g.skillsInProgress]
    if (skillModalIndex === null) {
      skills.push(nextText)
    } else {
      skills[skillModalIndex] = nextText
    }
    commitGrowth({ skillsInProgress: normalizeLines(skills) })
    setSkillModalOpen(false)
  }

  function deleteSkill() {
    if (skillModalIndex === null) return
    const g = baseGrowth()
    const skills = g.skillsInProgress.filter((_, i) => i !== skillModalIndex)
    commitGrowth({ skillsInProgress: normalizeLines(skills) })
    setSkillModalOpen(false)
  }

  function openAddTheme() {
    setThemeIsNew(true)
    setThemeDraft({ id: newId('growth-theme'), title: '', description: '', observedSinceLabel: undefined })
    setThemeModalOpen(true)
  }

  function openEditTheme(themeId: string) {
    const g = baseGrowth()
    const t = g.feedbackThemes.find((x) => x.id === themeId)
    if (!t) return
    setThemeIsNew(false)
    setThemeDraft({ ...t })
    setThemeModalOpen(true)
  }

  function saveTheme() {
    if (!themeDraft) return
    const title = themeDraft.title.trim()
    const description = themeDraft.description.trim()
    const observedSinceLabel = (themeDraft.observedSinceLabel ?? '').trim() || undefined

    // If user left it blank, treat as cancel (new) or delete (existing).
    if (!title && !description) {
      if (themeIsNew) {
        setThemeModalOpen(false)
        return
      }
      deleteTheme(themeDraft.id)
      return
    }

    const next: GrowthFeedbackTheme = { ...themeDraft, title, description, observedSinceLabel }
    const g = baseGrowth()
    const existing = g.feedbackThemes
    const themes = themeIsNew
      ? [...existing, next]
      : existing.map((t) => (t.id === next.id ? next : t))

    commitGrowth({ feedbackThemes: themes })
    setThemeModalOpen(false)
  }

  function deleteTheme(themeId: string) {
    const g = baseGrowth()
    commitGrowth({ feedbackThemes: g.feedbackThemes.filter((t) => t.id !== themeId) })
    setThemeModalOpen(false)
  }

  function openEditFocusAreas() {
    const g = baseGrowth()
    setFocusDraftText(g.focusAreasMarkdown ?? '')
    setFocusModalOpen(true)
  }

  function saveFocusAreas() {
    commitGrowth({ focusAreasMarkdown: focusDraftText })
    setFocusModalOpen(false)
  }

  const derivedFromNotes = useMemo(() => {
    const praise = member.notes.filter((n) => n.tag === 'Praise').slice(0, 4)
    const concerns = member.notes.filter((n) => n.tag === 'Concern' || n.tag === 'Blocker').slice(0, 4)
    return { praise, concerns }
  }, [member.notes])

  const activeGoals = growth?.goals ?? []
  const skills = growth?.skillsInProgress ?? []
  const themes = growth?.feedbackThemes ?? []
  const focusAreasMarkdown = growth?.focusAreasMarkdown ?? ''

  function statusPillTone(status: GrowthGoalStatus) {
    if (status === 'Completed') return 'toneGood'
    return status === 'OnTrack' ? 'toneGood' : 'toneWarn'
  }

  function statusLabel(status: GrowthGoalStatus) {
    if (status === 'Completed') return 'Completed'
    return status === 'OnTrack' ? 'On Track' : 'Needs Attention'
  }

  return (
    <section className="growthShell" aria-label="Growth tab">
      <div className="growthTitle">Growth Overview</div>

      <div className="growthSection">
        <div className="growthSectionTitle">Active Growth Goals</div>
        {activeGoals.length === 0 ? (
          <div className="card subtle">
            <div className="pad muted">No growth goals yet. We’ll add goals next.</div>
          </div>
        ) : (
          <div className="growthGoalsGrid">
            {activeGoals.map((g) => (
              <button
                key={g.id}
                className="card subtle growthGoalCard growthGoalCardBtn"
                onClick={() => navigate(`/team/${member.id}/growth/goals/${g.id}`)}
                type="button"
              >
                <div className="pad">
                  <div className="growthGoalTop">
                    <div className="growthGoalTitle">{g.title}</div>
                    <span className={`pill ${statusPillTone(g.status)}`}>{statusLabel(g.status)}</span>
                  </div>
                  <div className="growthGoalDesc">{g.description}</div>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="growthSection">
        <div className="growthSectionTitleRow">
          <div className="growthSectionTitle">Skills in Progress</div>
          <button className="btn btnGhost btnIcon" type="button" title="Add skill" onClick={openAddSkill}>
            +
          </button>
        </div>
        {skills.length === 0 ? (
          <div className="card subtle">
            <div className="pad muted">No skills tracked yet.</div>
          </div>
        ) : (
          <div className="card subtle">
            <div className="pad">
              <div className="growthChipsRow" aria-label="Skills in progress">
                {skills.map((s, idx) => (
                  <button key={`${s}-${idx}`} className="pill toneNeutral pillBtn" type="button" onClick={() => openEditSkill(idx)}>
                    {s}
                  </button>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="growthSection">
        <div className="growthSectionTitleRow">
          <div className="growthSectionTitle">Active Feedback Themes</div>
          <button className="btn btnGhost btnIcon" type="button" title="Add theme" onClick={openAddTheme}>
            +
          </button>
        </div>
        {themes.length === 0 ? (
          <div className="card subtle">
            <div className="pad muted">
              No feedback themes yet.
              <div className="mutedSmall" style={{ marginTop: 6 }}>
                Recent note signals:{' '}
                {[...derivedFromNotes.praise, ...derivedFromNotes.concerns].length === 0
                  ? 'none'
                  : [...derivedFromNotes.praise, ...derivedFromNotes.concerns]
                      .map((n) => `${new Date(n.createdIso).toLocaleDateString()}: ${getDerivedTitle(n)}`)
                      .join(' • ')}
              </div>
            </div>
          </div>
        ) : (
          <div className="card subtle">
            <div className="growthList">
              {themes.map((t) => (
                <button
                  key={t.id}
                  className="growthListRow growthListRowBtn"
                  type="button"
                  title="Edit theme"
                  onClick={() => openEditTheme(t.id)}
                >
                  <div className="growthListMain">
                    <div className="growthListTitle">{t.title}</div>
                    <div className="growthListDesc">{t.description}</div>
                  </div>
                  {t.observedSinceLabel ? <div className="growthListMeta">{t.observedSinceLabel}</div> : null}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>

      <div className="growthSection">
        <div className="growthSectionTitleRow">
          <div className="growthSectionTitle">Current Focus Areas</div>
          <button className="btn btnGhost" type="button" onClick={openEditFocusAreas}>
            Edit
          </button>
        </div>
        <div className="card subtle">
          <div className="pad">
            {focusAreasMarkdown.trim() ? (
              <div className="noteText noteBody">
                <Markdown text={focusAreasMarkdown} />
              </div>
            ) : (
              <div className="muted">No focus areas tracked yet.</div>
            )}
          </div>
        </div>
      </div>

      <Modal
        title={skillModalIndex === null ? 'Add Skill' : 'Edit Skill'}
        isOpen={skillModalOpen}
        onClose={() => {
          setSkillModalOpen(false)
          setSkillModalIndex(null)
          setSkillDraft({ label: '', from: '', to: '' })
        }}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button className="btn btnSecondary" type="button" onClick={saveSkill}>
              Save
            </button>
            {skillModalIndex !== null ? (
              <button className="btn btnGhost" type="button" onClick={deleteSkill} title="Remove skill">
                Delete
              </button>
            ) : null}
          </div>
        }
      >
        <div className="fieldGrid2">
          <label className="field span2">
            <div className="fieldLabel">Skill</div>
            <input
              className="input"
              value={skillDraft.label}
              onChange={(e) => setSkillDraft((d) => ({ ...d, label: e.target.value }))}
              placeholder="e.g., System Design"
            />
          </label>
          <label className="field">
            <div className="fieldLabel">Current level</div>
            <select
              className="select"
              value={skillDraft.from}
              onChange={(e) => setSkillDraft((d) => ({ ...d, from: e.target.value }))}
            >
              <option value="">(Select)</option>
              {SKILL_LEVEL_OPTIONS.map((x) => (
                <option key={x} value={x}>
                  {x}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <div className="fieldLabel">Target level</div>
            <select
              className="select"
              value={skillDraft.to}
              onChange={(e) => setSkillDraft((d) => ({ ...d, to: e.target.value }))}
            >
              <option value="">(Select)</option>
              {SKILL_LEVEL_OPTIONS.map((x) => (
                <option key={x} value={x}>
                  {x}
                </option>
              ))}
            </select>
          </label>
        </div>
      </Modal>

      <Modal
        title={themeIsNew ? 'Add Feedback Theme' : 'Edit Feedback Theme'}
        isOpen={themeModalOpen}
        onClose={() => {
          setThemeModalOpen(false)
          setThemeDraft(null)
          setThemeIsNew(false)
        }}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button className="btn btnSecondary" type="button" onClick={saveTheme}>
              Save
            </button>
            {!themeIsNew && themeDraft ? (
              <button className="btn btnGhost" type="button" onClick={() => deleteTheme(themeDraft.id)}>
                Delete
              </button>
            ) : null}
          </div>
        }
      >
        <div className="fieldGrid2">
          <label className="field span2">
            <div className="fieldLabel">Title</div>
            <input
              className="input"
              value={themeDraft?.title ?? ''}
              onChange={(e) => setThemeDraft((d) => (d ? { ...d, title: e.target.value } : d))}
              placeholder="Short label"
            />
          </label>
          <label className="field span2">
            <div className="fieldLabel">Description</div>
            <textarea
              className="textarea"
              value={themeDraft?.description ?? ''}
              onChange={(e) => setThemeDraft((d) => (d ? { ...d, description: e.target.value } : d))}
              placeholder="What’s the repeated pattern?"
            />
          </label>
          <label className="field">
            <div className="fieldLabel">Observed since (optional)</div>
            <input
              className="input"
              value={themeDraft?.observedSinceLabel ?? ''}
              onChange={(e) => setThemeDraft((d) => (d ? { ...d, observedSinceLabel: e.target.value || undefined } : d))}
              placeholder="e.g., Q4"
            />
          </label>
        </div>
      </Modal>

      <Modal
        title="Edit Focus Areas"
        isOpen={focusModalOpen}
        onClose={() => {
          setFocusModalOpen(false)
          setFocusDraftText('')
        }}
        footer={
          <div className="row" style={{ marginTop: 0 }}>
            <button className="btn btnSecondary" type="button" onClick={saveFocusAreas}>
              Save
            </button>
          </div>
        }
      >
        <div className="fieldGrid">
          <label className="field">
            <div className="fieldLabel">Markdown</div>
            <textarea
              className="textarea"
              value={focusDraftText}
              onChange={(e) => setFocusDraftText(e.target.value)}
              placeholder={'Example:\n- Reduce surprises by raising risks earlier\n- Focus on leading design discussions\n- Delegate WIP'}
            />
          </label>
        </div>
      </Modal>
    </section>
  )
}


