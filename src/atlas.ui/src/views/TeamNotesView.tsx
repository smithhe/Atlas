import { useEffect, useLayoutEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedTeamMember } from '../app/state/AppState'
import type { NoteTag, TeamMember } from '../app/types'
import { Markdown } from '../components/Markdown'

const FILTER_TAGS: Array<NoteTag | 'All'> = ['All', 'Quick', 'Standup', 'Progress', 'Praise', 'Concern', 'Blocker']

export function TeamNotesView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { memberId } = useParams<{ memberId: string }>()
  const { team } = useAppState()
  const member = useSelectedTeamMember()

  // Ensure we land at the top when entering the history view.
  useLayoutEffect(() => {
    window.scrollTo(0, 0)
    document.querySelector<HTMLElement>('.mainContent')?.scrollTo({ top: 0 })
  }, [])

  useEffect(() => {
    ai.setContext('Context: Team Notes', [
      { id: 'summarize-patterns', label: 'Summarize patterns (frequent blockers)' },
      { id: 'growth-areas', label: 'Highlight growth areas' },
      { id: 'cite-notes', label: 'Cite specific notes (draft)' },
    ])
  }, [ai.setContext])

  useEffect(() => {
    if (!memberId) return
    dispatch({ type: 'selectTeamMember', memberId })
  }, [dispatch, memberId])

  useEffect(() => {
    if (!memberId) return
    const exists = team.some((m) => m.id === memberId)
    if (!exists) navigate('/team', { replace: true })
  }, [memberId, navigate, team])

  return (
    <div className="page">
      <div className="detailHeader">
        <div>
          <div className="detailTitle">Notes</div>
          <div className="mutedSmall">{member?.name ?? ''}</div>
        </div>
        <button className="btn btnGhost" onClick={() => navigate(`/team/${memberId}`)}>
          Back to member
        </button>
      </div>

      {!member ? (
        <div className="card pad">
          <div className="muted">Select a team member.</div>
        </div>
      ) : (
        <NotesPanel member={member} />
      )}
    </div>
  )
}

function NotesPanel({ member }: { member: TeamMember }) {
  const [query, setQuery] = useState('')
  const [tagFilter, setTagFilter] = useState<NoteTag | 'All'>('All')
  const [sortBy, setSortBy] = useState<'Newest' | 'Oldest'>('Newest')

  const filteredSorted = useMemo(() => {
    const q = query.trim().toLowerCase()

    function matchesText(n: TeamMember['notes'][number]) {
      if (!q) return true
      const hay = [
        n.tag,
        n.text,
        n.adoWorkItemId ?? '',
        n.prUrl ?? '',
        new Date(n.createdIso).toLocaleDateString(),
      ]
        .join(' ')
        .toLowerCase()
      return hay.includes(q)
    }

    function matchesTag(n: TeamMember['notes'][number]) {
      if (tagFilter === 'All') return true
      return n.tag === tagFilter
    }

    const sorted = member.notes
      .filter((n) => matchesTag(n) && matchesText(n))
      .slice()
      .sort((a, b) => {
        const at = new Date(a.createdIso).getTime()
        const bt = new Date(b.createdIso).getTime()
        return sortBy === 'Newest' ? bt - at : at - bt
      })

    return sorted
  }, [member.notes, query, sortBy, tagFilter])

  return (
    <>
      <section className="card subtle" aria-label="All notes history">
        <div className="cardHeader">
          <div className="cardTitle">Notes History</div>
        </div>
        <div className="pad">
          <div className="tasksFiltersRow" aria-label="Notes filters">
            <label className="field">
              <div className="fieldLabel">Search</div>
              <input
                className="input"
                placeholder="Search tag, text, ADO id, date…"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
              />
            </label>

            <label className="field">
              <div className="fieldLabel">Tag</div>
              <select
                className="select"
                value={tagFilter}
                onChange={(e) => setTagFilter(e.target.value as NoteTag | 'All')}
              >
                {FILTER_TAGS.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Sort</div>
              <select
                className="select"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as 'Newest' | 'Oldest')}
              >
                <option value="Newest">Newest first</option>
                <option value="Oldest">Oldest first</option>
              </select>
            </label>
          </div>

          <div className="notesList">
            {filteredSorted.length === 0 ? (
              <div className="muted">No notes yet.</div>
            ) : (
              filteredSorted.map((n) => (
                <div key={n.id} className="noteRow">
                  <div className="noteMeta">
                    <span className={`chip chipTag chipTag-${n.tag.toLowerCase()}`}>{n.tag}</span>
                    <span className="mutedSmall">{new Date(n.createdIso).toLocaleDateString()}</span>
                    {n.adoWorkItemId ? <span className="chip chipGhost">ADO: {n.adoWorkItemId}</span> : null}
                    {n.prUrl ? <span className="chip chipGhost">PR</span> : null}
                  </div>
                  <div className="noteText">
                    <Markdown text={n.text} />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </section>
    </>
  )
}


