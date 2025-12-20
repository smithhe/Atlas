import { useAi } from '../app/state/AiState'

export function AiPanel() {
  const ai = useAi()
  const { actions, contextTitle, isOpen, output } = ai.state

  if (!isOpen) return null

  async function copyToClipboard() {
    try {
      await navigator.clipboard.writeText(output)
      ai.appendOutput('\n(Copied to clipboard)\n')
    } catch {
      ai.appendOutput('\n(Copy failed — clipboard permission unavailable)\n')
    }
  }

  return (
    <aside className="aiPanel" aria-label="AI panel">
      <div className="aiPanelHeader">
        <div className="aiPanelTitle">{contextTitle}</div>
        <button className="btn btnGhost" onClick={() => ai.setIsOpen(false)}>
          Close
        </button>
      </div>

      <div className="aiPanelActions">
        {actions.length === 0 ? (
          <div className="muted">No actions for this context yet.</div>
        ) : (
          actions.map((a) => (
            <button key={a.id} className="btn btnSecondary" onClick={() => ai.runAction(a.id)}>
              {a.label}
            </button>
          ))
        )}
      </div>

      <div className="aiPanelOutput" role="region" aria-label="AI output">
        <pre className="aiPanelOutputPre">{output || ' '}</pre>
      </div>

      <div className="aiPanelFooter">
        <button className="btn" onClick={() => ai.appendOutput('\n(Insert Draft: placeholder)\n')}>
          Insert Draft
        </button>
        <button className="btn btnSecondary" onClick={copyToClipboard}>
          Copy
        </button>
        <button className="btn btnGhost" onClick={() => ai.clearOutput()}>
          Clear
        </button>
      </div>
    </aside>
  )
}


