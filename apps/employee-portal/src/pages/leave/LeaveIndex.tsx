import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import * as Tabs from '@radix-ui/react-tabs'
import { Plus, Calendar, Clock, Gift, ChevronRight } from 'lucide-react'
import { leaveApi } from '@/api'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, Button, PageLoader, getStatusBadgeVariant } from '@/components/ui'
import { formatDate, formatDays } from '@/utils/format'
import { cn } from '@/utils/cn'
import type { LeaveDashboard, LeaveBalance, LeaveApplicationSummary, Holiday } from '@/types'

export function LeaveIndexPage() {
  const [activeTab, setActiveTab] = useState('balance')

  const { data: dashboard, isLoading } = useQuery<LeaveDashboard>({
    queryKey: ['leave-dashboard'],
    queryFn: leaveApi.getDashboard,
  })

  if (isLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="Leave"
        rightContent={
          <Link to="/leave/apply">
            <Button size="sm">
              <Plus size={18} className="mr-1" />
              Apply
            </Button>
          </Link>
        }
      />

      {/* Quick Stats */}
      <div className="px-4 py-4 grid grid-cols-2 gap-3">
        <Card className="p-3 flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-yellow-50">
            <Clock className="text-yellow-600" size={20} />
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{dashboard?.pendingApplications?.length || 0}</p>
            <p className="text-xs text-gray-500">Pending</p>
          </div>
        </Card>
        <Card className="p-3 flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-green-50">
            <Calendar className="text-green-600" size={20} />
          </div>
          <div>
            <p className="text-lg font-semibold text-gray-900">{dashboard?.upcomingLeaves?.length || 0}</p>
            <p className="text-xs text-gray-500">Upcoming</p>
          </div>
        </Card>
      </div>

      {/* Tabs */}
      <Tabs.Root value={activeTab} onValueChange={setActiveTab}>
        <Tabs.List className="flex border-b border-gray-200 px-4">
          <TabTrigger value="balance" label="Balance" />
          <TabTrigger value="applications" label="Applications" />
          <TabTrigger value="holidays" label="Holidays" />
        </Tabs.List>

        <div className="px-4 py-4">
          <Tabs.Content value="balance">
            <LeaveBalanceTab balances={dashboard?.balances || []} />
          </Tabs.Content>

          <Tabs.Content value="applications">
            <LeaveApplicationsTab applications={dashboard?.upcomingLeaves || []} />
          </Tabs.Content>

          <Tabs.Content value="holidays">
            <HolidaysTab holidays={dashboard?.upcomingHolidays || []} />
          </Tabs.Content>
        </div>
      </Tabs.Root>
    </div>
  )
}

function TabTrigger({ value, label }: { value: string; label: string }) {
  return (
    <Tabs.Trigger
      value={value}
      className={cn(
        'flex-1 py-3 text-sm font-medium border-b-2 -mb-px transition-colors',
        'data-[state=active]:border-primary-600 data-[state=active]:text-primary-600',
        'data-[state=inactive]:border-transparent data-[state=inactive]:text-gray-500'
      )}
    >
      {label}
    </Tabs.Trigger>
  )
}

function LeaveBalanceTab({ balances }: { balances: LeaveBalance[] }) {
  if (balances.length === 0) {
    return (
      <EmptyState
        icon={<Calendar className="text-gray-400" size={24} />}
        title="No leave balances"
        description="Your leave balances will appear here"
      />
    )
  }

  return (
    <div className="space-y-3">
      {balances.map((balance) => (
        <Card key={balance.id} className="p-4">
          <div className="flex items-start justify-between mb-3">
            <div>
              <p className="text-sm font-semibold text-gray-900">{balance.leaveTypeName}</p>
              <p className="text-xs text-gray-500">{balance.leaveTypeCode}</p>
            </div>
            <Badge variant="info">{balance.financialYear}</Badge>
          </div>

          <div className="grid grid-cols-4 gap-2 text-center">
            <div className="p-2 rounded-lg bg-gray-50">
              <p className="text-lg font-semibold text-gray-900">
                {balance.openingBalance + balance.accrued + balance.carryForwarded}
              </p>
              <p className="text-[10px] text-gray-500">Total</p>
            </div>
            <div className="p-2 rounded-lg bg-green-50">
              <p className="text-lg font-semibold text-green-700">{balance.availableBalance}</p>
              <p className="text-[10px] text-gray-500">Available</p>
            </div>
            <div className="p-2 rounded-lg bg-orange-50">
              <p className="text-lg font-semibold text-orange-700">{balance.taken}</p>
              <p className="text-[10px] text-gray-500">Taken</p>
            </div>
            <div className="p-2 rounded-lg bg-blue-50">
              <p className="text-lg font-semibold text-blue-700">{balance.carryForwarded}</p>
              <p className="text-[10px] text-gray-500">Carry Fwd</p>
            </div>
          </div>
        </Card>
      ))}
    </div>
  )
}

function LeaveApplicationsTab({ applications }: { applications: LeaveApplicationSummary[] }) {
  const { data: allApplications, isLoading } = useQuery({
    queryKey: ['leave-applications'],
    queryFn: () => leaveApi.getApplications(),
  })

  const displayApplications = allApplications || applications

  if (isLoading) {
    return <PageLoader />
  }

  if (displayApplications.length === 0) {
    return (
      <EmptyState
        icon={<Calendar className="text-gray-400" size={24} />}
        title="No leave applications"
        description="Your leave applications will appear here"
        action={
          <Link to="/leave/apply">
            <Button size="sm">Apply for Leave</Button>
          </Link>
        }
      />
    )
  }

  return (
    <div className="space-y-3">
      {displayApplications.map((application) => (
        <Link key={application.id} to={`/leave/applications/${application.id}`}>
          <Card className="p-4 touch-feedback">
            <div className="flex items-start justify-between mb-2">
              <div className="flex-1 min-w-0">
                <p className="text-sm font-semibold text-gray-900">{application.leaveTypeName}</p>
                <p className="text-xs text-gray-500 mt-0.5">
                  {formatDate(application.fromDate)} - {formatDate(application.toDate)}
                </p>
              </div>
              <Badge variant={getStatusBadgeVariant(application.status)}>
                {application.status}
              </Badge>
            </div>

            <div className="flex items-center justify-between text-xs text-gray-500">
              <span>{formatDays(application.totalDays)}</span>
              <div className="flex items-center gap-1">
                <span>Applied {formatDate(application.appliedAt, 'dd MMM')}</span>
                <ChevronRight size={14} className="text-gray-400" />
              </div>
            </div>
          </Card>
        </Link>
      ))}
    </div>
  )
}

function HolidaysTab({ holidays }: { holidays: Holiday[] }) {
  const { data: allHolidays, isLoading } = useQuery({
    queryKey: ['holidays', new Date().getFullYear()],
    queryFn: () => leaveApi.getHolidays(new Date().getFullYear()),
  })

  const displayHolidays = allHolidays || holidays

  if (isLoading) {
    return <PageLoader />
  }

  if (displayHolidays.length === 0) {
    return (
      <EmptyState
        icon={<Gift className="text-gray-400" size={24} />}
        title="No holidays"
        description="Company holidays will appear here"
      />
    )
  }

  return (
    <Card className="divide-y divide-gray-100">
      {displayHolidays.map((holiday) => (
        <div key={holiday.id} className="flex items-center gap-3 p-4">
          <div className="flex items-center justify-center w-10 h-10 rounded-full bg-orange-50">
            <Gift className="text-orange-500" size={18} />
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900">{holiday.name}</p>
            <p className="text-xs text-gray-500">{formatDate(holiday.date, 'EEEE, dd MMMM yyyy')}</p>
          </div>
          {holiday.isOptional && (
            <Badge variant="default">Optional</Badge>
          )}
        </div>
      ))}
    </Card>
  )
}
