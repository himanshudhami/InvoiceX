import { useMemo, useState, useEffect } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useQueryStates, parseAsString } from 'nuqs';
import { Asset, AssetAssignment, AssetMaintenance } from '@/services/api/types';
import { formatCurrency } from '@/lib/currency';
import {
  useAssets,
  useCreateAsset,
  useUpdateAsset,
  useDeleteAsset,
  useAssetAssignments,
  useAllAssetAssignments,
  useAssignAsset,
  useReturnAssetAssignment,
  useMaintenance,
  useCreateMaintenance,
  useAssetMaintenance,
  useUpdateMaintenance,
  useAssetCostSummary,
  useAssetCostReport,
  useDisposeAsset,
  useAssetDocuments,
  useAddAssetDocument,
  useDeleteAssetDocument,
} from '@/hooks/api/useAssets';
import { useEmployees } from '@/hooks/api/useEmployees';
import { useCompanies } from '@/hooks/api/useCompanies';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { DataTable } from '../components/ui/DataTable';
import { Drawer } from '@/components/ui/Drawer';
import { Modal } from '@/components/ui/Modal';
import { AssetForm } from '@/components/forms/AssetForm';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { Edit, Trash2, Link2, RefreshCcw, Eye, Upload } from 'lucide-react';
import { CurrencySelect } from '@/components/ui/currency-select';
import { AssetBulkUploadModal } from '@/components/forms/AssetBulkUploadModal';

const getStatusBadgeColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'available':
      return 'bg-green-100 text-green-800';
    case 'assigned':
      return 'bg-blue-100 text-blue-800';
    case 'maintenance':
      return 'bg-yellow-100 text-yellow-800';
    case 'retired':
      return 'bg-gray-100 text-gray-800';
    case 'reserved':
      return 'bg-purple-100 text-purple-800';
    case 'lost':
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const AssetsManagement = () => {
  const { data, isLoading, error, refetch } = useAssets({ pageNumber: 1, pageSize: 50 });
  const createAsset = useCreateAsset();
  const updateAsset = useUpdateAsset();
  const deleteAsset = useDeleteAsset();
  const assignAsset = useAssignAsset();
  const returnAsset = useReturnAssetAssignment();
  const maintenanceQuery = useMaintenance({ pageNumber: 1, pageSize: 50 });
  const createMaintenance = useCreateMaintenance();
  const updateMaintenance = useUpdateMaintenance();
  const disposeAsset = useDisposeAsset();
  const { data: employees = [] } = useEmployees();
  const { data: companies = [] } = useCompanies();
  const { data: allAssignments = [], refetch: refetchAssignments } = useAllAssetAssignments();

  // URL-backed filter state with nuqs - persists on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      tab: parseAsString.withDefault('assets'),
      company: parseAsString.withDefault(''),
      assignmentsCompany: parseAsString.withDefault(''),
      assignmentsSearch: parseAsString.withDefault(''),
      maintenanceCompany: parseAsString.withDefault(''),
      maintenanceStatus: parseAsString.withDefault(''),
      maintenanceSearch: parseAsString.withDefault(''),
    },
    { history: 'replace' }
  )

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isBulkUploadOpen, setIsBulkUploadOpen] = useState(false);
  const [editing, setEditing] = useState<Asset | null>(null);
  const [toDelete, setToDelete] = useState<Asset | null>(null);
  const [assigning, setAssigning] = useState<Asset | null>(null);
  const [viewing, setViewing] = useState<Asset | null>(null);
  const [assignForm, setAssignForm] = useState({
    targetType: 'company',
    companyId: '',
    employeeId: '',
  });
  const [maintenanceAssetId, setMaintenanceAssetId] = useState<string>('');
  const [isMaintenanceDrawerOpen, setIsMaintenanceDrawerOpen] = useState(false);
  const { data: costReport } = useAssetCostReport(urlState.company || undefined);
  const assignmentsQuery = useAssetAssignments(assigning?.id ?? '', !!assigning);
  const assetMaintenanceQuery = useAssetMaintenance(viewing?.id ?? '', !!viewing);
  const costSummaryQuery = useAssetCostSummary(viewing?.id ?? '', !!viewing);
  const documentsQuery = useAssetDocuments(viewing?.id ?? '', !!viewing);
  const addDocument = useAddAssetDocument();
  const deleteDocument = useDeleteAssetDocument();
  const [maintenanceForm, setMaintenanceForm] = useState({
    title: '',
    cost: '',
    currency: 'USD',
    dueDate: '',
    vendor: '',
    notes: '',
  });
  const [documentForm, setDocumentForm] = useState({
    name: '',
    url: '',
    contentType: '',
    notes: '',
  });
  const [disposalForm, setDisposalForm] = useState({
    disposedOn: '',
    method: 'retired',
    proceeds: '',
    disposalCost: '',
    currency: 'USD',
    buyer: '',
    notes: '',
  });

  const employeeMap = useMemo(() => new Map(employees.map(e => [e.id, e])), [employees]);
  const companyMap = useMemo(() => new Map(companies.map(c => [c.id, c])), [companies]);
  const assetMap = useMemo(() => new Map(data?.items?.map(a => [a.id, a]) || []), [data]);

  // Initialize disposal form with asset currency when viewing changes
  useEffect(() => {
    if (viewing) {
      setDisposalForm(prev => ({
        ...prev,
        currency: viewing.currency || 'USD'
      }));
    }
  }, [viewing]);

  type AssignmentWithAsset = AssetAssignment & {
    asset?: Asset;
    employeeName?: string;
    companyName?: string;
  };

  const enrichedAssignments: AssignmentWithAsset[] = useMemo(() => {
    return allAssignments.map(a => ({
      ...a,
      asset: assetMap.get(a.assetId),
      employeeName: a.targetType === 'employee' && a.employeeId ? employeeMap.get(a.employeeId)?.employeeName : undefined,
      companyName: companyMap.get(a.companyId)?.name,
    }));
  }, [allAssignments, assetMap, employeeMap, companyMap]);

  // Filter assets by company
  const filteredAssets = useMemo(() => {
    if (!urlState.company || !data?.items) return data?.items || [];
    return data.items.filter(a => a.companyId === urlState.company);
  }, [data?.items, urlState.company]);

  const filteredAssignments = useMemo(() => {
    let filtered = enrichedAssignments;
    
    // Filter by company
    if (urlState.assignmentsCompany) {
      filtered = filtered.filter(a => a.companyId === urlState.assignmentsCompany);
    }
    
    // Filter by search term
    if (urlState.assignmentsSearch) {
      const term = urlState.assignmentsSearch.toLowerCase();
      filtered = filtered.filter(a => {
        const assetTag = a.asset?.assetTag?.toLowerCase() || '';
        const assetName = a.asset?.name?.toLowerCase() || '';
        const empName = a.employeeName?.toLowerCase() || '';
        const companyName = a.companyName?.toLowerCase() || '';
        return assetTag.includes(term) || assetName.includes(term) || empName.includes(term) || companyName.includes(term);
      });
    }
    
    return filtered;
  }, [enrichedAssignments, urlState.assignmentsSearch, urlState.assignmentsCompany]);

  const maintenanceData: AssetMaintenance[] =
    (maintenanceQuery.data as any)?.items ??
    (maintenanceQuery.data as any)?.data ??
    maintenanceQuery.data ??
    [];
  const filteredMaintenance = useMemo(() => {
    let list = maintenanceData;
    if (urlState.maintenanceCompany) {
      list = list.filter((m) => {
        const asset = assetMap.get(m.assetId);
        return asset?.companyId === urlState.maintenanceCompany;
      });
    }
    if (urlState.maintenanceStatus) {
      list = list.filter((m) => m.status === urlState.maintenanceStatus);
    }
    if (!urlState.maintenanceSearch) return list;
    const term = urlState.maintenanceSearch.toLowerCase();
    return list.filter((m) => {
      const asset = assetMap.get(m.assetId);
      return (
        (m.title?.toLowerCase().includes(term) ?? false) ||
        (m.vendor?.toLowerCase().includes(term) ?? false) ||
        (asset?.assetTag?.toLowerCase().includes(term) ?? false) ||
        (asset?.name?.toLowerCase().includes(term) ?? false)
      );
    });
  }, [maintenanceData, urlState.maintenanceSearch, assetMap, urlState.maintenanceCompany, urlState.maintenanceStatus]);

  const maintenanceColumns: ColumnDef<AssetMaintenance>[] = useMemo(() => [
    {
      header: 'Asset',
      accessorKey: 'assetId',
      cell: ({ row }) => {
        const m = row.original;
        const asset = assetMap.get(m.assetId);
        return asset ? `${asset.assetTag} • ${asset.name}` : m.assetId;
      },
    },
    {
      header: 'Title',
      accessorKey: 'title',
    },
    {
      header: 'Status',
      accessorKey: 'status',
      cell: ({ row }) => <span className="capitalize">{row.original.status}</span>,
    },
    {
      header: 'Cost',
      accessorKey: 'cost',
      cell: ({ row }) => {
        const m = row.original;
        const asset = assetMap.get(m.assetId);
        return m.cost
          ? formatCurrency(m.cost, m.currency || asset?.currency || 'USD')
          : '—';
      },
    },
    {
      header: 'Due',
      accessorKey: 'dueDate',
      cell: ({ row }) => (row.original.dueDate ? new Date(row.original.dueDate).toLocaleDateString() : '—'),
    },
    {
      header: 'Vendor',
      accessorKey: 'vendor',
      cell: ({ row }) => row.original.vendor || '—',
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const m = row.original;
        if (m.status === 'closed' || m.status === 'resolved') return null;
        return (
          <button
            onClick={() =>
              updateMaintenance.mutate({
                maintenanceId: m.id,
                data: { status: 'closed', closedAt: new Date().toISOString().slice(0, 10) },
              })
            }
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Mark closed
          </button>
        );
      },
    },
  ], [assetMap, updateMaintenance]);

  const assignmentsColumns: ColumnDef<AssignmentWithAsset>[] = useMemo(() => [
    {
      header: 'Asset Tag',
      accessorKey: 'assetTag',
      cell: ({ row }) => row.original.asset?.assetTag || '—',
    },
    {
      header: 'Asset Name',
      accessorKey: 'assetName',
      cell: ({ row }) => row.original.asset?.name || '—',
    },
    {
      header: 'Employee',
      accessorKey: 'employeeName',
      cell: ({ row }) => {
        const assignment = row.original;
        if (assignment.targetType === 'employee' && assignment.employeeId) {
          return assignment.employeeName || '—';
        }
        return '—';
      },
    },
    {
      header: 'Company',
      accessorKey: 'companyName',
      cell: ({ row }) => row.original.companyName || '—',
    },
    {
      header: 'Assigned Date',
      accessorKey: 'assignedOn',
      cell: ({ row }) => new Date(row.original.assignedOn).toLocaleDateString(),
    },
    {
      header: 'Status',
      accessorKey: 'status',
      cell: ({ row }) => {
        const assignment = row.original;
        return assignment.returnedOn ? (
          <span className="text-xs text-gray-500">Returned</span>
        ) : (
          <span className="inline-flex px-2 py-1 text-xs font-medium rounded-full bg-green-100 text-green-800">
            Active
          </span>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const assignment = row.original;
        if (assignment.returnedOn) return null;
        return (
          <button
            onClick={async () => {
              try {
                await returnAsset.mutateAsync({
                  assignmentId: assignment.id,
                  data: { returnedOn: new Date().toISOString().slice(0, 10) },
                });
                // Explicitly refetch to ensure UI updates immediately
                await Promise.all([refetch(), refetchAssignments()]);
              } catch (error) {
                console.error('Failed to return asset:', error);
              }
            }}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Return
          </button>
        );
      },
    },
  ], [returnAsset, refetch, refetchAssignments]);

  const closeDrawer = () => {
    setIsDrawerOpen(false);
    setEditing(null);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Assets</h1>
          <p className="text-gray-600 mt-2">Manage assets and assignments</p>
        </div>
        <div className="flex items-center space-x-3">
          <button
            onClick={() => setIsBulkUploadOpen(true)}
            className="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors flex items-center space-x-2"
          >
            <Upload className="w-4 h-4" />
            <span>Bulk Upload</span>
          </button>
        </div>
      </div>

      <Tabs value={urlState.tab} onValueChange={(value) => setUrlState({ tab: value })} className="w-full">
        <TabsList>
          <TabsTrigger value="assets">Assets</TabsTrigger>
          <TabsTrigger value="assignments">Assignments</TabsTrigger>
          <TabsTrigger value="maintenance">Maintenance</TabsTrigger>
        </TabsList>

        <TabsContent value="assets" className="mt-6">
          <div className="bg-white rounded-lg shadow">
            <div className="p-6">
              {costReport && (
                <div className="space-y-4 mb-6">
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Purchase Cost</CardTitle>
                      </CardHeader>
                      <CardContent className="text-2xl font-semibold">
                        {costReport.totalPurchaseCost.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Maintenance Spend</CardTitle>
                      </CardHeader>
                      <CardContent className="text-2xl font-semibold">
                        {costReport.totalMaintenanceCost.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Accumulated Depreciation</CardTitle>
                      </CardHeader>
                      <CardContent className="text-2xl font-semibold">
                        {costReport.totalAccumulatedDepreciation.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Net Book Value</CardTitle>
                      </CardHeader>
                      <CardContent className="text-2xl font-semibold">
                        {costReport.totalNetBookValue.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">CapEx Purchase</CardTitle>
                      </CardHeader>
                      <CardContent className="text-xl font-semibold">
                        {costReport.totalCapexPurchase.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">OpEx Spend</CardTitle>
                      </CardHeader>
                      <CardContent className="text-xl font-semibold">
                        {costReport.totalOpexSpend.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Avg Age (months)</CardTitle>
                      </CardHeader>
                      <CardContent className="text-xl font-semibold">
                        {Number(costReport.averageAgeMonths ?? 0).toFixed(1)}
                      </CardContent>
                    </Card>
                  </div>

                  <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Aging Buckets</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2">
                          {costReport.agingBuckets?.map((bucket) => (
                            <div key={bucket.label} className="flex items-center justify-between text-sm">
                              <div className="text-gray-600">{bucket.label} ({bucket.assetCount})</div>
                              <div className="font-medium text-gray-900">
                                {bucket.netBookValue.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                              </div>
                            </div>
                          ))}
                          {(!costReport.agingBuckets || costReport.agingBuckets.length === 0) && (
                            <div className="text-sm text-gray-500">No aging data</div>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">CapEx vs OpEx</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2 text-sm">
                          {costReport.byPurchaseType?.map((row) => (
                            <div key={row.purchaseType} className="flex items-center justify-between">
                              <div className="text-gray-600 capitalize">{row.purchaseType}</div>
                              <div className="text-right">
                                <div className="font-semibold text-gray-900">
                                  {row.purchaseCost.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                                </div>
                                <div className="text-xs text-gray-500">{row.assetCount || 0} assets</div>
                              </div>
                            </div>
                          ))}
                          {(!costReport.byPurchaseType || costReport.byPurchaseType.length === 0) && (
                            <div className="text-sm text-gray-500">No purchase type data</div>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                    <Card>
                      <CardHeader>
                        <CardTitle className="text-sm text-gray-500">Top Maintenance Spend</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <div className="space-y-2 text-sm">
                          {costReport.topMaintenanceSpend?.map((row) => (
                            <div key={row.assetId} className="flex items-center justify-between">
                              <div>
                                <div className="font-medium text-gray-900">{row.assetTag}</div>
                                <div className="text-xs text-gray-500">{row.assetName}</div>
                              </div>
                              <div className="text-right text-gray-900 font-semibold">
                                {row.maintenanceCost.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })}
                              </div>
                            </div>
                          ))}
                          {(!costReport.topMaintenanceSpend || costReport.topMaintenanceSpend.length === 0) && (
                            <div className="text-sm text-gray-500">No maintenance spend recorded</div>
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  </div>
                </div>
              )}
              <div className="mb-4 flex items-center gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
                  <CompanyFilterDropdown
                    value={urlState.company}
                    onChange={(val) => setUrlState({ company: val || '' })}
                  />
                </div>
              </div>
              <DataTable
                columns={
                  [
                    { header: 'Tag', accessorKey: 'assetTag' },
                    { header: 'Name', accessorKey: 'name' },
                    { header: 'Type', accessorKey: 'assetType' },
                    {
                      header: 'Status',
                      accessorKey: 'status',
                      cell: ({ row }) => {
                        const asset = row.original as Asset;
                        const status = asset.status || 'available';
                        return (
                          <span className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getStatusBadgeColor(status)}`}>
                            {status.charAt(0).toUpperCase() + status.slice(1)}
                          </span>
                        );
                      },
                    },
                    {
                      id: 'actions',
                      header: 'Actions',
                      cell: ({ row }) => {
                        const asset = row.original as Asset;
                        return (
                          <div className="flex space-x-2">
                            <button
                              onClick={() => setViewing(asset)}
                              className="text-gray-600 hover:text-gray-900 p-1 rounded hover:bg-gray-50 transition-colors"
                              title="View"
                            >
                              <Eye size={16} />
                            </button>
                            <button
                              onClick={() => {
                                setEditing(asset);
                                setIsDrawerOpen(true);
                              }}
                              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                              title="Edit"
                            >
                              <Edit size={16} />
                            </button>
                            <button
                              onClick={() => {
                                setAssigning(asset);
                                setAssignForm({ targetType: 'company', companyId: asset.companyId, employeeId: '' });
                              }}
                              className="text-amber-600 hover:text-amber-800 p-1 rounded hover:bg-amber-50 transition-colors"
                              title="Assign"
                            >
                              <Link2 size={16} />
                            </button>
                            <button
                              onClick={() => setToDelete(asset)}
                              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                              title="Delete"
                            >
                              <Trash2 size={16} />
                            </button>
                          </div>
                        );
                      },
                    },
                  ] as ColumnDef<Asset, any>[]
                }
                data={filteredAssets}
                searchPlaceholder="Search assets..."
                onAdd={() => {
                  setEditing(null);
                  setIsDrawerOpen(true);
                }}
                addButtonText="Add Asset"
              />
            </div>
          </div>
        </TabsContent>

        <TabsContent value="assignments" className="mt-6">
          <div className="bg-white rounded-lg shadow">
            <div className="p-6">
              <div className="mb-4 flex items-center gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
                  <CompanyFilterDropdown
                    value={urlState.assignmentsCompany}
                    onChange={(val) => setUrlState({ assignmentsCompany: val || '' })}
                  />
                </div>
              </div>
              <DataTable
                columns={assignmentsColumns}
                data={filteredAssignments}
                searchPlaceholder="Search by asset tag, name, or employee..."
                searchValue={urlState.assignmentsSearch}
                onSearchChange={(value) => setUrlState({ assignmentsSearch: value || '' })}
              />
            </div>
          </div>
        </TabsContent>

        <TabsContent value="maintenance" className="mt-6">
          <div className="bg-white rounded-lg shadow">
            <div className="p-6 space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">Maintenance & Repairs</h3>
                  <p className="text-sm text-gray-600">Open and completed maintenance across assets</p>
                </div>
                <div className="flex items-center gap-3">
                  <div className="hidden md:block">
                    <CompanyFilterDropdown
                      value={urlState.maintenanceCompany}
                      onChange={(val) => setUrlState({ maintenanceCompany: val || '' })}
                    />
                  </div>
                  <select
                    className="rounded border px-3 py-2 text-sm"
                    value={urlState.maintenanceStatus}
                    onChange={(e) => setUrlState({ maintenanceStatus: e.target.value || '' })}
                  >
                    <option value="">All statuses</option>
                    <option value="open">Open</option>
                    <option value="in_progress">In Progress</option>
                    <option value="resolved">Resolved</option>
                    <option value="closed">Closed</option>
                  </select>
                  <button
                    onClick={() => setIsMaintenanceDrawerOpen(true)}
                    className="bg-primary text-white rounded px-4 py-2 hover:bg-primary/90 text-sm"
                  >
                    Add maintenance
                  </button>
                </div>
              </div>

              {maintenanceQuery.isLoading && <div className="text-sm text-gray-500">Loading maintenance...</div>}
              {maintenanceQuery.data && (
                <DataTable
                  columns={maintenanceColumns as ColumnDef<any, any>[]}
                  data={filteredMaintenance}
                  searchPlaceholder="Search maintenance, asset, vendor..."
                  searchValue={urlState.maintenanceSearch}
                  onSearchChange={(value) => setUrlState({ maintenanceSearch: value || '' })}
                />
              )}

            </div>
          </div>
        </TabsContent>
      </Tabs>

      <Drawer
        isOpen={isDrawerOpen}
        title={editing ? 'Edit Asset' : 'Add Asset'}
        onClose={closeDrawer}
      >
        <AssetForm
          asset={editing ?? undefined}
          onSuccess={() => {
            closeDrawer();
            refetch();
          }}
          onCancel={closeDrawer}
        />
      </Drawer>

      <Drawer
        isOpen={!!assigning}
        title={assigning ? `Assign ${assigning.assetTag}` : 'Assign Asset'}
        onClose={() => setAssigning(null)}
      >
        {assigning && (
          <div className="space-y-4">
            <form
              className="grid grid-cols-1 md:grid-cols-2 gap-4"
              onSubmit={async (e) => {
                e.preventDefault();
                await assignAsset.mutateAsync({
                  assetId: assigning.id,
                  data: {
                    targetType: assignForm.targetType as 'employee' | 'company',
                    companyId: assignForm.companyId,
                    employeeId: assignForm.targetType === 'employee' ? assignForm.employeeId : undefined,
                  },
                });
                refetch();
                refetchAssignments();
                assignmentsQuery.refetch();
              }}
            >
              <div>
                <label className="block text-sm font-medium text-gray-700">Target</label>
                <select
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                  value={assignForm.targetType}
                  onChange={(e) => setAssignForm({ ...assignForm, targetType: e.target.value })}
                >
                  <option value="company">Company</option>
                  <option value="employee">Employee</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700">Company ID</label>
                <input
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                  value={assignForm.companyId}
                  onChange={(e) => setAssignForm({ ...assignForm, companyId: e.target.value })}
                  required
                />
              </div>
              {assignForm.targetType === 'employee' && (
                <div>
                  <label className="block text-sm font-medium text-gray-700">Employee</label>
                  <select
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                    value={assignForm.employeeId}
                    onChange={(e) => setAssignForm({ ...assignForm, employeeId: e.target.value })}
                    required
                  >
                    <option value="">Select employee</option>
                    {employees.map((emp) => (
                      <option key={emp.id} value={emp.id}>
                        {emp.employeeName} ({emp.employeeId})
                      </option>
                    ))}
                  </select>
                </div>
              )}
              <button
                type="submit"
                className="md:col-span-2 bg-primary text-white rounded px-4 py-2 hover:bg-primary/90"
              >
                Assign
              </button>
            </form>

            <div className="border-t pt-4">
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-sm font-semibold text-gray-800">Active Assignments</h4>
                <button
                  onClick={() => assignmentsQuery.refetch()}
                  className="text-gray-500 hover:text-gray-700"
                  title="Refresh"
                >
                  <RefreshCcw size={14} />
                </button>
              </div>
              {assignmentsQuery.isLoading && <div className="text-sm text-gray-500">Loading...</div>}
              {assignmentsQuery.data && assignmentsQuery.data.length === 0 && (
                <div className="text-sm text-gray-500">No assignments</div>
              )}
              {assignmentsQuery.data && assignmentsQuery.data.length > 0 && (
                <div className="space-y-2">
                  {assignmentsQuery.data.map((a) => (
                    <div
                      key={a.id}
                      className="flex items-center justify-between rounded border px-3 py-2"
                    >
                      <div className="text-sm text-gray-800">
                        {a.targetType === 'employee' ? `Employee ${a.employeeId}` : 'Company'}
                      </div>
                      {a.returnedOn ? (
                        <span className="text-xs text-gray-500">Returned</span>
                      ) : (
                        <button
                          onClick={async () => {
                            await returnAsset.mutateAsync({
                              assignmentId: a.id,
                              data: { returnedOn: new Date().toISOString().slice(0, 10) },
                            });
                            refetch();
                            refetchAssignments();
                            assignmentsQuery.refetch();
                          }}
                          className="text-sm text-blue-600 hover:text-blue-800"
                        >
                          Mark returned
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        )}
      </Drawer>

      <Drawer
        isOpen={!!viewing}
        title={viewing ? `Asset ${viewing.assetTag}` : 'Asset'}
        onClose={() => setViewing(null)}
      >
        {viewing && (
          <div className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <div className="text-sm text-gray-500">Name</div>
                <div className="font-semibold text-gray-900">{viewing.name}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Status</div>
                <div className="font-semibold text-gray-900 capitalize">{viewing.status}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Company</div>
                <div className="font-semibold text-gray-900">{companyMap.get(viewing.companyId)?.name || viewing.companyId}</div>
              </div>
              <div>
                <div className="text-sm text-gray-500">Purchase</div>
                <div className="text-gray-800">
                  {viewing.purchaseDate ? new Date(viewing.purchaseDate).toLocaleDateString() : '—'} • {viewing.purchaseCost ? formatCurrency(viewing.purchaseCost, viewing.currency || 'USD') : '—'}
                </div>
              </div>
            </div>

            {costSummaryQuery.data && (
              <div className="space-y-3">
                <div className="grid grid-cols-2 gap-3 bg-gray-50 rounded-lg p-3">
                  <div>
                    <div className="text-xs text-gray-500">Purchase Cost</div>
                    <div className="font-semibold">
                      {formatCurrency(costSummaryQuery.data.purchaseCost, costSummaryQuery.data.currency || 'USD')}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">Maintenance</div>
                    <div className="font-semibold">
                      {formatCurrency(costSummaryQuery.data.maintenanceCost, costSummaryQuery.data.currency || 'USD')}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">Accumulated Depreciation</div>
                    <div className="font-semibold">
                      {formatCurrency(costSummaryQuery.data.accumulatedDepreciation, costSummaryQuery.data.currency || 'USD')}
                    </div>
                  </div>
                  <div>
                    <div className="text-xs text-gray-500">Net Book Value</div>
                    <div className="font-semibold">
                      {formatCurrency(costSummaryQuery.data.netBookValue, costSummaryQuery.data.currency || 'USD')}
                    </div>
                  </div>
                </div>
                <div className="grid grid-cols-2 gap-3 text-xs text-gray-600 bg-gray-50 rounded-lg p-3">
                  <div>
                    <div className="text-[11px] uppercase tracking-wide text-gray-500">Purchase Type</div>
                    <div className="text-sm font-semibold text-gray-900 capitalize">{costSummaryQuery.data.purchaseType}</div>
                  </div>
                  <div>
                    <div className="text-[11px] uppercase tracking-wide text-gray-500">Depreciation</div>
                    <div className="text-sm font-semibold text-gray-900 capitalize">
                      {costSummaryQuery.data.depreciationMethod?.replace(/_/g, ' ') || 'none'} • {formatCurrency(costSummaryQuery.data.monthlyDepreciation, costSummaryQuery.data.currency || 'USD')}/mo
                    </div>
                  </div>
                  <div>
                    <div className="text-[11px] uppercase tracking-wide text-gray-500">Age</div>
                    <div className="text-sm font-semibold text-gray-900">{costSummaryQuery.data.ageMonths} months</div>
                  </div>
                  <div>
                    <div className="text-[11px] uppercase tracking-wide text-gray-500">Remaining Life</div>
                    <div className="text-sm font-semibold text-gray-900">{costSummaryQuery.data.remainingLifeMonths} months</div>
                  </div>
                  <div>
                    <div className="text-[11px] uppercase tracking-wide text-gray-500">Disposal Gain / Loss</div>
                    <div className="text-sm font-semibold text-gray-900">
                      {formatCurrency(costSummaryQuery.data.disposalGainLoss, costSummaryQuery.data.currency || 'USD')}
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div className="border-t pt-3">
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-sm font-semibold text-gray-800">Documents</h4>
              </div>
              {documentsQuery.isLoading && <div className="text-sm text-gray-500">Loading documents...</div>}
              {documentsQuery.data && documentsQuery.data.length === 0 && (
                <div className="text-sm text-gray-500">No documents</div>
              )}
              {documentsQuery.data && documentsQuery.data.length > 0 && (
                <div className="space-y-2">
                  {documentsQuery.data.map((d) => (
                    <div key={d.id} className="flex items-center justify-between rounded border px-3 py-2">
                      <div>
                        <div className="font-medium text-gray-900">{d.name}</div>
                        <div className="text-xs text-gray-500">{d.url}</div>
                      </div>
                      <button
                        onClick={() => deleteDocument.mutate(d.id)}
                        className="text-xs text-red-600 hover:text-red-800"
                      >
                        Remove
                      </button>
                    </div>
                  ))}
                </div>
              )}
              <form
                className="grid grid-cols-1 md:grid-cols-2 gap-3 mt-3"
                onSubmit={async (e) => {
                  e.preventDefault();
                  await addDocument.mutateAsync({
                    assetId: viewing.id,
                    data: {
                      name: documentForm.name,
                      url: documentForm.url,
                      contentType: documentForm.contentType || undefined,
                      notes: documentForm.notes || undefined,
                    },
                  });
                  setDocumentForm({ name: '', url: '', contentType: '', notes: '' });
                  documentsQuery.refetch();
                }}
              >
                <input
                  className="rounded border px-3 py-2"
                  placeholder="Name"
                  value={documentForm.name}
                  onChange={(e) => setDocumentForm({ ...documentForm, name: e.target.value })}
                  required
                />
                <input
                  className="rounded border px-3 py-2"
                  placeholder="URL"
                  value={documentForm.url}
                  onChange={(e) => setDocumentForm({ ...documentForm, url: e.target.value })}
                  required
                />
                <input
                  className="rounded border px-3 py-2"
                  placeholder="Content type"
                  value={documentForm.contentType}
                  onChange={(e) => setDocumentForm({ ...documentForm, contentType: e.target.value })}
                />
                <input
                  className="rounded border px-3 py-2 md:col-span-2"
                  placeholder="Notes"
                  value={documentForm.notes}
                  onChange={(e) => setDocumentForm({ ...documentForm, notes: e.target.value })}
                />
                <button
                  type="submit"
                  className="bg-primary text-white rounded px-4 py-2 hover:bg-primary/90 md:col-span-2"
                >
                  Add document
                </button>
              </form>
            </div>

            <div className="border-t pt-3">
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-sm font-semibold text-gray-800">Maintenance history</h4>
              </div>
              {assetMaintenanceQuery.isLoading && <div className="text-sm text-gray-500">Loading...</div>}
              {assetMaintenanceQuery.data && assetMaintenanceQuery.data.length === 0 && (
                <div className="text-sm text-gray-500">No maintenance records</div>
              )}
              {assetMaintenanceQuery.data && assetMaintenanceQuery.data.length > 0 && (
                <div className="space-y-2">
                  {assetMaintenanceQuery.data.map((m) => (
                    <div key={m.id} className="border rounded px-3 py-2">
                      <div className="font-medium text-gray-900">{m.title}</div>
                      <div className="text-xs text-gray-500">
                        Status: {m.status} • Cost: {m.cost ? formatCurrency(m.cost, m.currency || viewing.currency || 'USD') : '—'}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="border-t pt-3 space-y-3">
              <h4 className="text-sm font-semibold text-gray-800">Dispose asset</h4>
              <form
                className="grid grid-cols-1 md:grid-cols-2 gap-3"
                onSubmit={async (e) => {
                  e.preventDefault();
                  await disposeAsset.mutateAsync({
                    assetId: viewing.id,
                    data: {
                      disposedOn: disposalForm.disposedOn || undefined,
                      method: disposalForm.method,
                      proceeds: disposalForm.proceeds ? Number(disposalForm.proceeds) : undefined,
                      disposalCost: disposalForm.disposalCost ? Number(disposalForm.disposalCost) : undefined,
                      currency: disposalForm.currency || viewing.currency || 'USD',
                      buyer: disposalForm.buyer || undefined,
                      notes: disposalForm.notes || undefined,
                    },
                  });
                  setDisposalForm({ disposedOn: '', method: 'retired', proceeds: '', disposalCost: '', currency: viewing.currency || 'USD', buyer: '', notes: '' });
                  refetch();
                }}
              >
                <input
                  type="date"
                  className="rounded border px-3 py-2"
                  value={disposalForm.disposedOn}
                  onChange={(e) => setDisposalForm({ ...disposalForm, disposedOn: e.target.value })}
                />
                <select
                  className="rounded border px-3 py-2"
                  value={disposalForm.method}
                  onChange={(e) => setDisposalForm({ ...disposalForm, method: e.target.value })}
                >
                  <option value="retired">Retired</option>
                  <option value="sold">Sold</option>
                  <option value="recycled">Recycled</option>
                  <option value="donated">Donated</option>
                  <option value="lost">Lost</option>
                </select>
                <input
                  className="rounded border px-3 py-2"
                  placeholder="Proceeds"
                  value={disposalForm.proceeds}
                  onChange={(e) => setDisposalForm({ ...disposalForm, proceeds: e.target.value })}
                />
                <input
                  className="rounded border px-3 py-2"
                  placeholder="Disposal cost"
                  value={disposalForm.disposalCost}
                  onChange={(e) => setDisposalForm({ ...disposalForm, disposalCost: e.target.value })}
                />
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Currency</label>
                  <CurrencySelect
                    value={disposalForm.currency}
                    onChange={(value) => setDisposalForm({ ...disposalForm, currency: value })}
                  />
                </div>
                <input
                  className="rounded border px-3 py-2 md:col-span-2"
                  placeholder="Buyer"
                  value={disposalForm.buyer}
                  onChange={(e) => setDisposalForm({ ...disposalForm, buyer: e.target.value })}
                />
                <input
                  className="rounded border px-3 py-2 md:col-span-2"
                  placeholder="Notes"
                  value={disposalForm.notes}
                  onChange={(e) => setDisposalForm({ ...disposalForm, notes: e.target.value })}
                />
                <button
                  type="submit"
                  className="bg-primary text-white rounded px-4 py-2 hover:bg-primary/90 md:col-span-2"
                >
                  Dispose asset
                </button>
              </form>
            </div>
          </div>
        )}
      </Drawer>

      <Drawer
        isOpen={isMaintenanceDrawerOpen}
        title="Add maintenance"
        onClose={() => setIsMaintenanceDrawerOpen(false)}
      >
        <form
          className="space-y-4"
          onSubmit={async (e) => {
            e.preventDefault();
            if (!maintenanceForm.title || !maintenanceForm.dueDate) return;
            const targetAssetId = maintenanceAssetId || data?.items?.[0]?.id;
            if (!targetAssetId) return;
            const selectedAsset = data?.items?.find(a => a.id === targetAssetId);
            await createMaintenance.mutateAsync({
              assetId: targetAssetId,
              data: {
                title: maintenanceForm.title,
                cost: maintenanceForm.cost ? Number(maintenanceForm.cost) : undefined,
                currency: maintenanceForm.currency || selectedAsset?.currency || 'USD',
                dueDate: maintenanceForm.dueDate || undefined,
                vendor: maintenanceForm.vendor || undefined,
                notes: maintenanceForm.notes || undefined,
              },
            });
            setMaintenanceForm({ title: '', cost: '', currency: 'USD', dueDate: '', vendor: '', notes: '' });
            setMaintenanceAssetId('');
            setIsMaintenanceDrawerOpen(false);
            maintenanceQuery.refetch();
            refetch();
          }}
        >
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="text-sm font-medium text-gray-700">Asset</label>
              <select
                className="mt-1 rounded border px-3 py-2 w-full"
                value={maintenanceAssetId}
                onChange={(e) => {
                  const assetId = e.target.value;
                  setMaintenanceAssetId(assetId);
                  const selectedAsset = data?.items?.find(a => a.id === assetId);
                  if (selectedAsset?.currency) {
                    setMaintenanceForm({ ...maintenanceForm, currency: selectedAsset.currency });
                  }
                }}
                required
              >
                <option value="">Select asset</option>
                {data?.items?.map((a) => (
                  <option key={a.id} value={a.id}>
                    {a.assetTag} - {a.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">Title</label>
              <input
                className="mt-1 rounded border px-3 py-2 w-full"
                placeholder="Title"
                value={maintenanceForm.title}
                onChange={(e) => setMaintenanceForm({ ...maintenanceForm, title: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">Cost</label>
              <input
                className="mt-1 rounded border px-3 py-2 w-full"
                placeholder="Cost"
                type="number"
                value={maintenanceForm.cost}
                onChange={(e) => setMaintenanceForm({ ...maintenanceForm, cost: e.target.value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">Currency</label>
              <CurrencySelect
                value={maintenanceForm.currency}
                onChange={(value) => setMaintenanceForm({ ...maintenanceForm, currency: value })}
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">Due date</label>
              <input
                className="mt-1 rounded border px-3 py-2 w-full"
                type="date"
                value={maintenanceForm.dueDate}
                onChange={(e) => setMaintenanceForm({ ...maintenanceForm, dueDate: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="text-sm font-medium text-gray-700">Vendor</label>
              <input
                className="mt-1 rounded border px-3 py-2 w-full"
                placeholder="Vendor"
                value={maintenanceForm.vendor}
                onChange={(e) => setMaintenanceForm({ ...maintenanceForm, vendor: e.target.value })}
              />
            </div>
            <div className="md:col-span-2">
              <label className="text-sm font-medium text-gray-700">Notes</label>
              <textarea
                className="mt-1 rounded border px-3 py-2 w-full"
                placeholder="Notes"
                value={maintenanceForm.notes}
                onChange={(e) => setMaintenanceForm({ ...maintenanceForm, notes: e.target.value })}
              />
            </div>
          </div>
          <div className="flex justify-end gap-3">
            <button
              type="button"
              onClick={() => setIsMaintenanceDrawerOpen(false)}
              className="px-4 py-2 rounded border border-gray-300 text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 rounded bg-primary text-white hover:bg-primary/90"
            >
              Save
            </button>
          </div>
        </form>
      </Drawer>

      <Modal
        isOpen={!!toDelete}
        title="Delete asset"
        onClose={() => setToDelete(null)}
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-700">
            Are you sure you want to delete this asset? This action cannot be undone.
          </p>
          <div className="flex justify-end gap-3">
            <button
              type="button"
              onClick={() => setToDelete(null)}
              className="px-4 py-2 rounded border border-gray-300 text-gray-700 hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={async () => {
                if (!toDelete) return;
                await deleteAsset.mutateAsync(toDelete.id);
                setToDelete(null);
                refetch();
              }}
              className="px-4 py-2 rounded bg-red-600 text-white hover:bg-red-700"
            >
              Delete
            </button>
          </div>
        </div>
      </Modal>

      <AssetBulkUploadModal
        isOpen={isBulkUploadOpen}
        onClose={() => setIsBulkUploadOpen(false)}
        onSuccess={() => {
          refetch();
        }}
      />
    </div>
  );
};

export default AssetsManagement;



