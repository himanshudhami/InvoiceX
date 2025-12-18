import { useParams, Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowLeft, TrendingUp, TrendingDown, Calendar } from 'lucide-react'
import { portalApi } from '@/api'
import { PageLoader, Badge, getStatusBadgeVariant, GlassCard } from '@/components/ui'
import { PayslipDownloadButton } from '@/components/pdf'
import { formatCurrency, getMonthName } from '@/utils/format'
import type { PayslipDetail } from '@/types'

export function PayslipDetailPage() {
  const { id } = useParams<{ id: string }>()

  const { data: payslip, isLoading, error } = useQuery<PayslipDetail>({
    queryKey: ['payslip-detail', id],
    queryFn: () => portalApi.getPayslipDetail(id!),
    enabled: !!id,
  })

  if (isLoading) {
    return <PageLoader />
  }

  if (error || !payslip) {
    return (
      <div className="p-4 text-center">
        <p className="text-gray-500">Failed to load payslip details</p>
        <Link to="/payslips" className="text-primary-600 hover:underline mt-2 inline-block">
          Go back to payslips
        </Link>
      </div>
    )
  }

  return (
    <div className="animate-fade-in pb-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <Link
            to="/payslips"
            className="flex items-center justify-center w-10 h-10 rounded-xl bg-white/70 backdrop-blur-sm border border-white/30 text-gray-600 hover:bg-white/80 transition-all"
          >
            <ArrowLeft size={20} />
          </Link>
          <div>
            <h1 className="text-xl font-bold text-gray-900">
              {getMonthName(payslip.month)} {payslip.year}
            </h1>
            <Badge variant={getStatusBadgeVariant(payslip.status)} className="mt-1">
              {payslip.status}
            </Badge>
          </div>
        </div>
        <PayslipDownloadButton payslip={payslip} variant="button" />
      </div>

      {/* Employee Info Card */}
      <GlassCard className="p-4 mb-4">
        <div className="flex items-center gap-3 mb-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-100">
            <Calendar size={18} className="text-primary-600" />
          </div>
          <div>
            <p className="text-sm font-semibold text-gray-900">{payslip.employeeName}</p>
            <p className="text-xs text-gray-500">{payslip.employeeCode}</p>
          </div>
        </div>
        <div className="grid grid-cols-2 gap-3 text-sm">
          {payslip.designation && (
            <div>
              <span className="text-gray-500">Designation:</span>
              <span className="ml-1 font-medium">{payslip.designation}</span>
            </div>
          )}
          {payslip.department && (
            <div>
              <span className="text-gray-500">Department:</span>
              <span className="ml-1 font-medium">{payslip.department}</span>
            </div>
          )}
          {payslip.paidOn && (
            <div className="col-span-2">
              <span className="text-gray-500">Paid on:</span>
              <span className="ml-1 font-medium">
                {new Date(payslip.paidOn).toLocaleDateString('en-IN', {
                  day: 'numeric',
                  month: 'long',
                  year: 'numeric',
                })}
              </span>
            </div>
          )}
        </div>
      </GlassCard>

      {/* Net Pay Card */}
      <GlassCard className="p-5 mb-4" glow="primary">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-500 mb-1">Net Pay</p>
            <p className="text-3xl font-bold text-gray-900">{formatCurrency(payslip.netPay)}</p>
          </div>
          <div className="flex items-center justify-center w-14 h-14 rounded-2xl bg-gradient-to-br from-primary-500 to-primary-600">
            <TrendingUp size={24} className="text-white" />
          </div>
        </div>
      </GlassCard>

      {/* Summary Cards */}
      <div className="grid grid-cols-2 gap-3 mb-6">
        <GlassCard className="p-4">
          <div className="flex items-center gap-2 mb-2">
            <TrendingUp size={16} className="text-green-600" />
            <span className="text-xs text-gray-500">Total Earnings</span>
          </div>
          <p className="text-lg font-bold text-green-600">
            {formatCurrency(payslip.grossEarnings)}
          </p>
        </GlassCard>
        <GlassCard className="p-4">
          <div className="flex items-center gap-2 mb-2">
            <TrendingDown size={16} className="text-red-600" />
            <span className="text-xs text-gray-500">Total Deductions</span>
          </div>
          <p className="text-lg font-bold text-red-600">
            {formatCurrency(payslip.totalDeductions)}
          </p>
        </GlassCard>
      </div>

      {/* Earnings Section */}
      <GlassCard className="mb-4">
        <div className="p-4 border-b border-gray-100/50">
          <div className="flex items-center gap-2">
            <TrendingUp size={18} className="text-green-600" />
            <h2 className="font-semibold text-gray-900">Earnings</h2>
          </div>
        </div>
        <div className="divide-y divide-gray-100/50">
          {payslip.earnings.map((earning, index) => (
            <div key={index} className="flex items-center justify-between px-4 py-3">
              <span className="text-sm text-gray-600">{earning.name}</span>
              <span className="text-sm font-semibold text-gray-900">
                {formatCurrency(earning.amount)}
              </span>
            </div>
          ))}
          <div className="flex items-center justify-between px-4 py-3 bg-green-50/50">
            <span className="text-sm font-semibold text-green-700">Total Earnings</span>
            <span className="text-sm font-bold text-green-700">
              {formatCurrency(payslip.grossEarnings)}
            </span>
          </div>
        </div>
      </GlassCard>

      {/* Deductions Section */}
      <GlassCard className="mb-4">
        <div className="p-4 border-b border-gray-100/50">
          <div className="flex items-center gap-2">
            <TrendingDown size={18} className="text-red-600" />
            <h2 className="font-semibold text-gray-900">Deductions</h2>
          </div>
        </div>
        <div className="divide-y divide-gray-100/50">
          {payslip.deductions.map((deduction, index) => (
            <div key={index} className="flex items-center justify-between px-4 py-3">
              <span className="text-sm text-gray-600">{deduction.name}</span>
              <span className="text-sm font-semibold text-gray-900">
                {formatCurrency(deduction.amount)}
              </span>
            </div>
          ))}
          <div className="flex items-center justify-between px-4 py-3 bg-red-50/50">
            <span className="text-sm font-semibold text-red-700">Total Deductions</span>
            <span className="text-sm font-bold text-red-700">
              {formatCurrency(payslip.totalDeductions)}
            </span>
          </div>
        </div>
      </GlassCard>

      {/* Download Button */}
      <div className="mt-6">
        <PayslipDownloadButton payslip={payslip} variant="button" className="w-full justify-center" />
      </div>
    </div>
  )
}
