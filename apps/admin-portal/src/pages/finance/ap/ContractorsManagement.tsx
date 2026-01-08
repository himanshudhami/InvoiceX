import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useContractorPayments, useContractorPaymentBreakdown } from '@/features/payroll/hooks/useContractorPayments';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { DataTable } from '@/components/ui/DataTable';
import { Drawer } from '@/components/ui/Drawer';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { Users, IndianRupee, FileText, CreditCard } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useQueryState, parseAsString } from 'nuqs';
import { formatINR } from '@/lib/currency';
import type { ContractorPayment, ContractorPaymentDetail } from '@/features/payroll/types/payroll';

const ContractorsManagement = () => {
  const [showPaymentBreakdown, setShowPaymentBreakdown] = useState(false);

  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext();

  // URL-backed filter state with nuqs - persists on refresh
  const [companyFilter, setCompanyFilter] = useQueryState('company', parseAsString.withDefault(''));

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = companyFilter || (hasMultiCompanyAccess ? selectedCompanyId : undefined);

  // Fetch contractor payments with pagination
  const { data: paymentsData, isLoading, error, refetch } = useContractorPayments({
    companyId: effectiveCompanyId || undefined,
    pageNumber: 1,
    pageSize: 100,
  });

  // Fetch payment breakdown
  const { data: paymentBreakdown } = useContractorPaymentBreakdown(
    effectiveCompanyId || '',
    !!effectiveCompanyId
  );

  // Get unique contractors from payments
  const uniqueContractors = paymentsData?.items
    ? Array.from(
        new Map(
          paymentsData.items
            .filter(p => p.partyId && p.partyName)
            .map(p => [p.partyId, { id: p.partyId, name: p.partyName }])
        ).values()
      )
    : [];

  const columns: ColumnDef<ContractorPayment>[] = [
    {
      accessorKey: 'partyName',
      header: 'Contractor',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{payment.partyName || 'Unknown'}</div>
            {payment.companyName && (
              <div className="text-sm text-gray-500">{payment.companyName}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'paymentMonth',
      header: 'Period',
      cell: ({ row }) => {
        const payment = row.original;
        const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return (
          <div className="text-sm text-gray-900">
            {monthNames[payment.paymentMonth - 1]} {payment.paymentYear}
          </div>
        );
      },
    },
    {
      accessorKey: 'grossAmount',
      header: 'Gross Amount',
      cell: ({ row }) => {
        const grossAmount = row.getValue('grossAmount') as number;
        return (
          <div className="text-sm font-medium text-gray-900">
            {formatINR(grossAmount)}
          </div>
        );
      },
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div>
            <div className="text-sm text-red-600">-{formatINR(payment.tdsAmount)}</div>
            <div className="text-xs text-gray-500">
              {payment.tdsSection} @ {payment.tdsRate}%
            </div>
          </div>
        );
      },
    },
    {
      accessorKey: 'netPayable',
      header: 'Net Payable',
      cell: ({ row }) => {
        const netPayable = row.getValue('netPayable') as number;
        return (
          <div className="text-sm font-semibold text-green-600">
            {formatINR(netPayable)}
          </div>
        );
      },
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.getValue('status') as string;
        const statusColors: Record<string, string> = {
          pending: 'bg-gray-100 text-gray-800',
          approved: 'bg-blue-100 text-blue-800',
          paid: 'bg-green-100 text-green-800',
          cancelled: 'bg-red-100 text-red-800',
        };
        return (
          <div className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${statusColors[status] || 'bg-gray-100 text-gray-800'}`}>
            {status.charAt(0).toUpperCase() + status.slice(1)}
          </div>
        );
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const payment = row.original;
        return (
          <div className="flex space-x-1">
            <Link
              to={`/finance/ap/contractor-payments?partyId=${payment.partyId}${effectiveCompanyId ? `&companyId=${effectiveCompanyId}` : ''}`}
              className="text-blue-600 hover:text-blue-800 text-sm font-medium"
            >
              View All
            </Link>
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
        <div className="text-red-600 mb-4">Failed to load contractors</div>
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
          <h1 className="text-3xl font-bold text-gray-900">Contractors</h1>
          <p className="text-gray-600 mt-2">Manage contractor payments and TDS deductions</p>
        </div>
        <div className="flex items-center gap-4">
          <Link
            to={`/finance/ap/contractor-payments${effectiveCompanyId ? `?companyId=${effectiveCompanyId}` : ''}`}
            className="inline-flex items-center px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
          >
            <CreditCard size={16} className="mr-2" />
            All Payments
          </Link>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <Users className="h-8 w-8 text-blue-600" />
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Contractors</p>
              <p className="text-2xl font-semibold text-gray-900">
                {paymentBreakdown?.contractorCount || uniqueContractors.length}
              </p>
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
                {formatINR(paymentBreakdown?.totalPaid || 0)}
              </p>
              <p className="text-xs text-gray-400">
                {paymentBreakdown?.paymentCount || 0} payments
              </p>
            </div>
          </div>
        </button>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-orange-100 rounded-full flex items-center justify-center">
              <span className="text-orange-600 font-semibold text-xs">GROSS</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total Gross</p>
              <p className="text-2xl font-semibold text-gray-900">
                {formatINR(paymentBreakdown?.totalGross || 0)}
              </p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center">
            <div className="h-8 w-8 bg-red-100 rounded-full flex items-center justify-center">
              <span className="text-red-600 font-semibold text-xs">TDS</span>
            </div>
            <div className="ml-3">
              <p className="text-sm font-medium text-gray-500">Total TDS Deducted</p>
              <p className="text-2xl font-semibold text-red-600">
                {formatINR(paymentBreakdown?.totalTds || 0)}
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
            data={paymentsData?.items || []}
            searchPlaceholder="Search contractors..."
          />
        </div>
      </div>

      {/* Payment Breakdown Drawer */}
      <Drawer
        isOpen={showPaymentBreakdown}
        onClose={() => setShowPaymentBreakdown(false)}
        title="Contractor Payment Breakdown"
        size="2/3"
        resizable
        resizeStorageKey="contractor-payment-breakdown-drawer-width"
      >
        <div className="space-y-4">
          {/* Summary Header */}
          <div className="bg-green-50 rounded-lg p-4 border border-green-200">
            <div className="flex justify-between items-center">
              <div>
                <p className="text-sm text-green-600 font-medium">Total Paid to Contractors</p>
                <p className="text-3xl font-bold text-green-700">
                  {formatINR(paymentBreakdown?.totalPaid || 0)}
                </p>
              </div>
              <div className="text-right">
                <p className="text-sm text-gray-500">{paymentBreakdown?.contractorCount || 0} contractors</p>
                <p className="text-sm text-gray-500">{paymentBreakdown?.paymentCount || 0} payments</p>
              </div>
            </div>
            <div className="mt-3 pt-3 border-t border-green-200 flex justify-between text-sm">
              <div>
                <span className="text-gray-500">Total Gross:</span>{' '}
                <span className="font-medium text-gray-900">{formatINR(paymentBreakdown?.totalGross || 0)}</span>
              </div>
              <div>
                <span className="text-gray-500">Total TDS:</span>{' '}
                <span className="font-medium text-red-600">{formatINR(paymentBreakdown?.totalTds || 0)}</span>
              </div>
            </div>
          </div>

          {/* Contractor List */}
          <div className="border rounded-lg overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Contractor
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Payments
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Gross
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    TDS
                  </th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Net Paid
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
                {paymentBreakdown?.contractors.map((contractor: ContractorPaymentDetail) => (
                  <tr key={contractor.contractorId} className="hover:bg-gray-50">
                    <td className="px-4 py-3 whitespace-nowrap">
                      <div className="text-sm font-medium text-gray-900">{contractor.contractorName}</div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                        {contractor.paymentCount}
                      </span>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm text-gray-900">
                        {formatINR(contractor.totalGross)}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm text-red-600">
                        {formatINR(contractor.totalTds)}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm font-semibold text-green-600">
                        {formatINR(contractor.totalPaid)}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-right">
                      <div className="text-sm text-gray-500">
                        {contractor.lastPaymentDate
                          ? new Date(contractor.lastPaymentDate).toLocaleDateString('en-IN')
                          : '-'}
                      </div>
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-center">
                      <Link
                        to={`/finance/ap/contractor-payments?partyId=${contractor.contractorId}${effectiveCompanyId ? `&companyId=${effectiveCompanyId}` : ''}`}
                        className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                      >
                        View Payments
                      </Link>
                    </td>
                  </tr>
                ))}
                {(!paymentBreakdown?.contractors || paymentBreakdown.contractors.length === 0) && (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-gray-500">
                      No contractor payments recorded yet
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

export default ContractorsManagement;
