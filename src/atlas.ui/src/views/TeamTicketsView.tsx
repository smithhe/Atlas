import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { AzureItem, TeamMember } from '../app/types'

export function TeamTicketsView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId } = useParams<{ memberId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

  useEffect(() => {
    ai.setContext('Context: Team Tickets', [
      { id: 'summarize-queue', label: 'Summarize ticket queue' },
      { id: 'spot-blockers', label: 'Spot likely blockers' },
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
          <div className="detailTitle">Tickets</div>
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
        <TicketsPanel member={member} />
      )}
    </div>
  )
}

function TicketsPanel({ member }: { member: TeamMember }) {
  const [selectedAzureId, setSelectedAzureId] = useState<string | undefined>(member.azureItems[0]?.id)

  useEffect(() => {
    setSelectedAzureId(member.azureItems[0]?.id)
  }, [member.id, member.azureItems])

  const selectedAzure: AzureItem | undefined = useMemo(
    () => member.azureItems.find((a) => a.id === selectedAzureId),
    [member.azureItems, selectedAzureId],
  )

  return (
    <section className="card subtle">
      <div className="cardHeader">
        <div className="cardTitle">All Azure DevOps Items</div>
      </div>

      <div className="azureGrid pad">
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
  )
}


