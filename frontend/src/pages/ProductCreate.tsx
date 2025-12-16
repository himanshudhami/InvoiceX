import { useNavigate } from 'react-router-dom'
import { ProductForm } from '@/components/forms/ProductForm'

const ProductCreate = () => {
  const navigate = useNavigate()

  const handleSuccess = () => {
    navigate('/products')
  }

  const handleCancel = () => {
    navigate('/products')
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Add Product</h1>
        <p className="text-gray-600 mt-2">Create a new product or service</p>
      </div>

      <div className="bg-white rounded-lg shadow p-6">
        <ProductForm
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      </div>
    </div>
  )
}

export default ProductCreate