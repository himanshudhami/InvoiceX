import { useState, useRef } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useEmployeeDocuments, useCompanyWideDocuments, usePendingDocumentRequests, useCreateEmployeeDocument, useUpdateEmployeeDocument, useDeleteEmployeeDocument, useProcessDocumentRequest } from '@/hooks/api/useEmployeeDocuments'
import { useCompanies } from '@/hooks/api/useCompanies'
import { useEmployees } from '@/hooks/api/useEmployees'
import { EmployeeDocument, CreateEmployeeDocumentDto, UpdateEmployeeDocumentDto, DocumentRequest } from '@/services/api/employeeDocumentService'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { EmployeeSelect } from '@/components/ui/EmployeeSelect'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { Edit, Trash2, FileText, Download, Clock, CheckCircle, XCircle, Building, Upload, Loader2 } from 'lucide-react'
import { format } from 'date-fns'
import { fileService } from '@/services/api/fileService'

const DOCUMENT_TYPES = [
  { value: 'offer_letter', label: 'Offer Letter' },
  { value: 'appointment_letter', label: 'Appointment Letter' },
  { value: 'form16', label: 'Form 16' },
  { value: 'form12bb', label: 'Form 12BB' },
  { value: 'salary_certificate', label: 'Salary Certificate' },
  { value: 'experience_certificate', label: 'Experience Certificate' },
  { value: 'relieving_letter', label: 'Relieving Letter' },
  { value: 'policy', label: 'Policy' },
  { value: 'handbook', label: 'Handbook' },
  { value: 'nda', label: 'NDA' },
  { value: 'agreement', label: 'Agreement' },
  { value: 'other', label: 'Other' },
]

const EmployeeDocumentsManagement = () => {
  const [activeTab, setActiveTab] = useState<'documents' | 'requests'>('documents')
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingDocument, setEditingDocument] = useState<EmployeeDocument | null>(null)
  const [deletingDocument, setDeletingDocument] = useState<EmployeeDocument | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>('')
  const [processingRequest, setProcessingRequest] = useState<DocumentRequest | null>(null)

  const { data: documents = [], isLoading, error, refetch } = useEmployeeDocuments(selectedCompanyId || undefined, selectedEmployeeId || undefined)
  const { data: companyWideDocuments = [] } = useCompanyWideDocuments(selectedCompanyId, !!selectedCompanyId)
  const { data: pendingRequests = [], refetch: refetchRequests } = usePendingDocumentRequests(selectedCompanyId, !!selectedCompanyId)
  const { data: companies = [] } = useCompanies()
  const { data: employees = [] } = useEmployees(selectedCompanyId || undefined)
  const createDocument = useCreateEmployeeDocument()
  const updateDocument = useUpdateEmployeeDocument()
  const deleteDocument = useDeleteEmployeeDocument()
  const processRequest = useProcessDocumentRequest()

  const handleDeleteConfirm = async () => {
    if (deletingDocument) {
      try {
        await deleteDocument.mutateAsync(deletingDocument.id)
        setDeletingDocument(null)
      } catch (error) {
        console.error('Failed to delete document:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingDocument(null)
    refetch()
  }

  const handleProcessRequest = async (status: 'completed' | 'rejected', rejectionReason?: string) => {
    if (processingRequest) {
      try {
        await processRequest.mutateAsync({
          id: processingRequest.id,
          data: { status, rejectionReason }
        })
        setProcessingRequest(null)
        refetchRequests()
      } catch (error) {
        console.error('Failed to process request:', error)
      }
    }
  }

  const getDocumentTypeLabel = (type: string) => {
    return DOCUMENT_TYPES.find(t => t.value === type)?.label || type
  }

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return '-'
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
  }

  const documentColumns: ColumnDef<EmployeeDocument>[] = [
    {
      accessorKey: 'title',
      header: 'Document',
      cell: ({ row }) => {
        const doc = row.original
        return (
          <div className="flex items-start gap-2">
            <FileText size={16} className="text-blue-600 mt-0.5 flex-shrink-0" />
            <div>
              <div className="font-medium text-gray-900">{doc.title}</div>
              <div className="text-sm text-gray-500">{doc.fileName}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'documentType',
      header: 'Type',
      cell: ({ row }) => (
        <span className="capitalize text-sm">{getDocumentTypeLabel(row.original.documentType)}</span>
      ),
    },
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => {
        const doc = row.original
        return doc.isCompanyWide ? (
          <span className="inline-flex items-center gap-1 text-sm text-purple-700">
            <Building size={14} />
            Company Wide
          </span>
        ) : (
          <span className="text-sm">{doc.employeeName || '-'}</span>
        )
      },
    },
    {
      accessorKey: 'financialYear',
      header: 'FY',
      cell: ({ row }) => (
        <span className="text-sm text-gray-600">{row.original.financialYear || '-'}</span>
      ),
    },
    {
      accessorKey: 'fileSize',
      header: 'Size',
      cell: ({ row }) => (
        <span className="text-sm text-gray-600">{formatFileSize(row.original.fileSize)}</span>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Uploaded',
      cell: ({ row }) => format(new Date(row.original.createdAt), 'MMM d, yyyy'),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const doc = row.original
        return (
          <div className="flex space-x-2">
            <a
              href={doc.fileUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="Download"
            >
              <Download size={16} />
            </a>
            <button
              onClick={() => setEditingDocument(doc)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit document"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => setDeletingDocument(doc)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete document"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  const requestColumns: ColumnDef<DocumentRequest>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => (
        <span className="font-medium text-gray-900">{row.original.employeeName || 'Unknown'}</span>
      ),
    },
    {
      accessorKey: 'documentType',
      header: 'Document Type',
      cell: ({ row }) => (
        <span className="capitalize">{getDocumentTypeLabel(row.original.documentType)}</span>
      ),
    },
    {
      accessorKey: 'purpose',
      header: 'Purpose',
      cell: ({ row }) => (
        <span className="text-sm text-gray-600 line-clamp-2">{row.original.purpose || '-'}</span>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.original.status
        const colors: Record<string, string> = {
          pending: 'bg-yellow-100 text-yellow-800',
          processing: 'bg-blue-100 text-blue-800',
          completed: 'bg-green-100 text-green-800',
          rejected: 'bg-red-100 text-red-800',
        }
        return (
          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full capitalize ${colors[status] || 'bg-gray-100 text-gray-800'}`}>
            {status}
          </span>
        )
      },
    },
    {
      accessorKey: 'createdAt',
      header: 'Requested',
      cell: ({ row }) => format(new Date(row.original.createdAt), 'MMM d, yyyy'),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const request = row.original
        if (request.status !== 'pending') return null
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleProcessRequest('completed')}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="Approve"
            >
              <CheckCircle size={16} />
            </button>
            <button
              onClick={() => setProcessingRequest(request)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Reject"
            >
              <XCircle size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load documents</div>
        <button onClick={() => refetch()} className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90">
          Retry
        </button>
      </div>
    )
  }

  const allDocuments = [...documents, ...companyWideDocuments.filter(d => !documents.some(doc => doc.id === d.id))]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <FileText className="h-8 w-8 text-blue-600" />
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Employee Documents</h1>
          <p className="text-gray-600 mt-1">Manage employee documents and document requests</p>
        </div>
      </div>

      <div className="flex items-center gap-4 flex-wrap">
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">Company:</label>
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={(value) => {
              setSelectedCompanyId(value)
              setSelectedEmployeeId('') // Reset employee when company changes
            }}
            showAllOption
            allOptionLabel="All Companies"
            className="w-[200px]"
          />
        </div>
        <div className="flex items-center gap-2">
          <label className="text-sm font-medium text-gray-700">Employee:</label>
          <EmployeeSelect
            employees={employees}
            value={selectedEmployeeId}
            onChange={setSelectedEmployeeId}
            placeholder="All Employees"
            className="w-[200px]"
          />
        </div>
        {selectedEmployeeId && (
          <button
            onClick={() => setSelectedEmployeeId('')}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Clear filter
          </button>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Documents</div>
          <div className="text-2xl font-bold text-gray-900">{allDocuments.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Company Wide</div>
          <div className="text-2xl font-bold text-purple-600">{allDocuments.filter(d => d.isCompanyWide).length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Form 16s</div>
          <div className="text-2xl font-bold text-green-600">{allDocuments.filter(d => d.documentType === 'form16').length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Pending Requests</div>
          <div className="text-2xl font-bold text-orange-600">{pendingRequests.length}</div>
        </div>
      </div>

      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          <button
            onClick={() => setActiveTab('documents')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'documents'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <FileText className="inline-block w-4 h-4 mr-2" />
            Documents ({allDocuments.length})
          </button>
          <button
            onClick={() => setActiveTab('requests')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'requests'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <Clock className="inline-block w-4 h-4 mr-2" />
            Document Requests ({pendingRequests.length})
          </button>
        </nav>
      </div>

      {activeTab === 'documents' && (
        <div className="bg-white rounded-lg shadow">
          <div className="p-6">
            <DataTable
              columns={documentColumns}
              data={allDocuments}
              searchPlaceholder="Search documents..."
              onAdd={() => setIsCreateDrawerOpen(true)}
              addButtonText="Upload Document"
            />
          </div>
        </div>
      )}

      {activeTab === 'requests' && (
        <div className="bg-white rounded-lg shadow">
          <div className="p-6">
            {selectedCompanyId ? (
              <DataTable
                columns={requestColumns}
                data={pendingRequests}
                searchPlaceholder="Search requests..."
              />
            ) : (
              <div className="text-center py-12 text-gray-500">
                Please select a company to view document requests
              </div>
            )}
          </div>
        </div>
      )}

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Upload Document"
        size="lg"
      >
        <DocumentForm
          companies={companies}
          employees={employees}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
          createMutation={createDocument}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingDocument}
        onClose={() => setEditingDocument(null)}
        title="Edit Document"
        size="lg"
      >
        {editingDocument && (
          <DocumentForm
            document={editingDocument}
            companies={companies}
            employees={employees}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingDocument(null)}
            updateMutation={updateDocument}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingDocument}
        onClose={() => setDeletingDocument(null)}
        title="Delete Document"
        size="sm"
      >
        {deletingDocument && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingDocument.title}</strong>?
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingDocument(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteDocument.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteDocument.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        isOpen={!!processingRequest}
        onClose={() => setProcessingRequest(null)}
        title="Reject Request"
        size="sm"
      >
        {processingRequest && (
          <RejectRequestForm
            onConfirm={(reason) => handleProcessRequest('rejected', reason)}
            onCancel={() => setProcessingRequest(null)}
            isPending={processRequest.isPending}
          />
        )}
      </Modal>
    </div>
  )
}

interface DocumentFormProps {
  document?: EmployeeDocument
  companies: { id: string; name: string }[]
  employees: { id: string; firstName: string; lastName: string; companyId: string }[]
  onSuccess: () => void
  onCancel: () => void
  createMutation?: ReturnType<typeof useCreateEmployeeDocument>
  updateMutation?: ReturnType<typeof useUpdateEmployeeDocument>
}

const DocumentForm = ({ document, companies, employees, onSuccess, onCancel, createMutation, updateMutation }: DocumentFormProps) => {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [formData, setFormData] = useState<CreateEmployeeDocumentDto>({
    employeeId: document?.employeeId || '',
    companyId: document?.companyId || companies[0]?.id || '',
    documentType: document?.documentType || 'other',
    title: document?.title || '',
    description: document?.description || '',
    fileUrl: document?.fileUrl || '',
    fileName: document?.fileName || '',
    fileSize: document?.fileSize,
    mimeType: document?.mimeType || '',
    financialYear: document?.financialYear || '',
    isCompanyWide: document?.isCompanyWide || false,
  })

  const filteredEmployees = employees.filter(e => e.companyId === formData.companyId)

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return

    // Validate file size (25MB max)
    if (file.size > 25 * 1024 * 1024) {
      setUploadError('File size must be less than 25MB')
      return
    }

    // Validate file type
    const allowedTypes = ['application/pdf', 'image/png', 'image/jpeg', 'image/jpg',
                          'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document']
    if (!allowedTypes.includes(file.type)) {
      setUploadError('Only PDF, PNG, JPG, DOC, and DOCX files are allowed')
      return
    }

    setUploadError(null)
    setIsUploading(true)

    try {
      const response = await fileService.upload(
        file,
        formData.companyId,
        'employee_document'
      )

      setFormData(prev => ({
        ...prev,
        fileUrl: response.storagePath,
        fileName: response.originalFilename,
        mimeType: response.mimeType,
        fileSize: response.fileSize,
        title: prev.title || response.originalFilename.replace(/\.[^/.]+$/, ''), // Set title from filename if empty
      }))
    } catch (error: any) {
      console.error('Upload failed:', error)
      setUploadError(error.message || 'Failed to upload file')
    } finally {
      setIsUploading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    if (!formData.fileUrl) {
      setUploadError('Please upload a file first')
      return
    }

    try {
      if (document && updateMutation) {
        const updateData: UpdateEmployeeDocumentDto = {
          documentType: formData.documentType,
          title: formData.title,
          description: formData.description,
          fileUrl: formData.fileUrl,
          fileName: formData.fileName,
          fileSize: formData.fileSize,
          mimeType: formData.mimeType,
          financialYear: formData.financialYear,
          isCompanyWide: formData.isCompanyWide,
        }
        await updateMutation.mutateAsync({ id: document.id, data: updateData })
      } else if (createMutation) {
        await createMutation.mutateAsync(formData)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save document:', error)
    }
  }

  const isPending = createMutation?.isPending || updateMutation?.isPending

  const formatFileSize = (bytes?: number) => {
    if (!bytes) return ''
    if (bytes < 1024) return bytes + ' B'
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {!document && (
        <>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
            <CompanySelect
              companies={companies}
              value={formData.companyId}
              onChange={(value) => setFormData({ ...formData, companyId: value, employeeId: '' })}
              placeholder="Select company..."
            />
          </div>

          <label className="flex items-center">
            <input
              type="checkbox"
              checked={formData.isCompanyWide}
              onChange={(e) => setFormData({ ...formData, isCompanyWide: e.target.checked, employeeId: '' })}
              className="mr-2"
            />
            <span className="text-sm text-gray-700">Company-wide document (visible to all employees)</span>
          </label>

          {!formData.isCompanyWide && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Employee *</label>
              <EmployeeSelect
                employees={filteredEmployees}
                value={formData.employeeId}
                onChange={(value) => setFormData({ ...formData, employeeId: value })}
                placeholder="Search and select employee..."
              />
            </div>
          )}
        </>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Document Type *</label>
        <select
          value={formData.documentType}
          onChange={(e) => setFormData({ ...formData, documentType: e.target.value })}
          className="w-full px-3 py-2 text-sm border border-gray-300 rounded-md bg-white text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500"
          required
        >
          {DOCUMENT_TYPES.map(type => (
            <option key={type.value} value={type.value}>{type.label}</option>
          ))}
        </select>
      </div>

      {/* File Upload */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">
          {document ? 'Replace File' : 'Upload File *'}
        </label>
        <input
          ref={fileInputRef}
          type="file"
          onChange={handleFileChange}
          accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
          className="hidden"
        />
        <div
          onClick={() => fileInputRef.current?.click()}
          className={`border-2 border-dashed rounded-lg p-6 text-center cursor-pointer transition-colors
            ${formData.fileUrl ? 'border-green-300 bg-green-50' : 'border-gray-300 hover:border-blue-400 hover:bg-blue-50'}`}
        >
          {isUploading ? (
            <div className="flex items-center justify-center gap-2 text-blue-600">
              <Loader2 className="w-5 h-5 animate-spin" />
              <span>Uploading...</span>
            </div>
          ) : formData.fileUrl ? (
            <div className="flex items-center justify-center gap-2 text-green-600">
              <FileText className="w-5 h-5" />
              <div>
                <p className="font-medium">{formData.fileName}</p>
                <p className="text-sm text-gray-500">{formatFileSize(formData.fileSize)} - Click to replace</p>
              </div>
            </div>
          ) : (
            <div className="flex flex-col items-center gap-2 text-gray-500">
              <Upload className="w-8 h-8" />
              <p>Click to upload or drag and drop</p>
              <p className="text-xs">PDF, PNG, JPG, DOC, DOCX (max 25MB)</p>
            </div>
          )}
        </div>
        {uploadError && (
          <p className="mt-1 text-sm text-red-600">{uploadError}</p>
        )}
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Title *</label>
        <input
          type="text"
          value={formData.title}
          onChange={(e) => setFormData({ ...formData, title: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          required
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
        <textarea
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          rows={3}
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Financial Year</label>
        <input
          type="text"
          value={formData.financialYear}
          onChange={(e) => setFormData({ ...formData, financialYear: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          placeholder="2024-25"
        />
      </div>

      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isPending || isUploading || (!document && !formData.fileUrl)}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? 'Saving...' : document ? 'Update' : 'Upload Document'}
        </button>
      </div>
    </form>
  )
}

interface RejectRequestFormProps {
  onConfirm: (reason: string) => void
  onCancel: () => void
  isPending: boolean
}

const RejectRequestForm = ({ onConfirm, onCancel, isPending }: RejectRequestFormProps) => {
  const [reason, setReason] = useState('')

  return (
    <div className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Rejection Reason</label>
        <textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          rows={3}
          placeholder="Please provide a reason for rejection..."
        />
      </div>
      <div className="flex justify-end space-x-3">
        <button
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          onClick={() => onConfirm(reason)}
          disabled={isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
        >
          {isPending ? 'Rejecting...' : 'Reject Request'}
        </button>
      </div>
    </div>
  )
}

export default EmployeeDocumentsManagement
