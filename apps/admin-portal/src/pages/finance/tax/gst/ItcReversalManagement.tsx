import { useState } from 'react';
import {
  useCalculateItcReversal,
  usePostItcReversal,
} from '@/features/gst-compliance/hooks';
import { useCompanies } from '@/hooks/api/useCompanies';
import type { ItcReversalCalculationRequest, ItcReversalCalculation } from '@/services/api/types';
import { Modal } from '@/components/ui/Modal';
import {
  TrendingUp,
  Building2,
  Calculator,
  FileText,
  CheckCircle,
  AlertTriangle,
  Info,
  ArrowRight,
  Receipt,
  RotateCcw,
} from 'lucide-react';
import { cn } from '@/lib/utils';

// Generate return period options (last 12 months)
const generateReturnPeriods = () => {
  const periods = [];
  const now = new Date();
  for (let i = 0; i < 12; i++) {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    periods.push({
      value: `${month}${year}`,
      label: `${date.toLocaleString('default', { month: 'short' })} ${year}`,
    });
  }
  return periods;
};

// Generate financial year options
const generateFinancialYears = () => {
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth() + 1;
  const startYear = currentMonth > 3 ? currentYear : currentYear - 1;

  const years = [];
  for (let i = 0; i < 3; i++) {
    const year = startYear - i;
    years.push({
      value: `${year}-${(year + 1).toString().slice(-2)}`,
      label: `FY ${year}-${(year + 1).toString().slice(-2)}`,
    });
  }
  return years;
};

const ItcReversalManagement = () => {
  const returnPeriods = generateReturnPeriods();
  const financialYears = generateFinancialYears();
  const [selectedCompanyId, setSelectedCompanyId] = useState<string>('');
  const [selectedReturnPeriod, setSelectedReturnPeriod] = useState(returnPeriods[0].value);
  const [selectedFY, setSelectedFY] = useState(financialYears[0].value);

  // Form state for calculation
  const [calculationInput, setCalculationInput] = useState({
    totalInputTax: 0,
    exemptTurnover: 0,
    taxableTurnover: 0,
    totalTurnover: 0,
    commonCredits: 0,
    exclusiveExemptCredits: 0,
    exclusiveTaxableCredits: 0,
  });

  const [calculationResult, setCalculationResult] = useState<ItcReversalCalculation | null>(null);
  const [showPostModal, setShowPostModal] = useState(false);

  const { data: companies = [] } = useCompanies();

  const calculateReversal = useCalculateItcReversal();
  const postReversal = usePostItcReversal();

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const handleCalculate = async () => {
    if (!selectedCompanyId) return;

    const request: ItcReversalCalculationRequest = {
      companyId: selectedCompanyId,
      returnPeriod: selectedReturnPeriod,
      financialYear: selectedFY,
      totalInputTax: calculationInput.totalInputTax,
      exemptTurnover: calculationInput.exemptTurnover,
      taxableTurnover: calculationInput.taxableTurnover,
      totalTurnover: calculationInput.totalTurnover ||
        (calculationInput.exemptTurnover + calculationInput.taxableTurnover),
      commonCredits: calculationInput.commonCredits,
      exclusiveExemptCredits: calculationInput.exclusiveExemptCredits,
      exclusiveTaxableCredits: calculationInput.exclusiveTaxableCredits,
    };

    try {
      const result = await calculateReversal.mutateAsync(request);
      setCalculationResult(result);
    } catch (error) {
      console.error('Failed to calculate ITC reversal:', error);
    }
  };

  const handlePostReversal = async () => {
    if (!calculationResult || !selectedCompanyId) return;

    try {
      await postReversal.mutateAsync({
        companyId: selectedCompanyId,
        returnPeriod: selectedReturnPeriod,
        financialYear: selectedFY,
        rule42Reversal: calculationResult.rule42Reversal,
        rule43Reversal: calculationResult.rule43Reversal || 0,
        totalReversal: calculationResult.totalReversal,
        calculation: calculationResult,
      });
      setShowPostModal(false);
      setCalculationResult(null);
      // Reset form
      setCalculationInput({
        totalInputTax: 0,
        exemptTurnover: 0,
        taxableTurnover: 0,
        totalTurnover: 0,
        commonCredits: 0,
        exclusiveExemptCredits: 0,
        exclusiveTaxableCredits: 0,
      });
    } catch (error) {
      console.error('Failed to post ITC reversal:', error);
    }
  };

  const handleInputChange = (field: keyof typeof calculationInput, value: string) => {
    const numValue = parseFloat(value) || 0;
    setCalculationInput(prev => ({
      ...prev,
      [field]: numValue,
      // Auto-calculate total turnover
      totalTurnover: field === 'exemptTurnover' || field === 'taxableTurnover'
        ? (field === 'exemptTurnover' ? numValue : prev.exemptTurnover) +
          (field === 'taxableTurnover' ? numValue : prev.taxableTurnover)
        : prev.totalTurnover,
    }));
    // Clear previous calculation when inputs change
    setCalculationResult(null);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-start">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">ITC Reversal - Rule 42/43</h1>
          <p className="text-gray-600 mt-2">
            Calculate and post ITC reversal for mixed use inputs under Rule 42/43 of CGST Rules
          </p>
        </div>
      </div>

      {/* Info Box */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <Info className="h-5 w-5 text-blue-600 mt-0.5" />
          <div>
            <h3 className="font-medium text-blue-800">Understanding Rule 42 & 43</h3>
            <div className="text-sm text-blue-700 mt-1 space-y-2">
              <p>
                <strong>Rule 42:</strong> Deals with reversal of ITC on inputs and input services used
                for both taxable and exempt supplies (common credits).
              </p>
              <p>
                <strong>Rule 43:</strong> Deals with reversal of ITC on capital goods used for both
                taxable and exempt supplies.
              </p>
              <p>
                The reversal is calculated based on the proportion of exempt turnover to total turnover.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label htmlFor="companyFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Company *
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
            <label htmlFor="periodFilter" className="block text-sm font-medium text-gray-700 mb-1">
              Return Period
            </label>
            <select
              id="periodFilter"
              value={selectedReturnPeriod}
              onChange={(e) => setSelectedReturnPeriod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {returnPeriods.map((period) => (
                <option key={period.value} value={period.value}>
                  {period.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {!selectedCompanyId ? (
        <div className="bg-white rounded-lg shadow p-8 text-center">
          <Building2 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">Select a Company</h3>
          <p className="text-gray-500">
            Please select a company to calculate ITC reversal
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Input Section */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <Calculator className="h-5 w-5 text-gray-500" />
              Rule 42 Calculation Inputs
            </h2>

            <div className="space-y-4">
              {/* Turnover Section */}
              <div className="border-b pb-4">
                <h3 className="text-sm font-medium text-gray-700 mb-3">Turnover Details</h3>
                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">Taxable Turnover</label>
                    <input
                      type="number"
                      value={calculationInput.taxableTurnover || ''}
                      onChange={(e) => handleInputChange('taxableTurnover', e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                      placeholder="0.00"
                    />
                  </div>
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">Exempt Turnover</label>
                    <input
                      type="number"
                      value={calculationInput.exemptTurnover || ''}
                      onChange={(e) => handleInputChange('exemptTurnover', e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                      placeholder="0.00"
                    />
                  </div>
                </div>
                <div className="mt-3 p-2 bg-gray-50 rounded">
                  <div className="flex justify-between text-sm">
                    <span className="text-gray-600">Total Turnover:</span>
                    <span className="font-medium">{formatCurrency(calculationInput.totalTurnover)}</span>
                  </div>
                  <div className="flex justify-between text-sm mt-1">
                    <span className="text-gray-600">Exempt Ratio:</span>
                    <span className="font-medium">
                      {calculationInput.totalTurnover > 0
                        ? ((calculationInput.exemptTurnover / calculationInput.totalTurnover) * 100).toFixed(2)
                        : 0}%
                    </span>
                  </div>
                </div>
              </div>

              {/* ITC Section */}
              <div className="border-b pb-4">
                <h3 className="text-sm font-medium text-gray-700 mb-3">Input Tax Credit Details</h3>
                <div className="space-y-3">
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">Total Input Tax (T)</label>
                    <input
                      type="number"
                      value={calculationInput.totalInputTax || ''}
                      onChange={(e) => handleInputChange('totalInputTax', e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                      placeholder="0.00"
                    />
                  </div>
                  <div>
                    <label className="block text-sm text-gray-600 mb-1">
                      Common Credits (C)
                      <span className="text-xs text-gray-400 ml-1">- Used for both taxable & exempt</span>
                    </label>
                    <input
                      type="number"
                      value={calculationInput.commonCredits || ''}
                      onChange={(e) => handleInputChange('commonCredits', e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                      placeholder="0.00"
                    />
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="block text-sm text-gray-600 mb-1">
                        Exclusive Exempt (T1)
                      </label>
                      <input
                        type="number"
                        value={calculationInput.exclusiveExemptCredits || ''}
                        onChange={(e) => handleInputChange('exclusiveExemptCredits', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                        placeholder="0.00"
                      />
                    </div>
                    <div>
                      <label className="block text-sm text-gray-600 mb-1">
                        Exclusive Taxable (T2)
                      </label>
                      <input
                        type="number"
                        value={calculationInput.exclusiveTaxableCredits || ''}
                        onChange={(e) => handleInputChange('exclusiveTaxableCredits', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                        placeholder="0.00"
                      />
                    </div>
                  </div>
                </div>
              </div>

              <button
                onClick={handleCalculate}
                disabled={calculateReversal.isPending || !selectedCompanyId || calculationInput.totalTurnover === 0}
                className="w-full px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90 disabled:opacity-50 flex items-center justify-center gap-2"
              >
                <Calculator className="h-4 w-4" />
                {calculateReversal.isPending ? 'Calculating...' : 'Calculate Reversal'}
              </button>
            </div>
          </div>

          {/* Results Section */}
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
              <TrendingUp className="h-5 w-5 text-gray-500" />
              Calculation Results
            </h2>

            {!calculationResult ? (
              <div className="text-center py-12 text-gray-500">
                <RotateCcw className="h-12 w-12 text-gray-300 mx-auto mb-4" />
                <p>Enter values and click "Calculate Reversal" to see results</p>
              </div>
            ) : (
              <div className="space-y-4">
                {/* Formula Display */}
                <div className="bg-gray-50 p-4 rounded-lg">
                  <h3 className="text-sm font-medium text-gray-700 mb-2">Rule 42 Formula</h3>
                  <p className="text-xs text-gray-600 font-mono">
                    D1 = C × (E1 ÷ F)
                  </p>
                  <p className="text-xs text-gray-500 mt-1">
                    Where: C = Common Credits, E1 = Exempt Turnover, F = Total Turnover
                  </p>
                </div>

                {/* Calculation Steps */}
                <div className="space-y-3">
                  <div className="flex items-center justify-between p-3 bg-blue-50 rounded-lg">
                    <div>
                      <p className="text-sm text-blue-700">Common Credits (C)</p>
                      <p className="text-xs text-blue-500">Credits used for both supplies</p>
                    </div>
                    <p className="font-bold text-blue-800">{formatCurrency(calculationInput.commonCredits)}</p>
                  </div>

                  <div className="flex items-center justify-center">
                    <ArrowRight className="h-5 w-5 text-gray-400" />
                  </div>

                  <div className="flex items-center justify-between p-3 bg-purple-50 rounded-lg">
                    <div>
                      <p className="text-sm text-purple-700">Exempt Ratio (E1 ÷ F)</p>
                      <p className="text-xs text-purple-500">
                        {formatCurrency(calculationInput.exemptTurnover)} ÷ {formatCurrency(calculationInput.totalTurnover)}
                      </p>
                    </div>
                    <p className="font-bold text-purple-800">
                      {calculationResult.exemptRatio?.toFixed(4) || '0.0000'}
                    </p>
                  </div>

                  <div className="flex items-center justify-center">
                    <ArrowRight className="h-5 w-5 text-gray-400" />
                  </div>

                  <div className="flex items-center justify-between p-3 bg-red-50 rounded-lg">
                    <div>
                      <p className="text-sm text-red-700">Rule 42 Reversal (D1)</p>
                      <p className="text-xs text-red-500">Amount to be reversed</p>
                    </div>
                    <p className="font-bold text-red-800">{formatCurrency(calculationResult.rule42Reversal)}</p>
                  </div>

                  {calculationResult.rule43Reversal > 0 && (
                    <div className="flex items-center justify-between p-3 bg-orange-50 rounded-lg">
                      <div>
                        <p className="text-sm text-orange-700">Rule 43 Reversal</p>
                        <p className="text-xs text-orange-500">Capital goods reversal</p>
                      </div>
                      <p className="font-bold text-orange-800">{formatCurrency(calculationResult.rule43Reversal)}</p>
                    </div>
                  )}

                  <div className="flex items-center justify-between p-4 bg-red-100 rounded-lg border-2 border-red-200">
                    <div>
                      <p className="font-medium text-red-800">Total ITC Reversal</p>
                      <p className="text-xs text-red-600">Rule 42 + Rule 43</p>
                    </div>
                    <p className="text-xl font-bold text-red-800">{formatCurrency(calculationResult.totalReversal)}</p>
                  </div>
                </div>

                {/* Eligible ITC */}
                <div className="p-4 bg-green-50 rounded-lg border border-green-200">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-medium text-green-800">Eligible ITC (Net)</p>
                      <p className="text-xs text-green-600">Total Input Tax - Total Reversal</p>
                    </div>
                    <p className="text-xl font-bold text-green-800">
                      {formatCurrency(calculationResult.eligibleItc ||
                        (calculationInput.totalInputTax - calculationResult.totalReversal))}
                    </p>
                  </div>
                </div>

                <button
                  onClick={() => setShowPostModal(true)}
                  disabled={!calculationResult || calculationResult.totalReversal === 0}
                  className="w-full px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 flex items-center justify-center gap-2"
                >
                  <CheckCircle className="h-4 w-4" />
                  Post Reversal Entry
                </button>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Post Reversal Confirmation Modal */}
      <Modal
        isOpen={showPostModal}
        onClose={() => setShowPostModal(false)}
        title="Confirm ITC Reversal Posting"
        size="md"
      >
        {calculationResult && (
          <div className="space-y-4">
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-4">
              <div className="flex items-start gap-3">
                <AlertTriangle className="h-5 w-5 text-amber-600 mt-0.5" />
                <div>
                  <p className="font-medium text-amber-800">Review Before Posting</p>
                  <p className="text-sm text-amber-700 mt-1">
                    This will create journal entries to reverse ITC. Please verify the amounts are correct.
                  </p>
                </div>
              </div>
            </div>

            <div className="bg-gray-50 p-4 rounded-lg space-y-2">
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Return Period:</span>
                <span className="font-medium">{selectedReturnPeriod}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Financial Year:</span>
                <span className="font-medium">{selectedFY}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-gray-600">Rule 42 Reversal:</span>
                <span className="font-medium text-red-600">{formatCurrency(calculationResult.rule42Reversal)}</span>
              </div>
              {calculationResult.rule43Reversal > 0 && (
                <div className="flex justify-between text-sm">
                  <span className="text-gray-600">Rule 43 Reversal:</span>
                  <span className="font-medium text-red-600">{formatCurrency(calculationResult.rule43Reversal)}</span>
                </div>
              )}
              <div className="flex justify-between pt-2 border-t">
                <span className="font-medium text-gray-900">Total Reversal:</span>
                <span className="font-bold text-red-600">{formatCurrency(calculationResult.totalReversal)}</span>
              </div>
            </div>

            <div className="bg-blue-50 p-4 rounded-lg">
              <p className="text-sm text-blue-700">
                <strong>Journal Entry:</strong><br />
                Dr. ITC Reversal (Expense) - {formatCurrency(calculationResult.totalReversal)}<br />
                &nbsp;&nbsp;&nbsp;&nbsp;Cr. CGST Input - {formatCurrency(calculationResult.totalReversal / 2)}<br />
                &nbsp;&nbsp;&nbsp;&nbsp;Cr. SGST Input - {formatCurrency(calculationResult.totalReversal / 2)}
              </p>
            </div>

            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowPostModal(false)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handlePostReversal}
                disabled={postReversal.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 border border-transparent rounded-md hover:bg-green-700 disabled:opacity-50"
              >
                {postReversal.isPending ? 'Posting...' : 'Confirm & Post'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default ItcReversalManagement;
