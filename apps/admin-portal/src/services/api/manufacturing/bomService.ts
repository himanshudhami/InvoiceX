import { apiClient } from '../client';
import type {
  BillOfMaterials,
  CreateBomDto,
  UpdateBomDto,
  CopyBomDto,
  PagedBomResponse,
} from '../types';

/**
 * Bill of Materials API service
 */
export class BomService {
  private readonly endpoint = 'manufacturing/bom';

  async getAll(companyId?: string): Promise<BillOfMaterials[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<BillOfMaterials[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<BillOfMaterials> {
    return apiClient.get<BillOfMaterials>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: {
    pageNumber?: number;
    pageSize?: number;
    companyId?: string;
    searchTerm?: string;
    finishedGoodId?: string;
    isActive?: boolean;
  } = {}): Promise<PagedBomResponse> {
    const queryParams = new URLSearchParams();
    if (params.pageNumber) queryParams.append('pageNumber', params.pageNumber.toString());
    if (params.pageSize) queryParams.append('pageSize', params.pageSize.toString());
    if (params.companyId) queryParams.append('companyId', params.companyId);
    if (params.searchTerm) queryParams.append('searchTerm', params.searchTerm);
    if (params.finishedGoodId) queryParams.append('finishedGoodId', params.finishedGoodId);
    if (params.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());

    const url = queryParams.toString() ? `${this.endpoint}/paged?${queryParams}` : `${this.endpoint}/paged`;
    return apiClient.get<PagedBomResponse>(url);
  }

  async getActive(companyId?: string): Promise<BillOfMaterials[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<BillOfMaterials[]>(`${this.endpoint}/active`, params);
  }

  async getByFinishedGood(finishedGoodId: string): Promise<BillOfMaterials[]> {
    return apiClient.get<BillOfMaterials[]>(`${this.endpoint}/by-finished-good/${finishedGoodId}`);
  }

  async getActiveBomForProduct(finishedGoodId: string): Promise<BillOfMaterials> {
    return apiClient.get<BillOfMaterials>(`${this.endpoint}/active-for-product/${finishedGoodId}`);
  }

  async create(data: CreateBomDto): Promise<BillOfMaterials> {
    return apiClient.post<BillOfMaterials, CreateBomDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateBomDto): Promise<void> {
    return apiClient.put<void, UpdateBomDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async copy(id: string, data: CopyBomDto): Promise<BillOfMaterials> {
    return apiClient.post<BillOfMaterials, CopyBomDto>(`${this.endpoint}/${id}/copy`, data);
  }
}

// Singleton instance
export const bomService = new BomService();
