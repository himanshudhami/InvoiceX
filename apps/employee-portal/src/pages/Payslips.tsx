import React from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { FileText, Download, ChevronRight, TrendingUp, TrendingDown } from 'lucide-react'
import { portalApi } from '@/api'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, PageLoader, getStatusBadgeVariant } from '@/components/ui'
import { formatCurrency, getMonthName } from '@/utils/format'
import type { PayslipSummary } from '@/types'

export function PayslipsPage() {
  const currentYear = new Date().getFullYear()

  const { data: payslips, isLoading } = useQuery<PayslipSummary[]>({
    queryKey: ['my-payslips', currentYear],
    queryFn: () => portalApi.getMyPayslips(currentYear),
  })

  if (isLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader title="Payslips" />

      <div className="px-4 py-4">
        {!payslips || payslips.length === 0 ? (
          <EmptyState
            icon={<FileText className="text-gray-400" size={24} />}
            title="No payslips"
            description="Your payslips will appear here once available"
          />
        ) : (
          <div className="space-y-3">
            {payslips.map((payslip) => (
              <PayslipCard key={payslip.id} payslip={payslip} />
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

function PayslipCard({ payslip }: { payslip: PayslipSummary }) {
  const handleDownload = async (e: React.MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
    try {
      const blob = await portalApi.downloadPayslipPdf(payslip.id)
      const url = window.URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `payslip-${getMonthName(payslip.month)}-${payslip.year}.pdf`
      document.body.appendChild(a)
      a.click()
      window.URL.revokeObjectURL(url)
      document.body.removeChild(a)
    } catch (error) {
      console.error('Failed to download payslip:', error)
    }
  }

  return (
    <Link to={`/payslips/${payslip.id}`}>
      <Card className="p-4 touch-feedback">
        <div className="flex items-start justify-between mb-3">
          <div>
            <p className="text-base font-semibold text-gray-900">
              {getMonthName(payslip.month)} {payslip.year}
            </p>
            <Badge variant={getStatusBadgeVariant(payslip.status)} className="mt-1">
              {payslip.status}
            </Badge>
          </div>
          <button
            onClick={handleDownload}
            className="flex items-center justify-center w-10 h-10 rounded-full bg-primary-50 text-primary-600 touch-feedback"
            aria-label="Download PDF"
          >
            <Download size={18} />
          </button>
        </div>

        <div className="grid grid-cols-3 gap-3 text-center">
          <div className="p-2 rounded-lg bg-green-50">
            <div className="flex items-center justify-center gap-1 mb-1">
              <TrendingUp size={12} className="text-green-600" />
              <span className="text-[10px] text-green-700">Earnings</span>
            </div>
            <p className="text-sm font-semibold text-green-700">
              {formatCurrency(payslip.grossEarnings)}
            </p>
          </div>
          <div className="p-2 rounded-lg bg-red-50">
            <div className="flex items-center justify-center gap-1 mb-1">
              <TrendingDown size={12} className="text-red-600" />
              <span className="text-[10px] text-red-700">Deductions</span>
            </div>
            <p className="text-sm font-semibold text-red-700">
              {formatCurrency(payslip.totalDeductions)}
            </p>
          </div>
          <div className="p-2 rounded-lg bg-primary-50">
            <div className="flex items-center justify-center gap-1 mb-1">
              <span className="text-[10px] text-primary-700">Net Pay</span>
            </div>
            <p className="text-sm font-semibold text-primary-700">
              {formatCurrency(payslip.netPay)}
            </p>
          </div>
        </div>

        <div className="flex items-center justify-end mt-3 text-xs text-gray-400">
          <span>View Details</span>
          <ChevronRight size={14} />
        </div>
      </Card>
    </Link>
  )
}
