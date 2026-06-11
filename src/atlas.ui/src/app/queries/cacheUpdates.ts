import type { QueryClient } from '@tanstack/react-query'
import type { Growth, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import { queryKeys } from './queryKeys'

export function setTasksCache(queryClient: QueryClient, updater: (tasks: Task[]) => Task[]) {
  queryClient.setQueryData<Task[]>(queryKeys.tasks, (old) => updater(old ?? []))
}

export function setRisksCache(queryClient: QueryClient, updater: (risks: Risk[]) => Risk[]) {
  queryClient.setQueryData<Risk[]>(queryKeys.risks, (old) => updater(old ?? []))
}

export function setProjectsCache(queryClient: QueryClient, updater: (projects: Project[]) => Project[]) {
  queryClient.setQueryData<Project[]>(queryKeys.projects, (old) => updater(old ?? []))
}

export function setSettingsCache(queryClient: QueryClient, settings: Settings) {
  queryClient.setQueryData<Settings>(queryKeys.settings, settings)
}

export function setProductOwnersCache(queryClient: QueryClient, productOwners: ProductOwner[]) {
  queryClient.setQueryData<ProductOwner[]>(queryKeys.productOwners, productOwners)
}

export function setTeamMembersCache(
  queryClient: QueryClient,
  team: TeamMember[],
  teamMemberRisks: TeamMemberRisk[],
) {
  queryClient.setQueryData(queryKeys.teamMembers, { team, teamMemberRisks })
}

export function setGrowthCache(queryClient: QueryClient, memberId: string, growth: Growth | null) {
  queryClient.setQueryData(queryKeys.growth(memberId), growth)
}

export function syncProjectLinkedTaskIds(queryClient: QueryClient, task: Task) {
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) => {
      const shouldInclude = !!task.project && p.name === task.project
      const has = p.linkedTaskIds.includes(task.id)
      if (shouldInclude && !has) return { ...p, linkedTaskIds: [...p.linkedTaskIds, task.id] }
      if (!shouldInclude && has) return { ...p, linkedTaskIds: p.linkedTaskIds.filter((id) => id !== task.id) }
      return p
    }),
  )
}

export function removeTaskFromProjectLinks(queryClient: QueryClient, taskId: string) {
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) =>
      p.linkedTaskIds.includes(taskId)
        ? { ...p, linkedTaskIds: p.linkedTaskIds.filter((id) => id !== taskId) }
        : p,
    ),
  )
}

export function addTaskToProjectLinks(queryClient: QueryClient, task: Task) {
  if (!task.project) return
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) => {
      if (p.name !== task.project || p.linkedTaskIds.includes(task.id)) return p
      return { ...p, linkedTaskIds: [...p.linkedTaskIds, task.id] }
    }),
  )
}
