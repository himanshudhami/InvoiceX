import { useState, useMemo } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import {
  useAdvanceTaxAssessmentByFy,
  useAdvanceTaxTracker,
  useAdvanceTaxSchedule,
  useAdvanceTaxPayments,
  useAdvanceTaxInterest,
  useAdvanceTaxScenarios,
  useComputeAdvanceTax,
  useUpdateAdvanceTaxAssessment,
  useActivateAdvanceTax,
  useFinalizeAdvanceTax,
  useRecordAdvanceTaxPayment,
  useRecalculateSchedules,
  useRunAdvanceTaxScenario,
  useDeleteAdvanceTaxScenario,
  useRefreshYtd,
} from '@/features/advance-tax/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type {
  AdvanceTaxSchedule,
  AdvanceTaxPayment,
  AdvanceTaxScenario,
  CreateAdvanceTaxAssessmentRequest,
  RecordAdvanceTaxPaymentRequest,
  RunScenarioRequest,
  TaxRegime,
} from '@/services/api/types';
import { DataTable } from '@/components/ui/DataTable';
import { Modal } from '@/components/ui/Modal';
import {
  Calculator,
  Calendar,
  CreditCard,
  CheckCircle,
  Clock,
  AlertCircle,
  AlertTriangle,
  Trash2,
  Plus,
  BarChart3,
  FileText,
  ArrowRight,
  IndianRupee,
  Building2,
  Play,
  Target,
  RefreshCcw,
  Lock,
  Pencil,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Financial Year options
const generateFinancialYears = () => {
  const years = [];
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth();
  // FY starts in April
  const currentFy = currentMonth >= 3 ? currentYear : currentYear - 1;

  for (let i = 0; i < 5; i++) {
    const startYear = currentFy - i;
    const endYear = startYear + 1;
    years.push({
      value: `${startYear}-${endYear.toString().slice(-2)}`,
      label: `FY ${startYear}-${endYear.toString().slice(-2)}`,
    });
  }
  return years;
};

const TAX_REGIMES: { value: TaxRegime; label: string; rate: string }[] = [
  { value: 'normal', label: 'Normal Regime', rate: '25%' },
  { value: '115BAA', label: 'Section 115BAA', rate: '22% (25.17% effective)' },
  { value: '115BAB', label: 'Section 115BAB (Manufacturing)', rate: '15% (17.16% effective)' },
];

const QUARTER_DUE_DATES: Record<string, string> = {
  Q1: '15 June',
  Q2: '15 September',
  Q3: '15 December',
  Q4: '15 March',
};

const AdvanceTaxManagement = () => {
  const financialYears = generateFinancialYears();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedFy, setSelectedFy] = useState(financialYears[0].value);

  // Modals
  const [showComputeModal, setShowComputeModal] = useState(false);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [showScenarioModal, setShowScenarioModal] = useState(false);
  const [selectedSchedule, setSelectedSchedule] = useState<AdvanceTaxSchedule | null>(null);
  const [deletingScenario, setDeletingScenario] = useState<AdvanceTaxScenario | null>(null);

  // Form states
  const [computeForm, setComputeForm] = useState<CreateAdvanceTaxAssessmentRequest>({
    companyId: '',
    financialYear: financialYears[0].value,
    taxRegime: 'normal',
  });

  const [paymentForm, setPaymentForm] = useState<RecordAdvanceTaxPaymentRequest>({
    assessmentId: '',
    paymentDate: new Date().toISOString().split('T')[0],
    amount: 0,
    challanNumber: '',
    bsrCode: '',
    createJournalEntry: true,
  });

  const [scenarioForm, setScenarioForm] = useState<RunScenarioRequest>({
    assessmentId: '',
    scenarioName: '',
    revenueAdjustment: 0,
    expenseAdjustment: 0,
    capexImpact: 0,
    payrollChange: 0,
    otherAdjustments: 0,
  });

  const { data: companies = [] } = useCompanies();

  // Fetch assessment data
  const { data: assessment, isLoading: assessmentLoading, error: assessmentError } = useAdvanceTaxAssessmentByFy(
    selectedCompanyId,
    selectedFy,
    !!selectedCompanyId
  );

  // Fetch tracker/dashboard data
  const { data: tracker } = useAdvanceTaxTracker(
    selectedCompanyId,
    selectedFy,
    !!selectedCompanyId && !!assessment
  );

  // Fetch schedule
  const { data: schedules = [] } = useAdvanceTaxSchedule(
    assessment?.id || '',
    !!assessment?.id
  );

  // Fetch payments
  const { data: payments = [] } = useAdvanceTaxPayments(
    assessment?.id || '',
    !!assessment?.id
  );

  // Fetch interest breakdown
  const { data: interestBreakdown } = useAdvanceTaxInterest(
    assessment?.id || '',
    !!assessment?.id
  );

  // Fetch scenarios
  const { data: scenarios = [] } = useAdvanceTaxScenarios(
    assessment?.id || '',
    !!assessment?.id
  );

  // Mutations
  const computeTax = useComputeAdvanceTax();
  const activateTax = useActivateAdvanceTax();
  const finalizeTax = useFinalizeAdvanceTax();
  const recordPayment = useRecordAdvanceTaxPayment();
  const recalculateSchedules = useRecalculateSchedules();
  const runScenario = useRunAdvanceTaxScenario();
  const deleteScenario = useDeleteAdvanceTaxScenario();
  const refreshYtd = useRefreshYtd();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  };

  const getStatusBadge = (status: string) => {
    const config: Record<string, { color: string; icon: typeof Clock; label: string }> = {
      draft: { color: 'bg-gray-100 text-gray-800', icon: FileText, label: 'Draft' },
      active: { color: 'bg-blue-100 text-blue-800', icon: Play, label: 'Active' },
      finalized: { color: 'bg-green-100 text-green-800', icon: CheckCircle, label: 'Finalized' },
    };
    const cfg = config[status] || config.draft;
    const Icon = cfg.icon;
    return (
      <span className={cn('inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium', cfg.color)}>
        <Icon className="h-3 w-3" />
        {cfg.label}
      </span>
    );
  };

  const getPaymentStatusBadge = (status: string, isOverdue: boolean) => {
    if (isOverdue && status !== 'paid') {
      return (
        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
          <AlertCircle className="h-3 w-3" />
          Overdue
        </span>
      );
    }
    const config: Record<string, { color: string; label: string }> = {
      pending: { color: 'bg-yellow-100 text-yellow-800', label: 'Pending' },
      partial: { color: 'bg-orange-100 text-orange-800', label: 'Partial' },
      paid: { color: 'bg-green-100 text-green-800', label: 'Paid' },
    };
    const cfg = config[status] || config.pending;
    return (
      <span className={cn('inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium', cfg.color)}>
        {cfg.label}
      </span>
    );
  };

  const handleOpenComputeModal = () => {
    setComputeForm({
      companyId: selectedCompanyId,
      financialYear: selectedFy,
      taxRegime: 'normal',
    });
    setShowComputeModal(true);
  };

  const handleCompute = async () => {
    try {
      await computeTax.mutateAsync(computeForm);
      setShowComputeModal(false);
    } catch (error) {
      console.error('Failed to compute advance tax:', error);
    }
  };

  const handleOpenPaymentModal = (schedule: AdvanceTaxSchedule) => {
    if (!assessment) return;
    setSelectedSchedule(schedule);
    setPaymentForm({
      assessmentId: assessment.id,
      scheduleId: schedule.id,
      paymentDate: new Date().toISOString().split('T')[0],
      amount: schedule.taxPayableThisQuarter - schedule.taxPaidThisQuarter,
      challanNumber: '',
      bsrCode: '',
      createJournalEntry: true,
    });
    setShowPaymentModal(true);
  };

  const handleRecordPayment = async () => {
    try {
      await recordPayment.mutateAsync(paymentForm);
      setShowPaymentModal(false);
      setSelectedSchedule(null);
    } catch (error) {
      console.error('Failed to record payment:', error);
    }
  };

  const handleOpenScenarioModal = () => {
    if (!assessment) return;
    setScenarioForm({
      assessmentId: assessment.id,
      scenarioName: '',
      revenueAdjustment: 0,
      expenseAdjustment: 0,
      capexImpact: 0,
      payrollChange: 0,
      otherAdjustments: 0,
    });
    setShowScenarioModal(true);
  };

  const handleRunScenario = async () => {
    try {
      await runScenario.mutateAsync(scenarioForm);
      setShowScenarioModal(false);
    } catch (error) {
      console.error('Failed to run scenario:', error);
    }
  };

  const handleDeleteScenario = async () => {
    if (!deletingScenario) return;
    try {
      await deleteScenario.mutateAsync(deletingScenario.id);
      setDeletingScenario(null);
    } catch (error) {
      console.error('Failed to delete scenario:', error);
    }
  };

  // Schedule columns
  const scheduleColumns: ColumnDef<AdvanceTaxSchedule>[] = [
    {
      accessorKey: 'quarterLabel',
      header: 'Quarter',
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <div className="p-2 bg-blue-100 rounded-lg">
            <Calendar className="h-4 w-4 text-blue-600" />
          </div>
          <div>
            <div className="font-medium">{row.original.quarterLabel}</div>
            <div className="text-xs text-gray-500">
              Due: {QUARTER_DUE_DATES[row.original.quarterLabel] || row.original.dueDate}
            </div>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'cumulativePercentage',
      header: 'Cumulative %',
      cell: ({ row }) => (
        <div className="text-center">
          <span className="px-2 py-1 bg-purple-100 text-purple-800 rounded-full text-xs font-medium">
            {row.original.cumulativePercentage}%
          </span>
        </div>
      ),
    },
    {
      accessorKey: 'taxPayableThisQuarter',
      header: 'Tax Payable',
      cell: ({ row }) => (
        <div className="text-right font-medium text-gray-900">
          {formatCurrency(row.original.taxPayableThisQuarter)}
        </div>
      ),
    },
    {
      accessorKey: 'taxPaidThisQuarter',
      header: 'Tax Paid',
      cell: ({ row }) => (
        <div className="text-right font-medium text-green-600">
          {formatCurrency(row.original.taxPaidThisQuarter)}
        </div>
      ),
    },
    {
      accessorKey: 'shortfallAmount',
      header: 'Shortfall',
      cell: ({ row }) => (
        <div className={cn(
          'text-right font-medium',
          row.original.shortfallAmount > 0 ? 'text-red-600' : 'text-gray-500'
        )}>
          {row.original.shortfallAmount > 0
            ? formatCurrency(row.original.shortfallAmount)
            : '-'}
        </div>
      ),
    },
    {
      accessorKey: 'interest234C',
      header: 'Interest 234C',
      cell: ({ row }) => (
        <div className={cn(
          'text-right font-medium',
          row.original.interest234C > 0 ? 'text-orange-600' : 'text-gray-500'
        )}>
          {row.original.interest234C > 0
            ? formatCurrency(row.original.interest234C)
            : '-'}
        </div>
      ),
    },
    {
      accessorKey: 'paymentStatus',
      header: 'Status',
      cell: ({ row }) => getPaymentStatusBadge(row.original.paymentStatus, row.original.isOverdue),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const schedule = row.original;
        const canPay = schedule.paymentStatus !== 'paid' && assessment?.status !== 'finalized';
        return (
          <div className="flex space-x-2">
            {canPay && (
              <button
                onClick={() => handleOpenPaymentModal(schedule)}
                className="text-blue-600 hover:text-blue-800 p-1 rounded hover:bg-blue-50 transition-colors"
                title="Record Payment"
              >
                <CreditCard size={16} />
              </button>
            )}
          </div>
        );
      },
    },
  ];

  // Payment columns
  const paymentColumns: ColumnDef<AdvanceTaxPayment>[] = [
    {
      accessorKey: 'paymentDate',
      header: 'Date',
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
      accessorKey: 'quarter',
      header: 'Quarter',
      cell: ({ row }) => (
        <span className="px-2 py-1 bg-blue-100 text-blue-800 rounded-full text-xs font-medium">
          Q{row.original.quarter || '-'}
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
      accessorKey: 'challanNumber',
      header: 'Challan No.',
      cell: ({ row }) => (
        <span className="text-gray-600 font-mono text-sm">
          {row.original.challanNumber || '-'}
        </span>
      ),
    },
    {
      accessorKey: 'journalNumber',
      header: 'Journal',
      cell: ({ row }) => (
        <span className="text-gray-600 font-mono text-sm">
          {row.original.journalNumber || '-'}
        </span>
      ),
    },
  ];

  // Scenario columns
  const scenarioColumns: ColumnDef<AdvanceTaxScenario>[] = [
    {
      accessorKey: 'scenarioName',
      header: 'Scenario',
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <div className="p-2 bg-purple-100 rounded-lg">
            <Target className="h-4 w-4 text-purple-600" />
          </div>
          <div className="font-medium">{row.original.scenarioName}</div>
        </div>
      ),
    },
    {
      accessorKey: 'adjustedTaxableIncome',
      header: 'Adjusted Taxable Income',
      cell: ({ row }) => (
        <div className="text-right font-medium">
          {formatCurrency(row.original.adjustedTaxableIncome)}
        </div>
      ),
    },
    {
      accessorKey: 'adjustedTaxLiability',
      header: 'Adjusted Tax',
      cell: ({ row }) => (
        <div className="text-right font-medium text-blue-600">
          {formatCurrency(row.original.adjustedTaxLiability)}
        </div>
      ),
    },
    {
      accessorKey: 'varianceFromBase',
      header: 'Variance',
      cell: ({ row }) => (
        <div className={cn(
          'text-right font-medium',
          row.original.varianceFromBase > 0 ? 'text-red-600' : 'text-green-600'
        )}>
          {row.original.varianceFromBase > 0 ? '+' : ''}
          {formatCurrency(row.original.varianceFromBase)}
        </div>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => (
        <button
          onClick={() => setDeletingScenario(row.original)}
          className="text-red-600 hover:text-red-800 p-1 rounded hover:bg-red-50 transition-colors"
          title="Delete Scenario"
        >
          <Trash2 size={16} />
        </button>
      ),
    },
  ];

  const hasNoAssessment = !assessmentLoading && !assessment && selectedCompanyId;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Advance Tax Management</h1>
          <p className="text-gray-600 mt-2">
            Section 207 - Corporate Advance Tax Assessment & Quarterly Payments
          </p>
        </div>
        {assessment && assessment.status === 'draft' && (
          <button
            onClick={() => activateTax.mutateAsync(assessment.id)}
            disabled={activateTax.isPending}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            <Play size={16} />
            {activateTax.isPending ? 'Activating...' : 'Activate Assessment'}
          </button>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
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
              <option value="">Select Company</option>
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
              value={selectedFy}
              onChange={(e) => setSelectedFy(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {financialYears.map((fy) => (
                <option key={fy.value} value={fy.value}>
                  {fy.label}
                </option>
              ))}
            </select>
          </div>
          {!assessment && selectedCompanyId && (
            <div className="flex items-end">
              <button
                onClick={handleOpenComputeModal}
                className="flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
              >
                <Calculator size={16} />
                Compute Assessment
              </button>
            </div>
          )}
        </div>
      </div>

      {/* No Company Selected */}
      {!selectedCompanyId && (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <Building2 className="mx-auto h-16 w-16 text-gray-300 mb-4" />
          <h3 className="text-xl font-medium text-gray-900 mb-2">Select a Company</h3>
          <p className="text-gray-500">Choose a company to view or create advance tax assessment</p>
        </div>
      )}

      {/* No Assessment */}
      {hasNoAssessment && (
        <div className="bg-white rounded-lg shadow p-12 text-center">
          <Calculator className="mx-auto h-16 w-16 text-gray-300 mb-4" />
          <h3 className="text-xl font-medium text-gray-900 mb-2">No Assessment Found</h3>
          <p className="text-gray-500 mb-4">
            No advance tax assessment exists for {selectedFy}
          </p>
          <button
            onClick={handleOpenComputeModal}
            className="flex items-center gap-2 px-6 py-3 bg-primary text-white rounded-md hover:bg-primary/90 mx-auto"
          >
            <Calculator size={18} />
            Create Assessment
          </button>
        </div>
      )}

      {/* Assessment Dashboard */}
      {assessment && (
        <>
          {/* Summary Cards */}
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">Total Tax Liability</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {formatCurrency(assessment.totalTaxLiability)}
                  </p>
                </div>
                <div className="p-3 bg-blue-100 rounded-full">
                  <Calculator className="h-6 w-6 text-blue-600" />
                </div>
              </div>
              <div className="mt-2 flex items-center gap-2">
                {getStatusBadge(assessment.status)}
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">Net Tax Payable</p>
                  <p className="text-2xl font-bold text-purple-600">
                    {formatCurrency(assessment.netTaxPayable)}
                  </p>
                </div>
                <div className="p-3 bg-purple-100 rounded-full">
                  <IndianRupee className="h-6 w-6 text-purple-600" />
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">After TDS/TCS credits</p>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">Advance Tax Paid</p>
                  <p className="text-2xl font-bold text-green-600">
                    {formatCurrency(assessment.advanceTaxAlreadyPaid)}
                  </p>
                </div>
                <div className="p-3 bg-green-100 rounded-full">
                  <CheckCircle className="h-6 w-6 text-green-600" />
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">
                {tracker?.paymentPercentage?.toFixed(1) || 0}% of liability
              </p>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-500">Total Interest</p>
                  <p className="text-2xl font-bold text-orange-600">
                    {formatCurrency(assessment.totalInterest)}
                  </p>
                </div>
                <div className="p-3 bg-orange-100 rounded-full">
                  <AlertTriangle className="h-6 w-6 text-orange-600" />
                </div>
              </div>
              <p className="text-xs text-gray-500 mt-2">234B + 234C</p>
            </div>
          </div>

          {/* Next Due Alert */}
          {tracker && tracker.nextDueDate && tracker.daysUntilNextDue <= 30 && (
            <div className={cn(
              'border rounded-lg p-4 flex items-center justify-between',
              tracker.daysUntilNextDue <= 7 ? 'bg-red-50 border-red-200' : 'bg-yellow-50 border-yellow-200'
            )}>
              <div className="flex items-center gap-3">
                <div className={cn(
                  'p-2 rounded-full',
                  tracker.daysUntilNextDue <= 7 ? 'bg-red-100' : 'bg-yellow-100'
                )}>
                  <Clock className={cn(
                    'h-5 w-5',
                    tracker.daysUntilNextDue <= 7 ? 'text-red-600' : 'text-yellow-600'
                  )} />
                </div>
                <div>
                  <p className={cn(
                    'font-medium',
                    tracker.daysUntilNextDue <= 7 ? 'text-red-800' : 'text-yellow-800'
                  )}>
                    Q{tracker.currentQuarter} Payment Due in {tracker.daysUntilNextDue} days
                  </p>
                  <p className="text-sm text-gray-600">
                    Amount: {formatCurrency(tracker.nextQuarterAmount)} | Due: {tracker.nextDueDate}
                  </p>
                </div>
              </div>
              <button className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700">
                <CreditCard size={16} />
                Pay Now
              </button>
            </div>
          )}

          {/* Tax Computation Summary */}
          <div className="bg-white rounded-lg shadow">
            <div className="p-6 border-b border-gray-200 flex justify-between items-center">
              <h2 className="text-lg font-semibold text-gray-900">Tax Computation</h2>
              {assessment.status !== 'finalized' && (
                <button
                  onClick={() => assessment && refreshYtd.mutateAsync({ assessmentId: assessment.id, autoProjectFromTrend: false })}
                  disabled={refreshYtd.isPending}
                  className="flex items-center gap-2 text-sm text-blue-600 hover:text-blue-800"
                >
                  <RefreshCcw size={14} className={refreshYtd.isPending ? 'animate-spin' : ''} />
                  {refreshYtd.isPending ? 'Refreshing...' : 'Refresh YTD'}
                </button>
              )}
            </div>
            <div className="p-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Income Section with YTD Split */}
                <div className="space-y-4">
                  <h3 className="font-medium text-gray-700">Income</h3>
                  <div className="space-y-2">
                    {/* Revenue */}
                    <div className="border border-gray-200 rounded-lg p-3 space-y-2">
                      <div className="flex justify-between items-center text-sm">
                        <span className="flex items-center gap-1 text-gray-500">
                          <Lock size={12} />
                          YTD Actual {assessment.ytdThroughDate && `(Apr-${new Date(assessment.ytdThroughDate).toLocaleDateString('en-IN', { month: 'short' })})`}
                        </span>
                        <span className="font-medium text-gray-700">{formatCurrency(assessment.ytdRevenue)}</span>
                      </div>
                      <div className="flex justify-between items-center text-sm">
                        <span className="flex items-center gap-1 text-gray-500">
                          <Pencil size={12} />
                          Projected Additional
                        </span>
                        <span className="font-medium text-blue-600">{formatCurrency(assessment.projectedAdditionalRevenue)}</span>
                      </div>
                      <div className="flex justify-between items-center pt-2 border-t border-gray-100">
                        <span className="text-gray-700 font-medium">Full Year Revenue</span>
                        <span className="font-semibold">{formatCurrency(assessment.projectedRevenue)}</span>
                      </div>
                    </div>

                    {/* Expenses */}
                    <div className="border border-gray-200 rounded-lg p-3 space-y-2">
                      <div className="flex justify-between items-center text-sm">
                        <span className="flex items-center gap-1 text-gray-500">
                          <Lock size={12} />
                          YTD Actual Expenses
                        </span>
                        <span className="font-medium text-gray-700">{formatCurrency(assessment.ytdExpenses)}</span>
                      </div>
                      <div className="flex justify-between items-center text-sm">
                        <span className="flex items-center gap-1 text-gray-500">
                          <Pencil size={12} />
                          Projected Additional
                        </span>
                        <span className="font-medium text-blue-600">{formatCurrency(assessment.projectedAdditionalExpenses)}</span>
                      </div>
                      <div className="flex justify-between items-center pt-2 border-t border-gray-100">
                        <span className="text-gray-700 font-medium">Full Year Expenses</span>
                        <span className="font-semibold text-red-600">({formatCurrency(assessment.projectedExpenses)})</span>
                      </div>
                    </div>

                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Less: Depreciation</span>
                      <span className="font-medium text-red-600">({formatCurrency(assessment.projectedDepreciation)})</span>
                    </div>
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Add: Other Income</span>
                      <span className="font-medium text-green-600">{formatCurrency(assessment.projectedOtherIncome)}</span>
                    </div>
                    <div className="flex justify-between py-2 bg-gray-50 px-2 rounded font-medium">
                      <span>Profit Before Tax</span>
                      <span>{formatCurrency(assessment.projectedProfitBeforeTax)}</span>
                    </div>
                  </div>
                </div>

                {/* Tax Calculation */}
                <div className="space-y-4">
                  <h3 className="font-medium text-gray-700">Tax Calculation ({assessment.taxRegime})</h3>
                  <div className="space-y-2">
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Base Tax @ {assessment.taxRate}%</span>
                      <span className="font-medium">{formatCurrency(assessment.baseTax)}</span>
                    </div>
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Surcharge @ {assessment.surchargeRate}%</span>
                      <span className="font-medium">{formatCurrency(assessment.surcharge)}</span>
                    </div>
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">H&E Cess @ {assessment.cessRate}%</span>
                      <span className="font-medium">{formatCurrency(assessment.cess)}</span>
                    </div>
                    <div className="flex justify-between py-2 bg-blue-50 px-2 rounded font-medium text-blue-800">
                      <span>Total Tax Liability</span>
                      <span>{formatCurrency(assessment.totalTaxLiability)}</span>
                    </div>
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Less: TDS Receivable</span>
                      <span className="font-medium text-green-600">({formatCurrency(assessment.tdsReceivable)})</span>
                    </div>
                    <div className="flex justify-between py-2 border-b border-gray-100">
                      <span className="text-gray-600">Less: TCS Credit</span>
                      <span className="font-medium text-green-600">({formatCurrency(assessment.tcsCredit)})</span>
                    </div>
                    <div className="flex justify-between py-2 bg-purple-50 px-2 rounded font-bold text-purple-800">
                      <span>Net Tax Payable</span>
                      <span>{formatCurrency(assessment.netTaxPayable)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Book Profit to Taxable Income Reconciliation */}
          <div className="bg-white rounded-lg shadow">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Book Profit to Taxable Income Reconciliation</h2>
              <p className="text-sm text-gray-500 mt-1">As per Income Tax Act provisions</p>
            </div>
            <div className="p-6">
              <div className="max-w-2xl mx-auto space-y-1">
                {/* Book Profit */}
                <div className="flex justify-between py-3 border-b border-gray-200 bg-gray-50 px-4 rounded-t-lg">
                  <span className="font-medium text-gray-800">Book Profit (as per P&L)</span>
                  <span className="font-semibold text-gray-900">{formatCurrency(assessment.bookProfit)}</span>
                </div>

                {/* Additions Section */}
                <div className="pt-4 pb-2">
                  <h4 className="text-sm font-semibold text-red-700 uppercase tracking-wide">ADD: Expenses Disallowed</h4>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Depreciation as per books</span>
                  <span className="font-medium text-red-600">{formatCurrency(assessment.addBookDepreciation)}</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Cash payments {'>'} ₹10,000 (Sec 40A(3))</span>
                  <span className="font-medium text-red-600">{formatCurrency(assessment.addDisallowed40A3)}</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Provision for gratuity (Sec 40A(7))</span>
                  <span className="font-medium text-red-600">{formatCurrency(assessment.addDisallowed40A7)}</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Unpaid statutory dues (Sec 43B)</span>
                  <span className="font-medium text-red-600">{formatCurrency(assessment.addDisallowed43B)}</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Other disallowances</span>
                  <span className="font-medium text-red-600">{formatCurrency(assessment.addOtherDisallowances)}</span>
                </div>
                <div className="flex justify-between py-2 px-4 bg-red-50 rounded">
                  <span className="font-medium text-red-800">Total Additions</span>
                  <span className="font-semibold text-red-800">{formatCurrency(assessment.totalAdditions)}</span>
                </div>

                {/* Deductions Section */}
                <div className="pt-4 pb-2">
                  <h4 className="text-sm font-semibold text-green-700 uppercase tracking-wide">LESS: Deductions Allowed</h4>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Depreciation as per IT Act</span>
                  <span className="font-medium text-green-600">({formatCurrency(assessment.lessItDepreciation)})</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Deduction u/s 80C</span>
                  <span className="font-medium text-green-600">({formatCurrency(assessment.lessDeductions80C)})</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Deduction u/s 80D</span>
                  <span className="font-medium text-green-600">({formatCurrency(assessment.lessDeductions80D)})</span>
                </div>
                <div className="flex justify-between py-2 px-4 text-sm">
                  <span className="text-gray-600">Other deductions</span>
                  <span className="font-medium text-green-600">({formatCurrency(assessment.lessOtherDeductions)})</span>
                </div>
                <div className="flex justify-between py-2 px-4 bg-green-50 rounded">
                  <span className="font-medium text-green-800">Total Deductions</span>
                  <span className="font-semibold text-green-800">({formatCurrency(assessment.totalDeductions)})</span>
                </div>

                {/* Taxable Income */}
                <div className="flex justify-between py-3 mt-4 border-t-2 border-gray-300 bg-blue-50 px-4 rounded-b-lg">
                  <span className="font-bold text-blue-900">TAXABLE INCOME</span>
                  <span className="font-bold text-blue-900">{formatCurrency(assessment.taxableIncome)}</span>
                </div>

                {/* Formula Note */}
                <div className="mt-4 text-xs text-gray-500 text-center">
                  Taxable Income = Book Profit + Total Additions - Total Deductions
                </div>
              </div>
            </div>
          </div>

          {/* Quarterly Payment Schedule */}
          <div className="bg-white rounded-lg shadow">
            <div className="p-6 border-b border-gray-200 flex justify-between items-center">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Quarterly Payment Schedule</h2>
                <p className="text-sm text-gray-500">Section 211 - Due dates and cumulative percentages</p>
              </div>
              <button
                onClick={() => assessment && recalculateSchedules.mutateAsync(assessment.id)}
                disabled={recalculateSchedules.isPending}
                className="text-sm text-blue-600 hover:text-blue-800"
              >
                {recalculateSchedules.isPending ? 'Recalculating...' : 'Recalculate'}
              </button>
            </div>
            <div className="p-6">
              <DataTable
                columns={scheduleColumns}
                data={schedules}
                searchPlaceholder="Search quarters..."
              />
            </div>
          </div>

          {/* Interest Breakdown */}
          {interestBreakdown && (assessment.interest234B > 0 || assessment.interest234C > 0) && (
            <div className="bg-white rounded-lg shadow">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">Interest Liability Breakdown</h2>
              </div>
              <div className="p-6 grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* 234B */}
                <div className="border rounded-lg p-4">
                  <h3 className="font-medium text-gray-700 mb-4">Section 234B - Shortfall in Advance Tax</h3>
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600">Assessed Tax</span>
                      <span>{formatCurrency(interestBreakdown.assessedTax)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Advance Tax Paid</span>
                      <span>{formatCurrency(interestBreakdown.advanceTaxPaid)}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600">Shortfall ({interestBreakdown.months234B} months)</span>
                      <span className="text-red-600">{formatCurrency(interestBreakdown.shortfallFor234B)}</span>
                    </div>
                    <div className="flex justify-between pt-2 border-t font-medium">
                      <span>Interest @ 1% p.m.</span>
                      <span className="text-orange-600">{formatCurrency(interestBreakdown.interest234B)}</span>
                    </div>
                  </div>
                </div>

                {/* 234C */}
                <div className="border rounded-lg p-4">
                  <h3 className="font-medium text-gray-700 mb-4">Section 234C - Deferment of Advance Tax</h3>
                  <div className="space-y-2 text-sm">
                    {interestBreakdown.quarterlyBreakdown.map((q) => (
                      <div key={q.quarter} className="flex justify-between">
                        <span className="text-gray-600">Q{q.quarter} Shortfall</span>
                        <span className={q.interest > 0 ? 'text-orange-600' : 'text-gray-500'}>
                          {formatCurrency(q.interest)}
                        </span>
                      </div>
                    ))}
                    <div className="flex justify-between pt-2 border-t font-medium">
                      <span>Total 234C Interest</span>
                      <span className="text-orange-600">{formatCurrency(interestBreakdown.totalInterest234C)}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Payment History */}
          {payments.length > 0 && (
            <div className="bg-white rounded-lg shadow">
              <div className="p-6 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">Payment History</h2>
              </div>
              <div className="p-6">
                <DataTable
                  columns={paymentColumns}
                  data={payments}
                  searchPlaceholder="Search payments..."
                />
              </div>
            </div>
          )}

          {/* Scenario Analysis */}
          <div className="bg-white rounded-lg shadow">
            <div className="p-6 border-b border-gray-200 flex justify-between items-center">
              <div>
                <h2 className="text-lg font-semibold text-gray-900">What-If Scenario Analysis</h2>
                <p className="text-sm text-gray-500">Model different business scenarios and their tax impact</p>
              </div>
              <button
                onClick={handleOpenScenarioModal}
                className="flex items-center gap-2 px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700"
              >
                <Plus size={16} />
                New Scenario
              </button>
            </div>
            <div className="p-6">
              {scenarios.length > 0 ? (
                <DataTable
                  columns={scenarioColumns}
                  data={scenarios}
                  searchPlaceholder="Search scenarios..."
                />
              ) : (
                <div className="text-center py-8 text-gray-500">
                  <BarChart3 className="mx-auto h-12 w-12 text-gray-300 mb-2" />
                  <p>No scenarios created yet. Create one to analyze tax impact of business changes.</p>
                </div>
              )}
            </div>
          </div>
        </>
      )}

      {/* Compute Assessment Modal */}
      <Modal
        isOpen={showComputeModal}
        onClose={() => setShowComputeModal(false)}
        title="Compute Advance Tax Assessment"
        size="lg"
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Company</label>
              <select
                value={computeForm.companyId}
                onChange={(e) => setComputeForm({ ...computeForm, companyId: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Select Company</option>
                {companies.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Financial Year</label>
              <select
                value={computeForm.financialYear}
                onChange={(e) => setComputeForm({ ...computeForm, financialYear: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              >
                {financialYears.map((fy) => (
                  <option key={fy.value} value={fy.value}>{fy.label}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Tax Regime</label>
            <select
              value={computeForm.taxRegime}
              onChange={(e) => setComputeForm({ ...computeForm, taxRegime: e.target.value as TaxRegime })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {TAX_REGIMES.map((regime) => (
                <option key={regime.value} value={regime.value}>
                  {regime.label} - {regime.rate}
                </option>
              ))}
            </select>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 text-sm text-blue-700">
            <strong>Note:</strong> Income projections will be computed from your P&L data. You can override these values after the assessment is created.
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">TDS Receivable (Optional)</label>
              <input
                type="number"
                value={computeForm.tdsReceivable || ''}
                onChange={(e) => setComputeForm({ ...computeForm, tdsReceivable: parseFloat(e.target.value) || undefined })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">TCS Credit (Optional)</label>
              <input
                type="number"
                value={computeForm.tcsCredit || ''}
                onChange={(e) => setComputeForm({ ...computeForm, tcsCredit: parseFloat(e.target.value) || undefined })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="0"
              />
            </div>
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <button
              onClick={() => setShowComputeModal(false)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleCompute}
              disabled={computeTax.isPending || !computeForm.companyId}
              className="px-4 py-2 text-sm font-medium text-white bg-primary border border-transparent rounded-md hover:bg-primary/90 disabled:opacity-50"
            >
              {computeTax.isPending ? 'Computing...' : 'Compute Assessment'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Record Payment Modal */}
      <Modal
        isOpen={showPaymentModal}
        onClose={() => setShowPaymentModal(false)}
        title="Record Advance Tax Payment"
        size="md"
      >
        {selectedSchedule && (
          <div className="space-y-4">
            <div className="bg-gray-50 p-4 rounded-lg">
              <p className="text-sm text-gray-600">{selectedSchedule.quarterLabel} Payment</p>
              <p className="font-medium">Due: {formatCurrency(selectedSchedule.taxPayableThisQuarter)}</p>
              <p className="text-sm text-gray-500">
                Already Paid: {formatCurrency(selectedSchedule.taxPaidThisQuarter)}
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Payment Date *</label>
              <input
                type="date"
                value={paymentForm.paymentDate}
                onChange={(e) => setPaymentForm({ ...paymentForm, paymentDate: e.target.value })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Amount *</label>
              <input
                type="number"
                value={paymentForm.amount}
                onChange={(e) => setPaymentForm({ ...paymentForm, amount: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Challan Number</label>
                <input
                  type="text"
                  value={paymentForm.challanNumber || ''}
                  onChange={(e) => setPaymentForm({ ...paymentForm, challanNumber: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="e.g., 280-ITNS"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">BSR Code</label>
                <input
                  type="text"
                  value={paymentForm.bsrCode || ''}
                  onChange={(e) => setPaymentForm({ ...paymentForm, bsrCode: e.target.value })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                  placeholder="7 digit code"
                />
              </div>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="createJournalEntry"
                checked={paymentForm.createJournalEntry}
                onChange={(e) => setPaymentForm({ ...paymentForm, createJournalEntry: e.target.checked })}
                className="h-4 w-4 text-primary border-gray-300 rounded focus:ring-primary"
              />
              <label htmlFor="createJournalEntry" className="text-sm text-gray-700">
                Create Journal Entry
              </label>
            </div>

            <div className="flex justify-end space-x-3 pt-4">
              <button
                onClick={() => setShowPaymentModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleRecordPayment}
                disabled={recordPayment.isPending || !paymentForm.amount}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {recordPayment.isPending ? 'Recording...' : 'Record Payment'}
              </button>
            </div>
          </div>
        )}
      </Modal>

      {/* Run Scenario Modal */}
      <Modal
        isOpen={showScenarioModal}
        onClose={() => setShowScenarioModal(false)}
        title="Run What-If Scenario"
        size="lg"
      >
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Scenario Name *</label>
            <input
              type="text"
              value={scenarioForm.scenarioName}
              onChange={(e) => setScenarioForm({ ...scenarioForm, scenarioName: e.target.value })}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="e.g., Revenue Growth 20%"
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Revenue Adjustment</label>
              <input
                type="number"
                value={scenarioForm.revenueAdjustment}
                onChange={(e) => setScenarioForm({ ...scenarioForm, revenueAdjustment: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="+/- amount"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Expense Adjustment</label>
              <input
                type="number"
                value={scenarioForm.expenseAdjustment}
                onChange={(e) => setScenarioForm({ ...scenarioForm, expenseAdjustment: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="+/- amount"
              />
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">CapEx Impact</label>
              <input
                type="number"
                value={scenarioForm.capexImpact}
                onChange={(e) => setScenarioForm({ ...scenarioForm, capexImpact: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="Depreciation change"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Payroll Change</label>
              <input
                type="number"
                value={scenarioForm.payrollChange}
                onChange={(e) => setScenarioForm({ ...scenarioForm, payrollChange: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="+/- amount"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Other Adjustments</label>
              <input
                type="number"
                value={scenarioForm.otherAdjustments}
                onChange={(e) => setScenarioForm({ ...scenarioForm, otherAdjustments: parseFloat(e.target.value) || 0 })}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="+/- amount"
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Assumptions (Optional)</label>
            <textarea
              value={scenarioForm.assumptions || ''}
              onChange={(e) => setScenarioForm({ ...scenarioForm, assumptions: e.target.value })}
              rows={2}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
              placeholder="Describe the assumptions behind this scenario..."
            />
          </div>

          <div className="flex justify-end space-x-3 pt-4">
            <button
              onClick={() => setShowScenarioModal(false)}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={handleRunScenario}
              disabled={runScenario.isPending || !scenarioForm.scenarioName}
              className="px-4 py-2 text-sm font-medium text-white bg-purple-600 border border-transparent rounded-md hover:bg-purple-700 disabled:opacity-50"
            >
              {runScenario.isPending ? 'Running...' : 'Run Scenario'}
            </button>
          </div>
        </div>
      </Modal>

      {/* Delete Scenario Modal */}
      <Modal
        isOpen={!!deletingScenario}
        onClose={() => setDeletingScenario(null)}
        title="Delete Scenario"
        size="sm"
      >
        {deletingScenario && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete the scenario <strong>"{deletingScenario.scenarioName}"</strong>?
            </p>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingScenario(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteScenario}
                disabled={deleteScenario.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteScenario.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default AdvanceTaxManagement;
