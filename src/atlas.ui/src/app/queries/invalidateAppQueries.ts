import { useQueryClient } from '@tanstack/react-query'
import { useCallback } from 'react'
import type { QueryClient } from '@tanstack/react-query'
import { queryKeys } from './queryKeys'

export type AppQueryScope = 'settings' | 'tasks' | 'risks' | 'projects' | 'teamMembers' | 'productOwners'

const scopeToKey: Record<AppQueryScope, readonly string[]> = {
  settings: queryKeys.settings,
  tasks: queryKeys.tasks,
  risks: queryKeys.risks,
  projects: queryKeys.projects,
  teamMembers: queryKeys.teamMembers,
  productOwners: queryKeys.productOwners,
}

export async function invalidateAppQueries(queryClient: QueryClient, scopes: AppQueryScope[]) {
  const expanded = new Set<AppQueryScope>(scopes)
  if (expanded.has('projects') || expanded.has('risks')) {
    expanded.add('tasks')
  }

  await Promise.all([...expanded].map((scope) => queryClient.invalidateQueries({ queryKey: scopeToKey[scope] })))
}

export function useInvalidateAppQueries() {
  const queryClient = useQueryClient()
  return useCallback((scopes: AppQueryScope[]) => invalidateAppQueries(queryClient, scopes), [queryClient])
}
