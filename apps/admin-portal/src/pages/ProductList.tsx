import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryState, parseAsString } from 'nuqs'
import { useProducts, useDeleteProduct } from '@/features/products/hooks'
import { Product } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { useNavigate } from 'react-router-dom'
import { Eye, Edit, Trash2 } from 'lucide-react'

const ProductList = () => {
  const navigate = useNavigate()
  const [deletingProduct, setDeletingProduct] = useState<Product | null>(null)
  
  // URL-backed search state with nuqs - persists on refresh
  const [search, setSearch] = useQueryState('search', parseAsString.withDefault(''))
  
  const { data: products = [], isLoading, error, refetch } = useProducts()
  const deleteProduct = useDeleteProduct()

  const handleDelete = (product: Product) => {
    setDeletingProduct(product)
  }

  const handleDeleteConfirm = async () => {
    if (deletingProduct) {
      try {
        await deleteProduct.mutateAsync(deletingProduct.id)
        setDeletingProduct(null)
      } catch (error) {
        console.error('Failed to delete product:', error)
      }
    }
  }

  const columns: ColumnDef<Product>[] = [
    {
      accessorKey: 'name',
      header: 'Product Name',
      cell: ({ row }) => {
        const product = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{product.name}</div>
            {product.sku && (
              <div className="text-sm text-gray-500">SKU: {product.sku}</div>
            )}
            {product.description && (
              <div className="text-sm text-gray-600 truncate max-w-xs">
                {product.description}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'category',
      header: 'Category',
      cell: ({ row }) => {
        const category = row.getValue('category') as string
        const type = row.original.type
        return (
          <div>
            {category && (
              <div className="text-sm text-gray-900">{category}</div>
            )}
            <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
              type === 'service' 
                ? 'bg-purple-100 text-purple-800'
                : 'bg-blue-100 text-blue-800'
            }`}>
              {type}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'unitPrice',
      header: 'Price',
      cell: ({ row }) => {
        const unitPrice = row.getValue('unitPrice') as number
        const unit = row.original.unit
        return (
          <div>
            <div className="font-medium text-gray-900">
              ${unitPrice.toFixed(2)}
            </div>
            {unit && (
              <div className="text-sm text-gray-500">per {unit}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.getValue('isActive') as boolean
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
            isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-800'
          }`}>
            {isActive ? 'Active' : 'Inactive'}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const product = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => navigate(`/products/${product.id}`)}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="View product"
            >
              <Eye size={16} />
            </button>
            <button
              onClick={() => navigate(`/products/${product.id}/edit`)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit product"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(product)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete product"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load products</div>
        <button
          onClick={() => refetch?.()}
          className="px-4 py-2 border rounded-md hover:bg-gray-50"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Products & Services</h1>
          <p className="text-gray-600 mt-2">Manage your product catalog</p>
        </div>
        <button
          onClick={() => navigate('/products/new')}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          Add Product
        </button>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={products}
            searchPlaceholder="Search products..."
            initialSearch={search}
            onSearchChange={(value) => setSearch(value || null)}
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingProduct}
        onClose={() => setDeletingProduct(null)}
        title="Delete Product"
        size="sm"
      >
        {deletingProduct && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingProduct.name}</strong>? 
              This action cannot be undone and may affect related invoices.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingProduct(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteProduct.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteProduct.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default ProductList
