# Leave Management Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Leave Management module handles employee leave types, applications, balances, and holiday calendars. Supports configurable leave policies with carry-forward, encashment, and approval workflows.

### Key Features
- Configurable leave types with policies
- Leave application workflow
- Leave balance tracking per FY
- Carry-forward and encashment rules
- Half-day leave support
- Company holiday calendar
- Manager approval workflow
- Employee self-service portal

### Key Entities
- **Leave Types** - Leave type configurations
- **Leave Applications** - Leave requests
- **Employee Leave Balances** - Per-employee balance tracking
- **Holidays** - Company holiday calendar

---

## Database Schema

### leave_types
Leave type master with policy settings.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Leave type name |
| `code` | VARCHAR | Short code (CL, PL, SL) |
| `description` | TEXT | Description |
| **Entitlement** |
| `days_per_year` | NUMERIC | Annual entitlement |
| **Carry Forward** |
| `carry_forward_allowed` | BOOLEAN | Allow carry forward |
| `max_carry_forward_days` | NUMERIC | Max CF days |
| **Encashment** |
| `encashment_allowed` | BOOLEAN | Allow encashment |
| `max_encashment_days` | NUMERIC | Max encash days |
| **Rules** |
| `requires_approval` | BOOLEAN | Needs manager approval |
| `min_days_notice` | INTEGER | Minimum notice days |
| `max_consecutive_days` | INTEGER | Max consecutive days |
| **Display** |
| `color_code` | VARCHAR | Calendar color |
| `sort_order` | INTEGER | Display order |
| `is_active` | BOOLEAN | Active flag |
| **Audit** |
| `created_by` | VARCHAR | Created by |
| `updated_by` | VARCHAR | Updated by |

### leave_applications
Employee leave requests.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `employee_id` | UUID | FK to employees |
| `leave_type_id` | UUID | FK to leave_types |
| `company_id` | UUID | FK to companies |
| **Dates** |
| `from_date` | DATE | Leave start date |
| `to_date` | DATE | Leave end date |
| `total_days` | NUMERIC | Total days (calculated) |
| **Half Day** |
| `is_half_day` | BOOLEAN | Half day flag |
| `half_day_type` | VARCHAR | `first_half`, `second_half` |
| **Request** |
| `reason` | TEXT | Leave reason |
| `status` | VARCHAR | `pending`, `approved`, `rejected`, `cancelled` |
| `applied_at` | TIMESTAMP | Application timestamp |
| **Approval** |
| `approved_by` | UUID | FK to employees (manager) |
| `approved_at` | TIMESTAMP | Approval timestamp |
| `rejection_reason` | TEXT | If rejected |
| **Cancellation** |
| `cancelled_at` | TIMESTAMP | Cancellation time |
| `cancellation_reason` | TEXT | Cancellation reason |
| **Additional** |
| `emergency_contact` | VARCHAR | Emergency contact |
| `handover_notes` | TEXT | Work handover notes |
| `attachment_url` | TEXT | Supporting documents |

### employee_leave_balances
FY-wise leave balance tracking.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `employee_id` | UUID | FK to employees |
| `leave_type_id` | UUID | FK to leave_types |
| `financial_year` | VARCHAR | FY (2024-25) |
| **Balance Components** |
| `opening_balance` | NUMERIC | Opening balance |
| `accrued` | NUMERIC | Accrued during FY |
| `taken` | NUMERIC | Days taken |
| `carry_forwarded` | NUMERIC | CF to next FY |
| `adjusted` | NUMERIC | Manual adjustments |
| `encashed` | NUMERIC | Encashed days |

**Calculated Balance**:
```
Available = opening_balance + accrued + carry_forwarded + adjusted - taken - encashed
```

### holidays
Company holiday calendar.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Holiday name |
| `date` | DATE | Holiday date |
| `year` | INTEGER | Calendar year |
| `is_optional` | BOOLEAN | Optional/Restricted holiday |
| `description` | TEXT | Description |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/LeaveTypesController.cs`
- `WebApi/Controllers/LeaveApplicationsController.cs`
- `WebApi/Controllers/LeaveBalancesController.cs`
- `WebApi/Controllers/HolidaysController.cs`
- `WebApi/Controllers/Portal/LeavePortalController.cs`

### Entities
- `Core/Entities/Leave/LeaveType.cs`
- `Core/Entities/Leave/LeaveApplication.cs`
- `Core/Entities/Leave/EmployeeLeaveBalance.cs`
- `Core/Entities/Leave/Holiday.cs`

---

## Frontend Structure

### Pages
- `pages/hr/leave/LeaveTypesManagement.tsx` - Leave type config
- `pages/hr/leave/LeaveApplicationsManagement.tsx` - Leave requests
- `pages/hr/leave/LeaveBalancesManagement.tsx` - Balance view
- `pages/hr/leave/HolidaysManagement.tsx` - Holiday calendar

### Services
- `services/api/hr/leave/leaveTypeService.ts`
- `services/api/hr/leave/leaveApplicationService.ts`
- `services/api/hr/leave/leaveBalanceService.ts`
- `services/api/hr/leave/holidayService.ts`

---

## API Endpoints

### Leave Types
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-types` | List leave types |
| GET | `/api/leave-types/{id}` | Get leave type |
| POST | `/api/leave-types` | Create leave type |
| PUT | `/api/leave-types/{id}` | Update leave type |
| DELETE | `/api/leave-types/{id}` | Delete leave type |

### Leave Applications
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-applications` | List applications |
| GET | `/api/leave-applications/paged` | Paginated list |
| GET | `/api/leave-applications/{id}` | Get application |
| POST | `/api/leave-applications` | Apply for leave |
| PUT | `/api/leave-applications/{id}` | Update application |
| POST | `/api/leave-applications/{id}/approve` | Approve |
| POST | `/api/leave-applications/{id}/reject` | Reject |
| POST | `/api/leave-applications/{id}/cancel` | Cancel |
| GET | `/api/leave-applications/pending-approval` | Pending for manager |
| GET | `/api/leave-applications/team/{managerId}` | Team applications |

### Leave Balances
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/leave-balances` | List balances |
| GET | `/api/leave-balances/employee/{employeeId}` | Employee balances |
| GET | `/api/leave-balances/employee/{employeeId}/{fy}` | Balance for FY |
| POST | `/api/leave-balances/adjust` | Manual adjustment |
| POST | `/api/leave-balances/year-end-process` | FY closing |

### Holidays
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/holidays` | List holidays |
| GET | `/api/holidays/year/{year}` | Holidays for year |
| POST | `/api/holidays` | Add holiday |
| PUT | `/api/holidays/{id}` | Update holiday |
| DELETE | `/api/holidays/{id}` | Delete holiday |
| POST | `/api/holidays/bulk` | Bulk add holidays |

### Employee Portal
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/portal/leaves/my-balances` | My leave balances |
| GET | `/api/portal/leaves/my-applications` | My applications |
| POST | `/api/portal/leaves/apply` | Apply for leave |
| POST | `/api/portal/leaves/{id}/cancel` | Cancel my leave |

---

## Business Rules

### Leave Application Status Flow
```
pending → approved → [cancelled]
       → rejected
[pending] → cancelled (by employee)
```

### Working Days Calculation
```
Total Days = To Date - From Date + 1
           - Weekends (Sat/Sun)
           - Holidays falling in range
           + 0.5 (if half day)
```

### Common Leave Types
| Code | Name | Days/Year | Carry Forward | Encash |
|------|------|-----------|---------------|--------|
| CL | Casual Leave | 12 | No | No |
| PL/EL | Privilege/Earned Leave | 15-21 | Yes (Max 30) | Yes |
| SL | Sick Leave | 12 | Sometimes | No |
| ML | Maternity Leave | 182 | No | No |
| PL_M | Paternity Leave | 15 | No | No |
| CO | Compensatory Off | - | No | No |
| LWP | Leave Without Pay | - | No | No |

### Leave Accrual
- Monthly accrual: `days_per_year / 12`
- Pro-rata for mid-year joiners
- Accrual credited at month start/end (configurable)

### Carry Forward Rules
- Processed at FY end (typically March 31)
- Excess over `max_carry_forward_days` lapses
- Some companies allow encashment of lapsed balance

### Leave Encashment
- Typically at FY end or separation
- Encashment rate = Basic salary / 30
- May be taxable under Income Tax

### Approval Workflow
1. Employee applies for leave
2. Manager receives notification
3. Manager approves/rejects
4. Balance auto-deducted on approval
5. Calendar updated

### Validation Rules
- Cannot apply for past dates (usually)
- Minimum notice period check
- Maximum consecutive days check
- Balance availability check
- Overlapping leave check
- Sandwich rule (weekends between leaves)

---

## Integration Points

### With Payroll Module
- Leave days affect salary calculation
- LWP deduction from salary
- Leave encashment in payroll

### With Employee Master
- Employee hire date for pro-rata
- Reporting manager for approvals
- Department for team views

### With Approvals Module
- Can integrate with generic approval workflow
- Multi-level approval support

---

## Reports

### Leave Register
- Month/year-wise leave records
- Employee-wise summary
- Department-wise summary

### Leave Balance Report
- All employees' current balances
- Type-wise breakdown
- CF/encashment eligible

### Absenteeism Report
- Absence patterns
- Department comparison
- Leave trends

### Pending Approvals
- Manager's pending queue
- Aging of pending requests

---

## Current Gaps / TODO

- [ ] Automatic leave accrual (monthly credit)
- [ ] FY-end carry forward processing
- [ ] Leave encashment workflow
- [ ] Sandwich rule implementation
- [ ] Leave calendar view (team)
- [ ] Integration with attendance system
- [ ] Email notifications for approval
- [ ] Compensatory off workflow
- [ ] Bulk leave credit (mid-year joiners)

---

## Related Modules

- [Payroll](06-PAYROLL.md) - LWP deduction, encashment
- [Administration](16-ADMINISTRATION.md) - Employee master
- [Approvals](14-APPROVALS.md) - Multi-level approval

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Core leave workflow operational
- Balance tracking functional
- Holiday calendar implemented
