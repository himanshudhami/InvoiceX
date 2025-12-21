import { useState, useMemo } from 'react'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useFircsPaged,
  useCreateFirc,
  useUpdateFirc,
  useDeleteFirc,
  useLinkFircToPayment,
  useMarkEdpmsReported,
  useAutoMatchFircs,
  useEdpmsComplianceSummary,
} from '@/hooks/api/useFircs'
import { useCompanies } from '@/hooks/api/useCompanies'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { DataTable } from '@/components/ui/DataTable'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ColumnDef } from '@tanstack/react-table'
import { FircTracking, CreateFircDto } from '@/services/api/types'
import {
  Plus,
  MoreHorizontal,
  Pencil,
  Trash2,
  Link as LinkIcon,
  CheckCircle,
  AlertCircle,
  FileText,
  RefreshCw,
} from 'lucide-react'
import toast from 'react-hot-toast'

const FircManagement = () => {
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingFirc, setEditingFirc] = useState<FircTracking | null>(null)
  const [deletingFirc, setDeletingFirc] = useState<FircTracking | null>(null)

  // URL-backed filter state
  const [urlState, setUrlState] = useQueryStates(
    {
      company: parseAsString.withDefault(''),
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(10),
      search: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const { data: companies = [] } = useCompanies()
  const selectedCompanyId = urlState.company || companies[0]?.id || ''

  // Fetch FIRCs
  const { data: fircsData, isLoading } = useFircsPaged({
    companyId: selectedCompanyId,
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: urlState.search || undefined,
  })

  // EDPMS compliance summary
  const { data: edpmsSummary } = useEdpmsComplianceSummary(selectedCompanyId, !!selectedCompanyId)

  // Mutations
  const createFirc = useCreateFirc()
  const updateFirc = useUpdateFirc()
  const deleteFirc = useDeleteFirc()
  const markEdpmsReported = useMarkEdpmsReported()
  const autoMatch = useAutoMatchFircs()

  // Form state for create/edit
  const [formData, setFormData] = useState<Partial<CreateFircDto>>({})

  const handleCreate = async () => {
    if (!formData.fircNumber || !formData.fircDate || !formData.bankName) {
      toast.error('Please fill in all required fields')
      return
    }

    try {
      await createFirc.mutateAsync({
        companyId: selectedCompanyId,
        fircNumber: formData.fircNumber,
        fircDate: formData.fircDate,
        bankName: formData.bankName,
        purposeCode: formData.purposeCode,
        foreignCurrency: formData.foreignCurrency || 'USD',
        foreignAmount: formData.foreignAmount || 0,
        inrAmount: formData.inrAmount || 0,
        exchangeRate: formData.exchangeRate || 0,
        beneficiaryName: formData.beneficiaryName,
        remitterName: formData.remitterName,
        remitterCountry: formData.remitterCountry,
      })
      toast.success('FIRC created successfully')
      setIsCreateDialogOpen(false)
      setFormData({})
    } catch (error) {
      toast.error('Failed to create FIRC')
    }
  }

  const handleUpdate = async () => {
    if (!editingFirc) return

    try {
      await updateFirc.mutateAsync({
        id: editingFirc.id,
        data: formData,
      })
      toast.success('FIRC updated successfully')
      setEditingFirc(null)
      setFormData({})
    } catch (error) {
      toast.error('Failed to update FIRC')
    }
  }

  const handleDelete = async () => {
    if (!deletingFirc) return

    try {
      await deleteFirc.mutateAsync(deletingFirc.id)
      toast.success('FIRC deleted successfully')
      setDeletingFirc(null)
    } catch (error) {
      toast.error('Failed to delete FIRC')
    }
  }

  const handleMarkEdpms = async (firc: FircTracking) => {
    try {
      await markEdpmsReported.mutateAsync({ fircId: firc.id })
      toast.success('Marked as EDPMS reported')
    } catch (error) {
      toast.error('Failed to mark as EDPMS reported')
    }
  }

  const handleAutoMatch = async () => {
    try {
      const results = await autoMatch.mutateAsync(selectedCompanyId)
      toast.success(`Auto-matched ${results.length} FIRCs`)
    } catch (error) {
      toast.error('Failed to auto-match FIRCs')
    }
  }

  // Format helpers
  const formatCurrency = (amount: number, currency: string = 'USD') => {
    if (currency === 'INR') {
      return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(amount)
    }
    return new Intl.NumberFormat('en-US', { style: 'currency', currency, maximumFractionDigits: 2 }).format(amount)
  }

  // Table columns
  const columns: ColumnDef<FircTracking>[] = [
    {
      accessorKey: 'fircNumber',
      header: 'FIRC Number',
      cell: ({ row }) => (
        <div className="font-medium">{row.original.fircNumber}</div>
      ),
    },
    {
      accessorKey: 'fircDate',
      header: 'Date',
      cell: ({ row }) => new Date(row.original.fircDate).toLocaleDateString(),
    },
    {
      accessorKey: 'bankName',
      header: 'Bank',
    },
    {
      accessorKey: 'foreignAmount',
      header: 'Amount',
      cell: ({ row }) => (
        <div className="text-right font-medium">
          {formatCurrency(row.original.foreignAmount, row.original.foreignCurrency)}
        </div>
      ),
    },
    {
      accessorKey: 'inrAmount',
      header: 'INR Value',
      cell: ({ row }) => (
        <div className="text-right">
          {formatCurrency(row.original.inrAmount, 'INR')}
        </div>
      ),
    },
    {
      accessorKey: 'exchangeRate',
      header: 'Rate',
      cell: ({ row }) => row.original.exchangeRate.toFixed(4),
    },
    {
      accessorKey: 'remitterName',
      header: 'Remitter',
      cell: ({ row }) => (
        <div className="max-w-[150px] truncate">
          {row.original.remitterName || '-'}
        </div>
      ),
    },
    {
      accessorKey: 'paymentId',
      header: 'Linked',
      cell: ({ row }) => (
        row.original.paymentId ? (
          <Badge variant="default" className="gap-1">
            <LinkIcon className="h-3 w-3" />
            Linked
          </Badge>
        ) : (
          <Badge variant="outline">Unlinked</Badge>
        )
      ),
    },
    {
      accessorKey: 'edpmsReported',
      header: 'EDPMS',
      cell: ({ row }) => (
        row.original.edpmsReported ? (
          <Badge variant="default" className="gap-1 bg-green-600">
            <CheckCircle className="h-3 w-3" />
            Reported
          </Badge>
        ) : (
          <Badge variant="secondary" className="gap-1">
            <AlertCircle className="h-3 w-3" />
            Pending
          </Badge>
        )
      ),
    },
    {
      id: 'actions',
      cell: ({ row }) => (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon">
              <MoreHorizontal className="h-4 w-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => {
              setEditingFirc(row.original)
              setFormData(row.original)
            }}>
              <Pencil className="h-4 w-4 mr-2" />
              Edit
            </DropdownMenuItem>
            {!row.original.edpmsReported && (
              <DropdownMenuItem onClick={() => handleMarkEdpms(row.original)}>
                <CheckCircle className="h-4 w-4 mr-2" />
                Mark EDPMS Reported
              </DropdownMenuItem>
            )}
            <DropdownMenuSeparator />
            <DropdownMenuItem
              className="text-destructive"
              onClick={() => setDeletingFirc(row.original)}
            >
              <Trash2 className="h-4 w-4 mr-2" />
              Delete
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      ),
    },
  ]

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold">FIRC Management</h1>
          <p className="text-muted-foreground">
            Track and reconcile Foreign Inward Remittance Certificates
          </p>
        </div>
        <div className="flex items-center gap-2">
          <CompanyFilterDropdown
            value={urlState.company}
            onChange={(value) => setUrlState({ company: value, page: 1 })}
            companies={companies}
          />
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Total FIRCs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{edpmsSummary?.totalFircs || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">EDPMS Reported</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">{edpmsSummary?.reported || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">EDPMS Pending</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-yellow-600">{edpmsSummary?.pending || 0}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Compliance Rate</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {(edpmsSummary?.complianceRate || 0).toFixed(1)}%
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Actions Bar */}
      <div className="flex flex-col sm:flex-row gap-4 justify-between">
        <Input
          placeholder="Search FIRCs..."
          value={urlState.search}
          onChange={(e) => setUrlState({ search: e.target.value, page: 1 })}
          className="max-w-sm"
        />
        <div className="flex gap-2">
          <Button variant="outline" onClick={handleAutoMatch} disabled={autoMatch.isPending}>
            <RefreshCw className={`h-4 w-4 mr-2 ${autoMatch.isPending ? 'animate-spin' : ''}`} />
            Auto-Match
          </Button>
          <Button onClick={() => setIsCreateDialogOpen(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Add FIRC
          </Button>
        </div>
      </div>

      {/* Data Table */}
      <Card>
        <CardContent className="pt-6">
          <DataTable
            columns={columns}
            data={fircsData?.items || []}
            isLoading={isLoading}
            pageCount={fircsData?.totalPages || 1}
            pageIndex={urlState.page - 1}
            pageSize={urlState.pageSize}
            onPageChange={(page) => setUrlState({ page: page + 1 })}
            onPageSizeChange={(size) => setUrlState({ pageSize: size, page: 1 })}
          />
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <Dialog
        open={isCreateDialogOpen || !!editingFirc}
        onOpenChange={(open) => {
          if (!open) {
            setIsCreateDialogOpen(false)
            setEditingFirc(null)
            setFormData({})
          }
        }}
      >
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{editingFirc ? 'Edit FIRC' : 'Add New FIRC'}</DialogTitle>
            <DialogDescription>
              Enter the details from the Foreign Inward Remittance Certificate
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="fircNumber">FIRC Number *</Label>
                <Input
                  id="fircNumber"
                  value={formData.fircNumber || ''}
                  onChange={(e) => setFormData({ ...formData, fircNumber: e.target.value })}
                  placeholder="FIRC123456"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="fircDate">FIRC Date *</Label>
                <Input
                  id="fircDate"
                  type="date"
                  value={formData.fircDate || ''}
                  onChange={(e) => setFormData({ ...formData, fircDate: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="bankName">Bank Name *</Label>
                <Select
                  value={formData.bankName || ''}
                  onValueChange={(value) => setFormData({ ...formData, bankName: value })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select bank" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="HDFC Bank">HDFC Bank</SelectItem>
                    <SelectItem value="ICICI Bank">ICICI Bank</SelectItem>
                    <SelectItem value="Axis Bank">Axis Bank</SelectItem>
                    <SelectItem value="SBI">State Bank of India</SelectItem>
                    <SelectItem value="Kotak Mahindra Bank">Kotak Mahindra Bank</SelectItem>
                    <SelectItem value="Yes Bank">Yes Bank</SelectItem>
                    <SelectItem value="Other">Other</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="purposeCode">Purpose Code</Label>
                <Input
                  id="purposeCode"
                  value={formData.purposeCode || ''}
                  onChange={(e) => setFormData({ ...formData, purposeCode: e.target.value })}
                  placeholder="P0802"
                />
              </div>
            </div>

            <div className="grid grid-cols-3 gap-4">
              <div className="space-y-2">
                <Label htmlFor="foreignCurrency">Currency</Label>
                <Select
                  value={formData.foreignCurrency || 'USD'}
                  onValueChange={(value) => setFormData({ ...formData, foreignCurrency: value })}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="USD">USD</SelectItem>
                    <SelectItem value="EUR">EUR</SelectItem>
                    <SelectItem value="GBP">GBP</SelectItem>
                    <SelectItem value="AUD">AUD</SelectItem>
                    <SelectItem value="CAD">CAD</SelectItem>
                    <SelectItem value="SGD">SGD</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="foreignAmount">Foreign Amount</Label>
                <Input
                  id="foreignAmount"
                  type="number"
                  step="0.01"
                  value={formData.foreignAmount || ''}
                  onChange={(e) => setFormData({ ...formData, foreignAmount: parseFloat(e.target.value) || 0 })}
                  placeholder="10000.00"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="exchangeRate">Exchange Rate</Label>
                <Input
                  id="exchangeRate"
                  type="number"
                  step="0.0001"
                  value={formData.exchangeRate || ''}
                  onChange={(e) => {
                    const rate = parseFloat(e.target.value) || 0
                    const foreignAmount = formData.foreignAmount || 0
                    setFormData({
                      ...formData,
                      exchangeRate: rate,
                      inrAmount: foreignAmount * rate,
                    })
                  }}
                  placeholder="83.5000"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="inrAmount">INR Amount</Label>
              <Input
                id="inrAmount"
                type="number"
                step="0.01"
                value={formData.inrAmount || ''}
                onChange={(e) => setFormData({ ...formData, inrAmount: parseFloat(e.target.value) || 0 })}
                placeholder="835000.00"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="remitterName">Remitter Name</Label>
                <Input
                  id="remitterName"
                  value={formData.remitterName || ''}
                  onChange={(e) => setFormData({ ...formData, remitterName: e.target.value })}
                  placeholder="Customer Company Inc."
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="remitterCountry">Remitter Country</Label>
                <Input
                  id="remitterCountry"
                  value={formData.remitterCountry || ''}
                  onChange={(e) => setFormData({ ...formData, remitterCountry: e.target.value })}
                  placeholder="United States"
                />
              </div>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setIsCreateDialogOpen(false)
                setEditingFirc(null)
                setFormData({})
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={editingFirc ? handleUpdate : handleCreate}
              disabled={createFirc.isPending || updateFirc.isPending}
            >
              {editingFirc ? 'Update' : 'Create'} FIRC
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deletingFirc} onOpenChange={() => setDeletingFirc(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete FIRC</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete FIRC {deletingFirc?.fircNumber}? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeletingFirc(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteFirc.isPending}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default FircManagement
