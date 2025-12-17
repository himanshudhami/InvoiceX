import type { PaginationParams } from '@/services/api/types'

export const payrollKeys = {
  all: ['payroll'] as const,
  
  // Payroll Runs
  runs: () => [...payrollKeys.all, 'runs'] as const,
  run: (id: string) => [...payrollKeys.runs(), id] as const,
  runList: (params?: PaginationParams & {
    companyId?: string;
    payrollMonth?: number;
    payrollYear?: number;
    status?: string;
  }) => [...payrollKeys.runs(), 'list', params ?? {}] as const,
  runSummary: (id: string) => [...payrollKeys.run(id), 'summary'] as const,
  
  // Payroll Transactions
  transactions: () => [...payrollKeys.all, 'transactions'] as const,
  transaction: (id: string) => [...payrollKeys.transactions(), id] as const,
  transactionList: (params?: PaginationParams & {
    payrollRunId?: string;
    employeeId?: string;
    payrollMonth?: number;
    payrollYear?: number;
    status?: string;
  }) => [...payrollKeys.transactions(), 'list', params ?? {}] as const,
  
  // Salary Structures
  salaryStructures: () => [...payrollKeys.all, 'salary-structures'] as const,
  salaryStructure: (id: string) => [...payrollKeys.salaryStructures(), id] as const,
  salaryStructureList: (params?: PaginationParams & {
    companyId?: string;
    employeeId?: string;
    isActive?: boolean;
  }) => [...payrollKeys.salaryStructures(), 'list', params ?? {}] as const,
  currentSalaryStructure: (employeeId: string) => [...payrollKeys.salaryStructures(), 'current', employeeId] as const,
  salaryStructureHistory: (employeeId: string) => [...payrollKeys.salaryStructures(), 'history', employeeId] as const,
  salaryBreakdown: (id: string) => [...payrollKeys.salaryStructure(id), 'breakdown'] as const,
  
  // Tax Declarations
  taxDeclarations: () => [...payrollKeys.all, 'tax-declarations'] as const,
  taxDeclaration: (id: string) => [...payrollKeys.taxDeclarations(), id] as const,
  taxDeclarationList: (params?: PaginationParams & {
    employeeId?: string;
    financialYear?: string;
    status?: string;
    taxRegime?: string;
  }) => [...payrollKeys.taxDeclarations(), 'list', params ?? {}] as const,
  employeeTaxDeclaration: (employeeId: string, financialYear?: string) => 
    [...payrollKeys.taxDeclarations(), 'employee', employeeId, financialYear ?? 'all'] as const,
  pendingVerifications: (financialYear?: string) => 
    [...payrollKeys.taxDeclarations(), 'pending', financialYear ?? 'all'] as const,
  
  // Contractor Payments
  contractorPayments: () => [...payrollKeys.all, 'contractor-payments'] as const,
  contractorPayment: (id: string) => [...payrollKeys.contractorPayments(), id] as const,
  contractorPaymentList: (params?: PaginationParams & {
    companyId?: string;
    employeeId?: string;
    paymentMonth?: number;
    paymentYear?: number;
    status?: string;
  }) => [...payrollKeys.contractorPayments(), 'list', params ?? {}] as const,
  contractorSummary: (month: number, year: number, companyId?: string) => 
    [...payrollKeys.contractorPayments(), 'summary', month, year, companyId ?? 'all'] as const,
  contractorYtd: (employeeId: string, financialYear: string) => 
    [...payrollKeys.contractorPayments(), 'ytd', employeeId, financialYear] as const,
  
  // Statutory Config
  statutoryConfigs: () => [...payrollKeys.all, 'statutory-configs'] as const,
  statutoryConfig: (id: string) => [...payrollKeys.statutoryConfigs(), id] as const,
  statutoryConfigList: (params?: PaginationParams & {
    companyId?: string;
    isActive?: boolean;
  }) => [...payrollKeys.statutoryConfigs(), 'list', params ?? {}] as const,
  companyStatutoryConfig: (companyId: string) => 
    [...payrollKeys.statutoryConfigs(), 'company', companyId] as const,
  activeStatutoryConfigs: () => [...payrollKeys.statutoryConfigs(), 'active'] as const,
  
  // Tax Configuration
  taxSlabs: (regime?: string, financialYear?: string) =>
    [...payrollKeys.all, 'tax-slabs', regime ?? 'all', financialYear ?? 'all'] as const,
  taxSlabForIncome: (income: number, regime: string, financialYear: string) =>
    [...payrollKeys.taxSlabs(regime, financialYear), 'for-income', income] as const,
  professionalTaxSlabs: (state?: string) =>
    [...payrollKeys.all, 'pt-slabs', state ?? 'all'] as const,
  professionalTaxSlab: (id: string) =>
    [...payrollKeys.all, 'pt-slabs', 'detail', id] as const,
  professionalTaxSlabForIncome: (monthlyIncome: number, state: string) =>
    [...payrollKeys.professionalTaxSlabs(state), 'for-income', monthlyIncome] as const,
  distinctPtStates: () =>
    [...payrollKeys.all, 'pt-slabs', 'states'] as const,
  indianStates: () =>
    [...payrollKeys.all, 'indian-states'] as const,
  noPtStates: () =>
    [...payrollKeys.all, 'no-pt-states'] as const,
}




