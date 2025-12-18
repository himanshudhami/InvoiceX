import { useEffect, useState, useMemo } from 'react';
import { Loan, CreateLoanDto, UpdateLoanDto } from '@/services/api/types';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCreateLoan, useUpdateLoan } from '@/hooks/api/useLoans';
import { cn } from '@/lib/utils';
import { CompanySelect } from '@/components/ui/CompanySelect';

type LoanFormProps = {
  loan?: Loan;
  onSuccess: () => void;
  onCancel: () => void;
};

const loanTypes = [
  { value: 'secured', label: 'Secured' },
  { value: 'unsecured', label: 'Unsecured' },
  { value: 'asset_financing', label: 'Asset Financing' },
];

const interestTypes = [
  { value: 'fixed', label: 'Fixed' },
  { value: 'floating', label: 'Floating' },
  { value: 'reducing', label: 'Reducing Balance' },
];

const compoundingFrequencies = [
  { value: 'monthly', label: 'Monthly' },
  { value: 'quarterly', label: 'Quarterly' },
  { value: 'annually', label: 'Annually' },
];

export const LoanForm = ({ loan, onSuccess, onCancel }: LoanFormProps) => {
  const isEditing = !!loan;
  const createLoan = useCreateLoan();
  const updateLoan = useUpdateLoan();
  const { data: companies = [], isLoading: companiesLoading } = useCompanies();

  const [formData, setFormData] = useState<CreateLoanDto>({
    companyId: '',
    loanName: '',
    lenderName: '',
    loanType: 'secured',
    principalAmount: 0,
    interestRate: 0,
    loanStartDate: new Date().toISOString().split('T')[0],
    tenureMonths: 12,
    interestType: 'fixed',
    compoundingFrequency: 'monthly',
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Calculate EMI preview
  const emiPreview = useMemo(() => {
    if (formData.principalAmount > 0 && formData.interestRate >= 0 && formData.tenureMonths > 0) {
      const monthlyRate = formData.interestRate / 12 / 100;
      if (monthlyRate === 0) {
        return formData.principalAmount / formData.tenureMonths;
      }
      const power = Math.pow(1 + monthlyRate, formData.tenureMonths);
      const emi = (formData.principalAmount * monthlyRate * power) / (power - 1);
      return Math.round(emi * 100) / 100;
    }
    return 0;
  }, [formData.principalAmount, formData.interestRate, formData.tenureMonths]);

  useEffect(() => {
    if (loan) {
      const formatDate = (dateStr?: string) => {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return '';
        return date.toISOString().split('T')[0];
      };

      setFormData({
        companyId: loan.companyId,
        loanName: loan.loanName,
        lenderName: loan.lenderName,
        loanType: loan.loanType as any,
        assetId: loan.assetId,
        principalAmount: loan.principalAmount,
        interestRate: loan.interestRate,
        loanStartDate: formatDate(loan.loanStartDate),
        loanEndDate: formatDate(loan.loanEndDate),
        tenureMonths: loan.tenureMonths,
        interestType: loan.interestType as any,
        compoundingFrequency: loan.compoundingFrequency as any,
        loanAccountNumber: loan.loanAccountNumber,
        notes: loan.notes,
      });
    }
  }, [loan]);

  const setField = (field: keyof CreateLoanDto, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});

    // Validation
    if (!formData.companyId) {
      setErrors({ companyId: 'Company is required' });
      return;
    }
    if (!formData.loanName.trim()) {
      setErrors({ loanName: 'Loan name is required' });
      return;
    }
    if (!formData.lenderName.trim()) {
      setErrors({ lenderName: 'Lender name is required' });
      return;
    }
    if (formData.principalAmount <= 0) {
      setErrors({ principalAmount: 'Principal amount must be greater than 0' });
      return;
    }
    if (formData.interestRate < 0 || formData.interestRate > 100) {
      setErrors({ interestRate: 'Interest rate must be between 0 and 100' });
      return;
    }
    if (formData.tenureMonths <= 0 || formData.tenureMonths > 360) {
      setErrors({ tenureMonths: 'Tenure must be between 1 and 360 months' });
      return;
    }

    try {
      if (isEditing && loan) {
        const updateData: UpdateLoanDto = {
          loanName: formData.loanName,
          lenderName: formData.lenderName,
          loanType: formData.loanType,
          assetId: formData.assetId,
          principalAmount: formData.principalAmount,
          interestRate: formData.interestRate,
          loanStartDate: formData.loanStartDate,
          loanEndDate: formData.loanEndDate,
          tenureMonths: formData.tenureMonths,
          interestType: formData.interestType,
          compoundingFrequency: formData.compoundingFrequency,
          loanAccountNumber: formData.loanAccountNumber,
          notes: formData.notes,
        };
        await updateLoan.mutateAsync({ id: loan.id, data: updateData });
      } else {
        await createLoan.mutateAsync(formData);
      }
      onSuccess();
    } catch (error: any) {
      setErrors({ submit: error?.message || 'Failed to save loan' });
    }
  };

  return (
    <form className="space-y-6" onSubmit={handleSubmit}>
      {errors.submit && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {errors.submit}
        </div>
      )}

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Basic Information</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Company *</label>
            <CompanySelect
              companies={companies}
              value={formData.companyId}
              onChange={(value) => setField('companyId', value)}
              placeholder={companiesLoading ? 'Loading companies...' : 'Select company...'}
              disabled={companiesLoading || isEditing}
              error={errors.companyId}
              className="mt-1"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Loan Name *</label>
            <input
              type="text"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.loanName && 'border-red-500',
              )}
              value={formData.loanName}
              onChange={(e) => setField('loanName', e.target.value)}
              placeholder="e.g., Office Building Loan"
            />
            {errors.loanName && <p className="text-sm text-red-600 mt-1">{errors.loanName}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Lender Name *</label>
            <input
              type="text"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.lenderName && 'border-red-500',
              )}
              value={formData.lenderName}
              onChange={(e) => setField('lenderName', e.target.value)}
              placeholder="e.g., HDFC Bank"
            />
            {errors.lenderName && <p className="text-sm text-red-600 mt-1">{errors.lenderName}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Loan Type</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.loanType}
              onChange={(e) => setField('loanType', e.target.value)}
            >
              {loanTypes.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Loan Account Number</label>
            <input
              type="text"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.loanAccountNumber || ''}
              onChange={(e) => setField('loanAccountNumber', e.target.value)}
              placeholder="Optional"
            />
          </div>
        </div>
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Loan Terms</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Principal Amount (INR) *</label>
            <input
              type="number"
              step="0.01"
              min="0"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.principalAmount && 'border-red-500',
              )}
              value={formData.principalAmount || ''}
              onChange={(e) => setField('principalAmount', parseFloat(e.target.value) || 0)}
            />
            {errors.principalAmount && <p className="text-sm text-red-600 mt-1">{errors.principalAmount}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Interest Rate (% per annum) *</label>
            <input
              type="number"
              step="0.01"
              min="0"
              max="100"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.interestRate && 'border-red-500',
              )}
              value={formData.interestRate || ''}
              onChange={(e) => setField('interestRate', parseFloat(e.target.value) || 0)}
            />
            {errors.interestRate && <p className="text-sm text-red-600 mt-1">{errors.interestRate}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Tenure (Months) *</label>
            <input
              type="number"
              min="1"
              max="360"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.tenureMonths && 'border-red-500',
              )}
              value={formData.tenureMonths || ''}
              onChange={(e) => setField('tenureMonths', parseInt(e.target.value) || 0)}
            />
            {errors.tenureMonths && <p className="text-sm text-red-600 mt-1">{errors.tenureMonths}</p>}
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Loan Start Date *</label>
            <input
              type="date"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.loanStartDate}
              onChange={(e) => setField('loanStartDate', e.target.value)}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Interest Type</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.interestType}
              onChange={(e) => setField('interestType', e.target.value)}
            >
              {interestTypes.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">Compounding Frequency</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.compoundingFrequency}
              onChange={(e) => setField('compoundingFrequency', e.target.value)}
            >
              {compoundingFrequencies.map((freq) => (
                <option key={freq.value} value={freq.value}>
                  {freq.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {emiPreview > 0 && (
          <div className="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-md">
            <p className="text-sm font-medium text-blue-900">Estimated EMI: ₹{emiPreview.toLocaleString('en-IN', { maximumFractionDigits: 2 })}</p>
            <p className="text-xs text-blue-700 mt-1">Total Interest: ₹{((emiPreview * formData.tenureMonths) - formData.principalAmount).toLocaleString('en-IN', { maximumFractionDigits: 2 })}</p>
          </div>
        )}
      </div>

      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Additional Information</h3>
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Notes</label>
            <textarea
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              rows={3}
              value={formData.notes || ''}
              onChange={(e) => setField('notes', e.target.value)}
              placeholder="Additional notes about the loan"
            />
          </div>
        </div>
      </div>

      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={createLoan.isPending || updateLoan.isPending}
          className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50"
        >
          {createLoan.isPending || updateLoan.isPending ? 'Saving...' : isEditing ? 'Update Loan' : 'Create Loan'}
        </button>
      </div>
    </form>
  );
};





