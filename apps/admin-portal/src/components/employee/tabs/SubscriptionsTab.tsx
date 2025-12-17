import { FC } from 'react'
import { useNavigate } from 'react-router-dom'
import { useEmployeeSubscriptions } from '@/features/employees/hooks/useEmployeeHub'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ExternalLink, Loader2, CreditCard, Plus } from 'lucide-react'
import { format } from 'date-fns'

interface SubscriptionsTabProps {
  employeeId: string
}

export const SubscriptionsTab: FC<SubscriptionsTabProps> = ({ employeeId }) => {
  const navigate = useNavigate()
  const { data: assignments, isLoading } = useEmployeeSubscriptions(employeeId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    )
  }

  const activeAssignments = assignments?.filter((a) => !a.revokedOn) || []
  const revokedAssignments = assignments?.filter((a) => a.revokedOn) || []

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-gray-900">Assigned Subscriptions</h3>
        <Button variant="ghost" size="sm" onClick={() => navigate(`/subscriptions`)}>
          <ExternalLink className="w-3 h-3 mr-1" />
          Manage
        </Button>
      </div>

      {!assignments || assignments.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-6 text-center">
          <CreditCard className="w-10 h-10 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500 text-sm mb-3">No subscriptions assigned</p>
          <Button variant="outline" size="sm" onClick={() => navigate('/subscriptions')}>
            <Plus className="w-3 h-3 mr-1" />
            Assign Subscription
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Active Assignments */}
          {activeAssignments.length > 0 && (
            <section>
              <h4 className="text-xs font-medium text-gray-500 uppercase mb-2">
                Active Subscriptions ({activeAssignments.length})
              </h4>
              <div className="space-y-2">
                {activeAssignments.map((assignment) => (
                  <div
                    key={assignment.id}
                    className="bg-gray-50 rounded-lg p-3 hover:bg-gray-100 cursor-pointer transition-colors"
                    onClick={() => navigate(`/subscriptions`)}
                  >
                    <div className="flex items-center justify-between mb-1">
                      <div className="font-medium text-gray-900 text-sm">
                        {assignment.seatIdentifier || `Seat #${assignment.id?.slice(0, 8)}`}
                      </div>
                      <Badge variant="default">Active</Badge>
                    </div>
                    <div className="text-xs text-gray-500 space-y-1">
                      {assignment.role && <div>Role: {assignment.role}</div>}
                      <div>Assigned: {format(new Date(assignment.assignedOn), 'MMM dd, yyyy')}</div>
                      {assignment.notes && <div className="truncate">Note: {assignment.notes}</div>}
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Revoked Assignments */}
          {revokedAssignments.length > 0 && (
            <section>
              <h4 className="text-xs font-medium text-gray-500 uppercase mb-2">
                Previously Assigned ({revokedAssignments.length})
              </h4>
              <div className="space-y-2">
                {revokedAssignments.slice(0, 3).map((assignment) => (
                  <div key={assignment.id} className="bg-gray-50 rounded-lg p-3 opacity-75">
                    <div className="flex items-center justify-between mb-1">
                      <div className="font-medium text-gray-700 text-sm">
                        {assignment.seatIdentifier || `Seat #${assignment.id?.slice(0, 8)}`}
                      </div>
                      <Badge variant="outline">Revoked</Badge>
                    </div>
                    <div className="text-xs text-gray-500">
                      {format(new Date(assignment.assignedOn), 'MMM dd, yyyy')} -{' '}
                      {assignment.revokedOn && format(new Date(assignment.revokedOn), 'MMM dd, yyyy')}
                    </div>
                  </div>
                ))}
                {revokedAssignments.length > 3 && (
                  <Button
                    variant="link"
                    size="sm"
                    className="w-full"
                    onClick={() => navigate('/subscriptions')}
                  >
                    View {revokedAssignments.length - 3} more...
                  </Button>
                )}
              </div>
            </section>
          )}
        </div>
      )}
    </div>
  )
}
