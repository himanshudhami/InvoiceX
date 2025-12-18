import * as React from 'react'
import { cn } from '@/utils/cn'

type GlassVariant = 'default' | 'strong' | 'light' | 'bordered'

interface GlassCardProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: GlassVariant
  gradient?: boolean
  glow?: 'primary' | 'success' | 'none'
  hoverEffect?: boolean
  children: React.ReactNode
}

const variantClasses: Record<GlassVariant, string> = {
  default: 'glass-card',
  strong: 'glass-card-strong',
  light: 'glass-card-light',
  bordered: 'glass-card border-2 border-primary-200/50',
}

const glowClasses = {
  primary: 'glow-primary',
  success: 'glow-success',
  none: '',
}

export function GlassCard({
  variant = 'default',
  gradient = false,
  glow = 'none',
  hoverEffect = false,
  children,
  className,
  ...props
}: GlassCardProps) {
  return (
    <div
      className={cn(
        variantClasses[variant],
        glowClasses[glow],
        gradient && 'bg-gradient-to-br from-white/80 to-white/40',
        hoverEffect && 'transition-all duration-300 hover:shadow-lg hover:scale-[1.01]',
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
}

// Sub-components for composition
interface GlassCardHeaderProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode
}

export function GlassCardHeader({ className, children, ...props }: GlassCardHeaderProps) {
  return (
    <div className={cn('p-4 border-b border-white/20', className)} {...props}>
      {children}
    </div>
  )
}

interface GlassCardContentProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode
}

export function GlassCardContent({ className, children, ...props }: GlassCardContentProps) {
  return (
    <div className={cn('p-4', className)} {...props}>
      {children}
    </div>
  )
}

interface GlassCardFooterProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode
}

export function GlassCardFooter({ className, children, ...props }: GlassCardFooterProps) {
  return (
    <div className={cn('p-4 border-t border-white/20', className)} {...props}>
      {children}
    </div>
  )
}
