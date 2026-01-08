import { useState, useEffect, useCallback, useRef } from 'react'
import {
  Loader2,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Clock,
  Package,
  FileText,
  Users,
  Building2
} from 'lucide-react'
import {
  tallyMigrationApi,
  TallyMappingConfig,
  TallyImportResult,
  TallyImportProgress as ImportProgressType
} from '@/services/api/migration/tallyMigrationService'

interface TallyImportProgressProps {
  batchId: string
  mappingConfig?: TallyMappingConfig | null
  onComplete: (result: TallyImportResult) => void
}

type ImportStatus = 'starting' | 'importing' | 'completed' | 'failed'

const TallyImportProgress = ({ batchId, mappingConfig: _mappingConfig, onComplete }: TallyImportProgressProps) => {
  const [status, setStatus] = useState<ImportStatus>('starting')
  const [progress, setProgress] = useState<ImportProgressType | null>(null)
  const [error, setError] = useState<string | null>(null)
  const pollingRef = useRef<NodeJS.Timeout | null>(null)
  const startedRef = useRef(false)

  const startImport = useCallback(async () => {
    if (startedRef.current) return
    startedRef.current = true

    try {
      setStatus('importing')

      // Start the import
      const result = await tallyMigrationApi.startImport(batchId, {
        createJournalEntries: true,
        updateStockQuantities: true
      })

      // If we get an immediate result (sync import), we're done
      if (result.status === 'completed' || result.status === 'completed_with_errors') {
        setStatus('completed')
        onComplete(result)
        return
      }

      // Otherwise, start polling for progress
      startPolling()
    } catch (err) {
      setStatus('failed')
      setError(err instanceof Error ? err.message : 'Import failed to start')
    }
  }, [batchId, onComplete])

  const startPolling = useCallback(() => {
    const poll = async () => {
      try {
        const progressData = await tallyMigrationApi.getProgress(batchId)
        setProgress(progressData)

        if (progressData.status === 'completed' || progressData.status === 'completed_with_errors') {
          // Import finished, get final result
          const result = await tallyMigrationApi.getResult(batchId)
          setStatus('completed')
          onComplete(result)
          return
        }

        if (progressData.status === 'failed') {
          setStatus('failed')
          setError(progressData.lastError || 'Import failed')
          return
        }

        // Continue polling
        pollingRef.current = setTimeout(poll, 1000)
      } catch (err) {
        // Don't stop polling on errors, might be temporary
        pollingRef.current = setTimeout(poll, 2000)
      }
    }

    poll()
  }, [batchId, onComplete])

  useEffect(() => {
    startImport()

    return () => {
      if (pollingRef.current) {
        clearTimeout(pollingRef.current)
      }
    }
  }, [startImport])

  const formatTime = (seconds: number) => {
    if (seconds < 60) return `${Math.round(seconds)}s`
    const mins = Math.floor(seconds / 60)
    const secs = Math.round(seconds % 60)
    return `${mins}m ${secs}s`
  }

  const getStatusIcon = () => {
    switch (status) {
      case 'starting':
      case 'importing':
        return <Loader2 className="h-12 w-12 text-blue-500 animate-spin" />
      case 'completed':
        return <CheckCircle className="h-12 w-12 text-green-500" />
      case 'failed':
        return <XCircle className="h-12 w-12 text-red-500" />
    }
  }

  const getStatusText = () => {
    switch (status) {
      case 'starting':
        return 'Starting import...'
      case 'importing':
        return progress?.currentPhase || 'Importing...'
      case 'completed':
        return 'Import completed!'
      case 'failed':
        return 'Import failed'
    }
  }

  return (
    <div className="p-6 space-y-6">
      <div className="text-center">
        <div className="flex justify-center mb-4">
          {getStatusIcon()}
        </div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          {getStatusText()}
        </h2>
        {progress?.currentItem && status === 'importing' && (
          <p className="text-gray-500 dark:text-gray-400 mt-2">
            Processing: {progress.currentItem}
          </p>
        )}
      </div>

      {/* Progress Bar */}
      {status === 'importing' && progress && (
        <div className="space-y-2">
          <div className="flex justify-between text-sm text-gray-600 dark:text-gray-400">
            <span>{progress.currentPhase}</span>
            <span>{Math.round(progress.percentComplete)}%</span>
          </div>
          <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-3 overflow-hidden">
            <div
              className="bg-blue-600 h-full rounded-full transition-all duration-500"
              style={{ width: `${progress.percentComplete}%` }}
            />
          </div>
        </div>
      )}

      {/* Detailed Progress */}
      {progress && (
        <div className="grid grid-cols-2 gap-4">
          {/* Masters Progress */}
          <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
            <div className="flex items-center gap-2 mb-3">
              <Users className="h-5 w-5 text-gray-500" />
              <h3 className="font-medium text-gray-900 dark:text-white">Masters</h3>
            </div>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">Total</span>
                <span className="text-gray-900 dark:text-white">{progress.totalMasters}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">Processed</span>
                <span className="text-gray-900 dark:text-white">{progress.processedMasters}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-green-600 dark:text-green-400">Successful</span>
                <span className="text-green-600 dark:text-green-400">{progress.successfulMasters}</span>
              </div>
              {progress.failedMasters > 0 && (
                <div className="flex justify-between text-sm">
                  <span className="text-red-600 dark:text-red-400">Failed</span>
                  <span className="text-red-600 dark:text-red-400">{progress.failedMasters}</span>
                </div>
              )}
            </div>
          </div>

          {/* Vouchers Progress */}
          <div className="bg-gray-50 dark:bg-gray-900/50 rounded-lg p-4">
            <div className="flex items-center gap-2 mb-3">
              <FileText className="h-5 w-5 text-gray-500" />
              <h3 className="font-medium text-gray-900 dark:text-white">Vouchers</h3>
            </div>
            <div className="space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">Total</span>
                <span className="text-gray-900 dark:text-white">{progress.totalVouchers}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-500 dark:text-gray-400">Processed</span>
                <span className="text-gray-900 dark:text-white">{progress.processedVouchers}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-green-600 dark:text-green-400">Successful</span>
                <span className="text-green-600 dark:text-green-400">{progress.successfulVouchers}</span>
              </div>
              {progress.failedVouchers > 0 && (
                <div className="flex justify-between text-sm">
                  <span className="text-red-600 dark:text-red-400">Failed</span>
                  <span className="text-red-600 dark:text-red-400">{progress.failedVouchers}</span>
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Time Info */}
      {progress && status === 'importing' && (
        <div className="flex items-center justify-center gap-6 text-sm text-gray-500 dark:text-gray-400">
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4" />
            <span>Elapsed: {formatTime(progress.elapsedSeconds)}</span>
          </div>
          {progress.estimatedRemainingSeconds !== undefined && progress.estimatedRemainingSeconds > 0 && (
            <div className="flex items-center gap-2">
              <span>Remaining: ~{formatTime(progress.estimatedRemainingSeconds)}</span>
            </div>
          )}
        </div>
      )}

      {/* Error Display */}
      {error && (
        <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-red-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium text-red-800 dark:text-red-200">Import Error</p>
              <p className="text-sm text-red-700 dark:text-red-300 mt-1">{error}</p>
            </div>
          </div>
        </div>
      )}

      {/* Warning about not closing */}
      {status === 'importing' && (
        <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
          <div className="flex items-center gap-3">
            <AlertTriangle className="h-5 w-5 text-yellow-500" />
            <p className="text-sm text-yellow-700 dark:text-yellow-300">
              Please do not close this page while the import is in progress.
              The import will continue in the background if you leave.
            </p>
          </div>
        </div>
      )}

      {/* Phase Details */}
      {status === 'importing' && progress?.currentPhase && (
        <div className="text-center">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-blue-50 dark:bg-blue-900/20 rounded-full">
            {progress.currentPhase.includes('Masters') && <Building2 className="h-4 w-4 text-blue-500" />}
            {progress.currentPhase.includes('Voucher') && <FileText className="h-4 w-4 text-blue-500" />}
            {progress.currentPhase.includes('Stock') && <Package className="h-4 w-4 text-blue-500" />}
            <span className="text-sm text-blue-700 dark:text-blue-300">
              {progress.currentPhase}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}

export default TallyImportProgress
