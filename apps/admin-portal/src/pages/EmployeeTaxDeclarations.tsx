import { useState } from 'react'
import { ColumnDef } from '@tanstack/react-table'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useTaxDeclarations,
  useDeleteTaxDeclaration,
  useSubmitTaxDeclaration,
  useVerifyTaxDeclaration,
  useRejectTaxDeclaration,
} from '@/features/payroll/hooks'
import { EmployeeTaxDeclaration } from '@/features/payroll/types/payroll'
import { DataTable } from '@/components/ui/DataTable'
import { Modal } from '@/components/ui/Modal'
import { Drawer } from '@/components/ui/Drawer'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Edit, Trash2, Plus, CheckCircle, Send, ArrowLeft, XCircle, RotateCcw, AlertCircle } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import { format } from 'date-fns'
import { TaxDeclarationForm } from '@/components/forms/TaxDeclarationForm'

const EmployeeTaxDeclarations = () => {
  const navigate = useNavigate()
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false)
  const [editingDeclaration, setEditingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)
  const [deletingDeclaration, setDeletingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)
  const [submittingDeclaration, setSubmittingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)
  const [verifyingDeclaration, setVerifyingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)
  const [rejectingDeclaration, setRejectingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)
  const [rejectionReason, setRejectionReason] = useState('')
  const [revisingDeclaration, setRevisingDeclaration] = useState<EmployeeTaxDeclaration | null>(null)

  const deleteTaxDeclaration = useDeleteTaxDeclaration()
  const submitTaxDeclaration = useSubmitTaxDeclaration()
  const verifyTaxDeclaration = useVerifyTaxDeclaration()
  const rejectTaxDeclaration = useRejectTaxDeclaration()

  const [urlState, setUrlState] = useQueryStates(
    {
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
      searchTerm: parseAsString,
      companyId: parseAsString,
      employeeId: parseAsString,
      financialYear: parseAsString,
      status: parseAsString,
      taxRegime: parseAsString,
    },
    { history: 'push' }
  )

  const { data, isLoading, error } = useTaxDeclarations({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: urlState.searchTerm || undefined,
    companyId: urlState.companyId || undefined,
    employeeId: urlState.employeeId || undefined,
    financialYear: urlState.financialYear || undefined,
    status: urlState.status || undefined,
    taxRegime: urlState.taxRegime || undefined,
  })

  const handleEdit = (declaration: EmployeeTaxDeclaration) => {
    setEditingDeclaration(declaration)
  }

  const handleDelete = (declaration: EmployeeTaxDeclaration) => {
    setDeletingDeclaration(declaration)
  }

  const handleDeleteConfirm = async () => {
    if (deletingDeclaration) {
      try {
        await deleteTaxDeclaration.mutateAsync(deletingDeclaration.id)
        setDeletingDeclaration(null)
      } catch (error) {
        console.error('Failed to delete tax declaration:', error)
      }
    }
  }

  const handleSubmit = (declaration: EmployeeTaxDeclaration) => {
    setSubmittingDeclaration(declaration)
  }

  const handleSubmitConfirm = async () => {
    if (submittingDeclaration) {
      try {
        await submitTaxDeclaration.mutateAsync(submittingDeclaration.id)
        setSubmittingDeclaration(null)
      } catch (error) {
        console.error('Failed to submit tax declaration:', error)
      }
    }
  }

  const handleVerify = (declaration: EmployeeTaxDeclaration) => {
    setVerifyingDeclaration(declaration)
  }

  const handleVerifyConfirm = async () => {
    if (verifyingDeclaration) {
      try {
        await verifyTaxDeclaration.mutateAsync({ id: verifyingDeclaration.id })
        setVerifyingDeclaration(null)
      } catch (error) {
        console.error('Failed to verify tax declaration:', error)
      }
    }
  }

  const handleReject = (declaration: EmployeeTaxDeclaration) => {
    setRejectingDeclaration(declaration)
    setRejectionReason('')
  }

  const handleRejectConfirm = async () => {
    if (rejectingDeclaration && rejectionReason.trim()) {
      try {
        await rejectTaxDeclaration.mutateAsync({
          id: rejectingDeclaration.id,
          data: { reason: rejectionReason.trim() },
        })
        setRejectingDeclaration(null)
        setRejectionReason('')
      } catch (error) {
        console.error('Failed to reject tax declaration:', error)
      }
    }
  }

  const handleRevise = (declaration: EmployeeTaxDeclaration) => {
    setRevisingDeclaration(declaration)
  }

  const getStatusBadge = (status: string, declaration?: EmployeeTaxDeclaration) => {
    const statusConfig: Record<string, { label: string; className: string }> = {
      draft: { label: 'Draft', className: 'bg-gray-100 text-gray-800' },
      submitted: { label: 'Submitted', className: 'bg-blue-100 text-blue-800' },
      verified: { label: 'Verified', className: 'bg-green-100 text-green-800' },
      rejected: { label: 'Rejected', className: 'bg-orange-100 text-orange-800' },
      locked: { label: 'Locked', className: 'bg-purple-100 text-purple-800' },
    }

    const config = statusConfig[status] || statusConfig.draft

    if (status === 'rejected' && declaration?.rejectionReason) {
      return (
        <div className="flex items-center gap-1">
          <Badge className={config.className}>{config.label}</Badge>
          <span title={declaration.rejectionReason} className="cursor-help">
            <AlertCircle className="w-4 h-4 text-orange-500" />
          </span>
        </div>
      )
    }

    return <Badge className={config.className}>{config.label}</Badge>
  }

  const columns: ColumnDef<EmployeeTaxDeclaration>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => row.original.employeeName || '—',
    },
    {
      accessorKey: 'financialYear',
      header: 'Financial Year',
    },
    {
      accessorKey: 'taxRegime',
      header: 'Regime',
      cell: ({ row }) => (
        <span className="capitalize">{row.original.taxRegime || 'new'}</span>
      ),
    },
    {
      accessorKey: 'totalDeductions',
      header: 'Total Deductions',
      cell: ({ row }) => `₹${row.original.totalDeductions.toLocaleString('en-IN')}`,
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status, row.original),
    },
    {
      accessorKey: 'submittedAt',
      header: 'Submitted',
      cell: ({ row }) =>
        row.original.submittedAt
          ? format(new Date(row.original.submittedAt), 'MMM dd, yyyy')
          : '—',
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const declaration = row.original
        return (
          <div className="flex items-center gap-2">
            {declaration.status === 'draft' && (
              <>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleEdit(declaration)}
                  title="Edit"
                >
                  <Edit className="w-4 h-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleSubmit(declaration)}
                  title="Submit"
                >
                  <Send className="w-4 h-4" />
                </Button>
              </>
            )}
            {declaration.status === 'submitted' && (
              <>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleVerify(declaration)}
                  title="Verify"
                >
                  <CheckCircle className="w-4 h-4 text-green-600" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleReject(declaration)}
                  title="Reject"
                >
                  <XCircle className="w-4 h-4 text-orange-600" />
                </Button>
              </>
            )}
            {declaration.status === 'rejected' && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleRevise(declaration)}
                title="Revise & Resubmit"
              >
                <RotateCcw className="w-4 h-4 text-blue-600" />
              </Button>
            )}
            {(declaration.status === 'draft' || declaration.status === 'submitted') && (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => handleDelete(declaration)}
                title="Delete"
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            )}
          </div>
        )
      },
    },
  ]

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/payroll')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Back to Dashboard
        </Button>
      </div>
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Tax Declarations</h1>
          <p className="text-gray-600 mt-1">Manage employee tax declarations and investment proofs</p>
        </div>
        <div className="flex gap-3">
          <CompanyFilterDropdown
            value={urlState.companyId || ''}
            onChange={(value) => setUrlState({ companyId: value || null, page: 1 })}
          />
          <Button onClick={() => setIsCreateDrawerOpen(true)}>
            <Plus className="w-4 h-4 mr-2" />
            Add Tax Declaration
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <p className="text-red-600">Failed to load tax declarations</p>
        </div>
      )}

      <DataTable
        columns={columns}
        data={data?.items || []}
        isLoading={isLoading}
        searchPlaceholder="Search by employee name, financial year..."
        onSearch={(value: string) => setUrlState({ searchTerm: value || null, page: 1 })}
        pagination={{
          pageIndex: (data?.pageNumber || urlState.page) - 1,
          pageSize: data?.pageSize || urlState.pageSize,
          totalCount: data?.totalCount || 0,
          onPageChange: (page) => setUrlState({ page: page + 1 }),
          onPageSizeChange: (size) => setUrlState({ pageSize: size, page: 1 }),
        }}
        footerInfo={() => {
          return `${data?.totalCount || 0} tax declarations • Page ${data?.pageNumber || urlState.page} of ${data?.totalPages || 1}`
        }}
      />

      {/* Create/Edit Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen || !!editingDeclaration}
        onClose={() => {
          setIsCreateDrawerOpen(false)
          setEditingDeclaration(null)
        }}
        title={editingDeclaration ? 'Edit Tax Declaration' : 'Add Tax Declaration'}
      >
        <TaxDeclarationForm
          declaration={editingDeclaration || undefined}
          onSuccess={() => {
            setIsCreateDrawerOpen(false)
            setEditingDeclaration(null)
          }}
          onCancel={() => {
            setIsCreateDrawerOpen(false)
            setEditingDeclaration(null)
          }}
        />
      </Drawer>

      {/* Delete Modal */}
      <Modal
        isOpen={!!deletingDeclaration}
        onClose={() => setDeletingDeclaration(null)}
        title="Delete Tax Declaration"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to delete the tax declaration for{' '}
            <span className="font-semibold">{deletingDeclaration?.employeeName}</span>?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setDeletingDeclaration(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleDeleteConfirm}
              disabled={deleteTaxDeclaration.isPending}
            >
              {deleteTaxDeclaration.isPending ? 'Deleting...' : 'Delete'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Submit Modal */}
      <Modal
        isOpen={!!submittingDeclaration}
        onClose={() => setSubmittingDeclaration(null)}
        title="Submit Tax Declaration"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to submit the tax declaration for{' '}
            <span className="font-semibold">{submittingDeclaration?.employeeName}</span>?
            Once submitted, it cannot be edited without verification.
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setSubmittingDeclaration(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleSubmitConfirm}
              disabled={submitTaxDeclaration.isPending}
            >
              {submitTaxDeclaration.isPending ? 'Submitting...' : 'Submit'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Verify Modal */}
      <Modal
        isOpen={!!verifyingDeclaration}
        onClose={() => setVerifyingDeclaration(null)}
        title="Verify Tax Declaration"
      >
        <div className="space-y-4">
          <p>
            Are you sure you want to verify the tax declaration for{' '}
            <span className="font-semibold">{verifyingDeclaration?.employeeName}</span>?
          </p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setVerifyingDeclaration(null)}>
              Cancel
            </Button>
            <Button
              onClick={handleVerifyConfirm}
              disabled={verifyTaxDeclaration.isPending}
            >
              {verifyTaxDeclaration.isPending ? 'Verifying...' : 'Verify'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Reject Modal */}
      <Modal
        isOpen={!!rejectingDeclaration}
        onClose={() => {
          setRejectingDeclaration(null)
          setRejectionReason('')
        }}
        title="Reject Tax Declaration"
      >
        <div className="space-y-4">
          <p>
            You are rejecting the tax declaration for{' '}
            <span className="font-semibold">{rejectingDeclaration?.employeeName}</span>.
            Please provide a reason for rejection.
          </p>
          <div className="space-y-2">
            <Label htmlFor="rejectionReason">Rejection Reason *</Label>
            <Textarea
              id="rejectionReason"
              value={rejectionReason}
              onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setRejectionReason(e.target.value)}
              placeholder="Enter the reason for rejection (e.g., missing documents, incorrect values, etc.)"
              rows={4}
            />
          </div>
          <div className="flex justify-end gap-3">
            <Button
              variant="outline"
              onClick={() => {
                setRejectingDeclaration(null)
                setRejectionReason('')
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={handleRejectConfirm}
              disabled={rejectTaxDeclaration.isPending || !rejectionReason.trim()}
              className="bg-orange-600 hover:bg-orange-700"
            >
              {rejectTaxDeclaration.isPending ? 'Rejecting...' : 'Reject'}
            </Button>
          </div>
        </div>
      </Modal>

      {/* Revise Drawer */}
      <Drawer
        isOpen={!!revisingDeclaration}
        onClose={() => setRevisingDeclaration(null)}
        title="Revise Tax Declaration"
      >
        {revisingDeclaration && (
          <div className="space-y-4">
            {revisingDeclaration.rejectionReason && (
              <div className="bg-orange-50 border border-orange-200 rounded-lg p-4">
                <div className="flex items-start gap-2">
                  <AlertCircle className="w-5 h-5 text-orange-600 mt-0.5" />
                  <div>
                    <p className="text-sm font-medium text-orange-800">Rejection Reason</p>
                    <p className="text-sm text-orange-700 mt-1">{revisingDeclaration.rejectionReason}</p>
                  </div>
                </div>
              </div>
            )}
            <TaxDeclarationForm
              declaration={revisingDeclaration}
              isRevision={true}
              onSuccess={() => setRevisingDeclaration(null)}
              onCancel={() => setRevisingDeclaration(null)}
            />
          </div>
        )}
      </Drawer>
    </div>
  )
}

export default EmployeeTaxDeclarations




