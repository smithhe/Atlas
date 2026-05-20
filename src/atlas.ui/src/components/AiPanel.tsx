import { useEffect, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { Markdown } from './Markdown'

function CopyIcon() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <rect x="5" y="5" width="9" height="9" rx="1.5" stroke="currentColor" strokeWidth="1.5" />
      <path d="M4 11H3.5A1.5 1.5 0 0 1 2 9.5v-7A1.5 1.5 0 0 1 3.5 1H10.5A1.5 1.5 0 0 1 12 2.5V3" stroke="currentColor" strokeWidth="1.5" />
    </svg>
  )
}

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
    notice,
    promptDraft,
    sessions,
    status,
    turns,
  } = ai.state

  const scrollRef = useRef<HTMLDivElement | null>(null)
  const [copiedTurnId, setCopiedTurnId] = useState<string | null>(null)

  const lastTurn = turns.length > 0 ? turns[turns.length - 1] : undefined
  const shouldStickToBottom = isRunning || (lastTurn !== undefined && !lastTurn.response.trim())

  useEffect(() => {
    if (!shouldStickToBottom) return
    const el = scrollRef.current
    if (!el) return
    el.scrollTop = el.scrollHeight
  }, [turns, shouldStickToBottom])

  if (!isOpen) return null

  async function copyTurnResponse(turnId: string, text: string) {
    if (!text.trim()) return
    try {
      await navigator.clipboard.writeText(text)
      setCopiedTurnId(turnId)
      window.setTimeout(() => setCopiedTurnId((prev) => (prev === turnId ? null : prev)), 1500)
    } catch {
      ai.appendOutput('Copy failed — clipboard permission unavailable.')
    }
  }

  return (
    <aside className="aiPanel" aria-label="AI panel">
      <div className="aiPanelHeader">
        <div className="aiPanelTitle">{contextTitle}</div>
        <div className="aiPanelHeaderActions">
          <button className="btn btnSecondary" onClick={() => ai.appendOutput('\n(Insert Draft action can be wired next.)\n')}>
            Insert Draft
          </button>
          <button className="btn btnGhost" onClick={() => ai.clearOutput()}>
            Clear
          </button>
          <button className="btn btnGhost" onClick={() => ai.setIsOpen(false)}>
            Close
          </button>
        </div>
      </div>

      <div className="aiPanelBody">
        <div className="aiPanelMeta">
          <div className="mutedSmall">
            Status: {status}{isRunning ? ' (running)' : ''}
          </div>
          {contextSupportMessage ? <div className="mutedSmall">{contextSupportMessage}</div> : null}
          {notice ? <div className="aiPanelNotice">{notice}</div> : null}
        </div>

        <div ref={scrollRef} className="aiPanelScroll" role="log" aria-label="AI conversation" aria-live="polite">
          {turns.length === 0 ? (
            <div className="aiPanelEmpty muted">Ask a question or choose an action to get started.</div>
          ) : (
            turns.map((turn, index) => {
              const isLast = index === turns.length - 1
              const isThinking = isLast && isRunning && !turn.response.trim()
              return (
                <article key={turn.id} className="aiPanelTurn">
                  <div className="aiPanelTurnPrompt">{turn.prompt}</div>
                  <div className="aiPanelTurnResponse">
                    {isThinking ? (
                      <div className="aiPanelThinking muted">Thinking…</div>
                    ) : turn.response.trim() ? (
                      <>
                        <Markdown text={turn.response} />
                        <button
                          type="button"
                          className={`btn btnGhost btnIcon aiPanelCopyBtn${copiedTurnId === turn.id ? ' aiPanelCopyBtnCopied' : ''}`}
                          title={copiedTurnId === turn.id ? 'Copied' : 'Copy response'}
                          aria-label={copiedTurnId === turn.id ? 'Copied' : 'Copy response'}
                          onClick={() => void copyTurnResponse(turn.id, turn.response)}
                        >
                          <CopyIcon />
                        </button>
                      </>
                    ) : null}
                  </div>
                </article>
              )
            })
          )}
        </div>

        {actions.length > 0 ? (
          <div className="aiPanelQuickActions">
            {actions.map((a) => (
              <button key={a.id} className="btn btnSecondary" disabled={isRunning || !isContextSupported} onClick={() => ai.runAction(a.id)}>
                {a.label}
              </button>
            ))}
          </div>
        ) : null}

        <div className="aiPanelComposer">
          <textarea
            className="textarea aiPanelPrompt"
            value={promptDraft}
            onChange={(e) => ai.setPromptDraft(e.target.value)}
            placeholder="Ask AI about this context..."
            rows={3}
          />
          <button
            className="btn aiPanelSend"
            disabled={isRunning || !isContextSupported || !promptDraft.trim()}
            onClick={() => {
              ai.sendPrompt(promptDraft)
              ai.setPromptDraft('')
            }}
          >
            Send Prompt
          </button>
        </div>

        {sessions.length > 0 ? (
          <div className="aiPanelHistory">
            <label className="aiPanelHistoryLabel mutedSmall" htmlFor="ai-session-select">
              Session history
            </label>
            <div className="aiPanelHistoryRow">
              <select
                id="ai-session-select"
                className="input aiPanelHistorySelect"
                value={activeSessionId ?? ''}
                disabled={isLoadingHistory || isRunning}
                onChange={(e) => {
                  if (e.target.value) ai.openSession(e.target.value)
                }}
              >
                <option value="">Recent sessions…</option>
                {sessions.map((session) => (
                  <option key={session.sessionId} value={session.sessionId}>
                    {new Date(session.createdAtUtc).toLocaleString()} — {session.title}
                  </option>
                ))}
              </select>
              <button className="btn btnSecondary aiPanelHistoryRefresh" disabled={isLoadingHistory} onClick={() => ai.loadSessions()}>
                Refresh
              </button>
            </div>
          </div>
        ) : null}
      </div>
    </aside>
  )
}
