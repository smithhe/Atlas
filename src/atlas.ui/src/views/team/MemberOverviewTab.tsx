import { useMemo } from 'react'
import type { TeamMember, TeamNote } from '../../app/types'
import { isCurrentTicketStatus } from '../../app/team'

const US_TIMEZONE_OPTIONS = [
  { value: 'PT', label: 'Pacific (PT)' },
  { value: 'MT', label: 'Mountain (MT)' },
  { value: 'CT', label: 'Central (CT)' },
  { value: 'ET', label: 'Eastern (ET)' },
  { value: 'AK', label: 'Alaska (AK)' },
  { value: 'HI', label: 'Hawaii (HI)' },
  { value: 'AZ', label: 'Arizona (MT-noDST)' },
] as const

function cycle<T extends string>(value: T, options: readonly T[]): T {
  const idx = options.indexOf(value)
  if (idx < 0) return options[0]
  return options[(idx + 1) % options.length]
}

function stripMarkdownHeadings(line: string) {
  return line.replace(/^#{1,6}\\s+/, '').trim()
}

function getDerivedTitle(note: TeamNote) {
  const explicit = note.title?.trim()
  if (explicit) return explicit
  const firstNonEmpty = note.text
    .split('\n')
    .map((l) => l.trim())
    .find((l) => l.length > 0)
  return stripMarkdownHeadings(firstNonEmpty ?? '(untitled)')
}

function getPreview(note: TeamNote) {
  const text = note.text.replace(/\\s+/g, ' ').trim()
  if (text.length <= 120) return text
  return text.slice(0, 120).trim() + '…'
}

function signalTone(value: string) {
  const v = value.toLowerCase()
  if (v.includes('blocked')) return 'toneBad'
  if (v.includes('atrisk')) return 'toneWarn'
  if (v.includes('heavy')) return 'toneWarn'
  if (v === 'high') return 'toneWarn'
  if (v.includes('medium')) return 'toneWarn'
  if (v.includes('ontrack')) return 'toneGood'
  if (v.includes('light')) return 'toneGood'
  if (v === 'low') return 'toneGood'
  return 'toneNeutral'
}

function ticketAttentionTone(status: string) {
  const s = status.toLowerCase()
  if (s.includes('blocked')) return 'toneBad'
  if (s.includes('code review') || s.includes('in review') || s.includes('review')) return 'toneWarn'
  return 'toneNeutral'
}

export function MemberOverviewTab({
  member,
  onUpdate,
  onGoToNotes,
  onGoToWorkItem,
}: {
  member: TeamMember
  onUpdate: (patch: Partial<TeamMember>) => void
  onGoToNotes: () => void
  onGoToWorkItem: (workItemId: string) => void
}) {
  const currentTickets = useMemo(() => member.azureItems.filter((a) => isCurrentTicketStatus(a.status)), [member.azureItems])

  const pinnedNotes = useMemo(() => {
    const byId = new Map(member.notes.map((n) => [n.id, n] as const))
    return member.pinnedNoteIds.map((id) => byId.get(id)).filter((x): x is TeamNote => !!x).slice(0, 3)
  }, [member.notes, member.pinnedNoteIds])

  const lastUpdated = member.activitySnapshot.lastUpdatedIso ? new Date(member.activitySnapshot.lastUpdatedIso) : null

  const loadOptions = ['Light', 'Normal', 'Heavy'] as const
  const deliveryOptions = ['AtRisk', 'OnTrack', 'Blocked'] as const
  const supportNeededOptions = ['Low', 'Medium', 'High'] as const

  return (
    <div className="memberOverviewGrid" aria-label="Team member overview">
      <section className="card subtle" aria-label="Profile">
        <div className="cardHeader">
          <div className="cardTitle">Profile</div>
        </div>
        <div className="pad">
          <div className="memberOverviewKv">
            <div className="memberOverviewKvRow">
              <div className="memberOverviewKvKey">Name</div>
              <div className="memberOverviewKvVal">{member.name}</div>
            </div>
            <div className="memberOverviewKvRow">
              <div className="memberOverviewKvKey">Role / Level</div>
              <div className="memberOverviewKvVal">{member.role ?? '—'}</div>
            </div>
          </div>

          <div className="fieldGrid" style={{ marginTop: 12 }}>
            <label className="field">
              <div className="fieldLabel">Time zone</div>
              <select
                className="select"
                value={member.profile.timeZone ?? ''}
                onChange={(e) => onUpdate({ profile: { ...member.profile, timeZone: e.target.value || undefined } })}
              >
                <option value="">(Select)</option>
                {US_TIMEZONE_OPTIONS.map((tz) => (
                  <option key={tz.value} value={tz.value}>
                    {tz.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="field">
              <div className="fieldLabel">Typical hours</div>
              <input
                className="input"
                placeholder="e.g., 9–5"
                value={member.profile.typicalHours ?? ''}
                onChange={(e) => onUpdate({ profile: { ...member.profile, typicalHours: e.target.value || undefined } })}
              />
            </label>
          </div>
        </div>
      </section>

      <section className="card subtle memberOverviewFocus" aria-label="Focus Right Now">
        <div className="cardHeader">
          <div className="cardTitle">Focus Right Now</div>
        </div>
        <div className="pad memberOverviewFocusBody">
          {currentTickets.length === 0 ? (
            <div className="muted">No current tickets.</div>
          ) : (
            <div className="focusTicketsList" role="list">
              {currentTickets.map((t) => (
                <button
                  key={t.id}
                  type="button"
                  className={`focusTicketRow focusTicketRowBtn ${ticketAttentionTone(t.status)}`}
                  role="listitem"
                  onClick={() => onGoToWorkItem(t.id)}
                >
                  <div className="listMain">
                    <div className="listTitle">
                      {t.id} — {t.title}
                    </div>
                    <div className="listMeta">{t.status}</div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="card subtle" aria-label="At-a-glance signals">
        <div className="cardHeader">
          <div className="cardTitle">At-a-glance Signals</div>
        </div>
        <div className="pad">
          <div className="signalPillsRow" aria-label="Signals">
            <button
              type="button"
              className={`pill pillBtn ${signalTone(member.signals.load)}`}
              onClick={() => onUpdate({ signals: { ...member.signals, load: cycle(member.signals.load, loadOptions) } })}
            >
              Load: {member.signals.load}
            </button>
            <button
              type="button"
              className={`pill pillBtn ${signalTone(member.signals.delivery)}`}
              onClick={() =>
                onUpdate({ signals: { ...member.signals, delivery: cycle(member.signals.delivery, deliveryOptions) } })
              }
            >
              Delivery:{' '}
              {member.signals.delivery === 'OnTrack' ? 'On Track' : member.signals.delivery === 'AtRisk' ? 'At Risk' : 'Blocked'}
            </button>
            <button
              type="button"
              className={`pill pillBtn ${signalTone(member.signals.supportNeeded)}`}
              onClick={() =>
                onUpdate({
                  signals: {
                    ...member.signals,
                    supportNeeded: cycle(member.signals.supportNeeded, supportNeededOptions),
                  },
                })
              }
            >
              Support needed: {member.signals.supportNeeded}
            </button>
          </div>
          <div className="mutedSmall" style={{ marginTop: 12 }}>
            Manually maintained, non-evaluative signals
          </div>
        </div>
      </section>

      <section className="card subtle span2" aria-label="Pinned Notes preview">
        <div className="cardHeader">
          <div className="cardTitle">Pinned Notes (Preview)</div>
          <button className="btn btnGhost" type="button" onClick={onGoToNotes}>
            View all notes
          </button>
        </div>
        <div className="pad pinnedNotesPreview">
          {pinnedNotes.length === 0 ? (
            <div className="muted">No pinned notes.</div>
          ) : (
            <div className="notesList" style={{ marginTop: 0 }}>
              {pinnedNotes.map((n) => (
                <button
                  key={n.id}
                  type="button"
                  className="noteRow noteRowBtn"
                  onClick={onGoToNotes}
                  title="Open Notes tab"
                >
                  <div className="noteMeta">
                    <span className={`chip chipTag chipTag-${n.tag.toLowerCase()}`}>{n.tag}</span>
                    <span className="mutedSmall">{new Date(n.createdIso).toLocaleDateString()}</span>
                  </div>
                  <div className="listTitle listTitleWrap">{getDerivedTitle(n)}</div>
                  <div className="listMeta listMetaWrap listNotesPreview">{getPreview(n)}</div>
                </button>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="card subtle span2" aria-label="Recent Activity Snapshot">
        <div className="cardHeader">
          <div className="cardTitle">Recent Activity Snapshot (30–60 days)</div>
        </div>
        <div className="pad">
          <div className="mutedSmall" style={{ marginBottom: 10 }}>
            High-signal things worth remembering:
          </div>
          {member.activitySnapshot.bullets.length === 0 ? (
            <div className="muted">No activity snapshot yet.</div>
          ) : (
            <ul className="memberOverviewBullets">
              {member.activitySnapshot.bullets.map((b, i) => (
                <li key={i}>{b}</li>
              ))}
            </ul>
          )}

          <div className="memberOverviewFooterRow">
            {lastUpdated ? <div className="mutedSmall">Last updated: {lastUpdated.toLocaleString()}</div> : <div />}
            <div />
          </div>
        </div>
      </section>
    </div>
  )
}


