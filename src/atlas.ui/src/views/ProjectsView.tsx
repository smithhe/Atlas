import { useEffect, useMemo } from 'react'
import { useAi } from '../app/state/AiState'
import {
  useAppDispatch,
  useAppState,
  useSelectedProject,
} from '../app/state/AppState'
import type { Project } from '../app/types'

export function ProjectsView() {
  const ai = useAi()
  const dispatch = useAppDispatch()
  const { projects, selectedProjectId } = useAppState()
  const selected = useSelectedProject()

  useEffect(() => {
    ai.setContext('Context: Projects', [
      { id: 'project-summary', label: 'Summarize project status (draft)' },
      { id: 'identify-risks', label: 'Identify risks (draft)' },
    ])
  }, [ai.setContext])

  return (
    <div className="page">
      <h2 className="pageTitle">Projects</h2>

      <div className="splitGrid">
        <section className="pane paneLeft" aria-label="Project list">
          <div className="list listCard">
            {projects.map((p) => (
              <button
                key={p.id}
                className={`listRow listRowBtn ${p.id === selectedProjectId ? 'listRowActive' : ''}`}
                onClick={() => dispatch({ type: 'selectProject', projectId: p.id })}
              >
                <div className="listMain">
                  <div className="listTitle">{p.name}</div>
                  <div className="listMeta">{p.summary}</div>
                </div>
              </button>
            ))}
          </div>
        </section>

        <section className="pane paneCenter" aria-label="Project detail">
          {!selected ? (
            <div className="card pad">
              <div className="muted">Select a project.</div>
            </div>
          ) : (
            <ProjectDetail project={selected} />
          )}
        </section>
      </div>
    </div>
  )
}

function ProjectDetail({ project }: { project: Project }) {
  const { tasks, risks, team } = useAppState()

  const linkedTasks = useMemo(
    () => tasks.filter((t) => project.linkedTaskIds.includes(t.id)),
    [project.linkedTaskIds, tasks],
  )
  const linkedRisks = useMemo(
    () => risks.filter((r) => project.linkedRiskIds.includes(r.id)),
    [project.linkedRiskIds, risks],
  )
  const members = useMemo(
    () => team.filter((m) => project.teamMemberIds.includes(m.id)),
    [project.teamMemberIds, team],
  )

  return (
    <div className="card pad">
      <div className="detailHeader">
        <div className="detailTitle">{project.name}</div>
        <div className="mutedSmall">Lightweight project grouping (mock)</div>
      </div>

      <label className="field">
        <div className="fieldLabel">Summary</div>
        <div className="placeholderBox">{project.summary}</div>
      </label>

      <div className="subGrid">
        <section className="card subtle">
          <div className="cardHeader">
            <div className="cardTitle">Linked tasks</div>
          </div>
          <div className="list inner">
            {linkedTasks.length === 0 ? (
              <div className="muted pad">No linked tasks.</div>
            ) : (
              linkedTasks.map((t) => (
                <div key={t.id} className="listRow">
                  <span className={`dot dot-${t.priority.toLowerCase()}`} />
                  <div className="listMain">
                    <div className="listTitle">{t.title}</div>
                    <div className="listMeta">{t.priority}</div>
                  </div>
                </div>
              ))
            )}
          </div>
        </section>

        <section className="card subtle">
          <div className="cardHeader">
            <div className="cardTitle">Linked risks</div>
          </div>
          <div className="list inner">
            {linkedRisks.length === 0 ? (
              <div className="muted pad">No linked risks.</div>
            ) : (
              linkedRisks.map((r) => (
                <div key={r.id} className="listRow">
                  <span className={`dot dot-${r.severity.toLowerCase()}`} />
                  <div className="listMain">
                    <div className="listTitle">{r.title}</div>
                    <div className="listMeta">
                      {r.status} • {r.severity}
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </section>

        <section className="card subtle">
          <div className="cardHeader">
            <div className="cardTitle">Team members</div>
          </div>
          <div className="list inner">
            {members.length === 0 ? (
              <div className="muted pad">No members.</div>
            ) : (
              members.map((m) => (
                <div key={m.id} className="listRow">
                  <span className={`dot dot-${m.statusDot.toLowerCase()}`} />
                  <div className="listMain">
                    <div className="listTitle">{m.name}</div>
                    <div className="listMeta">{m.currentFocus}</div>
                  </div>
                </div>
              ))
            )}
          </div>
        </section>
      </div>
    </div>
  )
}


