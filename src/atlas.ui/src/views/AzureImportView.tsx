import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  getAzureConnection,
  importAzureTeam,
  listImportedAzureUsers,
  listAzureImportWorkItems,
  listAzureUsers,
  linkAzureWorkItems,
} from '../app/api/azureDevOps'
import type { AzureConnectionDto, AzureImportWorkItemDto, AzureUserDto } from '../app/api/azureDevOps'
import { useAppState } from '../app/state/AppState'
import { LoadingButton } from '../components/LoadingButton'
import { LoadingOverlay } from '../components/LoadingOverlay'

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
  const [importingUsers, setImportingUsers] = useState(false)
  const [linkingWorkItems, setLinkingWorkItems] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const teamById = useMemo(() => new Map(team.map((m) => [m.id, m])), [team])
  const projectById = useMemo(() => new Map(projects.map((p) => [p.id, p])), [projects])
  const selectedProjectName = projectId ? projectById.get(projectId)?.name : ''

  useEffect(() => {
    let mounted = true
    setLoading(true)
    getAzureConnection()
      .then((conn) => {
        if (!mounted) return
        setConnection(conn)
        if (!conn?.organization || !conn.projectId || !conn.teamId) {
          setError('Set Project ID and Team ID in Settings before importing users.')
          return
        }

        return Promise.all([
          listAzureUsers(conn.organization, conn.projectId, conn.teamId),
          listImportedAzureUsers(),
        ]).then(([list, imported]) => {
          if (!mounted) return
          const importedSet = new Set(imported.map((u) => u.uniqueName.trim().toLowerCase()))
          const filtered = list.filter((u) => !importedSet.has(u.uniqueName.trim().toLowerCase()))
          setUsers(filtered)
        })
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
    setImportingUsers(true)
    try {
      const selected = users.filter((u) => selectedUsers.has(u.uniqueName))
      await importAzureTeam(selected)
      const selectedSet = new Set(selected.map((u) => u.uniqueName.trim().toLowerCase()))
      setUsers((prev) => prev.filter((u) => !selectedSet.has(u.uniqueName.trim().toLowerCase())))
      setSelectedUsers(new Set())
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to import team members')
    } finally {
      setImportingUsers(false)
    }
  }

  async function onLinkWorkItems() {
    if (!projectId || selectedWorkItems.size === 0) return
    setError(null)
    setLinkingWorkItems(true)
    try {
      await linkAzureWorkItems([...selectedWorkItems], projectId, teamMemberId || undefined)
      setSelectedWorkItems(new Set())
      const items = await listAzureImportWorkItems()
      setImportWorkItems(items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to link work items')
    } finally {
      setLinkingWorkItems(false)
    }
  }

  return (
    <div className="page">
      <div className="pageBreadcrumbs">
        <Link className="crumbLink" to="/settings">
          Settings
        </Link>
        <span className="crumbSep">/</span>
        <span className="crumbCurrent">Azure Import</span>
      </div>
      <h2 className="pageTitle">Azure DevOps Import</h2>

      {error ? (
        <div className="card pad" style={{ marginBottom: 12 }}>
          <div className="textBad">Error: {error}</div>
        </div>
      ) : null}

      <div className="card" style={{ marginBottom: 16 }}>
        <LoadingOverlay isLoading={loading || importingUsers} label="Loading Azure import data">
          <div className="cardHeader">
            <div className="cardTitle">Select Azure team members</div>
            <div className="mutedSmall">{users.length} available</div>
          </div>
          <div className="pad">
            {!connection?.organization ? (
              <div className="mutedSmall">Set your Azure organization in Settings before importing users.</div>
            ) : null}

            {users.length > 0 ? (
              <>
                <div className="rowTiny" style={{ marginTop: 10, alignItems: 'center' }}>
                  <button
                    type="button"
                    className="btn btnGhost"
                    disabled={users.length === 0}
                    onClick={() => setSelectedUsers(new Set(users.map((u) => u.uniqueName)))}
                  >
                    Select all
                  </button>
                  <button
                    type="button"
                    className="btn btnGhost"
                    disabled={selectedUsers.size === 0}
                    onClick={() => setSelectedUsers(new Set())}
                  >
                    Clear
                  </button>
                  <span className="mutedSmall" style={{ marginLeft: 'auto' }}>
                    {selectedUsers.size} selected
                  </span>
                </div>
                <div className="list listCard" style={{ marginTop: 10, maxHeight: 320, overflow: 'auto' }}>
                  {users.map((u) => (
                    <label
                      key={u.uniqueName}
                      className={`listRow listRowBtn ${selectedUsers.has(u.uniqueName) ? 'listRowActive' : ''}`}
                      style={{ cursor: 'pointer' }}
                    >
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
                      <div className="listMain">
                        <div className="listTitle">{u.displayName || u.uniqueName}</div>
                        {u.displayName ? <div className="listMeta">{u.uniqueName}</div> : null}
                      </div>
                    </label>
                  ))}
                </div>
              </>
            ) : (
              <div className="muted">No new users to import.</div>
            )}
          </div>
          <div className="cardFooter">
            <div className="rowTiny" style={{ alignItems: 'center' }}>
              <LoadingButton
                className="btn btnWide"
                onClick={onImportUsers}
                loading={importingUsers}
                spinnerLabel="Importing users"
                disabled={selectedUsers.size === 0 || loading}
              >
                Import selected users
              </LoadingButton>
              <div className="mutedSmall">Adds selected members to your team roster.</div>
            </div>
          </div>
        </LoadingOverlay>
      </div>

      <div className="card">
        <LoadingOverlay isLoading={loading || linkingWorkItems} label="Loading Azure import data">
          <div className="cardHeader">
            <div className="cardTitle">Work items awaiting import</div>
            <div className="mutedSmall">{importWorkItems.length} items</div>
          </div>
          {importWorkItems.length === 0 ? (
            <div className="pad muted">No work items awaiting import.</div>
          ) : (
            <div className="pad">
              <div className="fieldGrid2" style={{ marginBottom: 12 }}>
                <label className="field">
                  <div className="fieldLabel">Target project</div>
                  <select className="input" value={projectId} onChange={(e) => setProjectId(e.target.value)}>
                    <option value="">Select project</option>
                    {projects.map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.name}
                      </option>
                    ))}
                  </select>
                </label>

                <label className="field">
                  <div className="fieldLabel">Assign to</div>
                  <select className="input" value={teamMemberId} onChange={(e) => setTeamMemberId(e.target.value)}>
                    <option value="">Auto-assign from mapping</option>
                    {team.map((m) => (
                      <option key={m.id} value={m.id}>
                        {m.name}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <div className="rowTiny" style={{ alignItems: 'center', marginBottom: 12 }}>
                <LoadingButton
                  className="btn btnWide"
                  onClick={onLinkWorkItems}
                  loading={linkingWorkItems}
                  spinnerLabel="Linking work items"
                  disabled={!projectId || selectedWorkItems.size === 0}
                >
                  Link selected
                </LoadingButton>
                <div className="mutedSmall">
                  {selectedProjectName ? `Linking to ${selectedProjectName}.` : 'Select a project to link work items.'}
                </div>
                <span className="mutedSmall" style={{ marginLeft: 'auto' }}>
                  {selectedWorkItems.size} selected
                </span>
              </div>

              <div className="tableGrid" style={{ gridTemplateColumns: '40px 100px 1fr 120px 160px 140px' }}>
                <div className="tableCell tableHeader">✓</div>
                <div className="tableCell tableHeader">ID</div>
                <div className="tableCell tableHeader">Title</div>
                <div className="tableCell tableHeader">State</div>
                <div className="tableCell tableHeader">Assigned</div>
                <div className="tableCell tableHeader">Type</div>

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
                        {assigned ? assigned : <span className="chip chipGhost">Unmatched</span>}
                      </div>
                      <div className="tableCell">{item.workItemType}</div>
                    </div>
                  )
                })}
              </div>
            </div>
          )}
        </LoadingOverlay>
      </div>
    </div>
  )
}
