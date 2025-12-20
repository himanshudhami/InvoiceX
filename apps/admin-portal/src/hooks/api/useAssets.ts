import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { assetService } from '@/services/api/assetService';
import {
  Asset,
  AssetAssignment,
  CreateAssetDto,
  UpdateAssetDto,
  CreateAssetAssignmentDto,
  ReturnAssetAssignmentDto,
  PaginationParams,
  AssetMaintenance,
  CreateAssetMaintenanceDto,
  UpdateAssetMaintenanceDto,
  AssetDocument,
  CreateAssetDocumentDto,
  AssetDisposal,
  CreateAssetDisposalDto,
  AssetCostSummary,
  AssetCostReport,
  BulkAssetsDto,
  BulkUploadResult,
} from '@/services/api/types';

const assetsKey = (params?: PaginationParams) => ['assets', params];
const assetAssignmentsKey = (assetId: string) => ['asset-assignments', assetId];
const assetDocumentsKey = (assetId: string) => ['asset-documents', assetId];
const assetMaintenanceKey = (assetId: string) => ['asset-maintenance', assetId];

export const useAsset = (id: string, enabled = true) => {
  return useQuery({
    queryKey: ['asset', id],
    queryFn: () => assetService.getById(id),
    enabled: enabled && !!id,
    staleTime: 1000 * 30,
  });
};

export const useAssets = (params: PaginationParams = { pageNumber: 1, pageSize: 25 }) => {
  return useQuery({
    queryKey: assetsKey(params),
    queryFn: () => assetService.getPaged(params),
    staleTime: 1000 * 30,
  });
};

export const useCreateAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAssetDto) => assetService.create(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['assets'] }),
  });
};

export const useUpdateAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAssetDto }) => assetService.update(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['assets'] }),
  });
};

export const useDeleteAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => assetService.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['assets'] }),
  });
};

export const useAllAssetAssignments = () => {
  return useQuery({
    queryKey: ['asset-assignments', 'all'],
    queryFn: () => assetService.getAllAssignments(),
    staleTime: 1000 * 30,
  });
};

export const useAssetAssignments = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetAssignmentsKey(assetId),
    queryFn: () => assetService.getAssignments(assetId),
    enabled: enabled && !!assetId,
    staleTime: 1000 * 30,
  });
};

export const useAssignAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assetId, data }: { assetId: string; data: CreateAssetAssignmentDto }) =>
      assetService.assign(assetId, data),
    onSuccess: (_data, variables) => {
      // Invalidate all asset queries and assignment queries to ensure UI updates
      qc.invalidateQueries({ queryKey: ['assets'] });
      qc.invalidateQueries({ queryKey: assetAssignmentsKey(variables.assetId) });
      qc.invalidateQueries({ queryKey: ['asset-assignments'] });
    },
  });
};

export const useReturnAssetAssignment = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assignmentId, data }: { assignmentId: string; data: ReturnAssetAssignmentDto }) =>
      assetService.returnAssignment(assignmentId, data),
    onSuccess: () => {
      // Invalidate all asset queries and assignment queries to ensure UI updates
      qc.invalidateQueries({ queryKey: ['assets'] });
      qc.invalidateQueries({ queryKey: ['asset-assignments'] });
    },
  });
};

export const useAssetDocuments = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetDocumentsKey(assetId),
    queryFn: () => assetService.getDocuments(assetId),
    enabled: enabled && !!assetId,
    staleTime: 1000 * 30,
  });
};

export const useAddAssetDocument = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assetId, data }: { assetId: string; data: CreateAssetDocumentDto }) =>
      assetService.addDocument(assetId, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: assetDocumentsKey(variables.assetId) });
    },
  });
};

export const useDeleteAssetDocument = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (documentId: string) => assetService.deleteDocument(documentId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['asset-documents'] }),
  });
};

export const useMaintenance = (params: PaginationParams & { assetId?: string; companyId?: string; status?: string } = {}) => {
  return useQuery({
    queryKey: ['asset-maintenance', params],
    queryFn: () => assetService.getMaintenance(params),
    staleTime: 1000 * 30,
  });
};

export const useAssetMaintenance = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: assetMaintenanceKey(assetId),
    queryFn: () => assetService.getMaintenanceForAsset(assetId),
    enabled: enabled && !!assetId,
    staleTime: 1000 * 30,
  });
};

export const useCreateMaintenance = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assetId, data }: { assetId: string; data: CreateAssetMaintenanceDto }) =>
      assetService.createMaintenance(assetId, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['asset-maintenance'] });
      qc.invalidateQueries({ queryKey: assetMaintenanceKey(variables.assetId) });
    },
  });
};

export const useUpdateMaintenance = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ maintenanceId, data }: { maintenanceId: string; data: UpdateAssetMaintenanceDto }) =>
      assetService.updateMaintenance(maintenanceId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['asset-maintenance'] });
    },
  });
};

export const useDisposeAsset = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assetId, data }: { assetId: string; data: CreateAssetDisposalDto }) =>
      assetService.dispose(assetId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assets'] });
    },
  });
};

export const useAssetCostSummary = (assetId: string, enabled = true) => {
  return useQuery({
    queryKey: ['asset-cost-summary', assetId],
    queryFn: () => assetService.getCostSummary(assetId),
    enabled: enabled && !!assetId,
    staleTime: 1000 * 30,
  });
};

export const useAssetCostReport = (companyId?: string) => {
  return useQuery({
    queryKey: ['asset-cost-report', companyId || 'all'],
    queryFn: () => assetService.getCostReport(companyId),
    staleTime: 1000 * 30,
  });
};

export const useBulkCreateAssets = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: BulkAssetsDto) => assetService.bulkCreate(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['assets'] }),
  });
};

export const useLinkAssetToLoan = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ assetId, loanId }: { assetId: string; loanId: string }) =>
      assetService.linkAssetToLoan(assetId, loanId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['assets'] });
    },
  });
};

export const useAssetsByLoan = (loanId: string | undefined, enabled: boolean = true) => {
  return useQuery({
    queryKey: ['assets', 'by-loan', loanId],
    queryFn: () => assetService.getAssetsByLoan(loanId!),
    enabled: enabled && !!loanId,
    staleTime: 1000 * 30,
  });
};





