import { FC } from 'react'
import { useAssetDocumentList } from '@/features/assets/hooks'
import type { AssetDocument } from '@/services/api/types'
import { formatDate } from '@/lib/date'
import { Button } from '@/components/ui/button'
import {
  FileText,
  Plus,
  Download,
  ExternalLink,
  Trash2,
  AlertCircle,
  Loader2,
  FileImage,
  FileSpreadsheet,
  File,
} from 'lucide-react'

interface DocumentsTabProps {
  assetId: string
  onUpload?: () => void
  onDelete?: (documentId: string) => void
}

const getFileIcon = (contentType?: string) => {
  if (!contentType) return File
  if (contentType.includes('image')) return FileImage
  if (contentType.includes('pdf')) return FileText
  if (contentType.includes('spreadsheet') || contentType.includes('excel') || contentType.includes('csv')) {
    return FileSpreadsheet
  }
  return FileText
}

const DocumentCard: FC<{
  doc: AssetDocument
  onDelete?: (documentId: string) => void
}> = ({ doc, onDelete }) => {
  const FileIcon = getFileIcon(doc.contentType)

  return (
    <div className="flex items-start gap-3 p-3 rounded-lg border border-gray-200 bg-white hover:bg-gray-50 transition-colors">
      <div className="flex-shrink-0 w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center">
        <FileIcon className="w-5 h-5 text-blue-600" />
      </div>
      <div className="flex-1 min-w-0">
        <h4 className="text-sm font-medium text-gray-900 truncate">{doc.name}</h4>
        {doc.uploadedAt && (
          <p className="text-xs text-gray-500 mt-0.5">
            Uploaded {formatDate(doc.uploadedAt)}
          </p>
        )}
        {doc.notes && (
          <p className="text-xs text-gray-500 mt-1 line-clamp-1">{doc.notes}</p>
        )}
      </div>
      <div className="flex items-center gap-1">
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          asChild
        >
          <a href={doc.url} target="_blank" rel="noopener noreferrer">
            <ExternalLink className="w-4 h-4 text-gray-500" />
          </a>
        </Button>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          asChild
        >
          <a href={doc.url} download={doc.name}>
            <Download className="w-4 h-4 text-gray-500" />
          </a>
        </Button>
        {onDelete && (
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8 text-red-500 hover:text-red-600 hover:bg-red-50"
            onClick={() => onDelete(doc.id)}
          >
            <Trash2 className="w-4 h-4" />
          </Button>
        )}
      </div>
    </div>
  )
}

export const DocumentsTab: FC<DocumentsTabProps> = ({
  assetId,
  onUpload,
  onDelete,
}) => {
  const { data: documents, isLoading, isError } = useAssetDocumentList(assetId)

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-5 h-5 animate-spin text-gray-400" />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-500">
        <AlertCircle className="w-8 h-8 mb-2" />
        <p className="text-sm">Failed to load documents</p>
      </div>
    )
  }

  return (
    <div className="p-4 space-y-4">
      {/* Upload Action */}
      {onUpload && (
        <Button onClick={onUpload} className="w-full">
          <Plus className="w-4 h-4 mr-2" />
          Upload Document
        </Button>
      )}

      {/* Document Count */}
      {documents && documents.length > 0 && (
        <div className="flex items-center gap-2 text-sm text-gray-500">
          <FileText className="w-4 h-4" />
          <span>{documents.length} document{documents.length !== 1 ? 's' : ''}</span>
        </div>
      )}

      {/* Document List */}
      {documents && documents.length > 0 && (
        <div className="space-y-2">
          {documents.map((doc) => (
            <DocumentCard key={doc.id} doc={doc} onDelete={onDelete} />
          ))}
        </div>
      )}

      {/* Empty State */}
      {(!documents || documents.length === 0) && (
        <div className="flex flex-col items-center justify-center py-8 text-gray-500">
          <FileText className="w-10 h-10 mb-3 text-gray-300" />
          <p className="text-sm font-medium">No documents</p>
          <p className="text-xs mt-1">Upload invoices, warranties, or other files</p>
        </div>
      )}
    </div>
  )
}
