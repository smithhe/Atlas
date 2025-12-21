import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamMember, TeamNote } from '../app/types'

const TAGS: NoteTag[] = ['Blocker', 'Progress', 'Concern', 'Praise', 'Standup']

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

export function TeamNotesView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId } = useParams<{ memberId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

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
        <NotesPanel member={member} />
      )}
    </div>
  )
}

function NotesPanel({ member }: { member: TeamMember }) {
  const dispatch = useAppDispatch()
  const [quickNote, setQuickNote] = useState('')
  const [tag, setTag] = useState<NoteTag>('Standup')
  const [structured, setStructured] = useState('')
  const quickNoteRef = useRef<HTMLTextAreaElement | null>(null)
  const structuredRef = useRef<HTMLTextAreaElement | null>(null)
  const noteInputMaxHeightPx = 220

  function update(patch: Partial<TeamMember>) {
    dispatch({ type: 'updateTeamMember', member: { ...member, ...patch } })
  }

  function addNote(note: TeamNote) {
    update({ notes: [note, ...member.notes] })
  }

  function autoGrow(el: HTMLTextAreaElement | null, capPx: number) {
    if (!el) return
    el.style.height = 'auto'
    const next = Math.min(el.scrollHeight, capPx)
    el.style.height = `${next}px`
    el.style.overflowY = el.scrollHeight > capPx ? 'auto' : 'hidden'
  }

  useLayoutEffect(() => {
    autoGrow(quickNoteRef.current, noteInputMaxHeightPx)
  }, [noteInputMaxHeightPx, quickNote])

  useLayoutEffect(() => {
    autoGrow(structuredRef.current, noteInputMaxHeightPx)
  }, [noteInputMaxHeightPx, structured])

  return (
    <>
      <section className="card subtle">
        <div className="cardHeader">
          <div className="cardTitle">Add Note</div>
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

      <section className="card subtle" aria-label="All notes history">
        <div className="cardHeader">
          <div className="cardTitle">All Notes</div>
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
    </>
  )
}


