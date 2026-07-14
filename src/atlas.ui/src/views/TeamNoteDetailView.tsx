import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useTeam } from '../app/queries/hooks'
import { useSelectedTeamMember, useSelectionActions } from '../app/state/SelectionState'
import { useAppCache } from '../app/queries/useAppCache'
import type { NoteTag } from '../app/types'
import { Markdown } from '../components/Markdown'
import { updateTeamNote } from '../app/api/teamMembers'
import { formatReadableDateTime, getDerivedTitle, reportSaveError } from '../app/utils'

const NOTE_TAGS: NoteTag[] = ['Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

export function TeamNoteDetailView() {
  const ai = useAi()
  const { selectTeamMember } = useSelectionActions()
  const cache = useAppCache()
  const navigate = useNavigate()
  const { memberId, noteId } = useParams<{ memberId: string; noteId: string }>()
  const team = useTeam()
  const member = useSelectedTeamMember()
  const memberName = useMemo(() => {
    return member?.name ?? team.find((m) => m.id === memberId)?.name ?? memberId
  }, [member?.name, memberId, team])

  useEffect(() => {
    ai.setContext('Context: Team Note Detail', [{ id: 'summarize-note', label: 'Summarize this note' }])
  }, [ai.setContext])

  useEffect(() => {
    if (!memberId) return
    selectTeamMember(memberId)
  }, [memberId, selectTeamMember])

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
  const [draftAdoWorkItemId, setDraftAdoWorkItemId] = useState('')
  const [draftPrUrl, setDraftPrUrl] = useState('')

  // Keep drafts in sync when navigating between notes (but don't clobber active edits).
  useEffect(() => {
    if (!note) return
    if (isEditing) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
    setDraftAdoWorkItemId(note.adoWorkItemId ?? '')
    setDraftPrUrl(note.prUrl ?? '')
  }, [isEditing, note])

  useEffect(() => {
    if (!isEditing) {
      ai.registerDraftTarget(null)
      return
    }

    ai.registerDraftTarget({
      label: 'note body',
      insert: (text) => {
        setDraftText((prev) => (prev.trim() ? `${prev.trimEnd()}\n\n${text}` : text))
      },
    })

    return () => {
      ai.registerDraftTarget(null)
    }
  }, [ai, isEditing, noteId])

  function beginEdit() {
    if (!note) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
    setDraftAdoWorkItemId(note.adoWorkItemId ?? '')
    setDraftPrUrl(note.prUrl ?? '')
    setIsEditing(true)
  }

  function cancelEdit() {
    if (!note) return
    setDraftTitle(note.title ?? '')
    setDraftTag(note.tag)
    setDraftText(note.text)
    setDraftAdoWorkItemId(note.adoWorkItemId ?? '')
    setDraftPrUrl(note.prUrl ?? '')
    setIsEditing(false)
  }

  function saveEdit() {
    if (!member || !note) return
    const nowIso = new Date().toISOString()
    const nextTitle = draftTitle.trim()
    const ado = draftAdoWorkItemId.trim()
    const pr = draftPrUrl.trim()
    const updated = {
      ...note,
      title: nextTitle ? nextTitle : undefined,
      tag: draftTag,
      text: draftText,
      adoWorkItemId: ado || undefined,
      prUrl: pr || undefined,
      lastModifiedIso: nowIso,
    }

    void (async () => {
      try {
        await updateTeamNote(member.id, note.id, {
          tag: updated.tag,
          title: updated.title,
          text: updated.text,
          adoWorkItemId: updated.adoWorkItemId,
          prUrl: updated.prUrl,
        })
        const nextNotes = member.notes.map((n) => (n.id === note.id ? updated : n))
        cache.updateTeamMember({ ...member, notes: nextNotes })
        setIsEditing(false)
      } catch (err) {
        reportSaveError(err, 'Unable to save note changes right now. Please try again.')
      }
    })()
  }

  return (
    <div className="page pageFill">
      <nav className="pageBreadcrumbs" aria-label="Breadcrumbs">
        <Link className="crumbLink" to="/team">
          Team
        </Link>
        <span className="crumbSep" aria-hidden="true">
          /
        </span>
        <Link className="crumbLink" to={`/team/${memberId}`}>
          {memberName}
        </Link>
        <span className="crumbSep" aria-hidden="true">
          /
        </span>
        <Link className="crumbLink" to={`/team/${memberId}/notes`}>
          Notes
        </Link>
        <span className="crumbSep" aria-hidden="true">
          /
        </span>
        <span className="crumbCurrent">Note</span>
      </nav>

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

            <label className="field">
              <div className="fieldLabel">ADO work item id</div>
              {isEditing ? (
                <input
                  className="input"
                  value={draftAdoWorkItemId}
                  onChange={(e) => setDraftAdoWorkItemId(e.target.value)}
                  placeholder="e.g., 12345"
                />
              ) : (
                <div className="noteDetailReadonly">{note.adoWorkItemId ?? '—'}</div>
              )}
            </label>

            <label className="field">
              <div className="fieldLabel">PR URL</div>
              {isEditing ? (
                <input
                  className="input"
                  value={draftPrUrl}
                  onChange={(e) => setDraftPrUrl(e.target.value)}
                  placeholder="https://…"
                />
              ) : note.prUrl ? (
                <div className="noteDetailReadonly">
                  <a href={note.prUrl} target="_blank" rel="noreferrer">
                    {note.prUrl}
                  </a>
                </div>
              ) : (
                <div className="noteDetailReadonly">—</div>
              )}
            </label>
          </div>

          {isEditing ? (
            <textarea
              className="textarea noteDetailTextarea"
              value={draftText}
              onChange={(e) => setDraftText(e.target.value)}
            />
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



