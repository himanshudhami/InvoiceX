import { FC, useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useEmployeeAssets } from '@/features/employees/hooks/useEmployeeHub'
import { useAssets } from '@/hooks/api/useAssets'
import { useEmployee } from '@/hooks/api/useEmployees'
import { AssignAssetModal } from '@/components/modals/AssignAssetModal'
import { ReturnAssetModal } from '@/components/modals/ReturnAssetModal'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Loader2,
  Package,
  Plus,
  Laptop,
  RotateCcw,
  ExternalLink,
} from 'lucide-react'
import { format } from 'date-fns'
import type { AssetAssignment, Asset, PagedResponse } from '@/services/api/types'

interface AssetsTabProps {
  employeeId: string
}

interface EnrichedAssignment extends AssetAssignment {
  asset?: Asset
}

export const AssetsTab: FC<AssetsTabProps> = ({ employeeId }) => {
  const navigate = useNavigate()
  const { data: assignments, isLoading, refetch } = useEmployeeAssets(employeeId)
  const { data: assetsData } = useAssets({ pageSize: 100 })
  const { data: employee } = useEmployee(employeeId)

  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false)
  const [returningAssignment, setReturningAssignment] = useState<EnrichedAssignment | null>(null)

  // Create asset map for enrichment
  const assetMap = useMemo(() => {
    const items = (assetsData as PagedResponse<Asset> | undefined)?.items || []
    return new Map(items.map((a) => [a.id, a]))
  }, [assetsData])

  // Enrich assignments with asset details
  const enrichedAssignments: EnrichedAssignment[] = useMemo(() => {
    if (!assignments) return []
    return assignments.map((a) => ({
      ...a,
      asset: assetMap.get(a.assetId),
    }))
  }, [assignments, assetMap])

  const activeAssignments = enrichedAssignments.filter((a) => !a.returnedOn)
  const returnedAssignments = enrichedAssignments.filter((a) => a.returnedOn)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    )
  }

  const handleAssignSuccess = () => {
    setIsAssignModalOpen(false)
    refetch()
  }

  const handleReturnSuccess = () => {
    setReturningAssignment(null)
    refetch()
  }

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-gray-900">Assigned Assets</h3>
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsAssignModalOpen(true)}
          >
            <Plus className="w-3 h-3 mr-1" />
            Assign Asset
          </Button>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate('/assets')}
          >
            <ExternalLink className="w-3 h-3 mr-1" />
            All Assets
          </Button>
        </div>
      </div>

      {enrichedAssignments.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-6 text-center">
          <Package className="w-10 h-10 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500 text-sm mb-3">No assets assigned</p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setIsAssignModalOpen(true)}
          >
            <Plus className="w-3 h-3 mr-1" />
            Assign Asset
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          {/* Active Assignments */}
          {activeAssignments.length > 0 && (
            <section>
              <h4 className="text-xs font-medium text-gray-500 uppercase mb-2">
                Currently Assigned ({activeAssignments.length})
              </h4>
              <div className="space-y-2">
                {activeAssignments.map((assignment) => (
                  <div
                    key={assignment.id}
                    className="bg-white border border-gray-200 rounded-lg p-3 hover:border-gray-300 transition-colors"
                  >
                    <div className="flex items-start justify-between">
                      <div className="flex items-start gap-3">
                        <div className="flex-shrink-0 w-9 h-9 bg-blue-100 rounded-lg flex items-center justify-center">
                          <Laptop className="w-4 h-4 text-blue-600" />
                        </div>
                        <div>
                          <div className="font-medium text-gray-900 text-sm">
                            {assignment.asset?.name || 'Unknown Asset'}
                          </div>
                          <div className="text-xs text-gray-500">
                            {assignment.asset?.assetTag || assignment.assetId.slice(0, 8)}
                            {assignment.asset?.assetType && (
                              <span className="ml-1">
                                • {assignment.asset.assetType.replace(/_/g, ' ')}
                              </span>
                            )}
                          </div>
                          <div className="text-xs text-gray-400 mt-1">
                            Assigned: {format(new Date(assignment.assignedOn), 'MMM dd, yyyy')}
                            {assignment.conditionOut && ` • ${assignment.conditionOut}`}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Badge variant="default" className="text-xs">
                          Active
                        </Badge>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setReturningAssignment(assignment)}
                          className="text-orange-600 hover:text-orange-700 hover:bg-orange-50"
                        >
                          <RotateCcw className="w-3 h-3 mr-1" />
                          Return
                        </Button>
                      </div>
                    </div>
                    {assignment.notes && (
                      <div className="mt-2 text-xs text-gray-500 italic pl-12">
                        {assignment.notes}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Returned Assignments */}
          {returnedAssignments.length > 0 && (
            <section>
              <h4 className="text-xs font-medium text-gray-500 uppercase mb-2">
                Previously Assigned ({returnedAssignments.length})
              </h4>
              <div className="space-y-2">
                {returnedAssignments.slice(0, 3).map((assignment) => (
                  <div
                    key={assignment.id}
                    className="bg-gray-50 rounded-lg p-3 opacity-75"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-3">
                        <div className="flex-shrink-0 w-8 h-8 bg-gray-200 rounded-lg flex items-center justify-center">
                          <Laptop className="w-4 h-4 text-gray-500" />
                        </div>
                        <div>
                          <div className="font-medium text-gray-700 text-sm">
                            {assignment.asset?.name || 'Unknown Asset'}
                          </div>
                          <div className="text-xs text-gray-500">
                            {assignment.asset?.assetTag || assignment.assetId.slice(0, 8)}
                          </div>
                        </div>
                      </div>
                      <Badge variant="outline" className="text-xs">
                        Returned
                      </Badge>
                    </div>
                    <div className="text-xs text-gray-400 mt-1 pl-11">
                      {format(new Date(assignment.assignedOn), 'MMM dd, yyyy')} →{' '}
                      {assignment.returnedOn &&
                        format(new Date(assignment.returnedOn), 'MMM dd, yyyy')}
                    </div>
                  </div>
                ))}
                {returnedAssignments.length > 3 && (
                  <Button
                    variant="link"
                    size="sm"
                    className="w-full"
                    onClick={() => navigate('/assets')}
                  >
                    View {returnedAssignments.length - 3} more...
                  </Button>
                )}
              </div>
            </section>
          )}
        </div>
      )}

      {/* Assign Asset Modal */}
      <AssignAssetModal
        isOpen={isAssignModalOpen}
        onClose={() => setIsAssignModalOpen(false)}
        employeeId={employeeId}
        companyId={employee?.companyId || ''}
        onSuccess={handleAssignSuccess}
      />

      {/* Return Asset Modal */}
      <ReturnAssetModal
        isOpen={!!returningAssignment}
        onClose={() => setReturningAssignment(null)}
        assignment={returningAssignment}
        assetName={returningAssignment?.asset?.name}
        onSuccess={handleReturnSuccess}
      />
    </div>
  )
}
