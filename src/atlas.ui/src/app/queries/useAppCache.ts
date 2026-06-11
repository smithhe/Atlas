import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo } from 'react'
import type { Growth, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import {
  addTaskToProjectLinks,
  removeTaskFromProjectLinks,
  setGrowthCache,
  setProductOwnersCache,
  setProjectsCache,
  setRisksCache,
  setSettingsCache,
  setTasksCache,
  setTeamMembersCache,
  syncProjectLinkedTaskIds,
} from './cacheUpdates'
import { queryKeys } from './queryKeys'
import { useSelectionDispatch } from '../state/SelectionState'

export function useAppCache() {
  const queryClient = useQueryClient()
  const dispatch = useSelectionDispatch()

  const addTask = useCallback(
    (task: Task) => {
      setTasksCache(queryClient, (tasks) => [task, ...tasks])
      addTaskToProjectLinks(queryClient, task)
      dispatch({ type: 'selectTask', taskId: task.id })
    },
    [dispatch, queryClient],
  )

  const updateTask = useCallback(
    (task: Task) => {
      setTasksCache(queryClient, (tasks) => tasks.map((t) => (t.id === task.id ? task : t)))
      syncProjectLinkedTaskIds(queryClient, task)
    },
    [queryClient],
  )

  const removeTask = useCallback(
    (taskId: string) => {
      setTasksCache(queryClient, (tasks) => tasks.filter((t) => t.id !== taskId))
      removeTaskFromProjectLinks(queryClient, taskId)
    },
    [queryClient],
  )

  const addRisk = useCallback(
    (risk: Risk) => {
      setRisksCache(queryClient, (risks) => [risk, ...risks])
      dispatch({ type: 'selectRisk', riskId: risk.id })
    },
    [dispatch, queryClient],
  )

  const updateRisk = useCallback(
    (risk: Risk) => {
      setRisksCache(queryClient, (risks) => risks.map((r) => (r.id === risk.id ? risk : r)))
    },
    [queryClient],
  )

  const removeRisk = useCallback(
    (riskId: string) => {
      setRisksCache(queryClient, (risks) => risks.filter((r) => r.id !== riskId))
    },
    [queryClient],
  )

  const addProject = useCallback(
    (project: Project) => {
      setProjectsCache(queryClient, (projects) => [project, ...projects])
      dispatch({ type: 'selectProject', projectId: project.id })
    },
    [dispatch, queryClient],
  )

  const updateProject = useCallback(
    (project: Project) => {
      setProjectsCache(queryClient, (projects) => projects.map((p) => (p.id === project.id ? project : p)))
    },
    [queryClient],
  )

  const removeProject = useCallback(
    (projectId: string) => {
      setProjectsCache(queryClient, (projects) => projects.filter((p) => p.id !== projectId))
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
      setTeamMembersCache(
        queryClient,
        cached.team.map((m) => (m.id === member.id ? member : m)),
        cached.teamMemberRisks,
      )
    },
    [queryClient],
  )

  const replaceTeamMembers = useCallback(
    (team: TeamMember[], teamMemberRisks: TeamMemberRisk[]) => {
      setTeamMembersCache(queryClient, team, teamMemberRisks)
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
      dispatch({ type: 'selectTeamMemberRisk', teamMemberRiskId: teamMemberRisk.id })
    },
    [dispatch, queryClient],
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
