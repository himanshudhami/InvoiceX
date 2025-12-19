import { FC } from 'react'
import { useCompanies } from '@/hooks/api/useCompanies'
import { CompanySelect } from './CompanySelect'

interface CompanyFilterDropdownProps {
  value: string
  onChange: (companyId: string) => void
  className?: string
  allowAll?: boolean
}

// Wrapper to keep existing API while using the shared CompanySelect combobox
const CompanyFilterDropdown: FC<CompanyFilterDropdownProps> = ({
  value,
  onChange,
  className = '',
  allowAll = false,
}) => {
  const { data: companies = [] } = useCompanies()

  return (
    <CompanySelect
      companies={companies}
      value={value}
      onChange={onChange}
      placeholder="Filter by company"
      className={className}
      showAllOption={allowAll}
      allOptionLabel="All companies"
    />
  )
}

export default CompanyFilterDropdown



