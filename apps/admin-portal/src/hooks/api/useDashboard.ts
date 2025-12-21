import { useQuery } from '@tanstack/react-query'
import { dashboardService } from '@/services/api/core/dashboardService'

// Query keys for proper cache management
export const dashboardKeys = {
  all: ['dashboard'] as const,
  data: () => [...dashboardKeys.all, 'data'] as const,
}

// Get dashboard data with statistics and recent invoices
export const useDashboard = () => {
  return useQuery({
    queryKey: dashboardKeys.data(),
    queryFn: () => dashboardService.getDashboardData(),
    staleTime: 2 * 60 * 1000, // 2 minutes (refresh more frequently for dashboard)
    refetchOnWindowFocus: true, // Refetch when user returns to the app
  })
}