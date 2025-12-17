/**
 * Payroll Export Service
 * 
 * Single Responsibility: Payroll-specific Excel export formatting
 * Uses the base Excel export utility (DRY principle)
 * Separated from UI concerns (SOC principle)
 */

import { exportToExcel, ExcelColumn, ExcelSheet } from '@/lib/excelExport'
import { PayrollTransaction, PayrollRun } from '@/features/payroll/types/payroll'
import { formatINR } from '@/lib/currency'

export interface PayrollExportData {
  payrollRun: PayrollRun
  transactions: PayrollTransaction[]
}

/**
 * Formats currency values for Excel (removes currency symbol, keeps number)
 */
const formatCurrency = (value: number): number => {
  return value || 0
}

/**
 * Creates columns for payroll transaction details
 */
function createTransactionColumns(): ExcelColumn[] {
  return [
    { header: 'Employee Name', key: 'employeeName', width: 25 },
    { header: 'Employee ID', key: 'employeeId', width: 20 },
    { header: 'Payroll Type', key: 'payrollType', width: 15 },
    { header: 'Working Days', key: 'workingDays', width: 12 },
    { header: 'Present Days', key: 'presentDays', width: 12 },
    { header: 'LOP Days', key: 'lopDays', width: 12 },
    
    // Earnings Section
    { header: 'Basic Earned', key: 'basicEarned', width: 15, format: formatCurrency },
    { header: 'HRA Earned', key: 'hraEarned', width: 15, format: formatCurrency },
    { header: 'DA Earned', key: 'daEarned', width: 15, format: formatCurrency },
    { header: 'Conveyance Allowance', key: 'conveyanceEarned', width: 18, format: formatCurrency },
    { header: 'Medical Allowance', key: 'medicalEarned', width: 18, format: formatCurrency },
    { header: 'Special Allowance', key: 'specialAllowanceEarned', width: 18, format: formatCurrency },
    { header: 'Other Allowances', key: 'otherAllowancesEarned', width: 18, format: formatCurrency },
    { header: 'LTA Paid', key: 'ltaPaid', width: 15, format: formatCurrency },
    { header: 'Bonus Paid', key: 'bonusPaid', width: 15, format: formatCurrency },
    { header: 'Arrears', key: 'arrears', width: 15, format: formatCurrency },
    { header: 'Reimbursements', key: 'reimbursements', width: 18, format: formatCurrency },
    { header: 'Incentives', key: 'incentives', width: 15, format: formatCurrency },
    { header: 'Other Earnings', key: 'otherEarnings', width: 18, format: formatCurrency },
    { header: 'Gross Earnings', key: 'grossEarnings', width: 15, format: formatCurrency },
    
    // Deductions Section
    { header: 'PF (Employee)', key: 'pfEmployee', width: 15, format: formatCurrency },
    { header: 'ESI (Employee)', key: 'esiEmployee', width: 15, format: formatCurrency },
    { header: 'Professional Tax', key: 'professionalTax', width: 18, format: formatCurrency },
    { header: 'TDS Deducted', key: 'tdsDeducted', width: 15, format: formatCurrency },
    { header: 'Loan Recovery', key: 'loanRecovery', width: 15, format: formatCurrency },
    { header: 'Advance Recovery', key: 'advanceRecovery', width: 18, format: formatCurrency },
    { header: 'Other Deductions', key: 'otherDeductions', width: 18, format: formatCurrency },
    { header: 'Total Deductions', key: 'totalDeductions', width: 18, format: formatCurrency },
    
    // Net Pay
    { header: 'Net Payable', key: 'netPayable', width: 15, format: formatCurrency },
    
    // Employer Contributions
    { header: 'PF (Employer)', key: 'pfEmployer', width: 15, format: formatCurrency },
    { header: 'PF Admin Charges', key: 'pfAdminCharges', width: 18, format: formatCurrency },
    { header: 'PF EDLI', key: 'pfEdli', width: 15, format: formatCurrency },
    { header: 'ESI (Employer)', key: 'esiEmployer', width: 15, format: formatCurrency },
    { header: 'Gratuity Provision', key: 'gratuityProvision', width: 18, format: formatCurrency },
    { header: 'Total Employer Cost', key: 'totalEmployerCost', width: 20, format: formatCurrency },
    
    // Payment Info
    { header: 'Status', key: 'status', width: 12 },
    { header: 'Payment Date', key: 'paymentDate', width: 15 },
    { header: 'Payment Method', key: 'paymentMethod', width: 15 },
    { header: 'Payment Reference', key: 'paymentReference', width: 20 },
    { header: 'Bank Account', key: 'bankAccount', width: 20 },
    { header: 'Remarks', key: 'remarks', width: 30 },
  ]
}

/**
 * Creates a summary sheet with payroll run totals
 */
function createSummarySheet(payrollRun: PayrollRun): ExcelSheet {
  const summaryData = [
    { Label: 'Payroll Period', Value: `${payrollRun.payrollMonth}/${payrollRun.payrollYear}` },
    { Label: 'Financial Year', Value: payrollRun.financialYear },
    { Label: 'Status', Value: payrollRun.status },
    { Label: '', Value: '' }, // Empty row
    { Label: 'Total Employees', Value: payrollRun.totalEmployees },
    { Label: 'Total Contractors', Value: payrollRun.totalContractors },
    { Label: '', Value: '' }, // Empty row
    { Label: 'Total Gross Salary', Value: formatCurrency(payrollRun.totalGrossSalary) },
    { Label: 'Total Deductions', Value: formatCurrency(payrollRun.totalDeductions) },
    { Label: 'Total Net Salary', Value: formatCurrency(payrollRun.totalNetSalary) },
    { Label: '', Value: '' }, // Empty row
    { Label: 'PF (Employer)', Value: formatCurrency(payrollRun.totalEmployerPf) },
    { Label: 'ESI (Employer)', Value: formatCurrency(payrollRun.totalEmployerEsi) },
    { Label: 'Total Employer Cost', Value: formatCurrency(payrollRun.totalEmployerCost) },
  ]

  return {
    name: 'Summary',
    columns: [
      { header: 'Label', key: 'Label', width: 25 },
      { header: 'Value', key: 'Value', width: 20 },
    ],
    data: summaryData,
  }
}

/**
 * Creates a totals row for the transactions sheet
 */
function createTotalsRow(transactions: PayrollTransaction[]): any {
  const totals = transactions.reduce(
    (acc, txn) => ({
      employeeName: 'TOTALS',
      employeeId: '',
      payrollType: '',
      workingDays: '',
      presentDays: '',
      lopDays: '',
      basicEarned: acc.basicEarned + (txn.basicEarned || 0),
      hraEarned: acc.hraEarned + (txn.hraEarned || 0),
      daEarned: acc.daEarned + (txn.daEarned || 0),
      conveyanceEarned: acc.conveyanceEarned + (txn.conveyanceEarned || 0),
      medicalEarned: acc.medicalEarned + (txn.medicalEarned || 0),
      specialAllowanceEarned: acc.specialAllowanceEarned + (txn.specialAllowanceEarned || 0),
      otherAllowancesEarned: acc.otherAllowancesEarned + (txn.otherAllowancesEarned || 0),
      ltaPaid: acc.ltaPaid + (txn.ltaPaid || 0),
      bonusPaid: acc.bonusPaid + (txn.bonusPaid || 0),
      arrears: acc.arrears + (txn.arrears || 0),
      reimbursements: acc.reimbursements + (txn.reimbursements || 0),
      incentives: acc.incentives + (txn.incentives || 0),
      otherEarnings: acc.otherEarnings + (txn.otherEarnings || 0),
      grossEarnings: acc.grossEarnings + (txn.grossEarnings || 0),
      pfEmployee: acc.pfEmployee + (txn.pfEmployee || 0),
      esiEmployee: acc.esiEmployee + (txn.esiEmployee || 0),
      professionalTax: acc.professionalTax + (txn.professionalTax || 0),
      tdsDeducted: acc.tdsDeducted + (txn.tdsDeducted || 0),
      loanRecovery: acc.loanRecovery + (txn.loanRecovery || 0),
      advanceRecovery: acc.advanceRecovery + (txn.advanceRecovery || 0),
      otherDeductions: acc.otherDeductions + (txn.otherDeductions || 0),
      totalDeductions: acc.totalDeductions + (txn.totalDeductions || 0),
      netPayable: acc.netPayable + (txn.netPayable || 0),
      pfEmployer: acc.pfEmployer + (txn.pfEmployer || 0),
      pfAdminCharges: acc.pfAdminCharges + (txn.pfAdminCharges || 0),
      pfEdli: acc.pfEdli + (txn.pfEdli || 0),
      esiEmployer: acc.esiEmployer + (txn.esiEmployer || 0),
      gratuityProvision: acc.gratuityProvision + (txn.gratuityProvision || 0),
      totalEmployerCost: acc.totalEmployerCost + (txn.totalEmployerCost || 0),
      status: '',
      paymentDate: '',
      paymentMethod: '',
      paymentReference: '',
      bankAccount: '',
      remarks: '',
    }),
    {
      employeeName: 'TOTALS',
      employeeId: '',
      payrollType: '',
      workingDays: '',
      presentDays: '',
      lopDays: '',
      basicEarned: 0,
      hraEarned: 0,
      daEarned: 0,
      conveyanceEarned: 0,
      medicalEarned: 0,
      specialAllowanceEarned: 0,
      otherAllowancesEarned: 0,
      ltaPaid: 0,
      bonusPaid: 0,
      arrears: 0,
      reimbursements: 0,
      incentives: 0,
      otherEarnings: 0,
      grossEarnings: 0,
      pfEmployee: 0,
      esiEmployee: 0,
      professionalTax: 0,
      tdsDeducted: 0,
      loanRecovery: 0,
      advanceRecovery: 0,
      otherDeductions: 0,
      totalDeductions: 0,
      netPayable: 0,
      pfEmployer: 0,
      pfAdminCharges: 0,
      pfEdli: 0,
      esiEmployer: 0,
      gratuityProvision: 0,
      totalEmployerCost: 0,
      status: '',
      paymentDate: '',
      paymentMethod: '',
      paymentReference: '',
      bankAccount: '',
      remarks: '',
    }
  )

  return totals
}

/**
 * Exports payroll run data to Excel
 */
export function exportPayrollRunToExcel(data: PayrollExportData): void {
  const { payrollRun, transactions } = data

  // Generate filename
  const monthYear = `${String(payrollRun.payrollMonth).padStart(2, '0')}-${payrollRun.payrollYear}`
  const filename = `Payroll_${payrollRun.companyName || 'Company'}_${monthYear}`

  // Create summary sheet
  const summarySheet = createSummarySheet(payrollRun)

  // Create transactions sheet with totals
  const transactionsWithTotals = [...transactions, createTotalsRow(transactions)]
  const transactionsSheet: ExcelSheet = {
    name: 'Transactions',
    columns: createTransactionColumns(),
    data: transactionsWithTotals,
  }

  // Export to Excel
  exportToExcel({
    filename,
    sheets: [summarySheet, transactionsSheet],
  })
}

