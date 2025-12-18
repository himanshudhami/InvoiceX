import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  Calendar,
  FileText,
  Laptop,
  ChevronRight,
  Gift,
  TrendingUp,
  Sparkles,
  Bell,
} from 'lucide-react'
import { portalApi } from '@/api'
import { useAuth } from '@/context/AuthContext'
import {
  Badge,
  PageLoader,
  getStatusBadgeVariant,
  GlassCard,
  QuickStat,
} from '@/components/ui'
import { formatDate, formatCurrency, formatDays, getMonthName, getInitials } from '@/utils/format'
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

  const firstName = dashboard?.employee?.firstName || user?.employeeName?.split(' ')[0] || 'Employee'
  const fullName = dashboard?.employee
    ? `${dashboard.employee.firstName} ${dashboard.employee.lastName}`
    : user?.employeeName || 'User'

  return (
    <div className="animate-fade-in space-y-6">
      {/* Hero Section with Greeting */}
      <div className="relative overflow-hidden">
        {/* Gradient Background */}
        <div className="absolute inset-0 gradient-bg-primary rounded-3xl opacity-90" />
        <div className="absolute inset-0 bg-gradient-to-br from-white/10 to-transparent" />

        {/* Content */}
        <div className="relative px-5 py-6 text-white">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-4">
              <div className="relative">
                <div className="flex items-center justify-center w-14 h-14 rounded-2xl bg-white/20 backdrop-blur-sm border border-white/30 text-lg font-bold">
                  {getInitials(fullName)}
                </div>
                <div className="absolute -bottom-1 -right-1 w-5 h-5 bg-green-400 rounded-full border-2 border-white flex items-center justify-center">
                  <Sparkles size={10} className="text-white" />
                </div>
              </div>
              <div>
                <p className="text-sm text-white/70">Welcome back,</p>
                <h1 className="text-2xl font-bold">{firstName}</h1>
                <p className="text-xs text-white/60 mt-0.5">
                  {new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' })}
                </p>
              </div>
            </div>
            <button className="flex items-center justify-center w-10 h-10 rounded-xl bg-white/20 backdrop-blur-sm border border-white/30 transition-colors hover:bg-white/30">
              <Bell size={18} />
            </button>
          </div>
        </div>
      </div>

      {/* Quick Stats Grid */}
      <div className="grid grid-cols-3 gap-3">
        <QuickStat
          label="Leave Pending"
          value={dashboard?.pendingLeaveApplications?.toString() || '0'}
          color="primary"
          icon={<Calendar size={18} className="text-primary-600" />}
        />
        <QuickStat
          label="Assets"
          value={dashboard?.assignedAssets?.toString() || '0'}
          color="success"
          icon={<Laptop size={18} className="text-green-600" />}
        />
        <QuickStat
          label="Payslips"
          value={dashboard?.recentPayslips?.length?.toString() || '0'}
          color="warning"
          icon={<FileText size={18} className="text-yellow-600" />}
        />
      </div>

      {/* Leave Balance Section */}
      <section>
        <SectionHeader title="Leave Balance" linkTo="/leave" />
        <div className="grid grid-cols-2 gap-3">
          {dashboard?.leaveBalance?.slice(0, 4).map((balance) => (
            <LeaveBalanceCard key={balance.leaveTypeId} balance={balance} />
          ))}
          {(!dashboard?.leaveBalance || dashboard.leaveBalance.length === 0) && (
            <div className="col-span-2 text-center py-8 text-gray-400 text-sm">
              No leave balance available
            </div>
          )}
        </div>
      </section>

      {/* Upcoming Holidays */}
      {dashboard?.upcomingHolidays && dashboard.upcomingHolidays.length > 0 && (
        <section>
          <SectionHeader title="Upcoming Holidays" />
          <GlassCard className="divide-y divide-gray-100/50">
            {dashboard.upcomingHolidays.slice(0, 3).map((holiday) => (
              <HolidayItem key={holiday.id} holiday={holiday} />
            ))}
          </GlassCard>
        </section>
      )}

      {/* Recent Payslips */}
      {dashboard?.recentPayslips && dashboard.recentPayslips.length > 0 && (
        <section className="pb-4">
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

  // Dynamic color based on remaining balance
  const getProgressColor = () => {
    if (percentage > 50) return 'bg-green-500'
    if (percentage > 25) return 'bg-yellow-500'
    return 'bg-red-500'
  }

  return (
    <GlassCard className="p-4" hoverEffect>
      <div className="flex items-start justify-between mb-2">
        <span className="text-xs font-bold text-primary-600 bg-primary-50 px-2 py-0.5 rounded-full">
          {balance.leaveTypeCode}
        </span>
        <span className="text-xs font-medium text-gray-500">
          {formatDays(balance.available)} left
        </span>
      </div>
      <p className="text-sm font-semibold text-gray-900 mb-3">{balance.leaveTypeName}</p>
      <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
        <div
          className={`h-full ${getProgressColor()} rounded-full transition-all duration-500`}
          style={{ width: `${Math.min(percentage, 100)}%` }}
        />
      </div>
      <div className="flex justify-between mt-2">
        <span className="text-[10px] text-gray-400">
          {formatDays(balance.taken)} used
        </span>
        <span className="text-[10px] text-gray-400">
          of {formatDays(balance.totalEntitlement)}
        </span>
      </div>
    </GlassCard>
  )
}

function HolidayItem({ holiday }: { holiday: Holiday }) {
  return (
    <div className="flex items-center gap-3 py-3 px-4">
      <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-orange-100 to-orange-50">
        <Gift className="text-orange-500" size={18} />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold text-gray-900 truncate">{holiday.name}</p>
        <p className="text-xs text-gray-500">{formatDate(holiday.date, 'EEEE, dd MMM')}</p>
      </div>
      {holiday.isOptional && (
        <Badge variant="default" className="text-[10px] bg-gray-100">
          Optional
        </Badge>
      )}
    </div>
  )
}

function PayslipCard({ payslip }: { payslip: PayslipSummary }) {
  return (
    <Link to={`/payslips/${payslip.id}`}>
      <GlassCard className="flex items-center gap-3 p-4 touch-feedback" hoverEffect>
        <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-green-100 to-green-50">
          <TrendingUp className="text-green-600" size={18} />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-semibold text-gray-900">
            {getMonthName(payslip.month)} {payslip.year}
          </p>
          <p className="text-xs text-gray-500">
            Net Pay: <span className="font-semibold text-green-600">{formatCurrency(payslip.netPay)}</span>
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Badge variant={getStatusBadgeVariant(payslip.status)}>{payslip.status}</Badge>
          <ChevronRight size={16} className="text-gray-400" />
        </div>
      </GlassCard>
    </Link>
  )
}
