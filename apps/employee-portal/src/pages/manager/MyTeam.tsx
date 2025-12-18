import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Users, Building2, UserCircle, Calendar, Clock } from 'lucide-react'
import { useMyTeam, useTeamLeaves, useManagerDashboard } from '@/hooks'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Badge, PageLoader, QuickStat, getStatusBadgeVariant } from '@/components/ui'
import { formatDate, formatDays } from '@/utils/format'
import type { EmployeeHierarchy, TeamLeaveApplication } from '@/types'

export function MyTeamPage() {
  const [activeTab, setActiveTab] = useState<'team' | 'leaves'>('team')

  const { data: dashboard, isLoading: dashboardLoading } = useManagerDashboard()
  const { data: teamData, isLoading: teamLoading } = useMyTeam()
  const { data: teamLeaves, isLoading: leavesLoading } = useTeamLeaves()

  const isLoading = dashboardLoading || teamLoading || leavesLoading

  if (isLoading) {
    return <PageLoader />
  }

  const directReports = teamData?.directReports || []
  const pendingLeaves = teamLeaves?.filter((l) => l.status === 'pending') || []
  const upcomingLeaves = teamLeaves?.filter(
    (l) => l.status === 'approved' && new Date(l.fromDate) > new Date()
  ) || []

  return (
    <div className="animate-fade-in">
      <PageHeader title="My Team" />

      {/* Stats */}
      <div className="px-4 py-4 grid grid-cols-3 gap-3">
        <QuickStat
          label="Team Size"
          value={dashboard?.directReportsCount?.toString() || '0'}
          color="primary"
          icon={<Users size={18} className="text-primary-600" />}
        />
        <QuickStat
          label="Pending"
          value={pendingLeaves.length.toString()}
          color="warning"
          icon={<Clock size={18} className="text-yellow-600" />}
        />
        <QuickStat
          label="Upcoming"
          value={upcomingLeaves.length.toString()}
          color="success"
          icon={<Calendar size={18} className="text-green-600" />}
        />
      </div>

      {/* Tabs */}
      <div className="px-4 flex gap-2 border-b border-gray-200 mb-4">
        <button
          className={`pb-2 px-1 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'team'
              ? 'border-primary-500 text-primary-600'
              : 'border-transparent text-gray-500'
          }`}
          onClick={() => setActiveTab('team')}
        >
          Team Members ({directReports.length})
        </button>
        <button
          className={`pb-2 px-1 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'leaves'
              ? 'border-primary-500 text-primary-600'
              : 'border-transparent text-gray-500'
          }`}
          onClick={() => setActiveTab('leaves')}
        >
          Leave Requests ({teamLeaves?.length || 0})
        </button>
      </div>

      {/* Content */}
      <div className="px-4 pb-4">
        {activeTab === 'team' ? (
          directReports.length === 0 ? (
            <EmptyState
              icon={<Users className="text-gray-400\" size={48} />}
              title="No Team Members"
              description="You don't have any direct reports yet."
            />
          ) : (
            <div className="space-y-3">
              {directReports.map((member) => (
                <TeamMemberCard key={member.id} member={member} />
              ))}
            </div>
          )
        ) : (
          teamLeaves && teamLeaves.length === 0 ? (
            <EmptyState
              icon={<Calendar className="text-gray-400" size={48} />}
              title="No Leave Requests"
              description="Your team hasn't applied for any leaves."
            />
          ) : (
            <div className="space-y-3">
              {teamLeaves?.map((leave) => (
                <TeamLeaveCard key={leave.id} leave={leave} />
              ))}
            </div>
          )
        )}
      </div>
    </div>
  )
}

function TeamMemberCard({ member }: { member: EmployeeHierarchy }) {
  return (
    <Card className="p-4">
      <div className="flex items-center gap-3">
        <div className="flex items-center justify-center w-12 h-12 rounded-full bg-primary-100">
          <UserCircle className="w-7 h-7 text-primary-600" />
        </div>
        <div className="flex-1 min-w-0">
          <h3 className="font-medium text-gray-900 truncate">{member.employeeName}</h3>
          <div className="flex items-center gap-2 text-sm text-gray-500">
            {member.designation && <span>{member.designation}</span>}
            {member.designation && member.department && <span>•</span>}
            {member.department && (
              <span className="flex items-center gap-1">
                <Building2 size={12} />
                {member.department}
              </span>
            )}
          </div>
          {member.email && (
            <p className="text-xs text-gray-400 truncate mt-1">{member.email}</p>
          )}
        </div>
        {member.isManager && (
          <Badge variant="info" className="text-xs">
            Manager
          </Badge>
        )}
      </div>
    </Card>
  )
}

function TeamLeaveCard({ leave }: { leave: TeamLeaveApplication }) {
  return (
    <Link to={`/manager/approvals?request=${leave.approvalRequestId}`}>
      <Card className="p-4 hover:shadow-md transition-shadow">
        <div className="flex items-start justify-between">
          <div className="flex items-center gap-3">
            <div className="flex items-center justify-center w-10 h-10 rounded-full bg-gray-100">
              <UserCircle className="w-6 h-6 text-gray-600" />
            </div>
            <div>
              <h3 className="font-medium text-gray-900">{leave.employeeName}</h3>
              <p className="text-sm text-gray-500">{leave.leaveTypeName}</p>
            </div>
          </div>
          <Badge variant={getStatusBadgeVariant(leave.status as any)}>
            {leave.status}
          </Badge>
        </div>
        <div className="mt-3 pt-3 border-t border-gray-100 flex items-center justify-between">
          <div className="text-sm text-gray-600">
            <span>{formatDate(leave.fromDate)}</span>
            <span className="mx-2">→</span>
            <span>{formatDate(leave.toDate)}</span>
          </div>
          <div className="text-sm font-medium text-gray-900">
            {formatDays(leave.totalDays)}
          </div>
        </div>
        {leave.reason && (
          <p className="mt-2 text-sm text-gray-500 line-clamp-2">{leave.reason}</p>
        )}
        {leave.isCurrentApprover && leave.status === 'pending' && (
          <div className="mt-3 pt-3 border-t border-gray-100">
            <span className="text-xs font-medium text-primary-600">
              Action required - Tap to review
            </span>
          </div>
        )}
      </Card>
    </Link>
  )
}
