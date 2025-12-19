import { useState, useMemo, useEffect } from 'react';
import {
  ColumnDef,
} from '@tanstack/react-table';
import { usePayments, useDeletePayment } from '@/hooks/api/usePayments';
import { useInvoices } from '@/features/invoices/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCustomers } from '@/features/customers/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
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
import { cn } from '@/lib/utils';
import { CustomerSelect } from '@/components/ui/CustomerSelect';

const PaymentsManagement = () => {
  const [deletingPayment, setDeletingPayment] = useState<Payment | null>(null);
  const [selectedInvoiceId, setSelectedInvoiceId] = useState<string | null>(null);
  const [showDirectPaymentForm, setShowDirectPaymentForm] = useState(false);
  const [showPaymentMenu, setShowPaymentMenu] = useState(false);
  const navigate = useNavigate();

  // Get selected company from context (for multi-company users)
  const { selectedCompanyId, hasMultiCompanyAccess } = useCompanyContext();

  // URL-backed state with nuqs - persists filters on refresh
  const [urlState, setUrlState] = useQueryStates(
    {
      search: parseAsString.withDefault(''),
      company: parseAsString.withDefault(''),
      customerId: parseAsString.withDefault(''),
      paymentType: parseAsString.withDefault(''),
      page: parseAsInteger.withDefault(1),
      pageSize: parseAsInteger.withDefault(100),
    },
    { history: 'replace' }
  );

  // Determine effective company ID: URL filter takes precedence, then context selection
  const effectiveCompanyId = urlState.company || (hasMultiCompanyAccess ? selectedCompanyId : undefined);

  // Debounced search term
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState(urlState.search);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(urlState.search);
    }, 300);
    return () => clearTimeout(timer);
  }, [urlState.search]);

  // Server-side paginated data
  const { data: paymentsData, isLoading, error, refetch } = usePayments({
    pageNumber: urlState.page,
    pageSize: urlState.pageSize,
    searchTerm: debouncedSearchTerm || undefined,
    companyId: effectiveCompanyId || undefined,
    customerId: urlState.customerId || undefined,
    paymentType: urlState.paymentType || undefined,
  });

  const { data: invoices = [] } = useInvoices();
  const { data: companies = [] } = useCompanies();
  const { data: customers = [] } = useCustomers(effectiveCompanyId || undefined);
  const deletePayment = useDeletePayment();

  // Extract items and pagination info from response
  const payments = paymentsData?.items ?? [];
  const totalCount = paymentsData?.totalCount ?? 0;
  const totalPages = paymentsData?.totalPages ?? 1;

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
    if (payment.companyId) {
      const company = companies.find((c: Company) => c.id === payment.companyId);
      if (company) return company.name;
    }
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
    if (payment.currency) return payment.currency;
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

  // Calculate totals from current page data
  const totals = useMemo(() => {
    const result = {
      count: payments.length,
      grossAmount: 0,
      tdsAmount: 0,
      netAmount: 0,
      amountInInr: 0,
    };

    payments.forEach((payment) => {
      result.grossAmount += payment.grossAmount || payment.amount || 0;
      result.tdsAmount += payment.tdsAmount || 0;
      result.netAmount += payment.amount || 0;
      result.amountInInr += payment.amountInInr || 0;
    });

    return result;
  }, [payments]);

  // Check if any filters are active
  const hasActiveFilters = urlState.search || urlState.company || urlState.customerId || urlState.paymentType;

  // Clear all filters
  const clearFilters = () => {
    setUrlState({
      search: '',
      company: '',
      customerId: '',
      paymentType: '',
      page: 1,
    });
  };

  const columns = useMemo<ColumnDef<Payment>[]>(() => [
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
  ], [invoices, companies, customers]);

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

      {/* Summary Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">Total Payments</div>
          <div className="text-2xl font-bold text-gray-900">{totalCount}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Gross</div>
          <div className="text-2xl font-bold text-gray-900">{formatINR(totals.grossAmount)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page TDS</div>
          <div className="text-2xl font-bold text-red-600">-{formatINR(totals.tdsAmount)}</div>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <div className="text-sm font-medium text-gray-500">This Page Net</div>
          <div className="text-2xl font-bold text-green-600">{formatINR(totals.netAmount)}</div>
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
            value={urlState.company}
            onChange={(value) => setUrlState({ company: value || '', page: 1 })}
          />

          {/* Customer Filter */}
          <CustomerSelect
            customers={customers}
            value={urlState.customerId}
            onChange={(val) => setUrlState({ customerId: val, page: 1 })}
            placeholder="All customers"
            className="min-w-[220px]"
            disabled={!effectiveCompanyId}
            showAllOption
            allOptionLabel="All customers"
          />

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
          Showing {payments.length} payments on this page ({totalCount} total)
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
                  <tr>
                    {columns.map((column) => (
                      <th
                        key={column.id || (column as any).accessorKey}
                        className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider"
                      >
                        {typeof column.header === 'string' ? column.header : ''}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {payments.length > 0 ? (
                    payments.map((payment) => (
                      <tr key={payment.id} className="hover:bg-gray-50 transition-colors">
                        {columns.map((column) => (
                          <td
                            key={`${payment.id}-${column.id || (column as any).accessorKey}`}
                            className="px-6 py-4 whitespace-nowrap text-sm text-gray-900"
                          >
                            {column.cell
                              ? (column.cell as any)({ row: { original: payment } })
                              : (payment as any)[(column as any).accessorKey]}
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
                {payments.length > 0 && (
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
                  Page {urlState.page} of {totalPages}
                </span>
                <span className="text-sm text-gray-500">
                  ({totalCount} total payments)
                </span>
                <PageSizeSelect
                  value={urlState.pageSize}
                  onChange={(size) => setUrlState({ pageSize: size, page: 1 })}
                />
              </div>
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => setUrlState({ page: urlState.page - 1 })}
                  disabled={urlState.page <= 1}
                  className={cn(
                    "px-3 py-1 rounded-md text-sm transition-colors",
                    urlState.page > 1
                      ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                      : "bg-gray-100 text-gray-400 cursor-not-allowed"
                  )}
                >
                  Previous
                </button>

                <div className="flex items-center space-x-1">
                  {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                    const startPage = Math.max(1, urlState.page - 2);
                    const page = startPage + i;

                    if (page > totalPages) return null;

                    return (
                      <button
                        key={page}
                        onClick={() => setUrlState({ page })}
                        className={cn(
                          "w-8 h-8 rounded text-sm transition-colors",
                          page === urlState.page
                            ? "bg-primary text-primary-foreground"
                            : "bg-gray-200 hover:bg-gray-300 text-gray-700"
                        )}
                      >
                        {page}
                      </button>
                    );
                  })}
                </div>

                <button
                  onClick={() => setUrlState({ page: urlState.page + 1 })}
                  disabled={urlState.page >= totalPages}
                  className={cn(
                    "px-3 py-1 rounded-md text-sm transition-colors",
                    urlState.page < totalPages
                      ? "bg-gray-200 hover:bg-gray-300 text-gray-700"
                      : "bg-gray-100 text-gray-400 cursor-not-allowed"
                  )}
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
