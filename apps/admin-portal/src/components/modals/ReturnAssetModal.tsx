import { FC, useState, useEffect } from 'react'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useReturnAssetAssignment } from '@/hooks/api/useAssets'
import type { AssetAssignment } from '@/services/api/types'
import {
  Loader2,
  RotateCcw,
  Calendar,
  AlertCircle,
  CheckCircle2,
} from 'lucide-react'

interface ReturnAssetModalProps {
  isOpen: boolean
  onClose: () => void
  assignment: AssetAssignment | null
  assetName?: string
  onSuccess: () => void
}

export const ReturnAssetModal: FC<ReturnAssetModalProps> = ({
  isOpen,
  onClose,
  assignment,
  assetName,
  onSuccess,
}) => {
  const [returnedOn, setReturnedOn] = useState(new Date().toISOString().split('T')[0])
  const [conditionIn, setConditionIn] = useState('')
  const [notes, setNotes] = useState('')

  const returnAsset = useReturnAssetAssignment()

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setReturnedOn(new Date().toISOString().split('T')[0])
      setConditionIn(assignment?.conditionOut || 'good')
      setNotes('')
    }
  }, [isOpen, assignment])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!assignment) return

    try {
      await returnAsset.mutateAsync({
        assignmentId: assignment.id,
        data: {
          returnedOn,
          conditionIn: conditionIn || undefined,
        },
      })
      onSuccess()
      onClose()
    } catch (err) {
      console.error('Failed to return asset:', err)
    }
  }

  const handleClose = () => {
    setNotes('')
    onClose()
  }

  if (!assignment) return null

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title="Return Asset" size="md">
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Asset Info */}
        <div className="bg-gray-50 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <div className="w-10 h-10 bg-orange-100 rounded-lg flex items-center justify-center">
              <RotateCcw className="w-5 h-5 text-orange-600" />
            </div>
            <div className="flex-1">
              <div className="font-medium text-gray-900">
                {assetName || 'Asset'}
              </div>
              <div className="text-sm text-gray-500 mt-0.5">
                Assigned on: {new Date(assignment.assignedOn).toLocaleDateString()}
              </div>
              {assignment.conditionOut && (
                <div className="text-sm text-gray-500">
                  Condition when assigned: <span className="font-medium">{assignment.conditionOut}</span>
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Return Date */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            <Calendar className="w-4 h-4 inline-block mr-1" />
            Return Date
          </label>
          <Input
            type="date"
            value={returnedOn}
            onChange={(e) => setReturnedOn(e.target.value)}
            min={assignment.assignedOn.split('T')[0]}
          />
          {returnedOn < assignment.assignedOn.split('T')[0] && (
            <p className="text-xs text-red-500 mt-1">
              Return date cannot be before assignment date
            </p>
          )}
        </div>

        {/* Condition on Return */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Condition on Return
          </label>
          <Select value={conditionIn} onValueChange={setConditionIn}>
            <SelectTrigger>
              <SelectValue placeholder="Select condition" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="new">New</SelectItem>
              <SelectItem value="excellent">Excellent</SelectItem>
              <SelectItem value="good">Good</SelectItem>
              <SelectItem value="fair">Fair</SelectItem>
              <SelectItem value="poor">Poor</SelectItem>
              <SelectItem value="damaged">Damaged</SelectItem>
              <SelectItem value="needs_repair">Needs Repair</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {/* Condition Change Warning */}
        {assignment.conditionOut && conditionIn && conditionIn !== assignment.conditionOut && (
          <div className={`flex items-start gap-2 p-3 rounded-lg text-sm ${
            ['poor', 'damaged', 'needs_repair'].includes(conditionIn)
              ? 'bg-yellow-50 text-yellow-700'
              : 'bg-blue-50 text-blue-700'
          }`}>
            <AlertCircle className="w-4 h-4 mt-0.5 flex-shrink-0" />
            <span>
              Condition changed from <strong>{assignment.conditionOut}</strong> to <strong>{conditionIn}</strong>
            </span>
          </div>
        )}

        {/* Notes */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Notes (Optional)
          </label>
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Any notes about the return..."
            rows={2}
          />
        </div>

        {/* Error Display */}
        {returnAsset.isError && (
          <div className="flex items-center gap-2 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
            <AlertCircle className="w-4 h-4" />
            <span>Failed to return asset. Please try again.</span>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button type="button" variant="outline" onClick={handleClose}>
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={returnAsset.isPending || returnedOn < assignment.assignedOn.split('T')[0]}
            className="bg-orange-600 hover:bg-orange-700"
          >
            {returnAsset.isPending ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Processing...
              </>
            ) : (
              <>
                <CheckCircle2 className="w-4 h-4 mr-2" />
                Return Asset
              </>
            )}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
