import { getJson, postJson, toApiUrl } from './client'

export type AiView = 'Dashboard' | 'Tasks'

export interface CreateAiConversationRequest {
  prompt: string
  view: AiView
  actionId?: string
  taskId?: string
  projectId?: string
  riskId?: string
  teamMemberId?: string
}

export interface CreateAiConversationResponse {
  conversationId: string
  turnSessionId: string
}

export interface ContinueAiConversationRequest {
  prompt: string
}

export interface ContinueAiConversationResponse {
  turnSessionId: string
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

export interface AiConversationListItemDto {
  conversationId: string
  title: string
  view: AiView
  actionId?: string
  taskId?: string
  projectId?: string
  riskId?: string
  teamMemberId?: string
  createdAtUtc: string
  updatedAtUtc: string
  turnCount: number
  status: string
  isTerminal: boolean
}

export interface AiConversationTurnDto {
  sessionId: string
  turnIndex: number
  prompt: string
  status: string
  isTerminal: boolean
  events: AiSessionEventDto[]
}

export interface AiConversationDetailDto {
  conversationId: string
  title: string
  view: AiView
  actionId?: string
  taskId?: string
  projectId?: string
  riskId?: string
  teamMemberId?: string
  createdAtUtc: string
  updatedAtUtc: string
  turns: AiConversationTurnDto[]
}

export function createAiConversation(req: CreateAiConversationRequest): Promise<CreateAiConversationResponse> {
  return postJson<CreateAiConversationResponse>('/ai/conversations', req)
}

export function continueAiConversation(
  conversationId: string,
  req: ContinueAiConversationRequest,
): Promise<ContinueAiConversationResponse> {
  return postJson<ContinueAiConversationResponse>(`/ai/conversations/${conversationId}/messages`, req)
}

export function listAiConversations(take = 25): Promise<AiConversationListItemDto[]> {
  return getJson<AiConversationListItemDto[]>(`/ai/conversations?take=${encodeURIComponent(String(take))}`)
}

export function getAiConversation(conversationId: string): Promise<AiConversationDetailDto> {
  return getJson<AiConversationDetailDto>(`/ai/conversations/${conversationId}`)
}

export function openAiSessionEvents(sessionId: string): EventSource {
  return new EventSource(toApiUrl(`/ai/sessions/${sessionId}/events`))
}
