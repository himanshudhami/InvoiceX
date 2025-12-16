import { FC, useState, useMemo, useEffect } from 'react'
import { Modal } from '@/components/ui/Modal'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useAssignAsset } from '@/hooks/api/useAssets'
import { useAvailableAssets, useAsset } from '@/features/assets/hooks'
import { useEmployeesPaged, useEmployee } from '@/hooks/api/useEmployees'
import { useCompanies } from '@/hooks/api/useCompanies'
import type { Asset, Employee, CreateAssetAssignmentDto, PagedResponse } from '@/services/api/types'
import {
  Search,
  Loader2,
  Package,
  User,
  Check,
  AlertCircle,
  Laptop,
  Calendar,
  Building2,
} from 'lucide-react'

interface AssignAssetModalProps {
  isOpen: boolean
  onClose: () => void
  // Mode 1: From Asset view (search for employee)
  assetId?: string
  // Mode 2: From Employee view (search for asset)
  employeeId?: string
  // Context
  companyId: string
  onSuccess: () => void
}

// Debounce hook
function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value)

  useEffect(() => {
    const handler = setTimeout(() => setDebouncedValue(value), delay)
    return () => clearTimeout(handler)
  }, [value, delay])

  return debouncedValue
}

const AssetCard: FC<{
  asset: Asset
  selected: boolean
  onClick: () => void
}> = ({ asset, selected, onClick }) => (
  <button
    type="button"
    onClick={onClick}
    className={`w-full text-left p-3 rounded-lg border transition-colors ${
      selected
        ? 'border-blue-500 bg-blue-50'
        : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
    }`}
  >
    <div className="flex items-start gap-3">
      <div className={`flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center ${
        selected ? 'bg-blue-100' : 'bg-gray-100'
      }`}>
        <Laptop className={`w-5 h-5 ${selected ? 'text-blue-600' : 'text-gray-500'}`} />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="font-medium text-gray-900 truncate">{asset.name}</span>
          {selected && <Check className="w-4 h-4 text-blue-600 flex-shrink-0" />}
        </div>
        <div className="text-sm text-gray-500">{asset.assetTag}</div>
        <div className="flex items-center gap-2 mt-1">
          <Badge variant="outline" className="text-xs">
            {asset.assetType?.replace(/_/g, ' ')}
          </Badge>
          {asset.serialNumber && (
            <span className="text-xs text-gray-400">SN: {asset.serialNumber}</span>
          )}
        </div>
      </div>
    </div>
  </button>
)

const EmployeeCard: FC<{
  employee: Employee
  selected: boolean
  onClick: () => void
}> = ({ employee, selected, onClick }) => (
  <button
    type="button"
    onClick={onClick}
    className={`w-full text-left p-3 rounded-lg border transition-colors ${
      selected
        ? 'border-blue-500 bg-blue-50'
        : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
    }`}
  >
    <div className="flex items-center gap-3">
      <div className={`flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center ${
        selected ? 'bg-blue-100' : 'bg-gray-100'
      }`}>
        <User className={`w-5 h-5 ${selected ? 'text-blue-600' : 'text-gray-500'}`} />
      </div>
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="font-medium text-gray-900 truncate">{employee.employeeName}</span>
          {selected && <Check className="w-4 h-4 text-blue-600 flex-shrink-0" />}
        </div>
        <div className="text-sm text-gray-500">
          {employee.employeeId && `${employee.employeeId} • `}
          {employee.designation || employee.department || 'Employee'}
        </div>
      </div>
    </div>
  </button>
)

export const AssignAssetModal: FC<AssignAssetModalProps> = ({
  isOpen,
  onClose,
  assetId,
  employeeId,
  companyId,
  onSuccess,
}) => {
  // Determine mode: asset-to-employee or employee-to-asset
  const mode = assetId ? 'asset-to-employee' : 'employee-to-asset'

  // Form state
  const [searchTerm, setSearchTerm] = useState('')
  const [selectedAssetId, setSelectedAssetId] = useState<string | null>(assetId || null)
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string | null>(employeeId || null)
  const [assignedOn, setAssignedOn] = useState(new Date().toISOString().split('T')[0])
  const [conditionOut, setConditionOut] = useState('good')
  const [notes, setNotes] = useState('')

  const debouncedSearch = useDebounce(searchTerm, 300)

  // Fetch companies for displaying names
  const { data: companies = [] } = useCompanies()
  const companyName = useMemo(() => {
    const company = companies.find((c) => c.id === companyId)
    return company?.name || 'Unknown Company'
  }, [companies, companyId])

  // Fetch asset details if in asset-to-employee mode
  const { data: assetDetails } = useAsset(assetId || '', !!assetId)

  // Fetch employee details if in employee-to-asset mode
  const { data: employeeDetails } = useEmployee(employeeId || '', !!employeeId)

  // Fetch available assets for employee-to-asset mode
  const { data: availableAssets, isLoading: isLoadingAssets } = useAvailableAssets(
    companyId,
    mode === 'employee-to-asset' ? debouncedSearch : undefined,
    mode === 'employee-to-asset' && !!companyId
  )

  // Fetch employees for asset-to-employee mode
  // Filter by companyId to only show employees from the same company as the asset
  // Use searchTerm (not employeeName) for partial matching
  const { data: employeesData, isLoading: isLoadingEmployees } = useEmployeesPaged(
    mode === 'asset-to-employee' ? {
      companyId: companyId || undefined,
      searchTerm: debouncedSearch || undefined,
      status: 'active',
      pageSize: 100,
    } : { pageSize: 0 } // Don't fetch if not in asset-to-employee mode
  )

  // Mutation
  const assignAsset = useAssignAsset()

  // Filter results based on search
  const filteredAssets = useMemo(() => {
    if (!availableAssets) return []
    if (!debouncedSearch) return availableAssets
    const lower = debouncedSearch.toLowerCase()
    return availableAssets.filter(
      (a) =>
        a.name.toLowerCase().includes(lower) ||
        a.assetTag.toLowerCase().includes(lower) ||
        a.serialNumber?.toLowerCase().includes(lower)
    )
  }, [availableAssets, debouncedSearch])

  // Get employees and apply client-side filtering as safety net
  const allEmployees = (employeesData as PagedResponse<Employee> | undefined)?.items || []
  const employees = useMemo(() => {
    if (!debouncedSearch) return allEmployees
    const search = debouncedSearch.toLowerCase()
    return allEmployees.filter(
      (emp) =>
        emp.employeeName?.toLowerCase().includes(search) ||
        emp.employeeId?.toLowerCase().includes(search) ||
        emp.department?.toLowerCase().includes(search) ||
        emp.designation?.toLowerCase().includes(search)
    )
  }, [allEmployees, debouncedSearch])

  // Reset state when modal opens/closes
  useEffect(() => {
    if (isOpen) {
      setSearchTerm('')
      setSelectedAssetId(assetId || null)
      setSelectedEmployeeId(employeeId || null)
      setAssignedOn(new Date().toISOString().split('T')[0])
      setConditionOut('good')
      setNotes('')
    }
  }, [isOpen, assetId, employeeId])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    const targetAssetId = selectedAssetId || assetId
    const targetEmployeeId = selectedEmployeeId || employeeId

    if (!targetAssetId) {
      return
    }

    try {
      const data: CreateAssetAssignmentDto = {
        targetType: targetEmployeeId ? 'employee' : 'company',
        companyId,
        employeeId: targetEmployeeId || undefined,
        assignedOn,
        conditionOut,
        notes: notes || undefined,
      }

      await assignAsset.mutateAsync({ assetId: targetAssetId, data })
      onSuccess()
      onClose()
    } catch (err) {
      console.error('Failed to assign asset:', err)
    }
  }

  const handleClose = () => {
    setSearchTerm('')
    setSelectedAssetId(null)
    setSelectedEmployeeId(null)
    setNotes('')
    onClose()
  }

  const isValid = mode === 'asset-to-employee'
    ? !!selectedEmployeeId
    : !!selectedAssetId

  const title = mode === 'asset-to-employee'
    ? `Assign ${assetDetails?.name || 'Asset'}`
    : 'Assign Asset'

  return (
    <Modal isOpen={isOpen} onClose={handleClose} title={title} size="lg">
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Context Info */}
        <div className="bg-gray-50 rounded-lg p-3 space-y-2">
          {/* Asset Summary (when assigning from asset view) */}
          {mode === 'asset-to-employee' && assetDetails && (
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                <Package className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <div className="font-medium text-gray-900">{assetDetails.name}</div>
                <div className="text-sm text-gray-500">{assetDetails.assetTag}</div>
              </div>
            </div>
          )}

          {/* Employee Summary (when assigning from employee view) */}
          {mode === 'employee-to-asset' && employeeDetails && (
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-green-100 rounded-full flex items-center justify-center">
                <User className="w-5 h-5 text-green-600" />
              </div>
              <div>
                <div className="font-medium text-gray-900">{employeeDetails.employeeName}</div>
                <div className="text-sm text-gray-500">
                  {employeeDetails.employeeId && `${employeeDetails.employeeId} • `}
                  {employeeDetails.designation || employeeDetails.department || 'Employee'}
                </div>
              </div>
            </div>
          )}

          {/* Company Info */}
          <div className="flex items-center gap-2 text-sm text-gray-500 pt-1 border-t border-gray-200 mt-2">
            <Building2 className="w-4 h-4" />
            <span>{companyName}</span>
          </div>
        </div>

        {/* Search & Selection */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            {mode === 'asset-to-employee' ? 'Select Employee' : 'Select Asset'}
          </label>
          <div className="relative mb-3">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <Input
              type="text"
              placeholder={mode === 'asset-to-employee' ? 'Search employees...' : 'Search available assets...'}
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10"
            />
          </div>

          {/* Loading State */}
          {(isLoadingAssets || isLoadingEmployees) && (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
            </div>
          )}

          {/* Employee Selection List */}
          {mode === 'asset-to-employee' && !isLoadingEmployees && (
            <div className="max-h-64 overflow-y-auto space-y-2 border rounded-lg p-2">
              {employees.length === 0 ? (
                <div className="text-center py-6 text-gray-500">
                  <User className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                  <p className="text-sm">No employees found</p>
                  <p className="text-xs text-gray-400 mt-1">Try a different search term</p>
                </div>
              ) : (
                employees.map((emp) => (
                  <EmployeeCard
                    key={emp.id}
                    employee={emp}
                    selected={selectedEmployeeId === emp.id}
                    onClick={() => setSelectedEmployeeId(emp.id)}
                  />
                ))
              )}
            </div>
          )}

          {/* Asset Selection List */}
          {mode === 'employee-to-asset' && !isLoadingAssets && (
            <div className="max-h-64 overflow-y-auto space-y-2 border rounded-lg p-2">
              {filteredAssets.length === 0 ? (
                <div className="text-center py-6 text-gray-500">
                  <Package className="w-8 h-8 mx-auto mb-2 text-gray-300" />
                  <p className="text-sm">No available assets found</p>
                  <p className="text-xs text-gray-400 mt-1">All assets may be assigned</p>
                </div>
              ) : (
                filteredAssets.map((asset) => (
                  <AssetCard
                    key={asset.id}
                    asset={asset}
                    selected={selectedAssetId === asset.id}
                    onClick={() => setSelectedAssetId(asset.id)}
                  />
                ))
              )}
            </div>
          )}
        </div>

        {/* Assignment Details */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              <Calendar className="w-4 h-4 inline-block mr-1" />
              Assignment Date
            </label>
            <Input
              type="date"
              value={assignedOn}
              onChange={(e) => setAssignedOn(e.target.value)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Condition
            </label>
            <Select value={conditionOut} onValueChange={setConditionOut}>
              <SelectTrigger>
                <SelectValue placeholder="Select condition" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="new">New</SelectItem>
                <SelectItem value="excellent">Excellent</SelectItem>
                <SelectItem value="good">Good</SelectItem>
                <SelectItem value="fair">Fair</SelectItem>
                <SelectItem value="poor">Poor</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>

        {/* Notes */}
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">
            Notes (Optional)
          </label>
          <Textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            placeholder="Add any assignment notes..."
            rows={2}
          />
        </div>

        {/* Error Display */}
        {assignAsset.isError && (
          <div className="flex items-center gap-2 p-3 bg-red-50 text-red-600 rounded-lg text-sm">
            <AlertCircle className="w-4 h-4" />
            <span>Failed to assign asset. Please try again.</span>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3 pt-4 border-t">
          <Button type="button" variant="outline" onClick={handleClose}>
            Cancel
          </Button>
          <Button
            type="submit"
            disabled={!isValid || assignAsset.isPending}
          >
            {assignAsset.isPending ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Assigning...
              </>
            ) : (
              'Assign Asset'
            )}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
