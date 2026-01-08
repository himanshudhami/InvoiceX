import { useState, useCallback } from 'react'
import { FileSpreadsheet, AlertCircle, Loader2 } from 'lucide-react'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { tallyMigrationApi, TallyUploadResponse } from '@/services/api/migration/tallyMigrationService'

interface TallyFileUploadProps {
  onUploadComplete: (response: TallyUploadResponse) => void
}

const TallyFileUpload = ({ onUploadComplete }: TallyFileUploadProps) => {
  const { selectedCompany } = useCompanyContext()
  const [isUploading, setIsUploading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [importType, setImportType] = useState<'full' | 'incremental'>('full')
  const [dragOver, setDragOver] = useState(false)

  const handleFileUpload = useCallback(async (file: File) => {
    if (!selectedCompany?.id) {
      setError('Please select a company first')
      return
    }

    const extension = file.name.split('.').pop()?.toLowerCase()
    if (extension !== 'xml' && extension !== 'json') {
      setError('Only XML and JSON files are supported')
      return
    }

    setIsUploading(true)
    setError(null)

    try {
      const response = await tallyMigrationApi.upload(selectedCompany.id, file, importType)
      onUploadComplete(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed')
    } finally {
      setIsUploading(false)
    }
  }, [selectedCompany, importType, onUploadComplete])

  const handleInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      handleFileUpload(file)
    }
    e.target.value = ''
  }, [handleFileUpload])

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(false)
    const file = e.dataTransfer.files?.[0]
    if (file) {
      handleFileUpload(file)
    }
  }, [handleFileUpload])

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    setDragOver(true)
  }, [])

  const handleDragLeave = useCallback(() => {
    setDragOver(false)
  }, [])

  return (
    <div className="p-6 space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Upload Tally Export File
        </h2>
        <p className="text-gray-600 dark:text-gray-400 mt-1">
          Upload your Tally XML or JSON export file to begin the migration process
        </p>
      </div>

      {/* Import Type Selection */}
      <div className="space-y-3">
        <label className="block text-sm font-medium text-gray-700 dark:text-gray-300">
          Import Type
        </label>
        <div className="flex gap-4">
          <label className="flex items-center">
            <input
              type="radio"
              name="importType"
              value="full"
              checked={importType === 'full'}
              onChange={() => setImportType('full')}
              className="mr-2"
            />
            <span className="text-sm text-gray-700 dark:text-gray-300">
              Full Migration
            </span>
          </label>
          <label className="flex items-center">
            <input
              type="radio"
              name="importType"
              value="incremental"
              checked={importType === 'incremental'}
              onChange={() => setImportType('incremental')}
              className="mr-2"
            />
            <span className="text-sm text-gray-700 dark:text-gray-300">
              Incremental Sync
            </span>
          </label>
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-400">
          {importType === 'full'
            ? 'Import all data from Tally. Use for initial migration.'
            : 'Import only new/updated records since last sync.'
          }
        </p>
      </div>

      {/* File Upload Area */}
      <div
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        className={`
          border-2 border-dashed rounded-lg p-12 text-center transition-colors
          ${dragOver
            ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20'
            : 'border-gray-300 dark:border-gray-600 hover:border-blue-400'
          }
          ${isUploading ? 'opacity-50 pointer-events-none' : ''}
        `}
      >
        <input
          type="file"
          accept=".xml,.json"
          onChange={handleInputChange}
          className="hidden"
          id="tallyFileUpload"
          disabled={isUploading || !selectedCompany}
        />
        <label
          htmlFor="tallyFileUpload"
          className={`cursor-pointer ${!selectedCompany ? 'opacity-50 cursor-not-allowed' : ''}`}
        >
          {isUploading ? (
            <Loader2 className="h-12 w-12 text-blue-500 mx-auto mb-4 animate-spin" />
          ) : (
            <FileSpreadsheet className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          )}
          <p className="text-gray-600 dark:text-gray-400 mb-2">
            {isUploading
              ? 'Uploading and parsing file...'
              : selectedCompany
                ? 'Click to upload or drag and drop'
                : 'Please select a company first'
            }
          </p>
          <p className="text-sm text-gray-500">
            Supported formats: XML, JSON (Max 100MB)
          </p>
        </label>
      </div>

      {/* Error Display */}
      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-red-600 dark:text-red-400" />
            <p className="text-red-700 dark:text-red-300">{error}</p>
          </div>
        </div>
      )}

      {/* Help Text */}
      <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4 space-y-3">
        <h3 className="font-medium text-gray-900 dark:text-white">
          How to export data from Tally
        </h3>
        <ol className="list-decimal list-inside text-sm text-gray-600 dark:text-gray-400 space-y-2">
          <li>Open Tally and go to Gateway of Tally &gt; Export</li>
          <li>Select "Data" and choose XML or JSON format</li>
          <li>Select the data types to export (Masters, Vouchers, etc.)</li>
          <li>Choose the date range for vouchers</li>
          <li>Export and upload the file here</li>
        </ol>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-2">
          For TallyPrime users: Use Administration &gt; Export &gt; as XML (Data Interchange)
        </p>
      </div>
    </div>
  )
}

export default TallyFileUpload
