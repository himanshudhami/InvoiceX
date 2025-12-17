import { useState, useMemo } from 'react';
import {
  ColumnDef,
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  flexRender,
  SortingState,
} from '@tanstack/react-table';
import { usePayments, useDeletePayment } from '@/hooks/api/usePayments';
import { useInvoices } from '@/hooks/api/useInvoices';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCustomers } from '@/hooks/api/useCustomers';
import { Payment, Invoice, Company, Customer } from '@/services/api/types';
import { Modal } from '@/components/ui/Modal';
import { PageSizeSelect } from '@/components/ui/PageSizeSelect';
import { formatINR } from '@/lib/financialUtils';
import { formatCurrency } from '@/lib/currency';
import { Eye, Trash2, Plus, FileText, Banknote, ChevronDown, X, Search } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { RecordPaymentModal } from '@/components/modals/RecordPaymentModal';
import { DirectPaymentForm } from '@/components/forms/DirectPaymentForm';
import { useQueryStates, parseAsString, parseAsInteger } from 'nuqs';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';

const PaymentsManagement = () => {
  const [deletingPayment, setDeletingPayment] = useState<Payment | null>(null);
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);
  const [showDirectPaymentForm, setShowDirectPaymentForm] = useState(false);
  const [showPaymentMenu, setShowPaymentMenu] = useState(false);
  const [sorting, setSorting] = useState<SortingState>([]);
  const navigate = useNavigate();

  // URL-backed state with nuqs - persists filters on refresh
  const currentYear = new Date().getFullYear();
  const [urlState, setUrlState] = useQueryStates(
    {
      search: parseAsString.withDefault(''),
      companyId: parseAsString.withDefault(''),
      customerId: parseAsString.withDefault(''),
      paymentType: parseAsString.withDefault(''),
      year: parseAsInteger.withDefault(0), // 0 means all years
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
    },
    { history: 'replace' }
  );

  // Generate year options (current year ± 3 years)
  const yearOptions = Array.from({ length: 7 }, (_, i) => currentYear - 3 + i);

  const { data: paymentsData, isLoading, error, refetch } = usePayments({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
  });

  const { data: invoices = [] } = useInvoices();
  const { data: companies = [] } = useCompanies();
  const { data: customers = [] } = useCustomers();
  const deletePayment = useDeletePayment();

  const payments = paymentsData?.items || [];

  const handleViewInvoice = (invoiceId?: string) => {
    if (invoiceId) {
      navigate(`/invoices/${invoiceId}`);
    }
  };

  const handleDelete = (payment: Payment) => {
    setDeletingPayment(payment);
  };

  const handleDeleteConfirm = async () => {
    if (deletingPayment) {
      try {
        await deletePayment.mutateAsync(deletingPayment.id);
        setDeletingPayment(null);
        refetch();
      } catch (error) {
        console.error('Failed to delete payment:', error);
      }
    }
  };

  const handlePaymentSuccess = () => {
    setSelectedInvoiceId(null);
    refetch();
  };

  // Helper functions
  const getInvoiceNumber = (invoiceId?: string) => {
    if (!invoiceId) return '—';
    const invoice = invoices.find((inv: Invoice) => inv.id === invoiceId);
    return invoice?.invoiceNumber || '—';
  };

  const getCompanyName = (payment: Payment) => {
    // First try to get from payment's companyId
    if (payment.companyId) {
      const company = companies.find((c: Company) => c.id === payment.companyId);
      if (company) return company.name;
    }
    // Fallback to invoice's company
    if (payment.invoiceId) {
      const invoice = invoices.find((inv: Invoice) => inv.id === payment.invoiceId);
      if (invoice?.companyId) {
        const company = companies.find((c: Company) => c.id === invoice.companyId);
        return company?.name || '—';
      }
    }
    return '—';
  };

  const getCustomerName = (payment: Payment) => {
    if (payment.customerId) {
      const customer = customers.find((c: Customer) => c.id === payment.customerId);
      return customer?.name || '—';
    }
    return '—';
  };

  const getPaymentCurrency = (payment: Payment) => {
    // First use payment's currency if available
    if (payment.currency) return payment.currency;
    // Fallback to invoice's currency
    if (payment.invoiceId) {
      const invoice = invoices.find((inv: Invoice) => inv.id === payment.invoiceId);
      return invoice?.currency || 'INR';
    }
    return 'INR';
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-IN', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
    });
  };

  const formatPaymentType = (type?: string) => {
    if (!type) return '—';
    return type.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  };

  // Filter payments based on URL state
  const filteredPayments = useMemo(() => {
    return payments.filter((payment) => {
      // Company filter
      const companyMatches = !urlState.companyId || payment.companyId === urlState.companyId;

      // Customer filter
      const customerMatches = !urlState.customerId || payment.customerId === urlState.customerId;

      // Payment type filter
      const typeMatches = !urlState.paymentType || payment.paymentType === urlState.paymentType;

      // Year filter (0 means all years)
      let yearMatches = true;
      if (urlState.year && urlState.year > 0) {
        const paymentDate = new Date(payment.paymentDate);
        yearMatches = paymentDate.getFullYear() === urlState.year;
      }

      // Search filter
      const searchLower = urlState.search.toLowerCase();
      const searchMatches = !searchLower ||
        payment.description?.toLowerCase().includes(searchLower) ||
        payment.referenceNumber?.toLowerCase().includes(searchLower) ||
        getCustomerName(payment).toLowerCase().includes(searchLower) ||
        getCompanyName(payment).toLowerCase().includes(searchLower) ||
        getInvoiceNumber(payment.invoiceId).toLowerCase().includes(searchLower);

      return companyMatches && customerMatches && typeMatches && yearMatches && searchMatches;
    });
  }, [payments, urlState.companyId, urlState.customerId, urlState.paymentType, urlState.year, urlState.search, companies, customers, invoices]);

  // Calculate totals for filtered payments
  const totals = useMemo(() => {
    const result = {
      count: filteredPayments.length,
      grossAmount: 0,
      tdsAmount: 0,
      netAmount: 0,
      amountInInr: 0,
    };

    filteredPayments.forEach((payment) => {
      result.grossAmount += payment.grossAmount || payment.amount || 0;
      result.tdsAmount += payment.tdsAmount || 0;
      result.netAmount += payment.amount || 0;
      result.amountInInr += payment.amountInInr || 0;
    });

    return result;
  }, [filteredPayments]);

  // Check if any filters are active
  const hasActiveFilters = urlState.search || urlState.companyId || urlState.customerId || urlState.paymentType || urlState.year > 0;

  // Clear all filters
  const clearFilters = () => {
    setUrlState({
      search: '',
      companyId: '',
      customerId: '',
      paymentType: '',
      year: 0,
      page: 1,
    });
  };

  const columns: ColumnDef<Payment>[] = [
    {
      accessorKey: 'paymentDate',
      header: 'Date',
      cell: ({ row }) => formatDate(row.original.paymentDate),
    },
    {
      accessorKey: 'paymentType',
      header: 'Type',
      cell: ({ row }) => {
        const type = row.original.paymentType;
        const isInvoicePayment = row.original.invoiceId && !type;
        return (
          <span className={`px-2 py-1 text-xs rounded-full ${
            type === 'direct_income' ? 'bg-green-100 text-green-800' :
            type === 'advance_received' ? 'bg-blue-100 text-blue-800' :
            isInvoicePayment ? 'bg-purple-100 text-purple-800' :
            'bg-gray-100 text-gray-800'
          }`}>
            {isInvoicePayment ? 'Invoice' : formatPaymentType(type)}
          </span>
        );
      },
    },
    {
      accessorKey: 'invoiceId',
      header: 'Invoice/Description',
      cell: ({ row }) => {
        if (row.original.invoiceId) {
          return (
            <button
              onClick={() => handleViewInvoice(row.original.invoiceId)}
              className="text-blue-600 hover:text-blue-800 underline"
            >
              {getInvoiceNumber(row.original.invoiceId)}
            </button>
          );
        }
        return row.original.description || '—';
      },
    },
    {
      accessorKey: 'companyId',
      header: 'Company',
      cell: ({ row }) => getCompanyName(row.original),
    },
    {
      accessorKey: 'customerId',
      header: 'Customer',
      cell: ({ row }) => getCustomerName(row.original),
    },
    {
      accessorKey: 'grossAmount',
      header: 'Gross',
      cell: ({ row }) => {
        const currency = getPaymentCurrency(row.original);
        const gross = row.original.grossAmount || row.original.amount;
        return formatCurrency(gross, currency);
      },
    },
    {
      accessorKey: 'tdsAmount',
      header: 'TDS',
      cell: ({ row }) => {
        if (row.original.tdsApplicable && row.original.tdsAmount) {
          const currency = getPaymentCurrency(row.original);
          return (
            <span className="text-red-600">
              -{formatCurrency(row.original.tdsAmount, currency)}
              {row.original.tdsSection && (
                <span className="text-xs text-gray-500 ml-1">({row.original.tdsSection})</span>
              )}
            </span>
          );
        }
        return <span className="text-gray-400">—</span>;
      },
    },
    {
      accessorKey: 'amount',
      header: 'Net Received',
      cell: ({ row }) => {
        const currency = getPaymentCurrency(row.original);
        return (
          <span className="font-medium text-green-700">
            {formatCurrency(row.original.amount, currency)}
          </span>
        );
      },
    },
    {
      accessorKey: 'amountInInr',
      header: 'INR',
      cell: ({ row }) => {
        const inrAmount = row.original.amountInInr;
        if (inrAmount != null && inrAmount > 0) {
          return formatINR(inrAmount);
        }
        return <span className="text-gray-400">—</span>;
      },
    },
    {
      accessorKey: 'paymentMethod',
      header: 'Method',
      cell: ({ row }) => {
        const method = row.original.paymentMethod;
        return method ? method.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase()) : '—';
      },
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex items-center space-x-2">
          {row.original.invoiceId && (
            <button
              onClick={() => handleViewInvoice(row.original.invoiceId)}
              className="text-blue-600 hover:text-blue-800"
              title="View Invoice"
            >
              <Eye className="w-4 h-4" />
            </button>
          )}
          <button
            onClick={() => handleDelete(row.original)}
            className="text-red-600 hover:text-red-800"
            title="Delete Payment"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      ),
    },
  ];

  // Set up react-table
  const table = useReactTable({
    data: filteredPayments,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onSortingChange: setSorting,
    state: {
      sorting,
      pagination: {
        pageIndex: urlState.page - 1,
        pageSize: urlState.pageSize,
      },
    },
    onPaginationChange: (updater) => {
      const current = { pageIndex: urlState.page - 1, pageSize: urlState.pageSize };
      const next = typeof updater === 'function' ? updater(current) : updater;
      setUrlState({ page: next.pageIndex + 1, pageSize: next.pageSize });
    },
    manualPagination: false,
  });

  if (error) {
    return (
      <div className="p-6">
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          Error loading payments: {error instanceof Error ? error.message : 'Unknown error'}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 p-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Payments Management</h1>
          <p className="text-gray-600 mt-2">View and manage all payments (invoice and direct)</p>
        </div>
        <div className="relative">
          <button
            onClick={() => setShowPaymentMenu(!showPaymentMenu)}
            className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
          >
            <Plus className="w-4 h-4" />
            <span>Record Payment</span>
            <ChevronDown className="w-4 h-4" />
          </button>

          {showPaymentMenu && (
            <>
              <div
                className="fixed inset-0 z-10"
                onClick={() => setShowPaymentMenu(false)}
              />
              <div className="absolute right-0 mt-2 w-56 bg-white rounded-lg shadow-lg border z-20">
                <button
                  onClick={() => {
                    setShowPaymentMenu(false);
                    setShowDirectPaymentForm(true);
                  }}
                  className="w-full px-4 py-3 text-left hover:bg-gray-50 flex items-center space-x-3 rounded-t-lg"
                >
                  <Banknote className="w-5 h-5 text-green-600" />
                  <div>
                    <div className="font-medium">Direct Payment</div>
                    <div className="text-xs text-gray-500">Payment not linked to invoice</div>
                  </div>
                </button>
                <button
                  onClick={() => {
                    setShowPaymentMenu(false);
                    navigate('/invoices');
                  }}
                  className="w-full px-4 py-3 text-left hover:bg-gray-50 flex items-center space-x-3 border-t rounded-b-lg"
                >
                  <FileText className="w-5 h-5 text-purple-600" />
                  <div>
                    <div className="font-medium">Invoice Payment</div>
                    <div className="text-xs text-gray-500">Go to invoices to record</div>
                  </div>
                </button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Filters Section */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="flex flex-wrap gap-4 items-center">
          {/* Search Input */}
          <div className="relative flex-1 min-w-[200px] max-w-md">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              type="text"
              placeholder="Search payments..."
              value={urlState.search}
              onChange={(e) => setUrlState({ search: e.target.value, page: 1 })}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          {/* Company Filter */}
          <CompanyFilterDropdown
            value={urlState.companyId}
            onChange={(value) => setUrlState({ companyId: value, page: 1 })}
          />

          {/* Customer Filter */}
          <select
            value={urlState.customerId}
            onChange={(e) => setUrlState({ customerId: e.target.value, page: 1 })}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">All Customers</option>
            {customers.map((customer: Customer) => (
              <option key={customer.id} value={customer.id}>
                {customer.name}
              </option>
            ))}
          </select>

          {/* Payment Type Filter */}
          <select
            value={urlState.paymentType}
            onChange={(e) => setUrlState({ paymentType: e.target.value, page: 1 })}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">All Types</option>
            <option value="invoice_payment">Invoice Payment</option>
            <option value="direct_income">Direct Income</option>
            <option value="advance_received">Advance Received</option>
            <option value="refund_received">Refund Received</option>
          </select>

          {/* Year Filter */}
          <select
            value={urlState.year}
            onChange={(e) => setUrlState({ year: parseInt(e.target.value) || 0, page: 1 })}
            className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            <option value={0}>All Years</option>
            {yearOptions.map((year) => (
              <option key={year} value={year}>
                {year}
              </option>
            ))}
          </select>

          {/* Clear Filters Button */}
          {hasActiveFilters && (
            <button
              onClick={clearFilters}
              className="flex items-center gap-1 px-3 py-2 text-sm text-gray-600 hover:text-gray-800 hover:bg-gray-100 rounded-lg transition-colors"
            >
              <X className="h-4 w-4" />
              Clear Filters
            </button>
          )}
        </div>

        {/* Results count */}
        <div className="mt-3 text-sm text-gray-500">
          Showing {filteredPayments.length} of {payments.length} payments
          {hasActiveFilters && ' (filtered)'}
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg shadow">
        {isLoading ? (
          <div className="flex justify-center items-center p-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          </div>
        ) : (
          <>
            <div className="rounded-md border overflow-hidden">
              <table className="w-full">
                <thead className="bg-gray-50">
                  {table.getHeaderGroups().map((headerGroup) => (
                    <tr key={headerGroup.id}>
                      {headerGroup.headers.map((header) => (
                        <th
                          key={header.id}
                          className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                          onClick={header.column.getToggleSortingHandler()}
                        >
                          <div className="flex items-center space-x-1">
                            <span>
                              {header.isPlaceholder
                                ? null
                                : flexRender(header.column.columnDef.header, header.getContext())}
                            </span>
                            <span className="text-gray-400">
                              {header.column.getIsSorted() === 'desc' ? '↓' :
                               header.column.getIsSorted() === 'asc' ? '↑' :
                               header.column.getCanSort() ? '↕' : null}
                            </span>
                          </div>
                        </th>
                      ))}
                    </tr>
                  ))}
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {table.getRowModel().rows?.length ? (
                    table.getRowModel().rows.map((row) => (
                      <tr
                        key={row.id}
                        className="hover:bg-gray-50 transition-colors"
                      >
                        {row.getVisibleCells().map((cell) => (
                          <td key={cell.id} className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </td>
                        ))}
                      </tr>
                    ))
                  ) : (
                    <tr>
                      <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500">
                        No payments found.
                      </td>
                    </tr>
                  )}
                </tbody>
                {/* Totals Row */}
                {table.getFilteredRowModel().rows.length > 0 && (
                  <tfoot className="bg-gray-100 border-t-2 border-gray-300">
                    <tr className="font-semibold">
                      <td className="px-6 py-4 text-sm text-gray-900">
                        Totals ({totals.count} payments)
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                      <td className="px-6 py-4 text-sm text-gray-900">
                        <div className="font-bold">{formatINR(totals.grossAmount)}</div>
                      </td>
                      <td className="px-6 py-4 text-sm text-red-600">
                        <div className="font-bold">-{formatINR(totals.tdsAmount)}</div>
                      </td>
                      <td className="px-6 py-4 text-sm text-green-600">
                        <div className="font-bold">{formatINR(totals.netAmount)}</div>
                      </td>
                      <td className="px-6 py-4 text-sm text-purple-600">
                        <div className="font-bold">{formatINR(totals.amountInInr)}</div>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                      <td className="px-6 py-4 text-sm text-gray-900"></td>
                    </tr>
                  </tfoot>
                )}
              </table>
            </div>

            {/* Pagination */}
            <div className="flex items-center justify-between p-4 border-t">
              <div className="flex items-center space-x-2">
                <span className="text-sm text-gray-700">
                  Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount() || 1}
                </span>
                <span className="text-sm text-gray-500">
                  ({table.getFilteredRowModel().rows.length} total rows)
                </span>
                <PageSizeSelect
                  value={table.getState().pagination.pageSize}
                  onChange={(size) => setUrlState({ pageSize: size, page: 1 })}
                />
              </div>
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => table.previousPage()}
                  disabled={!table.getCanPreviousPage()}
                  className="px-3 py-1 border rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                <button
                  onClick={() => table.nextPage()}
                  disabled={!table.getCanNextPage()}
                  className="px-3 py-1 border rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingPayment}
        onClose={() => setDeletingPayment(null)}
        title="Delete Payment"
        size="sm"
      >
        <div className="space-y-4">
          <p>Are you sure you want to delete this payment?</p>
          {deletingPayment && (
            <div className="bg-gray-50 p-4 rounded">
              <p className="text-sm">
                <strong>Date:</strong> {formatDate(deletingPayment.paymentDate)}
              </p>
              <p className="text-sm">
                <strong>Amount:</strong> {formatCurrency(deletingPayment.amount, getPaymentCurrency(deletingPayment))}
              </p>
              {deletingPayment.amountInInr && (
                <p className="text-sm">
                  <strong>Amount (INR):</strong> {formatINR(deletingPayment.amountInInr)}
                </p>
              )}
            </div>
          )}
          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setDeletingPayment(null)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleDeleteConfirm}
              disabled={deletePayment.isPending}
              className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50"
            >
              {deletePayment.isPending ? 'Deleting...' : 'Delete'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Payment Modal (if invoice selected) */}
      {selectedInvoiceId && (
        <RecordPaymentModal
          isOpen={!!selectedInvoiceId}
          onClose={() => setSelectedInvoiceId(null)}
          invoice={invoices.find((inv: Invoice) => inv.id === selectedInvoiceId)!}
          onSuccess={handlePaymentSuccess}
        />
      )}

      {/* Direct Payment Form Modal */}
      <DirectPaymentForm
        isOpen={showDirectPaymentForm}
        onClose={() => setShowDirectPaymentForm(false)}
        onSuccess={() => {
          setShowDirectPaymentForm(false);
          refetch();
        }}
      />
    </div>
  );
};

export default PaymentsManagement;




