import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  FolderOpen,
  FileText,
  Download,
  Shield,
  Award,
  Receipt,
  Briefcase,
  Plus,
  Loader2,
  AlertCircle,
  Check,
  Clock,
  X
} from 'lucide-react'
import { EmptyState, PageHeader } from '@/components/layout'
import { Badge, GlassCard, Button, BottomSheet, cn } from '@/components/ui'
import {
  documentsApi,
  documentTypeLabels,
  documentCategories,
  formatFileSize,
  type EmployeeDocument,
  type CreateDocumentRequestDto
} from '@/api/documents'

// Document category display config
const categoryConfig = [
  { id: 'employment', label: 'Employment', icon: Briefcase, color: 'from-blue-100 to-blue-50' },
  { id: 'tax', label: 'Tax', icon: Receipt, color: 'from-purple-100 to-purple-50' },
  { id: 'policies', label: 'Policies', icon: Shield, color: 'from-green-100 to-green-50' },
  { id: 'payroll', label: 'Payroll', icon: Award, color: 'from-yellow-100 to-yellow-50' },
]

const getDocumentIcon = (type: string) => {
  switch (type) {
    case 'offer_letter':
    case 'appointment_letter':
    case 'relieving_letter':
    case 'experience_certificate':
      return Briefcase
    case 'form16':
    case 'form12bb':
    case 'salary_certificate':
      return Receipt
    case 'policy':
    case 'handbook':
    case 'nda':
    case 'agreement':
      return Shield
    case 'payslip':
      return Award
    default:
      return FileText
  }
}

const requestStatusConfig: Record<string, { label: string; color: string; icon: React.ElementType }> = {
  pending: { label: 'Pending', color: 'bg-yellow-100 text-yellow-700', icon: Clock },
  processing: { label: 'Processing', color: 'bg-blue-100 text-blue-700', icon: Loader2 },
  completed: { label: 'Completed', color: 'bg-green-100 text-green-700', icon: Check },
  rejected: { label: 'Rejected', color: 'bg-red-100 text-red-700', icon: X },
}

// Requestable document types (what employees can request)
const requestableDocumentTypes = [
  { value: 'salary_certificate', label: 'Salary Certificate' },
  { value: 'experience_certificate', label: 'Experience Certificate' },
  { value: 'relieving_letter', label: 'Relieving Letter' },
  { value: 'form16', label: 'Form 16' },
]

export function DocumentsPage() {
  const queryClient = useQueryClient()
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null)
  const [showRequestSheet, setShowRequestSheet] = useState(false)
  const [showRequestsSheet, setShowRequestsSheet] = useState(false)
  const [requestForm, setRequestForm] = useState<CreateDocumentRequestDto>({
    documentType: '',
    purpose: ''
  })

  // Fetch documents
  const { data: documents = [], isLoading, error, refetch } = useQuery({
    queryKey: ['my-documents', selectedCategory],
    queryFn: () => documentsApi.getMyDocuments(selectedCategory || undefined),
  })

  // Fetch document requests
  const { data: requests = [], isLoading: requestsLoading } = useQuery({
    queryKey: ['my-document-requests'],
    queryFn: () => documentsApi.getMyRequests(),
  })

  // Create document request mutation
  const createRequestMutation = useMutation({
    mutationFn: (dto: CreateDocumentRequestDto) => documentsApi.createRequest(dto),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['my-document-requests'] })
      setShowRequestSheet(false)
      setRequestForm({ documentType: '', purpose: '' })
    },
  })

  // Count documents per category
  const getCategoryCount = (categoryId: string) => {
    const category = documentCategories.find(c => c.id === categoryId)
    if (!category) return 0
    return documents.filter(d => category.types.includes(d.documentType)).length
  }

  // Handle document download
  const handleDownload = async (e: React.MouseEvent, doc: EmployeeDocument) => {
    e.preventDefault()
    e.stopPropagation()

    try {
      const detail = await documentsApi.getById(doc.id)

      if (!detail || !detail.fileUrl) {
        alert('Document file is not available for download')
        return
      }

      const downloadUrl = documentsApi.getDownloadUrl(detail.fileUrl)
      window.open(downloadUrl, '_blank')
    } catch (err) {
      console.error('Failed to download document:', err)
      alert('Failed to download document. Please try again.')
    }
  }

  // Handle request submission
  const handleSubmitRequest = () => {
    if (!requestForm.documentType) return
    createRequestMutation.mutate(requestForm)
  }

  // Filter documents by category
  const filteredDocuments = selectedCategory
    ? documents.filter(d => {
        const category = documentCategories.find(c => c.id === selectedCategory)
        return category?.types.includes(d.documentType)
      })
    : documents

  // Count pending requests
  const pendingRequestsCount = requests.filter(r => r.status === 'pending' || r.status === 'processing').length

  if (error) {
    return (
      <div className="animate-fade-in pb-4">
        <PageHeader title="Documents" subtitle="Access your employment documents and company policies" />
        <GlassCard className="p-6 text-center">
          <AlertCircle className="mx-auto mb-3 text-red-500" size={32} />
          <p className="text-sm text-gray-600 mb-4">Failed to load documents</p>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Try Again
          </Button>
        </GlassCard>
      </div>
    )
  }

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <PageHeader
        title="Documents"
        subtitle="Access your employment documents and company policies"
        rightContent={
          pendingRequestsCount > 0 && (
            <button
              onClick={() => setShowRequestsSheet(true)}
              className="flex items-center gap-1.5 px-3 py-1.5 rounded-full bg-yellow-100 text-yellow-700 text-xs font-medium"
            >
              <Clock size={14} />
              {pendingRequestsCount} pending
            </button>
          )
        }
      />

      {/* Quick Categories */}
      <div className="grid grid-cols-2 gap-3 mb-6">
        {categoryConfig.map((category) => {
          const Icon = category.icon
          const count = getCategoryCount(category.id)
          const isSelected = selectedCategory === category.id

          return (
            <GlassCard
              key={category.id}
              className={cn(
                'p-4 cursor-pointer touch-feedback',
                isSelected && 'ring-2 ring-primary-500'
              )}
              hoverEffect
              onClick={() => setSelectedCategory(isSelected ? null : category.id)}
            >
              <div className="flex items-center gap-3">
                <div className={`flex items-center justify-center w-10 h-10 rounded-xl bg-gradient-to-br ${category.color}`}>
                  <Icon size={18} className="text-gray-700" />
                </div>
                <div>
                  <p className="text-sm font-semibold text-gray-900">{category.label}</p>
                  <p className="text-xs text-gray-500">{count} document{count !== 1 ? 's' : ''}</p>
                </div>
              </div>
            </GlassCard>
          )
        })}
      </div>

      {/* Request Document Button */}
      <Button
        variant="outline"
        className="w-full mb-6"
        onClick={() => setShowRequestSheet(true)}
      >
        <Plus size={16} className="mr-2" />
        Request New Document
      </Button>

      {/* Documents List */}
      <div className="mb-4 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-gray-900">
          {selectedCategory
            ? `${categoryConfig.find(c => c.id === selectedCategory)?.label} Documents`
            : 'All Documents'}
        </h2>
        {selectedCategory && (
          <button
            onClick={() => setSelectedCategory(null)}
            className="text-xs text-primary-600 font-medium"
          >
            Show All
          </button>
        )}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="animate-spin text-primary-500" size={24} />
        </div>
      ) : filteredDocuments.length === 0 ? (
        <EmptyState
          icon={<FolderOpen className="text-gray-400" size={24} />}
          title="No documents"
          description={selectedCategory
            ? 'No documents in this category'
            : 'Your documents will appear here once uploaded'}
        />
      ) : (
        <div className="space-y-3">
          {filteredDocuments.map((doc) => {
            const DocIcon = getDocumentIcon(doc.documentType)

            return (
              <GlassCard key={doc.id} className="p-4" hoverEffect>
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-gray-100 to-gray-50">
                    <DocIcon size={18} className="text-gray-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-semibold text-gray-900 truncate">{doc.title}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge variant="glass" size="sm">
                        {documentTypeLabels[doc.documentType] || doc.documentType}
                      </Badge>
                      <span className="text-[10px] text-gray-400">{formatFileSize(doc.fileSize)}</span>
                    </div>
                  </div>
                  <button
                    onClick={(e) => handleDownload(e, doc)}
                    className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-50 text-primary-600 hover:bg-primary-100 transition-colors"
                    title="Download"
                  >
                    <Download size={18} />
                  </button>
                </div>
                {doc.financialYear && (
                  <p className="text-[10px] text-gray-400 mt-2 ml-14">
                    Financial Year: {doc.financialYear}
                  </p>
                )}
                {doc.isCompanyWide && (
                  <Badge variant="glass-primary" size="sm" className="mt-2 ml-14">
                    Company Policy
                  </Badge>
                )}
              </GlassCard>
            )
          })}
        </div>
      )}

      {/* Request Document Bottom Sheet */}
      <BottomSheet
        isOpen={showRequestSheet}
        onClose={() => setShowRequestSheet(false)}
        title="Request Document"
      >
        <div className="space-y-4 p-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Document Type
            </label>
            <select
              value={requestForm.documentType}
              onChange={(e) => setRequestForm(prev => ({ ...prev, documentType: e.target.value }))}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 bg-white focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm"
            >
              <option value="">Select document type</option>
              {requestableDocumentTypes.map(type => (
                <option key={type.value} value={type.value}>{type.label}</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Purpose (Optional)
            </label>
            <textarea
              value={requestForm.purpose || ''}
              onChange={(e) => setRequestForm(prev => ({ ...prev, purpose: e.target.value }))}
              placeholder="Why do you need this document?"
              rows={3}
              className="w-full px-3 py-2.5 rounded-xl border border-gray-200 bg-white focus:ring-2 focus:ring-primary-500 focus:border-transparent text-sm resize-none"
            />
          </div>

          <Button
            className="w-full"
            onClick={handleSubmitRequest}
            disabled={!requestForm.documentType || createRequestMutation.isPending}
          >
            {createRequestMutation.isPending ? (
              <>
                <Loader2 className="animate-spin mr-2" size={16} />
                Submitting...
              </>
            ) : (
              'Submit Request'
            )}
          </Button>

          {createRequestMutation.isError && (
            <p className="text-sm text-red-600 text-center">
              Failed to submit request. Please try again.
            </p>
          )}
        </div>
      </BottomSheet>

      {/* Document Requests Bottom Sheet */}
      <BottomSheet
        isOpen={showRequestsSheet}
        onClose={() => setShowRequestsSheet(false)}
        title="My Document Requests"
      >
        <div className="p-4">
          {requestsLoading ? (
            <div className="flex items-center justify-center py-8">
              <Loader2 className="animate-spin text-primary-500" size={24} />
            </div>
          ) : requests.length === 0 ? (
            <EmptyState
              icon={<FileText className="text-gray-400" size={24} />}
              title="No requests"
              description="You haven't made any document requests yet"
            />
          ) : (
            <div className="space-y-3">
              {requests.map((request) => {
                const statusInfo = requestStatusConfig[request.status] || requestStatusConfig.pending
                const StatusIcon = statusInfo.icon

                return (
                  <GlassCard key={request.id} className="p-4">
                    <div className="flex items-start justify-between mb-2">
                      <p className="text-sm font-semibold text-gray-900">
                        {documentTypeLabels[request.documentType] || request.documentType}
                      </p>
                      <span className={cn(
                        'inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium',
                        statusInfo.color
                      )}>
                        <StatusIcon size={12} className={request.status === 'processing' ? 'animate-spin' : ''} />
                        {statusInfo.label}
                      </span>
                    </div>
                    {request.purpose && (
                      <p className="text-xs text-gray-500 mb-2">{request.purpose}</p>
                    )}
                    <p className="text-[10px] text-gray-400">
                      Requested on {new Date(request.createdAt).toLocaleDateString()}
                    </p>
                  </GlassCard>
                )
              })}
            </div>
          )}
        </div>
      </BottomSheet>
    </div>
  )
}
