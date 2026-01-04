export type Id = string

export type Priority = 'Low' | 'Medium' | 'High' | 'Critical'
export type RiskStatus = 'Open' | 'Watching' | 'Resolved'
export type NoteTag = 'Blocker' | 'Progress' | 'Concern' | 'Praise' | 'Standup' | 'Quick'
export type Confidence = 'Low' | 'Medium' | 'High'

export type LoadSignal = 'Light' | 'Normal' | 'Heavy'
export type DeliverySignal = 'AtRisk' | 'OnTrack' | 'Blocked'
export type SupportNeededSignal = 'Low' | 'Medium' | 'High'

export type TeamMemberRiskSeverity = 'Low' | 'Medium' | 'High'
export type TeamMemberRiskStatus = 'Open' | 'Mitigating' | 'Resolved'
export type TeamMemberRiskTrend = 'Improving' | 'Stable' | 'Worsening'

export type ProjectStatus = 'Active' | 'Paused' | 'Completed'
export type HealthSignal = 'Green' | 'Yellow' | 'Red'

export type TaskStatus = 'Not Started' | 'In Progress' | 'Blocked' | 'Done'

export interface Task {
  id: Id
  title: string
  priority: Priority
  status?: TaskStatus
  assigneeId?: Id
  summary?: string
  project?: string
  risk?: string
  dueDate?: string // ISO date (YYYY-MM-DD)
  /**
   * IDs of tasks this task is blocked by (i.e., dependencies that must be completed first).
   */
  dependencyTaskIds: Id[]
  estimatedDurationText: string
  estimateConfidence: Confidence
  actualDurationText?: string
  notes: string
  lastTouchedIso: string
}

export interface TeamNote {
  id: Id
  createdIso: string
  lastModifiedIso?: string
  tag: NoteTag
  title?: string
  text: string
}

export interface AzureItem {
  id: Id
  title: string
  status: string
  timeTaken?: string
  ticketUrl?: string
  prUrls?: string[]
  commitsUrl?: string

  /**
   * Local-only fields (not sourced from Azure DevOps sync; safe to keep even after sync exists).
   */
  assignedTo?: string
  startDateIso?: string // ISO date (YYYY-MM-DD)
  projectId?: Id
  localNotes?: WorkItemNote[]

  /**
   * Sync-owned history (imported from Azure DevOps). Local edits should NOT append to this.
   */
  history?: WorkItemHistoryEntry[]
}

export interface WorkItemNote {
  id: Id
  createdIso: string
  text: string
}

export interface WorkItemHistoryEntry {
  id: Id
  createdIso: string
  kind: 'StateChanged' | 'AssignedToChanged' | 'CommentAdded' | 'PullRequestLinked' | 'Other'
  summary: string
}

export interface TeamMember {
  id: Id
  name: string
  role?: string
  statusDot: 'Green' | 'Yellow' | 'Red'
  currentFocus: string
  notes: TeamNote[]
  azureItems: AzureItem[]

  profile: {
    timeZone?: string
    typicalHours?: string
  }
  signals: {
    load: LoadSignal
    delivery: DeliverySignal
    supportNeeded: SupportNeededSignal
  }
  pinnedNoteIds: Id[]
  activitySnapshot: {
    bullets: string[]
    lastUpdatedIso?: string
    quickTags?: string[]
  }
}

export interface Risk {
  id: Id
  title: string
  status: RiskStatus
  severity: 'Low' | 'Medium' | 'High'
  project?: string
  ownerId?: Id
  description: string
  evidence: string
  linkedTaskIds: Id[]
  linkedTeamMemberIds: Id[]
  history: { id: Id; createdIso: string; text: string }[]
  lastUpdatedIso: string
}

export interface TeamMemberRisk {
  id: Id
  memberId: Id

  title: string

  severity: TeamMemberRiskSeverity
  riskType: string
  status: TeamMemberRiskStatus
  trend: TeamMemberRiskTrend

  /**
   * ISO date (YYYY-MM-DD)
   */
  firstNoticedDateIso: string
  impactArea: string

  description: string
  currentAction: string

  lastReviewedIso?: string

  /**
   * Optional reference to a global Risk (Risks & Mitigation tab).
   * This is a loose link only: the TeamMemberRisk remains its own independent record.
   */
  linkedRiskId?: Id
}

export interface Project {
  id: Id
  name: string
  summary: string
  description?: string
  status?: ProjectStatus
  health?: HealthSignal
  targetDateIso?: string // ISO date (YYYY-MM-DD)
  priority?: Priority
  productOwnerId?: Id
  tags?: string[]
  links?: { label: string; url: string }[]
  lastUpdatedIso?: string // ISO datetime
  latestCheckIn?: { dateIso: string; note: string }
  linkedTaskIds: Id[]
  linkedRiskIds: Id[]
  teamMemberIds: Id[]
}

export interface Settings {
  staleDays: number
  defaultAiManualOnly: true
  theme: 'Dark'
  azureDevOpsBaseUrl?: string
}

export type GrowthGoalStatus = 'OnTrack' | 'NeedsAttention' | 'Completed'

export type GrowthGoalActionState = 'Planned' | 'InProgress' | 'Complete'
export type GrowthGoalCheckInSignal = 'Positive' | 'Mixed' | 'Concern'

/**
 * Growth is tracked per team member. This is the top-level record we can expand over time
 * (sub-entities like goals, check-ins, experiments, etc.).
 */
export interface Growth {
  id: Id
  memberId: Id

  /**
   * Sub-entities (we'll expand these next).
   */
  goals: GrowthGoal[]

  /**
   * Lightweight summary surfaces.
   */
  skillsInProgress: string[]
  feedbackThemes: GrowthFeedbackTheme[]
  /**
   * Freeform markdown field for current focus areas.
   * (Allows short bullets, headings, links, etc.)
   */
  focusAreasMarkdown: string
}

export interface GrowthGoal {
  id: Id
  title: string
  description: string
  status: GrowthGoalStatus

  category?: string
  priority?: Priority

  /**
   * Detail view fields
   */
  startDateIso?: string // ISO date (YYYY-MM-DD)
  targetDateIso?: string // ISO date (YYYY-MM-DD)
  lastUpdatedIso?: string // ISO datetime
  progressPercent?: number // 0-100
  summary?: string
  successCriteria?: string[] // bullets

  actions: GrowthGoalAction[]
  checkIns: GrowthGoalCheckIn[]
}

export interface GrowthFeedbackTheme {
  id: Id
  title: string
  description: string
  observedSinceLabel?: string
}

export interface GrowthGoalAction {
  id: Id
  title: string
  dueDateIso?: string // ISO date (YYYY-MM-DD)
  state: GrowthGoalActionState
  priority?: Priority
  notes?: string
  links?: string[]
}

export interface GrowthGoalCheckIn {
  id: Id
  dateIso: string // ISO date (YYYY-MM-DD)
  signal: GrowthGoalCheckInSignal
  note: string
}


