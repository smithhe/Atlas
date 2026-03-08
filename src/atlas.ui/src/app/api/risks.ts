import { deleteJson, postJson, putJson } from './client'

interface CreateRiskRequest {
  title: string
  status: 'Open' | 'Watching' | 'Resolved'
  severity: 'Low' | 'Medium' | 'High'
  projectId?: string
  description: string
  evidence: string
}

interface CreateRiskResponse {
  id: string
}

export async function createRisk(request: CreateRiskRequest): Promise<string> {
  const res = await postJson<CreateRiskResponse>('/risks', request)
  return res.id
}

type UpdateRiskRequest = CreateRiskRequest

export async function updateRisk(riskId: string, request: UpdateRiskRequest): Promise<void> {
  await putJson<void>(`/risks/${riskId}`, request)
}

export async function deleteRisk(riskId: string): Promise<void> {
  await deleteJson(`/risks/${riskId}`)
}
