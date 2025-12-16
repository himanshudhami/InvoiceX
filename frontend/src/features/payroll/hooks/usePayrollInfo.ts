import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { payrollService } from '@/services/api/payrollService';
import { payrollKeys } from './payrollKeys';

export interface CreateEmployeePayrollInfoDto {
  employeeId: string;
  companyId: string;
  payrollType: 'employee' | 'contractor';
  taxRegime?: string;
  isPfApplicable?: boolean;
  isEsiApplicable?: boolean;
  isPtApplicable?: boolean;
  dateOfJoining?: string;
  // Statutory identifiers
  uan?: string;
  pfAccountNumber?: string;
  esiNumber?: string;
}

export const usePayrollInfo = (employeeId: string, enabled: boolean = true) => {
  return useQuery({
    queryKey: [...payrollKeys.all, 'payroll-info', employeeId],
    queryFn: () => payrollService.getPayrollInfoByEmployeeId(employeeId),
    enabled: enabled && !!employeeId,
  });
};

export const useCreateOrUpdatePayrollInfo = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEmployeePayrollInfoDto) =>
      payrollService.createOrUpdatePayrollInfo(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({
        queryKey: [...payrollKeys.all, 'payroll-info', variables.employeeId],
      });
      queryClient.invalidateQueries({
        queryKey: [...payrollKeys.all, 'payroll-info', 'by-type'],
      });
    },
  });
};

export const usePayrollInfoByType = (payrollType: 'employee' | 'contractor') => {
  return useQuery({
    queryKey: [...payrollKeys.all, 'payroll-info', 'by-type', payrollType],
    queryFn: () => payrollService.getPayrollInfoByType(payrollType),
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};


