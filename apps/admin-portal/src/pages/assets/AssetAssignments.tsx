import { useMemo, useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { AssetAssignment } from '@/services/api/types';
import { useAllAssetAssignments, useReturnAssetAssignment } from '@/hooks/api/useAssets';
import { useAssets } from '@/hooks/api/useAssets';
import { useEmployees } from '@/hooks/api/useEmployees';
import { useCompanies } from '@/hooks/api/useCompanies';
import CompanyFilterDropdown from '@/components/ui/CompanyFilterDropdown';
import { DataTable } from '../components/ui/DataTable';

type AssignmentWithAsset = AssetAssignment & {
  asset?: any;
  employeeName?: string;
  companyName?: string;
};

const AssetAssignmentsPage = () => {
  const { data: assignments = [], isLoading, refetch } = useAllAssetAssignments();
  const { data: assetsData } = useAssets({ pageNumber: 1, pageSize: 100 });
  const { data: employees = [] } = useEmployees();
  const { data: companies = [] } = useCompanies();
  const returnAsset = useReturnAssetAssignment();

  const filteredEmployees = useMemo(() => {
    if (!companyFilter) return employees;
    return employees.filter(e => e.companyId === companyFilter);
  }, [employees, companyFilter]);

  const employeeMap = useMemo(() => new Map(filteredEmployees.map(e => [e.id, e])), [filteredEmployees]);
  const companyMap = useMemo(() => new Map(companies.map(c => [c.id, c])), [companies]);
  const assetMap = useMemo(() => new Map(assetsData?.items?.map(a => [a.id, a]) || []), [assetsData]);

  const enrichedAssignments: AssignmentWithAsset[] = useMemo(() => {
    return assignments.map(a => ({
      ...a,
      asset: assetMap.get(a.assetId),
      employeeName: a.targetType === 'employee' && a.employeeId ? employeeMap.get(a.employeeId)?.employeeName : undefined,
      companyName: companyMap.get(a.companyId)?.name,
    }));
  }, [assignments, assetMap, employeeMap, companyMap]);

  const [searchTerm, setSearchTerm] = useState('');
  const [companyFilter, setCompanyFilter] = useState<string>('');

  const filteredAssignments = useMemo(() => {
    let filtered = enrichedAssignments;
    
    // Filter by company
    if (companyFilter) {
      filtered = filtered.filter(a => a.companyId === companyFilter);
    }
    
    // Filter by search term
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      filtered = filtered.filter(a => {
        const assetTag = a.asset?.assetTag?.toLowerCase() || '';
        const assetName = a.asset?.name?.toLowerCase() || '';
        const empName = a.employeeName?.toLowerCase() || '';
        const companyName = a.companyName?.toLowerCase() || '';
        return assetTag.includes(term) || assetName.includes(term) || empName.includes(term) || companyName.includes(term);
      });
    }
    
    return filtered;
  }, [enrichedAssignments, searchTerm, companyFilter]);

  const columns: ColumnDef<AssignmentWithAsset>[] = useMemo(() => [
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
              await returnAsset.mutateAsync({
                assignmentId: assignment.id,
                data: { returnedOn: new Date().toISOString().slice(0, 10) },
              });
              refetch();
            }}
            className="text-sm text-blue-600 hover:text-blue-800"
          >
            Return
          </button>
        );
      },
    },
  ], [returnAsset, refetch]);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Asset Assignments</h1>
        <p className="text-gray-600 mt-2">View and manage asset assignments by asset or employee</p>
      </div>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <div className="mb-4 flex items-center gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Company</label>
              <CompanyFilterDropdown
                value={companyFilter}
                onChange={setCompanyFilter}
              />
            </div>
          </div>
          <DataTable
            columns={columns}
            data={filteredAssignments}
            searchPlaceholder="Search by asset tag, name, or employee..."
            searchValue={searchTerm}
            onSearchChange={setSearchTerm}
          />
        </div>
      </div>
    </div>
  );
};

export default AssetAssignmentsPage;



