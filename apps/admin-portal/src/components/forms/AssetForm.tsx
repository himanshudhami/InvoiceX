import { useEffect, useState } from 'react';
import { Asset, CreateAssetDto, UpdateAssetDto } from '@/services/api/types';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCreateAsset, useUpdateAsset } from '@/hooks/api/useAssets';
import { useLoans } from '@/hooks/api/useLoans';
import { cn } from '@/lib/utils';
import { CurrencySelect } from '@/components/ui/currency-select';
import { CompanySelect } from '@/components/ui/CompanySelect';

type AssetFormProps = {
  asset?: Asset;
  onSuccess: () => void;
  onCancel: () => void;
};

const assetTypes = [
  { value: 'IT_Asset', label: 'IT Asset' },
  { value: 'Fixed_Asset', label: 'Fixed Asset' },
  { value: 'Intangible_Asset', label: 'Intangible Asset' },
];

const assetStatuses = ['available', 'assigned', 'maintenance', 'retired', 'reserved', 'lost'];

export const AssetForm = ({ asset, onSuccess, onCancel }: AssetFormProps) => {
  const isEditing = !!asset;
  const createAsset = useCreateAsset();
  const updateAsset = useUpdateAsset();
  const { data: companies = [] } = useCompanies();
  const [formData, setFormData] = useState<CreateAssetDto>({
    companyId: '',
    assetType: 'IT_Asset',
    status: 'available',
    assetTag: '',
    name: '',
    serialNumber: '',
    purchaseType: 'capex',
    currency: 'USD',
  });

  // Fetch loans for the selected company (only when company is selected)
  const loansQueryParams = {
    pageNumber: 1, 
    pageSize: 100, // Backend limit is 100
    ...(formData.companyId && { companyId: formData.companyId }),
    status: 'active'
  };
  const { data: loansData, isLoading: loansLoading, refetch: refetchLoans } = useLoans(loansQueryParams);
  const availableLoans = loansData?.items || [];

  // Debug: Log loans data
  useEffect(() => {
    if (formData.companyId && loansData) {
      console.log('Loans query result:', {
        companyId: formData.companyId,
        totalLoans: loansData.totalCount,
        activeLoans: availableLoans.length,
        loans: availableLoans.map(l => ({ id: l.id, name: l.loanName, status: l.status, companyId: l.companyId }))
      });
    }
  }, [formData.companyId, loansData, availableLoans]);

  // Refetch loans when company changes
  useEffect(() => {
    if (formData.companyId) {
      refetchLoans();
    }
  }, [formData.companyId, refetchLoans]);

  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (asset) {
      setFormData({
        companyId: asset.companyId,
        assetType: asset.assetType,
        status: asset.status,
        assetTag: asset.assetTag,
        name: asset.name,
        serialNumber: asset.serialNumber ?? '',
        categoryId: asset.categoryId,
        modelId: asset.modelId,
        purchaseCost: asset.purchaseCost,
        currency: asset.currency,
        depreciationMethod: asset.depreciationMethod,
        usefulLifeMonths: asset.usefulLifeMonths,
        salvageValue: asset.salvageValue,
        purchaseType: asset.purchaseType ?? 'capex',
        invoiceReference: asset.invoiceReference,
        purchaseDate: asset.purchaseDate,
        inServiceDate: asset.inServiceDate,
        depreciationStartDate: asset.depreciationStartDate,
        warrantyExpiration: asset.warrantyExpiration,
        location: asset.location,
        vendor: asset.vendor,
        description: asset.description,
        linkedLoanId: asset.linkedLoanId,
        downPaymentAmount: asset.downPaymentAmount,
        gstAmount: asset.gstAmount,
        gstRate: asset.gstRate,
        itcEligible: asset.itcEligible,
        tdsOnInterest: asset.tdsOnInterest,
      });
    }
  }, [asset]);

  const validate = () => {
    const next: Record<string, string> = {};
    if (!formData.companyId) next.companyId = 'Company is required';
    if (!formData.assetTag) next.assetTag = 'Asset tag is required';
    if (!formData.name) next.name = 'Name is required';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    try {
      if (isEditing && asset) {
        await updateAsset.mutateAsync({ id: asset.id, data: formData as UpdateAssetDto });
      } else {
        await createAsset.mutateAsync(formData);
      }
      onSuccess();
    } catch (err) {
      console.error(err);
    }
  };

  const setField = (field: keyof CreateAssetDto, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: '' }));
  };

  return (
    <form className="space-y-4" onSubmit={handleSubmit}>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">Company</label>
          <CompanySelect
            companies={companies}
            value={formData.companyId}
            onChange={(value) => setField('companyId', value)}
            placeholder="Select company..."
            error={errors.companyId}
            className="mt-1"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Asset Tag</label>
          <input
            className={cn(
              'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
              errors.assetTag && 'border-red-500',
            )}
            value={formData.assetTag}
            onChange={(e) => setField('assetTag', e.target.value)}
          />
          {errors.assetTag && <p className="text-sm text-red-600 mt-1">{errors.assetTag}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Name</label>
          <input
            className={cn(
              'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
              errors.name && 'border-red-500',
            )}
            value={formData.name}
            onChange={(e) => setField('name', e.target.value)}
          />
          {errors.name && <p className="text-sm text-red-600 mt-1">{errors.name}</p>}
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Serial</label>
          <input
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.serialNumber ?? ''}
            onChange={(e) => setField('serialNumber', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Purchase Type</label>
          <select
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.purchaseType}
            onChange={(e) => setField('purchaseType', e.target.value)}
          >
            <option value="capex">CAPEX</option>
            <option value="opex">OPEX</option>
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Type</label>
          <select
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.assetType}
            onChange={(e) => setField('assetType', e.target.value)}
          >
            {assetTypes.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Status</label>
          <select
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.status}
            onChange={(e) => setField('status', e.target.value)}
          >
            {assetStatuses.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700">Purchase Cost</label>
          <input
            type="number"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.purchaseCost ?? ''}
            onChange={(e) => setField('purchaseCost', e.target.value ? Number(e.target.value) : undefined)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Currency</label>
          <CurrencySelect
            value={formData.currency ?? 'USD'}
            onChange={(value) => setField('currency', value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Invoice / Reference</label>
          <input
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.invoiceReference ?? ''}
            onChange={(e) => setField('invoiceReference', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Vendor</label>
          <input
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.vendor ?? ''}
            onChange={(e) => setField('vendor', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Purchase Date</label>
          <input
            type="date"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.purchaseDate ?? ''}
            onChange={(e) => setField('purchaseDate', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">In Service Date</label>
          <input
            type="date"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.inServiceDate ?? ''}
            onChange={(e) => setField('inServiceDate', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Depreciation Start</label>
          <input
            type="date"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.depreciationStartDate ?? ''}
            onChange={(e) => setField('depreciationStartDate', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Warranty Expiration</label>
          <input
            type="date"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.warrantyExpiration ?? ''}
            onChange={(e) => setField('warrantyExpiration', e.target.value)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Useful Life (months)</label>
          <input
            type="number"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.usefulLifeMonths ?? ''}
            onChange={(e) => setField('usefulLifeMonths', e.target.value ? Number(e.target.value) : undefined)}
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Depreciation Method</label>
          <select
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.depreciationMethod ?? 'none'}
            onChange={(e) => setField('depreciationMethod', e.target.value)}
          >
            <option value="none">None</option>
            <option value="straight_line">Straight Line</option>
            <option value="double_declining">Double Declining</option>
            <option value="sum_of_years_digits">Sum of Years Digits</option>
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700">Salvage Value</label>
          <input
            type="number"
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
            value={formData.salvageValue ?? ''}
            onChange={(e) => setField('salvageValue', e.target.value ? Number(e.target.value) : undefined)}
          />
        </div>
      </div>

      {/* Loan Financing Section */}
      <div className="border-t pt-4 mt-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-medium text-gray-900">Loan Financing & GST</h3>
          {formData.companyId && (
            <button
              type="button"
              onClick={() => refetchLoans()}
              disabled={loansLoading}
              className="text-xs text-blue-600 hover:text-blue-800 disabled:text-gray-400"
              title="Refresh loans list"
            >
              {loansLoading ? 'Refreshing...' : '🔄 Refresh'}
            </button>
          )}
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Linked Loan</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.linkedLoanId || ''}
              onChange={(e) => setField('linkedLoanId', e.target.value || undefined)}
              disabled={!formData.companyId || loansLoading}
            >
              <option value="">
                {!formData.companyId 
                  ? 'Select company first' 
                  : loansLoading
                    ? 'Loading loans...'
                    : availableLoans.length === 0 
                      ? 'No active loans for this company'
                      : 'None (Cash Purchase)'}
              </option>
              {availableLoans.map((loan) => (
                <option key={loan.id} value={loan.id}>
                  {loan.loanName} - {loan.lenderName}
                </option>
              ))}
            </select>
            {!formData.companyId && (
              <p className="mt-1 text-xs text-gray-500">Please select a company to see available loans</p>
            )}
            {formData.companyId && availableLoans.length === 0 && !loansLoading && (
              <p className="mt-1 text-xs text-amber-600">
                No active loans found. Make sure the loan status is "active" and it belongs to the selected company. 
                Click "Refresh" to reload loans.
              </p>
            )}
          </div>
          {formData.linkedLoanId && (
            <div>
              <label className="block text-sm font-medium text-gray-700">Down Payment Amount</label>
              <input
                type="number"
                step="0.01"
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.downPaymentAmount ?? ''}
                onChange={(e) => setField('downPaymentAmount', e.target.value ? Number(e.target.value) : undefined)}
                placeholder="Amount paid upfront"
              />
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-gray-700">GST Rate (%)</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.gstRate ?? ''}
              onChange={(e) => {
                const rate = e.target.value ? Number(e.target.value) : undefined;
                setField('gstRate', rate);
                // Auto-calculate GST amount if purchase cost is available
                if (rate && formData.purchaseCost) {
                  setField('gstAmount', (formData.purchaseCost * rate) / 100);
                }
              }}
            >
              <option value="">Select GST Rate</option>
              <option value="0">0%</option>
              <option value="5">5%</option>
              <option value="12">12%</option>
              <option value="18">18%</option>
              <option value="28">28%</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">GST Amount</label>
            <input
              type="number"
              step="0.01"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.gstAmount ?? ''}
              onChange={(e) => setField('gstAmount', e.target.value ? Number(e.target.value) : undefined)}
              placeholder="GST paid on purchase"
            />
          </div>
          <div>
            <label className="flex items-center space-x-2">
              <input
                type="checkbox"
                className="rounded border-gray-300"
                checked={formData.itcEligible ?? false}
                onChange={(e) => setField('itcEligible', e.target.checked)}
              />
              <span className="text-sm font-medium text-gray-700">ITC Eligible (Input Tax Credit)</span>
            </label>
          </div>
          {formData.linkedLoanId && (
            <div>
              <label className="block text-sm font-medium text-gray-700">TDS on Interest</label>
              <input
                type="number"
                step="0.01"
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
                value={formData.tdsOnInterest ?? ''}
                onChange={(e) => setField('tdsOnInterest', e.target.value ? Number(e.target.value) : undefined)}
                placeholder="TDS deducted on loan interest"
              />
            </div>
          )}
        </div>
      </div>

      <div className="flex justify-end space-x-3">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 rounded-md border border-gray-300 text-gray-700 hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={createAsset.isPending || updateAsset.isPending}
          className="px-4 py-2 rounded-md bg-primary text-white hover:bg-primary/90 disabled:opacity-60"
        >
          {isEditing ? 'Update Asset' : 'Create Asset'}
        </button>
      </div>
    </form>
  );
};


