import React, { useMemo, useState } from 'react'
import { Modal } from '@/components/ui/Modal'
import { useInvoiceTemplates } from '@/hooks/api/useInvoiceTemplates'
import { Invoice, InvoiceItem, Customer, Company } from '@/services/api/types'
import { InvoicePDFDownload } from '@/components/InvoicePDF'

export interface TemplateSelectModalProps {
  isOpen: boolean
  onClose: () => void
  invoice: Invoice
  invoiceItems: InvoiceItem[]
  customer?: Customer
  company?: Company
  defaultTemplateKey?: 'minimal-classic' | 'minimal-ledger'
}

export const TemplateSelectModal: React.FC<TemplateSelectModalProps> = ({ isOpen, onClose, invoice, invoiceItems, customer, company, defaultTemplateKey = 'minimal-classic' }) => {
  const { data: templates = [] } = useInvoiceTemplates()
  const registryOptions: { key: 'minimal-classic'|'minimal-ledger'; name: string }[] = [
    { key: 'minimal-classic', name: 'Classic' },
    { key: 'minimal-ledger', name: 'Minimal Ledger' },
  ]

  // Try to map server templates by name to registry keys (simple heuristic)
  const serverNames = new Set(templates.map(t => t.name.toLowerCase()))
  const available = useMemo(() => registryOptions.filter(o => true), [templates])

  const [selected, setSelected] = useState<'minimal-classic'|'minimal-ledger'>(defaultTemplateKey)

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Select template" size="sm">
      <div className="space-y-4">
        <div>
          <label className="block text-sm text-gray-600 mb-1">Template</label>
          <select className="w-full border rounded px-3 py-2" value={selected} onChange={(e) => setSelected(e.target.value as any)}>
            {available.map(o => (
              <option key={o.key} value={o.key}>{o.name}</option>
            ))}
          </select>
        </div>

        <div className="flex justify-end space-x-2">
          <button onClick={onClose} className="px-4 py-2 text-sm rounded border">Cancel</button>

          <InvoicePDFDownload
            invoice={invoice}
            invoiceItems={invoiceItems}
            customer={customer}
            company={company}
            templateKey={selected}
            className="px-4 py-2 text-sm rounded bg-primary text-white hover:bg-primary/90 disabled:opacity-60"
            label="Download"
          />
        </div>
      </div>
    </Modal>
  )
}
