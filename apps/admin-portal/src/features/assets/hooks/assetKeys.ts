// Query key factory for asset-related queries
export const assetKeys = {
  all: ['assets'] as const,

  // List queries
  lists: () => [...assetKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...assetKeys.lists(), filters] as const,
  paged: (params: Record<string, unknown>) => [...assetKeys.lists(), 'paged', params] as const,
  available: (companyId: string, search?: string) => [...assetKeys.lists(), 'available', companyId, search] as const,

  // Detail queries
  details: () => [...assetKeys.all, 'detail'] as const,
  detail: (id: string) => [...assetKeys.details(), id] as const,

  // Assignment queries
  assignments: () => [...assetKeys.all, 'assignments'] as const,
  assetAssignments: (assetId: string) => [...assetKeys.assignments(), 'asset', assetId] as const,
  employeeAssignments: (employeeId: string) => [...assetKeys.assignments(), 'employee', employeeId] as const,
  allAssignments: () => [...assetKeys.assignments(), 'all'] as const,

  // Maintenance queries
  maintenance: () => [...assetKeys.all, 'maintenance'] as const,
  assetMaintenance: (assetId: string) => [...assetKeys.maintenance(), 'asset', assetId] as const,
  maintenancePaged: (params: Record<string, unknown>) => [...assetKeys.maintenance(), 'paged', params] as const,

  // Document queries
  documents: () => [...assetKeys.all, 'documents'] as const,
  assetDocuments: (assetId: string) => [...assetKeys.documents(), assetId] as const,

  // Cost/Finance queries
  costs: () => [...assetKeys.all, 'costs'] as const,
  costSummary: (assetId: string) => [...assetKeys.costs(), 'summary', assetId] as const,
  costReport: (companyId?: string) => [...assetKeys.costs(), 'report', companyId] as const,

  // Depreciation queries
  depreciation: () => [...assetKeys.all, 'depreciation'] as const,
  assetDepreciation: (assetId: string) => [...assetKeys.depreciation(), assetId] as const,
}
