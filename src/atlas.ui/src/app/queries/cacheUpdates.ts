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

/** Optimistic-only: growth mutations update this cache directly; no invalidateAppQueries scope. */
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

export function syncProjectLinkedRiskIds(queryClient: QueryClient, risk: Risk) {
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) => {
      const shouldInclude = !!risk.project && p.name === risk.project
      const has = p.linkedRiskIds.includes(risk.id)
      if (shouldInclude && !has) return { ...p, linkedRiskIds: [...p.linkedRiskIds, risk.id] }
      if (!shouldInclude && has) return { ...p, linkedRiskIds: p.linkedRiskIds.filter((id) => id !== risk.id) }
      return p
    }),
  )
}

export function removeRiskFromProjectLinks(queryClient: QueryClient, riskId: string) {
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) =>
      p.linkedRiskIds.includes(riskId)
        ? { ...p, linkedRiskIds: p.linkedRiskIds.filter((id) => id !== riskId) }
        : p,
    ),
  )
}

export function addRiskToProjectLinks(queryClient: QueryClient, risk: Risk) {
  if (!risk.project) return
  setProjectsCache(queryClient, (projects) =>
    projects.map((p) => {
      if (p.name !== risk.project || p.linkedRiskIds.includes(risk.id)) return p
      return { ...p, linkedRiskIds: [...p.linkedRiskIds, risk.id] }
    }),
  )
}

export function syncRiskLinkedTaskIds(queryClient: QueryClient, task: Task) {
  setRisksCache(queryClient, (risks) =>
    risks.map((r) => {
      const shouldInclude = !!task.risk && r.title === task.risk
      const has = r.linkedTaskIds.includes(task.id)
      if (shouldInclude && !has) return { ...r, linkedTaskIds: [...r.linkedTaskIds, task.id] }
      if (!shouldInclude && has) return { ...r, linkedTaskIds: r.linkedTaskIds.filter((id) => id !== task.id) }
      return r
    }),
  )
}

export function removeTaskFromRiskLinks(queryClient: QueryClient, taskId: string) {
  setRisksCache(queryClient, (risks) =>
    risks.map((r) =>
      r.linkedTaskIds.includes(taskId)
        ? { ...r, linkedTaskIds: r.linkedTaskIds.filter((id) => id !== taskId) }
        : r,
    ),
  )
}

export function repairProjectNameReferences(queryClient: QueryClient, oldName: string, newName: string) {
  setTasksCache(queryClient, (tasks) =>
    tasks.map((t) => (t.project === oldName ? { ...t, project: newName } : t)),
  )
  setRisksCache(queryClient, (risks) =>
    risks.map((r) => (r.project === oldName ? { ...r, project: newName } : r)),
  )
}

export function repairRiskTitleReferences(queryClient: QueryClient, oldTitle: string, newTitle: string) {
  setTasksCache(queryClient, (tasks) =>
    tasks.map((t) => (t.risk === oldTitle ? { ...t, risk: newTitle } : t)),
  )
}

export function clearProjectReferences(queryClient: QueryClient, projectName: string) {
  setTasksCache(queryClient, (tasks) =>
    tasks.map((t) => (t.project === projectName ? { ...t, project: undefined } : t)),
  )
  setRisksCache(queryClient, (risks) =>
    risks.map((r) => (r.project === projectName ? { ...r, project: undefined } : r)),
  )
}

export function clearRiskReferences(queryClient: QueryClient, riskTitle: string) {
  setTasksCache(queryClient, (tasks) =>
    tasks.map((t) => (t.risk === riskTitle ? { ...t, risk: undefined } : t)),
  )
}
