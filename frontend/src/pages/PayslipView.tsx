import { useParams, useNavigate } from 'react-router-dom'
import { usePayrollTransaction, usePayrollInfo } from '@/features/payroll/hooks'
import { useEmployees } from '@/hooks/api/useEmployees'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { formatINR } from '@/lib/currency'
import { ArrowLeft } from 'lucide-react'
import { format } from 'date-fns'
import { PayslipPDFDownload } from '@/components/payroll/PayslipPDF'

const PayslipView = () => {
  const { transactionId } = useParams<{ transactionId: string }>()
  const navigate = useNavigate()

  const { data: transaction, isLoading, error } = usePayrollTransaction(transactionId!, !!transactionId)
  const { data: employees = [] } = useEmployees()

  const employee = employees.find((e) => e.id === transaction?.employeeId)

  // Fetch payroll info for statutory identifiers (UAN, PF Account, ESI Number)
  const { data: payrollInfo } = usePayrollInfo(transaction?.employeeId || '', !!transaction?.employeeId)
  // Company name is now populated by the backend in the transaction DTO
  const companyName = transaction?.companyName

  const getMonthYear = (month: number, year: number) => {
    const date = new Date(year, month - 1, 1)
    return format(date, 'MMMM yyyy')
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="h-8 bg-gray-200 rounded animate-pulse w-1/4"></div>
        <div className="h-64 bg-gray-200 rounded animate-pulse"></div>
      </div>
    )
  }

  if (error || !transaction) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
        <div className="text-center py-8 text-gray-500">Payslip not found</div>
      </div>
    )
  }

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
        <PayslipPDFDownload
          transaction={transaction}
          employee={employee}
          company={companyName ? { name: companyName } : undefined}
        />
      </div>

      {/* Payslip Card */}
      <Card>
        <CardHeader>
          <div className="text-center">
            <CardTitle className="text-2xl">{companyName || 'Company Name'}</CardTitle>
            <p className="text-sm text-gray-600 mt-2">Salary Slip for {getMonthYear(transaction.payrollMonth, transaction.payrollYear)}</p>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Employee Details */}
          <div className="grid grid-cols-2 gap-4 border-b pb-4">
            <div>
              <div className="text-sm text-gray-500">Employee Name</div>
              <div className="font-semibold">{transaction.employeeName || employee?.employeeName || '—'}</div>
            </div>
            <div>
              <div className="text-sm text-gray-500">Employee ID</div>
              <div className="font-semibold">{employee?.employeeId || '—'}</div>
            </div>
            <div>
              <div className="text-sm text-gray-500">PAN Number</div>
              <div className="font-semibold">{payrollInfo?.panNumber || employee?.panNumber || '—'}</div>
            </div>
            <div>
              <div className="text-sm text-gray-500">UAN</div>
              <div className="font-semibold">{payrollInfo?.uan || '—'}</div>
            </div>
            {payrollInfo?.pfAccountNumber && (
              <div>
                <div className="text-sm text-gray-500">PF Account No</div>
                <div className="font-semibold">{payrollInfo.pfAccountNumber}</div>
              </div>
            )}
            {payrollInfo?.esiNumber && (
              <div>
                <div className="text-sm text-gray-500">ESI Number</div>
                <div className="font-semibold">{payrollInfo.esiNumber}</div>
              </div>
            )}
          </div>

          {/* Attendance */}
          <div className="grid grid-cols-3 gap-4 border-b pb-4">
            <div>
              <div className="text-sm text-gray-500">Working Days</div>
              <div className="font-semibold">{transaction.workingDays}</div>
            </div>
            <div>
              <div className="text-sm text-gray-500">Present Days</div>
              <div className="font-semibold">{transaction.presentDays}</div>
            </div>
            <div>
              <div className="text-sm text-gray-500">LOP Days</div>
              <div className="font-semibold">{transaction.lopDays}</div>
            </div>
          </div>

          {/* Earnings */}
          <div>
            <h3 className="font-semibold mb-3">Earnings</h3>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span>Basic Salary</span>
                <span className="font-medium">{formatINR(transaction.basicEarned)}</span>
              </div>
              <div className="flex justify-between">
                <span>HRA</span>
                <span className="font-medium">{formatINR(transaction.hraEarned)}</span>
              </div>
              <div className="flex justify-between">
                <span>Dearness Allowance</span>
                <span className="font-medium">{formatINR(transaction.daEarned)}</span>
              </div>
              <div className="flex justify-between">
                <span>Conveyance Allowance</span>
                <span className="font-medium">{formatINR(transaction.conveyanceEarned)}</span>
              </div>
              <div className="flex justify-between">
                <span>Medical Allowance</span>
                <span className="font-medium">{formatINR(transaction.medicalEarned)}</span>
              </div>
              <div className="flex justify-between">
                <span>Special Allowance</span>
                <span className="font-medium">{formatINR(transaction.specialAllowanceEarned)}</span>
              </div>
              {transaction.bonusPaid > 0 && (
                <div className="flex justify-between">
                  <span>Bonus</span>
                  <span className="font-medium">{formatINR(transaction.bonusPaid)}</span>
                </div>
              )}
              {transaction.arrears > 0 && (
                <div className="flex justify-between">
                  <span>Arrears</span>
                  <span className="font-medium">{formatINR(transaction.arrears)}</span>
                </div>
              )}
              <div className="flex justify-between border-t pt-2 mt-2">
                <span className="font-semibold">Gross Earnings</span>
                <span className="font-bold text-lg">{formatINR(transaction.grossEarnings)}</span>
              </div>
            </div>
          </div>

          {/* Deductions */}
          <div>
            <h3 className="font-semibold mb-3">Deductions</h3>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span>PF (Employee)</span>
                <span className="font-medium">{formatINR(transaction.pfEmployee)}</span>
              </div>
              <div className="flex justify-between">
                <span>ESI (Employee)</span>
                <span className="font-medium">{formatINR(transaction.esiEmployee)}</span>
              </div>
              <div className="flex justify-between">
                <span>Professional Tax</span>
                <span className="font-medium">{formatINR(transaction.professionalTax)}</span>
              </div>
              <div className="flex justify-between">
                <span>TDS</span>
                <span className="font-medium">{formatINR(transaction.tdsDeducted)}</span>
              </div>
              {transaction.loanRecovery > 0 && (
                <div className="flex justify-between">
                  <span>Loan Recovery</span>
                  <span className="font-medium">{formatINR(transaction.loanRecovery)}</span>
                </div>
              )}
              {transaction.otherDeductions > 0 && (
                <div className="flex justify-between">
                  <span>Other Deductions</span>
                  <span className="font-medium">{formatINR(transaction.otherDeductions)}</span>
                </div>
              )}
              <div className="flex justify-between border-t pt-2 mt-2">
                <span className="font-semibold">Total Deductions</span>
                <span className="font-bold text-lg">{formatINR(transaction.totalDeductions)}</span>
              </div>
            </div>
          </div>

          {/* Net Pay */}
          <div className="bg-blue-50 p-4 rounded-lg">
            <div className="flex justify-between items-center">
              <span className="text-lg font-semibold">Net Payable</span>
              <span className="text-2xl font-bold text-blue-700">{formatINR(transaction.netPayable)}</span>
            </div>
          </div>

          {/* Employer Contributions (Informational) */}
          <div className="border-t pt-4">
            <h3 className="font-semibold mb-3 text-sm text-gray-600">Employer Contributions (Informational)</h3>
            <div className="grid grid-cols-2 gap-4 text-sm">
              <div className="flex justify-between">
                <span>PF (Employer)</span>
                <span>{formatINR(transaction.pfEmployer)}</span>
              </div>
              <div className="flex justify-between">
                <span>ESI (Employer)</span>
                <span>{formatINR(transaction.esiEmployer)}</span>
              </div>
              <div className="flex justify-between">
                <span>Gratuity Provision</span>
                <span>{formatINR(transaction.gratuityProvision)}</span>
              </div>
              <div className="flex justify-between">
                <span>Total Employer Cost</span>
                <span className="font-semibold">{formatINR(transaction.totalEmployerCost)}</span>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* TODO: Add YTD Summary section when available from API */}
    </div>
  )
}

export default PayslipView




