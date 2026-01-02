import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react'
import { useAi } from '../app/state/AiState'
import { useAppDispatch, useAppState, useSelectedRisk } from '../app/state/AppState'
import type { Risk, RiskStatus, Task, TeamMember } from '../app/types'
import { useNavigate, useParams } from 'react-router-dom'
import { Markdown } from '../components/Markdown'
import { Modal } from '../components/Modal'

function newId(prefix: string) {
  return `${prefix}-${Math.random().toString(16).slice(2)}`
}

function applyTabIndentation(params: {
  value: string
  selectionStart: number
  selectionEnd: number
  outdent: boolean
}): { value: string; selectionStart: number; selectionEnd: number } {
  const { value, selectionStart, selectionEnd, outdent } = params
  const tab = '  '

  const before = value.slice(0, selectionStart)
  const selected = value.slice(selectionStart, selectionEnd)
  const after = value.slice(selectionEnd)

  // Find start of the first selected line.
  const lineStart = before.lastIndexOf('\n') + 1
  const block = value.slice(lineStart, selectionEnd)
  const lines = block.split('\n')

  if (lines.length <= 1) {
    if (!outdent) {
      const nextValue = before + tab + after
      const nextPos = selectionStart + tab.length
      return { value: nextValue, selectionStart: nextPos, selectionEnd: nextPos }
    }

    // Outdent: remove one tab (2 spaces) if present at the cursor line start.
    const linePrefix = value.slice(lineStart, selectionStart)
    const canOutdent = value.slice(lineStart, lineStart + tab.length) === tab
    if (!canOutdent) return { value, selectionStart, selectionEnd }

    const nextValue = value.slice(0, lineStart) + value.slice(lineStart + tab.length)
    const nextStart = Math.max(lineStart, selectionStart - tab.length)
    const nextEnd = Math.max(lineStart, selectionEnd - tab.length)
    void linePrefix
    return { value: nextValue, selectionStart: nextStart, selectionEnd: nextEnd }
  }

  let removedCountFirstLine = 0
  const nextLines = lines.map((ln, idx) => {
    if (!outdent) return tab + ln
    if (ln.startsWith(tab)) {
      if (idx === 0) removedCountFirstLine = tab.length
      return ln.slice(tab.length)
    }
    return ln
  })
  const nextBlock = nextLines.join('\n')
  const nextValue = value.slice(0, lineStart) + nextBlock + after

  if (!outdent) {
    const nextStart = selectionStart + tab.length
    const nextEnd = selectionEnd + tab.length * lines.length
    return { value: nextValue, selectionStart: nextStart, selectionEnd: nextEnd }
  }

  // Outdent selection: selectionStart should shift left by what we removed on first line.
  const removedTotal = nextLines.reduce((sum, ln, idx) => {
    const orig = lines[idx] ?? ''
    return sum + Math.max(0, orig.length - ln.length)
  }, 0)
  const nextStart = Math.max(lineStart, selectionStart - removedCountFirstLine)
  const nextEnd = Math.max(lineStart, selectionEnd - removedTotal)
  return { value: nextValue, selectionStart: nextStart, selectionEnd: nextEnd }
}

function daysSince(iso: string) {
  const a = new Date(iso).getTime()
  const b = Date.now()
  return Math.floor((b - a) / (1000 * 60 * 60 * 24))
}

export function RisksView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { riskId } = useParams<{ riskId?: string }>()
  const { risks, selectedRiskId, projects } = useAppState()
  const selected = useSelectedRisk()
  const listRef = useRef<HTMLElement | null>(null)
  const [listMaxHeightPx, setListMaxHeightPx] = useState<number | undefined>(undefined)

  const projectOptions = useMemo(() => projects.map((p) => p.name).sort((a, b) => a.localeCompare(b)), [projects])

  const [statusFilter, setStatusFilter] = useState<RiskStatus | 'All'>('All')
  const [projectFilter, setProjectFilter] = useState('')
  const [severityFilter, setSeverityFilter] = useState<'All' | 'Low' | 'Medium' | 'High'>('All')

  useEffect(() => {
    ai.setContext('Context: Risks', [
      { id: 'summarize-impact', label: 'Summarize impact' },
      { id: 'suggest-mitigations', label: 'Suggest mitigations' },
      { id: 'why-matters', label: 'Explain “why this matters” (draft)' },
    ])
  }, [ai.setContext])

  const filtered = useMemo(() => {
    return risks.filter((r) => {
      if (statusFilter !== 'All' && r.status !== statusFilter) return false
      if (severityFilter !== 'All' && r.severity !== severityFilter) return false
      if (projectFilter.trim() && !(r.project ?? '').toLowerCase().includes(projectFilter.trim().toLowerCase()))
        return false
      return true
    })
  }, [projectFilter, risks, severityFilter, statusFilter])

  const isFocusMode = !!riskId
  const showDetail = isFocusMode || !!selected

  useEffect(() => {
    if (!riskId) return
    dispatch({ type: 'selectRisk', riskId })
  }, [dispatch, riskId])

  // If we entered focus mode with an unknown ID, fall back to list view.
  useEffect(() => {
    if (!riskId) return
    const exists = risks.some((r) => r.id === riskId)
    if (!exists) navigate('/risks', { replace: true })
  }, [navigate, riskId, risks])

  // If there are >5 risks, cap list height to exactly 5 rows so the 6th+ scrolls (regardless of window size).
  useLayoutEffect(() => {
    const el = listRef.current
    if (!el) return
    if (isFocusMode) return
    if (!showDetail) {
      // When the detail pane is hidden, let the list expand to fill available space.
      setListMaxHeightPx(undefined)
      return
    }

    if (filtered.length <= 5) {
      setListMaxHeightPx(undefined)
      return
    }

    const rows = Array.from(el.querySelectorAll<HTMLElement>('.risksRiskRow')).slice(0, 5)
    if (rows.length === 0) return

    const sumHeights = rows.reduce((sum, r) => sum + r.getBoundingClientRect().height, 0)
    // Include list padding + gaps between rows so the cap is visually correct.
    const style = getComputedStyle(el)
    const paddingTop = Number.parseFloat(style.paddingTop || '0') || 0
    const paddingBottom = Number.parseFloat(style.paddingBottom || '0') || 0
    const rowGap = Number.parseFloat(style.rowGap || style.gap || '0') || 0
    const gaps = rowGap * Math.max(0, rows.length - 1)

    // Small padding so borders don't clip.
    setListMaxHeightPx(Math.ceil(sumHeights + gaps + paddingTop + paddingBottom + 2))
  }, [filtered.length, showDetail])

  return (
    <div className="page pageFill">
      <h2 className="pageTitle">Risks &amp; Mitigation</h2>

      <div
        className={`risksStack ${isFocusMode ? 'risksStackFocus' : ''} ${!showDetail ? 'risksStackNoDetail' : ''}`}
      >
        {!isFocusMode ? (
          <section className="card tight risksFiltersRow" aria-label="Risk filters">
            <label className="field">
              <div className="fieldLabel">Status</div>
              <select
                className="select selectCompact"
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value as RiskStatus | 'All')}
              >
                <option value="All">All</option>
                <option value="Open">Open</option>
                <option value="Watching">Watching</option>
                <option value="Resolved">Resolved</option>
              </select>
            </label>

            <label className="field">
              <div className="fieldLabel">Project</div>
              <input
                className="input"
                value={projectFilter}
                onChange={(e) => setProjectFilter(e.target.value)}
                list="riskProjectFilterOptions"
                placeholder="Type to filter…"
              />
              <datalist id="riskProjectFilterOptions">
                {projectOptions.map((p) => (
                  <option key={p} value={p} />
                ))}
              </datalist>
            </label>

            <label className="field">
              <div className="fieldLabel">Severity</div>
              <select
                className="select selectCompact"
                value={severityFilter}
                onChange={(e) => setSeverityFilter(e.target.value as 'All' | 'Low' | 'Medium' | 'High')}
              >
                <option value="All">All</option>
                <option value="Low">Low</option>
                <option value="Medium">Medium</option>
                <option value="High">High</option>
              </select>
            </label>
          </section>
        ) : null}

        {!isFocusMode ? (
          <section
            className="list listCard risksListRow"
            aria-label="Risk list"
            ref={(n) => {
              listRef.current = n
            }}
            style={listMaxHeightPx ? { maxHeight: `${listMaxHeightPx}px` } : undefined}
          >
            {filtered.map((r) => (
              <button
                key={r.id}
                className={`listRow listRowBtn risksRiskRow ${r.id === selectedRiskId ? 'listRowActive' : ''}`}
                onClick={() => dispatch({ type: 'selectRisk', riskId: r.id })}
                onDoubleClick={() => navigate(`/risks/${r.id}`)}
              >
                <span className={`dot dot-${r.severity.toLowerCase()}`} aria-hidden="true" />
                <div className="listMain">
                  <div className="listTitle listTitleWrap">{r.title}</div>
                  <div className="listMeta">
                    {r.status} • last updated {daysSince(r.lastUpdatedIso)}d
                    {r.project ? ` • ${r.project}` : ''}
                  </div>
                </div>
              </button>
            ))}
            {filtered.length === 0 ? <div className="muted pad">No risks match your filters.</div> : null}
          </section>
        ) : null}

        {showDetail ? (
          <section className="risksDetailRow" aria-label="Risk detail editor">
            {selected ? (
              <RiskDetail
                risk={selected}
                isFocusMode={isFocusMode}
                onEnterFocus={() => navigate(`/risks/${selected.id}`)}
                onExitFocus={() => navigate('/risks')}
                onClose={() => dispatch({ type: 'selectRisk', riskId: undefined })}
                projectOptions={projectOptions}
              />
            ) : (
              <div className="card pad">
                <div className="muted">Risk not found.</div>
              </div>
            )}
          </section>
        ) : null}
      </div>
    </div>
  )
}

function RiskDetail({
  risk,
  isFocusMode,
  onEnterFocus,
  onExitFocus,
  onClose,
  projectOptions,
}: {
  risk: Risk
  isFocusMode: boolean
  onEnterFocus: () => void
  onExitFocus: () => void
  onClose: () => void
  projectOptions: string[]
}) {
  const dispatch = useAppDispatch()
  const { tasks, team } = useAppState()
  const [isEditing, setIsEditing] = useState(false)
  const [isAddingNote, setIsAddingNote] = useState(false)
  const [newNoteText, setNewNoteText] = useState('')
  const [selectedHistoryId, setSelectedHistoryId] = useState<string | undefined>(undefined)
  const [isHistoryEditOpen, setIsHistoryEditOpen] = useState(false)
  const [historyDraftText, setHistoryDraftText] = useState('')
  const [historyEditTab, setHistoryEditTab] = useState<'Write' | 'Preview'>('Write')
  const historyTextareaRef = useRef<HTMLTextAreaElement | null>(null)

  function statusTone(status: RiskStatus) {
    switch (status) {
      case 'Open':
        return 'toneBad'
      case 'Watching':
        return 'toneWarn'
      case 'Resolved':
        return 'toneGood'
    }
  }

  function severityTone(sev: Risk['severity']) {
    switch (sev) {
      case 'High':
        return 'toneBad'
      case 'Medium':
        return 'toneWarn'
      case 'Low':
        return 'toneNeutral'
    }
  }

  function update(patch: Partial<Risk>) {
    dispatch({
      type: 'updateRisk',
      risk: { ...risk, ...patch, lastUpdatedIso: new Date().toISOString() },
    })
  }

  function addNote() {
    const text = newNoteText.trim()
    if (!text) return
    const nowIso = new Date().toISOString()
    const nextHistory = [{ id: newId('rh'), createdIso: nowIso, text }, ...(risk.history ?? [])]
    update({ history: nextHistory })
    setNewNoteText('')
    setIsAddingNote(false)
  }

  function updateHistoryItem(noteId: string, patch: { text: string }) {
    const nextHistory = (risk.history ?? []).map((h) => (h.id === noteId ? { ...h, text: patch.text } : h))
    update({ history: nextHistory })
  }

  useEffect(() => {
    // Switching selection should default back to view mode (prevents "sticky edit" across risks).
    setIsEditing(false)
    setIsAddingNote(false)
    setNewNoteText('')
    setSelectedHistoryId(undefined)
    setIsHistoryEditOpen(false)
    setHistoryDraftText('')
    setHistoryEditTab('Write')
  }, [risk.id])

  const taskById = useMemo(() => new Map<string, Task>(tasks.map((t) => [t.id, t])), [tasks])
  const memberById = useMemo(() => new Map<string, TeamMember>(team.map((m) => [m.id, m])), [team])

  const linkedTasks = useMemo(() => {
    return risk.linkedTaskIds.map((id) => taskById.get(id)).filter(Boolean) as Task[]
  }, [risk.linkedTaskIds, taskById])

  const linkedMembers = useMemo(() => {
    return risk.linkedTeamMemberIds.map((id) => memberById.get(id)).filter(Boolean) as TeamMember[]
  }, [memberById, risk.linkedTeamMemberIds])

  const tasksToShow = linkedTasks.length ? linkedTasks : tasks.slice(0, 2)
  const membersToShow = linkedMembers.length ? linkedMembers : team.slice(0, 2)
  const isTasksExample = linkedTasks.length === 0 && tasksToShow.length > 0
  const isMembersExample = linkedMembers.length === 0 && membersToShow.length > 0

  const historyNewestFirst = useMemo(() => {
    return [...(risk.history ?? [])].sort((a, b) => (a.createdIso < b.createdIso ? 1 : a.createdIso > b.createdIso ? -1 : 0))
  }, [risk.history])

  const selectedHistory = useMemo(() => {
    if (!selectedHistoryId) return undefined
    return (risk.history ?? []).find((h) => h.id === selectedHistoryId)
  }, [risk.history, selectedHistoryId])

  return (
    <div className="card pad riskDetailCard">
      <div className="riskDetailHeader">
        <div className="riskDetailHeaderTop">
          <div className="riskDetailTitle">{risk.title || 'Risk Detail'}</div>
          <div className="riskDetailHeaderRight">
            {!isEditing ? (
              <div className="pillRow">
                <span className={`pill ${statusTone(risk.status)}`}>{risk.status}</span>
                <span className={`pill ${severityTone(risk.severity)}`}>{risk.severity}</span>
              </div>
            ) : null}
            <div className="rowTiny">
              <button className="btn btnGhost" onClick={() => setIsEditing((e) => !e)}>
                {isEditing ? 'Done' : 'Edit'}
              </button>
              {isFocusMode ? (
                <button className="btn btnGhost" onClick={onExitFocus}>
                  Exit focus
                </button>
              ) : (
                <>
                  <button className="btn btnGhost" onClick={onClose}>
                    Close
                  </button>
                  <button className="btn btnGhost" onClick={onEnterFocus}>
                    Focus
                  </button>
                </>
              )}
            </div>
          </div>
        </div>
        <div className="mutedSmall">Last updated: {new Date(risk.lastUpdatedIso).toLocaleString()}</div>
      </div>

      <div className="fieldGrid2">
        {isEditing ? (
          <label className="field span2">
            <div className="fieldLabel">Title</div>
            <input className="input" value={risk.title} onChange={(e) => update({ title: e.target.value })} />
          </label>
        ) : null}

        {isEditing ? (
          <label className="field">
            <div className="fieldLabel">Status</div>
            <select className="select" value={risk.status} onChange={(e) => update({ status: e.target.value as RiskStatus })}>
              <option value="Open">Open</option>
              <option value="Watching">Watching</option>
              <option value="Resolved">Resolved</option>
            </select>
          </label>
        ) : null}

        {isEditing ? (
          <label className="field">
            <div className="fieldLabel">Severity</div>
            <select
              className="select"
              value={risk.severity}
              onChange={(e) => update({ severity: e.target.value as 'Low' | 'Medium' | 'High' })}
            >
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </label>
        ) : null}

        <div className="field">
          <div className="fieldLabel">Project</div>
          {isEditing ? (
            <select
              className="select"
              value={risk.project ?? ''}
              onChange={(e) => update({ project: e.target.value || undefined })}
            >
              <option value="">(None)</option>
              {projectOptions.map((p) => (
                <option key={p} value={p}>
                  {p}
                </option>
              ))}
              {risk.project && !projectOptions.includes(risk.project) ? (
                <option value={risk.project}>{risk.project} (custom)</option>
              ) : null}
            </select>
          ) : (
            <div className="noteDetailReadonly">{risk.project || '—'}</div>
          )}
        </div>

        <div className="field span2">
          <div className="fieldLabel">Description</div>
          {isEditing ? (
            <textarea className="textarea" value={risk.description} onChange={(e) => update({ description: e.target.value })} />
          ) : (
            <div className="riskDetailMarkdown riskDetailMarkdownBlock riskDetailDescriptionBlock">
              {risk.description?.trim() ? <Markdown text={risk.description} /> : <div className="muted">—</div>}
            </div>
          )}
        </div>

        <div className="field span2">
          <div className="fieldLabel">Evidence / Examples</div>
          {isEditing ? (
            <textarea className="textarea" value={risk.evidence} onChange={(e) => update({ evidence: e.target.value })} />
          ) : (
            <div className="riskDetailMarkdown riskDetailMarkdownBlock riskDetailEvidenceBlock">
              {risk.evidence?.trim() ? <Markdown text={risk.evidence} /> : <div className="muted">—</div>}
            </div>
          )}
        </div>

        <div className="field span2">
          <div className="risksLinkedGrid">
            <div className="field">
              <div className="fieldLabel">Linked Tasks</div>
              {isTasksExample ? <div className="mutedSmall">Example (not yet linked)</div> : null}
              <div className="list listCard risksLinkedListCard">
                {tasksToShow.length === 0 ? (
                  <div className="muted pad">No tasks to show.</div>
                ) : (
                  tasksToShow.map((t) => (
                    <div key={t.id} className="listRow">
                      <span className={`dot dot-${t.priority.toLowerCase()}`} aria-hidden="true" />
                      <div className="listMain">
                        <div className="listTitle">{t.title}</div>
                        <div className="listMeta">
                          {t.priority}
                          {t.project ? ` • ${t.project}` : ''}
                          {t.dueDate ? ` • Due ${t.dueDate}` : ''}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>

            <div className="field">
              <div className="fieldLabel">Linked Team Members</div>
              {isMembersExample ? <div className="mutedSmall">Example (not yet linked)</div> : null}
              <div className="list listCard risksLinkedListCard">
                {membersToShow.length === 0 ? (
                  <div className="muted pad">No team members to show.</div>
                ) : (
                  membersToShow.map((m) => (
                    <div key={m.id} className="listRow">
                      <span className={`dot dot-${m.statusDot.toLowerCase()}`} aria-hidden="true" />
                      <div className="listMain">
                        <div className="listTitle">{m.name}</div>
                        <div className="listMeta">
                          {m.role ?? 'Team member'}
                          {m.currentFocus ? ` • ${m.currentFocus}` : ''}
                        </div>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="field span2">
          <div className="fieldLabel">Notes / History</div>
          <div className="row" style={{ marginTop: 8 }}>
            <button className="btn btnSecondary" type="button" onClick={() => setIsAddingNote((v) => !v)}>
              {isAddingNote ? 'Cancel' : 'Add note'}
            </button>
          </div>

          {isAddingNote ? (
            <div style={{ marginTop: 10 }}>
              <label className="field">
                <div className="fieldLabel">New note</div>
                <textarea
                  className="textarea"
                  placeholder="Write a note (Markdown supported)…"
                  value={newNoteText}
                  onChange={(e) => setNewNoteText(e.target.value)}
                />
              </label>
              <div className="row" style={{ marginTop: 10 }}>
                <button className="btn btnSecondary" type="button" onClick={addNote}>
                  Add
                </button>
              </div>
            </div>
          ) : null}

          <div style={{ marginTop: 12 }}>
            {historyNewestFirst.length === 0 ? (
              <div className="muted">No history yet.</div>
            ) : (
              <div className="list listCard">
                {historyNewestFirst.map((h) => (
                  <button
                    key={h.id}
                    className="listRow listRowBtn"
                    onClick={() => {
                      setSelectedHistoryId(h.id)
                      // Open modal directly in edit mode.
                      setIsHistoryEditOpen(true)
                      setHistoryDraftText(h.text)
                      setHistoryEditTab('Write')
                      requestAnimationFrame(() => historyTextareaRef.current?.focus())
                    }}
                  >
                    <div className="listMain">
                      <div className="listMeta">{new Date(h.createdIso).toLocaleString()}</div>
                      <div className="noteBody" style={{ marginTop: 8 }}>
                        <Markdown text={h.text} />
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      <Modal
        title="Note"
        isOpen={!!selectedHistory}
        onClose={() => {
          setSelectedHistoryId(undefined)
          setIsHistoryEditOpen(false)
          setHistoryDraftText('')
          setHistoryEditTab('Write')
        }}
        footer={
          selectedHistory ? (
            <div className="row" style={{ marginTop: 0 }}>
              <button
                className="btn btnSecondary"
                onClick={() => {
                  const text = historyDraftText.trim()
                  if (!text) return
                  updateHistoryItem(selectedHistory.id, { text })
                  setSelectedHistoryId(undefined)
                  setIsHistoryEditOpen(false)
                  setHistoryDraftText('')
                  setHistoryEditTab('Write')
                }}
              >
                Save
              </button>
              <button
                className="btn btnGhost"
                onClick={() => {
                  setSelectedHistoryId(undefined)
                  setIsHistoryEditOpen(false)
                  setHistoryDraftText('')
                  setHistoryEditTab('Write')
                }}
              >
                Cancel
              </button>
            </div>
          ) : null
        }
      >
        {!selectedHistory ? null : (
          <>
            <div className="noteMeta">
              <span className="mutedSmall">{new Date(selectedHistory.createdIso).toLocaleString()}</span>
              <span className="chip chipGhost">Risk history</span>
            </div>

            <div className="noteEdit">
              <div className="noteEditTabs">
                <button
                  className={`btn btnGhost ${historyEditTab === 'Write' ? 'noteEditTabActive' : ''}`}
                  onClick={() => setHistoryEditTab('Write')}
                >
                  Write
                </button>
                <button
                  className={`btn btnGhost ${historyEditTab === 'Preview' ? 'noteEditTabActive' : ''}`}
                  onClick={() => setHistoryEditTab('Preview')}
                >
                  Preview
                </button>
              </div>

              {historyEditTab === 'Write' ? (
                <textarea
                  ref={historyTextareaRef}
                  className="textarea textareaAutoGrow noteEditTextarea"
                  value={historyDraftText}
                  onKeyDown={(e) => {
                    if (e.key !== 'Tab') return
                    e.preventDefault()

                    const el = e.currentTarget
                    const start = el.selectionStart ?? 0
                    const end = el.selectionEnd ?? 0
                    const next = applyTabIndentation({
                      value: historyDraftText,
                      selectionStart: start,
                      selectionEnd: end,
                      outdent: e.shiftKey,
                    })
                    setHistoryDraftText(next.value)

                    requestAnimationFrame(() => {
                      const ta = historyTextareaRef.current
                      if (!ta) return
                      ta.selectionStart = next.selectionStart
                      ta.selectionEnd = next.selectionEnd
                    })
                  }}
                  onChange={(e) => setHistoryDraftText(e.target.value)}
                />
              ) : (
                <div className="noteText noteBody">
                  <Markdown text={historyDraftText} />
                </div>
              )}
            </div>
          </>
        )}
      </Modal>
    </div>
  )
}


