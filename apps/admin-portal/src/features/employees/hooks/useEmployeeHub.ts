import { useQuery } from '@tanstack/react-query'
import { employeeHubKeys } from './employeeHubKeys'
import { useEmployee } from '@/hooks/api/useEmployees'
import { usePayrollInfo } from '@/features/payroll/hooks/usePayrollInfo'
import { useCurrentSalaryStructure } from '@/features/payroll/hooks/useSalaryStructures'
import { useTaxDeclarations } from '@/features/payroll/hooks/useTaxDeclarations'
import { assetService } from '@/services/api/assetService'
import { subscriptionService } from '@/services/api/subscriptionService'

/**
 * Combined hook for employee details with payroll info
 */
export const useEmployeeDetails = (employeeId: string, enabled = true) => {
  const employee = useEmployee(employeeId, enabled)
  const payrollInfo = usePayrollInfo(employeeId, enabled)

  return {
    employee: employee.data,
    payrollInfo: payrollInfo.data,
    isLoading: employee.isLoading || payrollInfo.isLoading,
    isError: employee.isError || payrollInfo.isError,
    error: employee.error || payrollInfo.error,
  }
}

/**
 * Hook for employee's current salary structure
 */
export const useEmployeeCurrentSalary = (employeeId: string, enabled = true) => {
  return useCurrentSalaryStructure(employeeId, enabled)
}

/**
 * Hook for employee's tax declarations
 */
export const useEmployeeTaxDeclarations = (employeeId: string, enabled = true) => {
  return useTaxDeclarations({ employeeId }, enabled)
}

/**
 * Hook for assets assigned to an employee
 */
export const useEmployeeAssets = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: employeeHubKeys.assets(employeeId),
    queryFn: () => assetService.getAssignmentsByEmployee(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Hook for subscriptions assigned to an employee
 */
export const useEmployeeSubscriptions = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: employeeHubKeys.subscriptions(employeeId),
    queryFn: () => subscriptionService.getAssignmentsByEmployee(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  })
}

/**
 * Combined hook for all employee hub data
 * Only fetches data for the active tab to optimize performance
 */
export const useEmployeeHubData = (
  employeeId: string,
  activeTab: 'overview' | 'payroll' | 'tax' | 'assets' | 'subscriptions',
  enabled = true
) => {
  const { employee, payrollInfo, isLoading: detailsLoading } = useEmployeeDetails(employeeId, enabled)

  const salary = useEmployeeCurrentSalary(
    employeeId,
    enabled && (activeTab === 'overview' || activeTab === 'payroll')
  )

  const taxDeclarations = useEmployeeTaxDeclarations(
    employeeId,
    enabled && activeTab === 'tax'
  )

  const assets = useEmployeeAssets(
    employeeId,
    enabled && activeTab === 'assets'
  )

  const subscriptions = useEmployeeSubscriptions(
    employeeId,
    enabled && activeTab === 'subscriptions'
  )

  return {
    employee,
    payrollInfo,
    salary: salary.data,
    taxDeclarations: taxDeclarations.data,
    assets: assets.data,
    subscriptions: subscriptions.data,
    isLoading: {
      details: detailsLoading,
      salary: salary.isLoading,
      taxDeclarations: taxDeclarations.isLoading,
      assets: assets.isLoading,
      subscriptions: subscriptions.isLoading,
    },
  }
}
