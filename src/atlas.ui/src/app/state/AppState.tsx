import { createContext, useContext, useMemo, useReducer } from 'react'
import type { Dispatch, ReactNode } from 'react'
import type { Growth, Project, Risk, Settings, Task, TeamMember, TeamMemberRisk } from '../types'
import { seedGrowth, seedProjects, seedRisks, seedSettings, seedTasks, seedTeam, seedTeamMemberRisks } from '../seed'

export interface AppState {
  tasks: Task[]
  risks: Risk[]
  teamMemberRisks: TeamMemberRisk[]
  team: TeamMember[]
  projects: Project[]
  growth: Growth[]
  settings: Settings

  selectedTaskId?: string
  selectedRiskId?: string
  selectedTeamMemberRiskId?: string
  selectedTeamMemberId?: string
  selectedProjectId?: string
}

type Action =
  | { type: 'selectTask'; taskId?: string }
  | { type: 'selectRisk'; riskId?: string }
  | { type: 'selectTeamMemberRisk'; teamMemberRiskId?: string }
  | { type: 'selectTeamMember'; memberId?: string }
  | { type: 'selectProject'; projectId?: string }
  | { type: 'updateTask'; task: Task }
  | { type: 'touchTask'; taskId: string; touchedIso: string }
  | { type: 'updateRisk'; risk: Risk }
  | { type: 'updateTeamMemberRisk'; teamMemberRisk: TeamMemberRisk }
  | { type: 'updateGrowth'; growth: Growth }
  | { type: 'updateSettings'; settings: Settings }
  | { type: 'updateTeamMember'; member: TeamMember }

function reduce(state: AppState, action: Action): AppState {
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
    case 'updateTask':
      return { ...state, tasks: state.tasks.map((t) => (t.id === action.task.id ? action.task : t)) }
    case 'touchTask':
      return {
        ...state,
        tasks: state.tasks.map((t) =>
          t.id === action.taskId ? { ...t, lastTouchedIso: action.touchedIso } : t,
        ),
      }
    case 'updateRisk':
      return { ...state, risks: state.risks.map((r) => (r.id === action.risk.id ? action.risk : r)) }
    case 'updateTeamMemberRisk':
      return {
        ...state,
        teamMemberRisks: state.teamMemberRisks.map((r) => (r.id === action.teamMemberRisk.id ? action.teamMemberRisk : r)),
      }
    case 'updateGrowth':
      return {
        ...state,
        growth: state.growth.some((g) => g.id === action.growth.id)
          ? state.growth.map((g) => (g.id === action.growth.id ? action.growth : g))
          : [...state.growth, action.growth],
      }
    case 'updateSettings':
      return { ...state, settings: action.settings }
    case 'updateTeamMember':
      return { ...state, team: state.team.map((m) => (m.id === action.member.id ? action.member : m)) }
    default: {
      const _exhaustive: never = action
      void _exhaustive
      return state
    }
  }
}

function initialState(): AppState {
  const tasks = seedTasks()
  const risks = seedRisks()
  const teamMemberRisks = seedTeamMemberRisks()
  const team = seedTeam()
  const projects = seedProjects()
  const growth = seedGrowth()
  const settings = seedSettings()

  return {
    tasks,
    risks,
    teamMemberRisks,
    team,
    projects,
    growth,
    settings,
    // Start with no task selected so the Tasks page can start "closed".
    selectedTaskId: undefined,
    // Start with no risk selected so the Risks page can start "closed".
    selectedRiskId: undefined,
    selectedTeamMemberRiskId: teamMemberRisks[0]?.id,
    selectedTeamMemberId: team[0]?.id,
    selectedProjectId: projects[0]?.id,
  }
}

const AppStateContext = createContext<AppState | undefined>(undefined)
const AppDispatchContext = createContext<Dispatch<Action> | undefined>(undefined)

export function AppStateProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(reduce, undefined, initialState)

  return (
    <AppStateContext.Provider value={state}>
      <AppDispatchContext.Provider value={dispatch}>{children}</AppDispatchContext.Provider>
    </AppStateContext.Provider>
  )
}

export function useAppState(): AppState {
  const ctx = useContext(AppStateContext)
  if (!ctx) throw new Error('useAppState must be used within AppStateProvider')
  return ctx
}

export function useAppDispatch() {
  const ctx = useContext(AppDispatchContext)
  if (!ctx) throw new Error('useAppDispatch must be used within AppStateProvider')
  return ctx
}

export function useSelectedTask(): Task | undefined {
  const { tasks, selectedTaskId } = useAppState()
  return useMemo(() => tasks.find((t) => t.id === selectedTaskId), [tasks, selectedTaskId])
}

export function useSelectedRisk(): Risk | undefined {
  const { risks, selectedRiskId } = useAppState()
  return useMemo(() => risks.find((r) => r.id === selectedRiskId), [risks, selectedRiskId])
}

export function useSelectedTeamMemberRisk(): TeamMemberRisk | undefined {
  const { teamMemberRisks, selectedTeamMemberRiskId } = useAppState()
  return useMemo(
    () => teamMemberRisks.find((r) => r.id === selectedTeamMemberRiskId),
    [teamMemberRisks, selectedTeamMemberRiskId],
  )
}

export function useSelectedTeamMember(): TeamMember | undefined {
  const { team, selectedTeamMemberId } = useAppState()
  return useMemo(() => team.find((m) => m.id === selectedTeamMemberId), [team, selectedTeamMemberId])
}

export function useGrowthForMember(memberId?: string): Growth | undefined {
  const { growth } = useAppState()
  return useMemo(() => (memberId ? growth.find((g) => g.memberId === memberId) : undefined), [growth, memberId])
}

export function useSelectedProject(): Project | undefined {
  const { projects, selectedProjectId } = useAppState()
  return useMemo(() => projects.find((p) => p.id === selectedProjectId), [projects, selectedProjectId])
}


