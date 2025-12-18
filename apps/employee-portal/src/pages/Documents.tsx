import {
  FolderOpen,
  FileText,
  Download,
  Shield,
  Award,
  Receipt,
  Briefcase,
  Sparkles
} from 'lucide-react'
import { EmptyState } from '@/components/layout'
import { Badge, GlassCard, Button } from '@/components/ui'

// Mock documents data
const mockDocuments = [
  {
    id: '1',
    title: 'Offer Letter',
    type: 'offer_letter',
    category: 'Employment',
    uploadedAt: '2024-01-15T10:00:00Z',
    fileSize: '245 KB',
  },
  {
    id: '2',
    title: 'Form 16 - FY 2023-24',
    type: 'form16',
    category: 'Tax',
    uploadedAt: '2024-06-15T09:30:00Z',
    fileSize: '1.2 MB',
    financialYear: '2023-24',
  },
  {
    id: '3',
    title: 'Employee Handbook',
    type: 'policy',
    category: 'Policies',
    uploadedAt: '2024-01-01T00:00:00Z',
    fileSize: '3.5 MB',
    isCompanyWide: true,
  },
  {
    id: '4',
    title: 'NDA Agreement',
    type: 'agreement',
    category: 'Legal',
    uploadedAt: '2024-01-15T10:00:00Z',
    fileSize: '180 KB',
  },
]

const documentCategories = [
  { id: 'employment', label: 'Employment', icon: Briefcase, color: 'from-blue-100 to-blue-50' },
  { id: 'tax', label: 'Tax', icon: Receipt, color: 'from-purple-100 to-purple-50' },
  { id: 'policies', label: 'Policies', icon: Shield, color: 'from-green-100 to-green-50' },
  { id: 'certificates', label: 'Certificates', icon: Award, color: 'from-yellow-100 to-yellow-50' },
]

const getDocumentIcon = (type: string) => {
  switch (type) {
    case 'offer_letter':
    case 'appointment_letter':
      return Briefcase
    case 'form16':
    case 'form12bb':
      return Receipt
    case 'policy':
    case 'handbook':
      return Shield
    case 'certificate':
      return Award
    default:
      return FileText
  }
}

export function DocumentsPage() {
  const documents = mockDocuments

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Documents</h1>
        <p className="text-sm text-gray-500">
          Access your employment documents and company policies
        </p>
      </div>

      {/* Coming Soon Notice */}
      <GlassCard className="p-4 mb-6 border-l-4 border-primary-500">
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-100">
            <Sparkles size={18} className="text-primary-600" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-gray-900">Document Repository Coming Soon</h3>
            <p className="text-xs text-gray-500 mt-0.5">
              Full document management and requests will be available once the backend is ready.
            </p>
          </div>
        </div>
      </GlassCard>

      {/* Quick Categories */}
      <div className="grid grid-cols-2 gap-3 mb-6">
        {documentCategories.map((category) => {
          const Icon = category.icon
          const count = documents.filter(d => d.category.toLowerCase() === category.id).length

          return (
            <GlassCard
              key={category.id}
              className="p-4 cursor-pointer touch-feedback"
              hoverEffect
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
      <Button variant="outline" className="w-full mb-6" disabled>
        <FileText size={16} className="mr-2" />
        Request New Document
      </Button>

      {/* Documents List */}
      <div className="mb-4">
        <h2 className="text-sm font-semibold text-gray-900 mb-3">Recent Documents</h2>
      </div>

      {documents.length === 0 ? (
        <EmptyState
          icon={<FolderOpen className="text-gray-400" size={24} />}
          title="No documents"
          description="Your documents will appear here once uploaded"
        />
      ) : (
        <div className="space-y-3">
          {documents.map((doc) => {
            const DocIcon = getDocumentIcon(doc.type)

            return (
              <GlassCard key={doc.id} className="p-4" hoverEffect>
                <div className="flex items-center gap-3">
                  <div className="flex items-center justify-center w-11 h-11 rounded-xl bg-gradient-to-br from-gray-100 to-gray-50">
                    <DocIcon size={18} className="text-gray-600" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-semibold text-gray-900 truncate">{doc.title}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Badge variant="glass" size="sm">{doc.category}</Badge>
                      <span className="text-[10px] text-gray-400">{doc.fileSize}</span>
                    </div>
                  </div>
                  <button
                    className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-50 text-primary-600 hover:bg-primary-100 transition-colors"
                    title="Download"
                    disabled
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
    </div>
  )
}
