import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getAzureConnection } from '../app/api/azureDevOps'

export function LoginView() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function onContinue() {
    setLoading(true)
    setError(null)
    try {
      const conn = await getAzureConnection()
      if (conn && conn.projectId && conn.teamId) {
        navigate('/dashboard')
      } else {
        navigate('/setup')
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to check connection')
      setLoading(false)
    }
  }

  return (
    <div className="page">
      <h2 className="pageTitle">Login</h2>
      <div className="card pad">
        <p className="muted">Authentication will be added later. Continue to setup for now.</p>
        {error ? <div className="muted" style={{ marginBottom: 8 }}>Error: {error}</div> : null}
        <button className="btn" onClick={onContinue} disabled={loading}>
          {loading ? 'Checking…' : 'Continue'}
        </button>
      </div>
    </div>
  )
}
