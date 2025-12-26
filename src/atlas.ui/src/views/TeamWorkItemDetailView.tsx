import { useEffect, useMemo } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'

export function TeamWorkItemDetailView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId, workItemId } = useParams<{ memberId: string; workItemId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

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

      {!member ? (
        <div className="card pad">
          <div className="muted">Select a team member.</div>
        </div>
      ) : !item ? (
        <div className="card pad">
          <div className="muted">Work item not found.</div>
        </div>
      ) : (
        <div className="card pad">
          <div className="detailTitle">{item.title}</div>
          <div className="mutedSmall">{item.id}</div>

          <div className="kv">
            <div className="kvRow">
              <div className="kvKey">Status</div>
              <div className="kvVal">{item.status}</div>
            </div>
            <div className="kvRow">
              <div className="kvKey">Time taken</div>
              <div className="kvVal">{item.timeTaken ?? '—'}</div>
            </div>
            <div className="kvRow">
              <div className="kvKey">Links</div>
              <div className="kvVal">
                {(item.ticketUrl ?? item.prUrl ?? item.commitsUrl) ? (
                  <>
                    {item.ticketUrl ? (
                      <div>
                        <a href={item.ticketUrl} target="_blank" rel="noreferrer">
                          Ticket
                        </a>
                      </div>
                    ) : null}
                    {item.prUrl ? (
                      <div>
                        <a href={item.prUrl} target="_blank" rel="noreferrer">
                          PR
                        </a>
                      </div>
                    ) : null}
                    {item.commitsUrl ? (
                      <div>
                        <a href={item.commitsUrl} target="_blank" rel="noreferrer">
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
        </div>
      )}
    </div>
  )
}



