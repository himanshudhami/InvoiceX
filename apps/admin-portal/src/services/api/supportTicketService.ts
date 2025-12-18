import api from '../api';

export interface SupportTicket {
  id: string;
  companyId: string;
  employeeId: string;
  ticketNumber: string;
  subject: string;
  description: string;
  category: string;
  priority: string;
  status: string;
  assignedTo?: string;
  assignedToName?: string;
  employeeName?: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  createdAt: string;
  messages?: TicketMessage[];
}

export interface TicketMessage {
  id: string;
  senderType: string;
  senderName?: string;
  message: string;
  attachmentUrl?: string;
  createdAt: string;
}

export interface UpdateSupportTicketDto {
  subject: string;
  description: string;
  category: string;
  priority: string;
  status: string;
  assignedTo?: string;
  resolutionNotes?: string;
}

export interface CreateTicketMessageDto {
  message: string;
  attachmentUrl?: string;
}

export interface FaqItem {
  id: string;
  companyId?: string;
  category: string;
  question: string;
  answer: string;
  sortOrder: number;
  isActive: boolean;
}

export interface CreateFaqDto {
  companyId?: string;
  category: string;
  question: string;
  answer: string;
  sortOrder: number;
  isActive: boolean;
}

export interface UpdateFaqDto {
  category: string;
  question: string;
  answer: string;
  sortOrder: number;
  isActive: boolean;
}

export const supportTicketService = {
  getAll: async (companyId?: string, status?: string): Promise<SupportTicket[]> => {
    const url = companyId
      ? `/supporttickets/company/${companyId}${status ? `?status=${status}` : ''}`
      : '/supporttickets/paged?pageNumber=1&pageSize=100';
    const response = await api.get(url);
    return companyId ? response.data : (response.data.data ?? []);
  },

  getPaged: async (params: { pageNumber: number; pageSize: number; companyId?: string; status?: string; searchTerm?: string }) => {
    const queryParams = new URLSearchParams({
      pageNumber: params.pageNumber.toString(),
      pageSize: params.pageSize.toString(),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.companyId && { companyId: params.companyId }),
      ...(params.status && { status: params.status }),
    });
    const response = await api.get(`/supporttickets/paged?${queryParams}`);
    return response.data;
  },

  getById: async (id: string): Promise<SupportTicket> => {
    const response = await api.get(`/supporttickets/${id}`);
    return response.data;
  },

  update: async (id: string, data: UpdateSupportTicketDto): Promise<void> => {
    await api.put(`/supporttickets/${id}`, data);
  },

  addMessage: async (ticketId: string, data: CreateTicketMessageDto): Promise<TicketMessage> => {
    const response = await api.post(`/supporttickets/${ticketId}/messages`, data);
    return response.data;
  },

  // FAQ endpoints
  getFaqItems: async (companyId?: string, category?: string): Promise<FaqItem[]> => {
    const queryParams = new URLSearchParams();
    if (companyId) queryParams.append('companyId', companyId);
    if (category) queryParams.append('category', category);
    const response = await api.get(`/supporttickets/faq?${queryParams}`);
    return response.data;
  },

  getFaqById: async (id: string): Promise<FaqItem> => {
    const response = await api.get(`/supporttickets/faq/${id}`);
    return response.data;
  },

  createFaq: async (data: CreateFaqDto): Promise<FaqItem> => {
    const response = await api.post('/supporttickets/faq', data);
    return response.data;
  },

  updateFaq: async (id: string, data: UpdateFaqDto): Promise<void> => {
    await api.put(`/supporttickets/faq/${id}`, data);
  },

  deleteFaq: async (id: string): Promise<void> => {
    await api.delete(`/supporttickets/faq/${id}`);
  },
};
