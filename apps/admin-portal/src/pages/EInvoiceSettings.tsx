import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { eInvoiceService } from '@/services/api/eInvoiceService';
import { EInvoiceCredentials, SaveEInvoiceCredentialsDto, EInvoiceAuditLog } from '@/services/api/types';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Switch } from '@/components/ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Loader2, Save, Trash2, AlertCircle, CheckCircle, XCircle, Clock, RefreshCw } from 'lucide-react';
import toast from 'react-hot-toast';

const GSP_PROVIDERS = [
  { value: 'cleartax', label: 'ClearTax (IRP 4)', description: 'Enterprise-grade, comprehensive API' },
  { value: 'iris', label: 'IRIS (IRP 6)', description: 'Free tier available, good for startups' },
  { value: 'nic_direct', label: 'NIC Direct', description: 'Direct government portal' },
];

const ENVIRONMENTS = [
  { value: 'sandbox', label: 'Sandbox (Testing)' },
  { value: 'production', label: 'Production' },
];

export default function EInvoiceSettings() {
  const { selectedCompanyId } = useCompanyContext();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState('credentials');
  const [isEditing, setIsEditing] = useState(false);
  const [formData, setFormData] = useState<Partial<SaveEInvoiceCredentialsDto>>({
    gspProvider: 'cleartax',
    environment: 'sandbox',
    autoGenerateIrn: false,
    autoCancelOnVoid: false,
    generateEwayBill: false,
    einvoiceThreshold: 50000000,
    isActive: true,
  });

  // Fetch credentials
  const { data: credentials, isLoading: credentialsLoading } = useQuery({
    queryKey: ['einvoice-credentials', selectedCompanyId],
    queryFn: () => eInvoiceService.getCredentials(selectedCompanyId!),
    enabled: !!selectedCompanyId,
  });

  // Fetch audit log
  const { data: auditLog, isLoading: auditLoading, refetch: refetchAudit } = useQuery({
    queryKey: ['einvoice-audit', selectedCompanyId],
    queryFn: () => eInvoiceService.getAuditLog(selectedCompanyId!, { pageSize: 50 }),
    enabled: !!selectedCompanyId && activeTab === 'audit',
  });

  // Fetch errors
  const { data: errors, isLoading: errorsLoading, refetch: refetchErrors } = useQuery({
    queryKey: ['einvoice-errors', selectedCompanyId],
    queryFn: () => eInvoiceService.getErrors(selectedCompanyId!),
    enabled: !!selectedCompanyId && activeTab === 'errors',
  });

  // Fetch queue
  const { data: queue, isLoading: queueLoading, refetch: refetchQueue } = useQuery({
    queryKey: ['einvoice-queue', selectedCompanyId],
    queryFn: () => eInvoiceService.getQueueStatus(selectedCompanyId!),
    enabled: !!selectedCompanyId && activeTab === 'queue',
  });

  // Save credentials mutation
  const saveMutation = useMutation({
    mutationFn: (data: SaveEInvoiceCredentialsDto) => eInvoiceService.saveCredentials(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['einvoice-credentials'] });
      toast.success('Credentials saved successfully');
      setIsEditing(false);
    },
    onError: (error: Error) => {
      toast.error(`Failed to save: ${error.message}`);
    },
  });

  // Delete credentials mutation
  const deleteMutation = useMutation({
    mutationFn: (id: string) => eInvoiceService.deleteCredentials(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['einvoice-credentials'] });
      toast.success('Credentials deleted');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete: ${error.message}`);
    },
  });

  const handleSave = () => {
    if (!selectedCompanyId) return;
    saveMutation.mutate({
      ...formData,
      companyId: selectedCompanyId,
    } as SaveEInvoiceCredentialsDto);
  };

  const handleEdit = (cred: EInvoiceCredentials) => {
    setFormData({
      gspProvider: cred.gspProvider,
      environment: cred.environment,
      clientId: cred.clientId,
      username: cred.username,
      autoGenerateIrn: cred.autoGenerateIrn,
      autoCancelOnVoid: cred.autoCancelOnVoid,
      generateEwayBill: cred.generateEwayBill,
      einvoiceThreshold: cred.einvoiceThreshold,
      isActive: cred.isActive,
    });
    setIsEditing(true);
  };

  const formatDate = (dateStr?: string) => {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleString('en-IN');
  };

  const getStatusBadge = (status?: string) => {
    switch (status) {
      case 'success':
        return <Badge className="bg-green-100 text-green-800"><CheckCircle className="h-3 w-3 mr-1" />Success</Badge>;
      case 'error':
        return <Badge className="bg-red-100 text-red-800"><XCircle className="h-3 w-3 mr-1" />Error</Badge>;
      case 'pending':
        return <Badge className="bg-yellow-100 text-yellow-800"><Clock className="h-3 w-3 mr-1" />Pending</Badge>;
      default:
        return <Badge variant="outline">{status}</Badge>;
    }
  };

  if (!selectedCompanyId) {
    return (
      <div className="p-6">
        <Alert>
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>No Company Selected</AlertTitle>
          <AlertDescription>Please select a company to manage e-invoice settings.</AlertDescription>
        </Alert>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">E-Invoice Settings</h1>
        <p className="text-gray-500 dark:text-gray-400">
          Configure GSP credentials and manage e-invoice integration for GST compliance
        </p>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="credentials">API Credentials</TabsTrigger>
          <TabsTrigger value="audit">Audit Log</TabsTrigger>
          <TabsTrigger value="errors">Errors</TabsTrigger>
          <TabsTrigger value="queue">Queue</TabsTrigger>
        </TabsList>

        <TabsContent value="credentials" className="space-y-4">
          {/* Existing Credentials */}
          {credentials && credentials.length > 0 && !isEditing && (
            <Card>
              <CardHeader>
                <CardTitle>Configured Credentials</CardTitle>
                <CardDescription>Your GSP API credentials for e-invoice generation</CardDescription>
              </CardHeader>
              <CardContent>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Provider</TableHead>
                      <TableHead>Environment</TableHead>
                      <TableHead>Username</TableHead>
                      <TableHead>Token Expiry</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Actions</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {credentials.map((cred) => (
                      <TableRow key={cred.id}>
                        <TableCell className="font-medium">{cred.gspProvider.toUpperCase()}</TableCell>
                        <TableCell>
                          <Badge variant={cred.environment === 'production' ? 'default' : 'secondary'}>
                            {cred.environment}
                          </Badge>
                        </TableCell>
                        <TableCell>{cred.username || '-'}</TableCell>
                        <TableCell>{formatDate(cred.tokenExpiry)}</TableCell>
                        <TableCell>
                          <Badge variant={cred.isActive ? 'default' : 'outline'}>
                            {cred.isActive ? 'Active' : 'Inactive'}
                          </Badge>
                        </TableCell>
                        <TableCell>
                          <div className="flex gap-2">
                            <Button variant="outline" size="sm" onClick={() => handleEdit(cred)}>
                              Edit
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => deleteMutation.mutate(cred.id)}
                              disabled={deleteMutation.isPending}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          )}

          {/* Add/Edit Form */}
          {(isEditing || !credentials || credentials.length === 0) && (
            <Card>
              <CardHeader>
                <CardTitle>{isEditing ? 'Edit Credentials' : 'Add GSP Credentials'}</CardTitle>
                <CardDescription>
                  Configure your GST Suvidha Provider (GSP) credentials for e-invoice integration
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <Label>GSP Provider</Label>
                    <Select
                      value={formData.gspProvider}
                      onValueChange={(v) => setFormData({ ...formData, gspProvider: v })}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select provider" />
                      </SelectTrigger>
                      <SelectContent>
                        {GSP_PROVIDERS.map((p) => (
                          <SelectItem key={p.value} value={p.value}>
                            <div>
                              <div className="font-medium">{p.label}</div>
                              <div className="text-xs text-gray-500">{p.description}</div>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Environment</Label>
                    <Select
                      value={formData.environment}
                      onValueChange={(v) => setFormData({ ...formData, environment: v })}
                    >
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {ENVIRONMENTS.map((e) => (
                          <SelectItem key={e.value} value={e.value}>
                            {e.label}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-2">
                    <Label>Client ID / App Key</Label>
                    <Input
                      value={formData.clientId || ''}
                      onChange={(e) => setFormData({ ...formData, clientId: e.target.value })}
                      placeholder="Your GSP client ID"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Client Secret / Auth Token</Label>
                    <Input
                      type="password"
                      value={formData.clientSecret || ''}
                      onChange={(e) => setFormData({ ...formData, clientSecret: e.target.value })}
                      placeholder="Your GSP secret"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Username (for IRP Auth)</Label>
                    <Input
                      value={formData.username || ''}
                      onChange={(e) => setFormData({ ...formData, username: e.target.value })}
                      placeholder="IRP portal username"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>Password (for IRP Auth)</Label>
                    <Input
                      type="password"
                      value={formData.password || ''}
                      onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                      placeholder="IRP portal password"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>E-Invoice Threshold (INR)</Label>
                    <Input
                      type="number"
                      value={formData.einvoiceThreshold || 50000000}
                      onChange={(e) => setFormData({ ...formData, einvoiceThreshold: Number(e.target.value) })}
                      placeholder="5 Cr default"
                    />
                    <p className="text-xs text-gray-500">Current threshold: 5 Cr (50,000,000)</p>
                  </div>
                </div>

                <div className="border-t pt-6 space-y-4">
                  <h4 className="font-medium">Automation Settings</h4>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <div className="font-medium">Auto-Generate IRN</div>
                        <div className="text-sm text-gray-500">Generate IRN on invoice finalize</div>
                      </div>
                      <Switch
                        checked={formData.autoGenerateIrn}
                        onCheckedChange={(v) => setFormData({ ...formData, autoGenerateIrn: v })}
                      />
                    </div>

                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <div className="font-medium">Auto-Cancel on Void</div>
                        <div className="text-sm text-gray-500">Cancel IRN when invoice voided</div>
                      </div>
                      <Switch
                        checked={formData.autoCancelOnVoid}
                        onCheckedChange={(v) => setFormData({ ...formData, autoCancelOnVoid: v })}
                      />
                    </div>

                    <div className="flex items-center justify-between p-3 border rounded-lg">
                      <div>
                        <div className="font-medium">Generate E-Way Bill</div>
                        <div className="text-sm text-gray-500">Create e-way bill with e-invoice</div>
                      </div>
                      <Switch
                        checked={formData.generateEwayBill}
                        onCheckedChange={(v) => setFormData({ ...formData, generateEwayBill: v })}
                      />
                    </div>
                  </div>
                </div>

                <div className="flex justify-end gap-2">
                  {isEditing && (
                    <Button variant="outline" onClick={() => setIsEditing(false)}>
                      Cancel
                    </Button>
                  )}
                  <Button onClick={handleSave} disabled={saveMutation.isPending}>
                    {saveMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                      <Save className="h-4 w-4 mr-2" />
                    )}
                    Save Credentials
                  </Button>
                </div>
              </CardContent>
            </Card>
          )}

          {!isEditing && credentials && credentials.length > 0 && (
            <Button onClick={() => setIsEditing(true)}>Add New Credentials</Button>
          )}
        </TabsContent>

        <TabsContent value="audit" className="space-y-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle>Audit Log</CardTitle>
                <CardDescription>E-invoice API activity history</CardDescription>
              </div>
              <Button variant="outline" size="sm" onClick={() => refetchAudit()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </CardHeader>
            <CardContent>
              {auditLoading ? (
                <div className="flex justify-center py-8">
                  <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Timestamp</TableHead>
                      <TableHead>Action</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>IRN</TableHead>
                      <TableHead>Response Time</TableHead>
                      <TableHead>Error</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {auditLog?.items?.map((log: EInvoiceAuditLog) => (
                      <TableRow key={log.id}>
                        <TableCell className="text-sm">{formatDate(log.requestTimestamp)}</TableCell>
                        <TableCell>
                          <Badge variant="outline">{log.actionType}</Badge>
                        </TableCell>
                        <TableCell>{getStatusBadge(log.responseStatus)}</TableCell>
                        <TableCell className="font-mono text-xs">{log.irn?.slice(0, 20)}...</TableCell>
                        <TableCell>{log.responseTimeMs ? `${log.responseTimeMs}ms` : '-'}</TableCell>
                        <TableCell className="text-red-600 text-sm">{log.errorMessage || '-'}</TableCell>
                      </TableRow>
                    ))}
                    {(!auditLog?.items || auditLog.items.length === 0) && (
                      <TableRow>
                        <TableCell colSpan={6} className="text-center text-gray-500 py-8">
                          No audit records found
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="errors" className="space-y-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle>Recent Errors</CardTitle>
                <CardDescription>Failed e-invoice API calls</CardDescription>
              </div>
              <Button variant="outline" size="sm" onClick={() => refetchErrors()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </CardHeader>
            <CardContent>
              {errorsLoading ? (
                <div className="flex justify-center py-8">
                  <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                </div>
              ) : errors && errors.length > 0 ? (
                <div className="space-y-4">
                  {errors.map((err) => (
                    <Alert key={err.id} variant="destructive">
                      <AlertCircle className="h-4 w-4" />
                      <AlertTitle className="flex items-center gap-2">
                        {err.actionType}
                        <Badge variant="outline" className="ml-2">
                          {err.errorCode}
                        </Badge>
                      </AlertTitle>
                      <AlertDescription>
                        <div className="mt-1">{err.errorMessage}</div>
                        <div className="text-xs mt-2 opacity-70">{formatDate(err.createdAt)}</div>
                      </AlertDescription>
                    </Alert>
                  ))}
                </div>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <CheckCircle className="h-12 w-12 mx-auto mb-4 text-green-500" />
                  <p>No errors found!</p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="queue" className="space-y-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between">
              <div>
                <CardTitle>Processing Queue</CardTitle>
                <CardDescription>Pending and in-progress e-invoice operations</CardDescription>
              </div>
              <Button variant="outline" size="sm" onClick={() => refetchQueue()}>
                <RefreshCw className="h-4 w-4 mr-2" />
                Refresh
              </Button>
            </CardHeader>
            <CardContent>
              {queueLoading ? (
                <div className="flex justify-center py-8">
                  <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
                </div>
              ) : queue && queue.length > 0 ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Invoice ID</TableHead>
                      <TableHead>Action</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Retries</TableHead>
                      <TableHead>Created</TableHead>
                      <TableHead>Error</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {queue.map((item) => (
                      <TableRow key={item.id}>
                        <TableCell className="font-mono text-xs">{item.invoiceId.slice(0, 8)}...</TableCell>
                        <TableCell>
                          <Badge variant="outline">{item.actionType}</Badge>
                        </TableCell>
                        <TableCell>{getStatusBadge(item.status)}</TableCell>
                        <TableCell>{item.retryCount}/{item.maxRetries}</TableCell>
                        <TableCell className="text-sm">{formatDate(item.createdAt)}</TableCell>
                        <TableCell className="text-red-600 text-sm">{item.errorMessage || '-'}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <Clock className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                  <p>Queue is empty</p>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
