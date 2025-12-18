import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { FileText, ChevronRight, TrendingUp, TrendingDown, Calendar, ChevronDown } from 'lucide-react'
import { portalApi } from '@/api'
import { EmptyState } from '@/components/layout'
import { Badge, PageLoader, getStatusBadgeVariant, GlassCard } from '@/components/ui'
import { formatCurrency, getMonthName } from '@/utils/format'
import type { PayslipSummary } from '@/types'

export function PayslipsPage() {
  const currentYear = new Date().getFullYear()
  const [selectedYear, setSelectedYear] = useState(currentYear)
  const [showYearPicker, setShowYearPicker] = useState(false)

  const years = Array.from({ length: 5 }, (_, i) => currentYear - i)

  const { data: payslips, isLoading } = useQuery<PayslipSummary[]>({
    queryKey: ['my-payslips', selectedYear],
    queryFn: () => portalApi.getMyPayslips(selectedYear),
  })

  if (isLoading) {
    return <PageLoader />
  }

  // Group payslips by year for summary
  const totalEarnings = payslips?.reduce((sum, p) => sum + (p.grossEarnings || 0), 0) || 0
  const totalDeductions = payslips?.reduce((sum, p) => sum + (p.totalDeductions || 0), 0) || 0
  const totalNetPay = payslips?.reduce((sum, p) => sum + (p.netPay || 0), 0) || 0

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center justify-between mb-2">
          <h1 className="text-2xl font-bold text-gray-900">Payslips</h1>
          <div className="relative">
            <button
              onClick={() => setShowYearPicker(!showYearPicker)}
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-gray-700 bg-white/70 backdrop-blur-sm border border-gray-200/50 rounded-xl hover:bg-white/80 transition-all"
            >
              <Calendar size={16} />
              {selectedYear}
              <ChevronDown size={14} className={`transition-transform ${showYearPicker ? 'rotate-180' : ''}`} />
            </button>
            {showYearPicker && (
              <div className="absolute right-0 top-12 z-20 bg-white rounded-xl shadow-lg border border-gray-100 overflow-hidden animate-fade-in">
                {years.map((year) => (
                  <button
                    key={year}
                    onClick={() => {
                      setSelectedYear(year)
                      setShowYearPicker(false)
                    }}
                    className={`block w-full px-6 py-2.5 text-sm text-left hover:bg-gray-50 transition-colors ${
                      year === selectedYear ? 'bg-primary-50 text-primary-700 font-medium' : 'text-gray-700'
                    }`}
                  >
                    {year}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
        <p className="text-sm text-gray-500">View and download your salary slips</p>
      </div>

      {/* Year Summary */}
      {payslips && payslips.length > 0 && (
        <GlassCard className="p-4 mb-6">
          <p className="text-xs text-gray-500 mb-3">Year-to-date Summary ({selectedYear})</p>
          <div className="grid grid-cols-3 gap-4">
            <div>
              <p className="text-xs text-green-600 mb-0.5">Total Earnings</p>
              <p className="text-sm font-bold text-gray-900">{formatCurrency(totalEarnings)}</p>
            </div>
            <div>
              <p className="text-xs text-red-600 mb-0.5">Total Deductions</p>
              <p className="text-sm font-bold text-gray-900">{formatCurrency(totalDeductions)}</p>
            </div>
            <div>
              <p className="text-xs text-primary-600 mb-0.5">Total Net Pay</p>
              <p className="text-sm font-bold text-gray-900">{formatCurrency(totalNetPay)}</p>
            </div>
          </div>
        </GlassCard>
      )}

      {/* Payslips List */}
      {!payslips || payslips.length === 0 ? (
        <EmptyState
          icon={<FileText className="text-gray-400" size={24} />}
          title="No payslips"
          description={`No payslips found for ${selectedYear}`}
        />
      ) : (
        <div className="space-y-3">
          {payslips.map((payslip) => (
            <PayslipCard key={payslip.id} payslip={payslip} />
          ))}
        </div>
      )}
    </div>
  )
}

function PayslipCard({ payslip }: { payslip: PayslipSummary }) {
  return (
    <Link to={`/payslips/${payslip.id}`}>
      <GlassCard className="p-4 touch-feedback" hoverEffect>
        <div className="flex items-start justify-between mb-3">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-primary-100 to-primary-50">
              <FileText size={18} className="text-primary-600" />
            </div>
            <div>
              <p className="text-base font-semibold text-gray-900">
                {getMonthName(payslip.month)} {payslip.year}
              </p>
              <Badge variant={getStatusBadgeVariant(payslip.status)} size="sm" className="mt-1">
                {payslip.status}
              </Badge>
            </div>
          </div>
          <div className="text-right">
            <p className="text-lg font-bold text-gray-900">{formatCurrency(payslip.netPay)}</p>
            <p className="text-xs text-gray-500">Net Pay</p>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div className="flex items-center gap-2 p-2 rounded-lg bg-green-50/80">
            <TrendingUp size={14} className="text-green-600" />
            <div>
              <p className="text-[10px] text-green-600">Earnings</p>
              <p className="text-sm font-semibold text-green-700">
                {formatCurrency(payslip.grossEarnings)}
              </p>
            </div>
          </div>
          <div className="flex items-center gap-2 p-2 rounded-lg bg-red-50/80">
            <TrendingDown size={14} className="text-red-600" />
            <div>
              <p className="text-[10px] text-red-600">Deductions</p>
              <p className="text-sm font-semibold text-red-700">
                {formatCurrency(payslip.totalDeductions)}
              </p>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-end mt-3 text-xs text-primary-600 font-medium">
          <span>View Details</span>
          <ChevronRight size={14} />
        </div>
      </GlassCard>
    </Link>
  )
}
