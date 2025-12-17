import React, { useState } from 'react'
import { Modal } from '@/components/ui/Modal'
import { useInvoiceTemplates } from '@/hooks/api/useInvoiceTemplates'
import { Quote, QuoteItem, Customer, Company } from '@/services/api/types'
import { QuotePDFDownload } from '@/components/QuotePDF'

export interface QuoteTemplateSelectModalProps {
  isOpen: boolean
  onClose: () => void
  quote: Quote
  quoteItems: QuoteItem[]
  customer?: Customer
  company?: Company
  defaultTemplateKey?: 'minimal-classic' | 'minimal-ledger'
}

export const QuoteTemplateSelectModal: React.FC<QuoteTemplateSelectModalProps> = ({
  isOpen,
  onClose,
  quote,
  quoteItems,
  customer,
  company,
  defaultTemplateKey = 'minimal-classic'
}) => {
  const { data: templates = [] } = useInvoiceTemplates()
  const registryOptions: { key: 'minimal-classic'|'minimal-ledger'; name: string }[] = [
    { key: 'minimal-classic', name: 'Classic' },
    { key: 'minimal-ledger', name: 'Minimal Ledger' },
  ]

  // Try to map server templates by name to registry keys (simple heuristic)
  const available = registryOptions

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

          <QuotePDFDownload
            quote={quote}
            quoteItems={quoteItems}
            customer={customer}
            company={company}
          >
            <button className="px-4 py-2 text-sm rounded bg-primary text-white hover:bg-primary/90">Download</button>
          </QuotePDFDownload>
        </div>
      </div>
    </Modal>
  )
}
