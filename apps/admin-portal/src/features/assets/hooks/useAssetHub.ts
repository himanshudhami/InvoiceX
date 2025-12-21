import { useQuery } from '@tanstack/react-query'
import { assetService } from '@/services/api/assets/assetService'
import { assetKeys } from './assetKeys'

// Hook to get a single asset by ID
export const useAsset = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.detail(assetId),
    queryFn: () => assetService.getById(assetId),
    enabled: enabled && !!assetId,
    staleTime: 5 * 60 * 1000,
  })
}

// Hook to get available assets for assignment
export const useAvailableAssets = (companyId: string, searchTerm?: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.available(companyId, searchTerm),
    queryFn: () => assetService.getAvailableAssets(companyId, searchTerm),
    enabled: enabled && !!companyId,
    staleTime: 30 * 1000, // 30 seconds - frequently changing
  })
}

// Hook to get assignments for a specific asset
export const useAssetAssignmentHistory = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.assetAssignments(assetId),
    queryFn: () => assetService.getAssignments(assetId),
    enabled: enabled && !!assetId,
    staleTime: 5 * 60 * 1000,
  })
}

// Hook to get maintenance records for a specific asset
export const useAssetMaintenanceHistory = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.assetMaintenance(assetId),
    queryFn: () => assetService.getMaintenanceForAsset(assetId),
    enabled: enabled && !!assetId,
    staleTime: 5 * 60 * 1000,
  })
}

// Hook to get documents for a specific asset
export const useAssetDocumentList = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.assetDocuments(assetId),
    queryFn: () => assetService.getDocuments(assetId),
    enabled: enabled && !!assetId,
    staleTime: 5 * 60 * 1000,
  })
}

// Hook to get cost summary for a specific asset
export const useAssetCost = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetKeys.costSummary(assetId),
    queryFn: () => assetService.getCostSummary(assetId),
    enabled: enabled && !!assetId,
    staleTime: 5 * 60 * 1000,
  })
}

// Combined hook for asset details (basic info + current assignment)
export const useAssetDetails = (assetId: string, enabled = true) => {
  const asset = useAsset(assetId, enabled)
  const assignments = useAssetAssignmentHistory(assetId, enabled)

  // Find current active assignment
  const currentAssignment = assignments.data?.find((a) => !a.returnedOn) || null

  return {
    asset: asset.data,
    currentAssignment,
    assignmentHistory: assignments.data || [],
    isLoading: asset.isLoading || assignments.isLoading,
    isError: asset.isError || assignments.isError,
    error: asset.error || assignments.error,
  }
}

// Tab-specific data loading for lazy loading in side panel
type AssetTabType = 'overview' | 'assignments' | 'maintenance' | 'documents' | 'finance'

export const useAssetHubData = (
  assetId: string,
  activeTab: AssetTabType,
  enabled = true
) => {
  // Always load asset details
  const asset = useAsset(assetId, enabled)

  // Conditionally load based on active tab
  const assignments = useAssetAssignmentHistory(
    assetId,
    enabled && (activeTab === 'overview' || activeTab === 'assignments')
  )
  const maintenance = useAssetMaintenanceHistory(
    assetId,
    enabled && activeTab === 'maintenance'
  )
  const documents = useAssetDocumentList(
    assetId,
    enabled && activeTab === 'documents'
  )
  const costs = useAssetCost(
    assetId,
    enabled && activeTab === 'finance'
  )

  // Find current active assignment
  const currentAssignment = assignments.data?.find((a) => !a.returnedOn) || null

  return {
    // Core data
    asset: asset.data,
    currentAssignment,

    // Tab-specific data
    assignmentHistory: assignments.data || [],
    maintenanceRecords: maintenance.data || [],
    documents: documents.data || [],
    costSummary: costs.data,

    // Loading states
    isLoading: asset.isLoading,
    isLoadingAssignments: assignments.isLoading,
    isLoadingMaintenance: maintenance.isLoading,
    isLoadingDocuments: documents.isLoading,
    isLoadingCosts: costs.isLoading,

    // Error states
    isError: asset.isError,
    error: asset.error,
  }
}
