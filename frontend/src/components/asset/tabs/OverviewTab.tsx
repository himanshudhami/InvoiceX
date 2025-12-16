import { FC } from 'react'
import type { Asset, AssetAssignment } from '@/services/api/types'
import { formatCurrency } from '@/lib/currency'
import { formatDate } from '@/lib/date'
import {
  Calendar,
  MapPin,
  Building2,
  Tag,
  FileText,
  User,
  DollarSign,
  Clock,
  Shield,
  Laptop,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'

interface OverviewTabProps {
  asset: Asset
  currentAssignment: AssetAssignment | null
}

const InfoRow: FC<{
  icon: React.ElementType
  label: string
  value?: string | number | null
  badge?: boolean
}> = ({ icon: Icon, label, value, badge }) => {
  if (!value && value !== 0) return null

  return (
    <div className="flex items-start gap-3 py-2">
      <Icon className="w-4 h-4 text-gray-400 mt-0.5 flex-shrink-0" />
      <div className="flex-1 min-w-0">
        <div className="text-xs text-gray-500">{label}</div>
        {badge ? (
          <Badge variant="outline" className="mt-0.5">
            {String(value)}
          </Badge>
        ) : (
          <div className="text-sm text-gray-900 truncate">{String(value)}</div>
        )}
      </div>
    </div>
  )
}

const SectionHeader: FC<{ title: string }> = ({ title }) => (
  <h3 className="text-sm font-medium text-gray-500 uppercase tracking-wider mb-2 mt-4 first:mt-0">
    {title}
  </h3>
)

export const OverviewTab: FC<OverviewTabProps> = ({ asset, currentAssignment }) => {
  return (
    <div className="p-4 space-y-1">
      {/* Current Assignment Section */}
      {currentAssignment && (
        <>
          <SectionHeader title="Current Assignment" />
          <div className="bg-blue-50 rounded-lg p-3 border border-blue-100">
            <div className="flex items-center gap-2">
              <User className="w-4 h-4 text-blue-600" />
              <span className="text-sm font-medium text-blue-900">
                {currentAssignment.targetType === 'employee' ? 'Assigned to Employee' : 'Assigned to Company'}
              </span>
            </div>
            <div className="mt-1 text-xs text-blue-700">
              Since {formatDate(currentAssignment.assignedOn)}
              {currentAssignment.conditionOut && ` • Condition: ${currentAssignment.conditionOut}`}
            </div>
            {currentAssignment.notes && (
              <div className="mt-1 text-xs text-blue-600 italic">{currentAssignment.notes}</div>
            )}
          </div>
        </>
      )}

      {/* Basic Information */}
      <SectionHeader title="Asset Details" />
      <InfoRow icon={Laptop} label="Asset Type" value={asset.assetType?.replace(/_/g, ' ')} />
      <InfoRow icon={Tag} label="Serial Number" value={asset.serialNumber} />
      <InfoRow icon={MapPin} label="Location" value={asset.location} />
      <InfoRow icon={Building2} label="Vendor" value={asset.vendor} />
      {asset.description && (
        <InfoRow icon={FileText} label="Description" value={asset.description} />
      )}

      {/* Purchase & Acquisition */}
      <SectionHeader title="Purchase Information" />
      <InfoRow
        icon={DollarSign}
        label="Purchase Cost"
        value={asset.purchaseCost ? formatCurrency(asset.purchaseCost, asset.currency) : null}
      />
      <InfoRow icon={Tag} label="Purchase Type" value={asset.purchaseType} badge />
      <InfoRow icon={Calendar} label="Purchase Date" value={asset.purchaseDate ? formatDate(asset.purchaseDate) : null} />
      <InfoRow icon={Calendar} label="In Service Date" value={asset.inServiceDate ? formatDate(asset.inServiceDate) : null} />
      <InfoRow icon={FileText} label="Invoice Reference" value={asset.invoiceReference} />

      {/* Warranty & Maintenance */}
      <SectionHeader title="Warranty" />
      <InfoRow
        icon={Shield}
        label="Warranty Expires"
        value={asset.warrantyExpiration ? formatDate(asset.warrantyExpiration) : 'No warranty'}
      />
      {asset.warrantyExpiration && (
        <WarrantyStatus expirationDate={asset.warrantyExpiration} />
      )}

      {/* Depreciation */}
      {asset.depreciationMethod && (
        <>
          <SectionHeader title="Depreciation" />
          <InfoRow icon={DollarSign} label="Method" value={asset.depreciationMethod} badge />
          <InfoRow
            icon={Clock}
            label="Useful Life"
            value={asset.usefulLifeMonths ? `${asset.usefulLifeMonths} months` : null}
          />
          <InfoRow
            icon={Calendar}
            label="Depreciation Start"
            value={asset.depreciationStartDate ? formatDate(asset.depreciationStartDate) : null}
          />
          <InfoRow
            icon={DollarSign}
            label="Salvage Value"
            value={asset.salvageValue ? formatCurrency(asset.salvageValue, asset.currency) : null}
          />
        </>
      )}

      {/* Notes */}
      {asset.notes && (
        <>
          <SectionHeader title="Notes" />
          <div className="text-sm text-gray-700 bg-gray-50 rounded-lg p-3 whitespace-pre-wrap">
            {asset.notes}
          </div>
        </>
      )}
    </div>
  )
}

const WarrantyStatus: FC<{ expirationDate: string }> = ({ expirationDate }) => {
  const expDate = new Date(expirationDate)
  const today = new Date()
  const daysUntilExpiry = Math.ceil((expDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))

  if (daysUntilExpiry < 0) {
    return (
      <div className="flex items-center gap-2 ml-7 mt-1">
        <Badge variant="destructive">Expired</Badge>
        <span className="text-xs text-gray-500">
          {Math.abs(daysUntilExpiry)} days ago
        </span>
      </div>
    )
  }

  if (daysUntilExpiry <= 30) {
    return (
      <div className="flex items-center gap-2 ml-7 mt-1">
        <Badge variant="outline" className="border-orange-300 text-orange-600">
          Expiring Soon
        </Badge>
        <span className="text-xs text-gray-500">
          {daysUntilExpiry} days remaining
        </span>
      </div>
    )
  }

  return (
    <div className="flex items-center gap-2 ml-7 mt-1">
      <Badge variant="outline" className="border-green-300 text-green-600">
        Active
      </Badge>
      <span className="text-xs text-gray-500">
        {daysUntilExpiry} days remaining
      </span>
    </div>
  )
}
