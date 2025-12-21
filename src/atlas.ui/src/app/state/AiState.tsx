import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'

export interface AiAction {
  id: string
  label: string
  description?: string
}

export interface AiState {
  isOpen: boolean
  contextTitle: string
  actions: AiAction[]
  output: string
  panelWidthPx?: number
}

export interface AiApi {
  state: AiState
  setIsOpen: (isOpen: boolean) => void
  setPanelWidthPx: (px: number | undefined) => void
  setContext: (contextTitle: string, actions: AiAction[]) => void
  runAction: (actionId: string) => void
  clearOutput: () => void
  appendOutput: (text: string) => void
}

const AiContext = createContext<AiApi | undefined>(undefined)

export function AiProvider({ children }: { children: ReactNode }) {
  const [isOpen, setIsOpen] = useState<boolean>(true)
  const [contextTitle, setContextTitle] = useState<string>('Context: Dashboard')
  const [actions, setActions] = useState<AiAction[]>([])
  const [output, setOutput] = useState<string>('AI is placeholder-only. Choose an action to generate a draft.\n')
  const [panelWidthPx, setPanelWidthPx] = useState<number | undefined>(undefined)

  // Keep latest values accessible to stable callbacks.
  const actionsRef = useRef<AiAction[]>(actions)
  const contextTitleRef = useRef<string>(contextTitle)

  useEffect(() => {
    actionsRef.current = actions
    contextTitleRef.current = contextTitle
  }, [actions, contextTitle])

  const setContext = useCallback((newContextTitle: string, newActions: AiAction[]) => {
    setContextTitle(newContextTitle)
    setActions(newActions)
  }, [])

  const clearOutput = useCallback(() => setOutput(''), [])
  const appendOutput = useCallback((text: string) => setOutput((prev) => prev + text), [])

  const runAction = useCallback((actionId: string) => {
    const currentActions = actionsRef.current
    const currentContext = contextTitleRef.current
    const action = currentActions.find((a) => a.id === actionId)
    const stamp = new Date().toLocaleString()
    const label = action?.label ?? actionId
    setIsOpen(true)
    setOutput((prev) => {
      const header = `\n---\n${stamp}\n${currentContext}\nAction: ${label}\n`
      const body = 'Draft (placeholder):\n- Summary: ...\n- Suggested next steps: ...\n- Risks/tradeoffs: ...\n'
      return prev + header + body
    })
  }, [])

  const api = useMemo<AiApi>(
    () => ({
      state: { isOpen, contextTitle, actions, output, panelWidthPx },
      setIsOpen,
      setPanelWidthPx,
      setContext,
      runAction,
      clearOutput,
      appendOutput,
    }),
    [actions, appendOutput, clearOutput, contextTitle, isOpen, output, panelWidthPx, runAction, setContext],
  )

  return <AiContext.Provider value={api}>{children}</AiContext.Provider>
}

export function useAi(): AiApi {
  const ctx = useContext(AiContext)
  if (!ctx) throw new Error('useAi must be used within AiProvider')
  return ctx
}


