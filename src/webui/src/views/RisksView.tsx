import { useEffect, useMemo, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedRisk } from '../app/state/AppState'
import type { Risk, RiskStatus } from '../app/types'

function daysSince(iso: string) {
  const a = new Date(iso).getTime()
  const b = Date.now()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

export function RisksView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const { risks, selectedRiskId } = useAppState()
  const selected = useSelectedRisk()

  const [statusFilter, setStatusFilter] = useState<RiskStatus | 'All'>('All')
  const [projectFilter, setProjectFilter] = useState('')
  const [severityFilter, setSeverityFilter] = useState<'All' | 'Low' | 'Medium' | 'High'>('All')

  useEffect(() => {
    ai.setContext('Context: Risks', [
      { id: 'summarize-impact', label: 'Summarize impact' },
      { id: 'suggest-mitigations', label: 'Suggest mitigations' },
      { id: 'why-matters', label: 'Explain “why this matters” (draft)' },
    ])
  }, [ai.setContext])

  const filtered = useMemo(() => {
    return risks.filter((r) => {
      if (statusFilter !== 'All' && r.status !== statusFilter) return false
      if (severityFilter !== 'All' && r.severity !== severityFilter) return false
      if (projectFilter.trim() && !(r.project ?? '').toLowerCase().includes(projectFilter.trim().toLowerCase()))
        return false
      return true
    })
  }, [projectFilter, risks, severityFilter, statusFilter])

  return (
    <div className="page">
      <h2 className="pageTitle">Risks &amp; Mitigation</h2>

      <div className="splitGrid">
        <section className="pane paneLeft" aria-label="Risk list and filters">
          <div className="card tight">
            <div className="fieldGrid">
              <label className="field">
                <div className="fieldLabel">Status</div>
                <select
                  className="select"
                  value={statusFilter}
                  onChange={(e) => setStatusFilter(e.target.value as RiskStatus | 'All')}
                >
                  <option value="All">All</option>
                  <option value="Open">Open</option>
                  <option value="Watching">Watching</option>
                  <option value="Resolved">Resolved</option>
                </select>
              </label>
              <label className="field">
                <div className="fieldLabel">Project</div>
                <input className="input" value={projectFilter} onChange={(e) => setProjectFilter(e.target.value)} />
              </label>
              <label className="field">
                <div className="fieldLabel">Severity</div>
                <select
                  className="select"
                  value={severityFilter}
                  onChange={(e) => setSeverityFilter(e.target.value as 'All' | 'Low' | 'Medium' | 'High')}
                >
                  <option value="All">All</option>
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                </select>
              </label>
            </div>
          </div>

          <div className="list listCard">
            {filtered.map((r) => (
              <button
                key={r.id}
                className={`listRow listRowBtn ${r.id === selectedRiskId ? 'listRowActive' : ''}`}
                onClick={() => dispatch({ type: 'selectRisk', riskId: r.id })}
              >
                <span className={`dot dot-${r.severity.toLowerCase()}`} aria-hidden="true" />
                <div className="listMain">
                  <div className="listTitle">{r.title}</div>
                  <div className="listMeta">
                    {r.status} • last updated {daysSince(r.lastUpdatedIso)}d
                  </div>
                </div>
              </button>
            ))}
            {filtered.length === 0 ? <div className="muted pad">No risks match your filters.</div> : null}
          </div>
        </section>

        <section className="pane paneCenter" aria-label="Risk detail">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a risk to edit.</div>
            </div>
          ) : (
            <RiskDetail risk={selected} />
          )}
        </section>
      </div>
    </div>
  )
}

function RiskDetail({ risk }: { risk: Risk }) {
  const dispatch = useAppDispatch()

  function update(patch: Partial<Risk>) {
    dispatch({
      type: 'updateRisk',
      risk: { ...risk, ...patch, lastUpdatedIso: new Date().toISOString() },
    })
  }

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div className="detailTitle">Risk Detail</div>
        <div className="mutedSmall">Last updated: {new Date(risk.lastUpdatedIso).toLocaleString()}</div>
      </div>

      <div className="fieldGrid2">
        <label className="field span2">
          <div className="fieldLabel">Title</div>
          <input className="input" value={risk.title} onChange={(e) => update({ title: e.target.value })} />
        </label>

        <label className="field">
          <div className="fieldLabel">Status</div>
          <select className="select" value={risk.status} onChange={(e) => update({ status: e.target.value as RiskStatus })}>
            <option value="Open">Open</option>
            <option value="Watching">Watching</option>
            <option value="Resolved">Resolved</option>
          </select>
        </label>

        <label className="field">
          <div className="fieldLabel">Severity</div>
          <select
            className="select"
            value={risk.severity}
            onChange={(e) => update({ severity: e.target.value as 'Low' | 'Medium' | 'High' })}
          >
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="High">High</option>
          </select>
        </label>

        <label className="field">
          <div className="fieldLabel">Project</div>
          <input className="input" value={risk.project ?? ''} onChange={(e) => update({ project: e.target.value })} />
        </label>

        <label className="field span2">
          <div className="fieldLabel">Description</div>
          <textarea
            className="textarea"
            value={risk.description}
            onChange={(e) => update({ description: e.target.value })}
          />
        </label>

        <label className="field span2">
          <div className="fieldLabel">Evidence / Examples</div>
          <textarea className="textarea" value={risk.evidence} onChange={(e) => update({ evidence: e.target.value })} />
        </label>

        <div className="field span2">
          <div className="fieldLabel">Linked Tasks</div>
          <div className="placeholderBox">Linked tasks… (mock)</div>
        </div>

        <div className="field span2">
          <div className="fieldLabel">Linked Team Members</div>
          <div className="placeholderBox">Linked team members… (mock)</div>
        </div>

        <div className="field span2">
          <div className="fieldLabel">Notes / History</div>
          <div className="placeholderBox">
            {risk.history.length === 0 ? 'No history.' : risk.history.map((h) => `${new Date(h.createdIso).toLocaleDateString()}: ${h.text}`).join('\n')}
          </div>
        </div>
      </div>
    </div>
  )
}


