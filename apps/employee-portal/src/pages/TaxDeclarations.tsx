import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Receipt, ChevronRight, CheckCircle, Clock, AlertCircle, IndianRupee } from 'lucide-react'
import { portalApi } from '@/api'
import { EmptyState } from '@/components/layout'
import { Badge, PageLoader, GlassCard } from '@/components/ui'
import { formatCurrency } from '@/utils/format'
import type { TaxDeclarationSummary } from '@/types'

const getStatusIcon = (status: string) => {
  switch (status.toLowerCase()) {
    case 'verified':
    case 'approved':
      return <CheckCircle size={16} className="text-green-600" />
    case 'pending':
    case 'submitted':
      return <Clock size={16} className="text-yellow-600" />
    case 'rejected':
      return <AlertCircle size={16} className="text-red-600" />
    default:
      return <Clock size={16} className="text-gray-400" />
  }
}

const getStatusBadgeVariant = (status: string) => {
  switch (status.toLowerCase()) {
    case 'verified':
    case 'approved':
      return 'approved'
    case 'pending':
    case 'submitted':
      return 'pending'
    case 'rejected':
      return 'rejected'
    default:
      return 'default'
  }
}

export function TaxDeclarationsPage() {
  const { data: declarations, isLoading } = useQuery<TaxDeclarationSummary[]>({
    queryKey: ['my-tax-declarations'],
    queryFn: portalApi.getMyTaxDeclarations,
  })

  if (isLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Tax Declarations</h1>
        <p className="text-sm text-gray-500">
          View and manage your income tax investment declarations
        </p>
      </div>

      {/* Info Card */}
      <GlassCard className="p-4 mb-6" glow="primary">
        <div className="flex items-start gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-100">
            <IndianRupee size={18} className="text-primary-600" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Tax Saving Tips</h3>
            <p className="text-xs text-gray-500 mt-1">
              Submit your investment proofs before the deadline to maximize tax benefits under sections 80C, 80D, and HRA.
            </p>
          </div>
        </div>
      </GlassCard>

      {/* Declarations List */}
      {!declarations || declarations.length === 0 ? (
        <EmptyState
          icon={<Receipt className="text-gray-400" size={24} />}
          title="No tax declarations"
          description="Your tax declarations will appear here once available"
        />
      ) : (
        <div className="space-y-3">
          {declarations.map((declaration) => (
            <TaxDeclarationCard key={declaration.id} declaration={declaration} />
          ))}
        </div>
      )}
    </div>
  )
}

function TaxDeclarationCard({ declaration }: { declaration: TaxDeclarationSummary }) {
  const verifiedPercentage = declaration.totalDeclaredAmount > 0
    ? Math.round((declaration.verifiedAmount / declaration.totalDeclaredAmount) * 100)
    : 0

  return (
    <Link to={`/tax-declarations/${declaration.id}`}>
      <GlassCard className="p-4 touch-feedback" hoverEffect>
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-purple-100 to-purple-50">
              <Receipt size={18} className="text-purple-600" />
            </div>
            <div>
              <p className="text-base font-semibold text-gray-900">
                FY {declaration.financialYear}
              </p>
              <div className="flex items-center gap-1.5 mt-1">
                {getStatusIcon(declaration.status)}
                <Badge variant={getStatusBadgeVariant(declaration.status)} size="sm">
                  {declaration.status}
                </Badge>
              </div>
            </div>
          </div>
          <ChevronRight size={18} className="text-gray-400" />
        </div>

        <div className="grid grid-cols-2 gap-3 mb-3">
          <div className="p-3 rounded-xl bg-gray-50/80">
            <p className="text-[10px] text-gray-500 mb-0.5">Declared</p>
            <p className="text-sm font-bold text-gray-900">
              {formatCurrency(declaration.totalDeclaredAmount)}
            </p>
          </div>
          <div className="p-3 rounded-xl bg-green-50/80">
            <p className="text-[10px] text-green-600 mb-0.5">Verified</p>
            <p className="text-sm font-bold text-green-700">
              {formatCurrency(declaration.verifiedAmount)}
            </p>
          </div>
        </div>

        {/* Progress Bar */}
        <div className="space-y-1">
          <div className="flex justify-between text-xs">
            <span className="text-gray-500">Verification Progress</span>
            <span className="font-medium text-gray-700">{verifiedPercentage}%</span>
          </div>
          <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-green-500 to-emerald-500 rounded-full transition-all duration-500"
              style={{ width: `${verifiedPercentage}%` }}
            />
          </div>
        </div>

        {declaration.submittedAt && (
          <p className="text-xs text-gray-400 mt-3">
            Submitted on {new Date(declaration.submittedAt).toLocaleDateString('en-IN', {
              day: 'numeric',
              month: 'short',
              year: 'numeric',
            })}
          </p>
        )}
      </GlassCard>
    </Link>
  )
}
