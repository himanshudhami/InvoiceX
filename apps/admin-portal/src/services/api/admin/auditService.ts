import { apiClient } from '../client';

export interface AuditTrailEntry {
  id: string;
  companyId: string;
  entityType: string;
  entityId: string;
  entityDisplayName?: string;
  operation: 'create' | 'update' | 'delete';
  oldValues?: Record<string, unknown>;
  newValues?: Record<string, unknown>;
  changedFields?: string[];
  actorId: string;
  actorName?: string;
  actorEmail?: string;
  actorIp?: string;
  correlationId?: string;
  requestPath?: string;
  requestMethod?: string;
  createdAt: string;
}

export interface AuditTrailQueryParams {
  companyId: string;
  pageNumber?: number;
  pageSize?: number;
  entityType?: string;
  entityId?: string;
  operation?: string;
  actorId?: string;
  fromDate?: string;
  toDate?: string;
  search?: string;
}

export interface AuditTrailExportParams {
  companyId: string;
  fromDate: string;
  toDate: string;
  entityType?: string;
}

export interface PagedAuditResponse {
  items: AuditTrailEntry[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface AuditTrailSummary {
  totalEntries: number;
  createCount: number;
  updateCount: number;
  deleteCount: number;
  lastActivityAt?: string;
}

function buildQueryString(params: Record<string, unknown>): string {
  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.append(key, String(value));
    }
  });
  return searchParams.toString();
}

class AuditService {
  private readonly endpoint = 'audit';

  async getPaged(params: AuditTrailQueryParams): Promise<PagedAuditResponse> {
    return apiClient.get<PagedAuditResponse>(`${this.endpoint}?${buildQueryString(params)}`);
  }

  async getById(id: string): Promise<AuditTrailEntry> {
    return apiClient.get<AuditTrailEntry>(`${this.endpoint}/${id}`);
  }

  async getEntityHistory(entityType: string, entityId: string): Promise<AuditTrailEntry[]> {
    return apiClient.get<AuditTrailEntry[]>(`${this.endpoint}/entity/${entityType}/${entityId}`);
  }

  async getRecent(companyId: string, limit = 50): Promise<AuditTrailEntry[]> {
    return apiClient.get<AuditTrailEntry[]>(`${this.endpoint}/recent`, { companyId, limit });
  }

  async getStats(companyId: string, fromDate?: string, toDate?: string): Promise<AuditTrailSummary> {
    return apiClient.get<AuditTrailSummary>(`${this.endpoint}/stats`, { companyId, fromDate, toDate });
  }

  async exportCsv(params: AuditTrailExportParams): Promise<Blob> {
    const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5001/api';
    const response = await fetch(`${baseUrl}/${this.endpoint}/export?${buildQueryString(params)}`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('admin_access_token')}` },
    });

    if (!response.ok) throw new Error('Export failed');
    return response.blob();
  }

  async getEntityTypes(): Promise<string[]> {
    return apiClient.get<string[]>(`${this.endpoint}/entity-types`);
  }
}

export const auditService = new AuditService();
