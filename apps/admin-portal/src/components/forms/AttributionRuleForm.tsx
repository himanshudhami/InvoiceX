import { useState, useEffect } from 'react';
import {
  AttributionRule,
  CreateAttributionRuleDto,
  UpdateAttributionRuleDto,
  RuleType,
  RuleConditions,
  TagAssignment,
  TransactionType,
  Tag,
} from '@/services/api/types';
import {
  useCreateAttributionRule,
  useUpdateAttributionRule,
  useTags,
} from '@/features/tags/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { useVendors } from '@/hooks/api/useVendors';
import { useCustomers } from '@/hooks/api/useCustomers';
import { cn } from '@/lib/utils';
import {
  Building2,
  Users,
  Calculator,
  Package,
  Search,
  DollarSign,
  UserCheck,
  Layers,
  Plus,
  X,
  AlertCircle,
} from 'lucide-react';

interface AttributionRuleFormProps {
  rule?: AttributionRule;
  onSuccess: () => void;
  onCancel: () => void;
}

const RULE_TYPE_OPTIONS: { value: RuleType; label: string; icon: React.ElementType; description: string }[] = [
  { value: 'vendor', label: 'Vendor', icon: Building2, description: 'Match by specific vendor' },
  { value: 'customer', label: 'Customer', icon: Users, description: 'Match by specific customer' },
  { value: 'keyword', label: 'Keyword', icon: Search, description: 'Match by description keywords' },
  { value: 'amount_range', label: 'Amount Range', icon: DollarSign, description: 'Match by transaction amount' },
  { value: 'account', label: 'Account', icon: Calculator, description: 'Match by ledger account' },
  { value: 'product', label: 'Product', icon: Package, description: 'Match by product/service' },
  { value: 'employee', label: 'Employee', icon: UserCheck, description: 'Match by employee' },
  { value: 'composite', label: 'Composite', icon: Layers, description: 'Combine multiple conditions' },
];

const TRANSACTION_TYPE_OPTIONS: { value: TransactionType; label: string }[] = [
  { value: 'invoice', label: 'Sales Invoice' },
  { value: 'vendor_invoice', label: 'Purchase Invoice' },
  { value: 'payment', label: 'Customer Payment' },
  { value: 'vendor_payment', label: 'Vendor Payment' },
  { value: 'expense_claim', label: 'Expense Claim' },
  { value: 'journal_entry', label: 'Journal Entry' },
  { value: 'bank_transaction', label: 'Bank Transaction' },
  { value: 'salary_transaction', label: 'Salary Transaction' },
  { value: 'contractor_payment', label: 'Contractor Payment' },
];

const ALLOCATION_METHOD_OPTIONS = [
  { value: 'full', label: 'Full Amount' },
  { value: 'percentage', label: 'Percentage Split' },
  { value: 'amount', label: 'Fixed Amount' },
  { value: 'split_equal', label: 'Equal Split' },
];

export const AttributionRuleForm = ({
  rule,
  onSuccess,
  onCancel,
}: AttributionRuleFormProps) => {
  const { selectedCompanyId } = useCompanyContext();

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [ruleType, setRuleType] = useState<RuleType>('vendor');
  const [appliesTo, setAppliesTo] = useState<TransactionType[]>(['invoice', 'vendor_invoice']);
  const [conditions, setConditions] = useState<RuleConditions>({});
  const [tagAssignments, setTagAssignments] = useState<TagAssignment[]>([]);
  const [allocationMethod, setAllocationMethod] = useState('full');
  const [priority, setPriority] = useState(100);
  const [stopOnMatch, setStopOnMatch] = useState(false);
  const [overwriteExisting, setOverwriteExisting] = useState(false);

  const [errors, setErrors] = useState<Record<string, string>>({});

  // Hooks
  const createRule = useCreateAttributionRule();
  const updateRule = useUpdateAttributionRule();
  const { data: tags = [] } = useTags(selectedCompanyId || undefined);
  const { data: vendors = [] } = useVendors(selectedCompanyId || undefined);
  const { data: customers = [] } = useCustomers(selectedCompanyId || undefined);

  const isEditing = !!rule;
  const isLoading = createRule.isPending || updateRule.isPending;

  // Populate form with existing rule data
  useEffect(() => {
    if (rule) {
      setName(rule.name);
      setDescription(rule.description || '');
      setRuleType(rule.ruleType);
      setAppliesTo(JSON.parse(rule.appliesTo || '[]'));
      setConditions(JSON.parse(rule.conditions || '{}'));
      setTagAssignments(JSON.parse(rule.tagAssignments || '[]'));
      setAllocationMethod(rule.allocationMethod || 'full');
      setPriority(rule.priority);
      setStopOnMatch(rule.stopOnMatch);
      setOverwriteExisting(rule.overwriteExisting);
    }
  }, [rule]);

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = 'Rule name is required';
    }

    if (appliesTo.length === 0) {
      newErrors.appliesTo = 'Select at least one transaction type';
    }

    if (tagAssignments.length === 0) {
      newErrors.tagAssignments = 'Select at least one tag to assign';
    }

    // Validate conditions based on rule type
    if (ruleType === 'vendor' && !conditions.vendorId) {
      newErrors.conditions = 'Select a vendor';
    }
    if (ruleType === 'customer' && !conditions.customerId) {
      newErrors.conditions = 'Select a customer';
    }
    if (ruleType === 'keyword' && (!conditions.keywords || conditions.keywords.length === 0)) {
      newErrors.conditions = 'Enter at least one keyword';
    }
    if (ruleType === 'amount_range' && conditions.minAmount === undefined && conditions.maxAmount === undefined) {
      newErrors.conditions = 'Set minimum or maximum amount';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) return;

    try {
      if (isEditing && rule) {
        const updateData: UpdateAttributionRuleDto = {
          name,
          description: description || undefined,
          ruleType,
          appliesTo,
          conditions,
          tagAssignments,
          allocationMethod,
          priority,
          stopOnMatch,
          overwriteExisting,
        };
        await updateRule.mutateAsync({ id: rule.id, data: updateData });
      } else {
        const createData: CreateAttributionRuleDto = {
          companyId: selectedCompanyId || undefined,
          name,
          description: description || undefined,
          ruleType,
          appliesTo,
          conditions,
          tagAssignments,
          allocationMethod,
          priority,
          stopOnMatch,
          overwriteExisting,
        };
        await createRule.mutateAsync(createData);
      }
      onSuccess();
    } catch (error) {
      console.error('Form submission error:', error);
    }
  };

  const handleTransactionTypeToggle = (type: TransactionType) => {
    setAppliesTo((prev) =>
      prev.includes(type) ? prev.filter((t) => t !== type) : [...prev, type]
    );
    if (errors.appliesTo) {
      setErrors((prev) => ({ ...prev, appliesTo: '' }));
    }
  };

  const handleAddTag = (tagId: string) => {
    if (!tagAssignments.find((ta) => ta.tagId === tagId)) {
      setTagAssignments((prev) => [...prev, { tagId, allocationPercentage: 100 }]);
    }
    if (errors.tagAssignments) {
      setErrors((prev) => ({ ...prev, tagAssignments: '' }));
    }
  };

  const handleRemoveTag = (tagId: string) => {
    setTagAssignments((prev) => prev.filter((ta) => ta.tagId !== tagId));
  };

  const handleKeywordAdd = (keyword: string) => {
    if (keyword.trim()) {
      setConditions((prev) => ({
        ...prev,
        keywords: [...(prev.keywords || []), keyword.trim()],
      }));
    }
  };

  const handleKeywordRemove = (keyword: string) => {
    setConditions((prev) => ({
      ...prev,
      keywords: (prev.keywords || []).filter((k) => k !== keyword),
    }));
  };

  const renderConditionsBuilder = () => {
    switch (ruleType) {
      case 'vendor':
        return (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Select Vendor *
            </label>
            <select
              value={conditions.vendorId || ''}
              onChange={(e) => setConditions({ vendorId: e.target.value || undefined })}
              className={cn(
                'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                errors.conditions ? 'border-red-500' : 'border-gray-300'
              )}
            >
              <option value="">Select a vendor</option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </div>
        );

      case 'customer':
        return (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Select Customer *
            </label>
            <select
              value={conditions.customerId || ''}
              onChange={(e) => setConditions({ customerId: e.target.value || undefined })}
              className={cn(
                'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                errors.conditions ? 'border-red-500' : 'border-gray-300'
              )}
            >
              <option value="">Select a customer</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
        );

      case 'keyword':
        return (
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Keywords *
            </label>
            <div className="flex flex-wrap gap-2 mb-2">
              {(conditions.keywords || []).map((keyword) => (
                <span
                  key={keyword}
                  className="inline-flex items-center px-2.5 py-1 rounded-full text-sm bg-blue-100 text-blue-800"
                >
                  {keyword}
                  <button
                    type="button"
                    onClick={() => handleKeywordRemove(keyword)}
                    className="ml-1.5 text-blue-600 hover:text-blue-800"
                  >
                    <X size={14} />
                  </button>
                </span>
              ))}
            </div>
            <div className="flex gap-2">
              <input
                type="text"
                placeholder="Enter keyword and press Add"
                className={cn(
                  'flex-1 px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.conditions ? 'border-red-500' : 'border-gray-300'
                )}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    e.preventDefault();
                    handleKeywordAdd((e.target as HTMLInputElement).value);
                    (e.target as HTMLInputElement).value = '';
                  }
                }}
              />
              <button
                type="button"
                onClick={(e) => {
                  const input = e.currentTarget.previousSibling as HTMLInputElement;
                  handleKeywordAdd(input.value);
                  input.value = '';
                }}
                className="px-3 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90"
              >
                Add
              </button>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              Transaction description must contain any of these keywords
            </p>
          </div>
        );

      case 'amount_range':
        return (
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Minimum Amount
              </label>
              <input
                type="number"
                step="0.01"
                value={conditions.minAmount ?? ''}
                onChange={(e) =>
                  setConditions((prev) => ({
                    ...prev,
                    minAmount: e.target.value ? parseFloat(e.target.value) : undefined,
                  }))
                }
                className={cn(
                  'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
                  errors.conditions ? 'border-red-500' : 'border-gray-300'
                )}
                placeholder="0.00"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Maximum Amount
              </label>
              <input
                type="number"
                step="0.01"
                value={conditions.maxAmount ?? ''}
                onChange={(e) =>
                  setConditions((prev) => ({
                    ...prev,
                    maxAmount: e.target.value ? parseFloat(e.target.value) : undefined,
                  }))
                }
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
                placeholder="No limit"
              />
            </div>
          </div>
        );

      default:
        return (
          <div className="p-4 bg-gray-50 rounded-md text-center text-gray-500">
            Conditions builder for "{ruleType}" coming soon.
          </div>
        );
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* Basic Info */}
      <div className="space-y-4">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            Rule Name *
          </label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => {
              setName(e.target.value);
              if (errors.name) setErrors((prev) => ({ ...prev, name: '' }));
            }}
            className={cn(
              'w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-ring',
              errors.name ? 'border-red-500' : 'border-gray-300'
            )}
            placeholder="e.g., Tag all Acme Corp invoices"
          />
          {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
        </div>

        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
            Description
          </label>
          <textarea
            id="description"
            rows={2}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Describe what this rule does..."
          />
        </div>
      </div>

      {/* Rule Type */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">Rule Type *</label>
        <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
          {RULE_TYPE_OPTIONS.map((opt) => {
            const Icon = opt.icon;
            const isSelected = ruleType === opt.value;
            return (
              <button
                key={opt.value}
                type="button"
                onClick={() => {
                  setRuleType(opt.value);
                  setConditions({});
                }}
                className={cn(
                  'p-3 rounded-lg border-2 text-left transition-all',
                  isSelected
                    ? 'border-primary bg-primary/5'
                    : 'border-gray-200 hover:border-gray-300'
                )}
              >
                <Icon
                  className={cn(
                    'w-5 h-5 mb-1',
                    isSelected ? 'text-primary' : 'text-gray-400'
                  )}
                />
                <div className={cn('text-sm font-medium', isSelected ? 'text-primary' : 'text-gray-900')}>
                  {opt.label}
                </div>
              </button>
            );
          })}
        </div>
      </div>

      {/* Transaction Types */}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-2">
          Apply to Transaction Types *
        </label>
        <div className="flex flex-wrap gap-2">
          {TRANSACTION_TYPE_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => handleTransactionTypeToggle(opt.value)}
              className={cn(
                'px-3 py-1.5 rounded-full text-sm font-medium transition-colors',
                appliesTo.includes(opt.value)
                  ? 'bg-primary text-white'
                  : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
              )}
            >
              {opt.label}
            </button>
          ))}
        </div>
        {errors.appliesTo && <p className="text-red-500 text-sm mt-1">{errors.appliesTo}</p>}
      </div>

      {/* Conditions */}
      <div className="border-t pt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Conditions</h3>
        {renderConditionsBuilder()}
        {errors.conditions && (
          <p className="text-red-500 text-sm mt-1 flex items-center gap-1">
            <AlertCircle size={14} />
            {errors.conditions}
          </p>
        )}
      </div>

      {/* Tag Assignments */}
      <div className="border-t pt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Assign Tags *</h3>
        <div className="space-y-3">
          {tagAssignments.map((ta) => {
            const tag = tags.find((t) => t.id === ta.tagId);
            return (
              <div key={ta.tagId} className="flex items-center gap-3 p-2 bg-gray-50 rounded-md">
                <div
                  className="w-3 h-3 rounded-full flex-shrink-0"
                  style={{ backgroundColor: tag?.color || '#6B7280' }}
                />
                <span className="flex-1 text-sm font-medium">{tag?.name || 'Unknown Tag'}</span>
                {allocationMethod === 'percentage' && (
                  <input
                    type="number"
                    min="0"
                    max="100"
                    value={ta.allocationPercentage || 100}
                    onChange={(e) => {
                      setTagAssignments((prev) =>
                        prev.map((item) =>
                          item.tagId === ta.tagId
                            ? { ...item, allocationPercentage: parseInt(e.target.value) || 0 }
                            : item
                        )
                      );
                    }}
                    className="w-20 px-2 py-1 text-sm border border-gray-300 rounded"
                  />
                )}
                <button
                  type="button"
                  onClick={() => handleRemoveTag(ta.tagId)}
                  className="text-gray-400 hover:text-red-600"
                >
                  <X size={16} />
                </button>
              </div>
            );
          })}
          <select
            value=""
            onChange={(e) => {
              if (e.target.value) handleAddTag(e.target.value);
            }}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="">+ Add a tag to assign</option>
            {tags
              .filter((t) => t.isActive && !tagAssignments.find((ta) => ta.tagId === t.id))
              .map((t) => (
                <option key={t.id} value={t.id}>
                  [{t.tagGroup}] {t.name}
                </option>
              ))}
          </select>
        </div>
        {errors.tagAssignments && (
          <p className="text-red-500 text-sm mt-1">{errors.tagAssignments}</p>
        )}
      </div>

      {/* Options */}
      <div className="border-t pt-4">
        <h3 className="text-sm font-semibold text-gray-900 mb-3">Options</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Allocation Method
            </label>
            <select
              value={allocationMethod}
              onChange={(e) => setAllocationMethod(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {ALLOCATION_METHOD_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Priority</label>
            <input
              type="number"
              min="1"
              value={priority}
              onChange={(e) => setPriority(parseInt(e.target.value) || 100)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
            />
            <p className="text-xs text-gray-500 mt-1">Lower = higher priority (evaluated first)</p>
          </div>
        </div>
        <div className="mt-4 space-y-2">
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={stopOnMatch}
              onChange={(e) => setStopOnMatch(e.target.checked)}
              className="rounded border-gray-300 text-primary focus:ring-primary"
            />
            <span className="text-sm text-gray-700">Stop evaluating other rules after match</span>
          </label>
          <label className="flex items-center gap-2">
            <input
              type="checkbox"
              checked={overwriteExisting}
              onChange={(e) => setOverwriteExisting(e.target.checked)}
              className="rounded border-gray-300 text-primary focus:ring-primary"
            />
            <span className="text-sm text-gray-700">Overwrite existing tags (if any)</span>
          </label>
        </div>
      </div>

      {/* Form Actions */}
      <div className="flex justify-end space-x-3 pt-4 border-t">
        <button
          type="button"
          onClick={onCancel}
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={isLoading}
          className="px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90 disabled:opacity-50"
        >
          {isLoading ? 'Saving...' : isEditing ? 'Update Rule' : 'Create Rule'}
        </button>
      </div>
    </form>
  );
};
