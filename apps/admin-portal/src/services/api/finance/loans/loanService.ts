import { apiClient } from '../../client';
import {
  Loan,
  LoanScheduleDto,
  CreateLoanDto,
  UpdateLoanDto,
  CreateEmiPaymentDto,
  PrepaymentDto,
  LoanTransaction,
  PagedResponse,
  PaginationParams,
} from '../../types';

export class LoanService {
  private readonly endpoint = 'loans';

  async getById(id: string): Promise<Loan> {
    return apiClient.get<Loan>(`${this.endpoint}/${id}`);
  }

  async getPaged(params: PaginationParams & { companyId?: string; status?: string; loanType?: string; assetId?: string } = {}): Promise<PagedResponse<Loan>> {
    return apiClient.getPaged<Loan>(this.endpoint, params);
  }

  async create(data: CreateLoanDto): Promise<Loan> {
    return apiClient.post<Loan, CreateLoanDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateLoanDto): Promise<void> {
    return apiClient.put<void, UpdateLoanDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async getSchedule(id: string): Promise<LoanScheduleDto> {
    return apiClient.get<LoanScheduleDto>(`${this.endpoint}/${id}/schedule`);
  }

  async recordEmiPayment(id: string, data: CreateEmiPaymentDto): Promise<Loan> {
    return apiClient.post<Loan, CreateEmiPaymentDto>(`${this.endpoint}/${id}/emi-payment`, data);
  }

  async recordPrepayment(id: string, data: PrepaymentDto): Promise<Loan> {
    return apiClient.post<Loan, PrepaymentDto>(`${this.endpoint}/${id}/prepayment`, data);
  }

  async foreclose(id: string, notes?: string): Promise<Loan> {
    return apiClient.post<Loan, { notes?: string }>(`${this.endpoint}/${id}/foreclose`, { notes });
  }

  async getOutstanding(companyId?: string): Promise<Loan[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return apiClient.get<Loan[]>(`${this.endpoint}/outstanding${params}`);
  }

  async getTotalInterestPaid(id: string, fromDate?: string, toDate?: string): Promise<number> {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    const queryString = params.toString();
    return apiClient.get<number>(`${this.endpoint}/${id}/interest${queryString ? `?${queryString}` : ''}`);
  }

  async getInterestPayments(companyId?: string, fromDate?: string, toDate?: string): Promise<LoanTransaction[]> {
    const params = new URLSearchParams();
    if (companyId) params.append('companyId', companyId);
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    const queryString = params.toString();
    return apiClient.get<LoanTransaction[]>(`${this.endpoint}/interest-payments${queryString ? `?${queryString}` : ''}`);
  }
}

export const loanService = new LoanService();

