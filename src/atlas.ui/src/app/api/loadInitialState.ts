import { getJson, HttpError } from './client'
import type { ProductOwnerListItemDto, ProjectDto, RiskDto, SettingsDto, TaskDto, TeamMemberDto, GrowthDto } from './mappers'
import { mapGrowth, mapProductOwner, mapProject, mapRisk, mapSettings, mapTask, mapTeamMember } from './mappers'
import type { Growth, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'

type ProjectListItemDto = { id: string }
type RiskListItemDto = { id: string }
type TeamMemberListItemDto = { id: string }

export interface InitialAppData {
  tasks: Task[]
  risks: Risk[]
  teamMemberRisks: TeamMemberRisk[]
  team: TeamMember[]
  projects: Project[]
  productOwners: ProductOwner[]
  growth: Growth[]
  settings: Settings
}

/** @deprecated Use TanStack Query fetchers in `app/queries/fetchers.ts` instead. */
export async function loadInitialState(): Promise<InitialAppData> {
  const [settingsDto, taskDtos, productOwnerDtos, projectList, riskList, memberList] = await Promise.all([
    getJson<SettingsDto>('/settings'),
    getJson<TaskDto[]>('/tasks'),
    getJson<ProductOwnerListItemDto[]>('/product-owners'),
    getJson<ProjectListItemDto[]>('/projects'),
    getJson<RiskListItemDto[]>('/risks'),
    getJson<TeamMemberListItemDto[]>('/team-members'),
  ])

  const [projectDtos, riskDtos, teamMemberDtos] = await Promise.all([
    Promise.all(projectList.map((p) => getJson<ProjectDto>(`/projects/${p.id}`))),
    Promise.all(riskList.map((r) => getJson<RiskDto>(`/risks/${r.id}`))),
    Promise.all(memberList.map((m) => getJson<TeamMemberDto>(`/team-members/${m.id}`))),
  ])

  const growthDtos: Array<GrowthDto | null> = await Promise.all(
    memberList.map(async (m) => {
      try {
        return await getJson<GrowthDto>(`/team-members/${m.id}/growth`)
      } catch (e) {
        if (e instanceof HttpError && e.status === 404) return null
        throw e
      }
    }),
  )

  const settings = mapSettings(settingsDto)
  const productOwners = productOwnerDtos.map(mapProductOwner)
  const projects = projectDtos.map(mapProject)

  const projectNameById = new Map(projects.map((p) => [p.id, p.name] as const))

  const risks = riskDtos.map((r) => mapRisk(r, { projectNameById }))
  const riskTitleById = new Map(risks.map((r) => [r.id, r.title] as const))

  const tasks = taskDtos.map((t) => mapTask(t, { projectNameById, riskTitleById }))

  const team: TeamMember[] = []
  const teamMemberRisks: TeamMemberRisk[] = []

  for (const dto of teamMemberDtos) {
    const mapped = mapTeamMember(dto)
    team.push(mapped.member)
    teamMemberRisks.push(...mapped.memberRisks)
  }

  const growth = growthDtos.filter((g): g is GrowthDto => g !== null).map(mapGrowth)

  return {
    tasks,
    risks,
    teamMemberRisks,
    team,
    projects,
    productOwners,
    growth,
    settings,
  }
}
