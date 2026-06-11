import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // Keep data immediately stale so refetch-on-focus acts as a safety net after cache updates.
      staleTime: 0,
      retry: 1,
    },
  },
})
