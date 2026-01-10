import { apiClient } from '../client';
import {
  CreditNote,
  CreditNoteItem,
  CreateCreditNoteDto,
  UpdateCreditNoteDto,
  PagedResponse,
  CreditNotesFilterParams,
  CreateCreditNoteFromInvoice
} from '../types';

/**
 * Credit Note API service - handles credit note related API calls
 * GST compliant as per Section 34 of CGST Act
 */
export class CreditNoteService {
  private readonly endpoint = 'creditnotes';

  // Credit Note operations
  async getAll(companyId?: string): Promise<CreditNote[]> {
    const params = companyId ? { companyId } : undefined;
    return apiClient.get<CreditNote[]>(this.endpoint, params);
  }

  async getById(id: string): Promise<CreditNote> {
    const creditNote = await apiClient.get<CreditNote>(`${this.endpoint}/${id}`);

    // Fetch items for this credit note
    try {
      const items = await this.getItems(id);
      return { ...creditNote, items };
    } catch (error) {
      console.error(`Failed to fetch items for credit note ${id}:`, error);
      return { ...creditNote, items: [] };
    }
  }

  async getPaged(params: CreditNotesFilterParams = {}): Promise<PagedResponse<CreditNote>> {
    return apiClient.getPaged<CreditNote>(this.endpoint, params);
  }

  async create(data: CreateCreditNoteDto): Promise<CreditNote> {
    return apiClient.post<CreditNote, CreateCreditNoteDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateCreditNoteDto): Promise<void> {
    return apiClient.put<void, UpdateCreditNoteDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  /**
   * Create a credit note from an existing invoice
   * This is the primary use case - selecting an invoice and issuing credit
   */
  async createFromInvoice(data: CreateCreditNoteFromInvoice): Promise<CreditNote> {
    return apiClient.post<CreditNote, CreateCreditNoteFromInvoice>(
      `${this.endpoint}/from-invoice`,
      data
    );
  }

  /**
   * Issue a draft credit note (changes status from draft to issued)
   */
  async issue(id: string): Promise<CreditNote> {
    return apiClient.post<CreditNote>(`${this.endpoint}/${id}/issue`, {});
  }

  /**
   * Cancel an issued credit note
   */
  async cancel(id: string, reason?: string): Promise<CreditNote> {
    return apiClient.post<CreditNote>(`${this.endpoint}/${id}/cancel`, { reason });
  }

  /**
   * Generate next credit note number for a company
   */
  async generateNextNumber(companyId: string): Promise<string> {
    const response = await apiClient.get<{ creditNoteNumber: string }>(
      `${this.endpoint}/generate-number?companyId=${companyId}`
    );
    return response.creditNoteNumber;
  }

  // Credit note items operations
  async getItems(creditNoteId: string): Promise<CreditNoteItem[]> {
    return apiClient.get<CreditNoteItem[]>(`${this.endpoint}/${creditNoteId}/items`);
  }

  /**
   * Get all credit notes for a specific invoice
   */
  async getByInvoiceId(invoiceId: string): Promise<CreditNote[]> {
    return apiClient.get<CreditNote[]>(`${this.endpoint}/by-invoice/${invoiceId}`);
  }
}

// Singleton instance
export const creditNoteService = new CreditNoteService();
