import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useLdcCertificatesPaged,
  useDeleteLdcCertificate,
  useCancelLdcCertificate,
  useExpiringLdcCertificates,
  useLdcUsageRecords,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { LowerDeductionCertificate, LdcUsageRecord } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import {
  Edit,
  Trash2,
  Award,
  Building2,
  Calendar,
  AlertTriangle,
  CheckCircle,
  Clock,
  XCircle,
  Eye,
  Ban,
  Receipt,
  TrendingDown,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// TDS Sections that support LDC
const TDS_SECTIONS = [
  { value: '194A', label: '194A - Interest' },
  { value: '194C', label: '194C - Contractor' },
  { value: '194I', label: '194I - Rent' },
  { value: '194J', label: '194J - Professional Fees' },
  { value: '194H', label: '194H - Commission' },
  { value: '194O', label: '194O - E-commerce' },
  { value: '195', label: '195 - Non-Resident' },
];

const LdcManagement = () => {
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedStatus, setSelectedStatus] = useState<string>('');
  const [selectedSection, setSelectedSection] = useState<string>('');

  const [deletingEntry, setDeletingEntry] = useState<LowerDeductionCertificate | null>(null);
  const [cancellingEntry, setCancellingEntry] = useState<LowerDeductionCertificate | null>(null);
  const [cancelReason, setCancelReason] = useState('');
  const [viewingUsage, setViewingUsage] = useState<LowerDeductionCertificate | null>(null);

  const { data: companies = [] } = useCompanies();

  const { data: ldcData, isLoading, error, refetch } = useLdcCertificatesPaged({
    companyId: selectedCompanyId || undefined,
    status: selectedStatus || undefined,
    tdsSection: selectedSection || undefined,
    page: 1,
    pageSize: 50,
  });

  const { data: expiringCerts = [] } = useExpiringLdcCertificates(
    selectedCompanyId,
    !!selectedCompanyId
  );

  const { data: usageRecords = [] } = useLdcUsageRecords(
    viewingUsage?.id || '',
    !!viewingUsage
  );

  const deleteLdc = useDeleteLdcCertificate();
  const cancelLdc = useCancelLdcCertificate();

  const ldcCertificates = ldcData?.data || [];

  const handleDelete = (entry: LowerDeductionCertificate) => {
    setDeletingEntry(entry);
  };

  const handleDeleteConfirm = async () => {
    if (deletingEntry) {
      try {
        await deleteLdc.mutateAsync(deletingEntry.id);
        setDeletingEntry(null);
      } catch (error) {
        console.error('Failed to delete LDC certificate:', error);
      }
    }
  };

  const handleCancel = (entry: LowerDeductionCertificate) => {
    setCancellingEntry(entry);
    setCancelReason('');
  };

  const handleCancelConfirm = async () => {
    if (cancellingEntry) {
      try {
        await cancelLdc.mutateAsync({
          id: cancellingEntry.id,
          reason: cancelReason || undefined,
        });
        setCancellingEntry(null);
        setCancelReason('');
      } catch (error) {
        console.error('Failed to cancel LDC certificate:', error);
      }
    }
  };

  const handleViewUsage = (entry: LowerDeductionCertificate) => {
    setViewingUsage(entry);
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  };

  const getStatusBadge = (status: string) => {
    const statusConfig: Record<string, { color: string; icon: typeof Clock; label: string }> = {
      active: { color: 'bg-green-100 text-green-800', icon: CheckCircle, label: 'Active' },
      expired: { color: 'bg-gray-100 text-gray-800', icon: Clock, label: 'Expired' },
      cancelled: { color: 'bg-red-100 text-red-800', icon: XCircle, label: 'Cancelled' },
      exhausted: { color: 'bg-yellow-100 text-yellow-800', icon: TrendingDown, label: 'Exhausted' },
    };
    const config = statusConfig[status] || statusConfig.active;
    const Icon = config.icon;
    return (
      <span className={cn('inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium', config.color)}>
        <Icon className="h-3 w-3" />
        {config.label}
      </span>
    );
  };

  const getCertificateTypeBadge = (type: string) => {
    const isNil = type === 'nil';
    return (
      <span className={cn(
        'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium',
        isNil ? 'bg-purple-100 text-purple-800' : 'bg-blue-100 text-blue-800'
      )}>
        {isNil ? 'Nil Deduction' : 'Lower Deduction'}
      </span>
    );
  };

  const getDaysUntilExpiry = (validTo: string) => {
    const expiryDate = new Date(validTo);
    const today = new Date();
    const diffTime = expiryDate.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    return diffDays;
  };

  const columns: ColumnDef<LowerDeductionCertificate>[] = [
    {
      accessorKey: 'deducteeName',
      header: 'Deductee',
      cell: ({ row }) => {
        const entry = row.original;
        return (
          <div className="flex items-start gap-3">
            <div className="p-2 bg-green-100 rounded-lg">
              <Building2 className="h-5 w-5 text-green-600" />
            </div>
            <div>
              <div className="font-medium text-gray-900">{entry.deducteeName}</div>
              <div className="text-xs text-gray-500 font-mono">PAN: {entry.deducteePan}</div>
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'certificateNumber',
      header: 'Certificate',
      cell: ({ row }) => {
        const entry = row.original;
        return (
          <div>
            <div className="font-mono text-sm">{entry.certificateNumber}</div>
            {getCertificateTypeBadge(entry.certificateType)}
          </div>
        );
      },
    },
    {
      accessorKey: 'tdsSection',
      header: 'Section',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {row.original.tdsSection}
        </span>
      ),
    },
    {
      accessorKey: 'reducedRate',
      header: 'Rate',
      cell: ({ row }) => {
        const entry = row.original;
        const originalRate = entry.originalRate || 10;
        return (
          <div className="text-right">
            <div className="font-medium text-green-600">{entry.reducedRate}%</div>
            <div className="text-xs text-gray-500 line-through">{originalRate}%</div>
          </div>
        );
      },
    },
    {
      accessorKey: 'limitAmount',
      header: 'Limit',
      cell: ({ row }) => {
        const entry = row.original;
        const usedAmount = entry.usedAmount || 0;
        const utilization = (usedAmount / entry.limitAmount) * 100;
        return (
          <div>
            <div className="font-medium text-gray-900">{formatCurrency(entry.limitAmount)}</div>
            <div className="text-xs text-gray-500">
              Used: {formatCurrency(usedAmount)} ({utilization.toFixed(1)}%)
            </div>
            <div className="w-full bg-gray-200 rounded-full h-1.5 mt-1">
              <div
                className={cn(
                  'h-1.5 rounded-full',
                  utilization > 90 ? 'bg-red-500' : utilization > 70 ? 'bg-yellow-500' : 'bg-green-500'
                )}
                style={{ width: `${Math.min(utilization, 100)}%` }}
              />
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'validTo',
      header: 'Validity',
      cell: ({ row }) => {
        const entry = row.original;
        const daysLeft = getDaysUntilExpiry(entry.validTo);
        const isExpiring = daysLeft > 0 && daysLeft <= 30;
        const isExpired = daysLeft <= 0;
        return (
          <div>
            <div className="text-sm text-gray-900">
              {formatDate(entry.validFrom)} - {formatDate(entry.validTo)}
            </div>
            {isExpired ? (
              <span className="text-xs text-red-600">Expired</span>
            ) : isExpiring ? (
              <span className="text-xs text-yellow-600">{daysLeft} days left</span>
            ) : (
              <span className="text-xs text-green-600">{daysLeft} days left</span>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => getStatusBadge(row.original.status),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const entry = row.original;
        const canCancel = entry.status === 'active';
        const canDelete = entry.status !== 'active';
        return (
          <div className="flex space-x-2">
            <button
              onClick={() => handleViewUsage(entry)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="View Usage"
            >
              <Eye size={16} />
            </button>
            {canCancel && (
              <button
                onClick={() => handleCancel(entry)}
                className="text-yellow-600 hover:text-yellow-800 p-1 rounded hover:bg-yellow-50 transition-colors"
                title="Cancel Certificate"
              >
                <Ban size={16} />
              </button>
            )}
            {canDelete && (
              <button
                onClick={() => handleDelete(entry)}
                className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                title="Delete"
              >
                <Trash2 size={16} />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  // Usage record columns
  const usageColumns: ColumnDef<LdcUsageRecord>[] = [
    {
      accessorKey: 'transactionDate',
      header: 'Date',
      cell: ({ row }) => formatDate(row.original.transactionDate),
    },
    {
      accessorKey: 'invoiceNumber',
      header: 'Invoice',
      cell: ({ row }) => (
        <span className="font-mono text-sm">{row.original.invoiceNumber}</span>
      ),
    },
    {
      accessorKey: 'transactionAmount',
      header: 'Amount',
      cell: ({ row }) => formatCurrency(row.original.transactionAmount),
    },
    {
      accessorKey: 'tdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => formatCurrency(row.original.tdsDeducted),
    },
    {
      accessorKey: 'tdsSaved',
      header: 'TDS Saved',
      cell: ({ row }) => (
        <span className="text-green-600 font-medium">
          {formatCurrency(row.original.tdsSaved || 0)}
        </span>
      ),
    },
  ];

  // Summary calculations
  const activeCerts = ldcCertificates.filter(c => c.status === 'active').length;
  const totalLimit = ldcCertificates.reduce((sum, c) => sum + c.limitAmount, 0);
  const totalUsed = ldcCertificates.reduce((sum, c) => sum + (c.usedAmount || 0), 0);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load LDC certificates</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">LDC Management</h1>
          <p className="text-gray-600 mt-2">
            Manage Lower Deduction Certificates (Form 13) for reduced TDS rates
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label htmlFor="companyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Company
            </label>
            <select
              id="companyFilter"
              value={selectedCompanyId}
              onChange={(e) => setSelectedCompanyId(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Companies</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="statusFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Status
            </label>
            <select
              id="statusFilter"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Status</option>
              <option value="active">Active</option>
              <option value="expired">Expired</option>
              <option value="cancelled">Cancelled</option>
              <option value="exhausted">Exhausted</option>
            </select>
          </div>
          <div>
            <label htmlFor="sectionFilter" className="block text-sm font-medium text-gray-700 mb-1">
              TDS Section
            </label>
            <select
              id="sectionFilter"
              value={selectedSection}
              onChange={(e) => setSelectedSection(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="">All Sections</option>
              {TDS_SECTIONS.map((section) => (
                <option key={section.value} value={section.value}>
                  {section.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Expiring Certificates Alert */}
      {expiringCerts.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5" />
            <div className="flex-1">
              <h3 className="font-medium text-amber-800">Certificates Expiring Soon</h3>
              <ul className="mt-2 space-y-1 text-sm text-amber-700">
                {expiringCerts.slice(0, 3).map((cert) => (
                  <li key={cert.id}>
                    • {cert.deducteeName} ({cert.tdsSection}) - expires {formatDate(cert.validTo)}
                  </li>
                ))}
                {expiringCerts.length > 3 && (
                  <li className="text-amber-600">... and {expiringCerts.length - 3} more</li>
                )}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total Certificates</p>
              <p className="text-2xl font-bold text-gray-900">{ldcCertificates.length}</p>
            </div>
            <div className="p-3 bg-green-100 rounded-full">
              <Award className="h-6 w-6 text-green-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">{activeCerts} active</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Total Limit</p>
              <p className="text-2xl font-bold text-blue-600">{formatCurrency(totalLimit)}</p>
            </div>
            <div className="p-3 bg-blue-100 rounded-full">
              <Receipt className="h-6 w-6 text-blue-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Aggregate limit amount</p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Utilized Amount</p>
              <p className="text-2xl font-bold text-purple-600">{formatCurrency(totalUsed)}</p>
            </div>
            <div className="p-3 bg-purple-100 rounded-full">
              <TrendingDown className="h-6 w-6 text-purple-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">
            {totalLimit > 0 ? ((totalUsed / totalLimit) * 100).toFixed(1) : 0}% utilized
          </p>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-500">Expiring Soon</p>
              <p className="text-2xl font-bold text-yellow-600">{expiringCerts.length}</p>
            </div>
            <div className="p-3 bg-yellow-100 rounded-full">
              <Calendar className="h-6 w-6 text-yellow-600" />
            </div>
          </div>
          <p className="text-sm text-gray-500 mt-2">Within 30 days</p>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="font-medium text-blue-800 mb-2">About Lower Deduction Certificates (Form 13)</h3>
        <p className="text-sm text-blue-700">
          LDC certificates allow deductees to have TDS deducted at a lower rate or nil rate.
          The certificate must be verified before applying reduced rates. Track usage to ensure
          the limit is not exceeded during the validity period.
        </p>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={columns}
            data={ldcCertificates}
            searchPlaceholder="Search by deductee name or certificate number..."
          />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingEntry}
        onClose={() => setDeletingEntry(null)}
        title="Delete LDC Certificate"
        size="sm"
      >
        {deletingEntry && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the LDC certificate <strong>{deletingEntry.certificateNumber}</strong>
              for <strong>{deletingEntry.deducteeName}</strong>? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteLdc.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteLdc.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Cancel Confirmation Modal */}
      <Modal
        isOpen={!!cancellingEntry}
        onClose={() => setCancellingEntry(null)}
        title="Cancel LDC Certificate"
        size="md"
      >
        {cancellingEntry && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">Certificate Details:</p>
              <p className="font-medium">{cancellingEntry.certificateNumber}</p>
              <p className="text-sm text-gray-500">{cancellingEntry.deducteeName}</p>
            </div>
            <div>
              <label htmlFor="cancelReason" className="block text-sm font-medium text-gray-700 mb-1">
                Cancellation Reason (Optional)
              </label>
              <textarea
                id="cancelReason"
                value={cancelReason}
                onChange={(e) => setCancelReason(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                rows={3}
                placeholder="Enter reason for cancellation..."
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setCancellingEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Back
              </button>
              <button
                onClick={handleCancelConfirm}
                disabled={cancelLdc.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-yellow-600 border border-transparent rounded-md hover:bg-yellow-700 disabled:opacity-50"
              >
                {cancelLdc.isPending ? 'Cancelling...' : 'Cancel Certificate'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Usage Records Drawer */}
      <Drawer
        isOpen={!!viewingUsage}
        onClose={() => setViewingUsage(null)}
        title="Certificate Usage History"
        size="lg"
      >
        {viewingUsage && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <p className="text-gray-600">Certificate Number</p>
                  <p className="font-mono font-medium">{viewingUsage.certificateNumber}</p>
                </div>
                <div>
                  <p className="text-gray-600">Deductee</p>
                  <p className="font-medium">{viewingUsage.deducteeName}</p>
                </div>
                <div>
                  <p className="text-gray-600">Limit Amount</p>
                  <p className="font-medium">{formatCurrency(viewingUsage.limitAmount)}</p>
                </div>
                <div>
                  <p className="text-gray-600">Used Amount</p>
                  <p className="font-medium">{formatCurrency(viewingUsage.usedAmount || 0)}</p>
                </div>
                <div>
                  <p className="text-gray-600">Remaining</p>
                  <p className="font-medium text-green-600">
                    {formatCurrency(viewingUsage.limitAmount - (viewingUsage.usedAmount || 0))}
                  </p>
                </div>
                <div>
                  <p className="text-gray-600">Utilization</p>
                  <p className="font-medium">
                    {((viewingUsage.usedAmount || 0) / viewingUsage.limitAmount * 100).toFixed(1)}%
                  </p>
                </div>
              </div>
            </div>

            <h3 className="font-medium text-gray-900">Transaction History</h3>
            {usageRecords.length > 0 ? (
              <DataTable
                columns={usageColumns}
                data={usageRecords}
                searchPlaceholder="Search transactions..."
              />
            ) : (
              <div className="text-center py-8 text-gray-500">
                No usage records found for this certificate
              </div>
            )}
          </div>
        )}
      </Drawer>
    </div>
  );
};

export default LdcManagement;
