import { FC } from 'react'
import { Employee } from '@/services/api/types'
import { format } from 'date-fns'
import { Mail, Phone, MapPin, Calendar, Building, CreditCard, User, Users } from 'lucide-react'

interface OverviewTabProps {
  employee: Employee
}

const DetailRow: FC<{ icon: React.ReactNode; label: string; value: string | undefined | null }> = ({
  icon,
  label,
  value,
}) => (
  <div className="flex items-start gap-3 py-2">
    <div className="text-gray-400 mt-0.5">{icon}</div>
    <div className="flex-1 min-w-0">
      <div className="text-xs text-gray-500 uppercase tracking-wide">{label}</div>
      <div className="text-sm text-gray-900 truncate">{value || '—'}</div>
    </div>
  </div>
)

export const OverviewTab: FC<OverviewTabProps> = ({ employee }) => {
  return (
    <div className="space-y-6 p-4">
      {/* Contact Information */}
      <section>
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Contact Information</h3>
        <div className="bg-gray-50 rounded-lg p-3 space-y-1">
          <DetailRow icon={<Mail className="w-4 h-4" />} label="Email" value={employee.email} />
          <DetailRow icon={<Phone className="w-4 h-4" />} label="Phone" value={employee.phone} />
          <DetailRow
            icon={<MapPin className="w-4 h-4" />}
            label="Address"
            value={
              [employee.addressLine1, employee.city, employee.state, employee.zipCode]
                .filter(Boolean)
                .join(', ') || undefined
            }
          />
        </div>
      </section>

      {/* Employment Details */}
      <section>
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Employment Details</h3>
        <div className="bg-gray-50 rounded-lg p-3 space-y-1">
          <DetailRow icon={<User className="w-4 h-4" />} label="Employee ID" value={employee.employeeId} />
          <DetailRow icon={<Building className="w-4 h-4" />} label="Department" value={employee.department} />
          <DetailRow icon={<User className="w-4 h-4" />} label="Designation" value={employee.designation} />
          <DetailRow icon={<User className="w-4 h-4" />} label="Contract Type" value={employee.contractType} />
          <DetailRow icon={<Users className="w-4 h-4" />} label="Reporting Manager" value={employee.managerName} />
          <DetailRow
            icon={<Calendar className="w-4 h-4" />}
            label="Hire Date"
            value={employee.hireDate ? format(new Date(employee.hireDate), 'MMM dd, yyyy') : undefined}
          />
          <DetailRow icon={<Building className="w-4 h-4" />} label="Company" value={employee.company} />
        </div>
      </section>

      {/* Bank Details */}
      <section>
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Bank Details</h3>
        <div className="bg-gray-50 rounded-lg p-3 space-y-1">
          <DetailRow icon={<CreditCard className="w-4 h-4" />} label="Bank Name" value={employee.bankName} />
          <DetailRow
            icon={<CreditCard className="w-4 h-4" />}
            label="Account Number"
            value={employee.bankAccountNumber}
          />
          <DetailRow icon={<CreditCard className="w-4 h-4" />} label="IFSC Code" value={employee.ifscCode} />
          <DetailRow icon={<User className="w-4 h-4" />} label="PAN Number" value={employee.panNumber} />
        </div>
      </section>
    </div>
  )
}
