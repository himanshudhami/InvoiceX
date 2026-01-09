# Approvals Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Approvals module provides a generic, configurable approval workflow engine that can be attached to any business activity. Supports multi-level approvals, role-based and user-based assignment, conditional routing, and auto-escalation.

### Key Features
- Configurable workflow templates
- Multi-step approval chains
- Role-based and user-specific approvers
- Conditional approval routing
- Auto-approval after timeout
- Step skip options
- Approval request tracking
- Audit trail for all actions

### Key Entities
- **Approval Workflow Templates** - Workflow definitions
- **Approval Workflow Steps** - Steps in template
- **Approval Requests** - Active approval instances
- **Approval Request Steps** - Per-request step status

---

## Database Schema

### approval_workflow_templates
Workflow template definitions by activity type.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `activity_type` | VARCHAR | Activity type (e.g., `expense`, `leave`, `purchase`) |
| `name` | VARCHAR | Template name |
| `description` | TEXT | Description |
| `is_active` | BOOLEAN | Active flag |
| `is_default` | BOOLEAN | Default for activity type |

### approval_workflow_steps
Steps within a workflow template.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `template_id` | UUID | FK to templates |
| `step_order` | INTEGER | Step sequence |
| `name` | VARCHAR | Step name |
| **Approver** |
| `approver_type` | VARCHAR | `role`, `user`, `manager`, `department_head` |
| `approver_role` | VARCHAR | Role name (if type=role) |
| `approver_user_id` | UUID | Specific user (if type=user) |
| **Rules** |
| `is_required` | BOOLEAN | Required step |
| `can_skip` | BOOLEAN | Can be skipped |
| `auto_approve_after_days` | INTEGER | Auto-approve timeout |
| `conditions_json` | JSONB | Conditional routing rules |

### approval_requests
Active approval instances linked to business activities.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `template_id` | UUID | FK to templates |
| **Activity** |
| `activity_type` | VARCHAR | Activity type |
| `activity_id` | UUID | FK to activity entity |
| `requestor_id` | UUID | FK to users/employees |
| **Progress** |
| `current_step` | INTEGER | Current step number |
| `status` | VARCHAR | `pending`, `approved`, `rejected`, `cancelled` |
| `completed_at` | TIMESTAMP | Completion time |

### approval_request_steps
Step-level status for each approval request.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `request_id` | UUID | FK to approval_requests |
| `step_order` | INTEGER | Step sequence |
| `step_name` | VARCHAR | Step name |
| `approver_type` | VARCHAR | Approver type |
| `assigned_to_id` | UUID | Assigned user |
| **Action** |
| `status` | VARCHAR | `pending`, `approved`, `rejected`, `skipped` |
| `action_by_id` | UUID | User who took action |
| `action_at` | TIMESTAMP | Action timestamp |
| `comments` | TEXT | Approver comments |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/ApprovalTemplateController.cs`
- `WebApi/Controllers/ApprovalWorkflowController.cs`
- `WebApi/Controllers/Manager/ManagerExpenseApprovalsController.cs`

### Entities
- `Core/Entities/Approval/ApprovalWorkflowTemplate.cs`
- `Core/Entities/Approval/ApprovalWorkflowStep.cs`
- `Core/Entities/Approval/ApprovalRequest.cs`
- `Core/Entities/Approval/ApprovalRequestStep.cs`

### Services
- `Application/Services/ApprovalWorkflowService.cs`

---

## Frontend Structure

### Services
- `services/api/approval/approvalTemplateService.ts`
- `services/api/approval/approvalWorkflowService.ts`

---

## API Endpoints

### Workflow Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/approval-templates` | List templates |
| GET | `/api/approval-templates/{id}` | Get template |
| GET | `/api/approval-templates/activity/{type}` | Templates by activity |
| POST | `/api/approval-templates` | Create template |
| PUT | `/api/approval-templates/{id}` | Update template |
| DELETE | `/api/approval-templates/{id}` | Delete template |
| POST | `/api/approval-templates/{id}/steps` | Add step |
| PUT | `/api/approval-templates/{id}/steps/{stepId}` | Update step |
| DELETE | `/api/approval-templates/{id}/steps/{stepId}` | Delete step |

### Approval Workflow
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/approval-workflow/pending` | My pending approvals |
| GET | `/api/approval-workflow/request/{id}` | Get request details |
| POST | `/api/approval-workflow/initiate` | Start workflow |
| POST | `/api/approval-workflow/{requestId}/approve` | Approve step |
| POST | `/api/approval-workflow/{requestId}/reject` | Reject step |
| POST | `/api/approval-workflow/{requestId}/skip` | Skip step |
| POST | `/api/approval-workflow/{requestId}/cancel` | Cancel request |
| GET | `/api/approval-workflow/activity/{type}/{id}` | Get by activity |

---

## Business Rules

### Approval Request Status Flow
```
pending → [step approved] → [next step] → ... → approved
                         → rejected (at any step)
                         → cancelled (by requestor)
```

### Approver Types
| Type | Description |
|------|-------------|
| `user` | Specific user assigned |
| `role` | Any user with specified role |
| `manager` | Requestor's reporting manager |
| `department_head` | Department head |
| `finance_head` | Finance team lead |
| `cfo` | Chief Financial Officer |

### Conditional Routing
Conditions can be defined in `conditions_json`:
```json
{
  "field": "amount",
  "operator": "gt",
  "value": 50000,
  "then": {
    "approver_type": "cfo"
  }
}
```

**Supported Operators**:
- `eq` - Equal
- `ne` - Not equal
- `gt` - Greater than
- `gte` - Greater than or equal
- `lt` - Less than
- `lte` - Less than or equal
- `in` - In list

### Auto-Approval
- If `auto_approve_after_days` is set
- Step auto-approves after timeout
- Useful for escalation scenarios

### Step Skip Rules
- `can_skip = true` allows manual skip
- Typically for optional reviewers
- Skip requires justification

### Activity Types (Common)
| Type | Description |
|------|-------------|
| `expense` | Expense claims |
| `leave` | Leave applications |
| `purchase` | Purchase requests |
| `vendor_payment` | Vendor payments |
| `invoice` | Invoice approval |
| `contractor_payment` | Contractor payments |
| `asset_request` | Asset requests |
| `payroll` | Payroll runs |

---

## Workflow Examples

### Expense Approval (Amount-Based)
```
Template: Expense Approval
Steps:
1. Manager (required) - All expenses
2. Finance Head (conditional) - If amount > 50,000
3. CFO (conditional) - If amount > 200,000
```

### Leave Approval
```
Template: Leave Approval
Steps:
1. Reporting Manager (required)
2. HR (optional, can_skip=true) - For extended leaves
```

### Vendor Payment
```
Template: Vendor Payment
Steps:
1. Account Payable Team (required)
2. Finance Manager (required) - If amount > 100,000
3. Director (conditional) - If amount > 500,000
```

---

## Integration Pattern

### Initiating Workflow
When a business activity needs approval:

```csharp
// In activity service
public async Task<Result<Activity>> CreateWithApproval(CreateDto dto)
{
    // Create activity in draft status
    var activity = await _repository.CreateAsync(dto);

    // Initiate approval workflow
    await _approvalService.InitiateWorkflow(
        activityType: "expense",
        activityId: activity.Id,
        requestorId: currentUserId
    );

    return activity;
}
```

### Handling Approval Completion
```csharp
// Callback when workflow completes
public async Task OnApprovalComplete(ApprovalRequest request)
{
    if (request.Status == "approved")
    {
        // Update activity status
        await UpdateActivityStatus(request.ActivityId, "approved");
    }
    else if (request.Status == "rejected")
    {
        // Notify requestor
        await NotifyRejection(request);
    }
}
```

---

## Notifications

### Events Triggering Notifications
| Event | Recipient |
|-------|-----------|
| Workflow initiated | First approver |
| Step approved | Next approver |
| Step rejected | Requestor |
| Workflow approved | Requestor |
| Workflow rejected | Requestor |
| Auto-approval | Requestor + Skipped approver |

---

## Current Gaps / TODO

- [ ] Email/push notifications integration
- [ ] Delegation/backup approver
- [ ] Approval delegation during leave
- [ ] Parallel approval steps (any-of / all-of)
- [ ] Approval history report
- [ ] SLA tracking and escalation
- [ ] Mobile-friendly approval actions
- [ ] Bulk approval operations
- [ ] Re-submission after rejection

---

## Related Modules

- [Expenses](15-EXPENSES.md) - Expense claim approval
- [Leave Management](13-LEAVE-MANAGEMENT.md) - Leave approval
- [Accounts Payable](02-ACCOUNTS-PAYABLE.md) - Payment approval
- [Contractor Payments](03-CONTRACTOR-PAYMENTS.md) - Contractor payment approval
- [Assets](12-ASSETS.md) - Asset request approval

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Generic workflow engine operational
- Multi-step approval chains functional
- Conditional routing supported
