import React from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft } from 'lucide-react'
import { cn } from '@/utils/cn'

interface PageHeaderProps {
  title: string
  subtitle?: string
  showBack?: boolean
  rightContent?: React.ReactNode
  className?: string
}

export function PageHeader({
  title,
  subtitle,
  showBack = false,
  rightContent,
  className,
}: PageHeaderProps) {
  const navigate = useNavigate()

  return (
    <header
      className={cn(
        'sticky top-0 z-40 bg-white/80 backdrop-blur-md border-b border-gray-100 safe-top',
        className
      )}
    >
      <div className="flex items-center justify-between h-14 px-4 max-w-lg mx-auto">
        <div className="flex items-center gap-2 min-w-0 flex-1">
          {showBack && (
            <button
              onClick={() => navigate(-1)}
              className="flex items-center justify-center w-8 h-8 -ml-2 rounded-full hover:bg-gray-100 touch-feedback"
              aria-label="Go back"
            >
              <ChevronLeft size={24} className="text-gray-700" />
            </button>
          )}
          <div className="min-w-0">
            <h1 className="text-lg font-semibold text-gray-900 truncate">{title}</h1>
            {subtitle && (
              <p className="text-xs text-gray-500 truncate">{subtitle}</p>
            )}
          </div>
        </div>
        {rightContent && <div className="flex-shrink-0 ml-2">{rightContent}</div>}
      </div>
    </header>
  )
}
