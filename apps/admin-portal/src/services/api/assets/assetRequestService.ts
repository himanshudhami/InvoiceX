import { apiClient } from '../client';
import {
  AssetRequestSummary,
  AssetRequestDetail,
  AssetRequestStats,
  CreateAssetRequestDto,
  UpdateAssetRequestDto,
  ApproveAssetRequestDto,
  RejectAssetRequestDto,
  CancelAssetRequestDto,
  FulfillAssetRequestDto,
  AssetRequestFilterParams,
} from '../types';

export class AssetRequestService {
  private readonly endpoint = 'asset-requests';

  async getById(id: string): Promise<AssetRequestDetail> {
    return apiClient.get<AssetRequestDetail>(`${this.endpoint}/${id}`);
  }

  async getByCompany(companyId: string, status?: string): Promise<AssetRequestSummary[]> {
    const params = status ? `?status=${status}` : '';
    return apiClient.get<AssetRequestSummary[]>(`${this.endpoint}/company/${companyId}${params}`);
  }

  async getByEmployee(employeeId: string, status?: string): Promise<AssetRequestSummary[]> {
    const params = status ? `?status=${status}` : '';
    return apiClient.get<AssetRequestSummary[]>(`${this.endpoint}/employee/${employeeId}${params}`);
  }

  async getPending(companyId: string): Promise<AssetRequestSummary[]> {
    return apiClient.get<AssetRequestSummary[]>(`${this.endpoint}/company/${companyId}/pending`);
  }

  async getUnfulfilled(companyId: string): Promise<AssetRequestSummary[]> {
    return apiClient.get<AssetRequestSummary[]>(`${this.endpoint}/company/${companyId}/unfulfilled`);
  }

  async getStats(companyId: string): Promise<AssetRequestStats> {
    return apiClient.get<AssetRequestStats>(`${this.endpoint}/company/${companyId}/stats`);
  }

  async create(employeeId: string, companyId: string, data: CreateAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.post<AssetRequestDetail, CreateAssetRequestDto>(
      `${this.endpoint}?employeeId=${employeeId}&companyId=${companyId}`,
      data
    );
  }

  async update(id: string, employeeId: string, data: UpdateAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.put<AssetRequestDetail, UpdateAssetRequestDto>(
      `${this.endpoint}/${id}?employeeId=${employeeId}`,
      data
    );
  }

  async approve(id: string, approvedBy: string, data: ApproveAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.post<AssetRequestDetail, ApproveAssetRequestDto>(
      `${this.endpoint}/${id}/approve?approvedBy=${approvedBy}`,
      data
    );
  }

  async reject(id: string, rejectedBy: string, data: RejectAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.post<AssetRequestDetail, RejectAssetRequestDto>(
      `${this.endpoint}/${id}/reject?rejectedBy=${rejectedBy}`,
      data
    );
  }

  async cancel(id: string, cancelledBy: string, data: CancelAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.post<AssetRequestDetail, CancelAssetRequestDto>(
      `${this.endpoint}/${id}/cancel?cancelledBy=${cancelledBy}`,
      data
    );
  }

  async fulfill(id: string, fulfilledBy: string, data: FulfillAssetRequestDto): Promise<AssetRequestDetail> {
    return apiClient.post<AssetRequestDetail, FulfillAssetRequestDto>(
      `${this.endpoint}/${id}/fulfill?fulfilledBy=${fulfilledBy}`,
      data
    );
  }

  async withdraw(id: string, employeeId: string, reason?: string): Promise<void> {
    const params = reason ? `&reason=${encodeURIComponent(reason)}` : '';
    return apiClient.post<void, {}>(`${this.endpoint}/${id}/withdraw?employeeId=${employeeId}${params}`, {});
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }
}

export const assetRequestService = new AssetRequestService();
