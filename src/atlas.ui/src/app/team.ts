import type { AzureItem, DeliverySignal, LoadSignal, SupportNeededSignal, TeamMember, TeamNote } from './types'
import { daysSince, getDerivedTitle } from './utils'

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

function maxIso(values: Array<string | undefined | null>): string | undefined {
  let best: string | undefined
  let bestMs = Number.NEGATIVE_INFINITY
  for (const value of values) {
    if (!value) continue
    const ms = new Date(value).getTime()
    if (Number.isNaN(ms)) continue
    if (ms > bestMs) {
      bestMs = ms
      best = value
    }
  }
  return best
}

function formatSignalSummary(signals: {
  load: LoadSignal
  delivery: DeliverySignal
  supportNeeded: SupportNeededSignal
}): string | undefined {
  const parts: string[] = []
  if (signals.load === 'Heavy') parts.push('Heavy load')
  else if (signals.load === 'Light') parts.push('Light load')

  if (signals.delivery === 'Blocked') parts.push('Blocked')
  else if (signals.delivery === 'AtRisk') parts.push('Delivery at risk')

  if (signals.supportNeeded === 'High') parts.push('High support need')
  else if (signals.supportNeeded === 'Medium') parts.push('Medium support need')

  if (parts.length === 0) return undefined
  return parts.join(' · ')
}

function formatRelativeDays(iso: string): string {
  const days = daysSince(iso)
  if (days === undefined) return 'recently'
  if (days === 0) return 'today'
  if (days === 1) return '1 day ago'
  return `${days} days ago`
}

/**
 * Derives Team Pulse / activitySnapshot from existing member data (no persistence).
 * Call after mapping from API and after optimistic cache updates.
 */
export function deriveActivitySnapshot(member: {
  currentFocus: string
  signals: TeamMember['signals']
  notes: TeamNote[]
  azureItems: AzureItem[]
}): TeamMember['activitySnapshot'] {
  const noteTimes = member.notes.map((n) => n.lastModifiedIso ?? n.createdIso)
  const azureTimes = member.azureItems.map((a) => a.changedDateUtc)
  const lastUpdatedIso = maxIso([...noteTimes, ...azureTimes])

  const bullets: string[] = []

  const focus = member.currentFocus.trim()
  if (focus) bullets.push(`Focus: ${focus}`)

  const signalSummary = formatSignalSummary(member.signals)
  if (signalSummary) bullets.push(signalSummary)

  const openItems = member.azureItems.filter((a) => isCurrentTicketStatus(a.status))
  if (openItems.length === 1) {
    bullets.push(`Active: ${openItems[0].title}`)
  } else if (openItems.length > 1) {
    bullets.push(`${openItems.length} active work items`)
  } else if (member.azureItems.length > 0) {
    bullets.push(`Recent: ${member.azureItems[0].title}`)
  }

  const latestNote = [...member.notes].sort((a, b) => {
    const aIso = a.lastModifiedIso ?? a.createdIso
    const bIso = b.lastModifiedIso ?? b.createdIso
    return new Date(bIso).getTime() - new Date(aIso).getTime()
  })[0]
  if (latestNote) {
    const when = formatRelativeDays(latestNote.lastModifiedIso ?? latestNote.createdIso)
    bullets.push(`${latestNote.tag} · ${getDerivedTitle(latestNote)} · ${when}`)
  }

  if (!lastUpdatedIso && bullets.length === 0) {
    return { bullets: [], lastUpdatedIso: undefined, quickTags: undefined }
  }

  return {
    bullets: bullets.slice(0, 5),
    lastUpdatedIso,
    quickTags: undefined,
  }
}

/** Recompute activitySnapshot on a TeamMember (e.g. after optimistic cache patches). */
export function withDerivedActivitySnapshot(member: TeamMember): TeamMember {
  return {
    ...member,
    activitySnapshot: deriveActivitySnapshot(member),
  }
}
