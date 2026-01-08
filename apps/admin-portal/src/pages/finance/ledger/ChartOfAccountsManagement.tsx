import { useState, useMemo } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useAccounts, useDeleteAccount, useInitializeChartOfAccounts } from '@/features/ledger/hooks'
import { useCompanyContext } from '@/contexts/CompanyContext'
import { ChartOfAccount, AccountType } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Edit, Trash2, BookOpen, TrendingUp, TrendingDown, Wallet, DollarSign, PlayCircle } from 'lucide-react'
import toast from 'react-hot-toast'
import { ChartOfAccountForm } from '@/components/forms/ChartOfAccountForm'
import { useQueryState, parseAsString } from 'nuqs'

const accountTypeConfig: Record<AccountType, { label: string; color: string; icon: React.ElementType }> = {
  asset: { label: 'Asset', color: 'bg-blue-100 text-blue-800', icon: Wallet },
  liability: { label: 'Liability', color: 'bg-red-100 text-red-800', icon: TrendingDown },
  equity: { label: 'Equity', color: 'bg-purple-100 text-purple-800', icon: BookOpen },
  income: { label: 'Income', color: 'bg-green-100 text-green-800', icon: TrendingUp },
  expense: { label: 'Expense', color: 'bg-orange-100 text-orange-800', icon: DollarSign },
}

const ChartOfAccountsManagement = () => {
  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext()

  // URL-backed filter state with nuqs
  const [companyFilter, setCompanyFilter] = useQueryState('company', parseAsString.withDefault(''))
  const [accountTypeFilter, setAccountTypeFilter] = useQueryState('type', parseAsString.withDefault(''))

  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingAccount, setEditingAccount] = useState<ChartOfAccount | null>(null)
  const [deletingAccount, setDeletingAccount] = useState<ChartOfAccount | null>(null)

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined)

  const { data: allAccounts = [], isLoading, error, refetch } = useAccounts(effectiveCompanyId || undefined)
  const deleteAccount = useDeleteAccount()
  const initializeCoA = useInitializeChartOfAccounts()

  // Filter accounts
  const accounts = useMemo(() => {
    let filtered = allAccounts
    if (accountTypeFilter) {
      filtered = filtered.filter(a => a.accountType === accountTypeFilter)
    }
    // Sort by account code
    return [...filtered].sort((a, b) => a.accountCode.localeCompare(b.accountCode))
  }, [allAccounts, accountTypeFilter])

  const handleEdit = (account: ChartOfAccount) => {
    if (account.isSystemAccount) {
      toast.error('System accounts cannot be edited')
      return
    }
    setEditingAccount(account)
  }

  const handleDelete = (account: ChartOfAccount) => {
    if (account.isSystemAccount) {
      toast.error('System accounts cannot be deleted')
      return
    }
    if (account.currentBalance !== 0) {
      toast.error('Cannot delete account with non-zero balance')
      return
    }
    setDeletingAccount(account)
  }

  const handleDeleteConfirm = async () => {
    if (deletingAccount) {
      try {
        await deleteAccount.mutateAsync(deletingAccount.id)
        setDeletingAccount(null)
        toast.success('Account deleted successfully')
      } catch (error) {
        console.error('Failed to delete account:', error)
        toast.error('Failed to delete account')
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingAccount(null)
    refetch()
  }

  const handleInitialize = async () => {
    if (!companyFilter) {
      toast.error('Please select a company first')
      return
    }
    try {
      await initializeCoA.mutateAsync(companyFilter)
      toast.success('Chart of Accounts initialized successfully')
      refetch()
    } catch (error) {
      console.error('Failed to initialize CoA:', error)
      toast.error('Failed to initialize Chart of Accounts')
    }
  }

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount)
  }

  const columns: ColumnDef<ChartOfAccount>[] = [
    {
      accessorKey: 'accountCode',
      header: 'Code',
      cell: ({ row }) => {
        const account = row.original
        const indent = account.depthLevel * 16
        return (
          <div style={{ paddingLeft: `${indent}px` }} className="font-mono text-sm">
            {account.accountCode}
          </div>
        )
      },
    },
    {
      accessorKey: 'accountName',
      header: 'Account Name',
      cell: ({ row }) => {
        const account = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{account.accountName}</div>
            {account.accountSubtype && (
              <div className="text-sm text-gray-500">{account.accountSubtype}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'accountType',
      header: 'Type',
      cell: ({ row }) => {
        const accountType = row.getValue('accountType') as AccountType
        const config = accountTypeConfig[accountType]
        const Icon = config.icon
        return (
          <div className={`inline-flex items-center gap-1 px-2 py-1 text-xs font-medium rounded-full ${config.color}`}>
            <Icon size={12} />
            {config.label}
          </div>
        )
      },
    },
    {
      accessorKey: 'normalBalance',
      header: 'Normal Balance',
      cell: ({ row }) => {
        const normalBalance = row.getValue('normalBalance') as string
        return (
          <div className={`text-sm font-medium ${
            normalBalance === 'debit' ? 'text-blue-600' : 'text-green-600'
          }`}>
            {normalBalance === 'debit' ? 'Dr' : 'Cr'}
          </div>
        )
      },
    },
    {
      accessorKey: 'currentBalance',
      header: 'Balance',
      cell: ({ row }) => {
        const balance = row.getValue('currentBalance') as number
        const normalBalance = row.original.normalBalance
        return (
          <div className={`font-medium text-right ${
            balance < 0 ? 'text-red-600' : 'text-gray-900'
          }`}>
            {formatCurrency(Math.abs(balance))}
            <span className="text-xs text-gray-500 ml-1">
              {balance !== 0 ? (balance > 0 ? (normalBalance === 'debit' ? 'Dr' : 'Cr') : (normalBalance === 'debit' ? 'Cr' : 'Dr')) : ''}
            </span>
          </div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.getValue('isActive') as boolean
        const isSystem = row.original.isSystemAccount
        return (
          <div className="flex gap-1">
            <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
              isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
            }`}>
              {isActive ? 'Active' : 'Inactive'}
            </span>
            {isSystem && (
              <span className="inline-flex px-2 py-1 text-xs font-medium rounded-full bg-yellow-100 text-yellow-800">
                System
              </span>
            )}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const account = row.original
        const canModify = !account.isSystemAccount
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleEdit(account)}
              className={`p-1 rounded transition-colors ${
                canModify
                  ? 'text-blue-600 hover:text-blue-800 hover:bg-blue-50'
                  : 'text-gray-300 cursor-not-allowed'
              }`}
              title={canModify ? 'Edit account' : 'System account - cannot edit'}
              disabled={!canModify}
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(account)}
              className={`p-1 rounded transition-colors ${
                canModify && account.currentBalance === 0
                  ? 'text-red-600 hover:text-red-800 hover:bg-red-50'
                  : 'text-gray-300 cursor-not-allowed'
              }`}
              title={
                !canModify
                  ? 'System account - cannot delete'
                  : account.currentBalance !== 0
                    ? 'Account has balance - cannot delete'
                    : 'Delete account'
              }
              disabled={!canModify || account.currentBalance !== 0}
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  if (!companyFilter) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Chart of Accounts</h1>
          <p className="text-gray-600 mt-2">Manage your general ledger accounts (Indian Schedule III)</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="text-center py-12">
            <BookOpen className="mx-auto h-12 w-12 text-gray-400" />
            <h3 className="mt-2 text-lg font-medium text-gray-900">Select a Company</h3>
            <p className="mt-1 text-sm text-gray-500">Please select a company to view its Chart of Accounts</p>
            <div className="mt-6 flex justify-center">
              <CompanyFilterDropdown
                value={companyFilter ?? ''}
                onChange={(value) => setCompanyFilter(value || null)}
              />
            </div>
          </div>
        </div>
      </div>
    )
  }

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
        <div className="text-red-600 mb-4">Failed to load accounts</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Chart of Accounts</h1>
          <p className="text-gray-600 mt-2">Manage your general ledger accounts (Indian Schedule III)</p>
        </div>
        {accounts.length === 0 && (
          <button
            onClick={handleInitialize}
            disabled={initializeCoA.isPending}
            className="inline-flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
          >
            <PlayCircle size={16} />
            {initializeCoA.isPending ? 'Initializing...' : 'Initialize Standard CoA'}
          </button>
        )}
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center gap-4 flex-wrap">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown
                value={companyFilter ?? ''}
                onChange={(value) => setCompanyFilter(value || null)}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Account Type</label>
              <select
                value={accountTypeFilter}
                onChange={(e) => setAccountTypeFilter(e.target.value)}
                className="block w-48 rounded-md border-gray-300 shadow-sm focus:border-primary focus:ring-primary sm:text-sm"
              >
                <option value="">All Types</option>
                <option value="asset">Assets</option>
                <option value="liability">Liabilities</option>
                <option value="equity">Equity</option>
                <option value="income">Income</option>
                <option value="expense">Expenses</option>
              </select>
            </div>
          </div>

          {accounts.length === 0 ? (
            <div className="text-center py-12">
              <BookOpen className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-lg font-medium text-gray-900">No Accounts Found</h3>
              <p className="mt-1 text-sm text-gray-500">
                Get started by initializing the standard Indian Chart of Accounts or create custom accounts.
              </p>
              <div className="mt-6 flex justify-center gap-4">
                <button
                  onClick={handleInitialize}
                  disabled={initializeCoA.isPending}
                  className="inline-flex items-center gap-2 px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
                >
                  <PlayCircle size={16} />
                  {initializeCoA.isPending ? 'Initializing...' : 'Initialize Standard CoA'}
                </button>
                <button
                  onClick={() => setIsCreateDrawerOpen(true)}
                  className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
                >
                  Create Custom Account
                </button>
              </div>
            </div>
          ) : (
            <DataTable
              columns={columns}
              data={accounts}
              searchPlaceholder="Search accounts..."
              onAdd={() => setIsCreateDrawerOpen(true)}
              addButtonText="Add Account"
            />
          )}
        </div>
      </div>

      {/* Account Type Summary */}
      {accounts.length > 0 && (
        <div className="grid grid-cols-5 gap-4">
          {(Object.entries(accountTypeConfig) as [AccountType, typeof accountTypeConfig[AccountType]][]).map(([type, config]) => {
            const typeAccounts = accounts.filter(a => a.accountType === type)
            const totalBalance = typeAccounts.reduce((sum, a) => sum + a.currentBalance, 0)
            const Icon = config.icon
            return (
              <div key={type} className="bg-white rounded-lg shadow p-4">
                <div className="flex items-center gap-2 mb-2">
                  <div className={`p-2 rounded-full ${config.color}`}>
                    <Icon size={16} />
                  </div>
                  <span className="font-medium text-gray-900">{config.label}</span>
                </div>
                <div className="text-2xl font-bold text-gray-900">
                  {formatCurrency(Math.abs(totalBalance))}
                </div>
                <div className="text-sm text-gray-500">
                  {typeAccounts.length} account{typeAccounts.length !== 1 ? 's' : ''}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Create Account Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Account"
        size="lg"
      >
        <ChartOfAccountForm
          companyId={companyFilter}
          accounts={accounts}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Account Drawer */}
      <Drawer
        isOpen={!!editingAccount}
        onClose={() => setEditingAccount(null)}
        title="Edit Account"
        size="lg"
      >
        {editingAccount && (
          <ChartOfAccountForm
            companyId={companyFilter}
            account={editingAccount}
            accounts={accounts}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingAccount(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingAccount}
        onClose={() => setDeletingAccount(null)}
        title="Delete Account"
      >
        <div className="space-y-4">
          <p className="text-gray-600">
            Are you sure you want to delete the account{' '}
            <span className="font-medium text-gray-900">
              {deletingAccount?.accountCode} - {deletingAccount?.accountName}
            </span>
            ? This action cannot be undone.
          </p>
          <div className="flex justify-end gap-3">
            <button
              onClick={() => setDeletingAccount(null)}
              className="px-4 py-2 text-gray-700 bg-gray-100 rounded-md hover:bg-gray-200"
            >
              Cancel
            </button>
            <button
              onClick={handleDeleteConfirm}
              disabled={deleteAccount.isPending}
              className="px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
            >
              {deleteAccount.isPending ? 'Deleting...' : 'Delete'}
            </button>
          </div>
        </div>
      </Modal>
    </div>
  )
}

export default ChartOfAccountsManagement
