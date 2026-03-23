export function isCurrentTicketStatus(status: string) {
  const s = status.trim().toLowerCase()
  const currentStatuses = new Set([
    'active',
    'blocked',
    'in progress',
    'code review',
    'in review',
    'ready for review',
    'test acceptance',
    'ui acceptance',
  ])
  return currentStatuses.has(s)
}


