import { useEffect } from 'react'
import { useAi } from '../app/state/AiState'

export function NotFoundView() {
  const ai = useAi()
  useEffect(() => {
    ai.setContext('Context: Unknown', [{ id: 'ai-help', label: 'Help (placeholder)' }])
  }, [ai.setContext])

  return (
    <div className="page">
      <h2 className="pageTitle">Not Found</h2>
      <div className="muted">That view doesn’t exist.</div>
    </div>
  )
}


