import { useParams, useNavigate } from 'react-router-dom'
import { useProduct } from '@/hooks/api/useProducts'
import { ProductForm } from '@/components/forms/ProductForm'

const ProductEdit = () => {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: product, isLoading, error } = useProduct(id!)

  const handleSuccess = () => {
    navigate('/products')
  }

  const handleCancel = () => {
    navigate('/products')
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error || !product) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load product</div>
        <button
          onClick={() => navigate('/products')}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Back to Products
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Edit Product</h1>
        <p className="text-gray-600 mt-2">Update product information</p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <ProductForm
          product={product}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default ProductEdit