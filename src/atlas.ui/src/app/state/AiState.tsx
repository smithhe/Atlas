import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useAppHydration, useAppState } from './AppState'
import {
  getAiSession,
  listAiSessions,
  openAiSessionEvents,
  startAiSession,
  type AiSessionEventDto,
  type AiSessionListItemDto,
  type AiView,
} from '../api/ai'

interface AiAction {
  id: string
  label: string
  description?: string
}

interface AiState {
  isOpen: boolean
  contextTitle: string
  actions: AiAction[]
  sessions: AiSessionListItemDto[]
  output: string
  status: string
  isRunning: boolean
  isLoadingHistory: boolean
  activeSessionId?: string
  promptDraft: string
  isContextSupported: boolean
  contextSupportMessage?: string
  panelWidthPx?: number
}

interface AiApi {
  state: AiState
  setIsOpen: (isOpen: boolean) => void
  setPanelWidthPx: (px: number | undefined) => void
  setContext: (contextTitle: string, actions: AiAction[]) => void
  setPromptDraft: (text: string) => void
  loadSessions: () => void
  openSession: (sessionId: string) => void
  runAction: (actionId: string, promptOverride?: string) => void
  sendPrompt: (prompt: string) => void
  clearOutput: () => void
  appendOutput: (text: string) => void
}

const AiContext = createContext<AiApi | undefined>(undefined)

export function AiProvider({ children }: { children: ReactNode }) {
  const { settings, selectedTaskId, selectedProjectId, selectedRiskId, selectedTeamMemberId } = useAppState()
  const isHydrating = useAppHydration()
  const [isOpen, setIsOpenState] = useState<boolean>(false)
  const [contextTitle, setContextTitle] = useState<string>('Context: Dashboard')
  const [actions, setActions] = useState<AiAction[]>([])
  const [events, setEvents] = useState<AiSessionEventDto[]>([])
  const [localOutput, setLocalOutput] = useState<string>('AI ready.\n')
  const [sessions, setSessions] = useState<AiSessionListItemDto[]>([])
  const [status, setStatus] = useState<string>('Idle')
  const [isRunning, setIsRunning] = useState<boolean>(false)
  const [isLoadingHistory, setIsLoadingHistory] = useState<boolean>(false)
  const [activeSessionId, setActiveSessionId] = useState<string | undefined>(undefined)
  const [promptDraft, setPromptDraft] = useState<string>('')
  const [panelWidthPx, setPanelWidthPx] = useState<number | undefined>(undefined)
  const userChangedIsOpenRef = useRef(false)
  const appliedStartupPreferenceRef = useRef(false)
  const eventSourceRef = useRef<EventSource | null>(null)

  // Keep latest values accessible to stable callbacks.
  const actionsRef = useRef<AiAction[]>(actions)
  const contextTitleRef = useRef<string>(contextTitle)

  useEffect(() => {
    actionsRef.current = actions
    contextTitleRef.current = contextTitle
  }, [actions, contextTitle])

  useEffect(() => {
    if (isHydrating || appliedStartupPreferenceRef.current || userChangedIsOpenRef.current) return
    setIsOpenState(settings.defaultAiPanelOpen)
    appliedStartupPreferenceRef.current = true
  }, [isHydrating, settings.defaultAiPanelOpen])

  const setIsOpen = useCallback((nextIsOpen: boolean) => {
    userChangedIsOpenRef.current = true
    setIsOpenState(nextIsOpen)
  }, [])

  const setContext = useCallback((newContextTitle: string, newActions: AiAction[]) => {
    setContextTitle(newContextTitle)
    setActions(newActions)
  }, [])

  const output = useMemo(() => {
    const renderedEvents = renderEvents(events)
    return `${renderedEvents}${localOutput}`
  }, [events, localOutput])

  const clearOutput = useCallback(() => {
    setEvents([])
    setLocalOutput('')
  }, [])

  const appendOutput = useCallback((text: string) => setLocalOutput((prev) => prev + text), [])

  const closeStream = useCallback(() => {
    eventSourceRef.current?.close()
    eventSourceRef.current = null
  }, [])

  useEffect(() => {
    return () => {
      closeStream()
    }
  }, [closeStream])

  const resolveView = useCallback((title: string): AiView | undefined => {
    const lower = title.toLowerCase()
    if (lower.includes('tasks')) return 'Tasks'
    if (lower.includes('dashboard')) return 'Dashboard'
    return undefined
  }, [])

  const resolvedView = resolveView(contextTitle)
  const isContextSupported = Boolean(resolvedView)
  const contextSupportMessage = isContextSupported ? undefined : 'AI context is currently available for Dashboard and Tasks.'

  const refreshSessions = useCallback(async () => {
    try {
      const recent = await listAiSessions(25)
      setSessions(recent)
    } catch {
      // History is useful but non-critical; active prompts can still run.
    }
  }, [])

  const onSessionEvent = useCallback((evt: AiSessionEventDto) => {
    setEvents((prev) => mergeEvent(prev, evt))

    if (evt.status) {
      if (evt.status === 'gathering_context') setStatus('Gathering context...')
      else if (evt.status === 'model_requested') setStatus('Calling model...')
      else if (evt.status === 'streaming') setStatus('Streaming response...')
      else if (evt.status === 'completed') setStatus('Completed')
      else if (evt.status === 'failed') setStatus('Failed')
      else if (evt.status === 'cancelled') setStatus('Cancelled')
      else setStatus(evt.status)
    }

    if (evt.isTerminal) {
      setIsRunning(false)
      closeStream()
      void refreshSessions()
    }
  }, [closeStream, refreshSessions])

  const connectStream = useCallback((sessionId: string) => {
    closeStream()

    const es = openAiSessionEvents(sessionId)
    eventSourceRef.current = es

    const processPayload = (raw: string) => {
      try {
        const parsed = JSON.parse(raw) as AiSessionEventDto
        onSessionEvent(parsed)
      } catch {
        // Ignore malformed event payloads.
      }
    }

    es.onmessage = (event) => processPayload(event.data)
    es.addEventListener('session.started', (event) => processPayload((event as MessageEvent).data))
    es.addEventListener('context.gathering', (event) => processPayload((event as MessageEvent).data))
    es.addEventListener('model.requested', (event) => processPayload((event as MessageEvent).data))
    es.addEventListener('model.delta', (event) => processPayload((event as MessageEvent).data))
    es.addEventListener('session.completed', (event) => processPayload((event as MessageEvent).data))
    es.addEventListener('session.failed', (event) => processPayload((event as MessageEvent).data))
    es.onerror = () => {
      setStatus('Failed')
      setIsRunning(false)
      closeStream()
    }
  }, [closeStream, onSessionEvent])

  const loadSessions = useCallback(() => {
    void refreshSessions()
  }, [refreshSessions])

  useEffect(() => {
    if (isOpen) {
      void refreshSessions()
    }
  }, [isOpen, refreshSessions])

  const openSession = useCallback((sessionId: string) => {
    void (async () => {
      closeStream()
      setIsLoadingHistory(true)
      setStatus('Loading history...')
      setIsRunning(false)

      try {
        const session = await getAiSession(sessionId)
        setActiveSessionId(session.sessionId)
        setEvents(sortEvents(session.events))
        setLocalOutput('')
        setStatus(toDisplayStatus(session.status))
        if (!session.isTerminal) {
          setIsRunning(true)
          setStatus('Reconnecting to stream...')
          connectStream(session.sessionId)
        }
      } catch (err) {
        setStatus('Failed')
        setLocalOutput((prev) => `${prev}${prev.endsWith('\n') ? '' : '\n'}${err instanceof Error ? err.message : 'Failed to load AI session'}\n`)
      } finally {
        setIsLoadingHistory(false)
      }
    })()
  }, [closeStream, connectStream])

  const startSession = useCallback(async (prompt: string, actionId?: string) => {
    const trimmedPrompt = prompt.trim()
    if (!trimmedPrompt) return

    const view = resolveView(contextTitleRef.current)
    if (!view) {
      setIsOpenState(true)
      setStatus('Unsupported context')
      setLocalOutput((prev) => `${prev}${prev.endsWith('\n') ? '' : '\n'}AI context is currently available for Dashboard and Tasks.\n`)
      return
    }

    setIsOpenState(true)
    setIsRunning(true)
    setStatus('Starting...')
    setEvents([])
    setLocalOutput(`---\n${new Date().toLocaleString()}\n${contextTitleRef.current}\n`)

    closeStream()

    try {
      const res = await startAiSession({
        prompt: trimmedPrompt,
        view,
        actionId,
        taskId: selectedTaskId,
        projectId: selectedProjectId,
        riskId: selectedRiskId,
        teamMemberId: selectedTeamMemberId,
      })

      setActiveSessionId(res.sessionId)
      setStatus('Connecting to stream...')
      await refreshSessions()
      connectStream(res.sessionId)
    } catch (err) {
      setStatus('Failed')
      setIsRunning(false)
      setLocalOutput((prev) => `${prev}${prev.endsWith('\n') ? '' : '\n'}${err instanceof Error ? err.message : 'Failed to start AI session'}\n`)
    }
  }, [closeStream, connectStream, refreshSessions, resolveView, selectedProjectId, selectedRiskId, selectedTaskId, selectedTeamMemberId])

  const runAction = useCallback((actionId: string, promptOverride?: string) => {
    const action = actionsRef.current.find((a) => a.id === actionId)
    const prompt = promptOverride?.trim() || `Please help with this action: ${action?.label ?? actionId}`
    void startSession(prompt, actionId)
  }, [startSession])

  const sendPrompt = useCallback((prompt: string) => {
    void startSession(prompt)
  }, [startSession])

  const api = useMemo<AiApi>(
    () => ({
      state: {
        isOpen,
        contextTitle,
        actions,
        sessions,
        output,
        status,
        isRunning,
        isLoadingHistory,
        activeSessionId,
        promptDraft,
        isContextSupported,
        contextSupportMessage,
        panelWidthPx,
      },
      setIsOpen,
      setPanelWidthPx,
      setContext,
      setPromptDraft,
      loadSessions,
      openSession,
      runAction,
      sendPrompt,
      clearOutput,
      appendOutput,
    }),
    [actions, activeSessionId, appendOutput, clearOutput, contextSupportMessage, contextTitle, isContextSupported, isLoadingHistory, isOpen, isRunning, loadSessions, openSession, output, panelWidthPx, promptDraft, runAction, sendPrompt, sessions, setContext, setIsOpen, status],
  )

  return <AiContext.Provider value={api}>{children}</AiContext.Provider>
}

function sortEvents(events: AiSessionEventDto[]): AiSessionEventDto[] {
  return [...events].sort((a, b) => a.sequence - b.sequence)
}

function mergeEvent(events: AiSessionEventDto[], evt: AiSessionEventDto): AiSessionEventDto[] {
  const existingIdx = events.findIndex((e) => e.eventId === evt.eventId)
  if (existingIdx >= 0) {
    const next = [...events]
    next[existingIdx] = evt
    return sortEvents(next)
  }

  return sortEvents([...events, evt])
}

function renderEvents(events: AiSessionEventDto[]): string {
  if (events.length === 0) return ''

  return sortEvents(events).reduce((text, evt) => {
    if (evt.message) {
      return `${text}${text.endsWith('\n') || text.length === 0 ? '' : '\n'}${evt.message}\n`
    }

    if (evt.delta) {
      return text + evt.delta
    }

    return text
  }, '')
}

function toDisplayStatus(status: string): string {
  if (status === 'gathering_context') return 'Gathering context...'
  if (status === 'model_requested') return 'Calling model...'
  if (status === 'streaming') return 'Streaming response...'
  if (status === 'completed') return 'Completed'
  if (status === 'failed') return 'Failed'
  if (status === 'cancelled') return 'Cancelled'
  return status
}

export function useAi(): AiApi {
  const ctx = useContext(AiContext)
  if (!ctx) throw new Error('useAi must be used within AiProvider')
  return ctx
}


