import { useState, useRef, useEffect, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import * as Select from '@radix-ui/react-select'
import * as Switch from '@radix-ui/react-switch'
import {
  ChevronDown,
  Check,
  AlertCircle,
  Upload,
  X,
  FileText,
  Image,
  File,
  IndianRupee,
  ChevronRight,
  Building2,
} from 'lucide-react'
import { expenseApi, ExpenseCategory } from '@/api/expense'
import { PageHeader } from '@/components/layout'
import { Card, Button, Input, Textarea, PageLoader } from '@/components/ui'
import { formatCurrency } from '@/utils/format'
import { cn } from '@/utils/cn'

interface FormValues {
  title: string
  categoryId: string
  expenseDate: string
  amount: string
  description: string
  // GST fields
  vendorName: string
  vendorGstin: string
  invoiceNumber: string
  invoiceDate: string
  isGstApplicable: boolean
  supplyType: 'intra_state' | 'inter_state'
  gstRate: string
  baseAmount: string
}

interface UploadedFile {
  file: File
  id?: string
  storagePath?: string
  uploading: boolean
  error?: string
}

export function SubmitExpensePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [serverError, setServerError] = useState<string | null>(null)
  const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([])

  const { data: categories, isLoading: categoriesLoading } = useQuery<ExpenseCategory[]>({
    queryKey: ['expense-categories'],
    queryFn: expenseApi.getCategories,
  })

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      title: '',
      categoryId: '',
      expenseDate: new Date().toISOString().split('T')[0],
      amount: '',
      description: '',
      // GST defaults
      vendorName: '',
      vendorGstin: '',
      invoiceNumber: '',
      invoiceDate: '',
      isGstApplicable: false,
      supplyType: 'intra_state',
      gstRate: '18',
      baseAmount: '',
    },
  })

  const selectedCategoryId = watch('categoryId')
  const amount = watch('amount')
  const isGstApplicable = watch('isGstApplicable')
  const supplyType = watch('supplyType')
  const gstRate = watch('gstRate')
  const baseAmount = watch('baseAmount')
  const selectedCategory = categories?.find((c) => c.id === selectedCategoryId)
  const [showGstSection, setShowGstSection] = useState(false)

  // Auto-fill GST defaults when category changes
  useEffect(() => {
    if (selectedCategory) {
      setValue('isGstApplicable', selectedCategory.isGstApplicable)
      setValue('gstRate', String(selectedCategory.defaultGstRate || 18))
      setShowGstSection(selectedCategory.isGstApplicable)
    }
  }, [selectedCategory, setValue])

  // Calculate GST amounts
  const gstCalculation = useMemo(() => {
    const base = parseFloat(baseAmount) || 0
    const rate = parseFloat(gstRate) || 0

    if (!isGstApplicable || base <= 0) {
      return { cgst: 0, sgst: 0, igst: 0, totalGst: 0, totalAmount: parseFloat(amount) || 0 }
    }

    if (supplyType === 'intra_state') {
      const halfRate = rate / 2
      const cgst = (base * halfRate) / 100
      const sgst = (base * halfRate) / 100
      return { cgst, sgst, igst: 0, totalGst: cgst + sgst, totalAmount: base + cgst + sgst }
    } else {
      const igst = (base * rate) / 100
      return { cgst: 0, sgst: 0, igst, totalGst: igst, totalAmount: base + igst }
    }
  }, [baseAmount, gstRate, isGstApplicable, supplyType, amount])

  // Sync total amount when GST is calculated
  useEffect(() => {
    if (isGstApplicable && baseAmount) {
      setValue('amount', String(gstCalculation.totalAmount.toFixed(2)))
    }
  }, [gstCalculation.totalAmount, isGstApplicable, baseAmount, setValue])

  const createMutation = useMutation({
    mutationFn: async (data: FormValues) => {
      // Create the expense claim with GST data
      const expense = await expenseApi.create({
        title: data.title,
        categoryId: data.categoryId,
        expenseDate: data.expenseDate,
        amount: parseFloat(data.amount),
        description: data.description || undefined,
        // GST fields
        vendorName: data.vendorName || undefined,
        vendorGstin: data.vendorGstin || undefined,
        invoiceNumber: data.invoiceNumber || undefined,
        invoiceDate: data.invoiceDate || undefined,
        isGstApplicable: data.isGstApplicable,
        supplyType: data.supplyType,
        gstRate: parseFloat(data.gstRate) || 0,
        baseAmount: parseFloat(data.baseAmount) || undefined,
        cgstAmount: gstCalculation.cgst,
        sgstAmount: gstCalculation.sgst,
        igstAmount: gstCalculation.igst,
      })

      // Add attachments
      for (const file of uploadedFiles) {
        if (file.id) {
          await expenseApi.addAttachment(expense.id, {
            fileStorageId: file.id,
            isPrimary: uploadedFiles.indexOf(file) === 0,
          })
        }
      }

      // Submit for approval
      return expenseApi.submit(expense.id)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-expenses'] })
      queryClient.invalidateQueries({ queryKey: ['portal-dashboard'] })
      navigate('/expenses')
    },
    onError: (error: Error) => {
      setServerError(error.message || 'Failed to submit expense claim')
    },
  })

  const handleFileSelect = async (files: FileList | null) => {
    if (!files) return

    for (let i = 0; i < files.length; i++) {
      const file = files[i]

      // Validate file type
      const allowedTypes = [
        'application/pdf',
        'image/png',
        'image/jpeg',
        'image/jpg',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
      ]
      if (!allowedTypes.includes(file.type)) {
        continue
      }

      // Validate file size (25 MB)
      if (file.size > 25 * 1024 * 1024) {
        continue
      }

      const uploadedFile: UploadedFile = { file, uploading: true }
      setUploadedFiles((prev) => [...prev, uploadedFile])

      try {
        const result = await expenseApi.uploadFile(file)
        setUploadedFiles((prev) =>
          prev.map((f) =>
            f.file === file
              ? { ...f, id: result.id, storagePath: result.storagePath, uploading: false }
              : f
          )
        )
      } catch (error) {
        setUploadedFiles((prev) =>
          prev.map((f) =>
            f.file === file ? { ...f, uploading: false, error: 'Upload failed' } : f
          )
        )
      }
    }
  }

  const removeFile = (file: File) => {
    setUploadedFiles((prev) => prev.filter((f) => f.file !== file))
  }

  const onSubmit = (data: FormValues) => {
    setServerError(null)
    createMutation.mutate(data)
  }

  const getFileIcon = (file: File) => {
    if (file.type.startsWith('image/')) return <Image size={20} className="text-blue-500" />
    if (file.type === 'application/pdf') return <FileText size={20} className="text-red-500" />
    return <File size={20} className="text-gray-500" />
  }

  if (categoriesLoading) {
    return <PageLoader />
  }

  const hasValidFiles = uploadedFiles.some((f) => f.id && !f.error)
  const isSubmitting = createMutation.isPending

  // Check if receipt is required
  const receiptRequired = selectedCategory?.requiresReceipt && !hasValidFiles

  return (
    <div className="animate-fade-in">
      <PageHeader title="Submit Expense" showBack />

      <form onSubmit={handleSubmit(onSubmit)} className="px-4 py-4 space-y-4">
        {serverError && (
          <Card className="p-3 bg-red-50 border-red-200">
            <div className="flex items-start gap-2">
              <AlertCircle className="text-red-500 flex-shrink-0 mt-0.5" size={18} />
              <p className="text-sm text-red-600">{serverError}</p>
            </div>
          </Card>
        )}

        {/* Title */}
        <Input
          label="Title"
          placeholder="e.g., Client meeting travel expenses"
          {...register('title', { required: 'Title is required' })}
          error={errors.title?.message}
        />

        {/* Category */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">Category</label>
          <Controller
            name="categoryId"
            control={control}
            rules={{ required: 'Please select a category' }}
            render={({ field }) => (
              <Select.Root value={field.value} onValueChange={field.onChange}>
                <Select.Trigger
                  className={cn(
                    'flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm',
                    'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                    !field.value && 'text-gray-400',
                    errors.categoryId && 'border-red-500'
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
                          key={cat.id}
                          value={cat.id}
                          className="relative flex items-center px-4 py-3 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
                        >
                          <Select.ItemText>
                            <div>
                              <span className="font-medium">{cat.name}</span>
                              {cat.maxAmount && (
                                <span className="text-gray-400 text-xs ml-2">
                                  (Max: {formatCurrency(cat.maxAmount)})
                                </span>
                              )}
                            </div>
                          </Select.ItemText>
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
          {errors.categoryId && (
            <p className="mt-1.5 text-sm text-red-600">{errors.categoryId.message}</p>
          )}
          {selectedCategory?.requiresReceipt && (
            <p className="mt-1 text-xs text-amber-600">This category requires a receipt</p>
          )}
        </div>

        {/* Date and Amount */}
        <div className="grid grid-cols-2 gap-3">
          <Input
            type="date"
            label="Expense Date"
            {...register('expenseDate', { required: 'Date is required' })}
            error={errors.expenseDate?.message}
          />
          <div>
            <label className="mb-1.5 block text-sm font-medium text-gray-700">Amount</label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
                <IndianRupee size={16} />
              </span>
              <input
                type="number"
                step="0.01"
                min="0"
                placeholder="0.00"
                className={cn(
                  'h-11 w-full rounded-xl border border-gray-300 bg-white pl-9 pr-4 py-2 text-sm',
                  'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                  errors.amount && 'border-red-500'
                )}
                {...register('amount', {
                  required: 'Amount is required',
                  min: { value: 0.01, message: 'Amount must be greater than 0' },
                  validate: (value) => {
                    if (selectedCategory?.maxAmount && parseFloat(value) > selectedCategory.maxAmount) {
                      return `Amount exceeds category limit of ${formatCurrency(selectedCategory.maxAmount)}`
                    }
                    return true
                  },
                })}
              />
            </div>
            {errors.amount && (
              <p className="mt-1.5 text-sm text-red-600">{errors.amount.message}</p>
            )}
          </div>
        </div>

        {/* GST Details Section */}
        <Card className="p-4 border-gray-200">
          <button
            type="button"
            onClick={() => setShowGstSection(!showGstSection)}
            className="flex items-center justify-between w-full"
          >
            <div className="flex items-center gap-2">
              <Building2 size={18} className="text-gray-500" />
              <span className="font-medium text-gray-700">GST Details</span>
              {isGstApplicable && (
                <span className="text-xs px-2 py-0.5 bg-green-100 text-green-700 rounded-full">
                  GST @ {gstRate}%
                </span>
              )}
            </div>
            <ChevronRight
              size={18}
              className={cn('text-gray-400 transition-transform', showGstSection && 'rotate-90')}
            />
          </button>

          {showGstSection && (
            <div className="mt-4 space-y-4 border-t pt-4">
              {/* GST Applicable Toggle */}
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-700">GST Applicable</label>
                  <p className="text-xs text-gray-500">Enable to claim Input Tax Credit</p>
                </div>
                <Controller
                  name="isGstApplicable"
                  control={control}
                  render={({ field }) => (
                    <Switch.Root
                      checked={field.value}
                      onCheckedChange={field.onChange}
                      className={cn(
                        'w-11 h-6 rounded-full relative transition-colors',
                        field.value ? 'bg-primary-600' : 'bg-gray-300'
                      )}
                    >
                      <Switch.Thumb className="block w-5 h-5 bg-white rounded-full shadow-md transition-transform data-[state=checked]:translate-x-5 translate-x-0.5" />
                    </Switch.Root>
                  )}
                />
              </div>

              {isGstApplicable && (
                <>
                  {/* Vendor Details */}
                  <div className="grid grid-cols-2 gap-3">
                    <Input
                      label="Vendor Name"
                      placeholder="Business name"
                      {...register('vendorName')}
                    />
                    <Input
                      label="Vendor GSTIN"
                      placeholder="15 characters"
                      maxLength={15}
                      {...register('vendorGstin', {
                        pattern: {
                          value: /^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/,
                          message: 'Invalid GSTIN format',
                        },
                      })}
                      error={errors.vendorGstin?.message}
                    />
                  </div>

                  {/* Invoice Details */}
                  <div className="grid grid-cols-2 gap-3">
                    <Input
                      label="Invoice Number"
                      placeholder="INV-001"
                      {...register('invoiceNumber')}
                    />
                    <Input
                      type="date"
                      label="Invoice Date"
                      {...register('invoiceDate')}
                    />
                  </div>

                  {/* Supply Type */}
                  <div>
                    <label className="mb-2 block text-sm font-medium text-gray-700">
                      Supply Type
                    </label>
                    <Controller
                      name="supplyType"
                      control={control}
                      render={({ field }) => (
                        <div className="flex gap-4">
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="radio"
                              checked={field.value === 'intra_state'}
                              onChange={() => field.onChange('intra_state')}
                              className="w-4 h-4 text-primary-600"
                            />
                            <span className="text-sm">Intra-State (CGST + SGST)</span>
                          </label>
                          <label className="flex items-center gap-2 cursor-pointer">
                            <input
                              type="radio"
                              checked={field.value === 'inter_state'}
                              onChange={() => field.onChange('inter_state')}
                              className="w-4 h-4 text-primary-600"
                            />
                            <span className="text-sm">Inter-State (IGST)</span>
                          </label>
                        </div>
                      )}
                    />
                  </div>

                  {/* Base Amount and GST Rate */}
                  <div className="grid grid-cols-2 gap-3">
                    <div>
                      <label className="mb-1.5 block text-sm font-medium text-gray-700">
                        Base Amount (Before GST)
                      </label>
                      <div className="relative">
                        <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
                          <IndianRupee size={16} />
                        </span>
                        <input
                          type="number"
                          step="0.01"
                          min="0"
                          placeholder="0.00"
                          className="h-11 w-full rounded-xl border border-gray-300 bg-white pl-9 pr-4 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20"
                          {...register('baseAmount')}
                        />
                      </div>
                    </div>
                    <div>
                      <label className="mb-1.5 block text-sm font-medium text-gray-700">
                        GST Rate
                      </label>
                      <Controller
                        name="gstRate"
                        control={control}
                        render={({ field }) => (
                          <Select.Root value={field.value} onValueChange={field.onChange}>
                            <Select.Trigger className="flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20">
                              <Select.Value />
                              <Select.Icon>
                                <ChevronDown size={16} className="text-gray-400" />
                              </Select.Icon>
                            </Select.Trigger>
                            <Select.Portal>
                              <Select.Content className="overflow-hidden bg-white rounded-xl shadow-lg border border-gray-200 z-50">
                                <Select.Viewport className="p-1">
                                  {['0', '5', '12', '18', '28'].map((rate) => (
                                    <Select.Item
                                      key={rate}
                                      value={rate}
                                      className="relative flex items-center px-4 py-2 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none"
                                    >
                                      <Select.ItemText>{rate}%</Select.ItemText>
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
                  </div>

                  {/* GST Calculation Summary */}
                  {baseAmount && parseFloat(baseAmount) > 0 && (
                    <Card className="p-3 bg-blue-50 border-blue-100">
                      <div className="space-y-1.5 text-sm">
                        <div className="flex justify-between text-gray-600">
                          <span>Base Amount</span>
                          <span>{formatCurrency(parseFloat(baseAmount))}</span>
                        </div>
                        {supplyType === 'intra_state' ? (
                          <>
                            <div className="flex justify-between text-gray-600">
                              <span>CGST ({parseFloat(gstRate) / 2}%)</span>
                              <span>{formatCurrency(gstCalculation.cgst)}</span>
                            </div>
                            <div className="flex justify-between text-gray-600">
                              <span>SGST ({parseFloat(gstRate) / 2}%)</span>
                              <span>{formatCurrency(gstCalculation.sgst)}</span>
                            </div>
                          </>
                        ) : (
                          <div className="flex justify-between text-gray-600">
                            <span>IGST ({gstRate}%)</span>
                            <span>{formatCurrency(gstCalculation.igst)}</span>
                          </div>
                        )}
                        <div className="flex justify-between font-semibold text-blue-800 pt-1 border-t border-blue-200">
                          <span>Total Amount</span>
                          <span>{formatCurrency(gstCalculation.totalAmount)}</span>
                        </div>
                      </div>
                    </Card>
                  )}

                  {/* ITC Eligibility Warning */}
                  {selectedCategory && !selectedCategory.itcEligible && (
                    <div className="flex items-start gap-2 p-3 bg-amber-50 rounded-lg border border-amber-200">
                      <AlertCircle className="text-amber-500 flex-shrink-0 mt-0.5" size={16} />
                      <p className="text-sm text-amber-700">
                        This category is not eligible for Input Tax Credit as per GST Act Section 17(5).
                      </p>
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </Card>

        {/* Amount Preview (shown when GST is not applicable) */}
        {!isGstApplicable && amount && parseFloat(amount) > 0 && (
          <Card className="p-4 bg-green-50 border-green-100">
            <div className="flex items-center justify-between">
              <span className="text-sm text-green-700">Total Amount</span>
              <span className="text-xl font-bold text-green-800">
                {formatCurrency(parseFloat(amount))}
              </span>
            </div>
          </Card>
        )}

        {/* Description */}
        <Textarea
          label="Description (Optional)"
          placeholder="Add details about this expense..."
          rows={3}
          {...register('description')}
        />

        {/* File Upload */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            Attachments {selectedCategory?.requiresReceipt ? '*' : '(Optional)'}
          </label>
          <div
            onClick={() => fileInputRef.current?.click()}
            className={cn(
              'border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors',
              'hover:border-primary-300 hover:bg-primary-50/50',
              receiptRequired ? 'border-amber-300 bg-amber-50' : 'border-gray-300'
            )}
          >
            <input
              ref={fileInputRef}
              type="file"
              multiple
              accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
              onChange={(e) => handleFileSelect(e.target.files)}
              className="hidden"
            />
            <Upload className="mx-auto h-8 w-8 text-gray-400" />
            <p className="mt-2 text-sm text-gray-600">
              <span className="font-medium text-primary-600">Click to upload</span> or drag and
              drop
            </p>
            <p className="mt-1 text-xs text-gray-500">PDF, PNG, JPG, DOC up to 25MB</p>
          </div>

          {/* Uploaded Files */}
          {uploadedFiles.length > 0 && (
            <div className="mt-3 space-y-2">
              {uploadedFiles.map((file, index) => (
                <div
                  key={index}
                  className={cn(
                    'flex items-center justify-between p-3 rounded-lg border',
                    file.error ? 'border-red-200 bg-red-50' : 'border-gray-200 bg-gray-50'
                  )}
                >
                  <div className="flex items-center gap-3">
                    {getFileIcon(file.file)}
                    <div>
                      <p className="text-sm font-medium text-gray-900 truncate max-w-[200px]">
                        {file.file.name}
                      </p>
                      <p className="text-xs text-gray-500">
                        {(file.file.size / 1024).toFixed(1)} KB
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    {file.uploading && (
                      <div className="w-4 h-4 border-2 border-primary-500 border-t-transparent rounded-full animate-spin" />
                    )}
                    {file.error && (
                      <span className="text-xs text-red-600">{file.error}</span>
                    )}
                    {file.id && !file.uploading && (
                      <Check className="w-4 h-4 text-green-500" />
                    )}
                    <button
                      type="button"
                      onClick={() => removeFile(file.file)}
                      className="p-1 text-gray-400 hover:text-red-500"
                    >
                      <X size={16} />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Submit Button */}
        <div className="pt-4">
          <Button
            type="submit"
            className="w-full"
            isLoading={isSubmitting}
            disabled={receiptRequired || uploadedFiles.some((f) => f.uploading)}
          >
            Submit Expense
          </Button>
          {receiptRequired && (
            <p className="mt-2 text-center text-sm text-amber-600">
              Please upload a receipt to continue
            </p>
          )}
        </div>
      </form>
    </div>
  )
}
