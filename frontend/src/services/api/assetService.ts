import { apiClient } from './client';
import {
  Asset,
  AssetAssignment,
  CreateAssetDto,
  UpdateAssetDto,
  CreateAssetAssignmentDto,
  ReturnAssetAssignmentDto,
  PagedResponse,
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
} from './types';

export class AssetService {
  private readonly endpoint = 'assets';

  async getById(id: string): Promise<Asset> {
    return apiClient.get<Asset>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams = {}): Promise<PagedResponse<Asset>> {
    return apiClient.getPaged<Asset>(this.endpoint, params);
  }

  async create(data: CreateAssetDto): Promise<Asset> {
    return apiClient.post<Asset, CreateAssetDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateAssetDto): Promise<void> {
    return apiClient.put<void, UpdateAssetDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async getAllAssignments(): Promise<AssetAssignment[]> {
    return apiClient.get<AssetAssignment[]>(`${this.endpoint}/assignments`);
  }

  async getAssignments(id: string): Promise<AssetAssignment[]> {
    return apiClient.get<AssetAssignment[]>(`${this.endpoint}/${id}/assignments`);
  }

  async assign(id: string, data: CreateAssetAssignmentDto): Promise<AssetAssignment> {
    return apiClient.post<AssetAssignment, CreateAssetAssignmentDto>(`${this.endpoint}/${id}/assign`, data);
  }

  async returnAssignment(assignmentId: string, data: ReturnAssetAssignmentDto): Promise<void> {
    return apiClient.post<void, ReturnAssetAssignmentDto>(`${this.endpoint}/assignments/${assignmentId}/return`, data);
  }

  async getDocuments(assetId: string): Promise<AssetDocument[]> {
    return apiClient.get<AssetDocument[]>(`${this.endpoint}/${assetId}/documents`);
  }

  async addDocument(assetId: string, data: CreateAssetDocumentDto): Promise<AssetDocument> {
    return apiClient.post<AssetDocument, CreateAssetDocumentDto>(`${this.endpoint}/${assetId}/documents`, data);
  }

  async deleteDocument(documentId: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/documents/${documentId}`);
  }

  async getMaintenance(params: PaginationParams & { assetId?: string; companyId?: string; status?: string } = {}): Promise<PagedResponse<AssetMaintenance>> {
    return apiClient.get<PagedResponse<AssetMaintenance>>(`${this.endpoint}/maintenance`, params);
  }

  async getMaintenanceForAsset(assetId: string): Promise<AssetMaintenance[]> {
    return apiClient.get<AssetMaintenance[]>(`${this.endpoint}/${assetId}/maintenance`);
  }

  async createMaintenance(assetId: string, data: CreateAssetMaintenanceDto): Promise<AssetMaintenance> {
    return apiClient.post<AssetMaintenance, CreateAssetMaintenanceDto>(`${this.endpoint}/${assetId}/maintenance`, data);
  }

  async updateMaintenance(maintenanceId: string, data: UpdateAssetMaintenanceDto): Promise<void> {
    return apiClient.put<void, UpdateAssetMaintenanceDto>(`${this.endpoint}/maintenance/${maintenanceId}`, data);
  }

  async dispose(assetId: string, data: CreateAssetDisposalDto): Promise<AssetDisposal> {
    return apiClient.post<AssetDisposal, CreateAssetDisposalDto>(`${this.endpoint}/${assetId}/dispose`, data);
  }

  async getCostSummary(assetId: string): Promise<AssetCostSummary> {
    return apiClient.get<AssetCostSummary>(`${this.endpoint}/${assetId}/summary`);
  }

  async getCostReport(companyId?: string): Promise<AssetCostReport> {
    return apiClient.get<AssetCostReport>(`${this.endpoint}/cost-report`, companyId ? { companyId } : undefined);
  }

  async bulkCreate(data: BulkAssetsDto): Promise<BulkUploadResult> {
    return apiClient.post<BulkUploadResult, BulkAssetsDto>(`${this.endpoint}/bulk`, data);
  }

  async linkAssetToLoan(assetId: string, loanId: string): Promise<Asset> {
    return apiClient.post<Asset, string>(`${this.endpoint}/${assetId}/link-loan`, loanId);
  }

  async getAssetsByLoan(loanId: string): Promise<Asset[]> {
    return apiClient.get<Asset[]>(`${this.endpoint}/by-loan/${loanId}`);
  }
}

export const assetService = new AssetService();









