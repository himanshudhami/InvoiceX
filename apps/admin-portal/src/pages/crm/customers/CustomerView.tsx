import { useNavigate, useParams } from 'react-router-dom'
import { useCustomer } from '@/features/customers/hooks'
import { Button } from '@/components/ui/button'

export default function CustomerView() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: customer, isLoading, error } = useCustomer(id!, !!id)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    )
  }

  if (error || !customer) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load customer</div>
        <Button onClick={() => navigate('/customers')} variant="outline">
          Back to Customers
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{customer.name}</h1>
          <p className="text-gray-600 mt-1">{customer.companyName || 'Individual'}</p>
        </div>
        <div className="space-x-2">
          <Button variant="outline" onClick={() => navigate('/customers')}>
            Back
          </Button>
          <Button onClick={() => navigate(`/customers/${customer.id}/edit`)}>Edit</Button>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6 space-y-4">
        <Section label="Contact">
          <Detail label="Email" value={customer.email} />
          <Detail label="Phone" value={customer.phone} />
        </Section>
        <Section label="Company">
          <Detail label="Company name" value={customer.companyName} />
          <Detail label="Tax number" value={customer.taxNumber} />
        </Section>
        <Section label="Address">
          <Detail label="Line 1" value={customer.addressLine1} />
          <Detail label="Line 2" value={customer.addressLine2} />
          <Detail
            label="City/State/Zip"
            value={[customer.city, customer.state, customer.zipCode].filter(Boolean).join(', ')}
          />
          <Detail label="Country" value={customer.country} />
        </Section>
        <Section label="Notes">
          <Detail label="Notes" value={customer.notes} />
        </Section>
      </div>
    </div>
  )
}

function Section({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div>
      <h2 className="text-sm font-semibold text-gray-700 mb-2">{label}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">{children}</div>
    </div>
  )
}

function Detail({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <div className="text-sm">
      <div className="text-gray-500">{label}</div>
      <div className="text-gray-900">{value != null && value !== '' ? value : '—'}</div>
    </div>
  )
}
