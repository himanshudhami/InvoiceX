import { useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Edit, Trash2, FileText, CheckCircle, XCircle, Download } from 'lucide-react'
import { useCompanyContext } from '@/contexts/CompanyContext'
import {
  useDocumentCategoriesByCompany,
  useCreateDocumentCategory,
  useUpdateDocumentCategory,
  useDeleteDocumentCategory,
  useSeedDocumentCategories,
} from '@/hooks/api/useDocumentCategories'
import {
  DocumentCategory,
  CreateDocumentCategoryDto,
  UpdateDocumentCategoryDto,
} from '@/services/api/documentCategoryService'
import { cn } from '@/lib/utils'

const DocumentCategoriesManagement = () => {
  const { selectedCompanyId, selectedCompany } = useCompanyContext()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<DocumentCategory | null>(null)
  const [deletingCategory, setDeletingCategory] = useState<DocumentCategory | null>(null)

  const { data: categories = [], isLoading, error, refetch } = useDocumentCategoriesByCompany(
    selectedCompanyId || '',
    !!selectedCompanyId
  )
  const createCategory = useCreateDocumentCategory()
  const updateCategory = useUpdateDocumentCategory()
  const deleteCategory = useDeleteDocumentCategory()
  const seedDefaults = useSeedDocumentCategories()

  const handleEdit = (category: DocumentCategory) => {
    setEditingCategory(category)
  }

  const handleDelete = (category: DocumentCategory) => {
    setDeletingCategory(category)
  }

  const handleDeleteConfirm = async () => {
    if (deletingCategory) {
      try {
        await deleteCategory.mutateAsync(deletingCategory.id)
        setDeletingCategory(null)
      } catch (error) {
        console.error('Failed to delete category:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingCategory(null)
    refetch()
  }

  const handleSeedDefaults = async () => {
    if (selectedCompanyId) {
      await seedDefaults.mutateAsync(selectedCompanyId)
      refetch()
    }
  }

  const columns: ColumnDef<DocumentCategory>[] = [
    {
      accessorKey: 'name',
      header: 'Category Name',
      cell: ({ row }) => {
        const category = row.original
        return (
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-blue-50 rounded-lg">
              <FileText className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{category.name}</div>
              <div className="text-sm text-gray-500">{category.code}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => {
        const description = row.original.description
        return (
          <div className="text-sm text-gray-600 max-w-[300px] truncate">
            {description || '—'}
          </div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.original.isActive
        const isSystem = row.original.isSystem
        return (
          <div className="space-y-1">
            {isSystem && (
              <span className="inline-flex px-2 py-1 text-xs font-medium rounded-full bg-purple-100 text-purple-800">
                System
              </span>
            )}
            <span
              className={cn(
                'inline-flex px-2 py-1 text-xs font-medium rounded-full',
                isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
              )}
            >
              {isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        )
      },
    },
    {
      accessorKey: 'requiresFinancialYear',
      header: 'Financial Year',
      cell: ({ row }) => {
        const requires = row.original.requiresFinancialYear
        return requires ? (
          <CheckCircle className="w-5 h-5 text-green-600" />
        ) : (
          <XCircle className="w-5 h-5 text-gray-400" />
        )
      },
    },
    {
      accessorKey: 'displayOrder',
      header: 'Order',
      cell: ({ row }) => (
        <span className="text-sm text-gray-600">{row.original.displayOrder}</span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const category = row.original
        const isSystem = category.isSystem
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(category)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit category"
            >
              <Edit size={16} />
            </button>
            {!isSystem && (
              <button
                onClick={() => handleDelete(category)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Delete category"
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>
        )
      },
    },
  ]

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-500">Please select a company to manage document categories.</div>
      </div>
    )
  }

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
        <div className="text-red-600 mb-4">Failed to load document categories</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  const activeCategories = categories.filter((c) => c.isActive)
  const systemCategories = categories.filter((c) => c.isSystem)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Document Categories</h1>
          <p className="text-gray-600 mt-2">
            Manage document categories for {selectedCompany?.name || 'your company'}
          </p>
        </div>
        {categories.length === 0 && (
          <button
            onClick={handleSeedDefaults}
            disabled={seedDefaults.isPending}
            className="flex items-center px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
          >
            <Download className="w-4 h-4 mr-2" />
            {seedDefaults.isPending ? 'Creating...' : 'Create Default Categories'}
          </button>
        )}
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Categories</div>
          <div className="text-2xl font-bold text-gray-900">{categories.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Active</div>
          <div className="text-2xl font-bold text-green-600">{activeCategories.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">System Categories</div>
          <div className="text-2xl font-bold text-purple-600">{systemCategories.length}</div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={categories}
            searchPlaceholder="Search categories..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Category"
          />
        </div>
      </div>

      {/* Create Category Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Document Category"
        size="lg"
      >
        <DocumentCategoryForm
          companyId={selectedCompanyId}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Category Drawer */}
      <Drawer
        isOpen={!!editingCategory}
        onClose={() => setEditingCategory(null)}
        title="Edit Document Category"
        size="lg"
      >
        {editingCategory && (
          <DocumentCategoryForm
            companyId={selectedCompanyId}
            category={editingCategory}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingCategory(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingCategory}
        onClose={() => setDeletingCategory(null)}
        title="Delete Document Category"
        size="sm"
      >
        {deletingCategory && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the category{' '}
              <strong>{deletingCategory.name}</strong>? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingCategory(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteCategory.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteCategory.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

// Form Component
interface DocumentCategoryFormProps {
  companyId: string
  category?: DocumentCategory
  onSuccess: () => void
  onCancel: () => void
}

const DocumentCategoryForm = ({
  companyId,
  category,
  onSuccess,
  onCancel,
}: DocumentCategoryFormProps) => {
  const [formData, setFormData] = useState<CreateDocumentCategoryDto>({
    companyId,
    name: '',
    code: '',
    description: '',
    isActive: true,
    requiresFinancialYear: false,
    displayOrder: 0,
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  const createCategory = useCreateDocumentCategory()
  const updateCategory = useUpdateDocumentCategory()

  const isEditing = !!category
  const isLoading = createCategory.isPending || updateCategory.isPending

  useEffect(() => {
    if (category) {
      setFormData({
        companyId: category.companyId,
        name: category.name,
        code: category.code,
        description: category.description || '',
        isActive: category.isActive,
        requiresFinancialYear: category.requiresFinancialYear,
        displayOrder: category.displayOrder,
      })
    }
  }, [category])

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {}

    if (!formData.name?.trim()) {
      newErrors.name = 'Name is required'
    }
    if (!formData.code?.trim()) {
      newErrors.code = 'Code is required'
    } else if (!/^[A-Z_]+$/.test(formData.code)) {
      newErrors.code = 'Code must be uppercase letters and underscores only'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && category) {
        const updateData: UpdateDocumentCategoryDto = {
          name: formData.name,
          code: formData.code,
          description: formData.description,
          isActive: formData.isActive,
          requiresFinancialYear: formData.requiresFinancialYear,
          displayOrder: formData.displayOrder,
        }
        await updateCategory.mutateAsync({ id: category.id, data: updateData })
      } else {
        await createCategory.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Form submission error:', error)
    }
  }

  const handleChange = (
    field: keyof CreateDocumentCategoryDto,
    value: string | number | boolean
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }))
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Name */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Category Name *
        </label>
        <input
          type="text"
          value={formData.name}
          onChange={(e) => handleChange('name', e.target.value)}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.name ? 'border-red-500' : 'border-gray-300'
          )}
          placeholder="e.g., Offer Letter"
        />
        {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
      </div>

      {/* Code */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Category Code *
        </label>
        <input
          type="text"
          value={formData.code}
          onChange={(e) => handleChange('code', e.target.value.toUpperCase())}
          className={cn(
            'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
            errors.code ? 'border-red-500' : 'border-gray-300'
          )}
          placeholder="e.g., OFFER_LETTER"
          disabled={isEditing && category?.isSystem}
        />
        {errors.code && <p className="text-red-500 text-sm mt-1">{errors.code}</p>}
        <p className="text-xs text-gray-500 mt-1">
          Uppercase letters and underscores only
        </p>
      </div>

      {/* Description */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Description
        </label>
        <textarea
          value={formData.description}
          onChange={(e) => handleChange('description', e.target.value)}
          rows={3}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Optional description"
        />
      </div>

      {/* Display Order */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Display Order
        </label>
        <input
          type="number"
          value={formData.displayOrder}
          onChange={(e) => handleChange('displayOrder', parseInt(e.target.value) || 0)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          min="0"
        />
      </div>

      {/* Checkboxes */}
      <div className="space-y-3">
        <div className="flex items-center">
          <input
            id="isActive"
            type="checkbox"
            checked={formData.isActive}
            onChange={(e) => handleChange('isActive', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
            Active
          </label>
        </div>
        <div className="flex items-center">
          <input
            id="requiresFinancialYear"
            type="checkbox"
            checked={formData.requiresFinancialYear}
            onChange={(e) => handleChange('requiresFinancialYear', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="requiresFinancialYear" className="ml-2 block text-sm text-gray-900">
            Requires Financial Year
          </label>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t border-gray-200">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-primary-foreground bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Category' : 'Create Category'}
        </button>
      </div>
    </form>
  )
}

export default DocumentCategoriesManagement
