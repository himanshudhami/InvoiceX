import { apiClient } from './client'

// Types
export interface EmployeeDocument {
  id: string
  documentType: string
  title: string
  fileName: string
  fileSize?: number
  financialYear?: string
  isCompanyWide: boolean
  createdAt: string
}

export interface EmployeeDocumentDetail extends EmployeeDocument {
  description?: string
  fileUrl: string
  mimeType?: string
  employeeName?: string
}

export interface DocumentRequest {
  id: string
  documentType: string
  purpose?: string
  status: string
  employeeName?: string
  createdAt: string
  processedAt?: string
}

export interface CreateDocumentRequestDto {
  documentType: string
  purpose?: string
}

// Document type display names
export const documentTypeLabels: Record<string, string> = {
  offer_letter: 'Offer Letter',
  appointment_letter: 'Appointment Letter',
  form16: 'Form 16',
  form12bb: 'Form 12BB',
  salary_certificate: 'Salary Certificate',
  experience_certificate: 'Experience Certificate',
  relieving_letter: 'Relieving Letter',
  policy: 'Policy',
  handbook: 'Handbook',
  nda: 'NDA',
  agreement: 'Agreement',
  payslip: 'Payslip',
  other: 'Other',
}

// Document categories for grouping
export const documentCategories = [
  { id: 'employment', label: 'Employment', types: ['offer_letter', 'appointment_letter', 'relieving_letter', 'experience_certificate'] },
  { id: 'tax', label: 'Tax', types: ['form16', 'form12bb', 'salary_certificate'] },
  { id: 'policies', label: 'Policies', types: ['policy', 'handbook', 'nda', 'agreement'] },
  { id: 'payroll', label: 'Payroll', types: ['payslip'] },
]

// API functions
export const documentsApi = {
  // Get all documents for the current employee (includes company-wide)
  getMyDocuments: async (documentType?: string): Promise<EmployeeDocument[]> => {
    const params = documentType ? `?documentType=${documentType}` : ''
    const response = await apiClient.get(`/portal/documents${params}`)
    return response.data
  },

  // Get document detail by ID
  getById: async (id: string): Promise<EmployeeDocumentDetail> => {
    const response = await apiClient.get(`/portal/documents/${id}`)
    return response.data
  },

  // Get document download URL
  getDownloadUrl: (fileUrl: string): string => {
    const token = localStorage.getItem('portal_access_token')
    const baseUrl = import.meta.env.VITE_API_URL || '/api'
    // Normalize the path - ensure it starts with /
    const normalizedPath = fileUrl.startsWith('/') ? fileUrl : `/${fileUrl}`
    return `${baseUrl}/files/download${normalizedPath}?token=${token}`
  },

  // Get all document requests for the current employee
  getMyRequests: async (): Promise<DocumentRequest[]> => {
    const response = await apiClient.get('/portal/documents/requests')
    return response.data
  },

  // Create a document request
  createRequest: async (dto: CreateDocumentRequestDto): Promise<DocumentRequest> => {
    const response = await apiClient.post('/portal/documents/requests', dto)
    return response.data
  },
}

// Helper function to get category for a document type
export function getCategoryForType(documentType: string): string {
  for (const category of documentCategories) {
    if (category.types.includes(documentType)) {
      return category.label
    }
  }
  return 'Other'
}

// Helper function to format file size
export function formatFileSize(bytes?: number): string {
  if (!bytes) return '-'
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}
