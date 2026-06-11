import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { Growth, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import {
  fetchGrowthForMember,
  fetchProductOwners,
  fetchProjects,
  fetchRisks,
  fetchSettings,
  fetchTasks,
  fetchTeamMembers,
} from './fetchers'
import { queryKeys } from './queryKeys'

export function useSettingsQuery() {
  return useQuery({
    queryKey: queryKeys.settings,
    queryFn: fetchSettings,
  })
}

export function useProjectsQuery() {
  return useQuery({
    queryKey: queryKeys.projects,
    queryFn: fetchProjects,
  })
}

export function useRisksQuery() {
  const projectsQuery = useProjectsQuery()
  return useQuery({
    queryKey: queryKeys.risks,
    queryFn: () => fetchRisks(projectsQuery.data ?? []),
    enabled: projectsQuery.isSuccess,
  })
}

export function useTasksQuery() {
  const projectsQuery = useProjectsQuery()
  const risksQuery = useRisksQuery()
  return useQuery({
    queryKey: queryKeys.tasks,
    queryFn: () => fetchTasks(projectsQuery.data ?? [], risksQuery.data ?? []),
    enabled: projectsQuery.isSuccess && risksQuery.isSuccess,
  })
}

export function useProductOwnersQuery() {
  return useQuery({
    queryKey: queryKeys.productOwners,
    queryFn: fetchProductOwners,
  })
}

export function useTeamMembersQuery() {
  return useQuery({
    queryKey: queryKeys.teamMembers,
    queryFn: fetchTeamMembers,
  })
}

export function useGrowthQuery(memberId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.growth(memberId ?? ''),
    queryFn: () => (memberId ? fetchGrowthForMember(memberId) : Promise.resolve(null)),
    enabled: !!memberId,
  })
}

export function useSettings(): Settings {
  const { data } = useSettingsQuery()
  return (
    data ?? {
      staleDays: 10,
      defaultAiManualOnly: true,
      defaultAiPanelOpen: false,
      theme: 'Dark',
      azureDevOpsBaseUrl: undefined,
    }
  )
}

export function useTasks(): Task[] {
  const { data } = useTasksQuery()
  return data ?? []
}

export function useRisks(): Risk[] {
  const { data } = useRisksQuery()
  return data ?? []
}

export function useProjects(): Project[] {
  const { data } = useProjectsQuery()
  return data ?? []
}

export function useProductOwners(): ProductOwner[] {
  const { data } = useProductOwnersQuery()
  return data ?? []
}

export function useTeamMembers(): { team: TeamMember[]; teamMemberRisks: TeamMemberRisk[] } {
  const { data } = useTeamMembersQuery()
  return data ?? { team: [], teamMemberRisks: [] }
}

export function useTeam(): TeamMember[] {
  return useTeamMembers().team
}

export function useTeamMemberRisks(): TeamMemberRisk[] {
  return useTeamMembers().teamMemberRisks
}

export function useGrowthForMember(memberId?: string): Growth | undefined {
  const { data } = useGrowthQuery(memberId)
  return data ?? undefined
}

export function useAppHydration(): boolean {
  const settings = useSettingsQuery()
  const projects = useProjectsQuery()
  const risks = useRisksQuery()
  const tasks = useTasksQuery()
  const team = useTeamMembersQuery()
  const productOwners = useProductOwnersQuery()

  return (
    settings.isPending ||
    projects.isPending ||
    risks.isPending ||
    tasks.isPending ||
    team.isPending ||
    productOwners.isPending
  )
}

export function useAppData() {
  const settings = useSettings()
  const tasks = useTasks()
  const risks = useRisks()
  const projects = useProjects()
  const productOwners = useProductOwners()
  const { team, teamMemberRisks } = useTeamMembers()

  return useMemo(
    () => ({
      settings,
      tasks,
      risks,
      projects,
      productOwners,
      team,
      teamMemberRisks,
    }),
    [settings, tasks, risks, projects, productOwners, team, teamMemberRisks],
  )
}
