import { useMemo } from 'react'
import {
  usePayrollTransactions,
  useContractorPayments,
} from '@/features/payroll/hooks'
import { useEmployees } from '@/hooks/api/useEmployees'
import type { PayrollTransaction, ContractorPayment } from '@/features/payroll/types/payroll'
import type { EmployeeSalaryTransaction } from '@/services/api/types'

/**
 * Combined payroll expenses hook that merges employee payroll transactions and contractor payments
 * This replaces the old useSalaryTransactions hook for expense reporting
 * Returns data in the old EmployeeSalaryTransaction format for backward compatibility
 */
export const usePayrollExpenses = (params?: {
  companyId?: string
  year?: number
  month?: number
}) => {
  const { data: employeeTransactionsData, isLoading: isLoadingEmployees } = usePayrollTransactions({
    companyId: params?.companyId,
    payrollYear: params?.year,
    payrollMonth: params?.month,
    status: 'paid',
    pageSize: 1000, // Get all for the period
  })

  const { data: contractorPaymentsData, isLoading: isLoadingContractors } = useContractorPayments({
    companyId: params?.companyId,
    paymentYear: params?.year,
    paymentMonth: params?.month,
    status: 'paid',
    pageSize: 1000,
  })

  const { data: employees = [] } = useEmployees()

  // Combine and normalize to match old EmployeeSalaryTransaction shape for compatibility
  const combinedExpenses = useMemo(() => {
    const employeeTransactions = employeeTransactionsData?.items || []
    const contractorPayments = contractorPaymentsData?.items || []

    // Map payroll transactions to old format for backward compatibility
    const mappedEmployeeTransactions: EmployeeSalaryTransaction[] = employeeTransactions.map((pt) => {
      const employee = employees.find((e) => e.id === pt.employeeId)
      return {
        id: pt.id,
        employeeId: pt.employeeId,
        employee: employee ? {
          employeeName: pt.employeeName || employee.employeeName,
          department: employee.department || '',
        } : { employeeName: pt.employeeName || '', department: '' },
        companyId: params?.companyId || '', // Transactions are already filtered by company via payroll runs
        salaryMonth: pt.payrollMonth,
        salaryYear: pt.payrollYear,
        grossSalary: pt.grossEarnings,
        netSalary: pt.netPayable,
        pfEmployee: pt.pfEmployee,
        pfEmployer: pt.pfEmployer,
        esiEmployee: pt.esiEmployee,
        esiEmployer: pt.esiEmployer,
        pt: pt.professionalTax,
        incomeTax: pt.tdsDeducted,
        otherDeductions: pt.otherDeductions,
        // Map other fields as needed
      } as EmployeeSalaryTransaction
    })

    // Map contractor payments to old format
    const mappedContractorPayments: EmployeeSalaryTransaction[] = contractorPayments.map((cp) => {
      const employee = employees.find((e) => e.id === cp.employeeId)
      return {
        id: cp.id,
        employeeId: cp.employeeId,
        employee: employee ? {
          employeeName: cp.employeeName || employee.employeeName,
          department: employee.department || '',
        } : { employeeName: cp.employeeName || '', department: '' },
        companyId: cp.companyId,
        salaryMonth: cp.paymentMonth,
        salaryYear: cp.paymentYear,
        grossSalary: cp.grossAmount,
        netSalary: cp.netPayable,
        pfEmployee: 0, // Contractors don't have PF
        pfEmployer: 0,
        esiEmployee: 0, // Contractors don't have ESI
        esiEmployer: 0,
        pt: 0, // Contractors don't have PT
        incomeTax: cp.tdsAmount,
        otherDeductions: cp.otherDeductions,
      } as EmployeeSalaryTransaction
    })

    return [...mappedEmployeeTransactions, ...mappedContractorPayments]
  }, [employeeTransactionsData, contractorPaymentsData, employees, params?.companyId])

  return {
    data: combinedExpenses,
    isLoading: isLoadingEmployees || isLoadingContractors,
    error: null,
  }
}




