import { useState, useMemo } from 'react';
import {
  useAttributionRules,
  useDeleteAttributionRule,
  useReorderRulePriorities,
  useUpdateAttributionRule,
} from '@/features/tags/hooks';
import { useCompanyContext } from '@/contexts/CompanyContext';
import { AttributionRule, RuleType } from '@/services/api/types';
import { Drawer } from '@/components/ui/Drawer';
import { Modal } from '@/components/ui/Modal';
import { AttributionRuleForm } from '@/components/forms/AttributionRuleForm';
import {
  Sparkles,
  Plus,
  Edit,
  Trash2,
  GripVertical,
  Play,
  Pause,
  ChevronLeft,
  Building2,
  Users,
  Calculator,
  Package,
  Search,
  DollarSign,
  UserCheck,
  Layers,
  TrendingUp,
  Clock,
  Hash,
} from 'lucide-react';
import { Link } from 'react-router-dom';
import { formatDistanceToNow } from 'date-fns';

const RULE_TYPE_CONFIG: Record<RuleType, { label: string; icon: React.ElementType; color: string }> = {
  vendor: { label: 'Vendor', icon: Building2, color: 'blue' },
  customer: { label: 'Customer', icon: Users, color: 'green' },
  account: { label: 'Account', icon: Calculator, color: 'purple' },
  product: { label: 'Product', icon: Package, color: 'orange' },
  keyword: { label: 'Keyword', icon: Search, color: 'pink' },
  amount_range: { label: 'Amount Range', icon: DollarSign, color: 'yellow' },
  employee: { label: 'Employee', icon: UserCheck, color: 'teal' },
  composite: { label: 'Composite', icon: Layers, color: 'indigo' },
};

const RuleCard = ({
  rule,
  onEdit,
  onDelete,
  onToggleActive,
  isUpdating,
}: {
  rule: AttributionRule;
  onEdit: (rule: AttributionRule) => void;
  onDelete: (rule: AttributionRule) => void;
  onToggleActive: (rule: AttributionRule) => void;
  isUpdating: boolean;
}) => {
  const config = RULE_TYPE_CONFIG[rule.ruleType];
  const Icon = config.icon;

  const appliesTo = JSON.parse(rule.appliesTo || '[]');
  const tagAssignments = JSON.parse(rule.tagAssignments || '[]');

  return (
    <div
      className={`bg-white rounded-lg border p-4 hover:shadow-md transition-shadow ${
        !rule.isActive ? 'opacity-60' : ''
      }`}
    >
      <div className="flex items-start gap-3">
        {/* Drag Handle */}
        <div className="mt-1 cursor-grab text-gray-400 hover:text-gray-600">
          <GripVertical size={16} />
        </div>

        {/* Rule Type Icon */}
        <div
          className={`flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center bg-${config.color}-100`}
        >
          <Icon className={`w-5 h-5 text-${config.color}-600`} />
        </div>

        {/* Rule Content */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h3 className="font-medium text-gray-900 truncate">{rule.name}</h3>
            <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-600">
              #{rule.priority}
            </span>
            {rule.stopOnMatch && (
              <span className="px-2 py-0.5 text-xs font-medium rounded-full bg-yellow-100 text-yellow-700">
                Stop on Match
              </span>
            )}
          </div>

          {rule.description && (
            <p className="text-sm text-gray-500 mt-1 truncate">{rule.description}</p>
          )}

          <div className="flex flex-wrap items-center gap-3 mt-2 text-xs text-gray-500">
            {/* Rule Type */}
            <span className={`inline-flex items-center gap-1 text-${config.color}-600`}>
              <Icon size={12} />
              {config.label}
            </span>

            {/* Applies To */}
            <span className="inline-flex items-center gap-1">
              <Hash size={12} />
              {appliesTo.length} transaction type{appliesTo.length !== 1 ? 's' : ''}
            </span>

            {/* Tags Assigned */}
            <span className="inline-flex items-center gap-1">
              <Sparkles size={12} />
              {tagAssignments.length} tag{tagAssignments.length !== 1 ? 's' : ''}
            </span>
          </div>

          {/* Stats */}
          <div className="flex flex-wrap items-center gap-4 mt-3 text-xs">
            <div className="flex items-center gap-1.5 text-gray-600">
              <TrendingUp size={12} className="text-green-500" />
              <span>{rule.timesApplied.toLocaleString()} applied</span>
            </div>
            <div className="flex items-center gap-1.5 text-gray-600">
              <DollarSign size={12} className="text-blue-500" />
              <span>
                {new Intl.NumberFormat('en-IN', {
                  style: 'currency',
                  currency: 'INR',
                  notation: 'compact',
                  maximumFractionDigits: 1,
                }).format(rule.totalAmountTagged)}
              </span>
            </div>
            {rule.lastAppliedAt && (
              <div className="flex items-center gap-1.5 text-gray-500">
                <Clock size={12} />
                <span>Last: {formatDistanceToNow(new Date(rule.lastAppliedAt), { addSuffix: true })}</span>
              </div>
            )}
          </div>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-1">
          <button
            onClick={() => onToggleActive(rule)}
            disabled={isUpdating}
            className={`p-2 rounded-md transition-colors ${
              rule.isActive
                ? 'text-green-600 hover:bg-green-50'
                : 'text-gray-400 hover:bg-gray-50'
            }`}
            title={rule.isActive ? 'Disable rule' : 'Enable rule'}
          >
            {rule.isActive ? <Play size={16} /> : <Pause size={16} />}
          </button>
          <button
            onClick={() => onEdit(rule)}
            className="p-2 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-md"
            title="Edit rule"
          >
            <Edit size={16} />
          </button>
          <button
            onClick={() => onDelete(rule)}
            className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-md"
            title="Delete rule"
          >
            <Trash2 size={16} />
          </button>
        </div>
      </div>
    </div>
  );
};

const AttributionRulesManagement = () => {
  const { selectedCompanyId } = useCompanyContext();
  const [filterType, setFilterType] = useState<RuleType | 'all'>('all');
  const [showActiveOnly, setShowActiveOnly] = useState(false);
  const [isCreateDrawerOpen, setIsCreateDrawerOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<AttributionRule | null>(null);
  const [deletingRule, setDeletingRule] = useState<AttributionRule | null>(null);

  const { data: rules = [], isLoading, error, refetch } = useAttributionRules(selectedCompanyId || undefined);
  const deleteRule = useDeleteAttributionRule();
  const updateRule = useUpdateAttributionRule();
  const reorderPriorities = useReorderRulePriorities();

  // Filter rules
  const filteredRules = useMemo(() => {
    let result = [...rules];

    if (filterType !== 'all') {
      result = result.filter((r) => r.ruleType === filterType);
    }

    if (showActiveOnly) {
      result = result.filter((r) => r.isActive);
    }

    // Sort by priority
    return result.sort((a, b) => a.priority - b.priority);
  }, [rules, filterType, showActiveOnly]);

  // Stats
  const stats = useMemo(() => {
    return {
      total: rules.length,
      active: rules.filter((r) => r.isActive).length,
      totalApplied: rules.reduce((sum, r) => sum + r.timesApplied, 0),
      totalTagged: rules.reduce((sum, r) => sum + r.totalAmountTagged, 0),
    };
  }, [rules]);

  const handleEdit = (rule: AttributionRule) => {
    setEditingRule(rule);
  };

  const handleDelete = (rule: AttributionRule) => {
    setDeletingRule(rule);
  };

  const handleToggleActive = async (rule: AttributionRule) => {
    try {
      await updateRule.mutateAsync({
        id: rule.id,
        data: { isActive: !rule.isActive },
      });
      refetch();
    } catch (error) {
      console.error('Failed to toggle rule:', error);
    }
  };

  const handleDeleteConfirm = async () => {
    if (deletingRule) {
      try {
        await deleteRule.mutateAsync(deletingRule.id);
        setDeletingRule(null);
      } catch (error) {
        console.error('Failed to delete rule:', error);
      }
    }
  };

  const handleFormSuccess = () => {
    setIsCreateDrawerOpen(false);
    setEditingRule(null);
    refetch();
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">Failed to load attribution rules</div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 bg-primary text-white rounded-md hover:bg-primary/90"
        >
          Retry
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link
            to="/settings/tags"
            className="p-2 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-md"
          >
            <ChevronLeft size={20} />
          </Link>
          <div>
            <h1 className="text-3xl font-bold text-gray-900">Auto-Attribution Rules</h1>
            <p className="text-gray-600 mt-1">
              Automatically assign tags to transactions based on conditions
            </p>
          </div>
        </div>
        <button
          onClick={() => setIsCreateDrawerOpen(true)}
          className="inline-flex items-center px-4 py-2 text-sm font-medium text-white bg-primary rounded-md hover:bg-primary/90"
        >
          <Plus size={16} className="mr-2" />
          Create Rule
        </button>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg border p-4">
          <div className="text-sm text-gray-500">Total Rules</div>
          <div className="text-2xl font-bold text-gray-900 mt-1">{stats.total}</div>
        </div>
        <div className="bg-white rounded-lg border p-4">
          <div className="text-sm text-gray-500">Active Rules</div>
          <div className="text-2xl font-bold text-green-600 mt-1">{stats.active}</div>
        </div>
        <div className="bg-white rounded-lg border p-4">
          <div className="text-sm text-gray-500">Times Applied</div>
          <div className="text-2xl font-bold text-blue-600 mt-1">
            {stats.totalApplied.toLocaleString()}
          </div>
        </div>
        <div className="bg-white rounded-lg border p-4">
          <div className="text-sm text-gray-500">Total Tagged</div>
          <div className="text-2xl font-bold text-purple-600 mt-1">
            {new Intl.NumberFormat('en-IN', {
              style: 'currency',
              currency: 'INR',
              notation: 'compact',
              maximumFractionDigits: 1,
            }).format(stats.totalTagged)}
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-4">
        <select
          value={filterType}
          onChange={(e) => setFilterType(e.target.value as RuleType | 'all')}
          className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-ring"
        >
          <option value="all">All Types</option>
          {(Object.entries(RULE_TYPE_CONFIG) as [RuleType, typeof RULE_TYPE_CONFIG[RuleType]][]).map(
            ([type, config]) => (
              <option key={type} value={type}>
                {config.label}
              </option>
            )
          )}
        </select>
        <label className="flex items-center gap-2 text-sm text-gray-600">
          <input
            type="checkbox"
            checked={showActiveOnly}
            onChange={(e) => setShowActiveOnly(e.target.checked)}
            className="rounded border-gray-300 text-primary focus:ring-primary"
          />
          Active only
        </label>
        <div className="flex-1" />
        <span className="text-sm text-gray-500">
          Showing {filteredRules.length} of {rules.length} rules
        </span>
      </div>

      {/* Rules List */}
      <div className="space-y-3">
        {filteredRules.length === 0 ? (
          <div className="text-center py-12 bg-white rounded-lg border">
            <Sparkles className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <p className="text-gray-500">No attribution rules yet.</p>
            <p className="text-sm text-gray-400 mt-1">
              Create a rule to automatically tag your transactions.
            </p>
            <button
              onClick={() => setIsCreateDrawerOpen(true)}
              className="mt-4 inline-flex items-center px-4 py-2 text-sm font-medium text-primary border border-primary rounded-md hover:bg-primary/5"
            >
              <Plus size={16} className="mr-2" />
              Create your first rule
            </button>
          </div>
        ) : (
          filteredRules.map((rule) => (
            <RuleCard
              key={rule.id}
              rule={rule}
              onEdit={handleEdit}
              onDelete={handleDelete}
              onToggleActive={handleToggleActive}
              isUpdating={updateRule.isPending}
            />
          ))
        )}
      </div>

      {/* Create Rule Drawer */}
      <Drawer
        isOpen={isCreateDrawerOpen}
        onClose={() => setIsCreateDrawerOpen(false)}
        title="Create Attribution Rule"
        size="lg"
      >
        <AttributionRuleForm
          onSuccess={handleFormSuccess}
          onCancel={() => setIsCreateDrawerOpen(false)}
        />
      </Drawer>

      {/* Edit Rule Drawer */}
      <Drawer
        isOpen={!!editingRule}
        onClose={() => setEditingRule(null)}
        title="Edit Attribution Rule"
        size="lg"
      >
        {editingRule && (
          <AttributionRuleForm
            rule={editingRule}
            onSuccess={handleFormSuccess}
            onCancel={() => setEditingRule(null)}
          />
        )}
      </Drawer>

      {/* Delete Confirmation Modal */}
      <Modal
        isOpen={!!deletingRule}
        onClose={() => setDeletingRule(null)}
        title="Delete Attribution Rule"
        size="sm"
      >
        {deletingRule && (
          <div className="space-y-4">
            <p className="text-gray-700">
              Are you sure you want to delete <strong>{deletingRule.name}</strong>?
            </p>
            {deletingRule.timesApplied > 0 && (
              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                <p className="text-sm text-yellow-800">
                  This rule has been applied {deletingRule.timesApplied.toLocaleString()} times.
                  Existing tags will not be affected.
                </p>
              </div>
            )}
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setDeletingRule(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50"
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteConfirm}
                disabled={deleteRule.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 disabled:opacity-50"
              >
                {deleteRule.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default AttributionRulesManagement;
