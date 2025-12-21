import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { HierarchyService } from '@/services/api/hr/employees/hierarchyService'
import { UpdateManagerDto } from 'shared-types'
import toast from 'react-hot-toast'

// Query keys
export const hierarchyKeys = {
  all: ['hierarchy'] as const,
  employee: (employeeId: string) => [...hierarchyKeys.all, 'employee', employeeId] as const,
  directReports: (managerId: string) => [...hierarchyKeys.all, 'directReports', managerId] as const,
  subordinates: (managerId: string) => [...hierarchyKeys.all, 'subordinates', managerId] as const,
  reportingChain: (employeeId: string) =>
    [...hierarchyKeys.all, 'reportingChain', employeeId] as const,
  orgTree: (companyId: string, rootEmployeeId?: string) =>
    [...hierarchyKeys.all, 'orgTree', companyId, rootEmployeeId] as const,
  managers: (companyId?: string) => [...hierarchyKeys.all, 'managers', companyId] as const,
  topLevel: (companyId: string) => [...hierarchyKeys.all, 'topLevel', companyId] as const,
  stats: (companyId: string) => [...hierarchyKeys.all, 'stats', companyId] as const,
}

/**
 * Get employee hierarchy details
 */
export function useEmployeeHierarchy(employeeId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.employee(employeeId),
    queryFn: () => HierarchyService.getEmployeeHierarchy(employeeId),
    enabled: enabled && !!employeeId,
  })
}

/**
 * Get direct reports for a manager
 */
export function useDirectReports(managerId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.directReports(managerId),
    queryFn: () => HierarchyService.getDirectReports(managerId),
    enabled: enabled && !!managerId,
  })
}

/**
 * Get all subordinates for a manager
 */
export function useAllSubordinates(managerId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.subordinates(managerId),
    queryFn: () => HierarchyService.getAllSubordinates(managerId),
    enabled: enabled && !!managerId,
  })
}

/**
 * Get reporting chain for an employee
 */
export function useReportingChain(employeeId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.reportingChain(employeeId),
    queryFn: () => HierarchyService.getReportingChain(employeeId),
    enabled: enabled && !!employeeId,
  })
}

/**
 * Get organizational tree
 */
export function useOrgTree(companyId: string, rootEmployeeId?: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.orgTree(companyId, rootEmployeeId),
    queryFn: () => HierarchyService.getOrgTree(companyId, rootEmployeeId),
    enabled: enabled && !!companyId,
  })
}

/**
 * Get all managers
 */
export function useManagers(companyId?: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.managers(companyId),
    queryFn: () => HierarchyService.getManagers(companyId),
    enabled,
  })
}

/**
 * Get top-level employees
 */
export function useTopLevelEmployees(companyId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.topLevel(companyId),
    queryFn: () => HierarchyService.getTopLevelEmployees(companyId),
    enabled: enabled && !!companyId,
  })
}

/**
 * Get hierarchy statistics
 */
export function useHierarchyStats(companyId: string, enabled = true) {
  return useQuery({
    queryKey: hierarchyKeys.stats(companyId),
    queryFn: () => HierarchyService.getHierarchyStats(companyId),
    enabled: enabled && !!companyId,
  })
}

/**
 * Update employee's manager
 */
export function useUpdateManager() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ employeeId, dto }: { employeeId: string; dto: UpdateManagerDto }) =>
      HierarchyService.updateManager(employeeId, dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: hierarchyKeys.all })
      toast.success('Manager updated successfully')
    },
    onError: (error: Error) => {
      toast.error(error.message || 'Failed to update manager')
    },
  })
}

/**
 * Validate manager assignment
 */
export function useValidateManagerAssignment(
  employeeId: string,
  managerId: string,
  enabled = true
) {
  return useQuery({
    queryKey: ['validateManager', employeeId, managerId],
    queryFn: () => HierarchyService.validateManagerAssignment(employeeId, managerId),
    enabled: enabled && !!employeeId && !!managerId,
  })
}
