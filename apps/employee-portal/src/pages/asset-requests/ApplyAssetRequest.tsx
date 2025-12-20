import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import * as Select from '@radix-ui/react-select'
import { ChevronDown, Check, AlertCircle, Laptop, Info } from 'lucide-react'
import { assetRequestApi } from '@/api'
import { PageHeader } from '@/components/layout'
import { Card, Button, Input, Textarea, PageLoader } from '@/components/ui'
import { cn } from '@/utils/cn'
import type { CreateAssetRequest, AssetRequestPriority, AssetPriority } from '@/types'

interface FormValues {
  assetType: string
  category: string
  title: string
  description: string
  justification: string
  specifications: string
  priority: AssetRequestPriority
  quantity: number
  estimatedBudget: string
  requestedByDate: string
}

const assetTypes = [
  { value: 'hardware', label: 'Hardware' },
  { value: 'software', label: 'Software' },
  { value: 'peripheral', label: 'Peripheral' },
  { value: 'furniture', label: 'Furniture' },
  { value: 'other', label: 'Other' },
]

export function ApplyAssetRequestPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: categories, isLoading: categoriesLoading } = useQuery<string[]>({
    queryKey: ['asset-categories'],
    queryFn: assetRequestApi.getCategories,
  })

  const { data: priorities, isLoading: prioritiesLoading } = useQuery<AssetPriority[]>({
    queryKey: ['asset-priorities'],
    queryFn: assetRequestApi.getPriorities,
  })

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      assetType: '',
      category: '',
      title: '',
      description: '',
      justification: '',
      specifications: '',
      priority: 'normal',
      quantity: 1,
      estimatedBudget: '',
      requestedByDate: '',
    },
  })

  const submitMutation = useMutation({
    mutationFn: (data: CreateAssetRequest) => assetRequestApi.submitRequest(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-asset-requests'] })
      queryClient.invalidateQueries({ queryKey: ['portal-dashboard'] })
      navigate('/assets')
    },
    onError: (error: Error) => {
      setServerError(error.message || 'Failed to submit asset request')
    },
  })

  const onSubmit = (data: FormValues) => {
    setServerError(null)
    submitMutation.mutate({
      assetType: data.assetType,
      category: data.category,
      title: data.title,
      description: data.description || undefined,
      justification: data.justification || undefined,
      specifications: data.specifications || undefined,
      priority: data.priority,
      quantity: data.quantity,
      estimatedBudget: data.estimatedBudget ? parseFloat(data.estimatedBudget) : undefined,
      requestedByDate: data.requestedByDate || undefined,
    })
  }

  const assetType = watch('assetType')
  const category = watch('category')
  const title = watch('title')

  if (categoriesLoading || prioritiesLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader title="Request an Asset" showBack />

      <form onSubmit={handleSubmit(onSubmit)} className="px-4 py-4 space-y-4">
        {serverError && (
          <Card className="p-3 bg-red-50 border-red-200">
            <div className="flex items-start gap-2">
              <AlertCircle className="text-red-500 flex-shrink-0 mt-0.5" size={18} />
              <p className="text-sm text-red-600">{serverError}</p>
            </div>
          </Card>
        )}

        {/* Info Card */}
        <Card className="p-3 bg-blue-50 border-blue-200">
          <div className="flex items-start gap-2">
            <Info className="text-blue-500 flex-shrink-0 mt-0.5" size={18} />
            <p className="text-sm text-blue-600">
              Asset requests require manager approval. You'll be notified once your request is reviewed.
            </p>
          </div>
        </Card>

        {/* Asset Type */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            Asset Type <span className="text-red-500">*</span>
          </label>
          <Controller
            name="assetType"
            control={control}
            rules={{ required: 'Please select an asset type' }}
            render={({ field }) => (
              <Select.Root value={field.value} onValueChange={field.onChange}>
                <Select.Trigger
                  className={cn(
                    'flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm',
                    'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                    !field.value && 'text-gray-400',
                    errors.assetType && 'border-red-500'
                  )}
                >
                  <Select.Value placeholder="Select asset type" />
                  <Select.Icon>
                    <ChevronDown size={16} className="text-gray-400" />
                  </Select.Icon>
                </Select.Trigger>
                <Select.Portal>
                  <Select.Content className="overflow-hidden bg-white rounded-xl shadow-lg border border-gray-200 z-50">
                    <Select.Viewport className="p-1">
                      {assetTypes.map((type) => (
                        <Select.Item
                          key={type.value}
                          value={type.value}
                          className="relative flex items-center px-4 py-3 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
                        >
                          <Select.ItemText>{type.label}</Select.ItemText>
                          <Select.ItemIndicator className="absolute right-3">
                            <Check size={16} className="text-primary-600" />
                          </Select.ItemIndicator>
                        </Select.Item>
                      ))}
                    </Select.Viewport>
                  </Select.Content>
                </Select.Portal>
              </Select.Root>
            )}
          />
          {errors.assetType && (
            <p className="mt-1.5 text-sm text-red-600">{errors.assetType.message}</p>
          )}
        </div>

        {/* Category */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            Category <span className="text-red-500">*</span>
          </label>
          <Controller
            name="category"
            control={control}
            rules={{ required: 'Please select a category' }}
            render={({ field }) => (
              <Select.Root value={field.value} onValueChange={field.onChange}>
                <Select.Trigger
                  className={cn(
                    'flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm',
                    'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                    !field.value && 'text-gray-400',
                    errors.category && 'border-red-500'
                  )}
                >
                  <Select.Value placeholder="Select category" />
                  <Select.Icon>
                    <ChevronDown size={16} className="text-gray-400" />
                  </Select.Icon>
                </Select.Trigger>
                <Select.Portal>
                  <Select.Content className="overflow-hidden bg-white rounded-xl shadow-lg border border-gray-200 z-50">
                    <Select.Viewport className="p-1">
                      {categories?.map((cat) => (
                        <Select.Item
                          key={cat}
                          value={cat}
                          className="relative flex items-center px-4 py-3 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
                        >
                          <Select.ItemText>{cat}</Select.ItemText>
                          <Select.ItemIndicator className="absolute right-3">
                            <Check size={16} className="text-primary-600" />
                          </Select.ItemIndicator>
                        </Select.Item>
                      ))}
                    </Select.Viewport>
                  </Select.Content>
                </Select.Portal>
              </Select.Root>
            )}
          />
          {errors.category && (
            <p className="mt-1.5 text-sm text-red-600">{errors.category.message}</p>
          )}
        </div>

        {/* Title */}
        <Input
          label="Title"
          placeholder="Brief title for your request"
          required
          {...register('title', { required: 'Title is required' })}
          error={errors.title?.message}
        />

        {/* Description */}
        <Textarea
          label="Description"
          placeholder="Describe the asset you need..."
          rows={3}
          {...register('description')}
        />

        {/* Justification */}
        <Textarea
          label="Justification"
          placeholder="Why do you need this asset?"
          rows={3}
          {...register('justification')}
        />

        {/* Priority & Quantity */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700">Priority</label>
            <Controller
              name="priority"
              control={control}
              render={({ field }) => (
                <Select.Root value={field.value} onValueChange={field.onChange}>
                  <Select.Trigger
                    className={cn(
                      'flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm',
                      'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20'
                    )}
                  >
                    <Select.Value />
                    <Select.Icon>
                      <ChevronDown size={16} className="text-gray-400" />
                    </Select.Icon>
                  </Select.Trigger>
                  <Select.Portal>
                    <Select.Content className="overflow-hidden bg-white rounded-xl shadow-lg border border-gray-200 z-50">
                      <Select.Viewport className="p-1">
                        {(priorities || []).map((p) => (
                          <Select.Item
                            key={p.value}
                            value={p.value}
                            className="relative flex items-center px-4 py-3 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
                          >
                            <Select.ItemText>{p.label}</Select.ItemText>
                            <Select.ItemIndicator className="absolute right-3">
                              <Check size={16} className="text-primary-600" />
                            </Select.ItemIndicator>
                          </Select.Item>
                        ))}
                      </Select.Viewport>
                    </Select.Content>
                  </Select.Portal>
                </Select.Root>
              )}
            />
          </div>

          <Input
            type="number"
            label="Quantity"
            min={1}
            {...register('quantity', { valueAsNumber: true, min: 1 })}
          />
        </div>

        {/* Specifications */}
        <Textarea
          label="Specifications (Optional)"
          placeholder="Any specific requirements like model, brand, specs..."
          rows={2}
          {...register('specifications')}
        />

        {/* Estimated Budget & Needed By Date */}
        <div className="grid grid-cols-2 gap-3">
          <Input
            type="number"
            label="Est. Budget (₹)"
            placeholder="0.00"
            step="0.01"
            min={0}
            {...register('estimatedBudget')}
          />
          <Input
            type="date"
            label="Needed By"
            {...register('requestedByDate')}
          />
        </div>

        {/* Summary Card */}
        {assetType && category && title && (
          <Card className="p-4 bg-primary-50 border-primary-100">
            <div className="flex items-start gap-3">
              <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-100">
                <Laptop className="text-primary-600" size={20} />
              </div>
              <div className="flex-1">
                <p className="text-sm font-semibold text-primary-900">{title}</p>
                <p className="text-xs text-primary-600 mt-0.5">
                  {assetTypes.find((t) => t.value === assetType)?.label} • {category}
                </p>
              </div>
            </div>
          </Card>
        )}

        {/* Submit Button */}
        <div className="pt-2">
          <Button
            type="submit"
            className="w-full"
            isLoading={submitMutation.isPending}
            disabled={!assetType || !category || !title}
          >
            Submit Request
          </Button>
        </div>
      </form>
    </div>
  )
}
