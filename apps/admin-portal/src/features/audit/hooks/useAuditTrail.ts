import { useQuery, useMutation } from '@tanstack/react-query';
import {
  auditService,
  type AuditTrailQueryParams,
  type AuditTrailExportParams,
  type AuditTrailEntry,
  type PagedAuditResponse,
  type AuditTrailSummary,
} from '@/services/api/admin/auditService';

const AUDIT_KEYS = {
  all: ['audit'] as const,
  list: (params: AuditTrailQueryParams) => [...AUDIT_KEYS.all, 'list', params] as const,
  detail: (id: string) => [...AUDIT_KEYS.all, 'detail', id] as const,
  entityHistory: (entityType: string, entityId: string) =>
    [...AUDIT_KEYS.all, 'entity', entityType, entityId] as const,
  recent: (companyId: string) => [...AUDIT_KEYS.all, 'recent', companyId] as const,
  stats: (companyId: string, fromDate?: string, toDate?: string) =>
    [...AUDIT_KEYS.all, 'stats', companyId, fromDate, toDate] as const,
  entityTypes: () => [...AUDIT_KEYS.all, 'entityTypes'] as const,
};

export function useAuditTrailList(params: AuditTrailQueryParams) {
  return useQuery<PagedAuditResponse, Error>({
    queryKey: AUDIT_KEYS.list(params),
    queryFn: () => auditService.getPaged(params),
    enabled: !!params.companyId,
    staleTime: 30000,
  });
}

export function useAuditTrailDetail(id: string | null) {
  return useQuery<AuditTrailEntry, Error>({
    queryKey: AUDIT_KEYS.detail(id!),
    queryFn: () => auditService.getById(id!),
    enabled: !!id,
  });
}

export function useEntityAuditHistory(entityType: string, entityId: string) {
  return useQuery<AuditTrailEntry[], Error>({
    queryKey: AUDIT_KEYS.entityHistory(entityType, entityId),
    queryFn: () => auditService.getEntityHistory(entityType, entityId),
    enabled: !!entityType && !!entityId,
  });
}

export function useRecentAuditTrail(companyId: string, limit = 50) {
  return useQuery<AuditTrailEntry[], Error>({
    queryKey: AUDIT_KEYS.recent(companyId),
    queryFn: () => auditService.getRecent(companyId, limit),
    enabled: !!companyId,
    staleTime: 30000,
  });
}

export function useAuditStats(companyId: string, fromDate?: string, toDate?: string) {
  return useQuery<AuditTrailSummary, Error>({
    queryKey: AUDIT_KEYS.stats(companyId, fromDate, toDate),
    queryFn: () => auditService.getStats(companyId, fromDate, toDate),
    enabled: !!companyId,
  });
}

export function useAuditEntityTypes() {
  return useQuery<string[], Error>({
    queryKey: AUDIT_KEYS.entityTypes(),
    queryFn: () => auditService.getEntityTypes(),
    staleTime: 300000,
  });
}

export function useExportAuditTrail() {
  return useMutation<Blob, Error, AuditTrailExportParams>({
    mutationFn: (params) => auditService.exportCsv(params),
    onSuccess: (blob, params) => {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `audit_trail_${params.fromDate}_${params.toDate}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
  });
}

const OPERATION_COLORS: Record<string, string> = {
  create: 'bg-green-100 text-green-800',
  update: 'bg-blue-100 text-blue-800',
  delete: 'bg-red-100 text-red-800',
};

export function getOperationColor(operation: string): string {
  return OPERATION_COLORS[operation] ?? 'bg-gray-100 text-gray-800';
}

export function formatEntityType(entityType: string): string {
  return entityType
    .split('_')
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
}
