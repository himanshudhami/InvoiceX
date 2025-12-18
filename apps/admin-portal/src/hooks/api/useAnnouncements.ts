import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { announcementService, CreateAnnouncementDto, UpdateAnnouncementDto } from '@/services/api/announcementService';
import toast from 'react-hot-toast';

export const ANNOUNCEMENT_QUERY_KEYS = {
  all: ['announcements'] as const,
  lists: () => [...ANNOUNCEMENT_QUERY_KEYS.all, 'list'] as const,
  list: (companyId?: string) => [...ANNOUNCEMENT_QUERY_KEYS.lists(), companyId] as const,
  details: () => [...ANNOUNCEMENT_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...ANNOUNCEMENT_QUERY_KEYS.details(), id] as const,
} as const;

export const useAnnouncements = (companyId?: string) => {
  return useQuery({
    queryKey: ANNOUNCEMENT_QUERY_KEYS.list(companyId),
    queryFn: () => announcementService.getAll(companyId),
    staleTime: 5 * 60 * 1000,
  });
};

export const useAnnouncement = (id: string, enabled = true) => {
  return useQuery({
    queryKey: ANNOUNCEMENT_QUERY_KEYS.detail(id),
    queryFn: () => announcementService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateAnnouncement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateAnnouncementDto) => announcementService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ANNOUNCEMENT_QUERY_KEYS.lists() });
      toast.success('Announcement created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create announcement';
      toast.error(message);
    },
  });
};

export const useUpdateAnnouncement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAnnouncementDto }) =>
      announcementService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ANNOUNCEMENT_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: ANNOUNCEMENT_QUERY_KEYS.lists() });
      toast.success('Announcement updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update announcement';
      toast.error(message);
    },
  });
};

export const useDeleteAnnouncement = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => announcementService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ANNOUNCEMENT_QUERY_KEYS.lists() });
      toast.success('Announcement deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete announcement';
      toast.error(message);
    },
  });
};
