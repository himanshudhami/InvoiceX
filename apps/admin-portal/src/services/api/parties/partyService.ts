import { apiClient } from '../client';
import type {
  Party,
  PartyListItem,
  CreatePartyDto,
  UpdatePartyDto,
  PartiesFilterParams,
  PartyVendorProfile,
  PartyCustomerProfile,
  CreatePartyVendorProfileDto,
  UpdatePartyVendorProfileDto,
  CreatePartyCustomerProfileDto,
  UpdatePartyCustomerProfileDto,
  PartyTag,
  TdsSectionRule,
  CreateTdsSectionRuleDto,
  UpdateTdsSectionRuleDto,
  TdsSectionRulesFilterParams,
  TdsConfiguration,
  PartyOutstanding,
  PartyAgingSummary,
  PartyTdsSummary,
  PagedResponse,
} from '../types';

/**
 * Party API service - unified party management (vendors, customers, employees)
 * Replaces separate VendorService and CustomerService
 */
export class PartyService {
  private readonly endpoint = 'parties';

  // ==================== Party CRUD ====================

  async getAll(companyId?: string): Promise<Party[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<Party[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Party> {
    return apiClient.get<Party>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PartiesFilterParams = {}): Promise<PagedResponse<PartyListItem>> {
    return apiClient.getPaged<PartyListItem>(this.endpoint, params);
  }

  async create(data: CreatePartyDto): Promise<Party> {
    return apiClient.post<Party, CreatePartyDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdatePartyDto): Promise<void> {
    return apiClient.put<void, UpdatePartyDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  // ==================== Filtered Queries ====================

  async getVendors(params: Omit<PartiesFilterParams, 'isVendor'> = {}): Promise<PagedResponse<PartyListItem>> {
    return this.getPaged({ ...params, isVendor: true });
  }

  async getCustomers(params: Omit<PartiesFilterParams, 'isCustomer'> = {}): Promise<PagedResponse<PartyListItem>> {
    return this.getPaged({ ...params, isCustomer: true });
  }

  async getByTallyGroup(companyId: string, tallyGroupName: string): Promise<Party[]> {
    return apiClient.get<Party[]>(this.endpoint, { companyId, tallyGroupName });
  }

  // ==================== Vendor Profile ====================

  async getVendorProfile(partyId: string): Promise<PartyVendorProfile | null> {
    try {
      return await apiClient.get<PartyVendorProfile>(`${this.endpoint}/${partyId}/vendor-profile`);
    } catch {
      return null;
    }
  }

  async createVendorProfile(partyId: string, data: CreatePartyVendorProfileDto): Promise<PartyVendorProfile> {
    return apiClient.post<PartyVendorProfile, CreatePartyVendorProfileDto>(
      `${this.endpoint}/${partyId}/vendor-profile`,
      data
    );
  }

  async updateVendorProfile(partyId: string, data: UpdatePartyVendorProfileDto): Promise<void> {
    return apiClient.put<void, UpdatePartyVendorProfileDto>(
      `${this.endpoint}/${partyId}/vendor-profile`,
      data
    );
  }

  // ==================== Customer Profile ====================

  async getCustomerProfile(partyId: string): Promise<PartyCustomerProfile | null> {
    try {
      return await apiClient.get<PartyCustomerProfile>(`${this.endpoint}/${partyId}/customer-profile`);
    } catch {
      return null;
    }
  }

  async createCustomerProfile(partyId: string, data: CreatePartyCustomerProfileDto): Promise<PartyCustomerProfile> {
    return apiClient.post<PartyCustomerProfile, CreatePartyCustomerProfileDto>(
      `${this.endpoint}/${partyId}/customer-profile`,
      data
    );
  }

  async updateCustomerProfile(partyId: string, data: UpdatePartyCustomerProfileDto): Promise<void> {
    return apiClient.put<void, UpdatePartyCustomerProfileDto>(
      `${this.endpoint}/${partyId}/customer-profile`,
      data
    );
  }

  // ==================== Party Tags ====================

  async getTags(partyId: string): Promise<PartyTag[]> {
    return apiClient.get<PartyTag[]>(`${this.endpoint}/${partyId}/tags`);
  }

  async addTag(partyId: string, tagId: string, source: string = 'manual'): Promise<PartyTag> {
    return apiClient.post<PartyTag, { tagId: string; source: string }>(
      `${this.endpoint}/${partyId}/tags`,
      { tagId, source }
    );
  }

  async removeTag(partyId: string, tagId: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${partyId}/tags/${tagId}`);
  }

  // ==================== TDS Configuration ====================

  async getTdsConfiguration(partyId: string): Promise<TdsConfiguration | null> {
    try {
      return await apiClient.get<TdsConfiguration>(`${this.endpoint}/${partyId}/tds-configuration`);
    } catch {
      return null;
    }
  }

  // ==================== Outstanding & Aging ====================

  async getOutstanding(companyId: string, isVendor?: boolean): Promise<PartyOutstanding[]> {
    const params: Record<string, string | boolean> = { companyId };
    if (isVendor !== undefined) params.isVendor = isVendor;
    return apiClient.get<PartyOutstanding[]>(`${this.endpoint}/outstanding`, params);
  }

  async getPartyOutstanding(partyId: string): Promise<PartyOutstanding> {
    return apiClient.get<PartyOutstanding>(`${this.endpoint}/${partyId}/outstanding`);
  }

  async getAgingSummary(companyId: string, isVendor?: boolean, asOfDate?: string): Promise<PartyAgingSummary[]> {
    const params: Record<string, string | boolean> = { companyId };
    if (isVendor !== undefined) params.isVendor = isVendor;
    if (asOfDate) params.asOfDate = asOfDate;
    return apiClient.get<PartyAgingSummary[]>(`${this.endpoint}/aging`, params);
  }

  // ==================== TDS Summary ====================

  async getTdsSummary(companyId: string, financialYear?: string): Promise<PartyTdsSummary[]> {
    const params: Record<string, string> = { companyId };
    if (financialYear) params.financialYear = financialYear;
    return apiClient.get<PartyTdsSummary[]>(`${this.endpoint}/tds-summary`, params);
  }
}

/**
 * TDS Section Rule API service
 */
export class TdsSectionRuleService {
  private readonly endpoint = 'tds-section-rules';

  async getAll(companyId: string): Promise<TdsSectionRule[]> {
    return apiClient.get<TdsSectionRule[]>(this.endpoint, { companyId });
  }

  async getById(id: string): Promise<TdsSectionRule> {
    return apiClient.get<TdsSectionRule>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: TdsSectionRulesFilterParams = {}): Promise<PagedResponse<TdsSectionRule>> {
    return apiClient.getPaged<TdsSectionRule>(this.endpoint, params);
  }

  async create(data: CreateTdsSectionRuleDto): Promise<TdsSectionRule> {
    return apiClient.post<TdsSectionRule, CreateTdsSectionRuleDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateTdsSectionRuleDto): Promise<void> {
    return apiClient.put<void, UpdateTdsSectionRuleDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async getBySection(companyId: string, tdsSection: string): Promise<TdsSectionRule[]> {
    return apiClient.get<TdsSectionRule[]>(`${this.endpoint}/by-section`, { companyId, tdsSection });
  }

  async seedDefaults(companyId: string): Promise<void> {
    return apiClient.post<void, { companyId: string }>(`${this.endpoint}/seed-defaults`, { companyId });
  }
}

// Singleton instances
export const partyService = new PartyService();
export const tdsSectionRuleService = new TdsSectionRuleService();
