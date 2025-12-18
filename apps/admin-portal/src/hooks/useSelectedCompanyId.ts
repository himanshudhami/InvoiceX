import { useCompanyContext } from '../contexts/CompanyContext'

/**
 * Hook that provides the selected company ID for API calls.
 * This should be used in all API hooks that need to filter by company.
 * 
 * For users with multi-company access, this returns the ID of the company
 * selected in the header company selector.
 * 
 * For regular users, this returns their single company ID.
 * 
 * @param overrideCompanyId Optional override - if provided, this takes precedence
 * @returns The company ID to use for filtering, or undefined if no company is selected
 */
export function useSelectedCompanyId(overrideCompanyId?: string): string | undefined {
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()
  
  // If an override is provided, use it
  if (overrideCompanyId) {
    return overrideCompanyId
  }
  
  // For multi-company users, use the selected company from context
  if (hasMultiCompanyAccess) {
    return selectedCompanyId ?? undefined
  }
  
  // For single-company users, they don't need to pass companyId
  // The backend will use their JWT company_id claim
  return undefined
}

/**
 * Hook that returns whether the current user has multi-company access
 * and should see the company selector.
 */
export function useHasMultiCompanyAccess(): boolean {
  const { hasMultiCompanyAccess } = useCompanyContext()
  return hasMultiCompanyAccess
}
