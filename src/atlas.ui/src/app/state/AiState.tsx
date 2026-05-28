import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useAppHydration, useAppState } from './AppState'
import {
  continueAiConversation,
  createAiConversation,
  getAiConversation,
  listAiConversations,
  openAiSessionEvents,
  type AiConversationListItemDto,
  type AiSessionEventDto,
  type AiView,
} from '../api/ai'

interface AiAction {
  id: string
  label: string
  description?: string
}

export interface AiTranscriptTurn {
  id: string
  sessionId?: string
  prompt: string
  response: string
}

interface AiState {
  isOpen: boolean
  contextTitle: string
  actions: AiAction[]
  conversations: AiConversationListItemDto[]
  turns: AiTranscriptTurn[]
  notice?: string
  status: string
  isRunning: boolean
  isLoadingHistory: boolean
  activeConversationId?: string
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
  loadConversations: () => void
  openConversation: (conversationId: string) => void
  startNewSession: () => void
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
  const [turns, setTurns] = useState<AiTranscriptTurn[]>([])
  const [activeTurnId, setActiveTurnId] = useState<string | null>(null)
  const [events, setEvents] = useState<AiSessionEventDto[]>([])
  const [notice, setNotice] = useState<string>('')
  const [conversations, setConversations] = useState<AiConversationListItemDto[]>([])
  const [status, setStatus] = useState<string>('Idle')
  const [isRunning, setIsRunning] = useState<boolean>(false)
  const [isLoadingHistory, setIsLoadingHistory] = useState<boolean>(false)
  const [activeConversationId, setActiveConversationId] = useState<string | undefined>(undefined)
  const [activeSessionId, setActiveSessionId] = useState<string | undefined>(undefined)
  const [promptDraft, setPromptDraft] = useState<string>('')
  const [panelWidthPx, setPanelWidthPx] = useState<number | undefined>(undefined)
  const userChangedIsOpenRef = useRef(false)
  const appliedStartupPreferenceRef = useRef(false)
  const eventSourceRef = useRef<EventSource | null>(null)

  const actionsRef = useRef<AiAction[]>(actions)
  const contextTitleRef = useRef<string>(contextTitle)
  const activeConversationIdRef = useRef<string | undefined>(activeConversationId)

  useEffect(() => {
    actionsRef.current = actions
    contextTitleRef.current = contextTitle
  }, [actions, contextTitle])

  useEffect(() => {
    activeConversationIdRef.current = activeConversationId
  }, [activeConversationId])

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

  const activeResponse = useMemo(() => renderEvents(events), [events])

  useEffect(() => {
    if (!activeTurnId) return
    setTurns((prev) =>
      prev.map((turn) => (turn.id === activeTurnId ? { ...turn, response: activeResponse } : turn)),
    )
  }, [activeTurnId, activeResponse])

  const clearOutput = useCallback(() => {
    setTurns([])
    setActiveTurnId(null)
    setEvents([])
    setNotice('')
    setActiveSessionId(undefined)
  }, [])

  const appendOutput = useCallback((text: string) => setNotice((prev) => (prev ? `${prev}${text}` : text.trimStart())), [])

  const closeStream = useCallback(() => {
    eventSourceRef.current?.close()
    eventSourceRef.current = null
  }, [])

  const startNewSession = useCallback(() => {
    closeStream()
    setTurns([])
    setActiveTurnId(null)
    setEvents([])
    setNotice('')
    setActiveConversationId(undefined)
    setActiveSessionId(undefined)
    setStatus('Idle')
    setIsRunning(false)
  }, [closeStream])

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

  const refreshConversations = useCallback(async () => {
    try {
      const recent = await listAiConversations(25)
      setConversations(recent)
    } catch {
      // History is useful but non-critical; active prompts can still run.
    }
  }, [])

  const onSessionEvent = useCallback((evt: AiSessionEventDto) => {
    setEvents((prev) => mergeEvent(prev, evt))

    if (evt.status) {
      if (evt.status === 'gathering_context') setStatus('Gathering context...')
      else if (evt.status === 'using_history') setStatus('Using conversation history...')
      else if (evt.status === 'model_requested') setStatus('Calling model...')
      else if (evt.status === 'streaming') setStatus('Streaming response...')
      else if (evt.status === 'completed') setStatus('Completed')
      else if (evt.status === 'failed') setStatus('Failed')
      else if (evt.status === 'cancelled') setStatus('Cancelled')
      else setStatus(evt.status)
    }

    if (evt.isTerminal) {
      setIsRunning(false)
      setActiveTurnId(null)
      closeStream()
      void refreshConversations()
    }
  }, [closeStream, refreshConversations])

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
    es.addEventListener('history.loading', (event) => processPayload((event as MessageEvent).data))
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

  const loadConversations = useCallback(() => {
    void refreshConversations()
  }, [refreshConversations])

  useEffect(() => {
    if (isOpen) {
      void refreshConversations()
    }
  }, [isOpen, refreshConversations])

  const openConversation = useCallback((conversationId: string) => {
    void (async () => {
      closeStream()
      setIsLoadingHistory(true)
      setStatus('Loading history...')
      setIsRunning(false)

      try {
        const conversation = await getAiConversation(conversationId)
        const loadedTurns: AiTranscriptTurn[] = conversation.turns.map((turn) => ({
          id: newTurnId(),
          sessionId: turn.sessionId,
          prompt: turn.prompt,
          response: renderEvents(sortEvents(turn.events)),
        }))

        setActiveConversationId(conversation.conversationId)
        setTurns(loadedTurns)

        const lastTurn = conversation.turns[conversation.turns.length - 1]
        if (lastTurn && !lastTurn.isTerminal) {
          const uiTurnId = loadedTurns[loadedTurns.length - 1]?.id ?? null
          setActiveTurnId(uiTurnId)
          setActiveSessionId(lastTurn.sessionId)
          setEvents(sortEvents(lastTurn.events))
          setStatus('Reconnecting to stream...')
          setIsRunning(true)
          connectStream(lastTurn.sessionId)
        } else {
          setActiveTurnId(null)
          setActiveSessionId(lastTurn?.sessionId)
          setEvents([])
          setStatus(toDisplayStatus(lastTurn?.status ?? 'completed'))
        }

        setNotice('')
      } catch (err) {
        setStatus('Failed')
        setNotice(err instanceof Error ? err.message : 'Failed to load AI conversation')
      } finally {
        setIsLoadingHistory(false)
      }
    })()
  }, [closeStream, connectStream])

  const sendTurn = useCallback(async (prompt: string, actionId?: string) => {
    const trimmedPrompt = prompt.trim()
    if (!trimmedPrompt) return

    const view = resolveView(contextTitleRef.current)
    if (!view) {
      setIsOpenState(true)
      setStatus('Unsupported context')
      setNotice('AI context is currently available for Dashboard and Tasks.')
      return
    }

    const turnId = newTurnId()
    const nextTurn: AiTranscriptTurn = { id: turnId, prompt: trimmedPrompt, response: '' }

    setIsOpenState(true)
    setIsRunning(true)
    setStatus('Starting...')
    setTurns((prev) => [...prev, nextTurn])
    setActiveTurnId(turnId)
    setEvents([])
    setNotice('')

    closeStream()

    try {
      const conversationId = activeConversationIdRef.current
      let turnSessionId: string

      if (conversationId) {
        const res = await continueAiConversation(conversationId, { prompt: trimmedPrompt })
        turnSessionId = res.turnSessionId
      } else {
        const res = await createAiConversation({
          prompt: trimmedPrompt,
          view,
          actionId,
          taskId: selectedTaskId,
          projectId: selectedProjectId,
          riskId: selectedRiskId,
          teamMemberId: selectedTeamMemberId,
        })
        setActiveConversationId(res.conversationId)
        activeConversationIdRef.current = res.conversationId
        turnSessionId = res.turnSessionId
      }

      setActiveSessionId(turnSessionId)
      setTurns((prev) => prev.map((t) => (t.id === turnId ? { ...t, sessionId: turnSessionId } : t)))
      setStatus('Connecting to stream...')
      await refreshConversations()
      connectStream(turnSessionId)
    } catch (err) {
      setStatus('Failed')
      setIsRunning(false)
      setActiveTurnId(null)
      setTurns((prev) => prev.filter((t) => t.id !== turnId))
      setNotice(err instanceof Error ? err.message : 'Failed to send AI message')
    }
  }, [closeStream, connectStream, refreshConversations, resolveView, selectedProjectId, selectedRiskId, selectedTaskId, selectedTeamMemberId])

  const runAction = useCallback((actionId: string, promptOverride?: string) => {
    const action = actionsRef.current.find((a) => a.id === actionId)
    const prompt = promptOverride?.trim() || `Please help with this action: ${action?.label ?? actionId}`
    void sendTurn(prompt, actionId)
  }, [sendTurn])

  const sendPrompt = useCallback((prompt: string) => {
    void sendTurn(prompt)
  }, [sendTurn])

  const api = useMemo<AiApi>(
    () => ({
      state: {
        isOpen,
        contextTitle,
        actions,
        conversations,
        turns,
        notice: notice || undefined,
        status,
        isRunning,
        isLoadingHistory,
        activeConversationId,
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
      loadConversations,
      openConversation,
      startNewSession,
      runAction,
      sendPrompt,
      clearOutput,
      appendOutput,
    }),
    [
      actions,
      activeConversationId,
      activeSessionId,
      appendOutput,
      clearOutput,
      contextSupportMessage,
      contextTitle,
      conversations,
      isContextSupported,
      isLoadingHistory,
      isOpen,
      isRunning,
      loadConversations,
      notice,
      openConversation,
      panelWidthPx,
      promptDraft,
      runAction,
      sendPrompt,
      setContext,
      setIsOpen,
      startNewSession,
      status,
      turns,
    ],
  )

  return <AiContext.Provider value={api}>{children}</AiContext.Provider>
}

function newTurnId(): string {
  return globalThis.crypto?.randomUUID?.() ?? `turn-${Date.now()}-${Math.random().toString(36).slice(2)}`
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
    if (evt.type === 'model.delta' && evt.delta) {
      return text + evt.delta
    }

    if ((evt.type === 'session.failed' || evt.type === 'session.cancelled') && evt.message) {
      return `${text}${text.endsWith('\n') || text.length === 0 ? '' : '\n'}${evt.message}\n`
    }

    return text
  }, '')
}

function toDisplayStatus(status: string): string {
  if (status === 'gathering_context') return 'Gathering context...'
  if (status === 'using_history') return 'Using conversation history...'
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
