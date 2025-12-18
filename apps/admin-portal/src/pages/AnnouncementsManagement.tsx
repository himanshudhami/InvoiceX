import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useAnnouncements, useCreateAnnouncement, useUpdateAnnouncement, useDeleteAnnouncement } from '@/hooks/api/useAnnouncements'
import { useCompanies } from '@/hooks/api/useCompanies'
import { Announcement, CreateAnnouncementDto, UpdateAnnouncementDto } from '@/services/api/announcementService'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { Edit, Trash2, Pin, Megaphone } from 'lucide-react'
import { format } from 'date-fns'

const CATEGORIES = ['general', 'hr', 'policy', 'event', 'celebration']
const PRIORITIES = ['low', 'normal', 'high', 'urgent']

const AnnouncementsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingAnnouncement, setEditingAnnouncement] = useState<Announcement | null>(null)
  const [deletingAnnouncement, setDeletingAnnouncement] = useState<Announcement | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')

  const { data: announcements = [], isLoading, error, refetch } = useAnnouncements(selectedCompanyId || undefined)
  const { data: companies = [] } = useCompanies()
  const createAnnouncement = useCreateAnnouncement()
  const updateAnnouncement = useUpdateAnnouncement()
  const deleteAnnouncement = useDeleteAnnouncement()

  const handleDeleteConfirm = async () => {
    if (deletingAnnouncement) {
      try {
        await deleteAnnouncement.mutateAsync(deletingAnnouncement.id)
        setDeletingAnnouncement(null)
      } catch (error) {
        console.error('Failed to delete announcement:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingAnnouncement(null)
    refetch()
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'urgent': return 'bg-red-100 text-red-800'
      case 'high': return 'bg-orange-100 text-orange-800'
      case 'normal': return 'bg-blue-100 text-blue-800'
      case 'low': return 'bg-gray-100 text-gray-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  const columns: ColumnDef<Announcement>[] = [
    {
      accessorKey: 'title',
      header: 'Announcement',
      cell: ({ row }) => {
        const announcement = row.original
        return (
          <div className="flex items-start gap-2">
            {announcement.isPinned && <Pin size={14} className="text-blue-600 mt-1 flex-shrink-0" />}
            <div>
              <div className="font-medium text-gray-900">{announcement.title}</div>
              <div className="text-sm text-gray-500 line-clamp-1">{announcement.content}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'category',
      header: 'Category',
      cell: ({ row }) => (
        <span className="capitalize text-sm">{row.original.category}</span>
      ),
    },
    {
      accessorKey: 'priority',
      header: 'Priority',
      cell: ({ row }) => (
        <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full capitalize ${getPriorityColor(row.original.priority)}`}>
          {row.original.priority}
        </span>
      ),
    },
    {
      accessorKey: 'publishedAt',
      header: 'Published',
      cell: ({ row }) => {
        const date = row.original.publishedAt
        return date ? format(new Date(date), 'MMM d, yyyy') : <span className="text-gray-400">Draft</span>
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const announcement = row.original
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => setEditingAnnouncement(announcement)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit announcement"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => setDeletingAnnouncement(announcement)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete announcement"
            >
              <Trash2 size={16} />
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
        <div className="text-red-600 mb-4">Failed to load announcements</div>
        <button onClick={() => refetch()} className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90">
          Retry
        </button>
      </div>
    )
  }

  const pinnedCount = announcements.filter(a => a.isPinned).length
  const urgentCount = announcements.filter(a => a.priority === 'urgent').length

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Megaphone className="h-8 w-8 text-blue-600" />
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Announcements</h1>
          <p className="text-gray-600 mt-1">Manage company announcements for employees</p>
        </div>
      </div>

      <div className="flex items-center gap-4">
        <label className="text-sm font-medium text-gray-700">Filter by Company:</label>
        <CompanySelect
          companies={companies}
          value={selectedCompanyId}
          onChange={setSelectedCompanyId}
          showAllOption
          allOptionLabel="All Companies"
          className="w-[200px]"
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Announcements</div>
          <div className="text-2xl font-bold text-gray-900">{announcements.length}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Pinned</div>
          <div className="text-2xl font-bold text-blue-600">{pinnedCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Urgent</div>
          <div className="text-2xl font-bold text-red-600">{urgentCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Month</div>
          <div className="text-2xl font-bold text-green-600">
            {announcements.filter(a => {
              const date = a.publishedAt ? new Date(a.publishedAt) : null
              return date && date.getMonth() === new Date().getMonth()
            }).length}
          </div>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={announcements}
            searchPlaceholder="Search announcements..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="New Announcement"
          />
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Announcement"
        size="lg"
      >
        <AnnouncementForm
          companies={companies}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
          createMutation={createAnnouncement}
        />
      </Drawer>

      <Drawer
        isOpen={!!editingAnnouncement}
        onClose={() => setEditingAnnouncement(null)}
        title="Edit Announcement"
        size="lg"
      >
        {editingAnnouncement && (
          <AnnouncementForm
            announcement={editingAnnouncement}
            companies={companies}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingAnnouncement(null)}
            updateMutation={updateAnnouncement}
          />
        )}
      </Drawer>

      <Modal
        isOpen={!!deletingAnnouncement}
        onClose={() => setDeletingAnnouncement(null)}
        title="Delete Announcement"
        size="sm"
      >
        {deletingAnnouncement && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingAnnouncement.title}</strong>?
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingAnnouncement(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteAnnouncement.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteAnnouncement.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

interface AnnouncementFormProps {
  announcement?: Announcement
  companies: { id: string; name: string }[]
  onSuccess: () => void
  onCancel: () => void
  createMutation?: ReturnType<typeof useCreateAnnouncement>
  updateMutation?: ReturnType<typeof useUpdateAnnouncement>
}

const AnnouncementForm = ({ announcement, companies, onSuccess, onCancel, createMutation, updateMutation }: AnnouncementFormProps) => {
  const [formData, setFormData] = useState<CreateAnnouncementDto>({
    companyId: announcement?.companyId || companies[0]?.id || '',
    title: announcement?.title || '',
    content: announcement?.content || '',
    category: announcement?.category || 'general',
    priority: announcement?.priority || 'normal',
    isPinned: announcement?.isPinned || false,
    publishedAt: announcement?.publishedAt ? announcement.publishedAt.split('T')[0] : '',
    expiresAt: announcement?.expiresAt ? announcement.expiresAt.split('T')[0] : '',
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      const payload = {
        ...formData,
        publishedAt: formData.publishedAt ? new Date(formData.publishedAt).toISOString() : undefined,
        expiresAt: formData.expiresAt ? new Date(formData.expiresAt).toISOString() : undefined,
      }
      if (announcement && updateMutation) {
        await updateMutation.mutateAsync({ id: announcement.id, data: payload as UpdateAnnouncementDto })
      } else if (createMutation) {
        await createMutation.mutateAsync(payload)
      }
      onSuccess()
    } catch (error) {
      console.error('Failed to save announcement:', error)
    }
  }

  const isPending = createMutation?.isPending || updateMutation?.isPending

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Company *</label>
        <CompanySelect
          companies={companies}
          value={formData.companyId}
          onChange={(value) => setFormData({ ...formData, companyId: value })}
          placeholder="Select company..."
        />
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
        <label className="block text-sm font-medium text-gray-700 mb-1">Content *</label>
        <textarea
          value={formData.content}
          onChange={(e) => setFormData({ ...formData, content: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
          rows={5}
          required
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Category *</label>
          <Select
            value={formData.category}
            onValueChange={(value) => setFormData({ ...formData, category: value })}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select category..." />
            </SelectTrigger>
            <SelectContent>
              {CATEGORIES.map(cat => (
                <SelectItem key={cat} value={cat} className="capitalize">{cat}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Priority *</label>
          <Select
            value={formData.priority}
            onValueChange={(value) => setFormData({ ...formData, priority: value })}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select priority..." />
            </SelectTrigger>
            <SelectContent>
              {PRIORITIES.map(p => (
                <SelectItem key={p} value={p} className="capitalize">{p}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Publish Date</label>
          <input
            type="date"
            value={formData.publishedAt || ''}
            onChange={(e) => setFormData({ ...formData, publishedAt: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">Expires On</label>
          <input
            type="date"
            value={formData.expiresAt || ''}
            onChange={(e) => setFormData({ ...formData, expiresAt: e.target.value })}
            className="w-full px-3 py-2 border border-gray-300 rounded-md"
          />
        </div>
      </div>

      <label className="flex items-center">
        <input
          type="checkbox"
          checked={formData.isPinned}
          onChange={(e) => setFormData({ ...formData, isPinned: e.target.checked })}
          className="mr-2"
        />
        <span className="text-sm text-gray-700">Pin this announcement</span>
      </label>

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
          disabled={isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {isPending ? 'Saving...' : announcement ? 'Update' : 'Publish'}
        </button>
      </div>
    </form>
  )
}

export default AnnouncementsManagement
