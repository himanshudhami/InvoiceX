import { apiClient } from '../client';
import type {
  Tag,
  CreateTagDto,
  UpdateTagDto,
  TagSummary,
  TagsFilterParams,
  TransactionTag,
  ApplyTagsToTransactionDto,
  AutoAttributeRequest,
  AutoAttributionResult,
  PagedResponse,
} from '../types';

/**
 * Tag API service - handles tag management and transaction tagging
 */
export class TagService {
  private readonly endpoint = 'tags';

  // ==================== Tag CRUD ====================

  async getAll(companyId?: string): Promise<Tag[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Tag[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Tag> {
    return apiClient.get<Tag>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: TagsFilterParams = {}): Promise<PagedResponse<Tag>> {
    return apiClient.getPaged<Tag>(this.endpoint, params);
  }

  async create(data: CreateTagDto): Promise<Tag> {
    return apiClient.post<Tag, CreateTagDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateTagDto): Promise<void> {
    return apiClient.put<void, UpdateTagDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // ==================== Tag Queries ====================

  async getByGroup(companyId: string, tagGroup: string): Promise<Tag[]> {
    return apiClient.get<Tag[]>(`${this.endpoint}/group/${tagGroup}`, { companyId });
  }

  async getHierarchy(companyId: string, tagGroup?: string): Promise<Tag[]> {
    const params: Record<string, string> = { companyId };
    if (tagGroup) params.tagGroup = tagGroup;
    return apiClient.get<Tag[]>(`${this.endpoint}/hierarchy`, params);
  }

  async getSummaries(companyId: string): Promise<TagSummary[]> {
    return apiClient.get<TagSummary[]>(`${this.endpoint}/summaries`, { companyId });
  }

  // ==================== Transaction Tagging ====================

  async getTransactionTags(transactionId: string, transactionType: string): Promise<TransactionTag[]> {
    return apiClient.get<TransactionTag[]>(
      `${this.endpoint}/transaction/${transactionType}/${transactionId}`
    );
  }

  async applyTags(data: ApplyTagsToTransactionDto): Promise<void> {
    return apiClient.post<void, ApplyTagsToTransactionDto>(`${this.endpoint}/apply`, data);
  }

  async removeTag(transactionId: string, transactionType: string, tagId: string): Promise<void> {
    return apiClient.delete<void>(
      `${this.endpoint}/transaction/${transactionType}/${transactionId}/tag/${tagId}`
    );
  }

  // ==================== Auto Attribution ====================

  async autoAttribute(data: AutoAttributeRequest): Promise<AutoAttributionResult> {
    return apiClient.post<AutoAttributionResult, AutoAttributeRequest>(
      `${this.endpoint}/auto-attribute`,
      data
    );
  }

  // ==================== Utilities ====================

  async seedDefaults(companyId: string): Promise<void> {
    return apiClient.post<void, {}>(`${this.endpoint}/seed-defaults`, {}, {
      params: { companyId }
    });
  }
}

// Singleton instance
export const tagService = new TagService();
