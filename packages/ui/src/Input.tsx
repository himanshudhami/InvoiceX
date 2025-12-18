import * as React from 'react'
import { cn } from './utils/cn'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  helperText?: string
  variant?: 'default' | 'glass' | 'floating'
  leftIcon?: React.ReactNode
  rightIcon?: React.ReactNode
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, label, error, helperText, id, variant = 'default', leftIcon, rightIcon, ...props }, ref) => {
    const inputId = id || label?.toLowerCase().replace(/\s+/g, '-')

    const variantClasses = {
      default: 'border-gray-300 bg-white focus:border-primary-500 focus:ring-primary-500/20',
      glass: 'border-white/30 bg-white/60 backdrop-blur-md focus:border-primary-400 focus:ring-primary-400/20 focus:bg-white/70',
      floating: 'border-gray-300 bg-white pt-6 pb-2 focus:border-primary-500 focus:ring-primary-500/20',
    }

    // For floating label variant
    if (variant === 'floating' && label) {
      return (
        <div className="relative w-full">
          <input
            type={type}
            id={inputId}
            placeholder=" "
            className={cn(
              'peer flex h-14 w-full rounded-xl border px-4 pt-6 pb-2 text-sm transition-all',
              'placeholder:text-transparent',
              'focus:outline-none focus:ring-2',
              'disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-500',
              variantClasses[variant],
              error && 'border-red-500 focus:border-red-500 focus:ring-red-500/20',
              className
            )}
            ref={ref}
            {...props}
          />
          <label
            htmlFor={inputId}
            className={cn(
              'absolute left-4 top-4 z-10 origin-[0] -translate-y-3 scale-75 transform text-sm text-gray-500 duration-200',
              'peer-placeholder-shown:translate-y-0 peer-placeholder-shown:scale-100',
              'peer-focus:-translate-y-3 peer-focus:scale-75 peer-focus:text-primary-600',
              error && 'text-red-500 peer-focus:text-red-500'
            )}
          >
            {label}
          </label>
          {error && <p className="mt-1.5 text-sm text-red-600">{error}</p>}
          {helperText && !error && <p className="mt-1.5 text-sm text-gray-500">{helperText}</p>}
        </div>
      )
    }

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={inputId}
            className="mb-1.5 block text-sm font-medium text-gray-700"
          >
            {label}
          </label>
        )}
        <div className="relative">
          {leftIcon && (
            <div className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">
              {leftIcon}
            </div>
          )}
          <input
            type={type}
            id={inputId}
            className={cn(
              'flex h-11 w-full rounded-xl border px-4 py-2 text-sm transition-all',
              'placeholder:text-gray-400',
              'focus:outline-none focus:ring-2',
              'disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-500',
              variantClasses[variant],
              leftIcon && 'pl-10',
              rightIcon && 'pr-10',
              error && 'border-red-500 focus:border-red-500 focus:ring-red-500/20',
              className
            )}
            ref={ref}
            {...props}
          />
          {rightIcon && (
            <div className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400">
              {rightIcon}
            </div>
          )}
        </div>
        {error && <p className="mt-1.5 text-sm text-red-600">{error}</p>}
        {helperText && !error && <p className="mt-1.5 text-sm text-gray-500">{helperText}</p>}
      </div>
    )
  }
)
Input.displayName = 'Input'

export interface TextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
  label?: string
  error?: string
  helperText?: string
}

const Textarea = React.forwardRef<HTMLTextAreaElement, TextareaProps>(
  ({ className, label, error, helperText, id, ...props }, ref) => {
    const textareaId = id || label?.toLowerCase().replace(/\s+/g, '-')

    return (
      <div className="w-full">
        {label && (
          <label
            htmlFor={textareaId}
            className="mb-1.5 block text-sm font-medium text-gray-700"
          >
            {label}
          </label>
        )}
        <textarea
          id={textareaId}
          className={cn(
            'flex min-h-[100px] w-full rounded-xl border border-gray-300 bg-white px-4 py-3 text-sm transition-colors',
            'placeholder:text-gray-400',
            'focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500/20',
            'disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-500',
            error && 'border-red-500 focus:border-red-500 focus:ring-red-500/20',
            className
          )}
          ref={ref}
          {...props}
        />
        {error && <p className="mt-1.5 text-sm text-red-600">{error}</p>}
        {helperText && !error && <p className="mt-1.5 text-sm text-gray-500">{helperText}</p>}
      </div>
    )
  }
)
Textarea.displayName = 'Textarea'

export { Input, Textarea }
