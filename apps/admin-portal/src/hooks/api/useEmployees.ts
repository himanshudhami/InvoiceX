import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { employeeService } from '@/services/api/hr/employees/employeeService';
import { Employee, CreateEmployeeDto, UpdateEmployeeDto, EmployeesFilterParams, BulkEmployeesDto, ResignEmployeeDto, RejoinEmployeeDto, PaginationParams } from '@/services/api/types';
import toast from 'react-hot-toast';

// Query keys
export const EMPLOYEE_QUERY_KEYS = {
  all: ['employees'] as const,
  lists: () => [...EMPLOYEE_QUERY_KEYS.all, 'list'] as const,
  listByCompany: (companyId?: string) => [...EMPLOYEE_QUERY_KEYS.lists(), companyId ?? 'default'] as const,
  list: (filters: string) => [...EMPLOYEE_QUERY_KEYS.lists(), filters] as const,
  details: () => [...EMPLOYEE_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...EMPLOYEE_QUERY_KEYS.details(), id] as const,
  paged: (params: PaginationParams) => [...EMPLOYEE_QUERY_KEYS.all, 'paged', params] as const,
} as const;

/**
 * Get all employees
 * @param companyId Optional company ID to filter by (for multi-company users)
 */
export const useEmployees = (companyId?: string) => {
  return useQuery({
    queryKey: EMPLOYEE_QUERY_KEYS.listByCompany(companyId),
    queryFn: () => employeeService.getAll(companyId),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get paginated employees
 */
export const useEmployeesPaged = (params: EmployeesFilterParams = {}) => {
  return useQuery({
    queryKey: EMPLOYEE_QUERY_KEYS.paged(params),
    queryFn: () => employeeService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Get employee by ID
 */
export const useEmployee = (id: string, enabled = true) => {
  return useQuery({
    queryKey: EMPLOYEE_QUERY_KEYS.detail(id),
    queryFn: () => employeeService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Get employee by Employee ID
 */
export const useEmployeeByEmployeeId = (employeeId: string, enabled = true) => {
  return useQuery({
    queryKey: [...EMPLOYEE_QUERY_KEYS.all, 'byEmployeeId', employeeId],
    queryFn: () => employeeService.getByEmployeeId(employeeId),
    enabled: enabled && !!employeeId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

/**
 * Create employee mutation
 */
export const useCreateEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEmployeeDto) => employeeService.create(data),
    onSuccess: (newEmployee) => {
      // Invalidate and refetch employee lists
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });
      
      toast.success('Employee created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create employee';
      toast.error(message);
    },
  });
};

/**
 * Update employee mutation
 */
export const useUpdateEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeDto }) =>
      employeeService.update(id, data),
    onSuccess: (_, { id }) => {
      // Invalidate and refetch employee data
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });
      
      toast.success('Employee updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update employee';
      toast.error(message);
    },
  });
};

/**
 * Delete employee mutation
 */
export const useDeleteEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => employeeService.delete(id),
    onSuccess: () => {
      // Invalidate and refetch employee lists
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });
      
      toast.success('Employee deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete employee';
      toast.error(message);
    },
  });
};

/**
 * Bulk create employees
 */
export const useBulkCreateEmployees = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkEmployeesDto) => employeeService.bulkCreate(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });

      if (result.successCount > 0) {
        toast.success(`${result.successCount} employees created successfully!`);
      }
      if (result.failureCount > 0) {
        toast.error(`${result.failureCount} employees failed to import. Check details in the modal.`);
      }
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to bulk create employees';
      toast.error(message);
    },
  });
};
/**
 * Check if employee ID is unique
 */
export const useCheckEmployeeIdUnique = (employeeId: string, excludeId?: string) => {
  return useQuery({
    queryKey: ['employees', 'checkEmployeeIdUnique', employeeId, excludeId],
    queryFn: () => employeeService.checkEmployeeIdUnique(employeeId, excludeId),
    enabled: !!employeeId && employeeId.length > 0,
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Check if email is unique
 */
export const useCheckEmailUnique = (email: string, excludeId?: string) => {
  return useQuery({
    queryKey: ['employees', 'checkEmailUnique', email, excludeId],
    queryFn: () => employeeService.checkEmailUnique(email, excludeId),
    enabled: !!email && email.length > 0 && email.includes('@'),
    staleTime: 30 * 1000, // 30 seconds
  });
};

/**
 * Resign employee mutation
 */
export const useResignEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ResignEmployeeDto }) =>
      employeeService.resign(id, data),
    onSuccess: (_, { id }) => {
      // Invalidate and refetch employee data
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });

      toast.success('Employee resigned successfully');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to resign employee';
      toast.error(message);
    },
  });
};

/**
 * Rejoin employee mutation
 */
export const useRejoinEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data?: RejoinEmployeeDto }) =>
      employeeService.rejoin(id, data),
    onSuccess: (_, { id }) => {
      // Invalidate and refetch employee data
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.lists() });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_QUERY_KEYS.all });

      toast.success('Employee rejoined successfully');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to rejoin employee';
      toast.error(message);
    },
  });
};
