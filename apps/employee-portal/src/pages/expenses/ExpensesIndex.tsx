import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Plus, Receipt, Clock, CheckCircle, XCircle, Wallet, FileText } from 'lucide-react'
import { expenseApi, ExpenseClaim } from '@/api/expense'
import { PageHeader, EmptyState } from '@/components/layout'
import { Card, Button, Badge, PageLoader } from '@/components/ui'
import { formatDate, formatCurrency } from '@/utils/format'
import { cn } from '@/utils/cn'

const statusConfig: Record<string, { color: string; icon: React.ReactNode; label: string }> = {
  draft: { color: 'secondary', icon: <FileText size={14} />, label: 'Draft' },
  submitted: { color: 'info', icon: <Clock size={14} />, label: 'Submitted' },
  pending_approval: { color: 'warning', icon: <Clock size={14} />, label: 'Pending' },
  approved: { color: 'success', icon: <CheckCircle size={14} />, label: 'Approved' },
  rejected: { color: 'destructive', icon: <XCircle size={14} />, label: 'Rejected' },
  reimbursed: { color: 'default', icon: <Wallet size={14} />, label: 'Reimbursed' },
  cancelled: { color: 'secondary', icon: <XCircle size={14} />, label: 'Cancelled' },
}

export function ExpensesIndexPage() {
  const navigate = useNavigate()
  const [activeTab, setActiveTab] = useState<'all' | 'pending' | 'completed'>('all')

  const { data, isLoading, error } = useQuery({
    queryKey: ['my-expenses', activeTab],
    queryFn: () => {
      const statusFilter =
        activeTab === 'pending'
          ? 'draft,submitted,pending_approval'
          : activeTab === 'completed'
          ? 'approved,rejected,reimbursed,cancelled'
          : undefined
      return expenseApi.getMyExpenses(1, 50, statusFilter)
    },
  })

  const expenses = data?.data || []

  // Group expenses by month
  const groupedExpenses = expenses.reduce((groups, expense) => {
    const date = new Date(expense.createdAt)
    const monthKey = date.toLocaleDateString('en-US', { year: 'numeric', month: 'long' })
    if (!groups[monthKey]) {
      groups[monthKey] = []
    }
    groups[monthKey].push(expense)
    return groups
  }, {} as Record<string, ExpenseClaim[]>)

  if (isLoading) {
    return <PageLoader />
  }

  if (error) {
    return (
      <div className="animate-fade-in">
        <PageHeader title="My Expenses" />
        <div className="px-4 py-8 text-center">
          <p className="text-red-600">Failed to load expenses. Please try again.</p>
        </div>
      </div>
    )
  }

  return (
    <div className="animate-fade-in">
      <PageHeader
        title="My Expenses"
        rightContent={
          <Button
            size="sm"
            onClick={() => navigate('/expenses/submit')}
            className="gap-1"
          >
            <Plus size={16} />
            New
          </Button>
        }
      />

      {/* Tabs */}
      <div className="px-4 pb-2">
        <div className="flex bg-gray-100 rounded-xl p-1">
          {[
            { key: 'all', label: 'All' },
            { key: 'pending', label: 'Pending' },
            { key: 'completed', label: 'Completed' },
          ].map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key as typeof activeTab)}
              className={cn(
                'flex-1 py-2 px-3 text-sm font-medium rounded-lg transition-all',
                activeTab === tab.key
                  ? 'bg-white text-gray-900 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {/* Content */}
      <div className="px-4 py-4 space-y-4">
        {expenses.length === 0 ? (
          <EmptyState
            icon={<Receipt className="w-12 h-12" />}
            title="No expenses yet"
            description={
              activeTab === 'pending'
                ? "You don't have any pending expense claims."
                : activeTab === 'completed'
                ? "You don't have any completed expense claims."
                : "You haven't submitted any expense claims yet."
            }
            action={
              <Button onClick={() => navigate('/expenses/submit')}>
                <Plus size={16} className="mr-1" />
                Submit Expense
              </Button>
            }
          />
        ) : (
          Object.entries(groupedExpenses).map(([month, monthExpenses]) => (
            <div key={month}>
              <h3 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
                {month}
              </h3>
              <div className="space-y-2">
                {monthExpenses.map((expense) => (
                  <ExpenseCard
                    key={expense.id}
                    expense={expense}
                    onClick={() => navigate(`/expenses/${expense.id}`)}
                  />
                ))}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  )
}

interface ExpenseCardProps {
  expense: ExpenseClaim
  onClick: () => void
}

function ExpenseCard({ expense, onClick }: ExpenseCardProps) {
  const config = statusConfig[expense.status] || statusConfig.draft

  return (
    <Card
      className="p-4 cursor-pointer hover:shadow-md transition-shadow active:scale-[0.99]"
      onClick={onClick}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium text-gray-500">{expense.claimNumber}</span>
            <Badge
              variant={config.color as any}
              className="flex items-center gap-1 text-xs"
            >
              {config.icon}
              {config.label}
            </Badge>
          </div>
          <h4 className="font-semibold text-gray-900 mt-1 truncate">{expense.title}</h4>
          <div className="flex items-center gap-3 mt-1 text-sm text-gray-500">
            <span>{expense.categoryName}</span>
            <span className="w-1 h-1 rounded-full bg-gray-300" />
            <span>{formatDate(expense.expenseDate)}</span>
          </div>
        </div>
        <div className="text-right ml-4">
          <p className="font-bold text-gray-900">
            {formatCurrency(expense.amount, expense.currency)}
          </p>
        </div>
      </div>
    </Card>
  )
}
