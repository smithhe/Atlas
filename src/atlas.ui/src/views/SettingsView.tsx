import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState } from '../app/state/AppState'
import { getAzureConnection, getAzureSyncState, runAzureSync, updateAzureConnection } from '../app/api/azureDevOps'
import type { AzureConnectionDto, AzureSyncStateDto } from '../app/api/azureDevOps'
import { updateSettings } from '../app/api/settings'
import { LoadingButton } from '../components/LoadingButton'
import { LoadingOverlay } from '../components/LoadingOverlay'

export function SettingsView() {
  const ai = useAi()
  const navigate = useNavigate()
  const dispatch = useAppDispatch()
  const { settings } = useAppState()
  const [settingsSaving, setSettingsSaving] = useState(false)
  const [settingsError, setSettingsError] = useState<string | null>(null)

  const [azureConnection, setAzureConnection] = useState<AzureConnectionDto>({
    organization: '',
    project: '',
    areaPath: '',
    teamName: '',
    isEnabled: true,
    projectId: '',
    teamId: '',
  })
  const [azureLoaded, setAzureLoaded] = useState(false)
  const [azureConnectionLoading, setAzureConnectionLoading] = useState(false)
  const [azureConnectionSaving, setAzureConnectionSaving] = useState(false)
  const [azureError, setAzureError] = useState<string | null>(null)
  const [syncState, setSyncState] = useState<AzureSyncStateDto | null>(null)
  const [syncRunning, setSyncRunning] = useState(false)
  const [syncStateLoading, setSyncStateLoading] = useState(false)

  useEffect(() => {
    ai.setContext('Context: Settings', [{ id: 'settings-help', label: 'Explain settings (placeholder)' }])
  }, [ai.setContext])

  useEffect(() => {
    let mounted = true
    setAzureConnectionLoading(true)
    getAzureConnection()
      .then((conn) => {
        if (!mounted) return
        if (conn) setAzureConnection(conn)
        setAzureLoaded(true)
      })
      .catch((err) => {
        if (!mounted) return
        setAzureError(err instanceof Error ? err.message : 'Failed to load Azure connection')
        setAzureLoaded(true)
      })
      .finally(() => {
        if (mounted) setAzureConnectionLoading(false)
      })

    setSyncStateLoading(true)
    getAzureSyncState()
      .then((state) => {
        if (mounted) setSyncState(state)
      })
      .catch(() => {
        if (mounted) setSyncState(null)
      })
      .finally(() => {
        if (mounted) setSyncStateLoading(false)
      })

    return () => {
      mounted = false
    }
  }, [])

  async function onSaveSettings() {
    setSettingsSaving(true)
    setSettingsError(null)
    try {
      await updateSettings(settings)
    } catch (err) {
      setSettingsError(err instanceof Error ? err.message : 'Failed to save settings')
    } finally {
      setSettingsSaving(false)
    }
  }

  async function onSaveAzureConnection() {
    setAzureError(null)
    if (!azureConnection.projectId.trim() || !azureConnection.teamId.trim()) {
      setAzureError('Project ID and Team ID are required. Use Azure Setup to select a project and team.')
      return
    }
    setAzureConnectionSaving(true)
    try {
      await updateAzureConnection(azureConnection)
      setAzureLoaded(true)
    } catch (err) {
      setAzureError(err instanceof Error ? err.message : 'Failed to save Azure connection')
    } finally {
      setAzureConnectionSaving(false)
    }
  }

  async function onSyncNow() {
    setSyncRunning(true)
    setAzureError(null)
    try {
      await runAzureSync()
      setSyncStateLoading(true)
      const state = await getAzureSyncState()
      setSyncState(state)
    } catch (err) {
      setAzureError(err instanceof Error ? err.message : 'Failed to run Azure sync')
    } finally {
      setSyncRunning(false)
      setSyncStateLoading(false)
    }
  }

  return (
    <div className="page">
      <h2 className="pageTitle">Settings</h2>

      <div className="card pad">
        <div className="fieldGrid2">
          <label className="field">
            <div className="fieldLabel">Stale threshold (days)</div>
            <input
              className="input"
              type="number"
              min={1}
              value={settings.staleDays}
              onChange={(e) =>
                dispatch({
                  type: 'updateSettings',
                  settings: { ...settings, staleDays: clampInt(e.target.value, 1, 365) },
                })
              }
            />
          </label>

          <label className="field">
            <div className="fieldLabel">Theme</div>
            <input className="input" value="Dark (locked)" readOnly />
          </label>

          <label className="field span2">
            <div className="fieldLabel">Default AI behavior</div>
            <div className="placeholderBox">Manual only (AI acts only when you click an action)</div>
          </label>

          <label className="field span2">
            <div className="fieldLabel">Keyboard shortcuts cheat sheet</div>
            <div className="placeholderBox">
              - Tab / Shift+Tab: move focus
              {'\n'}- Enter: activate buttons
              {'\n'}- Command palette: (coming later)
            </div>
          </label>

          <label className="field span2">
            <div className="fieldLabel">Azure DevOps base URL</div>
            <input
              className="input"
              value={settings.azureDevOpsBaseUrl ?? ''}
              onChange={(e) =>
                dispatch({
                  type: 'updateSettings',
                  settings: { ...settings, azureDevOpsBaseUrl: e.target.value },
                })
              }
            />
          </label>
        </div>

        <div className="row" style={{ marginTop: 16 }}>
          <LoadingButton className="btn" onClick={onSaveSettings} loading={settingsSaving} spinnerLabel="Saving settings">
            Save settings
          </LoadingButton>
          {settingsError ? <div className="muted" style={{ marginLeft: 12 }}>{settingsError}</div> : null}
        </div>
      </div>

      <div className="card pad" style={{ marginTop: 16 }}>
        <LoadingOverlay isLoading={azureConnectionLoading} label="Loading Azure DevOps connection">
          <div className="rowTiny" style={{ alignItems: 'center', marginBottom: 8 }}>
            <h3 style={{ margin: 0 }}>Azure DevOps</h3>
          </div>
          <div className="fieldGrid2">
            <label className="field">
              <div className="fieldLabel">Organization</div>
              <input
                className="input"
                value={azureConnection.organization}
                onChange={(e) => setAzureConnection({ ...azureConnection, organization: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Project</div>
              <input
                className="input"
                value={azureConnection.project}
                onChange={(e) => setAzureConnection({ ...azureConnection, project: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Area path</div>
              <input
                className="input"
                value={azureConnection.areaPath}
                onChange={(e) => setAzureConnection({ ...azureConnection, areaPath: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Team (optional)</div>
              <input
                className="input"
                value={azureConnection.teamName ?? ''}
                onChange={(e) => setAzureConnection({ ...azureConnection, teamName: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Project ID</div>
              <input
                className="input"
                value={azureConnection.projectId ?? ''}
                onChange={(e) => setAzureConnection({ ...azureConnection, projectId: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Team ID</div>
              <input
                className="input"
                value={azureConnection.teamId ?? ''}
                onChange={(e) => setAzureConnection({ ...azureConnection, teamId: e.target.value })}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Enabled</div>
              <select
                className="input"
                value={azureConnection.isEnabled ? 'yes' : 'no'}
                onChange={(e) => setAzureConnection({ ...azureConnection, isEnabled: e.target.value === 'yes' })}
              >
                <option value="yes">Yes</option>
                <option value="no">No</option>
              </select>
            </label>
          </div>

        <div className="rowTiny" style={{ marginTop: 16, alignItems: 'center', flexWrap: 'wrap' }}>
          <LoadingButton
            className="btn btnWide"
            onClick={onSaveAzureConnection}
            loading={azureConnectionSaving}
            spinnerLabel="Saving Azure DevOps connection"
            disabled={!azureLoaded || !azureConnection.projectId.trim() || !azureConnection.teamId.trim()}
          >
            Save Azure connection
          </LoadingButton>
          <LoadingButton
            className="btn btnSecondary btnWide"
            onClick={onSyncNow}
            loading={syncRunning}
            spinnerLabel="Running sync"
            disabled={!azureLoaded}
          >
            Sync now
          </LoadingButton>
          <button
            type="button"
            className="btn btnSecondary btnWide"
            onClick={() => navigate('/settings/azure-import')}
          >
            Open Azure Import
          </button>
        </div>
        {azureLoaded && (!azureConnection.projectId.trim() || !azureConnection.teamId.trim()) ? (
          <div className="mutedSmall textBad" style={{ marginTop: 8 }}>
            Azure connection needs Project ID and Team ID. Use <Link to="/setup">Azure Setup</Link>.
          </div>
        ) : null}
        {azureError ? (
          <div className="mutedSmall textBad" style={{ marginTop: 6 }}>
            {azureError}
          </div>
        ) : null}

          <LoadingOverlay isLoading={syncStateLoading} label="Loading sync history" spinnerSize="sm">
            {syncState ? (
              <div className="muted" style={{ marginTop: 12 }}>
                Last sync: {syncState.lastCompletedAtUtc ?? 'Never'} • Status: {syncState.lastRunStatus}
                {syncState.lastError ? ` • Error: ${syncState.lastError}` : ''}
              </div>
            ) : (
              <div className="muted" style={{ marginTop: 12 }}>No sync history yet.</div>
            )}
          </LoadingOverlay>
        </LoadingOverlay>
      </div>
    </div>
  )
}

function clampInt(value: string, min: number, max: number) {
  const n = Number.parseInt(value, 10)
  if (Number.isNaN(n)) return min
  return Math.max(min, Math.min(max, n))
}


