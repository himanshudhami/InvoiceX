import { apiClient } from '../../client';
import { 
  EmployeeSalaryTransaction, 
  CreateEmployeeSalaryTransactionDto, 
  UpdateEmployeeSalaryTransactionDto,
  BulkEmployeeSalaryTransactionsDto,
  CopySalaryTransactionsDto,
  BulkUploadResult,
  PagedResponse, 
  SalaryTransactionsFilterParams 
} from '../../types';

/**
 * Employee Salary Transaction API service
 */
export class EmployeeSalaryTransactionService {
  private readonly endpoint = 'employeesalarytransactions';

  async getAll(): Promise<EmployeeSalaryTransaction[]> {
    return apiClient.get<EmployeeSalaryTransaction[]>(this.endpoint);
  }

  async getById(id: string): Promise<EmployeeSalaryTransaction> {
    return apiClient.get<EmployeeSalaryTransaction>(`${this.endpoint}/${id}`);
  }

  async getByEmployeeAndMonth(
    employeeId: string, 
    salaryMonth: number, 
    salaryYear: number
  ): Promise<EmployeeSalaryTransaction> {
    const params = new URLSearchParams({
      employeeId,
      salaryMonth: salaryMonth.toString(),
      salaryYear: salaryYear.toString()
    });
    return apiClient.get<EmployeeSalaryTransaction>(`${this.endpoint}/by-employee-month?${params}`);
  }

  async getByEmployeeId(employeeId: string): Promise<EmployeeSalaryTransaction[]> {
    return apiClient.get<EmployeeSalaryTransaction[]>(`${this.endpoint}/by-employee/${employeeId}`);
  }

  async getByMonthYear(salaryMonth: number, salaryYear: number): Promise<EmployeeSalaryTransaction[]> {
    const params = new URLSearchParams({
      salaryMonth: salaryMonth.toString(),
      salaryYear: salaryYear.toString()
    });
    return apiClient.get<EmployeeSalaryTransaction[]>(`${this.endpoint}/by-month?${params}`);
  }

  async getPaged(params: SalaryTransactionsFilterParams = {}): Promise<PagedResponse<EmployeeSalaryTransaction>> {
    // apiClient.getPaged appends /paged, so pass the base endpoint to avoid /paged/paged
    return apiClient.getPaged<EmployeeSalaryTransaction>(this.endpoint, params);
  }

  async create(data: CreateEmployeeSalaryTransactionDto): Promise<EmployeeSalaryTransaction> {
    return apiClient.post<EmployeeSalaryTransaction, CreateEmployeeSalaryTransactionDto>(this.endpoint, data);
  }

  async update(id: string, data: UpdateEmployeeSalaryTransactionDto): Promise<void> {
    return apiClient.put<void, UpdateEmployeeSalaryTransactionDto>(`${this.endpoint}/${id}`, data);
  }

  async delete(id: string): Promise<void> {
    return apiClient.delete<void>(`${this.endpoint}/${id}`);
  }

  async bulkCreate(data: BulkEmployeeSalaryTransactionsDto): Promise<BulkUploadResult> {
    return apiClient.post<BulkUploadResult, BulkEmployeeSalaryTransactionsDto>(`${this.endpoint}/bulk`, data);
  }

  async copyTransactions(data: CopySalaryTransactionsDto): Promise<BulkUploadResult> {
    return apiClient.post<BulkUploadResult, CopySalaryTransactionsDto>(`${this.endpoint}/copy`, data);
  }

  async getMonthlySummary(salaryMonth: number, salaryYear: number, companyId?: string): Promise<Record<string, number>> {
    const params: Record<string, string> = {
      salaryMonth: salaryMonth.toString(),
      salaryYear: salaryYear.toString()
    };
    if (companyId) {
      params.companyId = companyId;
    }
    return apiClient.get<Record<string, number>>(`${this.endpoint}/summary/monthly`, params);
  }

  async getYearlySummary(salaryYear: number, companyId?: string): Promise<Record<string, number>> {
    const params = new URLSearchParams({
      salaryYear: salaryYear.toString()
    });
    if (companyId) {
      params.append('companyId', companyId);
    }
    return apiClient.get<Record<string, number>>(`${this.endpoint}/summary/yearly?${params}`);
  }

  async checkSalaryRecordExists(
    employeeId: string,
    salaryMonth: number,
    salaryYear: number,
    excludeId?: string
  ): Promise<boolean> {
    const params = new URLSearchParams({
      employeeId,
      salaryMonth: salaryMonth.toString(),
      salaryYear: salaryYear.toString()
    });
    if (excludeId) {
      params.append('excludeId', excludeId);
    }
    return apiClient.get<boolean>(`${this.endpoint}/check-exists?${params}`);
  }

  async bulkUpload(file: File): Promise<{ successCount: number; errorCount: number; errors: Array<{ row: number; errors: string[] }> }> {
    const formData = new FormData();
    formData.append('file', file);

    return apiClient.post<{ successCount: number; errorCount: number; errors: Array<{ row: number; errors: string[] }> }>(`${this.endpoint}/bulk-upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
  }
}

// Singleton instance
export const employeeSalaryTransactionService = new EmployeeSalaryTransactionService();
