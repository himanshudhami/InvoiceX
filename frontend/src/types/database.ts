// Database types that extend the existing invoice types

export interface Customer {
  id: string
  company_id: string
  name: string
  company_name?: string
  email?: string
  phone?: string
  address_line1?: string
  address_line2?: string
  city?: string
  state?: string
  zip_code?: string
  country?: string
  tax_number?: string
  notes?: string
  credit_limit?: number
  payment_terms?: number
  is_active: boolean
  created_at: string
  updated_at: string
}

export interface Product {
  id: string
  company_id: string
  name: string
  description?: string
  sku?: string
  category?: string
  type: 'product' | 'service'
  unit_price: number
  unit: string
  tax_rate: number
  is_active: boolean
  created_at: string
  updated_at: string
}

export interface InvoiceDB {
  id: string
  company_id: string
  customer_id: string
  customer?: Customer
  invoice_number: string
  invoice_date: string
  due_date: string
  status: 'draft' | 'sent' | 'viewed' | 'partially_paid' | 'paid' | 'overdue' | 'cancelled'
  subtotal: number
  tax_amount: number
  discount_amount: number
  total_amount: number
  paid_amount: number
  currency: string
  notes?: string
  terms?: string
  po_number?: string
  project_name?: string
  sent_at?: string
  viewed_at?: string
  paid_at?: string
  created_at: string
  updated_at: string
  items?: InvoiceItem[]
  payments?: Payment[]
}

export interface InvoiceItem {
  id: string
  invoice_id: string
  product_id?: string
  product?: Product
  description: string
  quantity: number
  unit_price: number
  tax_rate: number
  discount_rate: number
  line_total: number
  sort_order: number
  created_at: string
  updated_at: string
}

export interface Payment {
  id: string
  invoice_id: string
  payment_date: string
  amount: number
  amount_in_inr?: number
  payment_method: 'cash' | 'check' | 'credit_card' | 'bank_transfer' | 'paypal' | 'other'
  reference_number?: string
  notes?: string
  created_at: string
  updated_at: string
}

export interface Company {
  id: string
  name: string
  logo_url?: string
  address_line1?: string
  address_line2?: string
  city?: string
  state?: string
  zip_code?: string
  country?: string
  email?: string
  phone?: string
  website?: string
  tax_number?: string
  created_at: string
  updated_at: string
}

export interface TaxRate {
  id: string
  company_id: string
  name: string
  rate: number
  is_default: boolean
  is_active: boolean
  created_at: string
  updated_at: string
}

// API Response types
export interface ApiResponse<T> {
  data: T
  message?: string
  success: boolean
}

export interface PaginatedResponse<T> {
  data: T[]
  pagination: {
    page: number
    limit: number
    total: number
    pages: number
  }
  success: boolean
}

// Form types for creating/updating
export interface CustomerFormData {
  name: string
  company_name?: string
  email?: string
  phone?: string
  address_line1?: string
  address_line2?: string
  city?: string
  state?: string
  zip_code?: string
  country?: string
  tax_number?: string
  notes?: string
  credit_limit?: number
  payment_terms?: number
}

export interface ProductFormData {
  name: string
  description?: string
  sku?: string
  category?: string
  type: 'product' | 'service'
  unit_price: number
  unit: string
  tax_rate: number
}

export interface InvoiceFormData {
  customer_id: string
  invoice_date: string
  due_date: string
  po_number?: string
  project_name?: string
  notes?: string
  terms?: string
  items: InvoiceItemFormData[]
}

export interface InvoiceItemFormData {
  product_id?: string
  description: string
  quantity: number
  unit_price: number
  tax_rate: number
  discount_rate: number
}

// Dashboard types
export interface DashboardStats {
  total_revenue: number
  monthly_revenue: number
  outstanding_amount: number
  overdue_amount: number
  total_invoices: number
  paid_invoices: number
  pending_invoices: number
  overdue_invoices: number
  recent_activities: Activity[]
  top_customers: CustomerStats[]
  revenue_chart: RevenueData[]
}

export interface Activity {
  id: string
  type: 'invoice_created' | 'invoice_sent' | 'invoice_paid' | 'customer_created' | 'product_created'
  description: string
  created_at: string
  invoice_id?: string
  customer_id?: string
}

export interface CustomerStats {
  customer_id: string
  customer_name: string
  total_invoices: number
  total_amount: number
  outstanding_amount: number
}

export interface RevenueData {
  month: string
  revenue: number
  invoices_count: number
}