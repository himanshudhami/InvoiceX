import { useNavigate, useParams } from 'react-router-dom'
import { useProduct } from '@/features/products/hooks'
import { Button } from '@/components/ui/button'
import { formatCurrency } from '@/lib/currency'

export default function ProductView() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: product, isLoading, error } = useProduct(id!, !!id)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    )
  }

  if (error || !product) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load product</div>
        <Button onClick={() => navigate('/products')} variant="outline">
          Back to Products
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{product.name}</h1>
          <p className="text-gray-600 mt-1">{product.sku || 'No SKU'}</p>
        </div>
        <div className="space-x-2">
          <Button variant="outline" onClick={() => navigate('/products')}>
            Back
          </Button>
          <Button onClick={() => navigate(`/products/${product.id}/edit`)}>Edit</Button>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow p-6 space-y-4">
        <Section label="Details">
          <Detail label="Description" value={product.description} />
          <Detail label="Category" value={product.category} />
          <Detail label="Type" value={product.type} />
        </Section>

        <Section label="Pricing">
          <Detail label="Unit price" value={formatCurrency(product.unitPrice)} />
          <Detail label="Unit" value={product.unit} />
          <Detail label="Tax rate" value={product.taxRate != null ? `${product.taxRate}%` : '—'} />
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
