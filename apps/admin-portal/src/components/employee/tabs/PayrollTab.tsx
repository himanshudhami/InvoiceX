import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { usePayrollInfo } from '@/features/payroll/hooks/usePayrollInfo'
import { useCurrentSalaryStructure } from '@/features/payroll/hooks/useSalaryStructures'
import { formatINR } from '@/lib/currency'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ExternalLink, Loader2 } from 'lucide-react'

interface PayrollTabProps {
  employeeId: string
}

export const PayrollTab: FC<PayrollTabProps> = ({ employeeId }) => {
  const navigate = useNavigate()
  const { data: payrollInfo, isLoading: loadingPayrollInfo } = usePayrollInfo(employeeId)
  const { data: salaryStructure, isLoading: loadingSalary } = useCurrentSalaryStructure(employeeId)

  if (loadingPayrollInfo || loadingSalary) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    )
  }

  return (
    <div className="space-y-6 p-4">
      {/* Payroll Info */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-semibold text-gray-900">Payroll Information</h3>
        </div>

        {payrollInfo ? (
          <div className="bg-gray-50 rounded-lg p-4 space-y-3">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <div className="text-xs text-gray-500 uppercase">Payroll Type</div>
                <Badge variant={payrollInfo.payrollType === 'employee' ? 'default' : 'secondary'}>
                  {payrollInfo.payrollType}
                </Badge>
              </div>
              <div>
                <div className="text-xs text-gray-500 uppercase">Tax Regime</div>
                <div className="text-sm font-medium">{payrollInfo.taxRegime || '—'}</div>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4 pt-2 border-t">
              <div>
                <div className="text-xs text-gray-500 uppercase">PF</div>
                <Badge variant={payrollInfo.isPfApplicable ? 'default' : 'outline'}>
                  {payrollInfo.isPfApplicable ? 'Yes' : 'No'}
                </Badge>
              </div>
              <div>
                <div className="text-xs text-gray-500 uppercase">ESI</div>
                <Badge variant={payrollInfo.isEsiApplicable ? 'default' : 'outline'}>
                  {payrollInfo.isEsiApplicable ? 'Yes' : 'No'}
                </Badge>
              </div>
              <div>
                <div className="text-xs text-gray-500 uppercase">PT</div>
                <Badge variant={payrollInfo.isPtApplicable ? 'default' : 'outline'}>
                  {payrollInfo.isPtApplicable ? 'Yes' : 'No'}
                </Badge>
              </div>
            </div>

            {(payrollInfo.uan || payrollInfo.pfAccountNumber || payrollInfo.esiNumber) && (
              <div className="pt-2 border-t space-y-2">
                {payrollInfo.uan && (
                  <div>
                    <div className="text-xs text-gray-500 uppercase">UAN</div>
                    <div className="text-sm font-mono">{payrollInfo.uan}</div>
                  </div>
                )}
                {payrollInfo.pfAccountNumber && (
                  <div>
                    <div className="text-xs text-gray-500 uppercase">PF Account</div>
                    <div className="text-sm font-mono">{payrollInfo.pfAccountNumber}</div>
                  </div>
                )}
                {payrollInfo.esiNumber && (
                  <div>
                    <div className="text-xs text-gray-500 uppercase">ESI Number</div>
                    <div className="text-sm font-mono">{payrollInfo.esiNumber}</div>
                  </div>
                )}
              </div>
            )}
          </div>
        ) : (
          <div className="bg-gray-50 rounded-lg p-4 text-center text-gray-500 text-sm">
            No payroll information configured
          </div>
        )}
      </section>

      {/* Salary Structure */}
      <section>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-sm font-semibold text-gray-900">Current Salary Structure</h3>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate(`/payroll/salary-structures?employeeId=${employeeId}`)}
          >
            <ExternalLink className="w-3 h-3 mr-1" />
            Manage
          </Button>
        </div>

        {salaryStructure ? (
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="text-center mb-4">
              <div className="text-xs text-gray-500 uppercase">Annual CTC</div>
              <div className="text-2xl font-bold text-gray-900">{formatINR(salaryStructure.annualCtc)}</div>
              <div className="text-sm text-gray-500">
                Monthly Gross: {formatINR(salaryStructure.monthlyGross)}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3 text-sm pt-3 border-t">
              <div>
                <div className="text-xs text-gray-500">Basic</div>
                <div className="font-medium">{formatINR(salaryStructure.basicSalary)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">HRA</div>
                <div className="font-medium">{formatINR(salaryStructure.hra)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">DA</div>
                <div className="font-medium">{formatINR(salaryStructure.dearnessAllowance || 0)}</div>
              </div>
              <div>
                <div className="text-xs text-gray-500">Special Allowance</div>
                <div className="font-medium">{formatINR(salaryStructure.specialAllowance || 0)}</div>
              </div>
            </div>
          </div>
        ) : (
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="text-center text-gray-500 text-sm mb-3">
              No salary structure configured
            </div>
            <Button
              variant="outline"
              size="sm"
              className="w-full"
              onClick={() => navigate(`/payroll/salary-structures?employeeId=${employeeId}`)}
            >
              Add Salary Structure
            </Button>
          </div>
        )}
      </section>
    </div>
  )
}
