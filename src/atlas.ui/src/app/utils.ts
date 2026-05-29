import type { TeamNote } from './types'

export function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

export function isGuid(id: string): boolean {
  return GUID_RE.test(id)
}

export function reportSaveError(err: unknown, fallbackMessage: string) {
  console.error(fallbackMessage, err)
  window.alert(err instanceof Error ? err.message : fallbackMessage)
}

export function daysSince(iso: string): number
export function daysSince(iso: string | undefined): number | undefined
export function daysSince(iso?: string): number | undefined {
  if (!iso) return undefined
  const a = new Date(iso).getTime()
  if (Number.isNaN(a)) return undefined
  return Math.max(0, Math.floor((Date.now() - a) / (1000 * 60 * 60 * 24)))
}

export function stripMarkdownHeadings(line: string) {
  return line.replace(/^#{1,6}\s+/, '').trim()
}

export function getDerivedTitle(note: TeamNote) {
  const explicit = note.title?.trim()
  if (explicit) return explicit
  const firstNonEmpty = note.text
    .split('\n')
    .map((l) => l.trim())
    .find((l) => l.length > 0)
  return stripMarkdownHeadings(firstNonEmpty ?? '(untitled)')
}

export function formatIsoDate(iso?: string) {
  if (!iso) return '—'
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

export function formatIsoDateLong(iso?: string) {
  if (!iso) return '—'
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return iso
  return d.toLocaleDateString(undefined, { month: 'long', day: 'numeric', year: 'numeric' })
}

export function formatIsoDateShort(iso?: string) {
  if (!iso) return '—'
  const d = new Date(`${iso}T00:00:00`)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { month: 'short', day: '2-digit' })
}

export function formatReadableDateTime(iso: string) {
  const dt = new Date(iso)
  return dt.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: 'numeric',
    minute: '2-digit',
  })
}
