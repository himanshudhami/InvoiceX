import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { useStockMovementsPaged } from '@/features/inventory/hooks';
import type { StockMovement, StockMovementFilterParams } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Drawer } from '@/components/ui/Drawer';
import { StockMovementForm } from '@/components/forms/StockMovementForm';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { ArrowDownCircle, ArrowUpCircle, Plus } from 'lucide-react';

const StockMovementsManagement = () => {
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [companyFilter, setCompanyFilter] = useState<string>('');
  const [fromDate, setFromDate] = useState<string>('');
  const [toDate, setToDate] = useState<string>('');
  const [pageNumber, setPageNumber] = useState(1);
  const pageSize = 20;

  const params: StockMovementFilterParams = useMemo(
    () => ({
      companyId: companyFilter || undefined,
      fromDate: fromDate || undefined,
      toDate: toDate || undefined,
      pageNumber,
      pageSize,
    }),
    [companyFilter, fromDate, toDate, pageNumber]
  );

  const { data: pagedData, isLoading, error, refetch } = useStockMovementsPaged(params);

  const movements = pagedData?.items ?? [];
  const totalCount = pagedData?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    refetch();
  };

  const formatCurrency = (value: number | undefined) => {
    if (value === undefined) return '-';
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(value);
  };

  const getMovementTypeConfig = (type: string) => {
    const configs: Record<string, { label: string; color: string; direction: 'in' | 'out' }> = {
      purchase: { label: 'Purchase', color: 'bg-green-100 text-green-800', direction: 'in' },
      sale: { label: 'Sale', color: 'bg-red-100 text-red-800', direction: 'out' },
      transfer_in: { label: 'Transfer In', color: 'bg-blue-100 text-blue-800', direction: 'in' },
      transfer_out: { label: 'Transfer Out', color: 'bg-orange-100 text-orange-800', direction: 'out' },
      adjustment: { label: 'Adjustment', color: 'bg-purple-100 text-purple-800', direction: 'in' },
      opening: { label: 'Opening', color: 'bg-gray-100 text-gray-800', direction: 'in' },
      return_in: { label: 'Sales Return', color: 'bg-teal-100 text-teal-800', direction: 'in' },
      return_out: { label: 'Purchase Return', color: 'bg-amber-100 text-amber-800', direction: 'out' },
    };
    return configs[type] || { label: type, color: 'bg-gray-100 text-gray-800', direction: 'in' };
  };

  const columns: ColumnDef<StockMovement>[] = [
    {
      accessorKey: 'movementDate',
      header: 'Date',
      cell: ({ row }) => {
        const date = row.getValue('movementDate') as string;
        return (
          <div className="text-sm text-gray-900">
            {new Date(date).toLocaleDateString('en-IN', {
              day: '2-digit',
              month: 'short',
              year: 'numeric',
            })}
          </div>
        );
      },
    },
    {
      accessorKey: 'movementType',
      header: 'Type',
      cell: ({ row }) => {
        const type = row.getValue('movementType') as string;
        const config = getMovementTypeConfig(type);
        return (
          <div className="flex items-center gap-2">
            {config.direction === 'in' ? (
              <ArrowDownCircle className="h-4 w-4 text-green-600" />
            ) : (
              <ArrowUpCircle className="h-4 w-4 text-red-600" />
            )}
            <span className={`px-2 py-1 text-xs font-medium rounded-full ${config.color}`}>
              {config.label}
            </span>
          </div>
        );
      },
    },
    {
      accessorKey: 'stockItemName',
      header: 'Item',
      cell: ({ row }) => {
        const movement = row.original;
        return (
          <div>
            <div className="font-medium text-gray-900">{movement.stockItemName}</div>
            {movement.stockItemSku && (
              <div className="text-sm text-gray-500">{movement.stockItemSku}</div>
            )}
          </div>
        );
      },
    },
    {
      accessorKey: 'warehouseName',
      header: 'Warehouse',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">{row.getValue('warehouseName')}</div>
      ),
    },
    {
      accessorKey: 'quantity',
      header: 'Quantity',
      cell: ({ row }) => {
        const qty = row.getValue('quantity') as number;
        const unit = row.original.unitSymbol;
        const isPositive = qty > 0;
        return (
          <div className={`font-medium ${isPositive ? 'text-green-600' : 'text-red-600'}`}>
            {isPositive ? '+' : ''}
            {qty.toFixed(2)} {unit}
          </div>
        );
      },
    },
    {
      accessorKey: 'rate',
      header: 'Rate',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">
          {formatCurrency(row.getValue('rate'))}
        </div>
      ),
    },
    {
      accessorKey: 'value',
      header: 'Value',
      cell: ({ row }) => (
        <div className="font-medium text-gray-900">
          {formatCurrency(row.original.value)}
        </div>
      ),
    },
    {
      accessorKey: 'sourceNumber',
      header: 'Reference',
      cell: ({ row }) => {
        const movement = row.original;
        return movement.sourceNumber ? (
          <div className="text-sm text-gray-900">{movement.sourceNumber}</div>
        ) : (
          <div className="text-sm text-gray-400">-</div>
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
        <div className="text-red-600 mb-4">Failed to load stock movements</div>
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
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Stock Movements</h1>
        <p className="text-gray-600 mt-2">Track all inventory transactions and adjustments</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex flex-wrap items-end gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown value={companyFilter} onChange={setCompanyFilter} />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">From Date</label>
              <input
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">To Date</label>
              <input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div className="ml-auto">
              <button
                onClick={() => setIsCreateDrawerOpen(true)}
                className="px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90 flex items-center gap-2"
              >
                <Plus size={16} />
                Record Movement
              </button>
            </div>
          </div>

          <DataTable
            columns={columns}
            data={movements}
            searchPlaceholder="Search movements..."
          />

          {totalPages > 1 && (
            <div className="mt-4 flex items-center justify-between border-t pt-4">
              <div className="text-sm text-gray-500">
                Showing {(pageNumber - 1) * pageSize + 1} to{' '}
                {Math.min(pageNumber * pageSize, totalCount)} of {totalCount} entries
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  disabled={pageNumber === 1}
                  className="px-3 py-1 text-sm border rounded-md disabled:opacity-50 hover:bg-gray-50"
                >
                  Previous
                </button>
                <span className="px-3 py-1 text-sm">
                  Page {pageNumber} of {totalPages}
                </span>
                <button
                  onClick={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
                  disabled={pageNumber === totalPages}
                  className="px-3 py-1 text-sm border rounded-md disabled:opacity-50 hover:bg-gray-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Record Stock Movement"
        size="lg"
      >
        <StockMovementForm
          companyId={companyFilter || undefined}
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>
    </div>
  );
};

export default StockMovementsManagement;
