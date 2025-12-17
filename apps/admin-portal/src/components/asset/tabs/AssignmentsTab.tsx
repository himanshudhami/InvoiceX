import { FC } from 'react'
import { useAssetAssignmentHistory } from '@/features/assets/hooks'
import type { AssetAssignment } from '@/services/api/types'
import { formatDate } from '@/lib/date'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  UserPlus,
  RotateCcw,
  User,
  Building2,
  Calendar,
  AlertCircle,
  Loader2,
  History,
} from 'lucide-react'

interface AssignmentsTabProps {
  assetId: string
  currentAssignment: AssetAssignment | null
  onAssign?: () => void
  onReturn?: (assignmentId: string) => void
}

const AssignmentCard: FC<{
  assignment: AssetAssignment
  isCurrent: boolean
  onReturn?: (assignmentId: string) => void
}> = ({ assignment, isCurrent, onReturn }) => {
  return (
    <div
      className={`rounded-lg border p-3 ${
        isCurrent ? 'bg-blue-50 border-blue-200' : 'bg-white border-gray-200'
      }`}
    >
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-2">
          {assignment.targetType === 'employee' ? (
            <User className={`w-4 h-4 mt-0.5 ${isCurrent ? 'text-blue-600' : 'text-gray-400'}`} />
          ) : (
            <Building2 className={`w-4 h-4 mt-0.5 ${isCurrent ? 'text-blue-600' : 'text-gray-400'}`} />
          )}
          <div>
            <div className="flex items-center gap-2">
              <span className={`text-sm font-medium ${isCurrent ? 'text-blue-900' : 'text-gray-900'}`}>
                {assignment.targetType === 'employee' ? 'Employee Assignment' : 'Company Assignment'}
              </span>
              {isCurrent && (
                <Badge variant="default" className="text-xs">
                  Current
                </Badge>
              )}
            </div>
            <div className="flex items-center gap-3 mt-1 text-xs text-gray-500">
              <span className="flex items-center gap-1">
                <Calendar className="w-3 h-3" />
                {formatDate(assignment.assignedOn)}
              </span>
              {assignment.returnedOn && (
                <>
                  <span>→</span>
                  <span>{formatDate(assignment.returnedOn)}</span>
                </>
              )}
            </div>
            {(assignment.conditionOut || assignment.conditionIn) && (
              <div className="flex gap-3 mt-1 text-xs">
                {assignment.conditionOut && (
                  <span className="text-gray-500">
                    Out: <span className="text-gray-700">{assignment.conditionOut}</span>
                  </span>
                )}
                {assignment.conditionIn && (
                  <span className="text-gray-500">
                    In: <span className="text-gray-700">{assignment.conditionIn}</span>
                  </span>
                )}
              </div>
            )}
            {assignment.notes && (
              <div className="mt-1 text-xs text-gray-500 italic">{assignment.notes}</div>
            )}
          </div>
        </div>
        {isCurrent && onReturn && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => onReturn(assignment.id)}
            className="text-orange-600 hover:text-orange-700 hover:bg-orange-50"
          >
            <RotateCcw className="w-3 h-3 mr-1" />
            Return
          </Button>
        )}
      </div>
    </div>
  )
}

export const AssignmentsTab: FC<AssignmentsTabProps> = ({
  assetId,
  currentAssignment,
  onAssign,
  onReturn,
}) => {
  const { data: assignments, isLoading, isError } = useAssetAssignmentHistory(assetId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <AlertCircle className="w-8 h-8 mb-2" />
        <p className="text-sm">Failed to load assignment history</p>
      </div>
    )
  }

  const pastAssignments = assignments?.filter((a) => a.returnedOn) || []

  return (
    <div className="p-4 space-y-4">
      {/* Quick Assign Action */}
      {!currentAssignment && onAssign && (
        <Button onClick={onAssign} className="w-full">
          <UserPlus className="w-4 h-4 mr-2" />
          Assign Asset
        </Button>
      )}

      {/* Current Assignment */}
      {currentAssignment && (
        <div>
          <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">
            Current Assignment
          </h3>
          <AssignmentCard
            assignment={currentAssignment}
            isCurrent={true}
            onReturn={onReturn}
          />
        </div>
      )}

      {/* Assignment History */}
      {pastAssignments.length > 0 && (
        <div>
          <h3 className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2 flex items-center gap-1">
            <History className="w-3 h-3" />
            Assignment History
          </h3>
          <div className="space-y-2">
            {pastAssignments.map((assignment) => (
              <AssignmentCard
                key={assignment.id}
                assignment={assignment}
                isCurrent={false}
              />
            ))}
          </div>
        </div>
      )}

      {/* Empty State */}
      {!currentAssignment && pastAssignments.length === 0 && (
        <div className="flex flex-col items-center justify-center py-8 text-gray-500">
          <User className="w-10 h-10 mb-3 text-gray-300" />
          <p className="text-sm font-medium">No assignments yet</p>
          <p className="text-xs mt-1">This asset has never been assigned</p>
        </div>
      )}
    </div>
  )
}
