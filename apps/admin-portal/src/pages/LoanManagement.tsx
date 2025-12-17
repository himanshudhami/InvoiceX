import { useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { Loan } from '../services/api/types';
import {
  useLoans,
  useCreateLoan,
  useUpdateLoan,
  useDeleteLoan,
  useLoanSchedule,
} from '@/hooks/api/useLoans';
import { useCompanies } from '@/hooks/api/useCompanies';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { DataTable } from '../components/ui/DataTable';
import { Drawer } from '@/components/ui/Drawer';
import { LoanForm } from '@/components/forms/LoanForm';
import { LoanScheduleView } from '@/components/loans/LoanScheduleView';
import { LoanInterestCertificate } from '@/components/loans/LoanInterestCertificate';
import { Edit, Trash2, Calendar, FileText } from 'lucide-react';
import { formatINR } from '@/lib/financialUtils';

const getStatusBadgeColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'active':
      return 'bg-green-100 text-green-800';
    case 'closed':
      return 'bg-gray-100 text-gray-800';
    case 'foreclosed':
      return 'bg-blue-100 text-blue-800';
    case 'defaulted':
      return 'bg-red-100 text-red-800';
    default:
      return 'bg-gray-100 text-gray-800';
  }
};

const LoanManagement = () => {
  const { data, isLoading, error, refetch } = useLoans({ pageNumber: 1, pageSize: 50 });
  const { data: companies = [] } = useCompanies();
  const createLoan = useCreateLoan();
  const updateLoan = useUpdateLoan();
  const deleteLoan = useDeleteLoan();

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [editing, setEditing] = useState<Loan | null>(null);
  const [toDelete, setToDelete] = useState<Loan | null>(null);
  const [viewingSchedule, setViewingSchedule] = useState<Loan | null>(null);
  const [viewMode, setViewMode] = useState<'loans' | 'interest-certificate'>('loans');
  const [selectedCompanyId, setSelectedCompanyId] = useState<string | undefined>(undefined);
  const currentYear = new Date().getFullYear();
  const [financialYear, setFinancialYear] = useState<number>(currentYear);

  const closeDrawer = () => {
    setIsDrawerOpen(false);
    setEditing(null);
  };

  const handleDelete = async () => {
    if (toDelete) {
      try {
        await deleteLoan.mutateAsync(toDelete.id);
        setToDelete(null);
        refetch();
      } catch (error) {
        console.error('Failed to delete loan:', error);
      }
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Loan Management</h1>
          <p className="text-gray-600 mt-2">Manage loans, EMI schedules, and payments</p>
        </div>
        {/* View Toggle */}
        <div className="inline-flex rounded-md border border-gray-300 overflow-hidden">
          <button
            onClick={() => setViewMode('loans')}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              viewMode === 'loans'
                ? 'bg-primary text-white'
                : 'bg-white text-gray-700 hover:bg-gray-50'
            }`}
          >
            Loans
          </button>
          <button
            onClick={() => setViewMode('interest-certificate')}
            className={`px-4 py-2 text-sm font-medium transition-colors ${
              viewMode === 'interest-certificate'
                ? 'bg-primary text-white'
                : 'bg-white text-gray-700 hover:bg-gray-50'
            }`}
          >
            Interest Certificate
          </button>
        </div>
      </div>

      {viewMode === 'interest-certificate' ? (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Interest Certificate Settings</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Financial Year
                  </label>
                  <select
                    value={financialYear}
                    onChange={(e) => setFinancialYear(parseInt(e.target.value))}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    {Array.from({ length: 5 }, (_, i) => currentYear - 2 + i).map((year) => (
                      <option key={year} value={year}>
                        FY {year}-{year + 1}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Company (Optional)
                  </label>
                  <select
                    value={selectedCompanyId || ''}
                    onChange={(e) => setSelectedCompanyId(e.target.value || undefined)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    <option value="">All Companies</option>
                    {companies.map((company) => (
                      <option key={company.id} value={company.id}>
                        {company.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
            </CardContent>
          </Card>
          <LoanInterestCertificate companyId={selectedCompanyId} financialYear={financialYear} />
        </div>
      ) : (
        <>

      <div className="bg-white rounded-lg shadow">
        <div className="p-6">
          <DataTable
            columns={
              [
                { header: 'Loan Name', accessorKey: 'loanName' },
                { header: 'Lender', accessorKey: 'lenderName' },
                {
                  header: 'Type',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return <span className="capitalize">{loan.loanType?.replace('_', ' ')}</span>;
                  },
                },
                {
                  header: 'Principal',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return <div className="text-right">{formatINR(loan.principalAmount)}</div>;
                  },
                },
                {
                  header: 'Interest Rate',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return <div className="text-right">{loan.interestRate}%</div>;
                  },
                },
                {
                  header: 'EMI',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return <div className="text-right">{formatINR(loan.emiAmount)}</div>;
                  },
                },
                {
                  header: 'Outstanding',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return <div className="text-right">{formatINR(loan.outstandingPrincipal)}</div>;
                  },
                },
                {
                  header: 'Status',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return (
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${getStatusBadgeColor(loan.status)}`}>
                        {loan.status}
                      </span>
                    );
                  },
                },
                {
                  header: 'Actions',
                  cell: ({ row }) => {
                    const loan = row.original as Loan;
                    return (
                      <div className="flex items-center space-x-2">
                        <button
                          onClick={() => setViewingSchedule(loan)}
                          className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                          title="View Schedule"
                        >
                          <Calendar size={16} />
                        </button>
                        <button
                          onClick={() => {
                            setEditing(loan);
                            setIsDrawerOpen(true);
                          }}
                          className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                          title="Edit"
                        >
                          <Edit size={16} />
                        </button>
                        <button
                          onClick={() => setToDelete(loan)}
                          className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
                          title="Delete"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    );
                  },
                },
              ] as ColumnDef<Loan, any>[]
            }
            data={data?.items ?? []}
            searchPlaceholder="Search loans..."
            onAdd={() => {
              setEditing(null);
              setIsDrawerOpen(true);
            }}
            addButtonText="Add Loan"
          />
        </div>
      </div>

      <Drawer
        isOpen={isDrawerOpen}
        title={editing ? 'Edit Loan' : 'Add Loan'}
        onClose={closeDrawer}
      >
        <LoanForm
          loan={editing ?? undefined}
          onSuccess={() => {
            closeDrawer();
            refetch();
          }}
          onCancel={closeDrawer}
        />
      </Drawer>

      <Drawer
        isOpen={!!viewingSchedule}
        title={viewingSchedule ? `EMI Schedule - ${viewingSchedule.loanName}` : 'EMI Schedule'}
        onClose={() => setViewingSchedule(null)}
        size="large"
      >
        {viewingSchedule && <LoanScheduleView loanId={viewingSchedule.id} />}
      </Drawer>

      {toDelete && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Delete Loan</h3>
            <p className="text-gray-600 mb-6">
              Are you sure you want to delete "{toDelete.loanName}"? This action cannot be undone.
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setToDelete(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-md hover:bg-red-700"
              >
                Delete
              </button>
            </div>
          </div>
        </div>
      )}
        </>
      )}
    </div>
  );
};

export default LoanManagement;

