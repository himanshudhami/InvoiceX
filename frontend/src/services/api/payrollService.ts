import { apiClient } from './client';
import { PagedResponse, PaginationParams } from './types';
import type {
  PayrollRun,
  CreatePayrollRunDto,
  UpdatePayrollRunDto,
  ProcessPayrollDto,
  PayrollRunSummary,
  PayrollPreview,
  PayrollTransaction,
  TdsOverrideDto,
  EmployeeSalaryStructure,
  CreateEmployeeSalaryStructureDto,
  UpdateEmployeeSalaryStructureDto,
  SalaryBreakdown,
  EmployeeTaxDeclaration,
  CreateEmployeeTaxDeclarationDto,
  UpdateEmployeeTaxDeclarationDto,
  ContractorPayment,
  CreateContractorPaymentDto,
  UpdateContractorPaymentDto,
  ContractorPaymentSummary,
  CompanyStatutoryConfig,
  CreateCompanyStatutoryConfigDto,
  UpdateCompanyStatutoryConfigDto,
  TaxSlab,
  ProfessionalTaxSlab,
} from '@/features/payroll/types/payroll';

/**
 * Payroll API service following SRP - handles all payroll-related API calls
 */
export class PayrollService {
  private readonly payrollEndpoint = 'payroll';
  private readonly contractorEndpoint = 'contractorpayments';
  private readonly salaryStructureEndpoint = 'payroll/salary-structures';
  private readonly taxDeclarationEndpoint = 'payroll/tax-declarations';
  private readonly statutoryConfigEndpoint = 'payroll/statutory-configs';
  private readonly taxConfigEndpoint = 'payroll/tax-config';

  // ==================== Employee Payroll Info ====================

  async getPayrollInfoByEmployeeId(employeeId: string): Promise<any> {
    return apiClient.get<any>(`${this.payrollEndpoint}/payroll-info/employee/${employeeId}`);
  }

  async getPayrollInfoByType(payrollType: 'employee' | 'contractor'): Promise<any[]> {
    return apiClient.get<any[]>(`${this.payrollEndpoint}/payroll-info/by-type/${payrollType}`);
  }

  async createOrUpdatePayrollInfo(data: {
    employeeId: string;
    companyId: string;
    payrollType: 'employee' | 'contractor';
    taxRegime?: string;
    isPfApplicable?: boolean;
    isEsiApplicable?: boolean;
    isPtApplicable?: boolean;
    dateOfJoining?: string;
  }): Promise<any> {
    return apiClient.post<any, any>(`${this.payrollEndpoint}/payroll-info`, data);
  }

  // ==================== Payroll Runs ====================

  async getPayrollRuns(params: PaginationParams & {
    companyId?: string;
    payrollMonth?: number;
    payrollYear?: number;
    financialYear?: string;
    status?: string;
  } = {}): Promise<PagedResponse<PayrollRun>> {
    return apiClient.getPaged<PayrollRun>(`${this.payrollEndpoint}/runs`, params);
  }

  async getPayrollRunById(id: string): Promise<PayrollRun> {
    return apiClient.get<PayrollRun>(`${this.payrollEndpoint}/runs/${id}`);
  }

  async createPayrollRun(data: CreatePayrollRunDto): Promise<PayrollRun> {
    return apiClient.post<PayrollRun, CreatePayrollRunDto>(`${this.payrollEndpoint}/runs`, data);
  }

  async processPayroll(data: ProcessPayrollDto): Promise<PayrollRunSummary> {
    return apiClient.post<PayrollRunSummary, ProcessPayrollDto>(`${this.payrollEndpoint}/process`, data);
  }

  async getPayrollPreview(params: {
    companyId: string;
    payrollMonth: number;
    payrollYear: number;
    includeContractors?: boolean;
  }): Promise<PayrollPreview> {
    return apiClient.get<PayrollPreview>(`${this.payrollEndpoint}/preview`, params);
  }

  async approvePayrollRun(id: string, approvedBy?: string): Promise<void> {
    const url = approvedBy
      ? `${this.payrollEndpoint}/runs/${id}/approve?approvedBy=${encodeURIComponent(approvedBy)}`
      : `${this.payrollEndpoint}/runs/${id}/approve`;
    return apiClient.post<void>(url, {});
  }

  async markPayrollAsPaid(id: string, data: UpdatePayrollRunDto): Promise<void> {
    return apiClient.post<void, UpdatePayrollRunDto>(`${this.payrollEndpoint}/runs/${id}/pay`, data);
  }

  async getPayrollRunSummary(id: string): Promise<PayrollRunSummary> {
    return apiClient.get<PayrollRunSummary>(`${this.payrollEndpoint}/runs/${id}/summary`);
  }

  // ==================== Payroll Transactions ====================

  async getPayrollTransactions(params: PaginationParams & {
    payrollRunId?: string;
    companyId?: string;
    employeeId?: string;
    payrollMonth?: number;
    payrollYear?: number;
    status?: string;
  } = {}): Promise<PagedResponse<PayrollTransaction>> {
    return apiClient.getPaged<PayrollTransaction>(`${this.payrollEndpoint}/transactions`, params);
  }

  async getPayrollTransactionById(id: string): Promise<PayrollTransaction> {
    return apiClient.get<PayrollTransaction>(`${this.payrollEndpoint}/transactions/${id}`);
  }

  async overrideTds(transactionId: string, data: TdsOverrideDto): Promise<void> {
    return apiClient.post<void, TdsOverrideDto>(`${this.payrollEndpoint}/transactions/${transactionId}/tds-override`, data);
  }

  // ==================== Salary Structures ====================

  async getSalaryStructures(params: PaginationParams & {
    companyId?: string;
    employeeId?: string;
    isActive?: boolean;
  } = {}): Promise<PagedResponse<EmployeeSalaryStructure>> {
    return apiClient.getPaged<EmployeeSalaryStructure>(this.salaryStructureEndpoint, params);
  }

  async getSalaryStructureById(id: string): Promise<EmployeeSalaryStructure> {
    return apiClient.get<EmployeeSalaryStructure>(`${this.salaryStructureEndpoint}/${id}`);
  }

  async getCurrentSalaryStructureByEmployeeId(employeeId: string): Promise<EmployeeSalaryStructure> {
    return apiClient.get<EmployeeSalaryStructure>(`${this.salaryStructureEndpoint}/employee/${employeeId}`);
  }

  async getSalaryStructureHistory(employeeId: string): Promise<EmployeeSalaryStructure[]> {
    return apiClient.get<EmployeeSalaryStructure[]>(`${this.salaryStructureEndpoint}/employee/${employeeId}/history`);
  }

  async getEffectiveSalaryStructure(employeeId: string, asOfDate: string): Promise<EmployeeSalaryStructure> {
    return apiClient.get<EmployeeSalaryStructure>(`${this.salaryStructureEndpoint}/employee/${employeeId}/effective`, { asOfDate });
  }

  async getSalaryBreakdown(id: string): Promise<SalaryBreakdown> {
    return apiClient.get<SalaryBreakdown>(`${this.salaryStructureEndpoint}/${id}/breakdown`);
  }

  async createSalaryStructure(data: CreateEmployeeSalaryStructureDto): Promise<EmployeeSalaryStructure> {
    return apiClient.post<EmployeeSalaryStructure, CreateEmployeeSalaryStructureDto>(this.salaryStructureEndpoint, data);
  }

  async updateSalaryStructure(id: string, data: UpdateEmployeeSalaryStructureDto): Promise<void> {
    return apiClient.put<void, UpdateEmployeeSalaryStructureDto>(`${this.salaryStructureEndpoint}/${id}`, data);
  }

  async deleteSalaryStructure(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.salaryStructureEndpoint}/${id}`);
  }

  // ==================== Tax Declarations ====================

  async getTaxDeclarations(params: PaginationParams & {
    companyId?: string;
    employeeId?: string;
    financialYear?: string;
    status?: string;
    taxRegime?: string;
  } = {}): Promise<PagedResponse<EmployeeTaxDeclaration>> {
    return apiClient.getPaged<EmployeeTaxDeclaration>(this.taxDeclarationEndpoint, params);
  }

  async getTaxDeclarationById(id: string): Promise<EmployeeTaxDeclaration> {
    return apiClient.get<EmployeeTaxDeclaration>(`${this.taxDeclarationEndpoint}/${id}`);
  }

  async getTaxDeclarationByEmployeeId(employeeId: string, financialYear?: string): Promise<EmployeeTaxDeclaration | EmployeeTaxDeclaration[]> {
    const url = financialYear
      ? `${this.taxDeclarationEndpoint}/employee/${employeeId}?financialYear=${financialYear}`
      : `${this.taxDeclarationEndpoint}/employee/${employeeId}`;
    return apiClient.get<EmployeeTaxDeclaration | EmployeeTaxDeclaration[]>(url);
  }

  async getPendingVerifications(financialYear?: string): Promise<EmployeeTaxDeclaration[]> {
    const url = financialYear
      ? `${this.taxDeclarationEndpoint}/pending-verification?financialYear=${financialYear}`
      : `${this.taxDeclarationEndpoint}/pending-verification`;
    return apiClient.get<EmployeeTaxDeclaration[]>(url);
  }

  async createTaxDeclaration(data: CreateEmployeeTaxDeclarationDto): Promise<EmployeeTaxDeclaration> {
    return apiClient.post<EmployeeTaxDeclaration, CreateEmployeeTaxDeclarationDto>(this.taxDeclarationEndpoint, data);
  }

  async updateTaxDeclaration(id: string, data: UpdateEmployeeTaxDeclarationDto): Promise<void> {
    return apiClient.put<void, UpdateEmployeeTaxDeclarationDto>(`${this.taxDeclarationEndpoint}/${id}`, data);
  }

  async submitTaxDeclaration(id: string): Promise<void> {
    return apiClient.post<void>(`${this.taxDeclarationEndpoint}/${id}/submit`, {});
  }

  async verifyTaxDeclaration(id: string, verifiedBy?: string): Promise<void> {
    const url = verifiedBy
      ? `${this.taxDeclarationEndpoint}/${id}/verify?verifiedBy=${encodeURIComponent(verifiedBy)}`
      : `${this.taxDeclarationEndpoint}/${id}/verify`;
    return apiClient.post<void>(url, {});
  }

  async lockTaxDeclarations(financialYear: string): Promise<void> {
    return apiClient.post<void>(`${this.taxDeclarationEndpoint}/lock?financialYear=${encodeURIComponent(financialYear)}`, {});
  }

  async deleteTaxDeclaration(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.taxDeclarationEndpoint}/${id}`);
  }

  // ==================== Contractor Payments ====================

  async getContractorPayments(params: PaginationParams & {
    companyId?: string;
    employeeId?: string;
    paymentMonth?: number;
    paymentYear?: number;
    status?: string;
  } = {}): Promise<PagedResponse<ContractorPayment>> {
    return apiClient.getPaged<ContractorPayment>(this.contractorEndpoint, params);
  }

  async getContractorPaymentById(id: string): Promise<ContractorPayment> {
    return apiClient.get<ContractorPayment>(`${this.contractorEndpoint}/${id}`);
  }

  async createContractorPayment(data: CreateContractorPaymentDto): Promise<ContractorPayment> {
    return apiClient.post<ContractorPayment, CreateContractorPaymentDto>(this.contractorEndpoint, data);
  }

  async updateContractorPayment(id: string, data: UpdateContractorPaymentDto): Promise<void> {
    return apiClient.put<void, UpdateContractorPaymentDto>(`${this.contractorEndpoint}/${id}`, data);
  }

  async deleteContractorPayment(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.contractorEndpoint}/${id}`);
  }

  async approveContractorPayment(id: string): Promise<void> {
    return apiClient.post<void>(`${this.contractorEndpoint}/${id}/approve`, {});
  }

  async markContractorPaymentAsPaid(id: string, data: UpdateContractorPaymentDto): Promise<void> {
    return apiClient.post<void, UpdateContractorPaymentDto>(`${this.contractorEndpoint}/${id}/pay`, data);
  }

  async getContractorPaymentSummary(paymentMonth: number, paymentYear: number, companyId?: string): Promise<Record<string, number>> {
    const url = companyId
      ? `${this.contractorEndpoint}/summary?paymentMonth=${paymentMonth}&paymentYear=${paymentYear}&companyId=${companyId}`
      : `${this.contractorEndpoint}/summary?paymentMonth=${paymentMonth}&paymentYear=${paymentYear}`;
    return apiClient.get<Record<string, number>>(url);
  }

  async getContractorYtdSummary(employeeId: string, financialYear: string): Promise<ContractorPaymentSummary> {
    return apiClient.get<ContractorPaymentSummary>(`${this.contractorEndpoint}/ytd/${employeeId}`, { financialYear });
  }

  // ==================== Statutory Config ====================

  async getStatutoryConfigs(params: PaginationParams & {
    companyId?: string;
    isActive?: boolean;
    pfEnabled?: boolean;
    esiEnabled?: boolean;
    ptEnabled?: boolean;
  } = {}): Promise<PagedResponse<CompanyStatutoryConfig>> {
    return apiClient.getPaged<CompanyStatutoryConfig>(this.statutoryConfigEndpoint, params);
  }

  async getStatutoryConfigById(id: string): Promise<CompanyStatutoryConfig> {
    return apiClient.get<CompanyStatutoryConfig>(`${this.statutoryConfigEndpoint}/${id}`);
  }

  async getStatutoryConfigByCompanyId(companyId: string): Promise<CompanyStatutoryConfig | null> {
    try {
      return await apiClient.get<CompanyStatutoryConfig>(`${this.statutoryConfigEndpoint}/company/${companyId}`);
    } catch (error: any) {
      // Return null for 404 (config doesn't exist yet) - this is expected
      if (error?.type === 'NotFound' || error?.response?.status === 404) {
        return null;
      }
      throw error;
    }
  }

  async getActiveStatutoryConfigs(): Promise<CompanyStatutoryConfig[]> {
    return apiClient.get<CompanyStatutoryConfig[]>(`${this.statutoryConfigEndpoint}/active`);
  }

  async createStatutoryConfig(data: CreateCompanyStatutoryConfigDto): Promise<CompanyStatutoryConfig> {
    return apiClient.post<CompanyStatutoryConfig, CreateCompanyStatutoryConfigDto>(this.statutoryConfigEndpoint, data);
  }

  async updateStatutoryConfig(id: string, data: UpdateCompanyStatutoryConfigDto): Promise<void> {
    return apiClient.put<void, UpdateCompanyStatutoryConfigDto>(`${this.statutoryConfigEndpoint}/${id}`, data);
  }

  async deleteStatutoryConfig(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.statutoryConfigEndpoint}/${id}`);
  }

  // ==================== Tax Configuration ====================

  async getTaxSlabs(regime?: string, financialYear?: string): Promise<TaxSlab[]> {
    const params: Record<string, string> = {};
    if (regime) params.regime = regime;
    if (financialYear) params.financialYear = financialYear;
    return apiClient.get<TaxSlab[]>(`${this.taxConfigEndpoint}/tax-slabs`, Object.keys(params).length > 0 ? params : undefined);
  }

  async getTaxSlabForIncome(income: number, regime: string = 'new', financialYear: string = '2024-25'): Promise<TaxSlab> {
    return apiClient.get<TaxSlab>(`${this.taxConfigEndpoint}/tax-slabs/for-income`, { income, regime, financialYear });
  }

  async getProfessionalTaxSlabs(state?: string): Promise<ProfessionalTaxSlab[]> {
    const url = state
      ? `${this.taxConfigEndpoint}/professional-tax-slabs?state=${state}`
      : `${this.taxConfigEndpoint}/professional-tax-slabs`;
    return apiClient.get<ProfessionalTaxSlab[]>(url);
  }

  async getProfessionalTaxSlabForIncome(monthlyIncome: number, state: string = 'Karnataka'): Promise<ProfessionalTaxSlab> {
    return apiClient.get<ProfessionalTaxSlab>(`${this.taxConfigEndpoint}/professional-tax-slabs/for-income`, { monthlyIncome, state });
  }
}

// Singleton instance
export const payrollService = new PayrollService();




