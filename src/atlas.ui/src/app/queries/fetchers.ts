import type { Growth, ProductOwner, Project, Risk, Settings, Task } from '../types'
import { getJson, HttpError } from '../api/client'
import type {
  GrowthDto,
  ProductOwnerListItemDto,
  ProjectDto,
  RiskDto,
  SettingsDto,
  TaskDto,
} from '../api/mappers'
import {
  mapGrowth,
  mapProductOwner,
  mapProject,
  mapRisk,
  mapSettings,
  mapTask,
} from '../api/mappers'
import { loadTeamMembers } from '../api/teamMembers'

export async function fetchSettings(): Promise<Settings> {
  const dto = await getJson<SettingsDto>('/settings')
  return mapSettings(dto)
}

export async function fetchProjects(): Promise<Project[]> {
  const dtos = await getJson<ProjectDto[]>('/projects')
  return dtos.map(mapProject)
}

export async function fetchRisks(projects: Project[]): Promise<Risk[]> {
  const projectNameById = new Map(projects.map((p) => [p.id, p.name] as const))
  const dtos = await getJson<RiskDto[]>('/risks')
  return dtos.map((r) => mapRisk(r, { projectNameById }))
}

export async function fetchTasks(projects: Project[], risks: Risk[]): Promise<Task[]> {
  const projectNameById = new Map(projects.map((p) => [p.id, p.name] as const))
  const riskTitleById = new Map(risks.map((r) => [r.id, r.title] as const))
  const dtos = await getJson<TaskDto[]>('/tasks')
  return dtos.map((t) => mapTask(t, { projectNameById, riskTitleById }))
}

export async function fetchProductOwners(): Promise<ProductOwner[]> {
  const dtos = await getJson<ProductOwnerListItemDto[]>('/product-owners')
  return dtos.map(mapProductOwner)
}

export { loadTeamMembers as fetchTeamMembers }

export async function fetchGrowthForMember(memberId: string): Promise<Growth | null> {
  try {
    const dto = await getJson<GrowthDto>(`/team-members/${memberId}/growth`)
    return mapGrowth(dto)
  } catch (e) {
    if (e instanceof HttpError && e.status === 404) return null
    throw e
  }
}
