import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { ColumnDef } from '@tanstack/react-table'
import { useBankAccounts, useDeleteBankAccount, useSetPrimaryBankAccount } from '@/hooks/api/useBankAccounts'
import { useCompanies } from '@/hooks/api/useCompanies'
import { BankAccount } from '@/services/api/types'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { BankAccountForm } from '@/components/forms/BankAccountForm'
import { Edit, Trash2, Star, Upload, Building2, CreditCard, Eye } from 'lucide-react'
import { CompanySelect } from '@/components/ui/CompanySelect'

const BankAccountsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingAccount, setEditingAccount] = useState<BankAccount | null>(null)
  const [deletingAccount, setDeletingAccount] = useState<BankAccount | null>(null)
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('')

  const { data: bankAccounts = [], isLoading, error, refetch } = useBankAccounts()
  const { data: companies = [] } = useCompanies()
  const deleteBankAccount = useDeleteBankAccount()
  const setPrimaryAccount = useSetPrimaryBankAccount()

  const companiesMap = new Map(companies.map(c => [c.id, c.name]))
  const filteredAccounts = useMemo(
    () => (selectedCompanyId ? bankAccounts.filter((a) => a.companyId === selectedCompanyId) : bankAccounts),
    [bankAccounts, selectedCompanyId]
  )

  const handleEdit = (account: BankAccount) => {
    setEditingAccount(account)
  }

  const handleDelete = (account: BankAccount) => {
    setDeletingAccount(account)
  }

  const handleDeleteConfirm = async () => {
    if (deletingAccount) {
      try {
        await deleteBankAccount.mutateAsync(deletingAccount.id)
        setDeletingAccount(null)
      } catch (error) {
        console.error('Failed to delete bank account:', error)
      }
    }
  }

  const handleSetPrimary = async (account: BankAccount) => {
    if (account.companyId) {
      try {
        await setPrimaryAccount.mutateAsync({
          companyId: account.companyId,
          accountId: account.id,
        })
      } catch (error) {
        console.error('Failed to set primary account:', error)
      }
    }
  }

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false)
    setEditingAccount(null)
    refetch()
  }

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: currency || 'INR',
      maximumFractionDigits: 2,
    }).format(amount)
  }

  const columns: ColumnDef<BankAccount>[] = [
    {
      accessorKey: 'accountName',
      header: 'Account',
      cell: ({ row }) => {
        const account = row.original
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <CreditCard className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900 flex items-center gap-2">
                {account.accountName}
                {account.isPrimary && (
                  <span title="Primary Account">
                    <Star className="h-4 w-4 text-yellow-500 fill-yellow-500" />
                  </span>
                )}
              </div>
              <div className="text-sm text-gray-500">{account.accountNumber}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'bankName',
      header: 'Bank',
      cell: ({ row }) => {
        const account = row.original
        return (
          <div>
            <div className="font-medium text-gray-900">{account.bankName}</div>
            {account.branchName && (
              <div className="text-sm text-gray-500">{account.branchName}</div>
            )}
            {account.ifscCode && (
              <div className="text-xs text-gray-400 font-mono">{account.ifscCode}</div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'companyId',
      header: 'Company',
      cell: ({ row }) => {
        const companyId = row.original.companyId
        const companyName = companyId ? companiesMap.get(companyId) : null
        return companyName ? (
          <div className="flex items-center gap-2">
            <Building2 className="h-4 w-4 text-gray-400" />
            <span className="text-sm text-gray-900">{companyName}</span>
          </div>
        ) : (
          <span className="text-sm text-gray-500">-</span>
        )
      },
    },
    {
      accessorKey: 'accountType',
      header: 'Type',
      cell: ({ row }) => {
        const account = row.original
        const typeLabels: Record<string, string> = {
          current: 'Current',
          savings: 'Savings',
          cc: 'Cash Credit',
          od: 'Overdraft',
          foreign: 'Foreign Currency',
        }
        return (
          <div className="flex flex-col">
            <span className="text-sm font-medium text-gray-900">
              {typeLabels[account.accountType] || account.accountType}
            </span>
            <span className="text-xs text-gray-500">{account.currency}</span>
          </div>
        )
      },
    },
    {
      accessorKey: 'currentBalance',
      header: 'Balance',
      cell: ({ row }) => {
        const account = row.original
        const balance = account.currentBalance || 0
        return (
          <div className="text-right">
            <div className={`font-medium ${balance >= 0 ? 'text-green-600' : 'text-red-600'}`}>
              {formatCurrency(balance, account.currency)}
            </div>
            {account.asOfDate && (
              <div className="text-xs text-gray-400">
                as of {new Date(account.asOfDate).toLocaleDateString('en-IN')}
              </div>
            )}
          </div>
        )
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.original.isActive
        return (
          <span
            className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
              isActive
                ? 'bg-green-100 text-green-800'
                : 'bg-gray-100 text-gray-800'
            }`}
          >
            {isActive ? 'Active' : 'Inactive'}
          </span>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const account = row.original
        return (
          <div className="flex space-x-2">
            {account.companyId && !account.isPrimary && (
              <button
                onClick={() => handleSetPrimary(account)}
                className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                title="Set as primary account"
              >
                <Star size={16} />
              </button>
            )}
            <Link
              to={`/bank/transactions?accountId=${account.id}`}
              className="text-purple-600 hover:text-purple-800 p-1 rounded hover:bg-purple-50 transition-colors"
              title="View transactions"
            >
              <Eye size={16} />
            </Link>
            <Link
              to={`/bank/import?accountId=${account.id}`}
              className="text-green-600 hover:text-green-800 p-1 rounded hover:bg-green-50 transition-colors"
              title="Import statement"
            >
              <Upload size={16} />
            </Link>
            <button
              onClick={() => handleEdit(account)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit account"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(account)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete account"
            >
              <Trash2 size={16} />
            </button>
          </div>
        )
      },
    },
  ]

  // Summary statistics
  const totalBalance = bankAccounts.reduce((sum, acc) => {
    // Only sum INR accounts for the summary
    if (acc.currency === 'INR' && acc.isActive) {
      return sum + (acc.currentBalance || 0)
    }
    return sum
  }, 0)

  const activeAccounts = bankAccounts.filter(acc => acc.isActive).length

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
        <div className="text-red-600 mb-4">Failed to load bank accounts</div>
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
          <h1 className="text-3xl font-bold text-gray-900">Bank Accounts</h1>
          <p className="text-gray-600 mt-2">Manage your bank accounts and import statements</p>
        </div>
        <Link
          to="/bank/import"
          className="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-green-600 rounded-md hover:bg-green-700"
        >
          <Upload className="h-4 w-4 mr-2" />
          Import Statement
        </Link>
      </div>

      {/* Filters / Actions */}
      <div className="flex flex-wrap items-center gap-3 justify-between bg-white p-4 rounded-lg shadow">
        <div className="flex items-center gap-3">
          <CompanySelect
            companies={companies}
            value={selectedCompanyId}
            onChange={setSelectedCompanyId}
            placeholder="Filter by company"
            showAllOption
            className="w-64"
          />
        </div>
        <button
          onClick={() => setIsCreateDrawerOpen(true)}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 transition-colors"
        >
          Add Bank Account
        </button>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total Accounts</p>
              <p className="text-2xl font-bold text-gray-900">{bankAccounts.length}</p>
            </div>
            <div className="p-3 bg-blue-100 rounded-full">
              <CreditCard className="h-6 w-6 text-blue-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">{activeAccounts} active</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total Balance (INR)</p>
              <p className={`text-2xl font-bold ${totalBalance >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                {formatCurrency(totalBalance, 'INR')}
              </p>
            </div>
            <div className="p-3 bg-green-100 rounded-full">
              <Building2 className="h-6 w-6 text-green-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Across active INR accounts</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Banks Connected</p>
              <p className="text-2xl font-bold text-gray-900">
                {new Set(bankAccounts.map(a => a.bankName)).size}
              </p>
            </div>
            <div className="p-3 bg-purple-100 rounded-full">
              <Star className="h-6 w-6 text-purple-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Unique banks</p>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={filteredAccounts}
            searchPlaceholder="Search accounts..."
          />
        </div>
      </div>

      {/* Create Account Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Add Bank Account"
        size="lg"
      >
        <BankAccountForm
          defaultCompanyId={selectedCompanyId || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Account Drawer */}
      <Drawer
        isOpen={!!editingAccount}
        onClose={() => setEditingAccount(null)}
        title="Edit Bank Account"
        size="lg"
      >
        {editingAccount && (
          <BankAccountForm
            bankAccount={editingAccount}
            defaultCompanyId={selectedCompanyId || undefined}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingAccount(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingAccount}
        onClose={() => setDeletingAccount(null)}
        title="Delete Bank Account"
        size="sm"
      >
        {deletingAccount && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingAccount.accountName}</strong> ({deletingAccount.bankName})?
              This will also delete all associated transactions. This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingAccount(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteBankAccount.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteBankAccount.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

export default BankAccountsManagement
