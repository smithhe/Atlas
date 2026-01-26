import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  getAzureConnection,
  getAzureTeamAreaPaths,
  importAzureTeam,
  listAzureProjects,
  listAzureTeams,
  listAzureUsers,
  updateAzureConnection,
} from '../app/api/azureDevOps'
import type { AzureProjectDto, AzureTeamDto, AzureUserDto } from '../app/api/azureDevOps'
import { LoadingButton } from '../components/LoadingButton'
import { LoadingOverlay } from '../components/LoadingOverlay'
import { Spinner } from '../components/Spinner'

type Step = 'project' | 'team' | 'members' | 'saving'

export function AzureSetupView() {
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('project')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [organization, setOrganization] = useState('')
  const [areaPath, setAreaPath] = useState('')
  const [areaPathLoading, setAreaPathLoading] = useState(false)
  const [areaPathError, setAreaPathError] = useState<string | null>(null)

  const [projects, setProjects] = useState<AzureProjectDto[]>([])
  const [selectedProjectId, setSelectedProjectId] = useState('')
  const [teams, setTeams] = useState<AzureTeamDto[]>([])
  const [selectedTeamId, setSelectedTeamId] = useState('')
  const [users, setUsers] = useState<AzureUserDto[]>([])
  const [selectedUsers, setSelectedUsers] = useState<Set<string>>(new Set())

  const [projectQuery, setProjectQuery] = useState('')
  const [teamQuery, setTeamQuery] = useState('')
  const [memberQuery, setMemberQuery] = useState('')

  const selectedProject = useMemo(() => projects.find((p) => p.id === selectedProjectId), [projects, selectedProjectId])
  const selectedTeam = useMemo(() => teams.find((t) => t.id === selectedTeamId), [teams, selectedTeamId])

  const sortedProjects = useMemo(() => [...projects].sort((a, b) => a.name.localeCompare(b.name)), [projects])
  const sortedTeams = useMemo(() => [...teams].sort((a, b) => a.name.localeCompare(b.name)), [teams])
  const sortedUsers = useMemo(
    () => [...users].sort((a, b) => (a.displayName || a.uniqueName).localeCompare(b.displayName || b.uniqueName)),
    [users],
  )

  const filteredProjects = useMemo(() => {
    const q = projectQuery.trim().toLowerCase()
    if (!q) return sortedProjects
    return sortedProjects.filter((p) => p.name.toLowerCase().includes(q))
  }, [projectQuery, sortedProjects])

  const filteredTeams = useMemo(() => {
    const q = teamQuery.trim().toLowerCase()
    if (!q) return sortedTeams
    return sortedTeams.filter((t) => t.name.toLowerCase().includes(q))
  }, [sortedTeams, teamQuery])

  const filteredUsers = useMemo(() => {
    const q = memberQuery.trim().toLowerCase()
    if (!q) return sortedUsers
    return sortedUsers.filter((u) => {
      const hay = `${u.displayName ?? ''} ${u.uniqueName}`.toLowerCase()
      return hay.includes(q)
    })
  }, [memberQuery, sortedUsers])

  function toggleUser(uniqueName: string, checked: boolean) {
    setSelectedUsers((prev) => {
      const next = new Set(prev)
      if (checked) next.add(uniqueName)
      else next.delete(uniqueName)
      return next
    })
  }

  useEffect(() => {
    let mounted = true
    getAzureConnection()
      .then((conn) => {
        if (!mounted || !conn) return
        setOrganization(conn.organization)
      })
      .catch(() => {
        // noop; setup handles missing connection
      })
    return () => {
      mounted = false
    }
  }, [])

  async function onLoadProjects() {
    if (!organization) return
    setLoading(true)
    setError(null)
    try {
      const list = await listAzureProjects(organization)
      setProjects(list)
      setSelectedProjectId('')
      setSelectedTeamId('')
      setTeams([])
      setUsers([])
      setSelectedUsers(new Set())
      setProjectQuery('')
      setTeamQuery('')
      setMemberQuery('')
      setAreaPath('')
      setAreaPathError(null)
      setAreaPathLoading(false)
      setStep('project')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load projects')
    } finally {
      setLoading(false)
    }
  }

  async function onSelectProject(projectId: string) {
    setSelectedProjectId(projectId)
    setSelectedTeamId('')
    setTeams([])
    setUsers([])
    setSelectedUsers(new Set())
    setTeamQuery('')
    setMemberQuery('')
    setAreaPath('')
    setAreaPathError(null)
    setAreaPathLoading(false)
    setError(null)
    setLoading(true)
    try {
      const list = await listAzureTeams(organization, projectId)
      setTeams(list)
      setStep('team')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load teams')
    } finally {
      setLoading(false)
    }
  }

  async function onSelectTeam(teamId: string, teamName: string) {
    setSelectedTeamId(teamId)
    setUsers([])
    setSelectedUsers(new Set())
    setMemberQuery('')
    setError(null)
    setLoading(true)
    setAreaPathLoading(true)
    setAreaPathError(null)
    try {
      const [usersRes, areasRes] = await Promise.allSettled([
        listAzureUsers(organization, selectedProjectId, teamId),
        getAzureTeamAreaPaths(organization, selectedProjectId, teamName),
      ])

      if (usersRes.status === 'rejected') {
        throw usersRes.reason
      }

      setUsers(usersRes.value)

      if (areasRes.status === 'fulfilled') {
        const defaultValue = (areasRes.value.defaultValue ?? '').trim()
        if (defaultValue) {
          // Always use the team default in setup; allow override later in Settings.
          setAreaPath(defaultValue)
        } else if (areasRes.value.values?.length) {
          // Defensive fallback (should be rare): pick first configured value.
          setAreaPath(areasRes.value.values[0].value)
        } else {
          setAreaPath('')
        }
      } else {
        setAreaPathError(areasRes.reason instanceof Error ? areasRes.reason.message : 'Failed to load team area paths')
      }

      setStep('members')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load team members')
    } finally {
      setLoading(false)
      setAreaPathLoading(false)
    }
  }

  async function onSave() {
    if (!selectedProject || !selectedTeam) return
    setLoading(true)
    setError(null)
    setStep('saving')
    try {
      const selected = users.filter((u) => selectedUsers.has(u.uniqueName))
      await updateAzureConnection({
        organization,
        project: selectedProject.name,
        areaPath,
        teamName: selectedTeam.name,
        isEnabled: true,
        projectId: selectedProject.id,
        teamId: selectedTeam.id,
      })
      if (selected.length > 0) {
        await importAzureTeam(selected)
      }
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save connection')
      setStep('members')
    } finally {
      setLoading(false)
    }
  }

  const headerRef = useRef<HTMLElement | null>(null)
  const projectsCardRef = useRef<HTMLDivElement | null>(null)
  const teamsCardRef = useRef<HTMLDivElement | null>(null)
  const areaPathCardRef = useRef<HTMLDivElement | null>(null)
  const membersCardRef = useRef<HTMLDivElement | null>(null)

  function scrollToCard(target: HTMLElement | null) {
    if (!target) return
    const headerHeight = headerRef.current?.offsetHeight ?? 0
    const rect = target.getBoundingClientRect()
    const y = rect.top + window.scrollY - headerHeight - 12
    window.scrollTo({ top: Math.max(0, y), behavior: 'smooth' })
  }

  // Auto-scroll as each step "unlocks" the next interactive card.
  useEffect(() => {
    if (projects.length === 0) return
    requestAnimationFrame(() => scrollToCard(projectsCardRef.current))
  }, [projects])

  useEffect(() => {
    if (step !== 'team') return
    if (teams.length === 0) return
    requestAnimationFrame(() => scrollToCard(teamsCardRef.current))
  }, [step, teams])

  useEffect(() => {
    if (step !== 'members') return
    if (users.length > 0) requestAnimationFrame(() => scrollToCard(membersCardRef.current))
    else if (selectedTeamId) requestAnimationFrame(() => scrollToCard(areaPathCardRef.current))
  }, [selectedTeamId, step, users])

  const stepLabel =
    step === 'saving'
      ? 'Saving'
      : step === 'project'
        ? 'Step 1/3 • Project'
        : step === 'team'
          ? 'Step 2/3 • Team'
          : 'Step 3/3 • Members'

  return (
    <div className="entryShell">
      <header className="entryHeader" ref={headerRef}>
        <div className="entryHeaderInner">
          <div className="pageHeader">
            <div className="pageHeaderLeft">
              <h2 className="pageHeaderTitle">Azure Setup</h2>
              <div className="pageHeaderSubtitle">
                Connect your Azure DevOps organization, choose a project and team, then import members.
              </div>
            </div>
            <div className="pageHeaderRight">
              <span className="pill toneInfo">
                <span className="btnContent">
                  {stepLabel}
                  {step === 'saving' ? <Spinner size="sm" inline label="Saving Azure connection" /> : null}
                </span>
              </span>
            </div>
          </div>
        </div>
      </header>

      <div className="page entryPage">

      <div className="card pad" style={{ marginBottom: 16 }}>
        <div className="fieldGrid2">
          <label className="field span2">
            <div className="fieldLabel setupOrgLabel">Organization (required)</div>
            <input
              className="input"
              value={organization}
              onChange={(e) => setOrganization(e.target.value)}
              placeholder="e.g. contoso"
              autoCapitalize="none"
              autoCorrect="off"
              spellCheck={false}
            />
            <div className="mutedSmall">This is the Azure DevOps organization name in your URL.</div>
          </label>
        </div>

        <div className="rowTiny" style={{ marginTop: 12, alignItems: 'center' }}>
          <LoadingButton
            className="btn btnWide"
            onClick={onLoadProjects}
            loading={loading}
            spinnerLabel="Loading projects"
            disabled={!organization.trim()}
          >
            Load projects
          </LoadingButton>
          <div className="mutedSmall">
            {organization.trim() ? (
              <>Next: choose a project and team.</>
            ) : (
              <>Enter an organization to continue.</>
            )}
          </div>
        </div>
      </div>

      {error ? <div className="card pad" style={{ marginBottom: 12 }}>Error: {error}</div> : null}

      {projects.length > 0 ? (
        <div className="card pad scrollCard" style={{ marginBottom: 16 }} ref={projectsCardRef}>
          <LoadingOverlay isLoading={loading && step === 'project'} label="Loading projects">
            <h3>Select project</h3>
            <label className="field" style={{ marginTop: 8 }}>
              <div className="fieldLabel">Search</div>
              <input
                className="input"
                value={projectQuery}
                onChange={(e) => setProjectQuery(e.target.value)}
                placeholder="Filter projects…"
              />
            </label>
            <div className="mutedSmall" style={{ marginTop: 8 }}>
              {selectedProjectId ? 'Project selected.' : 'Choose one project.'} • {filteredProjects.length} shown
            </div>
            <div className="list listCard" style={{ marginTop: 10, maxHeight: 260, overflow: 'auto' }}>
              {filteredProjects.map((p) => (
                <button
                  key={p.id}
                  type="button"
                  className={`listRow listRowBtn ${selectedProjectId === p.id ? 'listRowActive' : ''}`}
                  onClick={() => onSelectProject(p.id)}
                  disabled={loading}
                >
                  <div className="listMain">
                    <div className="listTitle">{p.name}</div>
                  </div>
                  {selectedProjectId === p.id ? <span className="pill toneInfo">Selected</span> : null}
                </button>
              ))}
              {filteredProjects.length === 0 ? <div className="muted pad">No projects match your search.</div> : null}
            </div>
          </LoadingOverlay>
        </div>
      ) : null}

      {teams.length > 0 ? (
        <div className="card pad scrollCard" style={{ marginBottom: 16 }} ref={teamsCardRef}>
          <LoadingOverlay isLoading={loading && step === 'team'} label="Loading teams">
            <h3>Select team</h3>
            <label className="field" style={{ marginTop: 8 }}>
              <div className="fieldLabel">Search</div>
              <input className="input" value={teamQuery} onChange={(e) => setTeamQuery(e.target.value)} placeholder="Filter teams…" />
            </label>
            <div className="mutedSmall" style={{ marginTop: 8 }}>
              {selectedTeamId ? 'Team selected.' : 'Choose one team.'} • {filteredTeams.length} shown
            </div>
            <div className="list listCard" style={{ marginTop: 10, maxHeight: 320, overflow: 'auto' }}>
              {filteredTeams.map((t) => (
                <button
                  key={t.id}
                  type="button"
                  className={`listRow listRowBtn ${selectedTeamId === t.id ? 'listRowActive' : ''}`}
                  onClick={() => onSelectTeam(t.id, t.name)}
                  disabled={loading}
                >
                  <div className="listMain">
                    <div className="listTitle">{t.name}</div>
                  </div>
                  {selectedTeamId === t.id ? <span className="pill toneInfo">Selected</span> : null}
                </button>
              ))}
              {filteredTeams.length === 0 ? <div className="muted pad">No teams match your search.</div> : null}
            </div>
          </LoadingOverlay>
        </div>
      ) : null}

      {selectedTeamId ? (
        <div className="card pad scrollCard" style={{ marginBottom: 16 }} ref={areaPathCardRef}>
          <LoadingOverlay isLoading={areaPathLoading} label="Loading area path" spinnerSize="sm">
            <h3>Default area path</h3>
            <label className="field" style={{ marginTop: 8 }}>
              <div className="fieldLabel">Resolved from team settings</div>
              <input
                className="input inputReadonly"
                value={areaPath || ''}
                readOnly
                placeholder="(not available)"
                title="Read-only (resolved from team settings)"
              />
              <div className="mutedSmall">
                This is the team’s default Area Path and will be saved with the connection.
                {areaPathError ? ` (Could not load default: ${areaPathError})` : ''}
                {' '}You can override it later in Settings.
              </div>
            </label>
          </LoadingOverlay>
        </div>
      ) : null}

      {users.length > 0 ? (
        <div className="card pad scrollCard" ref={membersCardRef}>
          <LoadingOverlay isLoading={step === 'saving'} label="Saving connection">
            <h3>Select members to import</h3>
            <label className="field" style={{ marginTop: 8 }}>
              <div className="fieldLabel">Search</div>
              <input
                className="input"
                value={memberQuery}
                onChange={(e) => setMemberQuery(e.target.value)}
                placeholder="Filter members…"
              />
            </label>
            <div className="rowTiny" style={{ marginTop: 10 }}>
              <button
                type="button"
                className="btn btnGhost"
                disabled={filteredUsers.length === 0}
                onClick={() => setSelectedUsers(new Set(filteredUsers.map((u) => u.uniqueName)))}
              >
                Select all shown
              </button>
              <button type="button" className="btn btnGhost" disabled={selectedUsers.size === 0} onClick={() => setSelectedUsers(new Set())}>
                Clear
              </button>
              <span className="mutedSmall" style={{ marginLeft: 'auto' }}>
                {selectedUsers.size} selected • {filteredUsers.length} shown
              </span>
            </div>
            <div className="list listCard" style={{ marginTop: 10, maxHeight: 360, overflow: 'auto' }}>
              {filteredUsers.map((u) => (
                <label
                  key={u.uniqueName}
                  className={`listRow listRowBtn ${selectedUsers.has(u.uniqueName) ? 'listRowActive' : ''}`}
                  style={{ cursor: 'pointer' }}
                >
                  <input type="checkbox" checked={selectedUsers.has(u.uniqueName)} onChange={(e) => toggleUser(u.uniqueName, e.target.checked)} />
                  <div className="listMain">
                    <div className="listTitle">{u.displayName || u.uniqueName}</div>
                    {u.displayName ? <div className="listMeta">{u.uniqueName}</div> : null}
                  </div>
                </label>
              ))}
              {filteredUsers.length === 0 ? <div className="muted pad">No members match your search.</div> : null}
            </div>

            <div className="row" style={{ marginTop: 12 }}>
              <LoadingButton
                className="btn"
                onClick={onSave}
                loading={step === 'saving'}
                spinnerLabel="Saving connection"
                disabled={loading}
              >
                Save connection
              </LoadingButton>
            </div>
          </LoadingOverlay>
        </div>
      ) : null}
      </div>
    </div>
  )
}
