import React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/utils/cn'

const badgeVariants = cva(
  'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
  {
    variants: {
      variant: {
        default: 'bg-gray-100 text-gray-800',
        pending: 'bg-yellow-100 text-yellow-800',
        approved: 'bg-green-100 text-green-800',
        rejected: 'bg-red-100 text-red-800',
        cancelled: 'bg-gray-100 text-gray-600',
        withdrawn: 'bg-gray-100 text-gray-600',
        info: 'bg-blue-100 text-blue-800',
        warning: 'bg-orange-100 text-orange-800',
        success: 'bg-green-100 text-green-800',
        error: 'bg-red-100 text-red-800',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {}

export function Badge({ className, variant, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />
}

// Helper to get badge variant from status string
export function getStatusBadgeVariant(
  status: string
): 'pending' | 'approved' | 'rejected' | 'cancelled' | 'withdrawn' | 'default' {
  const statusLower = status.toLowerCase()
  if (statusLower === 'pending' || statusLower === 'draft' || statusLower === 'submitted') {
    return 'pending'
  }
  if (statusLower === 'approved' || statusLower === 'verified' || statusLower === 'active') {
    return 'approved'
  }
  if (statusLower === 'rejected') {
    return 'rejected'
  }
  if (statusLower === 'cancelled') {
    return 'cancelled'
  }
  if (statusLower === 'withdrawn') {
    return 'withdrawn'
  }
  return 'default'
}
