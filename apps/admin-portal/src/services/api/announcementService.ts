import api from '../api';

export interface Announcement {
  id: string;
  companyId: string;
  title: string;
  content: string;
  category: string;
  priority: string;
  isPinned: boolean;
  publishedAt?: string;
  expiresAt?: string;
  createdBy?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateAnnouncementDto {
  companyId: string;
  title: string;
  content: string;
  category: string;
  priority: string;
  isPinned: boolean;
  publishedAt?: string;
  expiresAt?: string;
}

export interface UpdateAnnouncementDto {
  title: string;
  content: string;
  category: string;
  priority: string;
  isPinned: boolean;
  publishedAt?: string;
  expiresAt?: string;
}

export const announcementService = {
  getAll: async (companyId?: string): Promise<Announcement[]> => {
    const url = companyId ? `/announcements/company/${companyId}` : '/announcements/paged?pageNumber=1&pageSize=100';
    const response = await api.get(url);
    return companyId ? response.data : (response.data.data ?? []);
  },

  getPaged: async (params: { pageNumber: number; pageSize: number; companyId?: string; searchTerm?: string }) => {
    const queryParams = new URLSearchParams({
      pageNumber: params.pageNumber.toString(),
      pageSize: params.pageSize.toString(),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.companyId && { companyId: params.companyId }),
    });
    const response = await api.get(`/announcements/paged?${queryParams}`);
    return response.data;
  },

  getById: async (id: string): Promise<Announcement> => {
    const response = await api.get(`/announcements/${id}`);
    return response.data;
  },

  create: async (data: CreateAnnouncementDto): Promise<Announcement> => {
    const response = await api.post('/announcements', data);
    return response.data;
  },

  update: async (id: string, data: UpdateAnnouncementDto): Promise<void> => {
    await api.put(`/announcements/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/announcements/${id}`);
  },
};
