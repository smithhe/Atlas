import { useEffect, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import { Markdown } from '../components/Markdown'

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

  return (
    <div className="page">
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
        <div className="card pad">
          <div className="noteMeta">
            <span className={`chip chipTag chipTag-${note.tag.toLowerCase()}`}>{note.tag}</span>
            <span className="mutedSmall">{new Date(note.createdIso).toLocaleString()}</span>
            {note.adoWorkItemId ? <span className="chip chipGhost">ADO: {note.adoWorkItemId}</span> : null}
            {note.prUrl ? <span className="chip chipGhost">PR</span> : null}
          </div>
          <div className="noteText noteBody">
            <Markdown text={note.text} />
          </div>
        </div>
      )}
    </div>
  )
}



