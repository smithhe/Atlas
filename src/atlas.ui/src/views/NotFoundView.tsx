import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useAi } from '../app/state/AiState'

export function NotFoundView() {
  const ai = useAi()
  useEffect(() => {
    ai.setContext('Context: Unknown', [])
  }, [ai.setContext])

  return (
    <div className="page">
      <h2 className="pageTitle">Not Found</h2>
      <div className="muted">That view doesn’t exist.</div>
      <div className="row mt-lg">
        <Link className="btn btnSecondary" to="/dashboard">
          Back to Dashboard
        </Link>
      </div>
    </div>
  )
}
