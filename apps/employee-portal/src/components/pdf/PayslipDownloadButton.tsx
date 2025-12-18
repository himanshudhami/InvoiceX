import { useState, useCallback } from 'react'
import { Download, Loader2 } from 'lucide-react'
import { cn } from '@repo/ui'
import type { PayslipDetail } from '@/types'

// Lazy load PDF dependencies to reduce initial bundle size
const loadPdfDeps = async () => {
  const [{ pdf }, { saveAs }, { PayslipPDFDocument }] = await Promise.all([
    import('@react-pdf/renderer'),
    import('file-saver'),
    import('./PayslipPDF'),
  ])
  return { pdf, saveAs, PayslipPDFDocument }
}

const MONTHS = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December'
]

interface PayslipDownloadButtonProps {
  payslip: PayslipDetail
  companyName?: string
  fileName?: string
  className?: string
  variant?: 'icon' | 'button' | 'text'
  size?: 'sm' | 'md' | 'lg'
}

export function PayslipDownloadButton({
  payslip,
  companyName,
  fileName,
  className,
  variant = 'icon',
  size = 'md',
}: PayslipDownloadButtonProps) {
  const [isGenerating, setIsGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const monthName = MONTHS[payslip.month - 1] || ''
  const defaultFileName = `Salary-Slip-${payslip.employeeName.replace(/\s+/g, '-')}-${monthName}-${payslip.year}.pdf`

  const handleDownload = useCallback(async () => {
    if (isGenerating) return

    try {
      setIsGenerating(true)
      setError(null)

      // Lazy load PDF dependencies
      const { pdf, saveAs, PayslipPDFDocument } = await loadPdfDeps()

      const blob = await pdf(
        <PayslipPDFDocument payslip={payslip} companyName={companyName} />
      ).toBlob()

      saveAs(blob, fileName || defaultFileName)
    } catch (err) {
      console.error('Failed to generate Salary Slip PDF', err)
      setError('Failed to generate PDF. Please try again.')
    } finally {
      setIsGenerating(false)
    }
  }, [payslip, companyName, fileName, defaultFileName, isGenerating])

  const sizeClasses = {
    sm: 'w-8 h-8',
    md: 'w-10 h-10',
    lg: 'w-12 h-12',
  }

  const iconSizes = {
    sm: 14,
    md: 18,
    lg: 22,
  }

  if (variant === 'icon') {
    return (
      <div className="relative">
        <button
          onClick={handleDownload}
          disabled={isGenerating}
          className={cn(
            'flex items-center justify-center rounded-full bg-primary-50 text-primary-600 transition-all',
            'hover:bg-primary-100 active:scale-95 disabled:opacity-50 disabled:cursor-wait',
            sizeClasses[size],
            className
          )}
          aria-label="Download PDF"
          title="Download Salary Slip"
        >
          {isGenerating ? (
            <Loader2 size={iconSizes[size]} className="animate-spin" />
          ) : (
            <Download size={iconSizes[size]} />
          )}
        </button>
        {error && (
          <div className="absolute right-0 top-12 z-10 px-3 py-2 text-xs font-medium text-white bg-red-600 rounded-lg shadow-lg whitespace-nowrap animate-fade-in">
            {error}
            <div className="absolute -top-1 right-4 w-2 h-2 bg-red-600 rotate-45" />
          </div>
        )}
      </div>
    )
  }

  if (variant === 'button') {
    return (
      <button
        onClick={handleDownload}
        disabled={isGenerating}
        className={cn(
          'inline-flex items-center gap-2 px-4 py-2 rounded-xl font-medium text-sm transition-all',
          'bg-primary-600 text-white hover:bg-primary-700 active:bg-primary-800',
          'disabled:opacity-50 disabled:cursor-wait',
          className
        )}
      >
        {isGenerating ? (
          <>
            <Loader2 size={16} className="animate-spin" />
            Generating...
          </>
        ) : (
          <>
            <Download size={16} />
            Download PDF
          </>
        )}
      </button>
    )
  }

  // Text variant
  return (
    <button
      onClick={handleDownload}
      disabled={isGenerating}
      className={cn(
        'inline-flex items-center gap-1.5 text-sm font-medium text-primary-600 hover:text-primary-700 transition-colors',
        'disabled:opacity-50 disabled:cursor-wait',
        className
      )}
    >
      {isGenerating ? (
        <>
          <Loader2 size={14} className="animate-spin" />
          Generating...
        </>
      ) : (
        <>
          <Download size={14} />
          Download
        </>
      )}
    </button>
  )
}
