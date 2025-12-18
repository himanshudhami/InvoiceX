import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { supportTicketService, UpdateSupportTicketDto, CreateTicketMessageDto, CreateFaqDto, UpdateFaqDto } from '@/services/api/supportTicketService';
import toast from 'react-hot-toast';

export const SUPPORT_TICKET_QUERY_KEYS = {
  all: ['supportTickets'] as const,
  lists: () => [...SUPPORT_TICKET_QUERY_KEYS.all, 'list'] as const,
  list: (companyId?: string, status?: string) => [...SUPPORT_TICKET_QUERY_KEYS.lists(), companyId, status] as const,
  details: () => [...SUPPORT_TICKET_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...SUPPORT_TICKET_QUERY_KEYS.details(), id] as const,
  faq: ['faq'] as const,
  faqList: (companyId?: string) => [...SUPPORT_TICKET_QUERY_KEYS.faq, 'list', companyId] as const,
} as const;

export const useSupportTickets = (companyId?: string, status?: string) => {
  return useQuery({
    queryKey: SUPPORT_TICKET_QUERY_KEYS.list(companyId, status),
    queryFn: () => supportTicketService.getAll(companyId, status),
    staleTime: 30 * 1000,
  });
};

export const useSupportTicket = (id: string, enabled = true) => {
  return useQuery({
    queryKey: SUPPORT_TICKET_QUERY_KEYS.detail(id),
    queryFn: () => supportTicketService.getById(id),
    enabled: enabled && !!id,
    staleTime: 30 * 1000,
  });
};

export const useUpdateSupportTicket = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSupportTicketDto }) =>
      supportTicketService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.lists() });
      toast.success('Ticket updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update ticket';
      toast.error(message);
    },
  });
};

export const useAddTicketMessage = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ ticketId, data }: { ticketId: string; data: CreateTicketMessageDto }) =>
      supportTicketService.addMessage(ticketId, data),
    onSuccess: (_, { ticketId }) => {
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.detail(ticketId) });
      toast.success('Message sent!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to send message';
      toast.error(message);
    },
  });
};

// FAQ hooks
export const useFaqItems = (companyId?: string, category?: string) => {
  return useQuery({
    queryKey: SUPPORT_TICKET_QUERY_KEYS.faqList(companyId),
    queryFn: () => supportTicketService.getFaqItems(companyId, category),
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateFaq = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateFaqDto) => supportTicketService.createFaq(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.faq });
      toast.success('FAQ created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create FAQ';
      toast.error(message);
    },
  });
};

export const useUpdateFaq = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateFaqDto }) =>
      supportTicketService.updateFaq(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.faq });
      toast.success('FAQ updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update FAQ';
      toast.error(message);
    },
  });
};

export const useDeleteFaq = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => supportTicketService.deleteFaq(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: SUPPORT_TICKET_QUERY_KEYS.faq });
      toast.success('FAQ deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete FAQ';
      toast.error(message);
    },
  });
};
