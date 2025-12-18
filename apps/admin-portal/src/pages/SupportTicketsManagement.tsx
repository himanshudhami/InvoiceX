import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useSupportTickets, useSupportTicket, useUpdateSupportTicket, useAddTicketMessage, useFaqItems, useCreateFaq, useUpdateFaq, useDeleteFaq } from '@/hooks/api/useSupportTickets'
import { useCompanies } from '@/hooks/api/useCompanies'
import { SupportTicket, UpdateSupportTicketDto, FaqItem, CreateFaqDto, UpdateFaqDto } from '@/services/api/supportTicketService'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { CompanySelect } from '@/components/ui/CompanySelect'
import { Edit, Trash2, MessageSquare, HelpCircle, Send } from 'lucide-react'
import { format } from 'date-fns'

const STATUSES = ['open', 'in_progress', 'waiting_on_employee', 'resolved', 'closed']
const CATEGORIES = ['payroll', 'leave', 'it', 'hr', 'assets', 'general']
const PRIORITIES = ['low', 'medium', 'high', 'urgent']

const SupportTicketsManagement = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')
  const [statusFilter, setStatusFilter] = useState<string>('')
  const [viewingTicket, setViewingTicket] = useState<SupportTicket | null>(null)
  const [newMessage, setNewMessage] = useState('')

  const { data: tickets = [], isLoading, error, refetch } = useSupportTickets(selectedCompanyId || undefined, statusFilter || undefined)
  const { data: companies = [] } = useCompanies()
  const { data: ticketDetail, refetch: refetchTicketDetail } = useSupportTicket(viewingTicket?.id || '', !!viewingTicket)
  const updateTicket = useUpdateSupportTicket()
  const addMessage = useAddTicketMessage()

  // FAQ state
  const [isCreateFaqOpen, setIsCreateFaqOpen] = useState(false)
  const [editingFaq, setEditingFaq] = useState<FaqItem | null>(null)
  const [deletingFaq, setDeletingFaq] = useState<FaqItem | null>(null)
  const { data: faqItems = [] } = useFaqItems(selectedCompanyId || undefined)
  const createFaq = useCreateFaq()
  const updateFaq = useUpdateFaq()
  const deleteFaq = useDeleteFaq()

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'open': return 'bg-blue-100 text-blue-800'
      case 'in_progress': return 'bg-yellow-100 text-yellow-800'
      case 'waiting_on_employee': return 'bg-purple-100 text-purple-800'
      case 'resolved': return 'bg-green-100 text-green-800'
      case 'closed': return 'bg-gray-100 text-gray-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'urgent': return 'bg-red-100 text-red-800'
      case 'high': return 'bg-orange-100 text-orange-800'
      case 'medium': return 'bg-blue-100 text-blue-800'
      case 'low': return 'bg-gray-100 text-gray-800'
      default: return 'bg-gray-100 text-gray-800'
    }
  }

  const handleStatusChange = async (ticketId: string, newStatus: string) => {
    const ticket = tickets.find(t => t.id === ticketId)
    if (!ticket) return

    await updateTicket.mutateAsync({
      id: ticketId,
      data: {
        subject: ticket.subject,
        description: ticket.description,
        category: ticket.category,
        priority: ticket.priority,
        status: newStatus,
      }
    })
    refetch()
  }

  const handleSendMessage = async () => {
    if (!viewingTicket || !newMessage.trim()) return
    await addMessage.mutateAsync({
      ticketId: viewingTicket.id,
      data: { message: newMessage }
    })
    setNewMessage('')
    refetchTicketDetail()
  }

  const handleDeleteFaqConfirm = async () => {
    if (deletingFaq) {
      await deleteFaq.mutateAsync(deletingFaq.id)
      setDeletingFaq(null)
    }
  }

  const ticketColumns: ColumnDef<SupportTicket>[] = [
    {
      accessorKey: 'ticketNumber',
      header: 'Ticket',
      cell: ({ row }) => (
        <div>
          <div className="font-mono text-sm font-medium">{row.original.ticketNumber}</div>
          <div className="text-xs text-gray-500">{row.original.employeeName}</div>
        </div>
      ),
    },
    {
      accessorKey: 'subject',
      header: 'Subject',
      cell: ({ row }) => (
        <div className="max-w-xs truncate">{row.original.subject}</div>
      ),
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
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => (
        <Select value={row.original.status} onValueChange={(value) => handleStatusChange(row.original.id, value)}>
          <SelectTrigger className={`h-7 text-xs font-medium border-0 ${getStatusColor(row.original.status)}`}>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STATUSES.map(s => (
              <SelectItem key={s} value={s}>{s.replace(/_/g, ' ')}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => format(new Date(row.original.createdAt), 'MMM d, HH:mm'),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => (
        <button
          onClick={() => setViewingTicket(row.original)}
          className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50"
          title="View messages"
        >
          <MessageSquare size={16} />
        </button>
      ),
    },
  ]

  const faqColumns: ColumnDef<FaqItem>[] = [
    { accessorKey: 'category', header: 'Category', cell: ({ row }) => <span className="capitalize">{row.original.category}</span> },
    { accessorKey: 'question', header: 'Question', cell: ({ row }) => <div className="max-w-md truncate">{row.original.question}</div> },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <span className={`px-2 py-1 text-xs rounded-full ${row.original.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
          {row.original.isActive ? 'Active' : 'Inactive'}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => (
        <div className="flex space-x-2">
          <button onClick={() => setEditingFaq(row.original)} className="text-blue-600 hover:text-blue-800 p-1"><Edit size={16} /></button>
          <button onClick={() => setDeletingFaq(row.original)} className="text-red-600 hover:text-red-800 p-1"><Trash2 size={16} /></button>
        </div>
      ),
    },
  ]

  if (isLoading) {
    return <div className="flex items-center justify-center h-64"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div></div>
  }

  if (error) {
    return <div className="text-center py-12"><div className="text-red-600 mb-4">Failed to load tickets</div></div>
  }

  const openCount = tickets.filter(t => t.status === 'open').length
  const inProgressCount = tickets.filter(t => t.status === 'in_progress').length

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <HelpCircle className="h-8 w-8 text-blue-600" />
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Help Desk</h1>
          <p className="text-gray-600 mt-1">Manage support tickets and FAQs</p>
        </div>
      </div>

      <Tabs defaultValue="tickets">
        <TabsList>
          <TabsTrigger value="tickets">Support Tickets</TabsTrigger>
          <TabsTrigger value="faq">FAQ Management</TabsTrigger>
        </TabsList>

        <TabsContent value="tickets" className="space-y-6">
          <div className="flex items-center gap-4">
            <CompanySelect
              companies={companies}
              value={selectedCompanyId}
              onChange={setSelectedCompanyId}
              showAllOption
              allOptionLabel="All Companies"
              className="w-[200px]"
            />
            <Select value={statusFilter || 'all'} onValueChange={(value) => setStatusFilter(value === 'all' ? '' : value)}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="All Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                {STATUSES.map(s => <SelectItem key={s} value={s}>{s.replace(/_/g, ' ')}</SelectItem>)}
              </SelectContent>
            </Select>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm font-medium text-gray-500">Total Tickets</div>
              <div className="text-2xl font-bold text-gray-900">{tickets.length}</div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm font-medium text-gray-500">Open</div>
              <div className="text-2xl font-bold text-blue-600">{openCount}</div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm font-medium text-gray-500">In Progress</div>
              <div className="text-2xl font-bold text-yellow-600">{inProgressCount}</div>
            </div>
            <div className="bg-white rounded-lg shadow p-4">
              <div className="text-sm font-medium text-gray-500">Resolved Today</div>
              <div className="text-2xl font-bold text-green-600">
                {tickets.filter(t => t.status === 'resolved' && t.resolvedAt && new Date(t.resolvedAt).toDateString() === new Date().toDateString()).length}
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow p-6">
            <DataTable columns={ticketColumns} data={tickets} searchPlaceholder="Search tickets..." />
          </div>
        </TabsContent>

        <TabsContent value="faq" className="space-y-6">
          <div className="bg-white rounded-lg shadow p-6">
            <DataTable
              columns={faqColumns}
              data={faqItems}
              searchPlaceholder="Search FAQs..."
              onAdd={() => setIsCreateFaqOpen(true)}
              addButtonText="Add FAQ"
            />
          </div>
        </TabsContent>
      </Tabs>

      {/* Ticket Detail Drawer */}
      <Drawer isOpen={!!viewingTicket} onClose={() => setViewingTicket(null)} title={viewingTicket?.ticketNumber || 'Ticket Detail'} size="lg">
        {ticketDetail && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <h3 className="font-medium">{ticketDetail.subject}</h3>
              <p className="text-sm text-gray-600 mt-2">{ticketDetail.description}</p>
              <div className="flex gap-4 mt-3 text-xs text-gray-500">
                <span>Category: {ticketDetail.category}</span>
                <span>Priority: {ticketDetail.priority}</span>
                <span>Status: {ticketDetail.status}</span>
              </div>
            </div>

            <div className="border-t pt-4">
              <h4 className="font-medium mb-3">Messages</h4>
              <div className="space-y-3 max-h-64 overflow-y-auto">
                {ticketDetail.messages?.map(msg => (
                  <div key={msg.id} className={`p-3 rounded-lg ${msg.senderType === 'admin' ? 'bg-blue-50 ml-8' : 'bg-gray-50 mr-8'}`}>
                    <div className="text-xs text-gray-500 mb-1">{msg.senderName || msg.senderType} - {format(new Date(msg.createdAt), 'MMM d, HH:mm')}</div>
                    <p className="text-sm">{msg.message}</p>
                  </div>
                ))}
              </div>

              <div className="flex gap-2 mt-4">
                <input
                  type="text"
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  placeholder="Type your reply..."
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md"
                  onKeyPress={(e) => e.key === 'Enter' && handleSendMessage()}
                />
                <button
                  onClick={handleSendMessage}
                  disabled={!newMessage.trim() || addMessage.isPending}
                  className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                >
                  <Send size={16} />
                </button>
              </div>
            </div>
          </div>
        )}
      </Drawer>

      {/* FAQ Create/Edit Drawer */}
      <Drawer isOpen={isCreateFaqOpen || !!editingFaq} onClose={() => { setIsCreateFaqOpen(false); setEditingFaq(null) }} title={editingFaq ? 'Edit FAQ' : 'Create FAQ'} size="lg">
        <FaqForm
          faq={editingFaq || undefined}
          companies={companies}
          onSuccess={() => { setIsCreateFaqOpen(false); setEditingFaq(null) }}
          onCancel={() => { setIsCreateFaqOpen(false); setEditingFaq(null) }}
          createMutation={createFaq}
          updateMutation={updateFaq}
        />
      </Drawer>

      {/* Delete FAQ Modal */}
      <Modal isOpen={!!deletingFaq} onClose={() => setDeletingFaq(null)} title="Delete FAQ" size="sm">
        {deletingFaq && (
          <div className="space-y-4">
            <p>Are you sure you want to delete this FAQ?</p>
            <div className="flex justify-end space-x-3">
              <button onClick={() => setDeletingFaq(null)} className="px-4 py-2 text-sm text-gray-700 bg-white border rounded-md hover:bg-gray-50">Cancel</button>
              <button onClick={handleDeleteFaqConfirm} disabled={deleteFaq.isPending} className="px-4 py-2 text-sm text-white bg-red-600 rounded-md hover:bg-red-700 disabled:opacity-50">
                {deleteFaq.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

interface FaqFormProps {
  faq?: FaqItem
  companies: { id: string; name: string }[]
  onSuccess: () => void
  onCancel: () => void
  createMutation: ReturnType<typeof useCreateFaq>
  updateMutation: ReturnType<typeof useUpdateFaq>
}

const FaqForm = ({ faq, companies, onSuccess, onCancel, createMutation, updateMutation }: FaqFormProps) => {
  const [formData, setFormData] = useState<CreateFaqDto>({
    companyId: faq?.companyId || companies[0]?.id,
    category: faq?.category || 'general',
    question: faq?.question || '',
    answer: faq?.answer || '',
    sortOrder: faq?.sortOrder || 0,
    isActive: faq?.isActive ?? true,
  })

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (faq) {
      await updateMutation.mutateAsync({ id: faq.id, data: formData as UpdateFaqDto })
    } else {
      await createMutation.mutateAsync(formData)
    }
    onSuccess()
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Category *</label>
        <Select value={formData.category} onValueChange={(value) => setFormData({ ...formData, category: value })}>
          <SelectTrigger className="w-full">
            <SelectValue placeholder="Select category..." />
          </SelectTrigger>
          <SelectContent>
            {CATEGORIES.map(c => <SelectItem key={c} value={c} className="capitalize">{c}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Question *</label>
        <input type="text" value={formData.question} onChange={(e) => setFormData({ ...formData, question: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-md" required />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1">Answer *</label>
        <textarea value={formData.answer} onChange={(e) => setFormData({ ...formData, answer: e.target.value })} className="w-full px-3 py-2 border border-gray-300 rounded-md" rows={4} required />
      </div>
      <label className="flex items-center">
        <input type="checkbox" checked={formData.isActive} onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })} className="mr-2" />
        <span className="text-sm text-gray-700">Active</span>
      </label>
      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button type="button" onClick={onCancel} className="px-4 py-2 text-sm text-gray-700 bg-white border rounded-md hover:bg-gray-50">Cancel</button>
        <button type="submit" disabled={isPending} className="px-4 py-2 text-sm text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50">
          {isPending ? 'Saving...' : faq ? 'Update' : 'Create'}
        </button>
      </div>
    </form>
  )
}

export default SupportTicketsManagement
