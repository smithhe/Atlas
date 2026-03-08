import { deleteJson, postJson, putJson } from './client'
import type { Confidence, Priority } from '../types'

type CreateTaskStatus = 'NotStarted' | 'InProgress' | 'Blocked' | 'Done'

interface CreateTaskRequest {
  title: string
  priority: Priority
  status: CreateTaskStatus
  assigneeId?: string
  projectId?: string
  riskId?: string
  dueDate?: string
  dependencyTaskIds?: string[]
  estimatedDurationText: string
  estimateConfidence: Confidence
  actualDurationText?: string
  notes: string
}

interface CreateTaskResponse {
  id: string
}

export async function createTask(request: CreateTaskRequest): Promise<string> {
  const res = await postJson<CreateTaskResponse>('/tasks', request)
  return res.id
}

type UpdateTaskRequest = CreateTaskRequest

export async function updateTask(taskId: string, request: UpdateTaskRequest): Promise<void> {
  await putJson<void>(`/tasks/${taskId}`, request)
}

export async function deleteTask(taskId: string): Promise<void> {
  await deleteJson(`/tasks/${taskId}`)
}
