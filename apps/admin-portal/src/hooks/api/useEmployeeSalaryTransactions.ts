import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { employeeSalaryTransactionService } from '@/services/api/employeeSalaryTransactionService';
import { 
  EmployeeSalaryTransaction, 
  CreateEmployeeSalaryTransactionDto, 
  UpdateEmployeeSalaryTransactionDto,
  BulkEmployeeSalaryTransactionsDto,
  CopySalaryTransactionsDto,
  SalaryTransactionsFilterParams 
} from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys
export const SALARY_TRANSACTION_QUERY_KEYS = {
  all: ['salaryTransactions'] as const,
  lists: () => [...SALARY_TRANSACTION_QUERY_KEYS.all, 'list'] as const,
  list: (filters: string) => [...SALARY_TRANSACTION_QUERY_KEYS.lists(), filters] as const,
  details: () => [...SALARY_TRANSACTION_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...SALARY_TRANSACTION_QUERY_KEYS.details(), id] as const,
  paged: (params: PaginationParams) => [...SALARY_TRANSACTION_QUERY_KEYS.all, 'paged', params] as const,
  byEmployee: (employeeId: string) => [...SALARY_TRANSACTION_QUERY_KEYS.all, 'byEmployee', employeeId] as const,
  byMonth: (month: number, year: number) => [...SALARY_TRANSACTION_QUERY_KEYS.all, 'byMonth', `${year}-${month}`] as const,
  summary: (type: 'monthly' | 'yearly', year: number, month?: number, companyId?: string) => 
    [...SALARY_TRANSACTION_QUERY_KEYS.all, 'summary', type, year, month, companyId || 'all'] as const,
} as const;

/**
 * Get all salary transactions
 */
export const useSalaryTransactions = () => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists(),
    queryFn: () => employeeSalaryTransactionService.getAll(),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get paginated salary transactions
 */
export const useSalaryTransactionsPaged = (params: SalaryTransactionsFilterParams = {}) => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.paged(params),
    queryFn: () => employeeSalaryTransactionService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Get salary transaction by ID
 */
export const useSalaryTransaction = (id: string, enabled = true) => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.detail(id),
    queryFn: () => employeeSalaryTransactionService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get salary transactions by employee ID
 */
export const useSalaryTransactionsByEmployee = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.byEmployee(employeeId),
    queryFn: () => employeeSalaryTransactionService.getByEmployeeId(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get salary transactions by month and year
 */
export const useSalaryTransactionsByMonth = (month: number, year: number, enabled = true) => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.byMonth(month, year),
    queryFn: () => employeeSalaryTransactionService.getByMonthYear(month, year),
    enabled: enabled && !!month && !!year,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get salary transaction by employee and month
 */
export const useSalaryTransactionByEmployeeAndMonth = (
  employeeId: string, 
  month: number, 
  year: number, 
  enabled = true
) => {
  return useQuery({
    queryKey: [...SALARY_TRANSACTION_QUERY_KEYS.all, 'byEmployeeAndMonth', employeeId, `${year}-${month}`],
    queryFn: () => employeeSalaryTransactionService.getByEmployeeAndMonth(employeeId, month, year),
    enabled: enabled && !!employeeId && !!month && !!year,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get monthly salary summary
 */
export const useMonthlySalarySummary = (month: number, year: number, companyId?: string, enabled = true) => {
  return useQuery({
    queryKey: [...SALARY_TRANSACTION_QUERY_KEYS.summary('monthly', year, month), companyId || 'all'],
    queryFn: async () => {
      const result = await employeeSalaryTransactionService.getMonthlySummary(month, year, companyId);
      console.log('Monthly summary result:', { month, year, companyId, result });
      return result;
    },
    enabled: enabled && !!month && !!year,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get yearly salary summary
 */
export const useYearlySalarySummary = (year: number, companyId?: string, enabled = true) => {
  return useQuery({
    queryKey: SALARY_TRANSACTION_QUERY_KEYS.summary('yearly', year, undefined, companyId),
    queryFn: () => employeeSalaryTransactionService.getYearlySummary(year, companyId),
    enabled: enabled && !!year,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Create salary transaction mutation
 */
export const useCreateSalaryTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEmployeeSalaryTransactionDto) => 
      employeeSalaryTransactionService.create(data),
    onSuccess: (newTransaction) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      queryClient.invalidateQueries({ 
        queryKey: SALARY_TRANSACTION_QUERY_KEYS.byEmployee(newTransaction.employeeId) 
      });
      queryClient.invalidateQueries({ 
        queryKey: SALARY_TRANSACTION_QUERY_KEYS.byMonth(newTransaction.salaryMonth, newTransaction.salaryYear) 
      });
      
      toast.success('Salary transaction created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create salary transaction';
      toast.error(message);
    },
  });
};

/**
 * Update salary transaction mutation
 */
export const useUpdateSalaryTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeSalaryTransactionDto }) =>
      employeeSalaryTransactionService.update(id, data),
    onSuccess: (_, { id }) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      
      toast.success('Salary transaction updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update salary transaction';
      toast.error(message);
    },
  });
};

/**
 * Delete salary transaction mutation
 */
export const useDeleteSalaryTransaction = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => employeeSalaryTransactionService.delete(id),
    onSuccess: () => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      
      toast.success('Salary transaction deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete salary transaction';
      toast.error(message);
    },
  });
};

/**
 * Bulk create salary transactions mutation
 */
export const useBulkCreateSalaryTransactions = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkEmployeeSalaryTransactionsDto) => 
      employeeSalaryTransactionService.bulkCreate(data),
    onSuccess: (result) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      
      if (result.successCount > 0) {
        toast.success(`${result.successCount} salary transactions created successfully!`);
      }
      if (result.failureCount > 0) {
        toast.error(`${result.failureCount} transactions failed to create. Check the results for details.`);
      }
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to bulk create salary transactions';
      toast.error(message);
    },
  });
};

/**
 * Check if salary record exists for employee and month
 */
export const useCheckSalaryRecordExists = (
  employeeId: string,
  month: number,
  year: number,
  excludeId?: string
) => {
  return useQuery({
    queryKey: ['salaryTransactions', 'checkExists', employeeId, `${year}-${month}`, excludeId],
    queryFn: () => employeeSalaryTransactionService.checkSalaryRecordExists(employeeId, month, year, excludeId),
    enabled: !!employeeId && !!month && !!year,
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Bulk upload salary transactions from file
 */
export const useBulkUploadSalaryTransactions = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => employeeSalaryTransactionService.bulkUpload(file),
    onSuccess: (result) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      
      if (result.successCount > 0) {
        toast.success(`${result.successCount} salary transactions uploaded successfully!`);
      }
      if (result.errorCount > 0) {
        toast.error(`${result.errorCount} transactions had errors. Check the results for details.`, {
          duration: 5000,
        });
      }
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to upload salary transactions';
      toast.error(message);
    },
  });
};

/**
 * Copy salary transactions from one period to another
 */
export const useCopySalaryTransactions = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CopySalaryTransactionsDto) => employeeSalaryTransactionService.copyTransactions(data),
    onSuccess: (result) => {
      // Invalidate relevant queries
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: SALARY_TRANSACTION_QUERY_KEYS.all });
      
      if (result.successCount > 0) {
        toast.success(`Successfully copied ${result.successCount} salary transaction(s)!`);
      }
      if (result.failureCount > 0) {
        toast.error(`${result.failureCount} transaction(s) could not be copied. Check the results for details.`, {
          duration: 5000,
        });
      }
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to copy salary transactions';
      toast.error(message);
    },
  });
};
