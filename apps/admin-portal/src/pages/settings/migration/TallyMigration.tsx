import { useState, useCallback } from 'react'
import { Link } from 'react-router-dom'
import {
  ArrowLeft,
  Upload,
  Eye,
  Settings,
  FileText,
  Play,
  CheckCircle,
  ChevronRight
} from 'lucide-react'
import TallyFileUpload from '@/components/migration/TallyFileUpload'
import TallyMasterPreview from '@/components/migration/TallyMasterPreview'
import TallyMappingConfig from '@/components/migration/TallyMappingConfig'
import TallyVoucherPreview from '@/components/migration/TallyVoucherPreview'
import TallyImportProgress from '@/components/migration/TallyImportProgress'
import TallyImportSummary from '@/components/migration/TallyImportSummary'
import {
  TallyUploadResponse,
  TallyParsedData,
  TallyMappingConfig as MappingConfigType,
  TallyImportResult
} from '@/services/api/migration/tallyMigrationService'

type WizardStep = 'upload' | 'preview-masters' | 'mappings' | 'preview-vouchers' | 'import' | 'summary'

interface StepConfig {
  id: WizardStep
  label: string
  icon: React.ComponentType<{ className?: string }>
}

const STEPS: StepConfig[] = [
  { id: 'upload', label: 'Upload', icon: Upload },
  { id: 'preview-masters', label: 'Preview Masters', icon: Eye },
  { id: 'mappings', label: 'Configure', icon: Settings },
  { id: 'preview-vouchers', label: 'Preview Vouchers', icon: FileText },
  { id: 'import', label: 'Import', icon: Play },
  { id: 'summary', label: 'Summary', icon: CheckCircle }
]

const TallyMigration = () => {
  const [currentStep, setCurrentStep] = useState<WizardStep>('upload')
  const [uploadResponse, setUploadResponse] = useState<TallyUploadResponse | null>(null)
  const [parsedData, setParsedData] = useState<TallyParsedData | null>(null)
  const [mappingConfig, setMappingConfig] = useState<MappingConfigType | null>(null)
  const [importResult, setImportResult] = useState<TallyImportResult | null>(null)

  const currentStepIndex = STEPS.findIndex(s => s.id === currentStep)

  const handleUploadComplete = useCallback((response: TallyUploadResponse) => {
    setUploadResponse(response)
    if (response.parsedData) {
      setParsedData(response.parsedData)
    }
    setCurrentStep('preview-masters')
  }, [])

  const handleMasterPreviewNext = useCallback(() => {
    setCurrentStep('mappings')
  }, [])

  const handleMappingConfigured = useCallback((config: MappingConfigType) => {
    setMappingConfig(config)
    setCurrentStep('preview-vouchers')
  }, [])

  const handleVoucherPreviewNext = useCallback(() => {
    setCurrentStep('import')
  }, [])

  const handleImportComplete = useCallback((result: TallyImportResult) => {
    setImportResult(result)
    setCurrentStep('summary')
  }, [])

  const handleStartNewImport = useCallback(() => {
    setUploadResponse(null)
    setParsedData(null)
    setMappingConfig(null)
    setImportResult(null)
    setCurrentStep('upload')
  }, [])

  const handleGoBack = useCallback(() => {
    const index = currentStepIndex
    if (index > 0) {
      setCurrentStep(STEPS[index - 1].id)
    }
  }, [currentStepIndex])

  const canGoBack = currentStepIndex > 0 && currentStep !== 'import' && currentStep !== 'summary'

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link
          to="/settings"
          className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg dark:text-gray-400 dark:hover:text-gray-200 dark:hover:bg-gray-800"
        >
          <ArrowLeft className="h-5 w-5" />
        </Link>
        <div>
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Tally Migration</h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            Import data from Tally ERP to your modern accounting system
          </p>
        </div>
      </div>

      {/* Progress Steps */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4">
        <div className="flex items-center justify-between">
          {STEPS.map((step, index) => {
            const isCompleted = index < currentStepIndex
            const isCurrent = step.id === currentStep
            const StepIcon = step.icon

            return (
              <div key={step.id} className="flex items-center">
                <div className="flex items-center">
                  <div
                    className={`
                      flex items-center justify-center w-10 h-10 rounded-full border-2 transition-colors
                      ${isCompleted
                        ? 'bg-green-500 border-green-500 text-white'
                        : isCurrent
                          ? 'bg-blue-500 border-blue-500 text-white'
                          : 'border-gray-300 text-gray-400 dark:border-gray-600'
                      }
                    `}
                  >
                    {isCompleted ? (
                      <CheckCircle className="h-5 w-5" />
                    ) : (
                      <StepIcon className="h-5 w-5" />
                    )}
                  </div>
                  <span
                    className={`ml-2 text-sm font-medium hidden md:block ${
                      isCurrent || isCompleted
                        ? 'text-gray-900 dark:text-white'
                        : 'text-gray-400 dark:text-gray-500'
                    }`}
                  >
                    {step.label}
                  </span>
                </div>
                {index < STEPS.length - 1 && (
                  <ChevronRight className="h-5 w-5 mx-4 text-gray-300 dark:text-gray-600" />
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* Step Content */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow">
        {currentStep === 'upload' && (
          <TallyFileUpload onUploadComplete={handleUploadComplete} />
        )}

        {currentStep === 'preview-masters' && parsedData && (
          <TallyMasterPreview
            parsedData={parsedData}
            onNext={handleMasterPreviewNext}
            onBack={handleGoBack}
          />
        )}

        {currentStep === 'mappings' && parsedData && uploadResponse && (
          <TallyMappingConfig
            batchId={uploadResponse.batchId}
            parsedData={parsedData}
            onConfigured={handleMappingConfigured}
            onBack={handleGoBack}
          />
        )}

        {currentStep === 'preview-vouchers' && parsedData && (
          <TallyVoucherPreview
            parsedData={parsedData}
            onNext={handleVoucherPreviewNext}
            onBack={handleGoBack}
          />
        )}

        {currentStep === 'import' && uploadResponse && (
          <TallyImportProgress
            batchId={uploadResponse.batchId}
            mappingConfig={mappingConfig}
            onComplete={handleImportComplete}
          />
        )}

        {currentStep === 'summary' && importResult && (
          <TallyImportSummary
            result={importResult}
            onNewImport={handleStartNewImport}
          />
        )}
      </div>

      {/* Navigation Buttons */}
      {canGoBack && (
        <div className="flex justify-start">
          <button
            onClick={handleGoBack}
            className="inline-flex items-center px-4 py-2 text-gray-600 hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </button>
        </div>
      )}
    </div>
  )
}

export default TallyMigration
