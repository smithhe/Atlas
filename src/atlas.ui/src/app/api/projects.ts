import { deleteJson, postJson, putJson } from './client'
import type { HealthSignal, Priority, ProjectStatus } from '../types'

interface CreateProjectRequest {
  name: string
  summary: string
  description?: string
  status?: ProjectStatus
  health?: HealthSignal
  targetDate?: string
  priority?: Priority
  productOwnerId?: string
  tags?: string[]
  links?: Array<{ label: string; url: string }>
}

interface CreateProjectResponse {
  id: string
}

export async function createProject(request: CreateProjectRequest): Promise<string> {
  const res = await postJson<CreateProjectResponse>('/projects', request)
  return res.id
}

type UpdateProjectRequest = CreateProjectRequest

export async function updateProject(projectId: string, request: UpdateProjectRequest): Promise<void> {
  await putJson<void>(`/projects/${projectId}`, request)
}

export async function deleteProject(projectId: string): Promise<void> {
  await deleteJson(`/projects/${projectId}`)
}
