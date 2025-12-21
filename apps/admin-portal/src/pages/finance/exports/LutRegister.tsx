import { useState } from 'react'
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs'
import {
  useLutsByCompany,
  useActiveLut,
  useCreateLut,
  useUpdateLut,
  useDeleteLut,
  useRenewLut,
  useLutComplianceSummary,
  useLutExpiryAlerts,
} from '@/hooks/api/useLuts'
import { useCompanies } from '@/hooks/api/useCompanies'
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { LutRegister as LutRegisterType, CreateLutDto } from '@/services/api/types'
import {
  Plus,
  MoreHorizontal,
  Pencil,
  Trash2,
  RefreshCw,
  CheckCircle,
  AlertTriangle,
  Clock,
  FileText,
  Shield,
} from 'lucide-react'
import toast from 'react-hot-toast'

const LutRegister = () => {
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false)
  const [editingLut, setEditingLut] = useState<LutRegisterType | null>(null)
  const [deletingLut, setDeletingLut] = useState<LutRegisterType | null>(null)
  const [renewingLut, setRenewingLut] = useState<LutRegisterType | null>(null)

  // URL-backed filter state
  const [urlState, setUrlState] = useQueryStates(
    {
      company: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const { data: companies = [] } = useCompanies()
  const selectedCompanyId = urlState.company || companies[0]?.id || ''
  const selectedCompany = companies.find(c => c.id === selectedCompanyId)

  // Fetch LUT data
  const { data: luts = [], isLoading } = useLutsByCompany(selectedCompanyId, !!selectedCompanyId)
  const { data: activeLut, isLoading: isLoadingActive } = useActiveLut(selectedCompanyId, !!selectedCompanyId)
  const { data: complianceSummary } = useLutComplianceSummary(selectedCompanyId, !!selectedCompanyId)
  const { data: expiryAlerts = [] } = useLutExpiryAlerts(selectedCompanyId, !!selectedCompanyId)

  // Mutations
  const createLut = useCreateLut()
  const updateLut = useUpdateLut()
  const deleteLut = useDeleteLut()
  const renewLut = useRenewLut()

  // Form state
  const [formData, setFormData] = useState<Partial<CreateLutDto>>({})

  // Get current financial year
  const getCurrentFY = () => {
    const now = new Date()
    const year = now.getMonth() >= 3 ? now.getFullYear() : now.getFullYear() - 1
    return `${year}-${(year + 1) % 100}`
  }

  const handleCreate = async () => {
    if (!formData.lutNumber || !formData.validFrom || !formData.validTo) {
      toast.error('Please fill in all required fields')
      return
    }

    try {
      await createLut.mutateAsync({
        companyId: selectedCompanyId,
        lutNumber: formData.lutNumber,
        financialYear: formData.financialYear || getCurrentFY(),
        validFrom: formData.validFrom,
        validTo: formData.validTo,
        gstin: formData.gstin || selectedCompany?.gstin || '',
        filingDate: formData.filingDate,
        arn: formData.arn,
      })
      toast.success('LUT registered successfully')
      setIsCreateDialogOpen(false)
      setFormData({})
    } catch (error) {
      toast.error('Failed to register LUT')
    }
  }

  const handleUpdate = async () => {
    if (!editingLut) return

    try {
      await updateLut.mutateAsync({
        id: editingLut.id,
        data: formData,
      })
      toast.success('LUT updated successfully')
      setEditingLut(null)
      setFormData({})
    } catch (error) {
      toast.error('Failed to update LUT')
    }
  }

  const handleDelete = async () => {
    if (!deletingLut) return

    try {
      await deleteLut.mutateAsync(deletingLut.id)
      toast.success('LUT deleted successfully')
      setDeletingLut(null)
    } catch (error) {
      toast.error('Failed to delete LUT')
    }
  }

  const handleRenew = async () => {
    if (!renewingLut || !formData.lutNumber || !formData.validFrom || !formData.validTo) {
      toast.error('Please fill in all required fields')
      return
    }

    try {
      await renewLut.mutateAsync({
        lutId: renewingLut.id,
        newLutData: {
          companyId: selectedCompanyId,
          lutNumber: formData.lutNumber,
          financialYear: formData.financialYear || getCurrentFY(),
          validFrom: formData.validFrom,
          validTo: formData.validTo,
          gstin: formData.gstin || selectedCompany?.gstin || '',
          filingDate: formData.filingDate,
          arn: formData.arn,
        },
      })
      toast.success('LUT renewed successfully')
      setRenewingLut(null)
      setFormData({})
    } catch (error) {
      toast.error('Failed to renew LUT')
    }
  }

  // Calculate days to expiry
  const getDaysToExpiry = (validTo: string) => {
    const today = new Date()
    const expiry = new Date(validTo)
    const diff = Math.ceil((expiry.getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
    return diff
  }

  const getStatusBadge = (status: string, validTo?: string) => {
    if (status === 'active' && validTo) {
      const days = getDaysToExpiry(validTo)
      if (days < 0) {
        return <Badge variant="destructive">Expired</Badge>
      }
      if (days <= 30) {
        return <Badge variant="secondary" className="bg-yellow-100 text-yellow-800">Expiring Soon</Badge>
      }
      return <Badge variant="default" className="bg-green-100 text-green-800">Active</Badge>
    }
    if (status === 'expired') {
      return <Badge variant="destructive">Expired</Badge>
    }
    if (status === 'superseded') {
      return <Badge variant="outline">Superseded</Badge>
    }
    return <Badge variant="outline">{status}</Badge>
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-2xl font-bold">LUT Register</h1>
          <p className="text-muted-foreground">
            Manage Letters of Undertaking for zero-rated GST exports
          </p>
        </div>
        <div className="flex items-center gap-2">
          <CompanyFilterDropdown
            value={urlState.company}
            onChange={(value) => setUrlState({ company: value })}
            companies={companies}
          />
        </div>
      </div>

      {/* Active LUT Card */}
      <Card className={activeLut ? 'border-green-200 bg-green-50/50 dark:bg-green-950/20' : 'border-red-200 bg-red-50/50 dark:bg-red-950/20'}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <CardTitle className="text-lg flex items-center gap-2">
              {activeLut ? (
                <CheckCircle className="h-5 w-5 text-green-600" />
              ) : (
                <AlertTriangle className="h-5 w-5 text-red-600" />
              )}
              Current LUT Status
            </CardTitle>
            {!activeLut && (
              <Button size="sm" onClick={() => setIsCreateDialogOpen(true)}>
                <Plus className="h-4 w-4 mr-2" />
                Register LUT
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          {isLoadingActive ? (
            <Skeleton className="h-20 w-full" />
          ) : activeLut ? (
            <div className="grid gap-4 md:grid-cols-4">
              <div>
                <p className="text-sm text-muted-foreground">LUT Number</p>
                <p className="font-semibold">{activeLut.lutNumber}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Financial Year</p>
                <p className="font-semibold">{activeLut.financialYear}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Valid Until</p>
                <p className="font-semibold">{new Date(activeLut.validTo).toLocaleDateString()}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Days Remaining</p>
                <p className={`font-semibold ${getDaysToExpiry(activeLut.validTo) <= 30 ? 'text-yellow-600' : 'text-green-600'}`}>
                  {getDaysToExpiry(activeLut.validTo)} days
                </p>
              </div>
            </div>
          ) : (
            <div className="text-center py-4">
              <p className="text-red-600 font-medium">No Active LUT Found</p>
              <p className="text-sm text-muted-foreground mt-1">
                Export invoices may not be GST compliant without a valid LUT
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Expiry Alerts */}
      {expiryAlerts.length > 0 && (
        <Card className="border-yellow-200">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm flex items-center gap-2">
              <Clock className="h-4 w-4 text-yellow-600" />
              Expiry Alerts
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {expiryAlerts.map((alert, index) => (
                <div
                  key={index}
                  className={`flex items-center justify-between p-3 rounded-lg ${
                    alert.severity === 'critical' ? 'bg-red-50 dark:bg-red-950' :
                    alert.severity === 'warning' ? 'bg-yellow-50 dark:bg-yellow-950' :
                    'bg-blue-50 dark:bg-blue-950'
                  }`}
                >
                  <div>
                    <p className="font-medium text-sm">{alert.lutNumber}</p>
                    <p className="text-xs text-muted-foreground">{alert.message}</p>
                  </div>
                  <Badge variant={alert.severity === 'critical' ? 'destructive' : 'secondary'}>
                    {alert.daysToExpiry} days
                  </Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Total LUTs</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{complianceSummary?.totalLuts || luts.length}</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Active LUT</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${complianceSummary?.hasActiveLut ? 'text-green-600' : 'text-red-600'}`}>
              {complianceSummary?.hasActiveLut ? 'Yes' : 'No'}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Days to Expiry</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${
              (complianceSummary?.daysToExpiry || 0) <= 30 ? 'text-yellow-600' : ''
            }`}>
              {complianceSummary?.daysToExpiry ?? '-'}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium">Invoices w/o LUT</CardTitle>
          </CardHeader>
          <CardContent>
            <div className={`text-2xl font-bold ${
              (complianceSummary?.exportInvoicesWithoutLut || 0) > 0 ? 'text-red-600' : ''
            }`}>
              {complianceSummary?.exportInvoicesWithoutLut || 0}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Actions */}
      <div className="flex justify-end">
        <Button onClick={() => setIsCreateDialogOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Register New LUT
        </Button>
      </div>

      {/* LUT History Table */}
      <Card>
        <CardHeader>
          <CardTitle>LUT History</CardTitle>
          <CardDescription>All registered Letters of Undertaking</CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : luts.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              No LUTs registered yet
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>LUT Number</TableHead>
                  <TableHead>Financial Year</TableHead>
                  <TableHead>Valid From</TableHead>
                  <TableHead>Valid To</TableHead>
                  <TableHead>ARN</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-[50px]"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {luts.map((lut) => (
                  <TableRow key={lut.id}>
                    <TableCell className="font-medium">{lut.lutNumber}</TableCell>
                    <TableCell>{lut.financialYear}</TableCell>
                    <TableCell>{new Date(lut.validFrom).toLocaleDateString()}</TableCell>
                    <TableCell>{new Date(lut.validTo).toLocaleDateString()}</TableCell>
                    <TableCell>{lut.arn || '-'}</TableCell>
                    <TableCell>{getStatusBadge(lut.status, lut.validTo)}</TableCell>
                    <TableCell>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" size="icon">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem onClick={() => {
                            setEditingLut(lut)
                            setFormData(lut)
                          }}>
                            <Pencil className="h-4 w-4 mr-2" />
                            Edit
                          </DropdownMenuItem>
                          {lut.status === 'active' && (
                            <DropdownMenuItem onClick={() => {
                              setRenewingLut(lut)
                              setFormData({
                                financialYear: getCurrentFY(),
                                gstin: lut.gstin,
                              })
                            }}>
                              <RefreshCw className="h-4 w-4 mr-2" />
                              Renew
                            </DropdownMenuItem>
                          )}
                          <DropdownMenuSeparator />
                          <DropdownMenuItem
                            className="text-destructive"
                            onClick={() => setDeletingLut(lut)}
                          >
                            <Trash2 className="h-4 w-4 mr-2" />
                            Delete
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit/Renew Dialog */}
      <Dialog
        open={isCreateDialogOpen || !!editingLut || !!renewingLut}
        onOpenChange={(open) => {
          if (!open) {
            setIsCreateDialogOpen(false)
            setEditingLut(null)
            setRenewingLut(null)
            setFormData({})
          }
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {renewingLut ? 'Renew LUT' : editingLut ? 'Edit LUT' : 'Register New LUT'}
            </DialogTitle>
            <DialogDescription>
              {renewingLut
                ? `Create a new LUT to replace ${renewingLut.lutNumber}`
                : 'Enter the LUT details from the GST portal'}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="lutNumber">LUT Number *</Label>
                <Input
                  id="lutNumber"
                  value={formData.lutNumber || ''}
                  onChange={(e) => setFormData({ ...formData, lutNumber: e.target.value })}
                  placeholder="AD123456789"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="financialYear">Financial Year</Label>
                <Input
                  id="financialYear"
                  value={formData.financialYear || getCurrentFY()}
                  onChange={(e) => setFormData({ ...formData, financialYear: e.target.value })}
                  placeholder="2025-26"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="validFrom">Valid From *</Label>
                <Input
                  id="validFrom"
                  type="date"
                  value={formData.validFrom || ''}
                  onChange={(e) => setFormData({ ...formData, validFrom: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="validTo">Valid To *</Label>
                <Input
                  id="validTo"
                  type="date"
                  value={formData.validTo || ''}
                  onChange={(e) => setFormData({ ...formData, validTo: e.target.value })}
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="gstin">GSTIN</Label>
                <Input
                  id="gstin"
                  value={formData.gstin || selectedCompany?.gstin || ''}
                  onChange={(e) => setFormData({ ...formData, gstin: e.target.value })}
                  placeholder="22AAAAA0000A1Z5"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="arn">ARN (Application Reference)</Label>
                <Input
                  id="arn"
                  value={formData.arn || ''}
                  onChange={(e) => setFormData({ ...formData, arn: e.target.value })}
                  placeholder="AA220425000001Z"
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="filingDate">Filing Date</Label>
              <Input
                id="filingDate"
                type="date"
                value={formData.filingDate || ''}
                onChange={(e) => setFormData({ ...formData, filingDate: e.target.value })}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => {
                setIsCreateDialogOpen(false)
                setEditingLut(null)
                setRenewingLut(null)
                setFormData({})
              }}
            >
              Cancel
            </Button>
            <Button
              onClick={renewingLut ? handleRenew : editingLut ? handleUpdate : handleCreate}
              disabled={createLut.isPending || updateLut.isPending || renewLut.isPending}
            >
              {renewingLut ? 'Renew' : editingLut ? 'Update' : 'Register'} LUT
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deletingLut} onOpenChange={() => setDeletingLut(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete LUT</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete LUT {deletingLut?.lutNumber}? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeletingLut(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleteLut.isPending}
            >
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export default LutRegister
