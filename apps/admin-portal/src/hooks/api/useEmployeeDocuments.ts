import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { employeeDocumentService, CreateEmployeeDocumentDto, UpdateEmployeeDocumentDto, UpdateDocumentRequestDto } from '@/services/api/employeeDocumentService';
import toast from 'react-hot-toast';

export const EMPLOYEE_DOCUMENT_QUERY_KEYS = {
  all: ['employeeDocuments'] as const,
  lists: () => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.all, 'list'] as const,
  list: (companyId?: string, employeeId?: string) => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.lists(), companyId, employeeId] as const,
  companyWide: (companyId: string) => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.all, 'companyWide', companyId] as const,
  details: () => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.details(), id] as const,
  requests: ['documentRequests'] as const,
  pendingRequests: (companyId: string) => [...EMPLOYEE_DOCUMENT_QUERY_KEYS.requests, 'pending', companyId] as const,
} as const;

export const useEmployeeDocuments = (companyId?: string, employeeId?: string) => {
  return useQuery({
    queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.list(companyId, employeeId),
    queryFn: () => employeeDocumentService.getAll(companyId, employeeId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useCompanyWideDocuments = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.companyWide(companyId),
    queryFn: () => employeeDocumentService.getCompanyWide(companyId),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useEmployeeDocument = (id: string, enabled = true) => {
  return useQuery({
    queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.detail(id),
    queryFn: () => employeeDocumentService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateEmployeeDocument = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateEmployeeDocumentDto) => employeeDocumentService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.lists() });
      toast.success('Document uploaded successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to upload document';
      toast.error(message);
    },
  });
};

export const useUpdateEmployeeDocument = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEmployeeDocumentDto }) =>
      employeeDocumentService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.lists() });
      toast.success('Document updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update document';
      toast.error(message);
    },
  });
};

export const useDeleteEmployeeDocument = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => employeeDocumentService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.lists() });
      toast.success('Document deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete document';
      toast.error(message);
    },
  });
};

// Document Requests
export const usePendingDocumentRequests = (companyId: string, enabled = true) => {
  return useQuery({
    queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.pendingRequests(companyId),
    queryFn: () => employeeDocumentService.getPendingRequests(companyId),
    enabled: enabled && !!companyId,
    staleTime: 30 * 1000,
  });
};

export const useProcessDocumentRequest = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateDocumentRequestDto }) =>
      employeeDocumentService.processRequest(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: EMPLOYEE_DOCUMENT_QUERY_KEYS.requests });
      toast.success('Request processed successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to process request';
      toast.error(message);
    },
  });
};
