import { apiClient } from '../../client';
import {
  ChartOfAccount,
  CreateChartOfAccountDto,
  UpdateChartOfAccountDto,
  JournalEntry,
  CreateJournalEntryDto,
  PostingRule,
  TrialBalanceReport,
  IncomeStatementReport,
  BalanceSheetReport,
  AccountLedgerReport,
  AbnormalBalanceReport,
  AbnormalBalanceAlertSummary,
  PagedResponse,
  ChartOfAccountsFilterParams,
  JournalEntriesFilterParams,
} from '../../types';

/**
 * Ledger API service for General Ledger operations
 * Handles Chart of Accounts, Journal Entries, and Financial Reports
 */
export class LedgerService {
  private readonly baseEndpoint = 'ledger';

  // ==================== Chart of Accounts ====================

  async getAccounts(companyId?: string): Promise<ChartOfAccount[]> {
    if (!companyId) {
      // Return empty array if no company selected - UI should prompt user to select company
      return [];
    }
    return apiClient.get<ChartOfAccount[]>(`${this.baseEndpoint}/accounts/company/${companyId}`);
  }

  async getAccountsPaged(params: ChartOfAccountsFilterParams = {}): Promise<PagedResponse<ChartOfAccount>> {
    const { companyId, ...rest } = params;
    if (companyId) {
      // Backend doesn't have paged endpoint, fetch all and simulate pagination
      const accounts = await this.getAccounts(companyId);
      return {
        items: accounts,
        totalCount: accounts.length,
        pageNumber: 1,
        pageSize: accounts.length,
        totalPages: 1,
      };
    }
    return apiClient.getPaged<ChartOfAccount>(`${this.baseEndpoint}/accounts`, rest);
  }

  async getAccountById(id: string): Promise<ChartOfAccount> {
    return apiClient.get<ChartOfAccount>(`${this.baseEndpoint}/accounts/${id}`);
  }

  async getAccountByCode(companyId: string, accountCode: string): Promise<ChartOfAccount> {
    return apiClient.get<ChartOfAccount>(`${this.baseEndpoint}/accounts/company/${companyId}/code/${accountCode}`);
  }

  async createAccount(data: CreateChartOfAccountDto): Promise<ChartOfAccount> {
    return apiClient.post<ChartOfAccount, CreateChartOfAccountDto>(`${this.baseEndpoint}/accounts`, data);
  }

  async updateAccount(id: string, data: UpdateChartOfAccountDto): Promise<void> {
    return apiClient.put<void, UpdateChartOfAccountDto>(`${this.baseEndpoint}/accounts/${id}`, data);
  }

  async deleteAccount(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.baseEndpoint}/accounts/${id}`);
  }

  async initializeChartOfAccounts(companyId: string): Promise<void> {
    return apiClient.post<void, Record<string, never>>(`${this.baseEndpoint}/accounts/company/${companyId}/initialize`, {});
  }

  // ==================== Journal Entries ====================

  async getJournalEntries(companyId?: string): Promise<JournalEntry[]> {
    if (!companyId) {
      // Return empty array if no company selected - UI should prompt user to select company
      return [];
    }
    return apiClient.get<JournalEntry[]>(`${this.baseEndpoint}/journals/company/${companyId}`);
  }

  async getJournalEntriesPaged(params: JournalEntriesFilterParams = {}): Promise<PagedResponse<JournalEntry>> {
    const { companyId, ...rest } = params;
    if (companyId) {
      // Backend doesn't have paged endpoint, fetch all and simulate pagination
      const entries = await this.getJournalEntries(companyId);
      return {
        items: entries,
        totalCount: entries.length,
        pageNumber: 1,
        pageSize: entries.length,
        totalPages: 1,
      };
    }
    return apiClient.getPaged<JournalEntry>(`${this.baseEndpoint}/journals`, rest);
  }

  async getJournalEntryById(id: string): Promise<JournalEntry> {
    return apiClient.get<JournalEntry>(`${this.baseEndpoint}/journals/${id}`);
  }

  async createJournalEntry(data: CreateJournalEntryDto): Promise<JournalEntry> {
    return apiClient.post<JournalEntry, CreateJournalEntryDto>(`${this.baseEndpoint}/journals`, data);
  }

  async postJournalEntry(id: string): Promise<JournalEntry> {
    return apiClient.post<JournalEntry, Record<string, never>>(`${this.baseEndpoint}/journals/${id}/post`, {});
  }

  async reverseJournalEntry(id: string, reason?: string): Promise<JournalEntry> {
    return apiClient.post<JournalEntry, { reason?: string }>(`${this.baseEndpoint}/journals/${id}/reverse`, { reason });
  }

  // ==================== Posting Rules ====================

  async getPostingRules(companyId: string): Promise<PostingRule[]> {
    return apiClient.get<PostingRule[]>(`${this.baseEndpoint}/posting-rules/company/${companyId}`);
  }

  async getPostingRuleById(id: string): Promise<PostingRule> {
    return apiClient.get<PostingRule>(`${this.baseEndpoint}/posting-rules/${id}`);
  }

  // ==================== Financial Reports ====================

  async getTrialBalance(companyId: string, asOfDate: string, includeZeroBalances = false): Promise<TrialBalanceReport> {
    return apiClient.get<TrialBalanceReport>(`${this.baseEndpoint}/reports/trial-balance/${companyId}`, {
      asOfDate,
      includeZeroBalances,
    });
  }

  async getTrialBalanceByPeriod(
    companyId: string,
    financialYear: string,
    periodMonth?: number
  ): Promise<TrialBalanceReport> {
    return apiClient.get<TrialBalanceReport>(`${this.baseEndpoint}/reports/trial-balance/${companyId}`, {
      financialYear,
      periodMonth,
    });
  }

  async getIncomeStatement(companyId: string, fromDate: string, toDate: string): Promise<IncomeStatementReport> {
    return apiClient.get<IncomeStatementReport>(`${this.baseEndpoint}/reports/income-statement/${companyId}`, {
      fromDate,
      toDate,
    });
  }

  async getBalanceSheet(companyId: string, asOfDate: string): Promise<BalanceSheetReport> {
    return apiClient.get<BalanceSheetReport>(`${this.baseEndpoint}/reports/balance-sheet/${companyId}`, {
      asOfDate,
    });
  }

  async getAccountLedger(accountId: string, fromDate: string, toDate: string): Promise<AccountLedgerReport> {
    return apiClient.get<AccountLedgerReport>(`${this.baseEndpoint}/reports/account-ledger/${accountId}`, {
      fromDate,
      toDate,
    });
  }

  // ==================== Data Quality Reports ====================

  async getAbnormalBalances(companyId: string): Promise<AbnormalBalanceReport> {
    return apiClient.get<AbnormalBalanceReport>(`${this.baseEndpoint}/reports/abnormal-balances/${companyId}`);
  }

  async getAbnormalBalanceAlert(companyId: string): Promise<AbnormalBalanceAlertSummary> {
    return apiClient.get<AbnormalBalanceAlertSummary>(`${this.baseEndpoint}/alerts/abnormal-balances/${companyId}`);
  }

  // ==================== Auto-Posting ====================

  async postInvoice(invoiceId: string, autoPost = true): Promise<JournalEntry | null> {
    return apiClient.post<JournalEntry | null, Record<string, never>>(
      `${this.baseEndpoint}/auto-posting/invoice/${invoiceId}`,
      {}
    );
  }

  async postPayment(paymentId: string, autoPost = true): Promise<JournalEntry | null> {
    return apiClient.post<JournalEntry | null, Record<string, never>>(
      `${this.baseEndpoint}/auto-posting/payment/${paymentId}`,
      {}
    );
  }
}

// Singleton instance
export const ledgerService = new LedgerService();
