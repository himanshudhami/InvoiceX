import { apiClient } from './client';
import { Quote, QuoteItem, CreateQuoteDto, UpdateQuoteDto, PagedResponse, QuotesFilterParams } from './types';

/**
 * Quote API service following SRP - handles only quote-related API calls
 */
export class QuoteService {
  private readonly endpoint = 'quotes';
  private readonly itemsEndpoint = 'quoteitems';

  // Quote operations
  async getAll(companyId?: string): Promise<Quote[]> {
    const params = companyId ? { companyId } : undefined;
    // Note: Items are fetched only when viewing/editing a single quote (getById)
    // to avoid N+1 query performance issues in list views
    return apiClient.get<Quote[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<Quote> {
    const quote = await apiClient.get<Quote>(`${this.endpoint}/${id}`);

    // Fetch items for this quote
    try {
      const items = await this.getQuoteItems(id);
      return { ...quote, items };
    } catch (error) {
      console.error(`Failed to fetch items for quote ${id}:`, error);
      return { ...quote, items: [] };
    }
  }

  async getPaged(params: QuotesFilterParams = {}): Promise<PagedResponse<Quote>> {
    return apiClient.getPaged<Quote>(this.endpoint, params);
  }

  async create(data: CreateQuoteDto): Promise<Quote> {
    return apiClient.post<Quote, CreateQuoteDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateQuoteDto): Promise<void> {
    return apiClient.put<void, UpdateQuoteDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async duplicate(id: string): Promise<Quote> {
    return apiClient.post<Quote>(`${this.endpoint}/${id}/duplicate`, {});
  }

  async send(id: string): Promise<void> {
    return apiClient.post<void>(`${this.endpoint}/${id}/send`, {});
  }

  async accept(id: string): Promise<void> {
    return apiClient.post<void>(`${this.endpoint}/${id}/accept`, {});
  }

  async reject(id: string, reason?: string): Promise<void> {
    return apiClient.post<void>(`${this.endpoint}/${id}/reject`, { reason });
  }

  async convertToInvoice(id: string): Promise<any> {
    return apiClient.post<any>(`${this.endpoint}/${id}/convert-to-invoice`, {});
  }

  // Quote items operations
  async getQuoteItems(quoteId: string): Promise<QuoteItem[]> {
    // Backend returns { data: T[], currentPage, totalCount, ... } format
    // We need to map this to our expected { items: T[], pageNumber, ... } format
    const backendResponse = await apiClient.get<{
      data: QuoteItem[];
      currentPage: number;
      totalCount: number;
      pageSize: number;
      totalPages: number;
      hasPrevious: boolean;
      hasNext: boolean;
    }>(`${this.itemsEndpoint}/paged?quoteId=${quoteId}&pageSize=100`);

    return backendResponse.data;
  }

  async createQuoteItem(data: Omit<QuoteItem, 'id' | 'createdAt' | 'updatedAt'>): Promise<QuoteItem> {
    return apiClient.post<QuoteItem>(this.itemsEndpoint, data);
  }

  async updateQuoteItem(id: string, data: Partial<QuoteItem>): Promise<void> {
    return apiClient.put<void>(`${this.itemsEndpoint}/${id}`, data);
  }

  async deleteQuoteItem(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.itemsEndpoint}/${id}`);
  }
}

// Singleton instance
export const quoteService = new QuoteService();
