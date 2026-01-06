// Employee types
import type { PaginationParams } from './common';

export interface Employee {
  id: string;
  employeeName: string;
  email?: string;
  phone?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  hireDate?: string;
  status: string;
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  panNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country: string;
  contractType?: string;
  company?: string;
  companyId?: string;
  managerId?: string;
  managerName?: string;
  reportingLevel?: number;
  isManager?: boolean;
  createdAt?: string;
  updatedAt?: string;
  uan?: string;
  pfAccountNumber?: string;
  esiNumber?: string;
}

export interface CreateEmployeeDto {
  employeeName: string;
  email?: string;
  phone?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  hireDate?: string;
  status?: string;
  bankAccountNumber?: string;
  bankName?: string;
  ifscCode?: string;
  panNumber?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  country?: string;
  contractType?: string;
  company?: string;
  companyId?: string;
  managerId?: string;
}

export interface UpdateEmployeeDto extends CreateEmployeeDto {}

export interface ResignEmployeeDto {
  lastWorkingDay: string;
  resignationReason?: string;
}

export interface RejoinEmployeeDto {
  rejoiningDate?: string;
}

export interface EmployeeSalaryTransaction {
  id: string;
  employeeId: string;
  companyId?: string;
  salaryMonth: number;
  salaryYear: number;
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod: string;
  paymentReference?: string;
  status: string;
  remarks?: string;
  currency: string;
  transactionType?: string;
  createdAt?: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
  employee?: Employee;
}

export interface CreateEmployeeSalaryTransactionDto {
  employeeId: string;
  companyId?: string;
  salaryMonth: number;
  salaryYear: number;
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  status?: string;
  remarks?: string;
  currency?: string;
  transactionType?: string;
  createdBy?: string;
}

export interface UpdateEmployeeSalaryTransactionDto {
  basicSalary: number;
  hra: number;
  conveyance: number;
  medicalAllowance: number;
  specialAllowance: number;
  lta: number;
  otherAllowances: number;
  grossSalary: number;
  pfEmployee: number;
  pfEmployer: number;
  pt: number;
  incomeTax: number;
  otherDeductions: number;
  netSalary: number;
  paymentDate?: string;
  paymentMethod?: string;
  paymentReference?: string;
  status?: string;
  remarks?: string;
  currency?: string;
  transactionType?: string;
  updatedBy?: string;
}

export interface BulkEmployeeSalaryTransactionsDto {
  salaryTransactions: CreateEmployeeSalaryTransactionDto[];
  skipValidationErrors?: boolean;
  overwriteExisting?: boolean;
  createdBy?: string;
}

export interface CopySalaryTransactionsDto {
  sourceMonth: number;
  sourceYear: number;
  targetMonth: number;
  targetYear: number;
  companyId?: string;
  duplicateHandling?: 'skip' | 'overwrite' | 'skip_and_report';
  resetPaymentInfo?: boolean;
  createdBy?: string;
}

export interface BulkEmployeesDto {
  employees: CreateEmployeeDto[];
  skipValidationErrors?: boolean;
  createdBy?: string;
}

export interface BulkUploadResult {
  successCount: number;
  failureCount: number;
  totalCount: number;
  errors: BulkUploadError[];
  createdIds: string[];
}

export interface BulkUploadError {
  rowNumber: number;
  employeeReference?: string;
  assetReference?: string;
  errorMessage: string;
  fieldName?: string;
}

export interface EmployeesFilterParams extends PaginationParams {
  employeeName?: string;
  employeeId?: string;
  department?: string;
  designation?: string;
  status?: string;
  city?: string;
  state?: string;
  country?: string;
  contractType?: string;
  company?: string;
  companyId?: string;
}

export interface SalaryTransactionsFilterParams extends PaginationParams {
  employeeId?: string;
  salaryMonth?: number;
  salaryYear?: number;
  status?: string;
  transactionType?: string;
  contractType?: string;
  department?: string;
  paymentMethod?: string;
  paymentDateFrom?: string;
  paymentDateTo?: string;
  company?: string;
  companyId?: string;
}
