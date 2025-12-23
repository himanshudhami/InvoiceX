import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useForm26Q,
  useForm26QSummary,
  useForm24Q,
  useForm24QSummary,
  useChallans,
  useChallanReconciliation,
  useTdsReturnDueDates,
  usePendingTdsReturns,
  useTdsFilingHistory,
  useMarkReturnFiled,
  useCombinedTdsSummary,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { Form26QDeducteeEntry, Form24QDeducteeEntry, ChallanEntry } from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import {
  FileText,
  Users,
  Briefcase,
  Receipt,
  CheckCircle,
  Clock,
  AlertTriangle,
  Calendar,
  Download,
  Eye,
  FileCheck,
  Building2,
  CreditCard,
  TrendingUp,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Generate financial year options
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth() + 1;
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1;

  const years = [];
  for (let i = 0; i < 5; i++) {
    const year = startYear - i;
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`,
    });
  }
  return years;
};

const QUARTERS = [
  { value: 'Q1', label: 'Q1 (Apr-Jun)' },
  { value: 'Q2', label: 'Q2 (Jul-Sep)' },
  { value: 'Q3', label: 'Q3 (Oct-Dec)' },
  { value: 'Q4', label: 'Q4 (Jan-Mar)' },
];

type TabType = '26Q' | '24Q' | 'challans' | 'filing';

const TdsReturnsManagement = () => {
  const financialYears = generateFinancialYears();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedFY, setSelectedFY] = useState(financialYears[0].value);
  const [selectedQuarter, setSelectedQuarter] = useState('Q3');
  const [activeTab, setActiveTab] = useState<TabType>('26Q');

  const [markFiledEntry, setMarkFiledEntry] = useState<{ formType: string; quarter: string } | null>(null);
  const [filedDetails, setFiledDetails] = useState({
    filingDate: new Date().toISOString().split('T')[0],
    acknowledgementNumber: '',
    tokenNumber: '',
  });

  const { data: companies = [] } = useCompanies();

  // Fetch Form 26Q data
  const { data: form26QData, isLoading: loading26Q } = useForm26Q(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId && activeTab === '26Q'
  );

  const { data: form26QSummary } = useForm26QSummary(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId && activeTab === '26Q'
  );

  // Fetch Form 24Q data
  const { data: form24QData, isLoading: loading24Q } = useForm24Q(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId && activeTab === '24Q'
  );

  const { data: form24QSummary } = useForm24QSummary(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId && activeTab === '24Q'
  );

  // Fetch Challans
  const { data: challans = [], isLoading: loadingChallans } = useChallans(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    undefined,
    !!selectedCompanyId && activeTab === 'challans'
  );

  const { data: reconciliation } = useChallanReconciliation(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId && activeTab === 'challans'
  );

  // Fetch Filing History
  const { data: filingHistory = [], isLoading: loadingFiling } = useTdsFilingHistory(
    selectedCompanyId,
    selectedFY,
    !!selectedCompanyId && activeTab === 'filing'
  );

  // Fetch Due Dates
  const { data: dueDates = [] } = useTdsReturnDueDates(selectedFY);

  // Combined Summary
  const { data: combinedSummary } = useCombinedTdsSummary(
    selectedCompanyId,
    selectedFY,
    selectedQuarter,
    !!selectedCompanyId
  );

  const markReturnFiled = useMarkReturnFiled();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const handleMarkFiled = (formType: string, quarter: string) => {
    setMarkFiledEntry({ formType, quarter });
    setFiledDetails({
      filingDate: new Date().toISOString().split('T')[0],
      acknowledgementNumber: '',
      tokenNumber: '',
    });
  };

  const handleMarkFiledConfirm = async () => {
    if (markFiledEntry && filedDetails.acknowledgementNumber) {
      try {
        await markReturnFiled.mutateAsync({
          companyId: selectedCompanyId,
          formType: markFiledEntry.formType,
          financialYear: selectedFY,
          quarter: markFiledEntry.quarter,
          filingDate: filedDetails.filingDate,
          acknowledgementNumber: filedDetails.acknowledgementNumber,
          tokenNumber: filedDetails.tokenNumber || undefined,
        });
        setMarkFiledEntry(null);
      } catch (error) {
        console.error('Failed to mark return as filed:', error);
      }
    }
  };

  // Form 26Q columns
  const form26QColumns: ColumnDef<Form26QDeducteeEntry>[] = [
    {
      accessorKey: 'deducteeName',
      header: 'Deductee',
      cell: ({ row }) => (
        <div className="flex items-start gap-3">
          <div className="p-2 bg-blue-100 rounded-lg">
            <Building2 className="h-5 w-5 text-blue-600" />
          </div>
          <div>
            <div className="font-medium text-gray-900">{row.original.deducteeName}</div>
            <div className="text-xs text-gray-500 font-mono">PAN: {row.original.deducteePan}</div>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'section',
      header: 'Section',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {row.original.section}
        </span>
      ),
    },
    {
      accessorKey: 'paymentDate',
      header: 'Payment Date',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">
          {new Date(row.original.paymentDate).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
          })}
        </div>
      ),
    },
    {
      accessorKey: 'amountPaid',
      header: 'Amount Paid',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.amountPaid)}
        </div>
      ),
    },
    {
      accessorKey: 'tdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => (
        <div className="text-right">
          <div className="font-medium text-green-600">{formatCurrency(row.original.tdsDeducted)}</div>
          <div className="text-xs text-gray-500">{row.original.tdsRate}%</div>
        </div>
      ),
    },
    {
      accessorKey: 'tdsDeposited',
      header: 'TDS Deposited',
      cell: ({ row }) => {
        const deposited = row.original.tdsDeposited || 0;
        const deducted = row.original.tdsDeducted;
        const isPending = deposited < deducted;
        return (
          <div className="text-right">
            <div className={cn('font-medium', isPending ? 'text-yellow-600' : 'text-green-600')}>
              {formatCurrency(deposited)}
            </div>
            {isPending && (
              <div className="text-xs text-yellow-600">
                Pending: {formatCurrency(deducted - deposited)}
              </div>
            )}
          </div>
        );
      },
    },
  ];

  // Form 24Q columns
  const form24QColumns: ColumnDef<Form24QDeducteeEntry>[] = [
    {
      accessorKey: 'employeeName',
      header: 'Employee',
      cell: ({ row }) => (
        <div className="flex items-start gap-3">
          <div className="p-2 bg-green-100 rounded-lg">
            <Users className="h-5 w-5 text-green-600" />
          </div>
          <div>
            <div className="font-medium text-gray-900">{row.original.employeeName}</div>
            <div className="text-xs text-gray-500 font-mono">PAN: {row.original.employeePan}</div>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'designation',
      header: 'Designation',
      cell: ({ row }) => (
        <span className="text-sm text-gray-700">{row.original.designation || '-'}</span>
      ),
    },
    {
      accessorKey: 'grossSalary',
      header: 'Gross Salary',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.grossSalary)}
        </div>
      ),
    },
    {
      accessorKey: 'totalDeductions',
      header: 'Deductions',
      cell: ({ row }) => (
        <div className="text-right text-gray-700">
          {formatCurrency(row.original.totalDeductions || 0)}
        </div>
      ),
    },
    {
      accessorKey: 'taxableIncome',
      header: 'Taxable Income',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.taxableIncome)}
        </div>
      ),
    },
    {
      accessorKey: 'tdsDeducted',
      header: 'TDS Deducted',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">
          {formatCurrency(row.original.tdsDeducted)}
        </div>
      ),
    },
  ];

  // Challan columns
  const challanColumns: ColumnDef<ChallanEntry>[] = [
    {
      accessorKey: 'challanNumber',
      header: 'Challan No.',
      cell: ({ row }) => (
        <div className="font-mono text-sm">{row.original.challanNumber}</div>
      ),
    },
    {
      accessorKey: 'bsrCode',
      header: 'BSR Code',
      cell: ({ row }) => (
        <span className="text-sm text-gray-700">{row.original.bsrCode}</span>
      ),
    },
    {
      accessorKey: 'depositDate',
      header: 'Deposit Date',
      cell: ({ row }) => (
        <div className="text-sm text-gray-900">
          {new Date(row.original.depositDate).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
          })}
        </div>
      ),
    },
    {
      accessorKey: 'section',
      header: 'Section',
      cell: ({ row }) => (
        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-purple-100 text-purple-800">
          {row.original.section}
        </span>
      ),
    },
    {
      accessorKey: 'amount',
      header: 'Amount',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">
          {formatCurrency(row.original.amount)}
        </div>
      ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const status = row.original.status || 'pending';
        const isVerified = status === 'verified';
        return (
          <span className={cn(
            'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium',
            isVerified ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'
          )}>
            {isVerified ? <CheckCircle className="h-3 w-3" /> : <Clock className="h-3 w-3" />}
            {isVerified ? 'Verified' : 'Pending'}
          </span>
        );
      },
    },
  ];

  const tabs = [
    { id: '26Q' as TabType, label: 'Form 26Q', icon: Briefcase, description: 'Non-Salary TDS' },
    { id: '24Q' as TabType, label: 'Form 24Q', icon: Users, description: 'Salary TDS' },
    { id: 'challans' as TabType, label: 'Challans', icon: CreditCard, description: 'Deposit Challans' },
    { id: 'filing' as TabType, label: 'Filing Status', icon: FileCheck, description: 'Return Filing' },
  ];

  const isLoading = (activeTab === '26Q' && loading26Q) ||
    (activeTab === '24Q' && loading24Q) ||
    (activeTab === 'challans' && loadingChallans) ||
    (activeTab === 'filing' && loadingFiling);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">TDS Returns Management</h1>
          <p className="text-gray-600 mt-2">
            Prepare and file Form 26Q (Non-Salary) and Form 24Q (Salary) TDS returns
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
              <option value="">Select a company</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="fyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Financial Year
            </label>
            <select
              id="fyFilter"
              value={selectedFY}
              onChange={(e) => setSelectedFY(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {financialYears.map((fy) => (
                <option key={fy.value} value={fy.value}>
                  {fy.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="quarterFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Quarter
            </label>
            <select
              id="quarterFilter"
              value={selectedQuarter}
              onChange={(e) => setSelectedQuarter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {QUARTERS.map((q) => (
                <option key={q.value} value={q.value}>
                  {q.label}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-end">
            <button
              className="w-full px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 flex items-center justify-center gap-2"
              disabled={!selectedCompanyId}
            >
              <Download className="h-4 w-4" />
              Export Return
            </button>
          </div>
        </div>
      </div>

      {!selectedCompanyId ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <Building2 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">Select a Company</h3>
          <p className="text-gray-500">
            Please select a company to view TDS returns data
          </p>
        </div>
      ) : (
        <>
          {/* Summary Cards */}
          {combinedSummary && (
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-500">Total TDS Deducted</p>
                    <p className="text-2xl font-bold text-green-600">
                      {formatCurrency(combinedSummary.totalTdsDeducted || 0)}
                    </p>
                  </div>
                  <div className="p-3 bg-green-100 rounded-full">
                    <Receipt className="h-6 w-6 text-green-600" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-500">TDS Deposited</p>
                    <p className="text-2xl font-bold text-blue-600">
                      {formatCurrency(combinedSummary.totalTdsDeposited || 0)}
                    </p>
                  </div>
                  <div className="p-3 bg-blue-100 rounded-full">
                    <CreditCard className="h-6 w-6 text-blue-600" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-500">26Q Entries</p>
                    <p className="text-2xl font-bold text-purple-600">
                      {combinedSummary.form26QCount || 0}
                    </p>
                  </div>
                  <div className="p-3 bg-purple-100 rounded-full">
                    <Briefcase className="h-6 w-6 text-purple-600" />
                  </div>
                </div>
              </div>
              <div className="bg-white rounded-lg shadow p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-500">24Q Entries</p>
                    <p className="text-2xl font-bold text-orange-600">
                      {combinedSummary.form24QCount || 0}
                    </p>
                  </div>
                  <div className="p-3 bg-orange-100 rounded-full">
                    <Users className="h-6 w-6 text-orange-600" />
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Tabs */}
          <div className="bg-white rounded-lg shadow">
            <div className="border-b border-gray-200">
              <nav className="flex -mb-px">
                {tabs.map((tab) => {
                  const Icon = tab.icon;
                  return (
                    <button
                      key={tab.id}
                      onClick={() => setActiveTab(tab.id)}
                      className={cn(
                        'flex-1 py-4 px-6 text-center border-b-2 font-medium text-sm transition-colors',
                        activeTab === tab.id
                          ? 'border-primary text-primary'
                          : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                      )}
                    >
                      <div className="flex items-center justify-center gap-2">
                        <Icon className="h-5 w-5" />
                        <span>{tab.label}</span>
                      </div>
                      <div className="text-xs text-gray-400 mt-1">{tab.description}</div>
                    </button>
                  );
                })}
              </nav>
            </div>

            <div className="p-6">
              {isLoading ? (
                <div className="flex items-center justify-center h-64">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : (
                <>
                  {activeTab === '26Q' && (
                    <div className="space-y-4">
                      {form26QSummary && (
                        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-4">
                          <h3 className="font-medium text-blue-800 mb-2">Form 26Q Summary - {selectedQuarter}</h3>
                          <div className="grid grid-cols-4 gap-4 text-sm">
                            <div>
                              <p className="text-blue-600">Total Deductees</p>
                              <p className="font-bold">{form26QSummary.totalDeductees}</p>
                            </div>
                            <div>
                              <p className="text-blue-600">Total Amount Paid</p>
                              <p className="font-bold">{formatCurrency(form26QSummary.totalAmountPaid)}</p>
                            </div>
                            <div>
                              <p className="text-blue-600">TDS Deducted</p>
                              <p className="font-bold">{formatCurrency(form26QSummary.totalTdsDeducted)}</p>
                            </div>
                            <div>
                              <p className="text-blue-600">TDS Deposited</p>
                              <p className="font-bold">{formatCurrency(form26QSummary.totalTdsDeposited)}</p>
                            </div>
                          </div>
                        </div>
                      )}
                      <DataTable
                        columns={form26QColumns}
                        data={form26QData?.deducteeEntries || []}
                        searchPlaceholder="Search by deductee name or PAN..."
                      />
                    </div>
                  )}

                  {activeTab === '24Q' && (
                    <div className="space-y-4">
                      {form24QSummary && (
                        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-4">
                          <h3 className="font-medium text-green-800 mb-2">Form 24Q Summary - {selectedQuarter}</h3>
                          <div className="grid grid-cols-4 gap-4 text-sm">
                            <div>
                              <p className="text-green-600">Total Employees</p>
                              <p className="font-bold">{form24QSummary.totalEmployees}</p>
                            </div>
                            <div>
                              <p className="text-green-600">Gross Salary</p>
                              <p className="font-bold">{formatCurrency(form24QSummary.totalGrossSalary)}</p>
                            </div>
                            <div>
                              <p className="text-green-600">Total Deductions</p>
                              <p className="font-bold">{formatCurrency(form24QSummary.totalDeductions)}</p>
                            </div>
                            <div>
                              <p className="text-green-600">TDS Deducted</p>
                              <p className="font-bold">{formatCurrency(form24QSummary.totalTdsDeducted)}</p>
                            </div>
                          </div>
                        </div>
                      )}
                      <DataTable
                        columns={form24QColumns}
                        data={form24QData?.deducteeEntries || []}
                        searchPlaceholder="Search by employee name or PAN..."
                      />
                    </div>
                  )}

                  {activeTab === 'challans' && (
                    <div className="space-y-4">
                      {reconciliation && (
                        <div className={cn(
                          'border rounded-lg p-4 mb-4',
                          reconciliation.isReconciled
                            ? 'bg-green-50 border-green-200'
                            : 'bg-yellow-50 border-yellow-200'
                        )}>
                          <h3 className={cn(
                            'font-medium mb-2',
                            reconciliation.isReconciled ? 'text-green-800' : 'text-yellow-800'
                          )}>
                            Challan Reconciliation - {selectedQuarter}
                          </h3>
                          <div className="grid grid-cols-3 gap-4 text-sm">
                            <div>
                              <p className={reconciliation.isReconciled ? 'text-green-600' : 'text-yellow-600'}>
                                TDS Deducted
                              </p>
                              <p className="font-bold">{formatCurrency(reconciliation.totalTdsDeducted)}</p>
                            </div>
                            <div>
                              <p className={reconciliation.isReconciled ? 'text-green-600' : 'text-yellow-600'}>
                                Challans Deposited
                              </p>
                              <p className="font-bold">{formatCurrency(reconciliation.totalChallansDeposited)}</p>
                            </div>
                            <div>
                              <p className={reconciliation.isReconciled ? 'text-green-600' : 'text-yellow-600'}>
                                Difference
                              </p>
                              <p className={cn(
                                'font-bold',
                                reconciliation.difference === 0 ? 'text-green-600' : 'text-red-600'
                              )}>
                                {formatCurrency(reconciliation.difference)}
                              </p>
                            </div>
                          </div>
                        </div>
                      )}
                      <DataTable
                        columns={challanColumns}
                        data={challans}
                        searchPlaceholder="Search by challan number..."
                      />
                    </div>
                  )}

                  {activeTab === 'filing' && (
                    <div className="space-y-4">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {/* Form 26Q Filing Status */}
                        <div className="border rounded-lg p-4">
                          <h3 className="font-medium text-gray-900 mb-4 flex items-center gap-2">
                            <Briefcase className="h-5 w-5 text-purple-600" />
                            Form 26Q Filing Status
                          </h3>
                          <div className="space-y-3">
                            {QUARTERS.map((q) => {
                              const filing = filingHistory.find(
                                f => f.formType === '26Q' && f.quarter === q.value
                              );
                              const isFiled = !!filing?.filingDate;
                              return (
                                <div
                                  key={q.value}
                                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                                >
                                  <div className="flex items-center gap-3">
                                    {isFiled ? (
                                      <CheckCircle className="h-5 w-5 text-green-600" />
                                    ) : (
                                      <Clock className="h-5 w-5 text-yellow-600" />
                                    )}
                                    <div>
                                      <p className="font-medium">{q.label}</p>
                                      {isFiled && filing?.acknowledgementNumber && (
                                        <p className="text-xs text-gray-500">
                                          Ack: {filing.acknowledgementNumber}
                                        </p>
                                      )}
                                    </div>
                                  </div>
                                  {!isFiled && (
                                    <button
                                      onClick={() => handleMarkFiled('26Q', q.value)}
                                      className="px-3 py-1 text-sm bg-primary text-white rounded hover:bg-primary/90"
                                    >
                                      Mark Filed
                                    </button>
                                  )}
                                </div>
                              );
                            })}
                          </div>
                        </div>

                        {/* Form 24Q Filing Status */}
                        <div className="border rounded-lg p-4">
                          <h3 className="font-medium text-gray-900 mb-4 flex items-center gap-2">
                            <Users className="h-5 w-5 text-green-600" />
                            Form 24Q Filing Status
                          </h3>
                          <div className="space-y-3">
                            {QUARTERS.map((q) => {
                              const filing = filingHistory.find(
                                f => f.formType === '24Q' && f.quarter === q.value
                              );
                              const isFiled = !!filing?.filingDate;
                              return (
                                <div
                                  key={q.value}
                                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
                                >
                                  <div className="flex items-center gap-3">
                                    {isFiled ? (
                                      <CheckCircle className="h-5 w-5 text-green-600" />
                                    ) : (
                                      <Clock className="h-5 w-5 text-yellow-600" />
                                    )}
                                    <div>
                                      <p className="font-medium">{q.label}</p>
                                      {isFiled && filing?.acknowledgementNumber && (
                                        <p className="text-xs text-gray-500">
                                          Ack: {filing.acknowledgementNumber}
                                        </p>
                                      )}
                                    </div>
                                  </div>
                                  {!isFiled && (
                                    <button
                                      onClick={() => handleMarkFiled('24Q', q.value)}
                                      className="px-3 py-1 text-sm bg-primary text-white rounded hover:bg-primary/90"
                                    >
                                      Mark Filed
                                    </button>
                                  )}
                                </div>
                              );
                            })}
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          </div>
        </>
      )}

      {/* Mark Filed Modal */}
      <Modal
        isOpen={!!markFiledEntry}
        onClose={() => setMarkFiledEntry(null)}
        title={`Mark ${markFiledEntry?.formType} ${markFiledEntry?.quarter} as Filed`}
        size="md"
      >
        {markFiledEntry && (
          <div className="space-y-4">
            <div>
              <label htmlFor="filingDate" className="block text-sm font-medium text-gray-700 mb-1">
                Filing Date *
              </label>
              <input
                id="filingDate"
                type="date"
                value={filedDetails.filingDate}
                onChange={(e) => setFiledDetails({ ...filedDetails, filingDate: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
            <div>
              <label htmlFor="ackNumber" className="block text-sm font-medium text-gray-700 mb-1">
                Acknowledgement Number *
              </label>
              <input
                id="ackNumber"
                type="text"
                value={filedDetails.acknowledgementNumber}
                onChange={(e) => setFiledDetails({ ...filedDetails, acknowledgementNumber: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Enter acknowledgement number"
              />
            </div>
            <div>
              <label htmlFor="tokenNumber" className="block text-sm font-medium text-gray-700 mb-1">
                Token Number
              </label>
              <input
                id="tokenNumber"
                type="text"
                value={filedDetails.tokenNumber}
                onChange={(e) => setFiledDetails({ ...filedDetails, tokenNumber: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Enter token number (optional)"
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setMarkFiledEntry(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleMarkFiledConfirm}
                disabled={markReturnFiled.isPending || !filedDetails.acknowledgementNumber || !filedDetails.filingDate}
                className="px-4 py-2 text-sm font-medium text-white bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
              >
                {markReturnFiled.isPending ? 'Saving...' : 'Mark as Filed'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default TdsReturnsManagement;
