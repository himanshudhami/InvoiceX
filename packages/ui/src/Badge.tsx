import * as React from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from './utils/cn'

const badgeVariants = cva(
  'inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-all',
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
        // New glassmorphism variants
        glass: 'bg-white/60 backdrop-blur-sm text-gray-700 border border-white/30 shadow-sm',
        'glass-primary': 'bg-primary-500/10 backdrop-blur-sm text-primary-700 border border-primary-200/50',
        'glass-success': 'bg-green-500/10 backdrop-blur-sm text-green-700 border border-green-200/50',
        'glass-warning': 'bg-yellow-500/10 backdrop-blur-sm text-yellow-700 border border-yellow-200/50',
        'glass-error': 'bg-red-500/10 backdrop-blur-sm text-red-700 border border-red-200/50',
        // Gradient variants
        gradient: 'bg-gradient-to-r from-primary-500 to-primary-600 text-white shadow-sm',
        'gradient-success': 'bg-gradient-to-r from-green-500 to-emerald-600 text-white shadow-sm',
        'gradient-warning': 'bg-gradient-to-r from-yellow-500 to-orange-500 text-white shadow-sm',
      },
      glow: {
        none: '',
        primary: 'shadow-[0_0_10px_rgba(99,102,241,0.3)]',
        success: 'shadow-[0_0_10px_rgba(34,197,94,0.3)]',
        warning: 'shadow-[0_0_10px_rgba(234,179,8,0.3)]',
        error: 'shadow-[0_0_10px_rgba(239,68,68,0.3)]',
      },
      size: {
        default: 'px-2.5 py-0.5 text-xs',
        sm: 'px-2 py-0.5 text-[10px]',
        lg: 'px-3 py-1 text-sm',
      },
    },
    defaultVariants: {
      variant: 'default',
      glow: 'none',
      size: 'default',
    },
  }
)

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>,
    VariantProps<typeof badgeVariants> {
  icon?: React.ReactNode
  dot?: boolean
}

export function Badge({ className, variant, glow, size, icon, dot, children, ...props }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant, glow, size }), className)} {...props}>
      {dot && (
        <span className="w-1.5 h-1.5 rounded-full bg-current opacity-70" />
      )}
      {icon && <span className="flex-shrink-0">{icon}</span>}
      {children}
    </span>
  )
}

export { badgeVariants }
