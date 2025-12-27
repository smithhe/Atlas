export function isCurrentTicketStatus(status: string) {
  const s = status.toLowerCase()
  return s.includes('in progress') || s.includes('blocked') || s.includes('code review') || s.includes('in review')
}


