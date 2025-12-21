import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  useApprovalTemplate,
  useUpdateApprovalTemplate,
  useAddApprovalStep,
  useUpdateApprovalStep,
  useDeleteApprovalStep,
  useReorderApprovalSteps,
} from '@/hooks/api/useApprovalWorkflow';
import { useEmployees } from '@/hooks/api/useEmployees';
import {
  ApprovalWorkflowStep,
  UpdateApprovalTemplateDto,
  CreateApprovalStepDto,
  UpdateApprovalStepDto,
  ApproverType,
} from 'shared-types';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  ArrowLeft,
  Plus,
  GripVertical,
  Edit,
  Trash2,
  Loader2,
  Save,
  CheckCircle,
  User,
  Users,
  UserCheck,
  Building,
} from 'lucide-react';
import { cn } from '@/lib/utils';

const APPROVER_TYPES: { value: ApproverType; label: string; description: string; icon: any }[] = [
  {
    value: 'direct_manager',
    label: 'Direct Manager',
    description: "The requestor's immediate supervisor",
    icon: User,
  },
  {
    value: 'skip_level_manager',
    label: 'Skip-Level Manager',
    description: "The manager's manager",
    icon: Users,
  },
  {
    value: 'role',
    label: 'Role-Based',
    description: 'Someone with a specific role (HR, Finance, etc.)',
    icon: UserCheck,
  },
  {
    value: 'specific_user',
    label: 'Specific User',
    description: 'A designated employee',
    icon: User,
  },
  {
    value: 'department_head',
    label: 'Department Head',
    description: "The head of the requestor's department",
    icon: Building,
  },
];

const ROLES = ['HR', 'Finance', 'Admin', 'Manager', 'Director'];

export default function WorkflowTemplateEditor() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [templateForm, setTemplateForm] = useState<UpdateApprovalTemplateDto>({
    name: '',
    description: '',
    isActive: true,
    isDefault: false,
  });
  const [isStepDialogOpen, setIsStepDialogOpen] = useState(false);
  const [editingStep, setEditingStep] = useState<ApprovalWorkflowStep | null>(null);
  const [stepForm, setStepForm] = useState<CreateApprovalStepDto>({
    name: '',
    approverType: 'direct_manager',
    approverRole: undefined,
    approverUserId: undefined,
    isRequired: true,
    canSkip: false,
    autoApproveAfterDays: undefined,
  });
  const [deleteStep, setDeleteStep] = useState<ApprovalWorkflowStep | null>(null);

  const { data: template, isLoading, refetch } = useApprovalTemplate(id || '', !!id);
  const { data: employees = [] } = useEmployees(
    template?.companyId ? { companyId: template.companyId } : {}
  );

  const updateTemplateMutation = useUpdateApprovalTemplate();
  const addStepMutation = useAddApprovalStep();
  const updateStepMutation = useUpdateApprovalStep();
  const deleteStepMutation = useDeleteApprovalStep();
  const reorderMutation = useReorderApprovalSteps();

  useEffect(() => {
    if (template) {
      setTemplateForm({
        name: template.name,
        description: template.description || '',
        isActive: template.isActive,
        isDefault: template.isDefault,
      });
    }
  }, [template]);

  const handleSaveTemplate = async () => {
    if (!id) return;
    await updateTemplateMutation.mutateAsync({ id, data: templateForm });
    refetch();
  };

  const handleOpenStepDialog = (step?: ApprovalWorkflowStep) => {
    if (step) {
      setEditingStep(step);
      setStepForm({
        name: step.name,
        approverType: step.approverType,
        approverRole: step.approverRole || undefined,
        approverUserId: step.approverUserId || undefined,
        isRequired: step.isRequired,
        canSkip: step.canSkip,
        autoApproveAfterDays: step.autoApproveAfterDays || undefined,
      });
    } else {
      setEditingStep(null);
      setStepForm({
        name: '',
        approverType: 'direct_manager',
        approverRole: undefined,
        approverUserId: undefined,
        isRequired: true,
        canSkip: false,
        autoApproveAfterDays: undefined,
      });
    }
    setIsStepDialogOpen(true);
  };

  const handleSaveStep = async () => {
    if (!id) return;

    if (editingStep) {
      await updateStepMutation.mutateAsync({
        templateId: id,
        stepId: editingStep.id,
        data: stepForm as UpdateApprovalStepDto,
      });
    } else {
      await addStepMutation.mutateAsync({
        templateId: id,
        data: stepForm,
      });
    }
    setIsStepDialogOpen(false);
    refetch();
  };

  const handleDeleteStep = async () => {
    if (!id || !deleteStep) return;
    await deleteStepMutation.mutateAsync({ templateId: id, stepId: deleteStep.id });
    setDeleteStep(null);
    refetch();
  };

  const getApproverTypeInfo = (type: ApproverType) => {
    return APPROVER_TYPES.find((t) => t.value === type);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-gray-400" />
      </div>
    );
  }

  if (!template) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Template not found</p>
        <Button variant="outline" className="mt-4" onClick={() => navigate('/workflows')}>
          Back to Templates
        </Button>
      </div>
    );
  }

  const sortedSteps = [...(template.steps || [])].sort((a, b) => a.stepOrder - b.stepOrder);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="icon" onClick={() => navigate('/workflows')}>
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div className="flex-1">
          <h1 className="text-2xl font-bold text-gray-900">Edit Workflow Template</h1>
          <p className="text-gray-500">Configure approval steps for {template.name}</p>
        </div>
        <Button onClick={handleSaveTemplate} disabled={updateTemplateMutation.isPending}>
          {updateTemplateMutation.isPending ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Save className="mr-2 h-4 w-4" />
          )}
          Save Changes
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-6">
        {/* Template Settings */}
        <Card className="col-span-1">
          <CardHeader>
            <CardTitle>Template Settings</CardTitle>
            <CardDescription>Configure the basic template properties</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="name">Template Name</Label>
              <Input
                id="name"
                value={templateForm.name}
                onChange={(e) => setTemplateForm({ ...templateForm, name: e.target.value })}
                className="mt-1"
              />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={templateForm.description || ''}
                onChange={(e) => setTemplateForm({ ...templateForm, description: e.target.value })}
                className="mt-1"
                rows={3}
              />
            </div>
            <div className="flex items-center justify-between">
              <div>
                <Label>Active</Label>
                <p className="text-sm text-gray-500">Enable this template for use</p>
              </div>
              <Switch
                checked={templateForm.isActive}
                onCheckedChange={(checked) => setTemplateForm({ ...templateForm, isActive: checked })}
              />
            </div>
            <div className="flex items-center justify-between">
              <div>
                <Label>Default Template</Label>
                <p className="text-sm text-gray-500">Use as default for this activity type</p>
              </div>
              <Switch
                checked={templateForm.isDefault}
                onCheckedChange={(checked) => setTemplateForm({ ...templateForm, isDefault: checked })}
              />
            </div>
            <div className="pt-4 border-t">
              <p className="text-sm text-gray-500">
                Activity Type:{' '}
                <Badge variant="secondary" className="ml-1">
                  {template.activityType}
                </Badge>
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Workflow Steps */}
        <Card className="col-span-2">
          <CardHeader className="flex flex-row items-center justify-between">
            <div>
              <CardTitle>Approval Steps</CardTitle>
              <CardDescription>Define the sequence of approvers</CardDescription>
            </div>
            <Button onClick={() => handleOpenStepDialog()}>
              <Plus className="mr-2 h-4 w-4" />
              Add Step
            </Button>
          </CardHeader>
          <CardContent>
            {sortedSteps.length === 0 ? (
              <div className="text-center py-8 border-2 border-dashed rounded-lg">
                <CheckCircle className="h-12 w-12 mx-auto text-gray-300 mb-4" />
                <p className="text-gray-500">No approval steps configured</p>
                <p className="text-sm text-gray-400 mt-1">Add steps to define the approval flow</p>
                <Button variant="outline" className="mt-4" onClick={() => handleOpenStepDialog()}>
                  <Plus className="mr-2 h-4 w-4" />
                  Add First Step
                </Button>
              </div>
            ) : (
              <div className="space-y-3">
                {sortedSteps.map((step, index) => {
                  const typeInfo = getApproverTypeInfo(step.approverType);
                  const TypeIcon = typeInfo?.icon || User;

                  return (
                    <div
                      key={step.id}
                      className={cn(
                        'flex items-center gap-4 p-4 border rounded-lg bg-white',
                        'hover:border-gray-300 transition-colors'
                      )}
                    >
                      <div className="flex items-center gap-2 text-gray-400">
                        <GripVertical className="h-5 w-5 cursor-move" />
                        <span className="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center font-medium text-sm">
                          {step.stepOrder}
                        </span>
                      </div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <h4 className="font-medium">{step.name}</h4>
                          {step.isRequired && (
                            <Badge variant="outline" className="text-xs">
                              Required
                            </Badge>
                          )}
                          {step.canSkip && (
                            <Badge variant="secondary" className="text-xs">
                              Can Skip
                            </Badge>
                          )}
                        </div>
                        <div className="flex items-center gap-2 mt-1 text-sm text-gray-500">
                          <TypeIcon className="h-4 w-4" />
                          <span>{typeInfo?.label}</span>
                          {step.approverRole && (
                            <span className="text-gray-400">({step.approverRole})</span>
                          )}
                          {step.approverUserName && (
                            <span className="text-gray-400">({step.approverUserName})</span>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center gap-2">
                        <Button variant="ghost" size="sm" onClick={() => handleOpenStepDialog(step)}>
                          <Edit className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-red-600 hover:text-red-700"
                          onClick={() => setDeleteStep(step)}
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Step Dialog */}
      <Dialog open={isStepDialogOpen} onOpenChange={setIsStepDialogOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{editingStep ? 'Edit Step' : 'Add Step'}</DialogTitle>
            <DialogDescription>
              {editingStep
                ? 'Modify the step configuration'
                : 'Configure a new approval step'}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <Label htmlFor="stepName">Step Name *</Label>
              <Input
                id="stepName"
                value={stepForm.name}
                onChange={(e) => setStepForm({ ...stepForm, name: e.target.value })}
                placeholder="e.g., Manager Approval"
                className="mt-1"
              />
            </div>
            <div>
              <Label>Approver Type *</Label>
              <Select
                value={stepForm.approverType}
                onValueChange={(value: ApproverType) =>
                  setStepForm({ ...stepForm, approverType: value, approverRole: undefined, approverUserId: undefined })
                }
              >
                <SelectTrigger className="mt-1">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {APPROVER_TYPES.map((type) => (
                    <SelectItem key={type.value} value={type.value}>
                      <div className="flex items-center gap-2">
                        <type.icon className="h-4 w-4" />
                        <span>{type.label}</span>
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-sm text-gray-500 mt-1">
                {APPROVER_TYPES.find((t) => t.value === stepForm.approverType)?.description}
              </p>
            </div>

            {stepForm.approverType === 'role' && (
              <div>
                <Label>Role *</Label>
                <Select
                  value={stepForm.approverRole || ''}
                  onValueChange={(value) => setStepForm({ ...stepForm, approverRole: value })}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                  <SelectContent>
                    {ROLES.map((role) => (
                      <SelectItem key={role} value={role}>
                        {role}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {stepForm.approverType === 'specific_user' && (
              <div>
                <Label>Select Employee *</Label>
                <Select
                  value={stepForm.approverUserId || ''}
                  onValueChange={(value) => setStepForm({ ...stepForm, approverUserId: value })}
                >
                  <SelectTrigger className="mt-1">
                    <SelectValue placeholder="Select employee" />
                  </SelectTrigger>
                  <SelectContent>
                    {employees.map((emp: any) => (
                      <SelectItem key={emp.id} value={emp.id}>
                        {emp.employeeName || `${emp.firstName} ${emp.lastName}`}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="grid grid-cols-2 gap-4">
              <div className="flex items-center justify-between">
                <Label>Required</Label>
                <Switch
                  checked={stepForm.isRequired}
                  onCheckedChange={(checked) => setStepForm({ ...stepForm, isRequired: checked })}
                />
              </div>
              <div className="flex items-center justify-between">
                <Label>Can Skip</Label>
                <Switch
                  checked={stepForm.canSkip}
                  onCheckedChange={(checked) => setStepForm({ ...stepForm, canSkip: checked })}
                />
              </div>
            </div>

            <div>
              <Label htmlFor="autoApprove">Auto-approve after (days)</Label>
              <Input
                id="autoApprove"
                type="number"
                min="0"
                value={stepForm.autoApproveAfterDays || ''}
                onChange={(e) =>
                  setStepForm({
                    ...stepForm,
                    autoApproveAfterDays: e.target.value ? parseInt(e.target.value) : undefined,
                  })
                }
                placeholder="Leave empty for no auto-approval"
                className="mt-1"
              />
              <p className="text-sm text-gray-500 mt-1">
                Optionally auto-approve if no action is taken within this many days
              </p>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsStepDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleSaveStep}
              disabled={
                addStepMutation.isPending ||
                updateStepMutation.isPending ||
                !stepForm.name ||
                (stepForm.approverType === 'role' && !stepForm.approverRole) ||
                (stepForm.approverType === 'specific_user' && !stepForm.approverUserId)
              }
            >
              {(addStepMutation.isPending || updateStepMutation.isPending) && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              {editingStep ? 'Update Step' : 'Add Step'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Step Confirmation */}
      <Dialog open={!!deleteStep} onOpenChange={() => setDeleteStep(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Step</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete "{deleteStep?.name}"? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteStep(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteStep}
              disabled={deleteStepMutation.isPending}
            >
              {deleteStepMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
