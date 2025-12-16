import { FC, useMemo } from 'react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useCompanies } from '@/hooks/api/useCompanies'
import {
  Search,
  X,
  Filter,
  Laptop,
  AlertTriangle,
  UserX,
  DollarSign,
  Clock,
  Package,
} from 'lucide-react'

export interface AssetFilters {
  search: string
  status: string
  assetType: string
  company: string
}

interface QuickViewPreset {
  id: string
  label: string
  icon: React.ElementType
  filters: Partial<AssetFilters>
  description: string
  forRole?: string[]
}

const QUICK_VIEW_PRESETS: QuickViewPreset[] = [
  {
    id: 'all',
    label: 'All Assets',
    icon: Package,
    filters: { status: '', assetType: '' },
    description: 'View all assets',
  },
  {
    id: 'it-equipment',
    label: 'IT Equipment',
    icon: Laptop,
    filters: { assetType: 'IT_Asset' },
    description: 'Laptops, phones, monitors',
    forRole: ['IT'],
  },
  {
    id: 'needs-attention',
    label: 'Needs Attention',
    icon: AlertTriangle,
    filters: { status: 'maintenance' },
    description: 'In maintenance or repair',
    forRole: ['IT'],
  },
  {
    id: 'unassigned',
    label: 'Unassigned',
    icon: UserX,
    filters: { status: 'available' },
    description: 'Available for assignment',
    forRole: ['HR', 'IT'],
  },
  {
    id: 'high-value',
    label: 'High Value',
    icon: DollarSign,
    filters: { assetType: '' },
    description: 'Assets over threshold',
    forRole: ['Finance'],
  },
  {
    id: 'aging',
    label: 'End of Life',
    icon: Clock,
    filters: { status: 'retired' },
    description: 'Due for disposal',
    forRole: ['Finance'],
  },
]

const ALL_VALUE = '__all__'

const ASSET_TYPES = [
  { value: ALL_VALUE, label: 'All Types' },
  { value: 'IT_Asset', label: 'IT Asset' },
  { value: 'Fixed_Asset', label: 'Fixed Asset' },
  { value: 'Furniture', label: 'Furniture' },
  { value: 'Vehicle', label: 'Vehicle' },
  { value: 'Equipment', label: 'Equipment' },
  { value: 'Software', label: 'Software' },
  { value: 'Other', label: 'Other' },
]

const ASSET_STATUSES = [
  { value: ALL_VALUE, label: 'All Statuses' },
  { value: 'available', label: 'Available' },
  { value: 'assigned', label: 'Assigned' },
  { value: 'maintenance', label: 'In Maintenance' },
  { value: 'reserved', label: 'Reserved' },
  { value: 'retired', label: 'Retired' },
  { value: 'disposed', label: 'Disposed' },
]

// Helper to convert filter value to select value
const toSelectValue = (value: string) => value || ALL_VALUE
// Helper to convert select value to filter value
const fromSelectValue = (value: string) => value === ALL_VALUE ? '' : value

interface AssetQuickFiltersProps {
  filters: AssetFilters
  onFilterChange: (filters: Partial<AssetFilters>) => void
  activePreset?: string
}

export const AssetQuickFilters: FC<AssetQuickFiltersProps> = ({
  filters,
  onFilterChange,
  activePreset,
}) => {
  const { data: companies } = useCompanies()

  // Check if any filters are active
  const hasActiveFilters = useMemo(() => {
    return filters.search || filters.status || filters.assetType || filters.company
  }, [filters])

  // Find which preset matches current filters
  const matchingPreset = useMemo(() => {
    if (activePreset) return activePreset

    for (const preset of QUICK_VIEW_PRESETS) {
      const matches = Object.entries(preset.filters).every(([key, value]) => {
        if (value === '') return !filters[key as keyof AssetFilters]
        return filters[key as keyof AssetFilters] === value
      })
      if (matches && !filters.search && !filters.company) {
        return preset.id
      }
    }
    return null
  }, [filters, activePreset])

  const handlePresetClick = (preset: QuickViewPreset) => {
    onFilterChange({
      search: '',
      company: '',
      ...preset.filters,
      status: preset.filters.status || '',
      assetType: preset.filters.assetType || '',
    })
  }

  const handleClearFilters = () => {
    onFilterChange({
      search: '',
      status: '',
      assetType: '',
      company: '',
    })
  }

  return (
    <div className="bg-white border-b border-gray-200 px-6 py-4 space-y-4">
      {/* Search and Filters Row */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Search */}
        <div className="relative flex-1 min-w-[200px] max-w-md">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            type="text"
            placeholder="Search assets by name, tag, or serial..."
            value={filters.search}
            onChange={(e) => onFilterChange({ search: e.target.value })}
            className="pl-10"
          />
        </div>

        {/* Status Filter */}
        <Select
          value={toSelectValue(filters.status)}
          onValueChange={(value) => onFilterChange({ status: fromSelectValue(value) })}
        >
          <SelectTrigger className="w-[140px]">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            {ASSET_STATUSES.map((status) => (
              <SelectItem key={status.value} value={status.value}>
                {status.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Type Filter */}
        <Select
          value={toSelectValue(filters.assetType)}
          onValueChange={(value) => onFilterChange({ assetType: fromSelectValue(value) })}
        >
          <SelectTrigger className="w-[140px]">
            <SelectValue placeholder="Type" />
          </SelectTrigger>
          <SelectContent>
            {ASSET_TYPES.map((type) => (
              <SelectItem key={type.value} value={type.value}>
                {type.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Company Filter */}
        {companies && companies.length > 1 && (
          <Select
            value={toSelectValue(filters.company)}
            onValueChange={(value) => onFilterChange({ company: fromSelectValue(value) })}
          >
            <SelectTrigger className="w-[160px]">
              <SelectValue placeholder="Company" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL_VALUE}>All Companies</SelectItem>
              {companies.map((company) => (
                <SelectItem key={company.id} value={company.id}>
                  {company.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Clear Filters */}
        {hasActiveFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={handleClearFilters}
            className="text-gray-500 hover:text-gray-700"
          >
            <X className="w-4 h-4 mr-1" />
            Clear
          </Button>
        )}
      </div>

      {/* Quick View Presets */}
      <div className="flex items-center gap-2">
        <span className="text-xs text-gray-500 mr-1">
          <Filter className="w-3 h-3 inline-block mr-1" />
          Quick views:
        </span>
        <div className="flex flex-wrap gap-2">
          {QUICK_VIEW_PRESETS.map((preset) => {
            const Icon = preset.icon
            const isActive = matchingPreset === preset.id

            return (
              <Button
                key={preset.id}
                variant={isActive ? 'default' : 'outline'}
                size="sm"
                onClick={() => handlePresetClick(preset)}
                className={`h-8 ${
                  isActive
                    ? 'bg-blue-600 hover:bg-blue-700 text-white'
                    : 'hover:bg-gray-100'
                }`}
                title={preset.description}
              >
                <Icon className="w-3.5 h-3.5 mr-1.5" />
                {preset.label}
                {preset.forRole && (
                  <Badge
                    variant="outline"
                    className={`ml-1.5 text-[10px] px-1 py-0 ${
                      isActive ? 'border-white/30 text-white/80' : 'text-gray-400'
                    }`}
                  >
                    {preset.forRole[0]}
                  </Badge>
                )}
              </Button>
            )
          })}
        </div>
      </div>

      {/* Active Filters Summary */}
      {hasActiveFilters && (
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <span>Filtering by:</span>
          {filters.search && (
            <Badge variant="secondary" className="gap-1">
              Search: {filters.search}
              <button
                onClick={() => onFilterChange({ search: '' })}
                className="ml-1 hover:text-gray-700"
              >
                <X className="w-3 h-3" />
              </button>
            </Badge>
          )}
          {filters.status && (
            <Badge variant="secondary" className="gap-1">
              Status: {filters.status}
              <button
                onClick={() => onFilterChange({ status: '' })}
                className="ml-1 hover:text-gray-700"
              >
                <X className="w-3 h-3" />
              </button>
            </Badge>
          )}
          {filters.assetType && (
            <Badge variant="secondary" className="gap-1">
              Type: {filters.assetType.replace(/_/g, ' ')}
              <button
                onClick={() => onFilterChange({ assetType: '' })}
                className="ml-1 hover:text-gray-700"
              >
                <X className="w-3 h-3" />
              </button>
            </Badge>
          )}
          {filters.company && companies && (
            <Badge variant="secondary" className="gap-1">
              Company: {companies.find((c) => c.id === filters.company)?.name}
              <button
                onClick={() => onFilterChange({ company: '' })}
                className="ml-1 hover:text-gray-700"
              >
                <X className="w-3 h-3" />
              </button>
            </Badge>
          )}
        </div>
      )}
    </div>
  )
}
