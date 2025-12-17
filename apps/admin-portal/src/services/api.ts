import axios, { AxiosResponse } from 'axios'
import {
  ApiResponse,
  PaginatedResponse,
  Customer,
  CustomerFormData,
  Product,
  ProductFormData,
  InvoiceDB,
  InvoiceFormData,
  Payment,
  Company,
  TaxRate,
  DashboardStats
} from '../types/database'

// Create axios instance with base configuration
const api = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Add request interceptor to include auth token
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('auth_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Add response interceptor to handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('auth_token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// Customer API
export const customerApi = {
  // Get all customers with pagination and filters
  getAll: (params?: {
    page?: number
    limit?: number
    search?: string
    is_active?: boolean
  }): Promise<PaginatedResponse<Customer>> =>
    api.get('/customers', { params }).then((res) => res.data),

  // Get customer by ID
  getById: (id: string): Promise<ApiResponse<Customer>> =>
    api.get(`/customers/${id}`).then((res) => res.data),

  // Create new customer
  create: (data: CustomerFormData): Promise<ApiResponse<Customer>> =>
    api.post('/customers', data).then((res) => res.data),

  // Update customer
  update: (id: string, data: Partial<CustomerFormData>): Promise<ApiResponse<Customer>> =>
    api.put(`/customers/${id}`, data).then((res) => res.data),

  // Delete customer
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(`/customers/${id}`).then((res) => res.data),

  // Search customers for autocomplete
  search: (query: string): Promise<ApiResponse<Customer[]>> =>
    api.get(`/customers/search`, { params: { q: query } }).then((res) => res.data),
}

// Product API
export const productApi = {
  // Get all products with pagination and filters
  getAll: (params?: {
    page?: number
    limit?: number
    search?: string
    type?: 'product' | 'service'
    category?: string
    is_active?: boolean
  }): Promise<PaginatedResponse<Product>> =>
    api.get('/products', { params }).then((res) => res.data),

  // Get product by ID
  getById: (id: string): Promise<ApiResponse<Product>> =>
    api.get(`/products/${id}`).then((res) => res.data),

  // Create new product
  create: (data: ProductFormData): Promise<ApiResponse<Product>> =>
    api.post('/products', data).then((res) => res.data),

  // Update product
  update: (id: string, data: Partial<ProductFormData>): Promise<ApiResponse<Product>> =>
    api.put(`/products/${id}`, data).then((res) => res.data),

  // Delete product
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(`/products/${id}`).then((res) => res.data),

  // Search products for autocomplete
  search: (query: string): Promise<ApiResponse<Product[]>> =>
    api.get(`/products/search`, { params: { q: query } }).then((res) => res.data),
}

// Invoice API
export const invoiceApi = {
  // Get all invoices with pagination and filters
  getAll: (params?: {
    page?: number
    limit?: number
    search?: string
    status?: string
    customer_id?: string
    from_date?: string
    to_date?: string
  }): Promise<PaginatedResponse<InvoiceDB>> =>
    api.get('/invoices', { params }).then((res) => res.data),

  // Get invoice by ID
  getById: (id: string): Promise<ApiResponse<InvoiceDB>> =>
    api.get(`/invoices/${id}`).then((res) => res.data),

  // Create new invoice
  create: (data: InvoiceFormData): Promise<ApiResponse<InvoiceDB>> =>
    api.post('/invoices', data).then((res) => res.data),

  // Update invoice
  update: (id: string, data: Partial<InvoiceFormData>): Promise<ApiResponse<InvoiceDB>> =>
    api.put(`/invoices/${id}`, data).then((res) => res.data),

  // Delete invoice
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(`/invoices/${id}`).then((res) => res.data),

  // Send invoice to customer
  send: (id: string): Promise<ApiResponse<void>> =>
    api.post(`/invoices/${id}/send`).then((res) => res.data),

  // Duplicate invoice
  duplicate: (id: string): Promise<ApiResponse<InvoiceDB>> =>
    api.post(`/invoices/${id}/duplicate`).then((res) => res.data),

  // Get PDF
  getPdf: (id: string): Promise<Blob> =>
    api.get(`/invoices/${id}/pdf`, { responseType: 'blob' }).then((res) => res.data),

  // Get next invoice number
  getNextNumber: (): Promise<ApiResponse<{ invoice_number: string }>> =>
    api.get('/invoices/next-number').then((res) => res.data),

  // Record payment
  recordPayment: (invoiceId: string, paymentData: {
    amount: number
    amountInInr?: number
    paymentDate: string
    paymentMethod: string
    referenceNumber?: string
    notes?: string
  }): Promise<ApiResponse<Payment>> =>
    api.post(`/invoices/${invoiceId}/payments`, paymentData).then((res) => res.data),

  // Get payments for invoice
  getPayments: (invoiceId: string): Promise<ApiResponse<Payment[]>> =>
    api.get(`/invoices/${invoiceId}/payments`).then((res) => res.data),
}

// Company API
export const companyApi = {
  // Get company details
  get: (): Promise<ApiResponse<Company>> =>
    api.get('/companies/current').then((res) => res.data),

  // Update company
  update: (data: Partial<Company>): Promise<ApiResponse<Company>> =>
    api.put('/companies/current', data).then((res) => res.data),
}

// Tax Rates API
export const taxRateApi = {
  // Get all tax rates
  getAll: (): Promise<ApiResponse<TaxRate[]>> =>
    api.get('/tax-rates').then((res) => res.data),

  // Create tax rate
  create: (data: { name: string; rate: number; is_default?: boolean }): Promise<ApiResponse<TaxRate>> =>
    api.post('/tax-rates', data).then((res) => res.data),

  // Update tax rate
  update: (id: string, data: Partial<{ name: string; rate: number; is_default: boolean }>): Promise<ApiResponse<TaxRate>> =>
    api.put(`/tax-rates/${id}`, data).then((res) => res.data),

  // Delete tax rate
  delete: (id: string): Promise<ApiResponse<void>> =>
    api.delete(`/tax-rates/${id}`).then((res) => res.data),
}

// Dashboard API
export const dashboardApi = {
  // Get dashboard stats
  getStats: (): Promise<ApiResponse<DashboardStats>> =>
    api.get('/reports/dashboard').then((res) => res.data),
}

// Reports API
export const reportsApi = {
  // Sales report
  getSalesReport: (params: {
    from_date: string
    to_date: string
  }): Promise<ApiResponse<any>> =>
    api.get('/reports/sales', { params }).then((res) => res.data),

  // Aging report
  getAgingReport: (): Promise<ApiResponse<any>> =>
    api.get('/reports/aging').then((res) => res.data),

  // Customer statement
  getCustomerStatement: (customerId: string, params?: {
    from_date?: string
    to_date?: string
  }): Promise<ApiResponse<any>> =>
    api.get(`/reports/customer-statement/${customerId}`, { params }).then((res) => res.data),
}

export default api