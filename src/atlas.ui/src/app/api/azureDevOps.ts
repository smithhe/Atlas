import { getJson, HttpError, postJson, putJson } from './client'

export type AzureConnectionDto = {
  organization: string
  project: string
  areaPath: string
  teamName?: string | null
  isEnabled: boolean
  projectId: string
  teamId: string
}

export type AzureProjectDto = { id: string; name: string }
export type AzureTeamDto = { id: string; name: string }
export type AzureUserDto = { displayName: string; uniqueName: string; descriptor?: string | null }

export type AzureSyncStateDto = {
  lastSuccessfulChangedUtc?: string | null
  lastSuccessfulWorkItemId?: number | null
  lastAttemptedAtUtc?: string | null
  lastCompletedAtUtc?: string | null
  lastRunStatus: string
  lastError?: string | null
}

export type AzureSyncResultDto = {
  succeeded: boolean
  itemsFetched: number
  itemsUpserted: number
  lastChangedUtc?: string | null
  lastWorkItemId?: number | null
  error?: string | null
}

export type AzureImportWorkItemDto = {
  id: string
  workItemId: number
  title: string
  state: string
  workItemType: string
  areaPath: string
  iterationPath: string
  changedDateUtc: string
  assignedToUniqueName?: string | null
  url: string
  suggestedTeamMemberId?: string | null
}

export async function getAzureConnection(): Promise<AzureConnectionDto | null> {
  try {
    return await getJson<AzureConnectionDto>('/azure-devops/connection')
  } catch (err) {
    if (err instanceof HttpError && err.status === 404) return null
    throw err
  }
}

export function updateAzureConnection(conn: AzureConnectionDto): Promise<void> {
  return putJson<void>('/azure-devops/connection', conn)
}

export function listAzureProjects(organization: string): Promise<AzureProjectDto[]> {
  return getJson<AzureProjectDto[]>(`/azure-devops/projects?organization=${encodeURIComponent(organization)}`)
}

export function listAzureTeams(organization: string, projectId: string): Promise<AzureTeamDto[]> {
  return getJson<AzureTeamDto[]>(
    `/azure-devops/teams?organization=${encodeURIComponent(organization)}&projectId=${encodeURIComponent(projectId)}`,
  )
}

export function listAzureUsers(organization: string, projectId: string, teamId: string): Promise<AzureUserDto[]> {
  return getJson<AzureUserDto[]>(
    `/azure-devops/users?organization=${encodeURIComponent(organization)}&projectId=${encodeURIComponent(projectId)}&teamId=${encodeURIComponent(teamId)}`,
  )
}

export function importAzureTeam(users: AzureUserDto[]): Promise<void> {
  return postJson<void>('/azure-devops/team/import', { users })
}

export function getAzureSyncState(): Promise<AzureSyncStateDto | null> {
  return getJson<AzureSyncStateDto>('/azure-devops/sync-state').catch((err) => {
    if (err instanceof HttpError && err.status === 404) return null
    throw err
  })
}

export function runAzureSync(): Promise<AzureSyncResultDto> {
  return postJson<AzureSyncResultDto>('/azure-devops/sync', {})
}

export function listAzureImportWorkItems(): Promise<AzureImportWorkItemDto[]> {
  return getJson<AzureImportWorkItemDto[]>('/azure-devops/import/work-items')
}

export function linkAzureWorkItems(
  azureWorkItemIds: string[],
  projectId: string,
  teamMemberId?: string,
): Promise<number> {
  return postJson<number>('/azure-devops/import/link', { azureWorkItemIds, projectId, teamMemberId })
}
