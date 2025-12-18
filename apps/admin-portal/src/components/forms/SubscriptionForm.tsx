import { useEffect, useState, useMemo } from 'react';
import { Subscription, CreateSubscriptionDto, UpdateSubscriptionDto } from '@/services/api/types';
import { useCompanies } from '@/hooks/api/useCompanies';
import { useCreateSubscription, useUpdateSubscription } from '@/hooks/api/useSubscriptions';
import { cn } from '@/lib/utils';
import { CurrencySelect } from '@/components/ui/currency-select';
import { CompanySelect } from '@/components/ui/CompanySelect';

type SubscriptionFormProps = {
  subscription?: Subscription;
  onSuccess: () => void;
  onCancel: () => void;
};

const statuses = [
  { value: 'trial', label: 'Trial' },
  { value: 'active', label: 'Active' },
  { value: 'on_hold', label: 'On Hold (Paused)' },
  { value: 'expired', label: 'Expired' },
  { value: 'cancelled', label: 'Cancelled' },
];

const periods = ['monthly', 'quarterly', 'yearly', 'custom'];

export const SubscriptionForm = ({ subscription, onSuccess, onCancel }: SubscriptionFormProps) => {
  const isEditing = !!subscription;
  const createSub = useCreateSubscription();
  const updateSub = useUpdateSubscription();
  const { data: companies = [], isLoading: companiesLoading } = useCompanies();

  const [formData, setFormData] = useState<CreateSubscriptionDto>({
    companyId: '',
    name: '',
    status: 'active',
    renewalPeriod: 'monthly',
    currency: 'USD',
    autoRenew: true,
  });

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Calculate cost per seat or cost per period based on user input
  const calculatedCosts = useMemo(() => {
    const seats = formData.seatsTotal;
    const costPerSeat = (formData as any).costPerSeat;
    const costPerPeriod = formData.costPerPeriod;

    if (seats && seats > 0) {
      if (costPerSeat && costPerSeat > 0) {
        return {
          costPerPeriod: costPerSeat * seats,
          costPerSeat: costPerSeat,
        };
      } else if (costPerPeriod && costPerPeriod > 0) {
        return {
          costPerPeriod: costPerPeriod,
          costPerSeat: costPerPeriod / seats,
        };
      }
    }
    return {
      costPerPeriod: costPerPeriod,
      costPerSeat: costPerSeat,
    };
  }, [formData.seatsTotal, (formData as any).costPerSeat, formData.costPerPeriod]);

  useEffect(() => {
    if (subscription) {
      // Format dates for date inputs (YYYY-MM-DD)
      const formatDate = (dateStr?: string) => {
        if (!dateStr) return '';
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return '';
        return date.toISOString().split('T')[0];
      };

      setFormData({
        companyId: subscription.companyId,
        name: subscription.name,
        vendor: subscription.vendor,
        planName: subscription.planName,
        category: subscription.category,
        status: subscription.status,
        renewalPeriod: subscription.renewalPeriod ?? 'monthly',
        seatsTotal: subscription.seatsTotal,
        seatsUsed: subscription.seatsUsed,
        licenseKey: subscription.licenseKey,
        costPerPeriod: subscription.costPerPeriod,
        currency: subscription.currency ?? 'USD',
        startDate: formatDate(subscription.startDate),
        renewalDate: formatDate(subscription.renewalDate),
        billingCycleStart: formatDate(subscription.billingCycleStart),
        billingCycleEnd: formatDate(subscription.billingCycleEnd),
        autoRenew: subscription.autoRenew ?? true,
        url: subscription.url,
        notes: subscription.notes,
      });

      // Calculate cost per seat if we have cost per period and seats
      if (subscription.costPerPeriod && subscription.seatsTotal && subscription.seatsTotal > 0) {
        (setFormData as any)((prev: any) => ({
          ...prev,
          costPerSeat: subscription.costPerPeriod! / subscription.seatsTotal!,
        }));
      }
    }
  }, [subscription]);

  const validate = () => {
    const next: Record<string, string> = {};
    if (!formData.companyId) next.companyId = 'Company is required';
    if (!formData.name) next.name = 'Name is required';
    
    // If seats are provided, need either cost per seat or cost per period
    if (formData.seatsTotal && formData.seatsTotal > 0) {
      const costPerSeat = (formData as any).costPerSeat;
      if (!costPerSeat && !formData.costPerPeriod) {
        next.costPerSeat = 'Either cost per seat or cost per period is required when seats are specified';
      }
    }
    
    // Date validations
    if (formData.startDate && formData.renewalDate) {
      const start = new Date(formData.startDate);
      const renewal = new Date(formData.renewalDate);
      if (renewal < start) {
        next.renewalDate = 'Renewal date must be after start date';
      }
    }

    if (formData.billingCycleStart && formData.billingCycleEnd) {
      const start = new Date(formData.billingCycleStart);
      const end = new Date(formData.billingCycleEnd);
      if (end < start) {
        next.billingCycleEnd = 'Billing cycle end must be after start';
      }
    }

    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate()) return;
    
    try {
      // Clean the data: convert empty strings to undefined for optional fields
      const cleanDate = (dateStr: string | undefined): string | undefined => {
        if (!dateStr || dateStr.trim() === '') return undefined;
        // Ensure date is in ISO format
        const date = new Date(dateStr);
        if (isNaN(date.getTime())) return undefined;
        return date.toISOString();
      };

      // Use calculated cost per period if cost per seat was entered
      const submitData: any = { ...formData };
      
      // Remove costPerSeat from submit data (it's not in the DTO, only costPerPeriod is)
      delete submitData.costPerSeat;
      
      if ((formData as any).costPerSeat && formData.seatsTotal && formData.seatsTotal > 0) {
        submitData.costPerPeriod = calculatedCosts.costPerPeriod;
      }

      // Clean date fields - convert empty strings to undefined, ensure ISO format
      submitData.startDate = cleanDate(submitData.startDate);
      submitData.renewalDate = cleanDate(submitData.renewalDate);
      submitData.billingCycleStart = cleanDate(submitData.billingCycleStart);
      submitData.billingCycleEnd = cleanDate(submitData.billingCycleEnd);

      // Clean optional string fields - convert empty strings to undefined
      if (submitData.vendor === '') submitData.vendor = undefined;
      if (submitData.planName === '') submitData.planName = undefined;
      if (submitData.category === '') submitData.category = undefined;
      if (submitData.licenseKey === '') submitData.licenseKey = undefined;
      if (submitData.url === '') submitData.url = undefined;
      if (submitData.notes === '') submitData.notes = undefined;
      if (submitData.currency === '') submitData.currency = undefined;

      // Remove undefined values to avoid sending them in JSON
      Object.keys(submitData).forEach(key => {
        if (submitData[key] === undefined) {
          delete submitData[key];
        }
      });

      if (isEditing && subscription) {
        await updateSub.mutateAsync({ id: subscription.id, data: submitData as UpdateSubscriptionDto });
      } else {
        await createSub.mutateAsync(submitData);
      }
      onSuccess();
    } catch (err) {
      console.error(err);
      setErrors({ submit: 'Failed to save subscription. Please try again.' });
    }
  };

  const setField = (field: keyof CreateSubscriptionDto, value: any) => {
    setFormData((prev) => {
      const updated = { ...prev, [field]: value };
      
      // Auto-calculate cost per period when cost per seat changes
      if (field === 'costPerSeat' && value && prev.seatsTotal && prev.seatsTotal > 0) {
        (updated as any).costPerPeriod = value * prev.seatsTotal;
      }
      
      // Auto-calculate cost per seat when cost per period changes (if seats exist)
      if (field === 'costPerPeriod' && value && prev.seatsTotal && prev.seatsTotal > 0) {
        (updated as any).costPerSeat = value / prev.seatsTotal;
      }
      
      // Clear cost per seat if seats total is cleared
      if (field === 'seatsTotal' && (!value || value === 0)) {
        (updated as any).costPerSeat = undefined;
      }
      
      return updated;
    });
    if (errors[field]) setErrors((prev) => ({ ...prev, [field]: '' }));
  };

  // Calculate monthly cost preview
  const monthlyCost = useMemo(() => {
    const cost = calculatedCosts.costPerPeriod || formData.costPerPeriod || 0;
    if (!cost) return 0;
    
    switch (formData.renewalPeriod) {
      case 'monthly':
        return cost;
      case 'quarterly':
        return cost / 3;
      case 'yearly':
        return cost / 12;
      default:
        return cost;
    }
  }, [calculatedCosts.costPerPeriod, formData.costPerPeriod, formData.renewalPeriod]);

  return (
    <form className="space-y-6" onSubmit={handleSubmit}>
      {errors.submit && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          {errors.submit}
        </div>
      )}

      {/* Basic Information Section */}
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
              disabled={companiesLoading}
              error={errors.companyId}
              className="mt-1"
            />
            {companies.length === 0 && !companiesLoading && (
              <p className="text-sm text-gray-500 mt-1">No companies available</p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Name *</label>
            <input
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.name && 'border-red-500',
              )}
              value={formData.name}
              onChange={(e) => setField('name', e.target.value)}
              placeholder="e.g., Microsoft Office 365"
            />
            {errors.name && <p className="text-sm text-red-600 mt-1">{errors.name}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Vendor</label>
            <input
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.vendor ?? ''}
              onChange={(e) => setField('vendor', e.target.value)}
              placeholder="e.g., Microsoft"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Plan Name</label>
            <input
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.planName ?? ''}
              onChange={(e) => setField('planName', e.target.value)}
              placeholder="e.g., Business Premium"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Category</label>
            <input
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.category ?? ''}
              onChange={(e) => setField('category', e.target.value)}
              placeholder="e.g., Productivity, CRM, Development"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Status</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.status}
              onChange={(e) => setField('status', e.target.value)}
            >
              {statuses.map((s) => (
                <option key={s.value} value={s.value}>
                  {s.label}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Dates & Billing Section */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Dates & Billing</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Start Date</label>
            <input
              type="date"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.startDate ?? ''}
              onChange={(e) => setField('startDate', e.target.value)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Renewal Date</label>
            <input
              type="date"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.renewalDate && 'border-red-500',
              )}
              value={formData.renewalDate ?? ''}
              onChange={(e) => setField('renewalDate', e.target.value)}
            />
            {errors.renewalDate && <p className="text-sm text-red-600 mt-1">{errors.renewalDate}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Renewal Period</label>
            <select
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.renewalPeriod}
              onChange={(e) => setField('renewalPeriod', e.target.value)}
            >
              {periods.map((p) => (
                <option key={p} value={p}>
                  {p.charAt(0).toUpperCase() + p.slice(1)}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Auto Renew</label>
            <div className="mt-2">
              <label className="inline-flex items-center">
                <input
                  type="checkbox"
                  className="rounded border-gray-300 text-primary focus:ring-primary"
                  checked={formData.autoRenew ?? true}
                  onChange={(e) => setField('autoRenew', e.target.checked)}
                />
                <span className="ml-2 text-sm text-gray-700">Automatically renew this subscription</span>
              </label>
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Billing Cycle Start</label>
            <input
              type="date"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.billingCycleStart ?? ''}
              onChange={(e) => setField('billingCycleStart', e.target.value)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Billing Cycle End</label>
            <input
              type="date"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.billingCycleEnd && 'border-red-500',
              )}
              value={formData.billingCycleEnd ?? ''}
              onChange={(e) => setField('billingCycleEnd', e.target.value)}
            />
            {errors.billingCycleEnd && <p className="text-sm text-red-600 mt-1">{errors.billingCycleEnd}</p>}
          </div>
        </div>
      </div>

      {/* Seats & Cost Section */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Seats & Cost</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">Total Seats</label>
            <input
              type="number"
              min="0"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.seatsTotal ?? ''}
              onChange={(e) => setField('seatsTotal', e.target.value === '' ? undefined : Number(e.target.value))}
              placeholder="e.g., 40"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Seats Used</label>
            <input
              type="number"
              min="0"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 bg-gray-50"
              value={formData.seatsUsed ?? ''}
              onChange={(e) => setField('seatsUsed', e.target.value === '' ? undefined : Number(e.target.value))}
              placeholder="Auto-calculated from assignments"
            />
            <p className="text-xs text-gray-500 mt-1">Tracked from seat assignments</p>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Cost per Seat</label>
            <input
              type="number"
              step="0.01"
              min="0"
              className={cn(
                'mt-1 block w-full rounded-md border border-gray-300 px-3 py-2',
                errors.costPerSeat && 'border-red-500',
              )}
              value={(formData as any).costPerSeat ?? ''}
              onChange={(e) => {
                const value = e.target.value === '' ? undefined : Number(e.target.value);
                setField('costPerSeat' as any, value);
              }}
              placeholder="e.g., 150"
            />
            {errors.costPerSeat && <p className="text-sm text-red-600 mt-1">{errors.costPerSeat}</p>}
            {calculatedCosts.costPerPeriod && formData.seatsTotal && (
              <p className="text-xs text-green-600 mt-1">
                Total: {calculatedCosts.costPerPeriod.toLocaleString('en-IN', {
                  style: 'currency',
                  currency: formData.currency || 'USD',
                })}
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Cost per Period</label>
            <input
              type="number"
              step="0.01"
              min="0"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.costPerPeriod ?? ''}
              onChange={(e) =>
                setField('costPerPeriod', e.target.value === '' ? undefined : Number(e.target.value))
              }
              placeholder="e.g., 6000"
            />
            {calculatedCosts.costPerSeat && formData.seatsTotal && formData.seatsTotal > 0 && (
              <p className="text-xs text-green-600 mt-1">
                Per seat: {calculatedCosts.costPerSeat.toLocaleString('en-IN', {
                  style: 'currency',
                  currency: formData.currency || 'USD',
                })}
              </p>
            )}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Currency</label>
            <CurrencySelect
              value={formData.currency ?? 'USD'}
              onChange={(value) => setField('currency', value)}
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Monthly Cost Preview</label>
            <div className="mt-1 block w-full rounded-md border border-gray-200 bg-gray-50 px-3 py-2">
              <span className="text-lg font-semibold text-gray-900">
                {monthlyCost.toLocaleString('en-IN', {
                  style: 'currency',
                  currency: formData.currency || 'USD',
                })}
              </span>
              <span className="text-sm text-gray-500 ml-2">per month</span>
            </div>
          </div>
        </div>
      </div>

      {/* Additional Information Section */}
      <div>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Additional Information</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">License Key</label>
            <input
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.licenseKey ?? ''}
              onChange={(e) => setField('licenseKey', e.target.value)}
              placeholder="Enter license key if applicable"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">URL</label>
            <input
              type="url"
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.url ?? ''}
              onChange={(e) => setField('url', e.target.value)}
              placeholder="https://..."
            />
          </div>
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700">Notes</label>
            <textarea
              rows={3}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2"
              value={formData.notes ?? ''}
              onChange={(e) => setField('notes', e.target.value)}
              placeholder="Additional notes about this subscription..."
            />
          </div>
        </div>
      </div>

      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 rounded-md border border-gray-300 text-gray-700 hover:bg-gray-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={createSub.isPending || updateSub.isPending}
          className="px-4 py-2 rounded-md bg-primary text-white hover:bg-primary/90 disabled:opacity-60"
        >
          {isEditing ? 'Update Subscription' : 'Create Subscription'}
        </button>
      </div>
    </form>
  );
};


