import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getAzureConnection,
  importAzureTeam,
  listAzureImportWorkItems,
  listAzureUsers,
  linkAzureWorkItems,
} from '../app/api/azureDevOps'
import type { AzureConnectionDto, AzureImportWorkItemDto, AzureUserDto } from '../app/api/azureDevOps'
import { useAppState } from '../app/state/AppState'

export function AzureImportView() {
  const { projects, team } = useAppState()
  const [connection, setConnection] = useState<AzureConnectionDto | null>(null)
  const [users, setUsers] = useState<AzureUserDto[]>([])
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set())
  const [importWorkItems, setImportWorkItems] = useState<AzureImportWorkItemDto[]>([])
  const [selectedWorkItems, setSelectedWorkItems] = useState<Set<string>>(new Set())
  const [projectId, setProjectId] = useState('')
  const [teamMemberId, setTeamMemberId] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const teamById = useMemo(() => new Map(team.map((m) => [m.id, m])), [team])

  useEffect(() => {
    let mounted = true
    setLoading(true)
    getAzureConnection()
      .then((conn) => {
        if (!mounted) return
        setConnection(conn)
        if (conn?.organization) {
          return listAzureUsers(conn.organization).then((list) => {
            if (mounted) setUsers(list)
          })
        }
      })
      .then(() => listAzureImportWorkItems())
      .then((items) => {
        if (mounted) setImportWorkItems(items ?? [])
      })
      .catch((err) => {
        if (mounted) setError(err instanceof Error ? err.message : 'Failed to load Azure import data')
      })
      .finally(() => {
        if (mounted) setLoading(false)
      })

    return () => {
      mounted = false
    }
  }, [])

  async function onImportUsers() {
    setError(null)
    try {
      const selected = users.filter((u) => selectedUsers.has(u.uniqueName))
      await importAzureTeam(selected)
      setSelectedUsers(new Set())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to import team members')
    }
  }

  async function onLinkWorkItems() {
    if (!projectId || selectedWorkItems.size === 0) return
    setError(null)
    try {
      await linkAzureWorkItems([...selectedWorkItems], projectId, teamMemberId || undefined)
      setSelectedWorkItems(new Set())
      const items = await listAzureImportWorkItems()
      setImportWorkItems(items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to link work items')
    }
  }

  return (
    <div className="page">
      <h2 className="pageTitle">Azure DevOps Import</h2>
      <div className="muted" style={{ marginBottom: 8 }}>
        <Link to="/settings">Back to Settings</Link>
      </div>

      {error ? <div className="card pad" style={{ marginBottom: 12 }}>Error: {error}</div> : null}

      <div className="card pad" style={{ marginBottom: 16 }}>
        <h3>Select Azure team members</h3>
        {!connection?.organization ? (
          <div className="muted">Set your Azure organization in Settings before importing users.</div>
        ) : null}

        {loading ? <div className="muted">Loading…</div> : null}

        {users.length > 0 ? (
          <div className="fieldGrid2" style={{ marginTop: 12 }}>
            {users.map((u) => (
              <label key={u.uniqueName} className="field" style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                  type="checkbox"
                  checked={selectedUsers.has(u.uniqueName)}
                  onChange={(e) => {
                    const next = new Set(selectedUsers)
                    if (e.target.checked) next.add(u.uniqueName)
                    else next.delete(u.uniqueName)
                    setSelectedUsers(next)
                  }}
                />
                <span>{u.displayName || u.uniqueName}</span>
              </label>
            ))}
          </div>
        ) : (
          <div className="muted">No users loaded yet.</div>
        )}

        <div className="row" style={{ marginTop: 12 }}>
          <button className="btn" onClick={onImportUsers} disabled={selectedUsers.size === 0}>
            Import selected users
          </button>
        </div>
      </div>

      <div className="card pad">
        <h3>Work items awaiting import</h3>
        {importWorkItems.length === 0 ? (
          <div className="muted">No work items awaiting import.</div>
        ) : (
          <>
            <div className="row" style={{ marginBottom: 12 }}>
              <select className="input" value={projectId} onChange={(e) => setProjectId(e.target.value)}>
                <option value="">Select project</option>
                {projects.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>

              <select className="input" value={teamMemberId} onChange={(e) => setTeamMemberId(e.target.value)}>
                <option value="">Auto-assign from mapping</option>
                {team.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.name}
                  </option>
                ))}
              </select>

              <button className="btn" onClick={onLinkWorkItems} disabled={!projectId || selectedWorkItems.size === 0}>
                Link selected
              </button>
            </div>

            <div className="tableGrid" style={{ gridTemplateColumns: '40px 100px 1fr 120px 160px 160px' }}>
              <div className="tableCell tableHeader">✓</div>
              <div className="tableCell tableHeader">ID</div>
              <div className="tableCell tableHeader">Title</div>
              <div className="tableCell tableHeader">State</div>
              <div className="tableCell tableHeader">Assigned</div>
              <div className="tableCell tableHeader">Project</div>

              {importWorkItems.map((item) => {
                const assigned = item.suggestedTeamMemberId ? teamById.get(item.suggestedTeamMemberId)?.name : null
                const isSelected = selectedWorkItems.has(item.id)
                return (
                  <div className="tableRow" key={item.id}>
                    <div className="tableCell">
                      <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={(e) => {
                        const next = new Set(selectedWorkItems)
                        if (e.target.checked) next.add(item.id)
                        else next.delete(item.id)
                        setSelectedWorkItems(next)
                      }}
                    />
                    </div>
                    <div className="tableCell">{item.workItemId}</div>
                    <div className="tableCell truncate">
                      <a href={item.url} target="_blank" rel="noreferrer">
                        {item.title}
                      </a>
                    </div>
                    <div className="tableCell">{item.state}</div>
                    <div className="tableCell">
                      {assigned ? (
                        assigned
                      ) : (
                        <span className="chip chipGhost">Unmatched</span>
                      )}
                    </div>
                    <div className="tableCell">{projectId ? projects.find((p) => p.id === projectId)?.name : '—'}</div>
                  </div>
                )
              })}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
