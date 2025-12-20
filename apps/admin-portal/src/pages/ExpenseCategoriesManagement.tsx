import { useState, useEffect } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Edit, Trash2, Receipt, CheckCircle, XCircle, Download, IndianRupee } from 'lucide-react'
import { useCompanyContext } from '@/contexts/CompanyContext'
import {
  useExpenseCategoriesByCompany,
  useCreateExpenseCategory,
  useUpdateExpenseCategory,
  useDeleteExpenseCategory,
  useSeedExpenseCategories,
} from '@/hooks/api/useExpenseCategories'
import {
  ExpenseCategory,
  CreateExpenseCategoryDto,
  UpdateExpenseCategoryDto,
} from '@/services/api/expenseCategoryService'
import { cn } from '@/lib/utils'

const ExpenseCategoriesManagement = () => {
  const { selectedCompanyId, selectedCompany } = useCompanyContext()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<ExpenseCategory | null>(null)
  const [deletingCategory, setDeletingCategory] = useState<ExpenseCategory | null>(null)

  const { data: categories = [], isLoading, error, refetch } = useExpenseCategoriesByCompany(
    selectedCompanyId || '',
    !!selectedCompanyId
  )
  const createCategory = useCreateExpenseCategory()
  const updateCategory = useUpdateExpenseCategory()
  const deleteCategory = useDeleteExpenseCategory()
  const seedDefaults = useSeedExpenseCategories()

  const handleEdit = (category: ExpenseCategory) => {
    setEditingCategory(category)
  }

  const handleDelete = (category: ExpenseCategory) => {
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

  const formatCurrency = (amount?: number) => {
    if (!amount) return '—'
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount)
  }

  const columns: ColumnDef<ExpenseCategory>[] = [
    {
      accessorKey: 'name',
      header: 'Category Name',
      cell: ({ row }) => {
        const category = row.original
        return (
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-green-50 rounded-lg">
              <Receipt className="w-5 h-5 text-green-600" />
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
          <div className="text-sm text-gray-600 max-w-[250px] truncate">
            {description || '—'}
          </div>
        )
      },
    },
    {
      accessorKey: 'maxAmount',
      header: 'Max Amount',
      cell: ({ row }) => {
        const maxAmount = row.original.maxAmount
        return (
          <div className="flex items-center text-sm">
            {maxAmount ? (
              <span className="font-medium text-gray-900">{formatCurrency(maxAmount)}</span>
            ) : (
              <span className="text-gray-400">No limit</span>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'requiresReceipt',
      header: 'Receipt Required',
      cell: ({ row }) => {
        const requires = row.original.requiresReceipt
        return requires ? (
          <div className="flex items-center text-green-600">
            <CheckCircle className="w-5 h-5" />
            <span className="ml-1 text-sm">Required</span>
          </div>
        ) : (
          <div className="flex items-center text-gray-400">
            <XCircle className="w-5 h-5" />
            <span className="ml-1 text-sm">Optional</span>
          </div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.original.isActive
        return (
          <span
            className={cn(
              'inline-flex px-2 py-1 text-xs font-medium rounded-full',
              isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
            )}
          >
            {isActive ? 'Active' : 'Inactive'}
          </span>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const category = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(category)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit category"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(category)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete category"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (!selectedCompanyId) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-500">Please select a company to manage expense categories.</div>
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
        <div className="text-red-600 mb-4">Failed to load expense categories</div>
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
  const categoriesWithLimit = categories.filter((c) => c.maxAmount && c.maxAmount > 0)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Expense Categories</h1>
          <p className="text-gray-600 mt-2">
            Manage expense categories for {selectedCompany?.name || 'your company'}
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
          <div className="text-sm font-medium text-gray-500">With Spending Limits</div>
          <div className="text-2xl font-bold text-blue-600">{categoriesWithLimit.length}</div>
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
        title="Create Expense Category"
        size="lg"
      >
        <ExpenseCategoryForm
          companyId={selectedCompanyId}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Category Drawer */}
      <Drawer
        isOpen={!!editingCategory}
        onClose={() => setEditingCategory(null)}
        title="Edit Expense Category"
        size="lg"
      >
        {editingCategory && (
          <ExpenseCategoryForm
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
        title="Delete Expense Category"
        size="sm"
      >
        {deletingCategory && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the category{' '}
              <strong>{deletingCategory.name}</strong>? This action cannot be undone and may
              affect existing expense claims.
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
interface ExpenseCategoryFormProps {
  companyId: string
  category?: ExpenseCategory
  onSuccess: () => void
  onCancel: () => void
}

const ExpenseCategoryForm = ({
  companyId,
  category,
  onSuccess,
  onCancel,
}: ExpenseCategoryFormProps) => {
  const [formData, setFormData] = useState<CreateExpenseCategoryDto>({
    companyId,
    name: '',
    code: '',
    description: '',
    isActive: true,
    maxAmount: undefined,
    requiresReceipt: true,
  })
  const [errors, setErrors] = useState<Record<string, string>>({})

  const createCategory = useCreateExpenseCategory()
  const updateCategory = useUpdateExpenseCategory()

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
        maxAmount: category.maxAmount,
        requiresReceipt: category.requiresReceipt,
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
    if (formData.maxAmount !== undefined && formData.maxAmount < 0) {
      newErrors.maxAmount = 'Max amount cannot be negative'
    }

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!validateForm()) return

    try {
      if (isEditing && category) {
        const updateData: UpdateExpenseCategoryDto = {
          name: formData.name,
          code: formData.code,
          description: formData.description,
          isActive: formData.isActive,
          maxAmount: formData.maxAmount,
          requiresReceipt: formData.requiresReceipt,
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
    field: keyof CreateExpenseCategoryDto,
    value: string | number | boolean | undefined
  ) => {
    setFormData((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: '' }))
    }
  }

  // Common expense category presets
  const presets = [
    { name: 'Travel', code: 'TRAVEL', maxAmount: 50000 },
    { name: 'Meals & Entertainment', code: 'MEALS', maxAmount: 5000 },
    { name: 'Office Supplies', code: 'OFFICE_SUPPLIES', maxAmount: 10000 },
    { name: 'Software & Subscriptions', code: 'SOFTWARE', maxAmount: 25000 },
    { name: 'Fuel & Conveyance', code: 'FUEL', maxAmount: 15000 },
    { name: 'Medical', code: 'MEDICAL', maxAmount: 20000 },
  ]

  const handlePresetSelect = (preset: { name: string; code: string; maxAmount: number }) => {
    setFormData((prev) => ({
      ...prev,
      name: preset.name,
      code: preset.code,
      maxAmount: preset.maxAmount,
    }))
    setErrors({})
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Quick Presets (only for new categories) */}
      {!isEditing && (
        <div className="space-y-3">
          <h3 className="text-lg font-medium text-gray-900 border-b border-gray-200 pb-2">
            Quick Presets
          </h3>
          <div className="grid grid-cols-2 gap-2">
            {presets.map((preset, index) => (
              <button
                key={index}
                type="button"
                onClick={() => handlePresetSelect(preset)}
                className="p-3 text-left border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-ring transition-colors"
              >
                <div className="font-medium text-gray-900">{preset.name}</div>
                <div className="text-xs text-gray-500">
                  Max: {new Intl.NumberFormat('en-IN', {
                    style: 'currency',
                    currency: 'INR',
                    maximumFractionDigits: 0,
                  }).format(preset.maxAmount)}
                </div>
              </button>
            ))}
          </div>
          <div className="text-center text-gray-500 text-sm">
            Or create a custom category below
          </div>
        </div>
      )}

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
          placeholder="e.g., Travel Expenses"
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
          placeholder="e.g., TRAVEL"
        />
        {errors.code && <p className="text-red-500 text-sm mt-1">{errors.code}</p>}
        <p className="text-xs text-gray-500 mt-1">
          Uppercase letters and underscores only
        </p>
      </div>

      {/* Description */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          value={formData.description}
          onChange={(e) => handleChange('description', e.target.value)}
          rows={3}
          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          placeholder="Optional description of this expense category"
        />
      </div>

      {/* Max Amount */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          Maximum Amount (per claim)
        </label>
        <div className="relative">
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <IndianRupee className="w-4 h-4 text-gray-400" />
          </div>
          <input
            type="number"
            value={formData.maxAmount || ''}
            onChange={(e) =>
              handleChange('maxAmount', e.target.value ? parseInt(e.target.value) : undefined)
            }
            className={cn(
              'w-full pl-8 pr-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.maxAmount ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="No limit"
            min="0"
          />
        </div>
        {errors.maxAmount && <p className="text-red-500 text-sm mt-1">{errors.maxAmount}</p>}
        <p className="text-xs text-gray-500 mt-1">
          Leave empty for no limit
        </p>
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
            id="requiresReceipt"
            type="checkbox"
            checked={formData.requiresReceipt}
            onChange={(e) => handleChange('requiresReceipt', e.target.checked)}
            className="h-4 w-4 text-primary focus:ring-ring border-gray-300 rounded"
          />
          <label htmlFor="requiresReceipt" className="ml-2 block text-sm text-gray-900">
            Receipt/Invoice Required
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

export default ExpenseCategoriesManagement
