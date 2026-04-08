const CURRENT_STATUSES = new Set([
  'active',
  'blocked',
  'in progress',
  'code review',
  'in review',
  'ready for review',
  'test acceptance',
  'ui acceptance',
])

export function isCurrentTicketStatus(status: string) {
  return CURRENT_STATUSES.has(status.trim().toLowerCase())
}
