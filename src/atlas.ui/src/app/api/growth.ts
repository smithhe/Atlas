import { deleteJson, getJson, postJson, putJson } from './client'
import type {
  Growth,
  GrowthFeedbackTheme,
  GrowthGoal,
  GrowthGoalAction,
  GrowthGoalActionState,
  GrowthGoalCheckIn,
  GrowthGoalCheckInSignal,
  GrowthGoalStatus,
  Priority,
} from '../types'
import type { GrowthDto } from './mappers'
import { mapGrowth } from './mappers'

export async function fetchGrowthForMember(memberId: string): Promise<Growth | null> {
  try {
    const dto = await getJson<GrowthDto>(`/team-members/${memberId}/growth`)
    return mapGrowth(dto)
  } catch {
    return null
  }
}

export async function ensureGrowthForMember(memberId: string): Promise<string> {
  const res = await postJson<{ growthId: string }>(`/team-members/${memberId}/growth/ensure`, {
    teamMemberId: memberId,
  })
  return res.growthId
}

export async function setGrowthSkillsInProgress(growthId: string, skillsInProgress: string[]): Promise<void> {
  await putJson<void>(`/growth/${growthId}/skills-in-progress`, {
    growthId,
    skillsInProgress,
  })
}

export async function updateGrowthFocusAreas(growthId: string, focusAreasMarkdown: string): Promise<void> {
  await putJson<void>(`/growth/${growthId}/focus-areas`, {
    growthId,
    focusAreasMarkdown,
  })
}

export async function addGrowthGoal(
  growthId: string,
  request: {
    title: string
    description: string
    status: GrowthGoalStatus
    startDateIso?: string
    targetDateIso?: string
    category?: string
    priority?: Priority
  },
): Promise<string> {
  const res = await postJson<{ id: string }>(`/growth/${growthId}/goals`, {
    growthId,
    title: request.title,
    description: request.description,
    status: request.status,
    startDate: request.startDateIso ?? null,
    targetDate: request.targetDateIso ?? null,
    category: request.category ?? null,
    priority: request.priority ?? null,
  })
  return res.id
}

export async function updateGrowthGoal(
  growthId: string,
  goalId: string,
  goal: GrowthGoal,
): Promise<void> {
  await putJson<void>(`/growth/${growthId}/goals/${goalId}`, {
    growthId,
    goalId,
    title: goal.title,
    description: goal.description,
    status: goal.status,
    startDate: goal.startDateIso ?? null,
    targetDate: goal.targetDateIso ?? null,
    category: goal.category ?? null,
    priority: goal.priority ?? null,
    progressPercent: goal.progressPercent ?? null,
    summary: goal.summary ?? null,
    successCriteria: goal.successCriteria ?? [],
  })
}

export async function deleteGrowthGoal(growthId: string, goalId: string): Promise<void> {
  await deleteJson(`/growth/${growthId}/goals/${goalId}`)
}

export async function addGrowthGoalAction(
  growthId: string,
  goalId: string,
  request: {
    title: string
    state: GrowthGoalActionState
    dueDateIso?: string
    priority?: Priority
    notes?: string
    links?: string[]
  },
): Promise<string> {
  const res = await postJson<{ id: string }>(`/growth/${growthId}/goals/${goalId}/actions`, {
    growthId,
    goalId,
    title: request.title,
    state: request.state,
    dueDate: request.dueDateIso ?? null,
    priority: request.priority ?? null,
    notes: request.notes ?? null,
    evidence: request.links?.length ? request.links.join('\n') : null,
  })
  return res.id
}

export async function updateGrowthGoalAction(
  growthId: string,
  goalId: string,
  action: GrowthGoalAction,
): Promise<void> {
  await putJson<void>(`/growth/${growthId}/goals/${goalId}/actions/${action.id}`, {
    growthId,
    goalId,
    actionId: action.id,
    title: action.title,
    state: action.state,
    dueDate: action.dueDateIso ?? null,
    priority: action.priority ?? null,
    notes: action.notes ?? null,
    evidence: action.links?.length ? action.links.join('\n') : null,
  })
}

export async function deleteGrowthGoalAction(growthId: string, goalId: string, actionId: string): Promise<void> {
  await deleteJson(`/growth/${growthId}/goals/${goalId}/actions/${actionId}`)
}

export async function addGrowthGoalCheckIn(
  growthId: string,
  goalId: string,
  request: { dateIso: string; signal: GrowthGoalCheckInSignal; note: string },
): Promise<string> {
  const res = await postJson<{ id: string }>(`/growth/${growthId}/goals/${goalId}/check-ins`, {
    growthId,
    goalId,
    date: request.dateIso,
    signal: request.signal,
    note: request.note,
  })
  return res.id
}

export async function updateGrowthGoalCheckIn(
  growthId: string,
  goalId: string,
  checkIn: GrowthGoalCheckIn,
): Promise<void> {
  await putJson<void>(`/growth/${growthId}/goals/${goalId}/check-ins/${checkIn.id}`, {
    growthId,
    goalId,
    checkInId: checkIn.id,
    date: checkIn.dateIso,
    signal: checkIn.signal,
    note: checkIn.note,
  })
}

export async function deleteGrowthGoalCheckIn(growthId: string, goalId: string, checkInId: string): Promise<void> {
  await deleteJson(`/growth/${growthId}/goals/${goalId}/check-ins/${checkInId}`)
}

export async function addFeedbackTheme(
  growthId: string,
  theme: Pick<GrowthFeedbackTheme, 'title' | 'description' | 'observedSinceLabel'>,
): Promise<string> {
  const res = await postJson<{ id: string }>(`/growth/${growthId}/feedback-themes`, {
    growthId,
    title: theme.title,
    description: theme.description,
    observedSinceLabel: theme.observedSinceLabel ?? null,
  })
  return res.id
}

export async function updateFeedbackTheme(growthId: string, theme: GrowthFeedbackTheme): Promise<void> {
  await putJson<void>(`/growth/${growthId}/feedback-themes/${theme.id}`, {
    growthId,
    themeId: theme.id,
    title: theme.title,
    description: theme.description,
    observedSinceLabel: theme.observedSinceLabel ?? null,
  })
}

export async function deleteFeedbackTheme(growthId: string, themeId: string): Promise<void> {
  await deleteJson(`/growth/${growthId}/feedback-themes/${themeId}`)
}
