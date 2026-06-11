import { useMemo } from 'react'
import type { ReactNode } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '../queries/queryClient'
import { useAppData, useAppHydration, useGrowthForMember } from '../queries/hooks'
import {
  SelectionProvider,
  useSelectedProject,
  useSelectedRisk,
  useSelectedTask,
  useSelectedTeamMember,
  useSelectionDispatch,
  useSelectionState,
} from './SelectionState'

export type { SelectionState as AppState } from './SelectionState'

export function AppStateProvider({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <SelectionProvider>{children}</SelectionProvider>
    </QueryClientProvider>
  )
}

export function useAppState() {
  const data = useAppData()
  const selection = useSelectionState()
  return useMemo(() => ({ ...data, ...selection }), [data, selection])
}

export function useAppDispatch() {
  return useSelectionDispatch()
}

export { useAppHydration, useGrowthForMember, useSelectedTask, useSelectedRisk, useSelectedTeamMember, useSelectedProject }
