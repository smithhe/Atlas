import { deleteJson, getJson, postJson, putJson } from './client'
import type { TeamMember, TeamMemberRisk, NoteTag, LoadSignal, DeliverySignal, SupportNeededSignal } from '../types'
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

export async function fetchTeamMember(memberId: string): Promise<{ member: TeamMember; memberRisks: TeamMemberRisk[] }> {
  const dto = await getJson<TeamMemberDto>(`/team-members/${memberId}`)
  return mapTeamMember(dto)
}

export async function updateTeamMember(
  memberId: string,
  request: {
    name: string
    role?: string
    statusDot: TeamMember['statusDot']
    currentFocus: string
  },
): Promise<void> {
  await putJson<void>(`/team-members/${memberId}`, {
    name: request.name,
    role: request.role ?? null,
    statusDot: request.statusDot,
    currentFocus: request.currentFocus,
  })
}

export async function updateTeamMemberProfile(
  memberId: string,
  request: { timeZone?: string; typicalHours?: string },
): Promise<void> {
  await putJson<void>(`/team-members/${memberId}/profile`, {
    teamMemberId: memberId,
    timeZone: request.timeZone ?? null,
    typicalHours: request.typicalHours ?? null,
  })
}

export async function updateTeamMemberSignals(
  memberId: string,
  signals: { load: LoadSignal; delivery: DeliverySignal; supportNeeded: SupportNeededSignal },
): Promise<void> {
  await putJson<void>(`/team-members/${memberId}/signals`, {
    teamMemberId: memberId,
    load: signals.load,
    delivery: signals.delivery,
    supportNeeded: signals.supportNeeded,
  })
}

export async function addTeamNote(
  memberId: string,
  request: { tag: NoteTag; title?: string; text: string; adoWorkItemId?: string; prUrl?: string },
): Promise<string> {
  const res = await postJson<{ id: string }>(`/team-members/${memberId}/notes`, {
    teamMemberId: memberId,
    type: request.tag,
    title: request.title ?? null,
    text: request.text,
    adoWorkItemId: request.adoWorkItemId ?? null,
    prUrl: request.prUrl ?? null,
  })
  return res.id
}

export async function updateTeamNote(
  memberId: string,
  noteId: string,
  request: { tag: NoteTag; title?: string; text: string; pinnedOrder?: number | null; adoWorkItemId?: string; prUrl?: string },
): Promise<void> {
  await putJson<void>(`/team-members/${memberId}/notes/${noteId}`, {
    teamMemberId: memberId,
    noteId,
    type: request.tag,
    title: request.title ?? null,
    text: request.text,
    pinnedOrder: request.pinnedOrder ?? null,
    adoWorkItemId: request.adoWorkItemId ?? null,
    prUrl: request.prUrl ?? null,
  })
}

export async function deleteTeamNote(memberId: string, noteId: string): Promise<void> {
  await deleteJson(`/team-members/${memberId}/notes/${noteId}`)
}

export async function addTeamMemberRisk(
  memberId: string,
  request: {
    title: string
    severity: TeamMemberRisk['severity']
    riskType: string
    status: TeamMemberRisk['status']
    trend: TeamMemberRisk['trend']
    firstNoticedDateIso: string
    impactArea: string
    description: string
    currentAction: string
    linkedRiskId?: string
  },
): Promise<string> {
  const res = await postJson<{ id: string }>(`/team-members/${memberId}/risks`, {
    teamMemberId: memberId,
    title: request.title,
    severity: request.severity,
    riskType: request.riskType,
    status: request.status,
    trend: request.trend,
    firstNoticedDate: request.firstNoticedDateIso,
    impactArea: request.impactArea,
    description: request.description,
    currentAction: request.currentAction,
    linkedGlobalRiskId: request.linkedRiskId ?? null,
  })
  return res.id
}

export async function updateTeamMemberRisk(
  memberId: string,
  riskId: string,
  request: {
    title: string
    severity: TeamMemberRisk['severity']
    riskType: string
    status: TeamMemberRisk['status']
    trend: TeamMemberRisk['trend']
    firstNoticedDateIso: string
    impactArea: string
    description: string
    currentAction: string
    linkedRiskId?: string
    lastReviewedIso?: string
  },
): Promise<void> {
  await putJson<void>(`/team-members/${memberId}/risks/${riskId}`, {
    teamMemberId: memberId,
    teamMemberRiskId: riskId,
    title: request.title,
    severity: request.severity,
    riskType: request.riskType,
    status: request.status,
    trend: request.trend,
    firstNoticedDate: request.firstNoticedDateIso,
    impactArea: request.impactArea,
    description: request.description,
    currentAction: request.currentAction,
    linkedGlobalRiskId: request.linkedRiskId ?? null,
    lastReviewedAt: request.lastReviewedIso ?? null,
  })
}

export async function addAzureWorkItemLocalNote(
  memberId: string,
  workItemId: string,
  text: string,
): Promise<{ id: string; createdIso: string }> {
  const workItemIdNum = Number.parseInt(workItemId, 10)
  if (!Number.isFinite(workItemIdNum)) {
    throw new Error('Invalid work item id')
  }

  const res = await postJson<{ id: string; createdAt: string }>(
    `/team-members/${memberId}/azure-work-items/${workItemIdNum}/notes`,
    {
      teamMemberId: memberId,
      workItemId: workItemIdNum,
      text,
    },
  )
  return { id: res.id, createdIso: res.createdAt }
}
