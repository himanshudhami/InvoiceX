import React, { useState, useMemo } from 'react';
import { format } from 'date-fns';
import {
  History,
  Download,
  Eye,
  X,
  ChevronLeft,
  ChevronRight,
  Filter,
  RefreshCw,
} from 'lucide-react';
import { useCompanyContext } from '@/contexts/CompanyContext';
import {
  useAuditTrailList,
  useAuditTrailDetail,
  useAuditEntityTypes,
  useExportAuditTrail,
  useAuditStats,
  getOperationColor,
  formatEntityType,
} from '@/features/audit/hooks/useAuditTrail';
import { AuditDiffViewer } from '@/components/audit/AuditDiffViewer';
import { AuditTrailQueryParams } from '@/services/api/admin/auditService';

const OPERATION_OPTIONS = [
  { value: '', label: 'All Operations' },
  { value: 'create', label: 'Created' },
  { value: 'update', label: 'Updated' },
  { value: 'delete', label: 'Deleted' },
];

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

export default function AuditTrailViewer() {
  const { selectedCompanyId } = useCompanyContext();

  // Filter state
  const [filters, setFilters] = useState<AuditTrailQueryParams>({
    companyId: selectedCompanyId || '',
    pageNumber: 1,
    pageSize: 20,
  });

  // Update companyId when context changes
  React.useEffect(() => {
    if (selectedCompanyId) {
      setFilters((prev) => ({ ...prev, companyId: selectedCompanyId }));
    }
  }, [selectedCompanyId]);

  // Detail modal state
  const [selectedEntryId, setSelectedEntryId] = useState<string | null>(null);

  // Queries
  const { data: auditData, isLoading, refetch } = useAuditTrailList(filters);
  const { data: entryDetail, isLoading: isLoadingDetail } = useAuditTrailDetail(selectedEntryId);
  const { data: entityTypes } = useAuditEntityTypes();
  const { data: stats } = useAuditStats(filters.companyId, filters.fromDate, filters.toDate);
  const exportMutation = useExportAuditTrail();

  // Entity type options with formatted labels
  const entityTypeOptions = useMemo(() => {
    return [
      { value: '', label: 'All Entities' },
      ...(entityTypes || []).map((type) => ({
        value: type,
        label: formatEntityType(type),
      })),
    ];
  }, [entityTypes]);

  // Handle filter changes
  const updateFilter = (key: keyof AuditTrailQueryParams, value: string | number) => {
    setFilters((prev) => ({
      ...prev,
      [key]: value,
      ...(key !== 'pageNumber' && { pageNumber: 1 }), // Reset page when filter changes
    }));
  };

  // Handle export
  const handleExport = () => {
    if (!filters.companyId) return;

    const fromDate = filters.fromDate || format(new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), 'yyyy-MM-dd');
    const toDate = filters.toDate || format(new Date(), 'yyyy-MM-dd');

    exportMutation.mutate({
      companyId: filters.companyId,
      fromDate,
      toDate,
      entityType: filters.entityType,
    });
  };

  // Pagination
  const totalPages = auditData?.totalPages || 0;
  const currentPage = filters.pageNumber || 1;

  if (!selectedCompanyId) {
    return (
      <div className="p-6">
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 text-yellow-800">
          Please select a company to view audit trail.
        </div>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div className="flex items-center gap-3">
          <History className="w-8 h-8 text-gray-600" />
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Audit Trail</h1>
            <p className="text-sm text-gray-500">MCA-compliant change history</p>
          </div>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => refetch()}
            className="flex items-center gap-2 px-3 py-2 text-gray-600 hover:text-gray-800 border rounded-lg hover:bg-gray-50"
          >
            <RefreshCw className="w-4 h-4" />
            Refresh
          </button>
          <button
            onClick={handleExport}
            disabled={exportMutation.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            <Download className="w-4 h-4" />
            {exportMutation.isPending ? 'Exporting...' : 'Export CSV'}
          </button>
        </div>
      </div>

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-4 gap-4">
          <div className="bg-white rounded-lg border p-4">
            <div className="text-2xl font-bold text-gray-900">{stats.totalEntries}</div>
            <div className="text-sm text-gray-500">Total Entries</div>
          </div>
          <div className="bg-green-50 rounded-lg border border-green-200 p-4">
            <div className="text-2xl font-bold text-green-700">{stats.createCount}</div>
            <div className="text-sm text-green-600">Created</div>
          </div>
          <div className="bg-blue-50 rounded-lg border border-blue-200 p-4">
            <div className="text-2xl font-bold text-blue-700">{stats.updateCount}</div>
            <div className="text-sm text-blue-600">Updated</div>
          </div>
          <div className="bg-red-50 rounded-lg border border-red-200 p-4">
            <div className="text-2xl font-bold text-red-700">{stats.deleteCount}</div>
            <div className="text-sm text-red-600">Deleted</div>
          </div>
        </div>
      )}

      {/* Filters */}
      <div className="bg-white rounded-lg border p-4">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="w-4 h-4 text-gray-500" />
          <span className="text-sm font-medium text-gray-700">Filters</span>
        </div>
        <div className="grid grid-cols-5 gap-4">
          <select
            value={filters.entityType || ''}
            onChange={(e) => updateFilter('entityType', e.target.value)}
            className="border rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            {entityTypeOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>

          <select
            value={filters.operation || ''}
            onChange={(e) => updateFilter('operation', e.target.value)}
            className="border rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          >
            {OPERATION_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>

          <input
            type="date"
            value={filters.fromDate || ''}
            onChange={(e) => updateFilter('fromDate', e.target.value)}
            className="border rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="From Date"
          />

          <input
            type="date"
            value={filters.toDate || ''}
            onChange={(e) => updateFilter('toDate', e.target.value)}
            className="border rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="To Date"
          />

          <input
            type="text"
            value={filters.search || ''}
            onChange={(e) => updateFilter('search', e.target.value)}
            className="border rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Search..."
          />
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg border overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Timestamp
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Entity
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Operation
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Changed By
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Changes
              </th>
              <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-500">
                  Loading...
                </td>
              </tr>
            ) : auditData?.items.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-gray-500">
                  No audit entries found
                </td>
              </tr>
            ) : (
              auditData?.items.map((entry) => (
                <tr key={entry.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-600">
                    {format(new Date(entry.createdAt), 'dd MMM yyyy HH:mm:ss')}
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-sm font-medium text-gray-900">
                      {entry.entityDisplayName || entry.entityId.slice(0, 8)}
                    </div>
                    <div className="text-xs text-gray-500">
                      {formatEntityType(entry.entityType)}
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span
                      className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getOperationColor(
                        entry.operation
                      )}`}
                    >
                      {entry.operation.charAt(0).toUpperCase() + entry.operation.slice(1)}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="text-sm text-gray-900">{entry.actorName || 'Unknown'}</div>
                    <div className="text-xs text-gray-500">{entry.actorIp}</div>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-600">
                    {entry.changedFields?.length || 0} fields
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => setSelectedEntryId(entry.id)}
                      className="text-blue-600 hover:text-blue-800"
                      title="View Details"
                    >
                      <Eye className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {totalPages > 0 && (
          <div className="px-4 py-3 bg-gray-50 border-t flex items-center justify-between">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              <span>Rows per page:</span>
              <select
                value={filters.pageSize}
                onChange={(e) => updateFilter('pageSize', parseInt(e.target.value))}
                className="border rounded px-2 py-1"
              >
                {PAGE_SIZE_OPTIONS.map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
              <span className="ml-4">
                {((currentPage - 1) * (filters.pageSize || 20)) + 1}-
                {Math.min(currentPage * (filters.pageSize || 20), auditData?.totalCount || 0)} of{' '}
                {auditData?.totalCount || 0}
              </span>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => updateFilter('pageNumber', currentPage - 1)}
                disabled={currentPage <= 1}
                className="p-1 rounded hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronLeft className="w-5 h-5" />
              </button>
              <span className="text-sm text-gray-600">
                Page {currentPage} of {totalPages}
              </span>
              <button
                onClick={() => updateFilter('pageNumber', currentPage + 1)}
                disabled={currentPage >= totalPages}
                className="p-1 rounded hover:bg-gray-200 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ChevronRight className="w-5 h-5" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Detail Modal */}
      {selectedEntryId && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-4xl w-full mx-4 max-h-[90vh] overflow-hidden">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h2 className="text-lg font-semibold text-gray-900">Audit Entry Details</h2>
              <button
                onClick={() => setSelectedEntryId(null)}
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6 overflow-y-auto max-h-[calc(90vh-120px)]">
              {isLoadingDetail ? (
                <div className="text-center py-8 text-gray-500">Loading...</div>
              ) : entryDetail ? (
                <div className="space-y-6">
                  {/* Metadata */}
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-xs text-gray-500 uppercase">Entity</label>
                      <div className="text-sm font-medium">
                        {formatEntityType(entryDetail.entityType)}
                      </div>
                      <div className="text-xs text-gray-500">
                        {entryDetail.entityDisplayName || entryDetail.entityId}
                      </div>
                    </div>
                    <div>
                      <label className="text-xs text-gray-500 uppercase">Operation</label>
                      <div>
                        <span
                          className={`inline-flex px-2 py-1 text-xs font-medium rounded-full ${getOperationColor(
                            entryDetail.operation
                          )}`}
                        >
                          {entryDetail.operation.charAt(0).toUpperCase() +
                            entryDetail.operation.slice(1)}
                        </span>
                      </div>
                    </div>
                    <div>
                      <label className="text-xs text-gray-500 uppercase">Changed By</label>
                      <div className="text-sm">{entryDetail.actorName || 'Unknown'}</div>
                      <div className="text-xs text-gray-500">{entryDetail.actorEmail}</div>
                    </div>
                    <div>
                      <label className="text-xs text-gray-500 uppercase">Timestamp</label>
                      <div className="text-sm">
                        {format(new Date(entryDetail.createdAt), 'dd MMM yyyy HH:mm:ss')}
                      </div>
                    </div>
                    <div>
                      <label className="text-xs text-gray-500 uppercase">IP Address</label>
                      <div className="text-sm font-mono">{entryDetail.actorIp || '-'}</div>
                    </div>
                    <div>
                      <label className="text-xs text-gray-500 uppercase">Correlation ID</label>
                      <div className="text-sm font-mono text-xs">
                        {entryDetail.correlationId || '-'}
                      </div>
                    </div>
                  </div>

                  {/* Diff Viewer */}
                  <AuditDiffViewer
                    oldValues={entryDetail.oldValues}
                    newValues={entryDetail.newValues}
                    changedFields={entryDetail.changedFields}
                    operation={entryDetail.operation}
                  />
                </div>
              ) : (
                <div className="text-center py-8 text-gray-500">Entry not found</div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
