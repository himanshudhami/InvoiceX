import { createContext, useContext, ReactNode } from 'react'
import { Invoice, Customer, Company, Product } from '@/services/api/types'

interface InvoiceFormData {
  invoiceNumber: string
  status: string
  invoiceDate: string
  dueDate: string
  currency: string
  customerId?: string
  companyId?: string
  subtotal: number
  taxAmount: number
  discountAmount: number
  totalAmount: number
  paidAmount: number
  notes?: string
  terms?: string
  poNumber?: string
  projectName?: string
  lineItems: LineItem[]
}

interface LineItem {
  id: string
  productId?: string
  description: string
  quantity: number
  unitPrice: number
  taxRate: number
  discountRate?: number
  lineTotal: number
  sortOrder?: number
}

interface InvoiceFormContextType {
  formData: InvoiceFormData
  updateField: (field: keyof InvoiceFormData, value: any) => void
  addLineItem: () => void
  removeLineItem: (id: string) => void
  updateLineItem: (id: string, field: keyof LineItem, value: any) => void
  customers: Customer[]
  companies: Company[]
  products: Product[]
  isLoading: boolean
  errors: Record<string, string>
  setErrors: (errors: Record<string, string>) => void
}

const InvoiceFormContext = createContext<InvoiceFormContextType | undefined>(undefined)

export const useInvoiceForm = () => {
  const context = useContext(InvoiceFormContext)
  if (!context) {
    throw new Error('useInvoiceForm must be used within InvoiceFormProvider')
  }
  return context
}

interface InvoiceFormProviderProps {
  children: ReactNode
  value: InvoiceFormContextType
}

export const InvoiceFormProvider = ({ children, value }: InvoiceFormProviderProps) => {
  return (
    <InvoiceFormContext.Provider value={value}>
      {children}
    </InvoiceFormContext.Provider>
  )
}
