import { useEffect } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState } from '../app/state/AppState'

export function SettingsView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const { settings } = useAppState()

  useEffect(() => {
    ai.setContext('Context: Settings', [{ id: 'settings-help', label: 'Explain settings (placeholder)' }])
  }, [ai.setContext])

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
      </div>
    </div>
  )
}

function clampInt(value: string, min: number, max: number) {
  const n = Number.parseInt(value, 10)
  if (Number.isNaN(n)) return min
  return Math.max(min, Math.min(max, n))
}


