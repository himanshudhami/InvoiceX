import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTaxDeclarations } from '@/features/payroll/hooks/useTaxDeclarations'
import { formatINR } from '@/lib/currency'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ExternalLink, Loader2, FileText, Plus } from 'lucide-react'
import { format } from 'date-fns'

interface TaxDeclarationsTabProps {
  employeeId: string
}

const getStatusBadgeVariant = (status: string) => {
  switch (status) {
    case 'verified':
      return 'default'
    case 'submitted':
      return 'secondary'
    case 'rejected':
      return 'destructive'
    case 'locked':
      return 'outline'
    default:
      return 'outline'
  }
}

export const TaxDeclarationsTab: FC<TaxDeclarationsTabProps> = ({ employeeId }) => {
  const navigate = useNavigate()
  const { data: declarations, isLoading } = useTaxDeclarations({ employeeId })

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    )
  }

  const declarationsList = declarations?.items || []

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-gray-900">Tax Declarations</h3>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate(`/payroll/tax-declarations?employeeId=${employeeId}`)}
        >
          <ExternalLink className="w-3 h-3 mr-1" />
          Manage
        </Button>
      </div>

      {declarationsList.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-6 text-center">
          <FileText className="w-10 h-10 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500 text-sm mb-3">No tax declarations found</p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate(`/payroll/tax-declarations?employeeId=${employeeId}`)}
          >
            <Plus className="w-3 h-3 mr-1" />
            Add Declaration
          </Button>
        </div>
      ) : (
        <div className="space-y-3">
          {declarationsList.map((declaration) => {
            const total80C =
              (declaration.sec80cPpf || 0) +
              (declaration.sec80cElss || 0) +
              (declaration.sec80cLifeInsurance || 0) +
              (declaration.sec80cHomeLoanPrincipal || 0) +
              (declaration.sec80cChildrenTuition || 0) +
              (declaration.sec80cOthers || 0)

            const total80D =
              (declaration.sec80dSelfFamily || 0) +
              (declaration.sec80dParents || 0) +
              (declaration.sec80dPreventiveCheckup || 0)

            return (
              <div
                key={declaration.id}
                className="bg-gray-50 rounded-lg p-4 hover:bg-gray-100 cursor-pointer transition-colors"
                onClick={() => navigate(`/payroll/tax-declarations?employeeId=${employeeId}`)}
              >
                <div className="flex items-center justify-between mb-2">
                  <div className="font-medium text-gray-900">FY {declaration.financialYear}</div>
                  <Badge variant={getStatusBadgeVariant(declaration.status)}>
                    {declaration.status}
                  </Badge>
                </div>

                <div className="flex items-center gap-2 text-xs text-gray-500 mb-3">
                  <Badge variant="outline" className="text-xs">
                    {declaration.taxRegime === 'new' ? 'New Regime' : 'Old Regime'}
                  </Badge>
                  {declaration.submittedAt && (
                    <span>Submitted {format(new Date(declaration.submittedAt), 'MMM dd, yyyy')}</span>
                  )}
                </div>

                <div className="grid grid-cols-2 gap-3 text-sm">
                  <div>
                    <div className="text-xs text-gray-500">80C Total</div>
                    <div className="font-medium">{formatINR(total80C)}</div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">80D Total</div>
                    <div className="font-medium">{formatINR(total80D)}</div>
                  </div>
                  {declaration.hraRentPaidAnnual && declaration.hraRentPaidAnnual > 0 && (
                    <div className="col-span-2">
                      <div className="text-xs text-gray-500">Annual Rent (HRA)</div>
                      <div className="font-medium">{formatINR(declaration.hraRentPaidAnnual)}</div>
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
