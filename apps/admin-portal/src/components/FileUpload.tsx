import { useState, useCallback, useRef } from 'react'
import { Upload, X, File, Image, FileText, AlertCircle } from 'lucide-react'
import { cn } from '@/lib/utils'

const ALLOWED_TYPES = [
  'application/pdf',
  'image/png',
  'image/jpeg',
  'image/jpg',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
]

const MAX_FILE_SIZE = 25 * 1024 * 1024 // 25 MB

interface FileUploadProps {
  onFileSelect: (file: File) => void
  onFileRemove?: () => void
  selectedFile?: File | null
  accept?: string
  maxSize?: number
  disabled?: boolean
  className?: string
  label?: string
  hint?: string
  error?: string
}

export const FileUpload = ({
  onFileSelect,
  onFileRemove,
  selectedFile,
  accept = '.pdf,.png,.jpg,.jpeg,.doc,.docx',
  maxSize = MAX_FILE_SIZE,
  disabled = false,
  className,
  label = 'Upload File',
  hint = 'PDF, PNG, JPG, DOC, DOCX up to 25MB',
  error,
}: FileUploadProps) => {
  const [isDragOver, setIsDragOver] = useState(false)
  const [validationError, setValidationError] = useState<string | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const validateFile = (file: File): string | null => {
    if (!ALLOWED_TYPES.includes(file.type)) {
      return `File type "${file.type}" is not allowed. Please upload PDF, PNG, JPG, DOC, or DOCX files.`
    }
    if (file.size > maxSize) {
      const maxMB = maxSize / (1024 * 1024)
      return `File size exceeds ${maxMB}MB limit.`
    }
    return null
  }

  const handleFileChange = useCallback(
    (files: FileList | null) => {
      if (!files || files.length === 0) return

      const file = files[0]
      const error = validateFile(file)

      if (error) {
        setValidationError(error)
        return
      }

      setValidationError(null)
      onFileSelect(file)
    },
    [onFileSelect, maxSize]
  )

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    if (!disabled) {
      setIsDragOver(true)
    }
  }, [disabled])

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    setIsDragOver(false)
  }, [])

  const handleDrop = useCallback(
    (e: React.DragEvent) => {
      e.preventDefault()
      e.stopPropagation()
      setIsDragOver(false)

      if (disabled) return
      handleFileChange(e.dataTransfer.files)
    },
    [disabled, handleFileChange]
  )

  const handleClick = () => {
    if (!disabled && inputRef.current) {
      inputRef.current.click()
    }
  }

  const handleRemove = (e: React.MouseEvent) => {
    e.stopPropagation()
    setValidationError(null)
    onFileRemove?.()
    if (inputRef.current) {
      inputRef.current.value = ''
    }
  }

  const getFileIcon = (file: File) => {
    if (file.type.startsWith('image/')) {
      return <Image className="w-8 h-8 text-blue-500" />
    }
    if (file.type === 'application/pdf') {
      return <FileText className="w-8 h-8 text-red-500" />
    }
    return <File className="w-8 h-8 text-gray-500" />
  }

  const formatFileSize = (bytes: number): string => {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  }

  const displayError = error || validationError

  return (
    <div className={cn('space-y-2', className)}>
      {label && (
        <label className="block text-sm font-medium text-gray-700">{label}</label>
      )}

      <div
        onClick={handleClick}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        className={cn(
          'relative border-2 border-dashed rounded-lg p-6 transition-colors cursor-pointer',
          isDragOver && 'border-primary bg-primary/5',
          displayError && 'border-red-500 bg-red-50',
          disabled && 'opacity-50 cursor-not-allowed',
          !isDragOver && !displayError && 'border-gray-300 hover:border-gray-400 hover:bg-gray-50'
        )}
      >
        <input
          ref={inputRef}
          type="file"
          accept={accept}
          onChange={(e) => handleFileChange(e.target.files)}
          disabled={disabled}
          className="sr-only"
        />

        {selectedFile ? (
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              {getFileIcon(selectedFile)}
              <div>
                <p className="text-sm font-medium text-gray-900 truncate max-w-[200px]">
                  {selectedFile.name}
                </p>
                <p className="text-xs text-gray-500">
                  {formatFileSize(selectedFile.size)}
                </p>
              </div>
            </div>
            {onFileRemove && (
              <button
                type="button"
                onClick={handleRemove}
                className="p-1 text-gray-400 hover:text-red-500 transition-colors"
              >
                <X className="w-5 h-5" />
              </button>
            )}
          </div>
        ) : (
          <div className="text-center">
            <Upload className="mx-auto h-12 w-12 text-gray-400" />
            <div className="mt-4">
              <span className="text-sm font-medium text-primary">
                Click to upload
              </span>
              <span className="text-sm text-gray-500"> or drag and drop</span>
            </div>
            {hint && (
              <p className="mt-1 text-xs text-gray-500">{hint}</p>
            )}
          </div>
        )}
      </div>

      {displayError && (
        <div className="flex items-center space-x-1 text-red-600 text-sm">
          <AlertCircle className="w-4 h-4" />
          <span>{displayError}</span>
        </div>
      )}
    </div>
  )
}

export default FileUpload
