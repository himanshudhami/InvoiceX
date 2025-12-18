import { FC, useState, useRef, useEffect } from 'react'
import { ChevronDown, Building2, Check } from 'lucide-react'
import { useCompanyContext } from '../contexts/CompanyContext'

export const HeaderCompanySelector: FC = () => {
  const {
    selectedCompany,
    availableCompanies,
    hasMultiCompanyAccess,
    setSelectedCompany,
    isLoading,
  } = useCompanyContext()

  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Don't show selector if user only has access to one company
  if (!hasMultiCompanyAccess || availableCompanies.length <= 1) {
    return null
  }

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 text-sm text-gray-500">
        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
        <span>Loading...</span>
      </div>
    )
  }

  return (
    <div className="relative" ref={dropdownRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-200 bg-gray-100 dark:bg-gray-700 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors min-w-[180px]"
      >
        <Building2 className="h-4 w-4 text-gray-500 dark:text-gray-400" />
        <span className="truncate flex-1 text-left">
          {selectedCompany?.name || 'Select Company'}
        </span>
        <ChevronDown
          className={`h-4 w-4 text-gray-500 dark:text-gray-400 transition-transform ${
            isOpen ? 'rotate-180' : ''
          }`}
        />
      </button>

      {isOpen && (
        <div className="absolute left-0 mt-1 w-64 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 z-50 py-1 max-h-60 overflow-y-auto">
          {availableCompanies.map((company) => (
            <button
              key={company.id}
              onClick={() => {
                setSelectedCompany(company)
                setIsOpen(false)
              }}
              className={`w-full flex items-center gap-3 px-4 py-2 text-sm text-left hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors ${
                selectedCompany?.id === company.id
                  ? 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300'
                  : 'text-gray-700 dark:text-gray-200'
              }`}
            >
              <Building2 className="h-4 w-4 flex-shrink-0" />
              <span className="truncate flex-1">{company.name}</span>
              {selectedCompany?.id === company.id && (
                <Check className="h-4 w-4 flex-shrink-0 text-blue-600 dark:text-blue-400" />
              )}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
