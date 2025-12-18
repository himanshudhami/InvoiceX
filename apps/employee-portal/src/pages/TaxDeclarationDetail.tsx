import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, Receipt, CheckCircle, Clock, AlertCircle, FileText, ChevronDown, ChevronUp } from 'lucide-react'
import { useState } from 'react'
import { portalApi } from '@/api'
import { PageLoader, Badge, GlassCard } from '@/components/ui'
import { formatCurrency } from '@/utils/format'
import type { TaxDeclarationDetail, TaxDeclarationSection } from '@/types'

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

export function TaxDeclarationDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: declaration, isLoading, error } = useQuery<TaxDeclarationDetail>({
    queryKey: ['tax-declaration-detail', id],
    queryFn: () => portalApi.getTaxDeclarationDetail(id!),
    enabled: !!id,
  })

  if (isLoading) {
    return <PageLoader />
  }

  if (error || !declaration) {
    return (
      <div className="p-4 text-center">
        <p className="text-gray-500">Failed to load tax declaration details</p>
        <Link to="/tax-declarations" className="text-primary-600 hover:underline mt-2 inline-block">
          Go back to declarations
        </Link>
      </div>
    )
  }

  const verifiedPercentage = declaration.totalDeclaredAmount > 0
    ? Math.round((declaration.verifiedAmount / declaration.totalDeclaredAmount) * 100)
    : 0

  return (
    <div className="animate-fade-in pb-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Link
            to="/tax-declarations"
            className="flex items-center justify-center w-10 h-10 rounded-xl bg-white/70 backdrop-blur-sm border border-white/30 text-gray-600 hover:bg-white/80 transition-all"
          >
            <ArrowLeft size={20} />
          </Link>
          <div>
            <h1 className="text-xl font-bold text-gray-900">
              FY {declaration.financialYear}
            </h1>
            <Badge variant={getStatusBadgeVariant(declaration.status)} className="mt-1">
              {declaration.status}
            </Badge>
          </div>
        </div>
      </div>

      {/* Summary Card */}
      <GlassCard className="p-5 mb-6" glow="primary">
        <div className="grid grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-xs text-gray-500 mb-1">Total Declared</p>
            <p className="text-2xl font-bold text-gray-900">
              {formatCurrency(declaration.totalDeclaredAmount)}
            </p>
          </div>
          <div>
            <p className="text-xs text-green-600 mb-1">Verified</p>
            <p className="text-2xl font-bold text-green-600">
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
          <div className="h-2.5 bg-gray-100 rounded-full overflow-hidden">
            <div
              className="h-full bg-gradient-to-r from-green-500 to-emerald-500 rounded-full transition-all duration-500"
              style={{ width: `${verifiedPercentage}%` }}
            />
          </div>
        </div>
      </GlassCard>

      {/* Sections */}
      <div className="space-y-3">
        {declaration.sections.map((section) => (
          <SectionCard key={section.sectionCode} section={section} />
        ))}
      </div>

      {declaration.submittedAt && (
        <p className="text-center text-xs text-gray-400 mt-6">
          Submitted on {new Date(declaration.submittedAt).toLocaleDateString('en-IN', {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
          })}
        </p>
      )}
    </div>
  )
}

function SectionCard({ section }: { section: TaxDeclarationSection }) {
  const [isExpanded, setIsExpanded] = useState(true)

  const sectionVerifiedPercentage = section.declaredAmount > 0
    ? Math.round((section.verifiedAmount / section.declaredAmount) * 100)
    : 0

  const getStatusIcon = () => {
    if (sectionVerifiedPercentage === 100) return <CheckCircle size={16} className="text-green-600" />
    if (sectionVerifiedPercentage > 0) return <Clock size={16} className="text-yellow-600" />
    return <AlertCircle size={16} className="text-gray-400" />
  }

  return (
    <GlassCard className="overflow-hidden">
      <button
        onClick={() => setIsExpanded(!isExpanded)}
        className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50/50 transition-colors"
      >
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-purple-50">
            <Receipt size={18} className="text-purple-600" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <p className="text-sm font-semibold text-gray-900">{section.sectionCode}</p>
              {getStatusIcon()}
            </div>
            <p className="text-xs text-gray-500">{section.sectionName}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <div className="text-right">
            <p className="text-sm font-bold text-gray-900">{formatCurrency(section.declaredAmount)}</p>
            <p className="text-[10px] text-gray-500">of {formatCurrency(section.maxLimit)} max</p>
          </div>
          {isExpanded ? (
            <ChevronUp size={18} className="text-gray-400" />
          ) : (
            <ChevronDown size={18} className="text-gray-400" />
          )}
        </div>
      </button>

      {isExpanded && section.items.length > 0 && (
        <div className="border-t border-gray-100/50 divide-y divide-gray-100/50">
          {section.items.map((item) => (
            <div key={item.id} className="flex items-center justify-between px-4 py-3 bg-gray-50/30">
              <div className="flex items-center gap-3 flex-1 min-w-0">
                <div className="flex items-center justify-center w-8 h-8 rounded-lg bg-gray-100">
                  <FileText size={14} className="text-gray-500" />
                </div>
                <div className="min-w-0">
                  <p className="text-sm text-gray-700 truncate">{item.description}</p>
                  {item.documentPath && (
                    <p className="text-[10px] text-green-600 flex items-center gap-1 mt-0.5">
                      <CheckCircle size={10} />
                      Proof uploaded
                    </p>
                  )}
                </div>
              </div>
              <div className="text-right ml-3">
                <p className="text-sm font-semibold text-gray-900">
                  {formatCurrency(item.declaredAmount)}
                </p>
                {item.verifiedAmount > 0 && (
                  <p className="text-[10px] text-green-600">
                    Verified: {formatCurrency(item.verifiedAmount)}
                  </p>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {isExpanded && section.items.length === 0 && (
        <div className="border-t border-gray-100/50 p-4 text-center text-sm text-gray-400">
          No items declared in this section
        </div>
      )}
    </GlassCard>
  )
}
