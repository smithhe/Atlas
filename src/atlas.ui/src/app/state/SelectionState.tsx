import { createContext, useCallback, useContext, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import type { Project, Risk, Task, TeamMember } from '../types'
import { useProjects, useRisks, useTasks, useTeam } from '../queries/hooks'

export interface SelectionState {
  selectedTaskId?: string
  selectedRiskId?: string
  selectedTeamMemberRiskId?: string
  selectedTeamMemberId?: string
  selectedProjectId?: string
}

type SelectionAction =
  | { type: 'selectTask'; taskId?: string }
  | { type: 'selectRisk'; riskId?: string }
  | { type: 'selectTeamMemberRisk'; teamMemberRiskId?: string }
  | { type: 'selectTeamMember'; memberId?: string }
  | { type: 'selectProject'; projectId?: string }

interface SelectionApi {
  state: SelectionState
  dispatch: (action: SelectionAction) => void
}

const SelectionContext = createContext<SelectionApi | undefined>(undefined)

function reduceSelection(state: SelectionState, action: SelectionAction): SelectionState {
  switch (action.type) {
    case 'selectTask':
      return { ...state, selectedTaskId: action.taskId }
    case 'selectRisk':
      return { ...state, selectedRiskId: action.riskId }
    case 'selectTeamMemberRisk':
      return { ...state, selectedTeamMemberRiskId: action.teamMemberRiskId }
    case 'selectTeamMember':
      return { ...state, selectedTeamMemberId: action.memberId }
    case 'selectProject':
      return { ...state, selectedProjectId: action.projectId }
    default:
      return state
  }
}

export function SelectionProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<SelectionState>({
    selectedTaskId: undefined,
    selectedRiskId: undefined,
    selectedTeamMemberRiskId: undefined,
    selectedTeamMemberId: undefined,
    selectedProjectId: undefined,
  })

  const dispatch = useCallback((action: SelectionAction) => {
    setState((prev) => reduceSelection(prev, action))
  }, [])

  const api = useMemo(() => ({ state, dispatch }), [state, dispatch])

  return <SelectionContext.Provider value={api}>{children}</SelectionContext.Provider>
}

export function useSelectionState(): SelectionState {
  const ctx = useContext(SelectionContext)
  if (!ctx) throw new Error('useSelectionState must be used within SelectionProvider')
  return ctx.state
}

export function useSelectionDispatch() {
  const ctx = useContext(SelectionContext)
  if (!ctx) throw new Error('useSelectionDispatch must be used within SelectionProvider')
  return ctx.dispatch
}

export function useSelectedTask(): Task | undefined {
  const tasks = useTasks()
  const { selectedTaskId } = useSelectionState()
  return useMemo(() => tasks.find((t) => t.id === selectedTaskId), [tasks, selectedTaskId])
}

export function useSelectedRisk(): Risk | undefined {
  const risks = useRisks()
  const { selectedRiskId } = useSelectionState()
  return useMemo(() => risks.find((r) => r.id === selectedRiskId), [risks, selectedRiskId])
}

export function useSelectedTeamMember(): TeamMember | undefined {
  const team = useTeam()
  const { selectedTeamMemberId } = useSelectionState()
  return useMemo(() => team.find((m) => m.id === selectedTeamMemberId), [team, selectedTeamMemberId])
}

export function useSelectedProject(): Project | undefined {
  const projects = useProjects()
  const { selectedProjectId } = useSelectionState()
  return useMemo(() => projects.find((p) => p.id === selectedProjectId), [projects, selectedProjectId])
}
