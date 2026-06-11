import type { ReactNode } from 'react'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '../queries/queryClient'
import { SelectionProvider } from '../state/SelectionState'

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <QueryClientProvider client={queryClient}>
      <SelectionProvider>{children}</SelectionProvider>
    </QueryClientProvider>
  )
}
