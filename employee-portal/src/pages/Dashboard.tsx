import React from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Calendar,
  FileText,
  Laptop,
  ChevronRight,
  Gift,
  TrendingUp,
} from 'lucide-react'
import { portalApi } from '@/api'
import { useAuth } from '@/context/AuthContext'
import { Card, Badge, Avatar, PageLoader, getStatusBadgeVariant } from '@/components/ui'
import { formatDate, formatCurrency, formatDays, getMonthName } from '@/utils/format'
import type { PortalDashboard, LeaveBalanceSummary, PayslipSummary, Holiday } from '@/types'

export function DashboardPage() {
  const { user } = useAuth()

  const { data: dashboard, isLoading } = useQuery<PortalDashboard>({
    queryKey: ['portal-dashboard'],
    queryFn: portalApi.getDashboard,
    staleTime: 1000 * 60 * 5, // 5 minutes
  })

  if (isLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      {/* Header with greeting */}
      <div className="bg-primary-600 text-white px-4 pt-6 pb-8 safe-top">
        <div className="flex items-center gap-3 mb-4">
          <Avatar
            name={dashboard?.employee?.firstName + ' ' + dashboard?.employee?.lastName || 'User'}
            size="lg"
          />
          <div>
            <p className="text-sm text-primary-100">Welcome back,</p>
            <h1 className="text-xl font-semibold">
              {dashboard?.employee?.firstName || user?.employeeName?.split(' ')[0] || 'Employee'}
            </h1>
          </div>
        </div>
      </div>

      {/* Quick Stats */}
      <div className="px-4 -mt-4">
        <Card className="grid grid-cols-3 gap-4 p-4">
          <QuickStatItem
            icon={<Calendar className="text-primary-600" size={20} />}
            label="Leave Pending"
            value={dashboard?.pendingLeaveApplications?.toString() || '0'}
          />
          <QuickStatItem
            icon={<Laptop className="text-green-600" size={20} />}
            label="Assets"
            value={dashboard?.assignedAssets?.toString() || '0'}
          />
          <QuickStatItem
            icon={<FileText className="text-orange-600" size={20} />}
            label="Payslips"
            value={dashboard?.recentPayslips?.length?.toString() || '0'}
          />
        </Card>
      </div>

      {/* Leave Balance */}
      <section className="px-4 mt-6">
        <SectionHeader title="Leave Balance" linkTo="/leave" />
        <div className="grid grid-cols-2 gap-3">
          {dashboard?.leaveBalance?.slice(0, 4).map((balance) => (
            <LeaveBalanceCard key={balance.leaveTypeId} balance={balance} />
          ))}
        </div>
      </section>

      {/* Upcoming Holidays */}
      {dashboard?.upcomingHolidays && dashboard.upcomingHolidays.length > 0 && (
        <section className="px-4 mt-6">
          <SectionHeader title="Upcoming Holidays" />
          <Card className="divide-y divide-gray-100">
            {dashboard.upcomingHolidays.slice(0, 3).map((holiday) => (
              <HolidayItem key={holiday.id} holiday={holiday} />
            ))}
          </Card>
        </section>
      )}

      {/* Recent Payslips */}
      {dashboard?.recentPayslips && dashboard.recentPayslips.length > 0 && (
        <section className="px-4 mt-6 pb-6">
          <SectionHeader title="Recent Payslips" linkTo="/payslips" />
          <div className="space-y-3">
            {dashboard.recentPayslips.slice(0, 3).map((payslip) => (
              <PayslipCard key={payslip.id} payslip={payslip} />
            ))}
          </div>
        </section>
      )}
    </div>
  )
}

interface QuickStatItemProps {
  icon: React.ReactNode
  label: string
  value: string
}

function QuickStatItem({ icon, label, value }: QuickStatItemProps) {
  return (
    <div className="flex flex-col items-center text-center">
      <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gray-50 mb-2">
        {icon}
      </div>
      <span className="text-lg font-semibold text-gray-900">{value}</span>
      <span className="text-xs text-gray-500">{label}</span>
    </div>
  )
}

interface SectionHeaderProps {
  title: string
  linkTo?: string
}

function SectionHeader({ title, linkTo }: SectionHeaderProps) {
  return (
    <div className="flex items-center justify-between mb-3">
      <h2 className="text-base font-semibold text-gray-900">{title}</h2>
      {linkTo && (
        <Link
          to={linkTo}
          className="flex items-center text-sm text-primary-600 font-medium touch-feedback"
        >
          View All
          <ChevronRight size={16} />
        </Link>
      )}
    </div>
  )
}

function LeaveBalanceCard({ balance }: { balance: LeaveBalanceSummary }) {
  const percentage = balance.totalEntitlement > 0
    ? Math.round((balance.available / balance.totalEntitlement) * 100)
    : 0

  return (
    <Card className="p-3">
      <div className="flex items-start justify-between mb-2">
        <span className="text-xs font-medium text-gray-500">{balance.leaveTypeCode}</span>
        <Badge variant="info" className="text-[10px]">
          {formatDays(balance.available)} left
        </Badge>
      </div>
      <p className="text-sm font-medium text-gray-900 mb-2">{balance.leaveTypeName}</p>
      <div className="h-1.5 bg-gray-100 rounded-full overflow-hidden">
        <div
          className="h-full bg-primary-500 rounded-full transition-all"
          style={{ width: `${Math.min(percentage, 100)}%` }}
        />
      </div>
      <p className="text-[10px] text-gray-400 mt-1">
        {formatDays(balance.taken)} used of {formatDays(balance.totalEntitlement)}
      </p>
    </Card>
  )
}

function HolidayItem({ holiday }: { holiday: Holiday }) {
  return (
    <div className="flex items-center gap-3 py-3 px-4">
      <div className="flex items-center justify-center w-10 h-10 rounded-full bg-orange-50">
        <Gift className="text-orange-500" size={18} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-900 truncate">{holiday.name}</p>
        <p className="text-xs text-gray-500">{formatDate(holiday.date, 'EEEE, dd MMM')}</p>
      </div>
      {holiday.isOptional && (
        <Badge variant="default" className="text-[10px]">
          Optional
        </Badge>
      )}
    </div>
  )
}

function PayslipCard({ payslip }: { payslip: PayslipSummary }) {
  return (
    <Link to={`/payslips/${payslip.id}`}>
      <Card className="flex items-center gap-3 p-4 touch-feedback">
        <div className="flex items-center justify-center w-10 h-10 rounded-full bg-green-50">
          <TrendingUp className="text-green-600" size={18} />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-gray-900">
            {getMonthName(payslip.month)} {payslip.year}
          </p>
          <p className="text-xs text-gray-500">
            Net Pay: {formatCurrency(payslip.netPay)}
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={getStatusBadgeVariant(payslip.status)}>{payslip.status}</Badge>
          <ChevronRight size={16} className="text-gray-400" />
        </div>
      </Card>
    </Link>
  )
}
