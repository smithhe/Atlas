export interface ParsedDuration {
  totalMinutes: number
  parts: { days: number; hours: number; minutes: number }
}

/**
 * Parses flexible duration text like:
 * - "1d", "2h", "30m"
 * - "2h30m", "1d 2h", "1d2h30m"
 * - "1.5h" (treated as 90m)
 *
 * Rules:
 * - Units supported: d, h, m (case-insensitive)
 * - Whitespace is ignored
 * - Multiple tokens allowed; units can appear at most once (e.g., "1h2h" invalid)
 */
export function parseDurationText(input: string): ParsedDuration | null {
  const raw = (input ?? '').trim()
  if (!raw) return null

  const s = raw.replace(/\s+/g, '').toLowerCase()
  const tokenRe = /(\d+(?:\.\d+)?)([dhm])/g

  let match: RegExpExecArray | null
  let consumed = 0
  const seen = new Set<string>()

  let days = 0
  let hours = 0
  let minutes = 0

  while ((match = tokenRe.exec(s)) !== null) {
    const value = Number.parseFloat(match[1])
    const unit = match[2]
    if (!Number.isFinite(value) || value < 0) return null
    if (seen.has(unit)) return null
    seen.add(unit)
    consumed += match[0].length

    if (unit === 'd') days = value
    if (unit === 'h') hours = value
    if (unit === 'm') minutes = value
  }

  // Reject any unknown characters.
  if (consumed !== s.length) return null

  const totalMinutes =
    Math.round(days * 24 * 60) +
    Math.round(hours * 60) +
    Math.round(minutes)

  if (totalMinutes <= 0) return null

  // Normalize into integer parts for display.
  const normDays = Math.floor(totalMinutes / (24 * 60))
  const remAfterDays = totalMinutes - normDays * 24 * 60
  const normHours = Math.floor(remAfterDays / 60)
  const normMinutes = remAfterDays - normHours * 60

  return {
    totalMinutes,
    parts: { days: normDays, hours: normHours, minutes: normMinutes },
  }
}

export function formatDurationFromMinutes(totalMinutes: number): string {
  if (!Number.isFinite(totalMinutes) || totalMinutes <= 0) return '—'
  const mins = Math.round(totalMinutes)
  const days = Math.floor(mins / (24 * 60))
  const remAfterDays = mins - days * 24 * 60
  const hours = Math.floor(remAfterDays / 60)
  const minutes = remAfterDays - hours * 60

  const parts: string[] = []
  if (days) parts.push(`${days}d`)
  if (hours) parts.push(`${hours}h`)
  if (minutes) parts.push(`${minutes}m`)
  return parts.join(' ')
}


