import { useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import * as Select from '@radix-ui/react-select'
import { ChevronDown, Check, AlertCircle } from 'lucide-react'
import { leaveApi } from '@/api'
import { PageHeader } from '@/components/layout'
import { Card, Button, Input, Textarea, PageLoader } from '@/components/ui'
import { formatDate, formatDays } from '@/utils/format'
import { cn } from '@/utils/cn'
import type { ApplyLeaveRequest, LeaveType, LeaveCalculation } from '@/types'

interface FormValues {
  leaveTypeId: string
  fromDate: string
  toDate: string
  isHalfDayStart: boolean
  isHalfDayEnd: boolean
  reason: string
}

export function ApplyLeavePage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [serverError, setServerError] = useState<string | null>(null)

  const { data: leaveTypes, isLoading: typesLoading } = useQuery<LeaveType[]>({
    queryKey: ['leave-types'],
    queryFn: leaveApi.getLeaveTypes,
  })

  const {
    register,
    control,
    handleSubmit,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      leaveTypeId: '',
      fromDate: '',
      toDate: '',
      isHalfDayStart: false,
      isHalfDayEnd: false,
      reason: '',
    },
  })

  const fromDate = watch('fromDate')
  const toDate = watch('toDate')

  // Calculate leave days when dates change
  const { data: calculation } = useQuery<LeaveCalculation>({
    queryKey: ['leave-calculation', fromDate, toDate],
    queryFn: () => leaveApi.calculateLeaveDays(fromDate, toDate),
    enabled: !!fromDate && !!toDate && fromDate <= toDate,
  })

  const applyMutation = useMutation({
    mutationFn: (data: ApplyLeaveRequest) => leaveApi.applyLeave(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['leave-dashboard'] })
      queryClient.invalidateQueries({ queryKey: ['leave-applications'] })
      queryClient.invalidateQueries({ queryKey: ['portal-dashboard'] })
      navigate('/leave')
    },
    onError: (error: Error) => {
      setServerError(error.message || 'Failed to apply for leave')
    },
  })

  const onSubmit = (data: FormValues) => {
    setServerError(null)
    applyMutation.mutate({
      leaveTypeId: data.leaveTypeId,
      fromDate: data.fromDate,
      toDate: data.toDate,
      isHalfDayStart: data.isHalfDayStart,
      isHalfDayEnd: data.isHalfDayEnd,
      reason: data.reason || undefined,
    })
  }

  const selectedLeaveType = useMemo(() => {
    const leaveTypeId = watch('leaveTypeId')
    return leaveTypes?.find((t) => t.id === leaveTypeId)
  }, [leaveTypes, watch('leaveTypeId')])

  if (typesLoading) {
    return <PageLoader />
  }

  return (
    <div className="animate-fade-in">
      <PageHeader title="Apply for Leave" showBack />

      <form onSubmit={handleSubmit(onSubmit)} className="px-4 py-4 space-y-4">
        {serverError && (
          <Card className="p-3 bg-red-50 border-red-200">
            <div className="flex items-start gap-2">
              <AlertCircle className="text-red-500 flex-shrink-0 mt-0.5" size={18} />
              <p className="text-sm text-red-600">{serverError}</p>
            </div>
          </Card>
        )}

        {/* Leave Type */}
        <div>
          <label className="mb-1.5 block text-sm font-medium text-gray-700">
            Leave Type
          </label>
          <Controller
            name="leaveTypeId"
            control={control}
            rules={{ required: 'Please select a leave type' }}
            render={({ field }) => (
              <Select.Root value={field.value} onValueChange={field.onChange}>
                <Select.Trigger
                  className={cn(
                    'flex h-11 w-full items-center justify-between rounded-xl border border-gray-300 bg-white px-4 py-2 text-sm',
                    'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
                    !field.value && 'text-gray-400',
                    errors.leaveTypeId && 'border-red-500'
                  )}
                >
                  <Select.Value placeholder="Select leave type" />
                  <Select.Icon>
                    <ChevronDown size={16} className="text-gray-400" />
                  </Select.Icon>
                </Select.Trigger>
                <Select.Portal>
                  <Select.Content className="overflow-hidden bg-white rounded-xl shadow-lg border border-gray-200 z-50">
                    <Select.Viewport className="p-1">
                      {leaveTypes?.map((type) => (
                        <Select.Item
                          key={type.id}
                          value={type.id}
                          className="relative flex items-center px-4 py-3 text-sm rounded-lg cursor-pointer select-none hover:bg-gray-50 focus:bg-gray-50 focus:outline-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50"
                        >
                          <Select.ItemText>
                            <span className="font-medium">{type.name}</span>
                            <span className="text-gray-400 ml-2">({type.code})</span>
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
          {errors.leaveTypeId && (
            <p className="mt-1.5 text-sm text-red-600">{errors.leaveTypeId.message}</p>
          )}
        </div>

        {/* Date Selection */}
        <div className="grid grid-cols-2 gap-3">
          <Input
            type="date"
            label="From Date"
            {...register('fromDate', { required: 'From date is required' })}
            error={errors.fromDate?.message}
          />
          <Input
            type="date"
            label="To Date"
            {...register('toDate', {
              required: 'To date is required',
              validate: (value) =>
                !fromDate || value >= fromDate || 'To date must be after from date',
            })}
            error={errors.toDate?.message}
          />
        </div>

        {/* Leave Calculation */}
        {calculation && (
          <Card className="p-4 bg-primary-50 border-primary-100">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-primary-900">Leave Duration</p>
                <p className="text-xs text-primary-600 mt-0.5">
                  {formatDate(calculation.fromDate)} - {formatDate(calculation.toDate)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-2xl font-bold text-primary-700">
                  {formatDays(calculation.workingDays)}
                </p>
                {calculation.weekendDays > 0 && (
                  <p className="text-[10px] text-primary-500">
                    {calculation.weekendDays} weekend(s) excluded
                  </p>
                )}
              </div>
            </div>
            {calculation.holidays.length > 0 && (
              <div className="mt-3 pt-3 border-t border-primary-200">
                <p className="text-xs text-primary-600">
                  Holidays during this period: {calculation.holidays.map((h) => h.name).join(', ')}
                </p>
              </div>
            )}
          </Card>
        )}

        {/* Half Day Options */}
        <Card className="p-4">
          <p className="text-sm font-medium text-gray-700 mb-3">Half Day Options</p>
          <div className="space-y-3">
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                {...register('isHalfDayStart')}
                className="h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
              />
              <span className="text-sm text-gray-700">Half day on start date</span>
            </label>
            <label className="flex items-center gap-3 cursor-pointer">
              <input
                type="checkbox"
                {...register('isHalfDayEnd')}
                className="h-5 w-5 rounded border-gray-300 text-primary-600 focus:ring-primary-500"
              />
              <span className="text-sm text-gray-700">Half day on end date</span>
            </label>
          </div>
        </Card>

        {/* Reason */}
        <Textarea
          label="Reason (Optional)"
          placeholder="Enter reason for leave..."
          rows={3}
          {...register('reason')}
        />

        {/* Submit Button */}
        <div className="pt-2">
          <Button
            type="submit"
            className="w-full"
            isLoading={applyMutation.isPending}
            disabled={!selectedLeaveType || !fromDate || !toDate}
          >
            Submit Application
          </Button>
        </div>
      </form>
    </div>
  )
}
