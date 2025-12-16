import { FC } from 'react'
import { useCompanies } from '@/hooks/api/useCompanies'

interface CompanyFilterDropdownProps {
  value: string
  onChange: (companyId: string) => void
  className?: string
}

const CompanyFilterDropdown: FC<CompanyFilterDropdownProps> = ({ value, onChange, className = '' }) => {
  const { data: companies = [], isLoading } = useCompanies()

  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className={`px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent ${className}`}
      disabled={isLoading}
    >
      <option value="">All companies</option>
      {companies.map((company) => (
        <option key={company.id} value={company.id}>
          {company.name}
        </option>
      ))}
    </select>
  )
}

export default CompanyFilterDropdown



