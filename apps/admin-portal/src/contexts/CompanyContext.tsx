import React, { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { useAuth } from './AuthContext'
import { hasMultiCompanyAccess } from '../types/auth'
import { Company } from '../services/api/types'
import { companyService } from '../services/api/companyService'

interface CompanyContextType {
  // Currently selected company for data filtering
  selectedCompanyId: string | null
  selectedCompany: Company | null
  // All companies the user has access to
  availableCompanies: Company[]
  // Whether user has multi-company access
  hasMultiCompanyAccess: boolean
  // Switch to a different company
  setSelectedCompany: (company: Company) => void
  // Loading state
  isLoading: boolean
}

const CompanyContext = createContext<CompanyContextType | undefined>(undefined)

const SELECTED_COMPANY_KEY = 'admin_selected_company_id'

export function CompanyProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth()
  const [selectedCompany, setSelectedCompanyState] = useState<Company | null>(null)
  const [availableCompanies, setAvailableCompanies] = useState<Company[]>([])
  const [isLoading, setIsLoading] = useState(true)

  // Fetch available companies based on user access
  useEffect(() => {
    const fetchCompanies = async () => {
      if (!user) {
        setAvailableCompanies([])
        setSelectedCompanyState(null)
        setIsLoading(false)
        return
      }

      try {
        setIsLoading(true)

        // For users with multi-company access, fetch all companies
        // The backend will filter based on JWT claims
        const companies = await companyService.getAll()

        // Filter companies based on user's companyIds if available
        let filteredCompanies = companies
        if (user.companyIds && user.companyIds.length > 0 && !user.isSuperAdmin) {
          filteredCompanies = companies.filter(c => user.companyIds!.includes(c.id))
        }

        setAvailableCompanies(filteredCompanies)

        // Restore previously selected company or use default
        const savedCompanyId = localStorage.getItem(SELECTED_COMPANY_KEY)
        let defaultCompany: Company | null = null

        if (savedCompanyId) {
          defaultCompany = filteredCompanies.find(c => c.id === savedCompanyId) || null
        }

        if (!defaultCompany && filteredCompanies.length > 0) {
          // Try to find user's primary company
          defaultCompany = filteredCompanies.find(c => c.id === user.companyId) ||
                          filteredCompanies[0]
        }

        setSelectedCompanyState(defaultCompany)
      } catch (error) {
        console.error('Failed to fetch companies:', error)
        setAvailableCompanies([])
      } finally {
        setIsLoading(false)
      }
    }

    fetchCompanies()
  }, [user])

  const setSelectedCompany = useCallback((company: Company) => {
    setSelectedCompanyState(company)
    localStorage.setItem(SELECTED_COMPANY_KEY, company.id)
  }, [])

  const value: CompanyContextType = {
    selectedCompanyId: selectedCompany?.id || null,
    selectedCompany,
    availableCompanies,
    hasMultiCompanyAccess: hasMultiCompanyAccess(user),
    setSelectedCompany,
    isLoading,
  }

  return <CompanyContext.Provider value={value}>{children}</CompanyContext.Provider>
}

export function useCompanyContext() {
  const context = useContext(CompanyContext)
  if (context === undefined) {
    throw new Error('useCompanyContext must be used within a CompanyProvider')
  }
  return context
}
