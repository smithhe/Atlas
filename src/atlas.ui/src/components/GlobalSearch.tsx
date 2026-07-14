import { useEffect, useId, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppData, useAppHydration } from '../app/queries/hooks'
import { useSelectionActions } from '../app/state/SelectionState'

type SearchKind = 'Task' | 'Risk' | 'Person' | 'Project'

type SearchResult = {
  id: string
  kind: SearchKind
  title: string
  meta: string
  to: string
}

const KIND_ORDER: SearchKind[] = ['Task', 'Risk', 'Person', 'Project']
const MAX_RESULTS = 12

function matchesQuery(haystack: string, query: string) {
  return haystack.toLowerCase().includes(query)
}

function buildResults(
  query: string,
  data: ReturnType<typeof useAppData>,
): SearchResult[] {
  const q = query.trim().toLowerCase()
  if (!q) return []

  const results: SearchResult[] = []

  for (const task of data.tasks) {
    const haystack = [task.title, task.project, task.risk, task.status, task.priority, task.notes]
      .filter(Boolean)
      .join(' ')
    if (!matchesQuery(haystack, q)) continue
    results.push({
      id: `task:${task.id}`,
      kind: 'Task',
      title: task.title,
      meta: [task.status, task.priority, task.project].filter(Boolean).join(' · '),
      to: `/tasks/${task.id}`,
    })
  }

  for (const risk of data.risks) {
    const haystack = [risk.title, risk.project, risk.description, risk.evidence, risk.status, risk.severity]
      .filter(Boolean)
      .join(' ')
    if (!matchesQuery(haystack, q)) continue
    results.push({
      id: `risk:${risk.id}`,
      kind: 'Risk',
      title: risk.title,
      meta: [risk.status, risk.severity, risk.project].filter(Boolean).join(' · '),
      to: `/risks/${risk.id}`,
    })
  }

  for (const member of data.team) {
    const haystack = [member.name, member.role, member.currentFocus].filter(Boolean).join(' ')
    if (!matchesQuery(haystack, q)) continue
    results.push({
      id: `person:${member.id}`,
      kind: 'Person',
      title: member.name,
      meta: [member.role, member.currentFocus].filter(Boolean).join(' · '),
      to: `/team/${member.id}`,
    })
  }

  for (const project of data.projects) {
    const haystack = [project.name, project.summary, project.description, project.status, ...(project.tags ?? [])]
      .filter(Boolean)
      .join(' ')
    if (!matchesQuery(haystack, q)) continue
    results.push({
      id: `project:${project.id}`,
      kind: 'Project',
      title: project.name,
      meta: [project.status, project.summary].filter(Boolean).join(' · '),
      to: `/projects/${project.id}`,
    })
  }

  results.sort((a, b) => {
    const kindDiff = KIND_ORDER.indexOf(a.kind) - KIND_ORDER.indexOf(b.kind)
    if (kindDiff !== 0) return kindDiff
    return a.title.localeCompare(b.title)
  })

  return results.slice(0, MAX_RESULTS)
}

export function GlobalSearch() {
  const data = useAppData()
  const isHydrating = useAppHydration()
  const navigate = useNavigate()
  const { selectTask, selectRisk, selectTeamMember, selectProject } = useSelectionActions()
  const listId = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const [query, setQuery] = useState('')
  const [open, setOpen] = useState(false)
  const [activeIndex, setActiveIndex] = useState(0)

  const results = buildResults(query, data)
  const showResults = open && query.trim().length > 0

  useEffect(() => {
    setActiveIndex(0)
  }, [query])

  useEffect(() => {
    function onPointerDown(e: PointerEvent) {
      if (!rootRef.current?.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    window.addEventListener('pointerdown', onPointerDown)
    return () => window.removeEventListener('pointerdown', onPointerDown)
  }, [])

  function close() {
    setOpen(false)
    setActiveIndex(0)
  }

  function selectResult(result: SearchResult) {
    const entityId = result.id.slice(result.id.indexOf(':') + 1)
    if (result.kind === 'Task') selectTask(entityId)
    if (result.kind === 'Risk') selectRisk(entityId)
    if (result.kind === 'Person') selectTeamMember(entityId)
    if (result.kind === 'Project') selectProject(entityId)
    navigate(result.to)
    setQuery('')
    close()
    inputRef.current?.blur()
  }

  function onKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Escape') {
      if (showResults) {
        e.preventDefault()
        close()
      } else if (query) {
        setQuery('')
      }
      return
    }

    if (!showResults || results.length === 0) return

    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setActiveIndex((i) => (i + 1) % results.length)
      return
    }
    if (e.key === 'ArrowUp') {
      e.preventDefault()
      setActiveIndex((i) => (i - 1 + results.length) % results.length)
      return
    }
    if (e.key === 'Enter') {
      e.preventDefault()
      const result = results[activeIndex]
      if (result) selectResult(result)
    }
  }

  return (
    <div className="globalSearch" ref={rootRef}>
      <input
        ref={inputRef}
        className="searchInput"
        value={query}
        onChange={(e) => {
          setQuery(e.target.value)
          setOpen(true)
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={onKeyDown}
        placeholder="Search tasks, risks, people, projects…"
        aria-label="Search"
        aria-autocomplete="list"
        aria-controls={showResults ? listId : undefined}
        aria-expanded={showResults}
        aria-activedescendant={showResults && results[activeIndex] ? `${listId}-${results[activeIndex].id}` : undefined}
        disabled={isHydrating}
        role="combobox"
      />
      {showResults ? (
        <div className="searchResults list listCard" id={listId} role="listbox" aria-label="Search results">
          {results.length === 0 ? (
            <div className="searchResultsEmpty muted">No matches</div>
          ) : (
            results.map((result, index) => (
              <button
                key={result.id}
                id={`${listId}-${result.id}`}
                type="button"
                role="option"
                aria-selected={index === activeIndex}
                className={`listRow listRowBtn searchResultRow ${index === activeIndex ? 'listRowActive' : ''}`}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => selectResult(result)}
              >
                <div className="listMain">
                  <div className="listTitle">{result.title}</div>
                  <div className="listMeta">{result.meta || result.kind}</div>
                </div>
                <span className="pill searchResultKind">{result.kind}</span>
              </button>
            ))
          )}
        </div>
      ) : null}
    </div>
  )
}
