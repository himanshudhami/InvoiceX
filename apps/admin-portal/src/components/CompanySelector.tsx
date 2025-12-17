import { FC, useState } from 'react'
import { useCompanies } from '@/hooks/api'
import { Company } from '@/services/api/types'

interface Props {
  selectedCompany?: Company
  onCompanySelect: (company: Company) => void
  pdfMode?: boolean
}

const CompanySelector: FC<Props> = ({ selectedCompany, onCompanySelect, pdfMode }) => {
  const [isOpen, setIsOpen] = useState(false)
  const { data: companies = [], isLoading, error } = useCompanies()

  if (pdfMode) {
    return <span className="text-xl font-bold text-center block w-full">{selectedCompany?.name || 'Select Company'}</span>
  }

  if (error) {
    return (
      <div className="text-red-500 text-sm p-2">
        Failed to load companies. Please try again.
      </div>
    )
  }

  return (
    <div className="relative">
      <button
        type="button"
        className="w-full text-left bg-transparent border-none outline-none p-1 focus:bg-gray-50 focus:ring-1 focus:ring-primary/20 rounded text-xl font-bold text-center"
        onClick={() => setIsOpen(!isOpen)}
        disabled={isLoading}
      >
        {isLoading ? 'Loading...' : (selectedCompany?.name || 'Select Company')}
      </button>
      
      {isOpen && !isLoading && (
        <div className="absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-md shadow-lg z-50 max-h-60 overflow-y-auto">
          {companies.length === 0 ? (
            <div className="px-3 py-2 text-gray-500">No companies available</div>
          ) : (
            companies.map((company) => (
              <button
                key={company.id}
                type="button"
                className="block w-full text-left px-3 py-2 hover:bg-gray-50 border-none bg-transparent"
                onClick={() => {
                  onCompanySelect(company)
                  setIsOpen(false)
                }}
              >
                <div className="font-medium">{company.name}</div>
                <div className="text-sm text-gray-600">
                  {company.city && company.state ? `${company.city}, ${company.state}` : 'Address not available'}
                </div>
              </button>
            ))
          )}
        </div>
      )}
    </div>
  )
}

export default CompanySelector