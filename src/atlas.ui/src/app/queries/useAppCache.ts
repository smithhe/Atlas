import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo } from 'react'
import type { Growth, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import { withDerivedActivitySnapshot } from '../team'
import {
  addRiskToProjectLinks,
  addTaskToProjectLinks,
  clearProjectReferences,
  clearRiskReferences,
  removeRiskFromProjectLinks,
  removeTaskFromProjectLinks,
  removeTaskFromRiskLinks,
  repairProjectNameReferences,
  repairRiskTitleReferences,
  setGrowthCache,
  setProductOwnersCache,
  setProjectsCache,
  setRisksCache,
  setSettingsCache,
  setTasksCache,
  setTeamMembersCache,
  syncProjectLinkedRiskIds,
  syncProjectLinkedTaskIds,
  syncRiskLinkedTaskIds,
} from './cacheUpdates'
import { queryKeys } from './queryKeys'
import { useSelectionActions } from '../state/SelectionState'

export function useAppCache() {
  const queryClient = useQueryClient()
  const { selectTask, selectRisk, selectProject } = useSelectionActions()

  const addTask = useCallback(
    (task: Task) => {
      setTasksCache(queryClient, (tasks) => [task, ...tasks])
      addTaskToProjectLinks(queryClient, task)
      syncRiskLinkedTaskIds(queryClient, task)
      selectTask(task.id)
    },
    [queryClient, selectTask],
  )

  const updateTask = useCallback(
    (task: Task) => {
      setTasksCache(queryClient, (tasks) => tasks.map((t) => (t.id === task.id ? task : t)))
      syncProjectLinkedTaskIds(queryClient, task)
      syncRiskLinkedTaskIds(queryClient, task)
    },
    [queryClient],
  )

  const removeTask = useCallback(
    (taskId: string) => {
      setTasksCache(queryClient, (tasks) => tasks.filter((t) => t.id !== taskId))
      removeTaskFromProjectLinks(queryClient, taskId)
      removeTaskFromRiskLinks(queryClient, taskId)
    },
    [queryClient],
  )

  const addRisk = useCallback(
    (risk: Risk) => {
      setRisksCache(queryClient, (risks) => [risk, ...risks])
      addRiskToProjectLinks(queryClient, risk)
      selectRisk(risk.id)
    },
    [queryClient, selectRisk],
  )

  const updateRisk = useCallback(
    (risk: Risk) => {
      const previous = queryClient.getQueryData<Risk[]>(queryKeys.risks)?.find((r) => r.id === risk.id)
      setRisksCache(queryClient, (risks) => risks.map((r) => (r.id === risk.id ? risk : r)))
      syncProjectLinkedRiskIds(queryClient, risk)
      if (previous && previous.title !== risk.title) {
        repairRiskTitleReferences(queryClient, previous.title, risk.title)
        const tasks = queryClient.getQueryData<Task[]>(queryKeys.tasks) ?? []
        for (const task of tasks) {
          if (task.risk === risk.title) syncRiskLinkedTaskIds(queryClient, task)
        }
      }
    },
    [queryClient],
  )

  const removeRisk = useCallback(
    (riskId: string) => {
      const previous = queryClient.getQueryData<Risk[]>(queryKeys.risks)?.find((r) => r.id === riskId)
      setRisksCache(queryClient, (risks) => risks.filter((r) => r.id !== riskId))
      removeRiskFromProjectLinks(queryClient, riskId)
      if (previous) clearRiskReferences(queryClient, previous.title)
    },
    [queryClient],
  )

  const addProject = useCallback(
    (project: Project) => {
      setProjectsCache(queryClient, (projects) => [project, ...projects])
      selectProject(project.id)
    },
    [queryClient, selectProject],
  )

  const updateProject = useCallback(
    (project: Project) => {
      const previous = queryClient.getQueryData<Project[]>(queryKeys.projects)?.find((p) => p.id === project.id)
      setProjectsCache(queryClient, (projects) => projects.map((p) => (p.id === project.id ? project : p)))
      if (previous && previous.name !== project.name) {
        repairProjectNameReferences(queryClient, previous.name, project.name)
      }
    },
    [queryClient],
  )

  const removeProject = useCallback(
    (projectId: string) => {
      const previous = queryClient.getQueryData<Project[]>(queryKeys.projects)?.find((p) => p.id === projectId)
      setProjectsCache(queryClient, (projects) => projects.filter((p) => p.id !== projectId))
      if (previous) clearProjectReferences(queryClient, previous.name)
    },
    [queryClient],
  )

  const updateSettings = useCallback(
    (settings: Settings) => {
      setSettingsCache(queryClient, settings)
    },
    [queryClient],
  )

  const updateTeamMember = useCallback(
    (member: TeamMember) => {
      const cached = queryClient.getQueryData<{ team: TeamMember[]; teamMemberRisks: TeamMemberRisk[] }>(
        queryKeys.teamMembers,
      )
      if (!cached) return
      const next = withDerivedActivitySnapshot(member)
      setTeamMembersCache(
        queryClient,
        cached.team.map((m) => (m.id === next.id ? next : m)),
        cached.teamMemberRisks,
      )
    },
    [queryClient],
  )

  const replaceTeamMembers = useCallback(
    (team: TeamMember[], teamMemberRisks: TeamMemberRisk[]) => {
      setTeamMembersCache(queryClient, team.map(withDerivedActivitySnapshot), teamMemberRisks)
    },
    [queryClient],
  )

  const addTeamMemberRisk = useCallback(
    (teamMemberRisk: TeamMemberRisk) => {
      const cached = queryClient.getQueryData<{ team: TeamMember[]; teamMemberRisks: TeamMemberRisk[] }>(
        queryKeys.teamMembers,
      )
      if (!cached) return
      setTeamMembersCache(queryClient, cached.team, [teamMemberRisk, ...cached.teamMemberRisks])
    },
    [queryClient],
  )

  const updateTeamMemberRisk = useCallback(
    (teamMemberRisk: TeamMemberRisk) => {
      const cached = queryClient.getQueryData<{ team: TeamMember[]; teamMemberRisks: TeamMemberRisk[] }>(
        queryKeys.teamMembers,
      )
      if (!cached) return
      setTeamMembersCache(
        queryClient,
        cached.team,
        cached.teamMemberRisks.map((r) => (r.id === teamMemberRisk.id ? teamMemberRisk : r)),
      )
    },
    [queryClient],
  )

  const updateGrowth = useCallback(
    (growth: Growth) => {
      setGrowthCache(queryClient, growth.memberId, growth)
    },
    [queryClient],
  )

  const replaceProductOwners = useCallback(
    (productOwners: ProductOwner[]) => {
      setProductOwnersCache(queryClient, productOwners)
    },
    [queryClient],
  )

  return useMemo(
    () => ({
      addTask,
      updateTask,
      removeTask,
      addRisk,
      updateRisk,
      removeRisk,
      addProject,
      updateProject,
      removeProject,
      updateSettings,
      updateTeamMember,
      replaceTeamMembers,
      addTeamMemberRisk,
      updateTeamMemberRisk,
      updateGrowth,
      replaceProductOwners,
    }),
    [
      addProject,
      addRisk,
      addTask,
      addTeamMemberRisk,
      removeProject,
      removeRisk,
      removeTask,
      replaceProductOwners,
      replaceTeamMembers,
      updateGrowth,
      updateProject,
      updateRisk,
      updateSettings,
      updateTask,
      updateTeamMember,
      updateTeamMemberRisk,
    ],
  )
}
