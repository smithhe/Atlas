import type {
  GrowthGoal,
  GrowthGoalActionState,
  GrowthGoalCheckInSignal,
  HealthSignal,
  Priority,
  Risk,
  RiskStatus,
  TaskStatus,
} from './types'

export function taskStatusTone(s?: TaskStatus) {
  if (s === 'Done') return 'toneGood'
  if (s === 'Blocked') return 'toneBad'
  if (s === 'In Progress') return 'toneInfo'
  return 'toneNeutral'
}

export function priorityTone(p?: Priority) {
  if (!p || p === 'Low') return 'toneNeutral'
  if (p === 'Medium') return 'toneWarn'
  return 'toneBad'
}

export function riskStatusTone(status: RiskStatus) {
  if (status === 'Open') return 'toneBad'
  if (status === 'Watching') return 'toneWarn'
  if (status === 'Resolved') return 'toneGood'
  return 'toneNeutral'
}

export function severityTone(sev: Risk['severity']) {
  if (sev === 'High') return 'toneBad'
  if (sev === 'Medium') return 'toneWarn'
  return 'toneNeutral'
}

export function healthTone(health?: HealthSignal) {
  if (health === 'Green') return 'toneGood'
  if (health === 'Yellow') return 'toneWarn'
  if (health === 'Red') return 'toneBad'
  return 'toneNeutral'
}

export function projectStatusTone(status?: string) {
  if (status === 'Active') return 'toneGood'
  if (status === 'Paused') return 'toneWarn'
  return 'toneNeutral'
}

export function goalStatusTone(status: GrowthGoal['status']) {
  if (status === 'Completed' || status === 'OnTrack') return 'toneGood'
  if (status === 'NeedsAttention') return 'toneWarn'
  return 'toneNeutral'
}

export function actionStateTone(state: GrowthGoalActionState) {
  if (state === 'Complete') return 'toneGood'
  if (state === 'InProgress') return 'toneWarn'
  return 'toneNeutral'
}

export function checkInSignalTone(signal: GrowthGoalCheckInSignal) {
  if (signal === 'Positive') return 'toneGood'
  if (signal === 'Mixed') return 'toneWarn'
  if (signal === 'Concern') return 'toneBad'
  return 'toneNeutral'
}

export function signalTone(value: string) {
  const v = value.toLowerCase()
  if (v.includes('blocked')) return 'toneBad'
  if (v.includes('atrisk') || v.includes('heavy') || v === 'high' || v.includes('medium')) return 'toneWarn'
  if (v.includes('ontrack') || v.includes('light') || v === 'low') return 'toneGood'
  return 'toneNeutral'
}

export function ticketAttentionTone(status: string) {
  const s = status.toLowerCase()
  if (s.includes('blocked')) return 'toneBad'
  if (s.includes('code review') || s.includes('in review') || s.includes('review')) return 'toneWarn'
  return 'toneNeutral'
}
