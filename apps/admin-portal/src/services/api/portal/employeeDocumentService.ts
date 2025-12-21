import api from '../../api';

export interface EmployeeDocument {
  id: string;
  employeeId: string;
  companyId: string;
  documentType: string;
  title: string;
  description?: string;
  fileUrl: string;
  fileName: string;
  fileSize?: number;
  mimeType?: string;
  financialYear?: string;
  isCompanyWide: boolean;
  employeeName?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateEmployeeDocumentDto {
  employeeId: string;
  companyId: string;
  documentType: string;
  title: string;
  description?: string;
  fileUrl: string;
  fileName: string;
  fileSize?: number;
  mimeType?: string;
  financialYear?: string;
  isCompanyWide: boolean;
}

export interface UpdateEmployeeDocumentDto {
  documentType: string;
  title: string;
  description?: string;
  fileUrl: string;
  fileName: string;
  fileSize?: number;
  mimeType?: string;
  financialYear?: string;
  isCompanyWide: boolean;
}

export interface DocumentRequest {
  id: string;
  employeeId: string;
  documentType: string;
  purpose?: string;
  status: string;
  employeeName?: string;
  processedAt?: string;
  createdAt: string;
}

export interface UpdateDocumentRequestDto {
  status: string;
  rejectionReason?: string;
  documentId?: string;
}

export const employeeDocumentService = {
  getAll: async (companyId?: string, employeeId?: string): Promise<EmployeeDocument[]> => {
    if (employeeId && companyId) {
      const response = await api.get(`/employeedocuments/employee/${employeeId}/company/${companyId}`);
      return response.data;
    }

    if (companyId) {
      const response = await api.get(`/employeedocuments/company/${companyId}`);
      return response.data;
    }

    // Fallback: fetch first page of all documents (admin view)
    const response = await api.get('/employeedocuments/paged?pageNumber=1&pageSize=100');
    return response.data.data ?? [];
  },

  getPaged: async (params: { pageNumber: number; pageSize: number; companyId?: string; employeeId?: string; documentType?: string; searchTerm?: string }) => {
    const queryParams = new URLSearchParams({
      pageNumber: params.pageNumber.toString(),
      pageSize: params.pageSize.toString(),
      ...(params.searchTerm && { searchTerm: params.searchTerm }),
      ...(params.companyId && { companyId: params.companyId }),
      ...(params.employeeId && { employeeId: params.employeeId }),
      ...(params.documentType && { documentType: params.documentType }),
    });
    const response = await api.get(`/employeedocuments/paged?${queryParams}`);
    return response.data;
  },

  getById: async (id: string): Promise<EmployeeDocument> => {
    const response = await api.get(`/employeedocuments/${id}`);
    return response.data;
  },

  getCompanyWide: async (companyId: string): Promise<EmployeeDocument[]> => {
    const response = await api.get(`/employeedocuments/company/${companyId}/company-wide`);
    return response.data;
  },

  create: async (data: CreateEmployeeDocumentDto): Promise<EmployeeDocument> => {
    const response = await api.post('/employeedocuments', data);
    return response.data;
  },

  update: async (id: string, data: UpdateEmployeeDocumentDto): Promise<void> => {
    await api.put(`/employeedocuments/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/employeedocuments/${id}`);
  },

  // Document Requests
  getPendingRequests: async (companyId: string): Promise<DocumentRequest[]> => {
    const response = await api.get(`/employeedocuments/requests/pending/${companyId}`);
    return response.data;
  },

  getRequestById: async (id: string): Promise<DocumentRequest> => {
    const response = await api.get(`/employeedocuments/requests/${id}`);
    return response.data;
  },

  processRequest: async (id: string, data: UpdateDocumentRequestDto): Promise<void> => {
    await api.put(`/employeedocuments/requests/${id}`, data);
  },
};
