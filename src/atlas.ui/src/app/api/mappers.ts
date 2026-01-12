import type { Confidence, Growth, GrowthGoalActionState, GrowthGoalCheckInSignal, GrowthGoalStatus, Priority, ProductOwner, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import { teamMemberLocalExtrasById, teamNoteLocalExtrasById } from '../localExtras'

export interface SettingsDto {
  staleDays: number
  defaultAiManualOnly: boolean
  theme: 'Dark' | 'Light'
  azureDevOpsBaseUrl?: string | null
}

export interface ProductOwnerListItemDto {
  id: string
  name: string
}

export interface TaskDto {
  id: string
  title: string
  priority: Priority
  status: 'NotStarted' | 'InProgress' | 'Blocked' | 'Done'
  assigneeId?: string | null
  projectId?: string | null
  riskId?: string | null
  dueDate?: string | null // ISO date
  dependencyTaskIds: string[]
  estimatedDurationText: string
  estimateConfidence: Confidence
  actualDurationText?: string | null
  notes: string
  lastTouchedAt: string
}

export interface ProjectTagDto {
  value: string
}
export interface ProjectLinkDto {
  label: string
  url: string
}
export interface ProjectDto {
  id: string
  name: string
  summary: string
  description?: string | null
  status?: 'Active' | 'Paused' | 'Completed' | null
  health?: 'Green' | 'Yellow' | 'Red' | null
  targetDate?: string | null
  priority?: Priority | null
  productOwnerId?: string | null
  tags: ProjectTagDto[]
  links: ProjectLinkDto[]
  lastUpdatedAt?: string | null
  linkedTaskIds: string[]
  linkedRiskIds: string[]
  teamMemberIds: string[]
}

export interface RiskHistoryEntryDto {
  id: string
  text: string
  createdAt: string
}
export interface RiskDto {
  id: string
  title: string
  status: 'Open' | 'Watching' | 'Resolved'
  severity: 'Low' | 'Medium' | 'High'
  projectId?: string | null
  description: string
  evidence: string
  linkedTaskIds: string[]
  linkedTeamMemberIds: string[]
  history: RiskHistoryEntryDto[]
  lastUpdatedAt: string
}

export interface TeamMemberProfileDto {
  timeZone?: string | null
  typicalHours?: string | null
}
export interface TeamMemberSignalsDto {
  load: 'Light' | 'Normal' | 'Heavy'
  delivery: 'AtRisk' | 'OnTrack' | 'Blocked'
  supportNeeded: 'Low' | 'Medium' | 'High'
}
export interface TeamNoteDto {
  id: string
  createdAt: string
  lastModifiedAt?: string | null
  type: 'Blocker' | 'Progress' | 'Concern' | 'Praise' | 'Standup' | 'Quick'
  title?: string | null
  text: string
  pinnedOrder?: number | null
}
export interface TeamMemberRiskDto {
  id: string
  title: string
  severity: 'Low' | 'Medium' | 'High'
  riskType: string
  status: 'Open' | 'Mitigating' | 'Resolved'
  trend: 'Improving' | 'Stable' | 'Worsening'
  firstNoticedDate: string
  impactArea: string
  description: string
  currentAction: string
  lastReviewedAt?: string | null
  linkedGlobalRiskId?: string | null
}
export interface TeamMemberDto {
  id: string
  name: string
  role: string
  statusDot: 'Green' | 'Yellow' | 'Red'
  currentFocus: string
  profile: TeamMemberProfileDto
  signals: TeamMemberSignalsDto
  notes: TeamNoteDto[]
  risks: TeamMemberRiskDto[]
  projectIds: string[]
  linkedGlobalRiskIds: string[]
}

export interface GrowthFeedbackThemeDto {
  id: string
  title: string
  description: string
  observedSinceLabel?: string | null
}
export interface GrowthGoalActionDto {
  id: string
  title: string
  dueDate?: string | null
  state: GrowthGoalActionState
  priority?: Priority | null
  notes?: string | null
  evidence?: string | null
}
export interface GrowthGoalCheckInDto {
  id: string
  date: string
  signal: GrowthGoalCheckInSignal
  note: string
}
export interface GrowthGoalDto {
  id: string
  title: string
  description: string
  status: GrowthGoalStatus
  category?: string | null
  priority?: Priority | null
  startDate?: string | null
  targetDate?: string | null
  lastUpdatedAt?: string | null
  progressPercent?: number | null
  summary?: string | null
  successCriteria: string[]
  actions: GrowthGoalActionDto[]
  checkIns: GrowthGoalCheckInDto[]
}
export interface GrowthDto {
  id: string
  teamMemberId: string
  goals: GrowthGoalDto[]
  skillsInProgress: string[]
  feedbackThemes: GrowthFeedbackThemeDto[]
  focusAreasMarkdown: string
}

export function mapSettings(dto: SettingsDto): Settings {
  return {
    staleDays: dto.staleDays,
    defaultAiManualOnly: dto.defaultAiManualOnly,
    theme: dto.theme,
    azureDevOpsBaseUrl: dto.azureDevOpsBaseUrl ?? undefined,
  }
}

export function mapProductOwner(dto: ProductOwnerListItemDto): ProductOwner {
  return { id: dto.id, name: dto.name }
}

function mapTaskStatus(status: TaskDto['status']): Task['status'] {
  switch (status) {
    case 'NotStarted':
      return 'Not Started'
    case 'InProgress':
      return 'In Progress'
    case 'Blocked':
      return 'Blocked'
    case 'Done':
      return 'Done'
  }
}

export function mapTask(dto: TaskDto, lookups: { projectNameById: Map<string, string>; riskTitleById: Map<string, string> }): Task {
  const project = dto.projectId ? lookups.projectNameById.get(dto.projectId) : undefined
  const risk = dto.riskId ? lookups.riskTitleById.get(dto.riskId) : undefined

  return {
    id: dto.id,
    title: dto.title,
    priority: dto.priority,
    status: mapTaskStatus(dto.status),
    assigneeId: dto.assigneeId ?? undefined,
    project,
    risk,
    dueDate: dto.dueDate ?? undefined,
    dependencyTaskIds: dto.dependencyTaskIds ?? [],
    estimatedDurationText: dto.estimatedDurationText,
    estimateConfidence: dto.estimateConfidence,
    actualDurationText: dto.actualDurationText ?? undefined,
    notes: dto.notes,
    lastTouchedIso: dto.lastTouchedAt,
  }
}

export function mapProject(dto: ProjectDto): Project {
  return {
    id: dto.id,
    name: dto.name,
    summary: dto.summary,
    description: dto.description ?? undefined,
    status: dto.status ?? undefined,
    health: dto.health ?? undefined,
    targetDateIso: dto.targetDate ?? undefined,
    priority: dto.priority ?? undefined,
    productOwnerId: dto.productOwnerId ?? undefined,
    tags: (dto.tags ?? []).map((t) => t.value),
    links: (dto.links ?? []).map((l) => ({ label: l.label, url: l.url })),
    lastUpdatedIso: dto.lastUpdatedAt ?? undefined,
    linkedTaskIds: dto.linkedTaskIds ?? [],
    linkedRiskIds: dto.linkedRiskIds ?? [],
    teamMemberIds: dto.teamMemberIds ?? [],
  }
}

export function mapRisk(dto: RiskDto, lookups: { projectNameById: Map<string, string> }): Risk {
  const project = dto.projectId ? lookups.projectNameById.get(dto.projectId) : undefined
  return {
    id: dto.id,
    title: dto.title,
    status: dto.status,
    severity: dto.severity,
    project,
    ownerId: undefined,
    description: dto.description,
    evidence: dto.evidence,
    linkedTaskIds: dto.linkedTaskIds ?? [],
    linkedTeamMemberIds: dto.linkedTeamMemberIds ?? [],
    history: (dto.history ?? []).map((h) => ({ id: h.id, createdIso: h.createdAt, text: h.text })),
    lastUpdatedIso: dto.lastUpdatedAt,
  }
}

export function mapTeamMember(dto: TeamMemberDto): { member: TeamMember; memberRisks: TeamMemberRisk[] } {
  const notes = (dto.notes ?? []).map((n) => {
    const local = teamNoteLocalExtrasById[n.id]
    return {
      id: n.id,
      createdIso: n.createdAt,
      lastModifiedIso: n.lastModifiedAt ?? undefined,
      tag: n.type,
      title: n.title ?? undefined,
      text: n.text,
      adoWorkItemId: local?.adoWorkItemId,
      prUrl: local?.prUrl,
    }
  })

  const pinnedNoteIds = (dto.notes ?? [])
    .filter((n) => n.pinnedOrder != null)
    .sort((a, b) => (a.pinnedOrder ?? 0) - (b.pinnedOrder ?? 0))
    .map((n) => n.id)

  const local = teamMemberLocalExtrasById[dto.id]
  const azureItems = local?.azureItems ?? []
  const activitySnapshot = local?.activitySnapshot ?? { bullets: [], lastUpdatedIso: undefined, quickTags: undefined }

  const member: TeamMember = {
    id: dto.id,
    name: dto.name,
    role: dto.role || undefined,
    statusDot: dto.statusDot,
    currentFocus: dto.currentFocus,
    profile: {
      timeZone: dto.profile.timeZone ?? undefined,
      typicalHours: dto.profile.typicalHours ?? undefined,
    },
    signals: dto.signals,
    notes,
    pinnedNoteIds,
    activitySnapshot,
    azureItems,
  }

  const memberRisks: TeamMemberRisk[] = (dto.risks ?? []).map((r) => ({
    id: r.id,
    memberId: dto.id,
    title: r.title,
    severity: r.severity,
    riskType: r.riskType,
    status: r.status,
    trend: r.trend,
    firstNoticedDateIso: r.firstNoticedDate,
    impactArea: r.impactArea,
    description: r.description,
    currentAction: r.currentAction,
    lastReviewedIso: r.lastReviewedAt ?? undefined,
    linkedRiskId: r.linkedGlobalRiskId ?? undefined,
  }))

  return { member, memberRisks }
}

export function mapGrowth(dto: GrowthDto): Growth {
  return {
    id: dto.id,
    memberId: dto.teamMemberId,
    goals: (dto.goals ?? []).map((g) => ({
      id: g.id,
      title: g.title,
      description: g.description,
      status: g.status,
      category: g.category ?? undefined,
      priority: g.priority ?? undefined,
      startDateIso: g.startDate ?? undefined,
      targetDateIso: g.targetDate ?? undefined,
      lastUpdatedIso: g.lastUpdatedAt ?? undefined,
      progressPercent: g.progressPercent ?? undefined,
      summary: g.summary ?? undefined,
      successCriteria: g.successCriteria ?? [],
      actions: (g.actions ?? []).map((a) => ({
        id: a.id,
        title: a.title,
        dueDateIso: a.dueDate ?? undefined,
        state: a.state,
        priority: a.priority ?? undefined,
        notes: a.notes ?? undefined,
        links: a.evidence ? a.evidence.split('\n').map((s) => s.trim()).filter(Boolean) : [],
      })),
      checkIns: (g.checkIns ?? []).map((c) => ({
        id: c.id,
        dateIso: c.date,
        signal: c.signal,
        note: c.note,
      })),
    })),
    skillsInProgress: dto.skillsInProgress ?? [],
    feedbackThemes: (dto.feedbackThemes ?? []).map((t) => ({
      id: t.id,
      title: t.title,
      description: t.description,
      observedSinceLabel: t.observedSinceLabel ?? undefined,
    })),
    focusAreasMarkdown: dto.focusAreasMarkdown ?? '',
  }
}

