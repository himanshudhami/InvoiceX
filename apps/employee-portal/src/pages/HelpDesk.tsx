import { useState } from 'react'
import {
  MessageSquare,
  ChevronDown,
  ChevronUp,
  Plus,
  Clock,
  CheckCircle,
  AlertCircle,
  Mail,
  Phone,
  Sparkles
} from 'lucide-react'
import { Badge, GlassCard, Button } from '@/components/ui'

// Mock FAQ data
const faqs = [
  {
    id: '1',
    category: 'Leave',
    question: 'How do I apply for leave?',
    answer: 'You can apply for leave by navigating to the Leave section in the sidebar, then clicking on "Apply Leave". Fill in the required details including leave type, dates, and reason.',
  },
  {
    id: '2',
    category: 'Payroll',
    question: 'When are salaries credited?',
    answer: 'Salaries are typically credited on the last working day of each month. You can view your payslips in the Payslips section.',
  },
  {
    id: '3',
    category: 'Tax',
    question: 'How do I submit my tax declarations?',
    answer: 'Go to the Tax Declarations section, select the current financial year, and add your investment details under the relevant sections like 80C, 80D, etc. Remember to upload proof documents.',
  },
  {
    id: '4',
    category: 'Assets',
    question: 'How do I report an issue with my assigned asset?',
    answer: 'Navigate to the Assets section, find the asset with the issue, and use the "Report Issue" option. Describe the problem and our IT team will reach out to you.',
  },
  {
    id: '5',
    category: 'General',
    question: 'How do I update my personal information?',
    answer: 'Go to Profile section where you can view your current information. For most updates, you\'ll need to contact HR as some fields require verification.',
  },
]

// Mock tickets data
const mockTickets = [
  {
    id: '1',
    ticketNumber: 'TKT-2025-001',
    subject: 'Payslip discrepancy for October',
    category: 'payroll',
    status: 'in_progress',
    createdAt: '2025-10-28T10:00:00Z',
  },
  {
    id: '2',
    ticketNumber: 'TKT-2025-002',
    subject: 'Leave balance not updating',
    category: 'leave',
    status: 'resolved',
    createdAt: '2025-10-20T14:30:00Z',
  },
]

const getStatusConfig = (status: string) => {
  switch (status) {
    case 'open':
      return { variant: 'pending' as const, icon: Clock, label: 'Open' }
    case 'in_progress':
      return { variant: 'info' as const, icon: Clock, label: 'In Progress' }
    case 'resolved':
      return { variant: 'approved' as const, icon: CheckCircle, label: 'Resolved' }
    case 'closed':
      return { variant: 'default' as const, icon: CheckCircle, label: 'Closed' }
    default:
      return { variant: 'default' as const, icon: AlertCircle, label: status }
  }
}

export function HelpDeskPage() {
  const [activeTab, setActiveTab] = useState<'faq' | 'tickets'>('faq')
  const [expandedFaq, setExpandedFaq] = useState<string | null>(null)

  return (
    <div className="animate-fade-in pb-4">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 mb-2">Help & Support</h1>
        <p className="text-sm text-gray-500">
          Find answers or raise a support ticket
        </p>
      </div>

      {/* Quick Contact */}
      <GlassCard className="p-4 mb-6">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Need Immediate Help?</h3>
        <div className="grid grid-cols-2 gap-3">
          <a
            href="mailto:hr@company.com"
            className="flex items-center gap-2 p-3 rounded-xl bg-primary-50 text-primary-700 hover:bg-primary-100 transition-colors"
          >
            <Mail size={16} />
            <span className="text-sm font-medium">Email HR</span>
          </a>
          <a
            href="tel:+911234567890"
            className="flex items-center gap-2 p-3 rounded-xl bg-green-50 text-green-700 hover:bg-green-100 transition-colors"
          >
            <Phone size={16} />
            <span className="text-sm font-medium">Call Support</span>
          </a>
        </div>
      </GlassCard>

      {/* Tabs */}
      <div className="flex gap-2 mb-4">
        <button
          onClick={() => setActiveTab('faq')}
          className={`flex-1 py-2.5 px-4 rounded-xl text-sm font-medium transition-all ${
            activeTab === 'faq'
              ? 'bg-primary-600 text-white shadow-md'
              : 'bg-white/70 text-gray-600 hover:bg-white/90'
          }`}
        >
          FAQs
        </button>
        <button
          onClick={() => setActiveTab('tickets')}
          className={`flex-1 py-2.5 px-4 rounded-xl text-sm font-medium transition-all ${
            activeTab === 'tickets'
              ? 'bg-primary-600 text-white shadow-md'
              : 'bg-white/70 text-gray-600 hover:bg-white/90'
          }`}
        >
          My Tickets
        </button>
      </div>

      {/* FAQ Section */}
      {activeTab === 'faq' && (
        <div className="space-y-3">
          {faqs.map((faq) => (
            <GlassCard key={faq.id} className="overflow-hidden">
              <button
                onClick={() => setExpandedFaq(expandedFaq === faq.id ? null : faq.id)}
                className="w-full flex items-start justify-between p-4 text-left hover:bg-gray-50/50 transition-colors"
              >
                <div className="flex-1 pr-3">
                  <Badge variant="glass" size="sm" className="mb-2">{faq.category}</Badge>
                  <p className="text-sm font-medium text-gray-900">{faq.question}</p>
                </div>
                {expandedFaq === faq.id ? (
                  <ChevronUp size={18} className="text-gray-400 mt-1" />
                ) : (
                  <ChevronDown size={18} className="text-gray-400 mt-1" />
                )}
              </button>
              {expandedFaq === faq.id && (
                <div className="px-4 pb-4 pt-0">
                  <p className="text-sm text-gray-600 leading-relaxed">{faq.answer}</p>
                </div>
              )}
            </GlassCard>
          ))}
        </div>
      )}

      {/* Tickets Section */}
      {activeTab === 'tickets' && (
        <div className="space-y-4">
          {/* Coming Soon Notice */}
          <GlassCard className="p-4 border-l-4 border-primary-500">
            <div className="flex items-center gap-3">
              <div className="flex items-center justify-center w-10 h-10 rounded-xl bg-primary-100">
                <Sparkles size={18} className="text-primary-600" />
              </div>
              <div>
                <h3 className="text-sm font-semibold text-gray-900">Ticket System Coming Soon</h3>
                <p className="text-xs text-gray-500 mt-0.5">
                  Full ticket management will be available once the backend is ready.
                </p>
              </div>
            </div>
          </GlassCard>

          {/* Create Ticket Button */}
          <Button variant="gradient" className="w-full" disabled>
            <Plus size={16} className="mr-2" />
            Create New Ticket
          </Button>

          {/* Tickets List */}
          {mockTickets.length === 0 ? (
            <div className="text-center py-8">
              <MessageSquare className="mx-auto text-gray-300 mb-3" size={40} />
              <p className="text-sm text-gray-500">No tickets yet</p>
              <p className="text-xs text-gray-400 mt-1">Create a ticket if you need help</p>
            </div>
          ) : (
            <div className="space-y-3">
              {mockTickets.map((ticket) => {
                const statusConfig = getStatusConfig(ticket.status)
                const StatusIcon = statusConfig.icon

                return (
                  <GlassCard key={ticket.id} className="p-4" hoverEffect>
                    <div className="flex items-start justify-between mb-2">
                      <div>
                        <p className="text-xs text-gray-500 font-mono">{ticket.ticketNumber}</p>
                        <p className="text-sm font-semibold text-gray-900 mt-0.5">{ticket.subject}</p>
                      </div>
                      <Badge variant={statusConfig.variant} size="sm">
                        <StatusIcon size={12} className="mr-1" />
                        {statusConfig.label}
                      </Badge>
                    </div>
                    <div className="flex items-center justify-between">
                      <Badge variant="glass" size="sm">{ticket.category}</Badge>
                      <span className="text-[10px] text-gray-400">
                        {new Date(ticket.createdAt).toLocaleDateString('en-IN', {
                          day: 'numeric',
                          month: 'short',
                          year: 'numeric',
                        })}
                      </span>
                    </div>
                  </GlassCard>
                )
              })}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
