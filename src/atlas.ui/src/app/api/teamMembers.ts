import { getJson } from './client'
import type { TeamMember, TeamMemberRisk } from '../types'
import type { TeamMemberDto } from './mappers'
import { mapTeamMember } from './mappers'

type TeamMemberListItemDto = { id: string }

export async function loadTeamMembers(): Promise<{ team: TeamMember[]; teamMemberRisks: TeamMemberRisk[] }> {
  const list = await getJson<TeamMemberListItemDto[]>('/team-members')
  const teamMemberDtos = await Promise.all(list.map((m) => getJson<TeamMemberDto>(`/team-members/${m.id}`)))

  const team: TeamMember[] = []
  const teamMemberRisks: TeamMemberRisk[] = []

  for (const dto of teamMemberDtos) {
    const mapped = mapTeamMember(dto)
    team.push(mapped.member)
    teamMemberRisks.push(...mapped.memberRisks)
  }

  return { team, teamMemberRisks }
}
