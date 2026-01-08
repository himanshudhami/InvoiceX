import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useVendors, useDeleteVendor, useVendorPaymentSummary } from '@/features/vendors/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { Vendor, VendorPaymentDetail } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import { Drawer } from '@/components/ui/Drawer';
import { VendorForm } from '@/components/forms/VendorForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Edit, Trash2, Building2, FileText, CreditCard, AlertTriangle, IndianRupee } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useQueryState, parseAsString } from 'nuqs';
import { formatINR } from '@/lib/currency';

const VendorsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingVendor, setEditingVendor] = useState<Vendor | null>(null);
  const [deletingVendor, setDeletingVendor] = useState<Vendor | null>(null);
  const [showPaymentBreakdown, setShowPaymentBreakdown] = useState(false);

  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext();

  // URL-backed filter state with nuqs - persists on refresh
  const [companyFilter, setCompanyFilter] = useQueryState('company', parseAsString.withDefault(''));

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined);

  // Pass effectiveCompanyId to useVendors to fetch filtered data from API
  const { data: vendors = [], isLoading, error, refetch } = useVendors(effectiveCompanyId || undefined);
  const deleteVendor = useDeleteVendor();

  // Fetch payment summary
  const { data: paymentSummary } = useVendorPaymentSummary(effectiveCompanyId || '', !!effectiveCompanyId);

  const handleEdit = (vendor: Vendor) => {
    setEditingVendor(vendor);
  };

  const handleDelete = (vendor: Vendor) => {
    setDeletingVendor(vendor);
  };

  const handleDeleteConfirm = async () => {
    if (deletingVendor) {
      try {
        await deleteVendor.mutateAsync(deletingVendor.id);
        setDeletingVendor(null);
      } catch (error) {
        console.error('Failed to delete vendor:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingVendor(null);
    refetch();
  };

  const columns: ColumnDef<Vendor>[] = [
    {
      accessorKey: 'name',
      header: 'Vendor Name',
      cell: ({ row }) => {
        const vendor = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{vendor.name}</div>
            {vendor.companyName && (
              <div className="text-sm text-gray-500">{vendor.companyName}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'gstin',
      header: 'GST Details',
      cell: ({ row }) => {
        const vendor = row.original;
        return (
          <div>
            {vendor.gstin ? (
              <div className="text-sm text-gray-900 font-mono">{vendor.gstin}</div>
            ) : (
              <div className="text-sm text-gray-500">—</div>
            )}
            <div className={`text-xs ${vendor.isGstRegistered ? 'text-green-600' : 'text-yellow-600'}`}>
              {vendor.vendorType === 'registered' ? 'Registered' : vendor.vendorType || 'Unregistered'}
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'email',
      header: 'Contact',
      cell: ({ row }) => {
        const vendor = row.original;
        return (
          <div>
            {vendor.email && (
              <div className="text-sm text-gray-900">{vendor.email}</div>
            )}
            {vendor.phone && (
              <div className="text-sm text-gray-500">{vendor.phone}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'tdsApplicable',
      header: 'TDS',
      cell: ({ row }) => {
        const vendor = row.original;
        if (!vendor.tdsApplicable) {
          return <div className="text-sm text-gray-500">N/A</div>;
        }
        return (
          <div>
            <div className="text-sm text-gray-900">{vendor.defaultTdsSection || 'N/A'}</div>
            <div className="text-xs text-gray-500">{vendor.defaultTdsRate}%</div>
          </div>
        );
      },
    },
    {
      accessorKey: 'msmeRegistered',
      header: 'MSME',
      cell: ({ row }) => {
        const vendor = row.original;
        if (!vendor.msmeRegistered) {
          return <div className="text-sm text-gray-500">—</div>;
        }
        return (
          <div className="inline-flex items-center px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800">
            {vendor.msmeCategory?.toUpperCase() || 'MSME'}
          </div>
        );
      },
    },
    {
      accessorKey: 'city',
      header: 'Location',
      cell: ({ row }) => {
        const vendor = row.original;
        const location = [vendor.city, vendor.state].filter(Boolean).join(', ');
        return location ? (
          <div className="text-sm text-gray-900">{location}</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        );
      },
    },
    {
      accessorKey: 'paymentTerms',
      header: 'Payment Terms',
      cell: ({ row }) => {
        const paymentTerms = row.getValue('paymentTerms') as number;
        return paymentTerms ? (
          <div className="text-sm text-gray-900">{paymentTerms} days</div>
        ) : (
          <div className="text-sm text-gray-500">—</div>
        );
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => {
        const isActive = row.getValue('isActive') as boolean;
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${
            isActive
              ? 'bg-green-100 text-green-800'
              : 'bg-gray-100 text-gray-800'
          }`}>
            {isActive ? 'Active' : 'Inactive'}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const vendor = row.original;
        return (
          <div className="flex space-x-1">
            <Link
              to={`/finance/ap/vendor-invoices?vendorId=${vendor.id}${effectiveCompanyId ? `&company=${effectiveCompanyId}` : ''}`}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View invoices"
            >
              <FileText size={16} />
            </Link>
            <Link
              to={`/finance/ap/vendor-payments?vendorId=${vendor.id}${effectiveCompanyId ? `&company=${effectiveCompanyId}` : ''}`}
              className="text-gray-600 hover:text-gray-800 p-1 rounded hover:bg-gray-50 transition-colors"
              title="View payments"
            >
              <CreditCard size={16} />
            </Link>
            <button
              onClick={() => handleEdit(vendor)}
              className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
              title="Edit vendor"
            >
              <Edit size={16} />
            </button>
            <button
              onClick={() => handleDelete(vendor)}
              className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
              title="Delete vendor"
            >
              <Trash2 size={16} />
            </button>
          </div>
        );
      },
    },
  ];

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
        <div className="text-red-600 mb-4">Failed to load vendors</div>
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
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Vendors</h1>
          <p className="text-gray-600 mt-2">Manage your suppliers and creditors</p>
        </div>
        <div className="flex items-center gap-4">
          <Link
            to={`/finance/ap/vendor-invoices${effectiveCompanyId ? `?company=${effectiveCompanyId}` : ''}`}
            className="inline-flex items-center px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            <FileText size={16} className="mr-2" />
            Bills
          </Link>
          <Link
            to={`/finance/ap/vendor-payments${effectiveCompanyId ? `?company=${effectiveCompanyId}` : ''}`}
            className="inline-flex items-center px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            <CreditCard size={16} className="mr-2" />
            Payments
          </Link>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <Building2 className="h-8 w-8 text-blue-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Vendors</p>
              <p className="text-2xl font-semibold text-gray-900">{vendors.length}</p>
            </div>
          </div>
        </div>
        <button
          onClick={() => setShowPaymentBreakdown(true)}
          className="bg-white rounded-lg shadow p-4 hover:bg-green-50 hover:shadow-md transition-all text-left cursor-pointer border-2 border-transparent hover:border-green-200"
        >
          <div className="flex items-center">
            <div className="h-8 w-8 bg-green-100 rounded-full flex items-center justify-center">
              <IndianRupee className="h-4 w-4 text-green-600" />
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Paid</p>
              <p className="text-2xl font-semibold text-green-600">
                {formatINR(paymentSummary?.totalPaid || 0)}
              </p>
              <p className="text-xs text-gray-400">
                {paymentSummary?.vendorCount || 0} vendors
              </p>
            </div>
          </div>
        </button>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-blue-100 rounded-full flex items-center justify-center">
              <span className="text-blue-600 font-semibold text-sm">GST</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">GST Registered</p>
              <p className="text-2xl font-semibold text-gray-900">
                {vendors.filter(v => v.isGstRegistered).length}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-purple-100 rounded-full flex items-center justify-center">
              <span className="text-purple-600 font-semibold text-sm">TDS</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">TDS Applicable</p>
              <p className="text-2xl font-semibold text-gray-900">
                {vendors.filter(v => v.tdsApplicable).length}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-orange-100 rounded-full flex items-center justify-center">
              <AlertTriangle className="h-4 w-4 text-orange-600" />
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">MSME Vendors</p>
              <p className="text-2xl font-semibold text-gray-900">
                {vendors.filter(v => v.msmeRegistered).length}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Data Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown
                value={companyFilter ?? ''}
                onChange={(value) => setCompanyFilter(value || null)}
              />
            </div>
          </div>
          <DataTable
            columns={columns}
            data={vendors}
            searchPlaceholder="Search vendors..."
            onAdd={() => setIsCreateDrawerOpen(true)}
            addButtonText="Add Vendor"
          />
        </div>
      </div>

      {/* Create Vendor Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create New Vendor"
        size="xl"
      >
        <VendorForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Vendor Drawer */}
      <Drawer
        isOpen={!!editingVendor}
        onClose={() => setEditingVendor(null)}
        title="Edit Vendor"
        size="xl"
      >
        {editingVendor && (
          <VendorForm
            vendor={editingVendor}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingVendor(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingVendor}
        onClose={() => setDeletingVendor(null)}
        title="Delete Vendor"
        size="sm"
      >
        {deletingVendor && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingVendor.name}</strong>?
              This action cannot be undone and may affect related invoices and payments.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingVendor(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteVendor.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteVendor.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Payment Breakdown Drawer */}
      <Drawer
        isOpen={showPaymentBreakdown}
        onClose={() => setShowPaymentBreakdown(false)}
        title="Vendor Payment Breakdown"
        size="2/3"
        resizable
        resizeStorageKey="vendor-payment-breakdown-drawer-width"
      >
        <div className="space-y-4">
          {/* Summary Header */}
          <div className="bg-green-50 rounded-lg p-4 border border-green-200">
            <div className="flex justify-between items-center">
              <div>
                <p className="text-sm text-green-600 font-medium">Total Paid to Vendors</p>
                <p className="text-3xl font-bold text-green-700">
                  {formatINR(paymentSummary?.totalPaid || 0)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">{paymentSummary?.vendorCount || 0} vendors</p>
                <p className="text-sm text-gray-500">{paymentSummary?.paymentCount || 0} payments</p>
              </div>
            </div>
          </div>

          {/* Vendor List */}
          <div className="border rounded-lg overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Vendor
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Payments
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Total Paid
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Last Payment
                  </th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {paymentSummary?.vendors.map((vendor: VendorPaymentDetail) => (
                  <tr key={vendor.vendorId} className="hover:bg-gray-50">
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{vendor.vendorName}</div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                        {vendor.paymentCount}
                      </span>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm font-semibold text-green-600">
                        {formatINR(vendor.totalPaid)}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm text-gray-500">
                        {vendor.lastPaymentDate
                          ? new Date(vendor.lastPaymentDate).toLocaleDateString('en-IN')
                          : '-'}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-center">
                      <Link
                        to={`/finance/ap/vendor-payments?vendorId=${vendor.vendorId}${effectiveCompanyId ? `&company=${effectiveCompanyId}` : ''}`}
                        className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                      >
                        View Payments
                      </Link>
                    </td>
                  </tr>
                ))}
                {(!paymentSummary?.vendors || paymentSummary.vendors.length === 0) && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-gray-500">
                      No payments recorded yet
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </Drawer>
    </div>
  );
};

export default VendorsManagement;
