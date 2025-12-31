export type Id = string

export type Priority = 'Low' | 'Medium' | 'High' | 'Critical'
export type RiskStatus = 'Open' | 'Watching' | 'Resolved'
export type NoteTag = 'Blocker' | 'Progress' | 'Concern' | 'Praise' | 'Standup' | 'Quick'
export type Confidence = 'Low' | 'Medium' | 'High'

export type LoadSignal = 'Light' | 'Normal' | 'Heavy'
export type DeliverySignal = 'AtRisk' | 'OnTrack' | 'Blocked'
export type SupportNeededSignal = 'Low' | 'Medium' | 'High'

export interface Task {
  id: Id
  title: string
  priority: Priority
  project?: string
  risk?: string
  dueDate?: string // ISO date (YYYY-MM-DD)
  estimatedDurationText: string
  estimateConfidence: Confidence
  actualDurationText?: string
  notes: string
  lastTouchedIso: string
}

export interface TeamNote {
  id: Id
  createdIso: string
  tag: NoteTag
  title?: string
  text: string
  adoWorkItemId?: string
  prUrl?: string
}

export interface AzureItem {
  id: Id
  title: string
  status: string
  timeTaken?: string
  ticketUrl?: string
  prUrl?: string
  commitsUrl?: string
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
  description: string
  evidence: string
  linkedTaskIds: Id[]
  linkedTeamMemberIds: Id[]
  history: { id: Id; createdIso: string; text: string }[]
  lastUpdatedIso: string
}

export interface Project {
  id: Id
  name: string
  summary: string
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


