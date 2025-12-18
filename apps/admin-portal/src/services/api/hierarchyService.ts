import { apiClient } from './client'
import {
  EmployeeHierarchy,
  OrgTreeNode,
  HierarchyStats,
  ManagerSummary,
  DirectReports,
  ReportingChain,
  UpdateManagerDto,
} from 'shared-types'

/**
 * Service for employee hierarchy operations
 * Note: apiClient.get() already returns response.data, so we return directly
 */
export const HierarchyService = {
  /**
   * Get employee hierarchy details
   */
  getEmployeeHierarchy: (employeeId: string): Promise<EmployeeHierarchy> => {
    return apiClient.get<EmployeeHierarchy>(`/hierarchy/employee/${employeeId}`)
  },

  /**
   * Get direct reports for a manager
   */
  getDirectReports: (managerId: string): Promise<DirectReports> => {
    return apiClient.get<DirectReports>(`/hierarchy/direct-reports/${managerId}`)
  },

  /**
   * Get all subordinates (recursive) for a manager
   */
  getAllSubordinates: (managerId: string): Promise<EmployeeHierarchy[]> => {
    return apiClient.get<EmployeeHierarchy[]>(`/hierarchy/subordinates/${managerId}`)
  },

  /**
   * Get reporting chain for an employee
   */
  getReportingChain: (employeeId: string): Promise<ReportingChain> => {
    return apiClient.get<ReportingChain>(`/hierarchy/reporting-chain/${employeeId}`)
  },

  /**
   * Get organizational tree for visualization
   */
  getOrgTree: (companyId: string, rootEmployeeId?: string): Promise<OrgTreeNode[]> => {
    const params: Record<string, string> = { companyId }
    if (rootEmployeeId) params.rootEmployeeId = rootEmployeeId
    return apiClient.get<OrgTreeNode[]>('/hierarchy/org-tree', params)
  },

  /**
   * Get all managers
   */
  getManagers: (companyId?: string): Promise<ManagerSummary[]> => {
    const params = companyId ? { companyId } : undefined
    return apiClient.get<ManagerSummary[]>('/hierarchy/managers', params)
  },

  /**
   * Get top-level employees (no manager)
   */
  getTopLevelEmployees: (companyId: string): Promise<EmployeeHierarchy[]> => {
    return apiClient.get<EmployeeHierarchy[]>('/hierarchy/top-level', { companyId })
  },

  /**
   * Get hierarchy statistics
   */
  getHierarchyStats: (companyId: string): Promise<HierarchyStats> => {
    return apiClient.get<HierarchyStats>('/hierarchy/stats', { companyId })
  },

  /**
   * Update employee's manager
   */
  updateManager: (employeeId: string, dto: UpdateManagerDto): Promise<EmployeeHierarchy> => {
    return apiClient.put<EmployeeHierarchy, UpdateManagerDto>(`/hierarchy/employee/${employeeId}/manager`, dto)
  },

  /**
   * Validate manager assignment
   */
  validateManagerAssignment: (employeeId: string, managerId: string): Promise<boolean> => {
    return apiClient.get<boolean>('/hierarchy/validate-manager', { employeeId, managerId })
  },

  /**
   * Check if user can approve for an employee
   */
  canApproveForEmployee: (approverId: string, employeeId: string): Promise<boolean> => {
    return apiClient.get<boolean>('/hierarchy/can-approve', { approverId, employeeId })
  },
}
