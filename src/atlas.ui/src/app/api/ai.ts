import { getJson, postJson, toApiUrl } from './client'

export type AiView = 'Dashboard' | 'Tasks'

export interface StartAiSessionRequest {
  prompt: string
  view: AiView
  actionId?: string
  taskId?: string
  projectId?: string
  riskId?: string
  teamMemberId?: string
}

export interface StartAiSessionResponse {
  sessionId: string
}

export interface AiSessionEventDto {
  eventId: string
  sessionId: string
  sequence: number
  type: string
  status?: string
  message?: string
  delta?: string
  occurredAtUtc: string
  isTerminal: boolean
}

export interface AiSessionListItemDto {
  sessionId: string
  title: string
  prompt: string
  view: AiView
  actionId?: string
  taskId?: string
  projectId?: string
  riskId?: string
  teamMemberId?: string
  createdAtUtc: string
  completedAtUtc?: string
  status: string
  isTerminal: boolean
}

export interface AiSessionDetailDto extends AiSessionListItemDto {
  events: AiSessionEventDto[]
}

export function startAiSession(req: StartAiSessionRequest): Promise<StartAiSessionResponse> {
  return postJson<StartAiSessionResponse>('/ai/sessions', req)
}

export function listAiSessions(take = 25): Promise<AiSessionListItemDto[]> {
  return getJson<AiSessionListItemDto[]>(`/ai/sessions?take=${encodeURIComponent(String(take))}`)
}

export function getAiSession(sessionId: string): Promise<AiSessionDetailDto> {
  return getJson<AiSessionDetailDto>(`/ai/sessions/${sessionId}`)
}

export function openAiSessionEvents(sessionId: string): EventSource {
  return new EventSource(toApiUrl(`/ai/sessions/${sessionId}/events`))
}

