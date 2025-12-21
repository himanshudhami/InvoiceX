import { apiClient } from '../../client';
import {
  BankAccount,
  CreateBankAccountDto,
  UpdateBankAccountDto,
  UpdateBalanceDto,
  BankAccountFilterParams,
  PagedResponse
} from '../../types';

export class BankAccountService {
  private readonly endpoint = 'bankaccounts';

  async getPaged(params?: BankAccountFilterParams): Promise<PagedResponse<BankAccount>> {
    return apiClient.getPaged<BankAccount>(this.endpoint, params);
  }

  async getAll(): Promise<BankAccount[]> {
    return apiClient.get<BankAccount[]>(this.endpoint);
  }

  async getById(id: string): Promise<BankAccount> {
    return apiClient.get<BankAccount>(`${this.endpoint}/${id}`);
  }

  async create(data: CreateBankAccountDto): Promise<BankAccount> {
    return apiClient.post<BankAccount>(this.endpoint, data);
  }

  async update(id: string, data: UpdateBankAccountDto): Promise<BankAccount> {
    return apiClient.put<BankAccount>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete(`${this.endpoint}/${id}`);
  }

  // Get bank accounts by company
  async getByCompanyId(companyId: string): Promise<BankAccount[]> {
    return apiClient.get<BankAccount[]>(`${this.endpoint}/by-company/${companyId}`);
  }

  // Get primary bank account for a company
  async getPrimaryAccount(companyId: string): Promise<BankAccount | null> {
    try {
      return await apiClient.get<BankAccount>(`${this.endpoint}/primary/${companyId}`);
    } catch {
      return null;
    }
  }

  // Get all active bank accounts
  async getActiveAccounts(): Promise<BankAccount[]> {
    return apiClient.get<BankAccount[]>(`${this.endpoint}/active`);
  }

  // Update account balance
  async updateBalance(id: string, data: UpdateBalanceDto): Promise<void> {
    return apiClient.put(`${this.endpoint}/${id}/update-balance`, data);
  }

  // Set primary bank account for a company
  async setPrimaryAccount(companyId: string, accountId: string): Promise<void> {
    return apiClient.put(`${this.endpoint}/${companyId}/set-primary/${accountId}`, {});
  }
}

export const bankAccountService = new BankAccountService();
