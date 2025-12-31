import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamNote } from '../app/types'
import { Markdown } from '../components/Markdown'

const NOTE_TAGS: NoteTag[] = ['Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

function stripMarkdownHeadings(line: string) {
  return line.replace(/^#{1,6}\s+/, '').trim()
}

function getDerivedTitle(note: TeamNote) {
  const explicit = note.title?.trim()
  if (explicit) return explicit
  const firstNonEmpty = note.text.split('\n').map((l) => l.trim()).find((l) => l.length > 0) ?? '(untitled)'
  return stripMarkdownHeadings(firstNonEmpty)
}

function formatReadableDateTime(iso: string) {
  const dt = new Date(iso)
  return dt.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: 'numeric',
    minute: '2-digit',
  })
}

export function TeamNoteDetailView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId, noteId } = useParams<{ memberId: string; noteId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

  useEffect(() => {
    ai.setContext('Context: Team Note Detail', [{ id: 'summarize-note', label: 'Summarize this note' }])
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

  const note = useMemo(() => {
    if (!member || !noteId) return undefined
    return member.notes.find((n) => n.id === noteId)
  }, [member, noteId])

  const [isEditing, setIsEditing] = useState(false)
  const [draftTitle, setDraftTitle] = useState('')
  const [draftTag, setDraftTag] = useState<NoteTag>('Quick')
  const [draftText, setDraftText] = useState('')

  // Keep drafts in sync when navigating between notes (but don't clobber active edits).
  useEffect(() => {
    if (!note) return
    if (isEditing) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
  }, [isEditing, note])

  function beginEdit() {
    if (!note) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
    setIsEditing(true)
  }

  function cancelEdit() {
    if (!note) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
    setIsEditing(false)
  }

  function saveEdit() {
    if (!member || !note) return
    const nowIso = new Date().toISOString()
    const nextTitle = draftTitle.trim()
    const nextNotes = member.notes.map((n) =>
      n.id === note.id
        ? {
            ...n,
            title: nextTitle ? nextTitle : undefined,
            tag: draftTag,
            text: draftText,
            lastModifiedIso: nowIso,
          }
        : n,
    )
    dispatch({ type: 'updateTeamMember', member: { ...member, notes: nextNotes } })
    setIsEditing(false)
  }

  return (
    <div className="page pageFill">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Note</div>
          <div className="mutedSmall">{member?.name ?? ''}</div>
        </div>
        <div className="row" style={{ marginTop: 0 }}>
          <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}/notes`)}>
            Back to notes
          </button>
          <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}`)}>
            Back to member
          </button>
        </div>
      </div>

      {!member ? (
        <div className="card pad">
          <div className="muted">Select a team member.</div>
        </div>
      ) : !note ? (
        <div className="card pad">
          <div className="muted">Note not found.</div>
        </div>
      ) : (
        <div className="card pad noteDetailCard">
          <div className="noteMeta">
            <span className="noteDetailLastUpdated">
              <span className="noteDetailLastUpdatedLabel">Last updated</span>
              <span className="noteDetailLastUpdatedValue">{formatReadableDateTime(note.lastModifiedIso ?? note.createdIso)}</span>
            </span>
            {note.adoWorkItemId ? <span className="chip chipGhost">ADO: {note.adoWorkItemId}</span> : null}
            {note.prUrl ? <span className="chip chipGhost">PR</span> : null}
            <div className="noteActions">
              {isEditing ? (
                <>
                  <button className="btn btnSecondary" type="button" onClick={saveEdit}>
                    Save
                  </button>
                  <button className="btn btnGhost" type="button" onClick={cancelEdit}>
                    Cancel
                  </button>
                </>
              ) : (
                <button className="btn btnGhost" type="button" onClick={beginEdit}>
                  Edit
                </button>
              )}
            </div>
          </div>

          <div className="fieldGrid2 noteDetailFields" aria-label="Note fields">
            <label className="field">
              <div className="fieldLabel">Title</div>
              {isEditing ? (
                <input
                  className="input"
                  value={draftTitle}
                  placeholder={getDerivedTitle(note)}
                  onChange={(e) => setDraftTitle(e.target.value)}
                />
              ) : (
                <div className="noteDetailReadonly">{getDerivedTitle(note)}</div>
              )}
            </label>

            <label className="field">
              <div className="fieldLabel">Type</div>
              {isEditing ? (
                <select className="select" value={draftTag} onChange={(e) => setDraftTag(e.target.value as NoteTag)}>
                  {NOTE_TAGS.map((t) => (
                    <option key={t} value={t}>
                      {t}
                    </option>
                  ))}
                </select>
              ) : (
                <div className="noteDetailReadonly">
                  <span className={`chip chipTag chipTag-${note.tag.toLowerCase()}`}>{note.tag}</span>
                </div>
              )}
            </label>
          </div>

          {isEditing ? (
            <textarea className="textarea noteDetailTextarea" value={draftText} onChange={(e) => setDraftText(e.target.value)} />
          ) : (
            <div className="noteText noteBody noteDetailBody">
              <Markdown text={note.text} />
            </div>
          )}
        </div>
      )}
    </div>
  )
}



