import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamMember } from '../app/types'
import { Markdown } from '../components/Markdown'

const FILTER_TAGS: Array<NoteTag | 'All'> = ['All', 'Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

export function TeamNotesView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId } = useParams<{ memberId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

  // Ensure we land at the top when entering the history view.
  useLayoutEffect(() => {
    window.scrollTo(0, 0)
    document.querySelector<HTMLElement>('.mainContent')?.scrollTo({ top: 0 })
  }, [])

  useEffect(() => {
    ai.setContext('Context: Team Notes', [
      { id: 'summarize-patterns', label: 'Summarize patterns (frequent blockers)' },
      { id: 'growth-areas', label: 'Highlight growth areas' },
      { id: 'cite-notes', label: 'Cite specific notes (draft)' },
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

  return (
    <div className="page">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Notes</div>
          <div className="mutedSmall">{member?.name ?? ''}</div>
        </div>
        <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}`)}>
          Back to member
        </button>
      </div>

      {!member ? (
        <div className="card pad">
          <div className="muted">Select a team member.</div>
        </div>
      ) : (
        <NotesPanel key={member.id} member={member} />
      )}
    </div>
  )
}

function NotesPanel({ member }: { member: TeamMember }) {
  const dispatch = useAppDispatch()
  const [query, setQuery] = useState('')
  const [tagFilter, setTagFilter] = useState<NoteTag | 'All'>('All')
  const [sortBy, setSortBy] = useState<'Newest' | 'Oldest'>('Newest')
  const [editingNoteId, setEditingNoteId] = useState<string | undefined>(undefined)
  const [draftText, setDraftText] = useState('')
  const [editTab, setEditTab] = useState<'Write' | 'Preview'>('Write')
  const editTextareaRef = useRef<HTMLTextAreaElement | null>(null)
  const editInputMaxHeightPx = 360

  function autoGrow(el: HTMLTextAreaElement | null, capPx: number) {
    if (!el) return

    // Reset height so scrollHeight reflects the full content.
    el.style.height = 'auto'
    const next = Math.min(el.scrollHeight, capPx)
    el.style.height = `${next}px`
    el.style.overflowY = el.scrollHeight > capPx ? 'auto' : 'hidden'
  }

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

  function saveEdit(noteId: string) {
    const nextNotes = member.notes.map((n) => (n.id === noteId ? { ...n, text: draftText } : n))
    dispatch({ type: 'updateTeamMember', member: { ...member, notes: nextNotes } })
    setEditingNoteId(undefined)
    setDraftText('')
    setEditTab('Write')
  }

  const filteredSorted = useMemo(() => {
    const q = query.trim().toLowerCase()

    function matchesText(n: TeamMember['notes'][number]) {
      if (!q) return true
      const hay = [
        n.tag,
        n.title ?? '',
        n.text,
        n.adoWorkItemId ?? '',
        n.prUrl ?? '',
        new Date(n.createdIso).toLocaleDateString(),
      ]
        .join(' ')
        .toLowerCase()
      return hay.includes(q)
    }

    function matchesTag(n: TeamMember['notes'][number]) {
      if (tagFilter === 'All') return true
      return n.tag === tagFilter
    }

    const sorted = member.notes
      .filter((n) => matchesTag(n) && matchesText(n))
      .slice()
      .sort((a, b) => {
        const at = new Date(a.createdIso).getTime()
        const bt = new Date(b.createdIso).getTime()
        return sortBy === 'Newest' ? bt - at : at - bt
      })

    return sorted
  }, [member.notes, query, sortBy, tagFilter])

  return (
    <>
      <section className="card subtle" aria-label="All notes history">
        <div className="cardHeader">
          <div className="cardTitle">Notes History</div>
        </div>
        <div className="pad">
          <div className="tasksFiltersRow" aria-label="Notes filters">
            <label className="field">
              <div className="fieldLabel">Search</div>
              <input
                className="input"
                placeholder="Search tag, text, ADO id, date…"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Tag</div>
              <select
                className="select"
                value={tagFilter}
                onChange={(e) => setTagFilter(e.target.value as NoteTag | 'All')}
              >
                {FILTER_TAGS.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Sort</div>
              <select
                className="select"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as 'Newest' | 'Oldest')}
              >
                <option value="Newest">Newest first</option>
                <option value="Oldest">Oldest first</option>
              </select>
            </label>
          </div>

          <div className="notesList">
            {filteredSorted.length === 0 ? (
              <div className="muted">No notes yet.</div>
            ) : (
              filteredSorted.map((n) => (
                <div key={n.id} className="noteRow">
                  <div className="noteMeta">
                    <span className={`chip chipTag chipTag-${n.tag.toLowerCase()}`}>{n.tag}</span>
                    <span className="mutedSmall">{new Date(n.createdIso).toLocaleDateString()}</span>
                    {n.adoWorkItemId ? <span className="chip chipGhost">ADO: {n.adoWorkItemId}</span> : null}
                    {n.prUrl ? <span className="chip chipGhost">PR</span> : null}
                    <div className="noteActions">
                      {editingNoteId === n.id ? (
                        <>
                          <button className="btn btnSecondary" onClick={() => saveEdit(n.id)}>
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
    </>
  )
}


