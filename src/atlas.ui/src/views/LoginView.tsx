import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { getAzureConnection } from '../app/api/azureDevOps'
import { LoadingButton } from '../components/LoadingButton'
import { LoadingOverlay } from '../components/LoadingOverlay'

export function LoginView() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function onContinue() {
    setLoading(true)
    setError(null)
    try {
      // Probe API connectivity. Azure is optional — missing connection is not an error.
      await getAzureConnection()
      navigate('/dashboard')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reach the Atlas API')
      setLoading(false)
    }
  }

  return (
    <div className="page">
      <h2 className="pageTitle">Login</h2>
      <div className="card pad">
        <LoadingOverlay isLoading={loading} label="Checking connection">
          <p className="muted">
            Authentication will be added later. Continue opens the app. Azure DevOps setup is optional.
          </p>
          {error ? <div className="muted" style={{ marginBottom: 8 }}>Error: {error}</div> : null}
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', alignItems: 'center' }}>
            <LoadingButton className="btn" onClick={onContinue} loading={loading} spinnerLabel="Checking connection">
              Continue
            </LoadingButton>
            <Link className="btn btnSecondary" to="/setup">
              Azure Setup
            </Link>
          </div>
        </LoadingOverlay>
      </div>
    </div>
  )
}
