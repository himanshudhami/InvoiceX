import { QueryClient } from '@tanstack/react-query'

// Centralized QueryClient used by both React Query hooks and TanStack DB collections.
// This keeps the caches aligned and avoids multiple clients fetching the same data.
export const appQueryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      gcTime: 10 * 60 * 1000, // 10 minutes (formerly cacheTime)
      retry: (failureCount, error: any) => {
        if (error?.type === 'Validation' || error?.type === 'NotFound') {
          return false
        }
        return failureCount < 2
      },
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: (failureCount, error: any) => {
        if (error?.type === 'Validation' || error?.type === 'NotFound' || error?.type === 'Conflict') {
          return false
        }
        return failureCount < 1
      },
    },
  },
})
