import { useAi } from '../app/state/AiState'

export function AiPanel() {
  const ai = useAi()
  const {
    actions,
    activeSessionId,
    contextSupportMessage,
    contextTitle,
    isContextSupported,
    isLoadingHistory,
    isOpen,
    isRunning,
    output,
    promptDraft,
    sessions,
    status,
  } = ai.state

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

      <div className="mutedSmall" style={{ padding: '0 12px 6px 12px' }}>
        Status: {status}{isRunning ? ' (running)' : ''}
      </div>

      {contextSupportMessage ? (
        <div className="mutedSmall" style={{ padding: '0 12px 6px 12px' }}>
          {contextSupportMessage}
        </div>
      ) : null}

      {sessions.length > 0 ? (
        <div className="aiPanelActions" style={{ paddingTop: 0 }}>
          <select
            className="input"
            value={activeSessionId ?? ''}
            disabled={isLoadingHistory || isRunning}
            onChange={(e) => {
              if (e.target.value) ai.openSession(e.target.value)
            }}
          >
            <option value="">Recent AI sessions...</option>
            {sessions.map((session) => (
              <option key={session.sessionId} value={session.sessionId}>
                {new Date(session.createdAtUtc).toLocaleString()} - {session.title}
              </option>
            ))}
          </select>
          <button className="btn btnSecondary" disabled={isLoadingHistory} onClick={() => ai.loadSessions()}>
            Refresh History
          </button>
        </div>
      ) : null}

      <div className="aiPanelActions" style={{ paddingTop: 0 }}>
        <textarea
          className="textarea"
          value={promptDraft}
          onChange={(e) => ai.setPromptDraft(e.target.value)}
          placeholder="Ask AI about this context..."
        />
        <button
          className="btn"
          disabled={isRunning || !isContextSupported || !promptDraft.trim()}
          onClick={() => {
            ai.sendPrompt(promptDraft)
            ai.setPromptDraft('')
          }}
        >
          Send Prompt
        </button>
      </div>

      <div className="aiPanelActions">
        {actions.length === 0 ? (
          <div className="muted">No actions for this context yet.</div>
        ) : (
          actions.map((a) => (
            <button key={a.id} className="btn btnSecondary" disabled={isRunning || !isContextSupported} onClick={() => ai.runAction(a.id)}>
              {a.label}
            </button>
          ))
        )}
      </div>

      <div className="aiPanelOutput" role="region" aria-label="AI output">
        <pre className="aiPanelOutputPre">{output || ' '}</pre>
      </div>

      <div className="aiPanelFooter">
        <button className="btn" onClick={() => ai.appendOutput('\n(Insert Draft action can be wired next.)\n')}>
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


