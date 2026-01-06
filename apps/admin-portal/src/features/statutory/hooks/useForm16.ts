import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { form16Service } from '@/services/api/finance/statutory';
import { statutoryKeys } from './statutoryKeys';
import type { Form16FilterParams, GenerateForm16Request } from '@/services/api/types';

/**
 * Hook to get paginated Form 16 list for a company
 */
export const useForm16List = (companyId: string, params: Form16FilterParams = {}) => {
  return useQuery({
    queryKey: statutoryKeys.form16.list({ companyId, ...params }),
    queryFn: () => form16Service.getPaged(companyId, params),
    enabled: !!companyId,
    staleTime: 5 * 60 * 1000,
    gcTime: 10 * 60 * 1000,
  });
};

/**
 * Hook to get Form 16 by ID
 */
export const useForm16 = (id: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.form16.detail(id),
    queryFn: () => form16Service.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get Form 16 for employee and financial year
 */
export const useForm16ByEmployee = (
  companyId: string,
  employeeId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: statutoryKeys.form16.byEmployee(employeeId, financialYear),
    queryFn: () => form16Service.getByEmployeeAndFY(companyId, employeeId, financialYear),
    enabled: enabled && !!companyId && !!employeeId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to get Form 16 summary/statistics for company and financial year
 */
export const useForm16Summary = (companyId: string, financialYear: string, enabled = true) => {
  return useQuery({
    queryKey: statutoryKeys.form16.summary(companyId, financialYear),
    queryFn: () => form16Service.getSummary(companyId, financialYear),
    enabled: enabled && !!companyId && !!financialYear,
    staleTime: 5 * 60 * 1000,
  });
};

/**
 * Hook to preview Form 16 data
 */
export const useForm16Preview = (
  companyId: string,
  employeeId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ['form16', 'preview', companyId, employeeId, financialYear],
    queryFn: () => form16Service.preview(companyId, employeeId, financialYear),
    enabled: enabled && !!companyId && !!employeeId && !!financialYear,
    staleTime: 2 * 60 * 1000,
  });
};

/**
 * Hook to generate Form 16 (bulk)
 */
export const useGenerateForm16 = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: GenerateForm16Request) => form16Service.generate(request),
    onSuccess: (result, { companyId }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.all });
      if (result.successCount > 0) {
        toast.success(`Generated Form 16 for ${result.successCount} employees`);
      }
      if (result.failedCount > 0) {
        toast.error(`Failed to generate for ${result.failedCount} employees`);
      }
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate Form 16');
    },
  });
};

/**
 * Hook to generate Form 16 for a single employee
 */
export const useGenerateForm16ForEmployee = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      companyId,
      employeeId,
      financialYear,
    }: {
      companyId: string;
      employeeId: string;
      financialYear: string;
    }) => form16Service.generateForEmployee(companyId, employeeId, financialYear),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.all });
      toast.success('Form 16 generated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate Form 16');
    },
  });
};

/**
 * Hook to regenerate Form 16
 */
export const useRegenerateForm16 = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => form16Service.regenerate(id),
    onSuccess: (data, id) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.lists() });
      toast.success('Form 16 regenerated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to regenerate Form 16');
    },
  });
};

/**
 * Hook to verify Form 16
 */
export const useVerifyForm16 = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, verifiedBy, place }: { id: string; verifiedBy: string; place: string }) =>
      form16Service.verify(id, verifiedBy, place),
    onSuccess: (data, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.lists() });
      toast.success('Form 16 verified successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to verify Form 16');
    },
  });
};

/**
 * Hook to mark Form 16 as issued
 */
export const useIssueForm16 = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => form16Service.markAsIssued(id),
    onSuccess: (data, id) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.lists() });
      toast.success('Form 16 marked as issued');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to issue Form 16');
    },
  });
};

/**
 * Hook to cancel Form 16
 */
export const useCancelForm16 = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason, cancelledBy }: { id: string; reason: string; cancelledBy: string }) =>
      form16Service.cancel(id, reason, cancelledBy),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.detail(id) });
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.lists() });
      toast.success('Form 16 cancelled');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to cancel Form 16');
    },
  });
};

/**
 * Hook to generate PDF for Form 16
 */
export const useGenerateForm16Pdf = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => form16Service.generatePdf(id),
    onSuccess: (data, id) => {
      queryClient.invalidateQueries({ queryKey: statutoryKeys.form16.detail(id) });
      toast.success('PDF generated successfully');
    },
    onError: (error: any) => {
      toast.error(error?.message ?? 'Failed to generate PDF');
    },
  });
};

/**
 * Hook to validate Form 16 generation
 */
export const useValidateForm16 = (
  companyId: string,
  employeeId: string,
  financialYear: string,
  enabled = true
) => {
  return useQuery({
    queryKey: ['form16', 'validate', companyId, employeeId, financialYear],
    queryFn: () => form16Service.validate(companyId, employeeId, financialYear),
    enabled: enabled && !!companyId && !!employeeId && !!financialYear,
    staleTime: 1 * 60 * 1000,
  });
};
