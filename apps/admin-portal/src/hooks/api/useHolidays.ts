import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { holidayService } from '@/services/api/hr/leave/holidayService';
import {
  CreateHolidayDto,
  UpdateHolidayDto,
  BulkHolidaysDto,
  HolidayFilterParams,
} from '@/services/api/types';
import toast from 'react-hot-toast';

export const HOLIDAY_QUERY_KEYS = {
  all: ['holidays'] as const,
  lists: () => [...HOLIDAY_QUERY_KEYS.all, 'list'] as const,
  list: (params: HolidayFilterParams) => [...HOLIDAY_QUERY_KEYS.lists(), params] as const,
  details: () => [...HOLIDAY_QUERY_KEYS.all, 'detail'] as const,
  detail: (id: string) => [...HOLIDAY_QUERY_KEYS.details(), id] as const,
  byYear: (companyId: string, year: number) => [...HOLIDAY_QUERY_KEYS.all, 'year', companyId, year] as const,
  upcoming: (companyId: string, days: number) => [...HOLIDAY_QUERY_KEYS.all, 'upcoming', companyId, days] as const,
  paged: (params: HolidayFilterParams) => [...HOLIDAY_QUERY_KEYS.all, 'paged', params] as const,
} as const;

export const useHolidays = (params: HolidayFilterParams = {}) => {
  return useQuery({
    queryKey: HOLIDAY_QUERY_KEYS.list(params),
    queryFn: () => holidayService.getAll(params),
    staleTime: 5 * 60 * 1000,
  });
};

export const useHolidaysPaged = (params: HolidayFilterParams = {}) => {
  return useQuery({
    queryKey: HOLIDAY_QUERY_KEYS.paged(params),
    queryFn: () => holidayService.getPaged(params),
    keepPreviousData: true,
    staleTime: 30 * 1000,
  });
};

export const useHoliday = (id: string, enabled = true) => {
  return useQuery({
    queryKey: HOLIDAY_QUERY_KEYS.detail(id),
    queryFn: () => holidayService.getById(id),
    enabled: enabled && !!id,
    staleTime: 5 * 60 * 1000,
  });
};

export const useHolidaysByYear = (companyId: string, year: number, enabled = true) => {
  return useQuery({
    queryKey: HOLIDAY_QUERY_KEYS.byYear(companyId, year),
    queryFn: () => holidayService.getByYear(companyId, year),
    enabled: enabled && !!companyId && !!year,
    staleTime: 5 * 60 * 1000,
  });
};

export const useUpcomingHolidays = (companyId: string, days: number = 30, enabled = true) => {
  return useQuery({
    queryKey: HOLIDAY_QUERY_KEYS.upcoming(companyId, days),
    queryFn: () => holidayService.getUpcoming(companyId, days),
    enabled: enabled && !!companyId,
    staleTime: 5 * 60 * 1000,
  });
};

export const useCreateHoliday = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateHolidayDto) => holidayService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.lists() });
      toast.success('Holiday created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create holiday';
      toast.error(message);
    },
  });
};

export const useBulkCreateHolidays = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: BulkHolidaysDto) => holidayService.bulkCreate(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.lists() });
      if (result.successCount > 0) {
        toast.success(`${result.successCount} holidays created successfully!`);
      }
      if (result.failureCount > 0) {
        toast.error(`${result.failureCount} holidays failed to create.`);
      }
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to create holidays';
      toast.error(message);
    },
  });
};

export const useUpdateHoliday = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateHolidayDto }) =>
      holidayService.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.detail(id) });
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.lists() });
      toast.success('Holiday updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to update holiday';
      toast.error(message);
    },
  });
};

export const useDeleteHoliday = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => holidayService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.lists() });
      toast.success('Holiday deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to delete holiday';
      toast.error(message);
    },
  });
};

export const useCopyHolidaysToNextYear = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ companyId, sourceYear }: { companyId: string; sourceYear: number }) =>
      holidayService.copyToNextYear(companyId, sourceYear),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: HOLIDAY_QUERY_KEYS.lists() });
      toast.success(`Copied ${result.copied} holidays to next year!`);
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || error?.message || 'Failed to copy holidays';
      toast.error(message);
    },
  });
};
