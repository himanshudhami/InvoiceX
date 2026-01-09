# Expenses Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Expenses module handles employee expense claims, reimbursements, and GST ITC tracking on expenses. Supports configurable expense categories, receipt attachments, approval workflows, and bank reconciliation.

### Key Features
- Expense category configuration with limits
- Employee expense claim submission
- Receipt/invoice attachment
- GST details capture for ITC
- Multi-level approval workflow
- Reimbursement tracking
- Bank transaction reconciliation
- GL account mapping

### Key Entities
- **Expense Categories** - Category master with policies
- **Expense Claims** - Employee expense submissions
- **Expense Attachments** - Receipts and documents

---

## Database Schema

### expense_categories
Expense category master with policies.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `name` | VARCHAR | Category name |
| `code` | VARCHAR | Category code |
| `description` | TEXT | Description |
| `is_active` | BOOLEAN | Active flag |
| **Limits** |
| `max_amount` | NUMERIC | Per-claim limit |
| **Rules** |
| `requires_receipt` | BOOLEAN | Receipt mandatory |
| `requires_approval` | BOOLEAN | Approval required |
| **GST** |
| `is_gst_applicable` | BOOLEAN | GST capture |
| `default_gst_rate` | NUMERIC | Default GST rate |
| `default_hsn_sac` | VARCHAR | Default HSN/SAC |
| `itc_eligible` | BOOLEAN | ITC can be claimed |
| **Ledger** |
| `gl_account_code` | VARCHAR | Expense GL account |
| `display_order` | INTEGER | Display order |

### expense_claims
Employee expense claims with GST details.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `employee_id` | UUID | FK to employees |
| `claim_number` | VARCHAR | Claim reference |
| `title` | VARCHAR | Expense title |
| `description` | TEXT | Description |
| `category_id` | UUID | FK to expense_categories |
| `expense_date` | DATE | Date of expense |
| `amount` | NUMERIC | Total amount claimed |
| `currency` | VARCHAR | Currency |
| **Vendor/Invoice** |
| `vendor_name` | VARCHAR | Vendor name |
| `vendor_gstin` | VARCHAR | Vendor GSTIN |
| `invoice_number` | VARCHAR | Invoice number |
| `invoice_date` | DATE | Invoice date |
| **GST Details** |
| `is_gst_applicable` | BOOLEAN | GST invoice |
| `supply_type` | VARCHAR | `intra_state`, `inter_state` |
| `hsn_sac_code` | VARCHAR | HSN/SAC |
| `gst_rate` | NUMERIC | GST rate |
| `base_amount` | NUMERIC | Pre-tax amount |
| `cgst_rate` | NUMERIC | CGST rate |
| `cgst_amount` | NUMERIC | CGST amount |
| `sgst_rate` | NUMERIC | SGST rate |
| `sgst_amount` | NUMERIC | SGST amount |
| `igst_rate` | NUMERIC | IGST rate |
| `igst_amount` | NUMERIC | IGST amount |
| `cess_rate` | NUMERIC | Cess rate |
| `cess_amount` | NUMERIC | Cess amount |
| `total_gst_amount` | NUMERIC | Total GST |
| **ITC** |
| `itc_eligible` | BOOLEAN | ITC eligible |
| `itc_claimed` | BOOLEAN | ITC claimed |
| `itc_claimed_in_return` | VARCHAR | GSTR return period |
| **Status** |
| `status` | VARCHAR | `draft`, `submitted`, `approved`, `rejected`, `reimbursed` |
| `approval_request_id` | UUID | FK to approval_requests |
| **Submission** |
| `submitted_at` | TIMESTAMP | Submission time |
| **Approval** |
| `approved_at` | TIMESTAMP | Approval time |
| `approved_by` | UUID | Approver |
| `rejected_at` | TIMESTAMP | Rejection time |
| `rejected_by` | UUID | Rejector |
| `rejection_reason` | TEXT | Rejection reason |
| **Reimbursement** |
| `reimbursed_at` | TIMESTAMP | Reimbursement time |
| `reimbursement_reference` | VARCHAR | Payment reference |
| `reimbursement_notes` | TEXT | Notes |
| **Bank Reconciliation** |
| `bank_transaction_id` | UUID | FK to bank_transactions |
| `reconciled_at` | TIMESTAMP | Reconciliation time |
| `reconciled_by` | VARCHAR | Reconciled by |

### expense_attachments
Receipt and document attachments.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `expense_id` | UUID | FK to expense_claims |
| `file_storage_id` | UUID | FK to file_storage |
| `description` | TEXT | Attachment description |
| `is_primary` | BOOLEAN | Primary receipt |
| `attachment_type` | VARCHAR | `receipt`, `invoice`, `supporting` |
| `uploaded_by` | UUID | Uploader |

---

## Backend Structure

### Controllers
- `WebApi/Controllers/ExpenseClaimsController.cs`
- `WebApi/Controllers/ExpenseCategoriesController.cs`
- `WebApi/Controllers/Portal/PortalExpensesController.cs`
- `WebApi/Controllers/Manager/ManagerExpenseApprovalsController.cs`

### Entities
- `Core/Entities/Expense/ExpenseCategory.cs`
- `Core/Entities/Expense/ExpenseClaim.cs`
- `Core/Entities/Expense/ExpenseAttachment.cs`

---

## Frontend Structure

### Pages
- `pages/finance/expenses/ExpenseDashboard.tsx` - Overview
- `pages/finance/expenses/ExpenseClaimsManagement.tsx` - Claims list
- `pages/finance/expenses/ExpenseCategoriesManagement.tsx` - Categories
- `pages/finance/expenses/OutgoingPaymentsReconciliation.tsx` - Reconciliation

### Services
- `services/api/finance/expense/expenseClaimService.ts`
- `services/api/finance/expense/expenseCategoryService.ts`

---

## API Endpoints

### Expense Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/expense-categories` | List categories |
| GET | `/api/expense-categories/{id}` | Get category |
| POST | `/api/expense-categories` | Create category |
| PUT | `/api/expense-categories/{id}` | Update category |
| DELETE | `/api/expense-categories/{id}` | Delete category |

### Expense Claims
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/expense-claims` | List claims |
| GET | `/api/expense-claims/paged` | Paginated list |
| GET | `/api/expense-claims/{id}` | Get claim |
| POST | `/api/expense-claims` | Create claim |
| PUT | `/api/expense-claims/{id}` | Update claim |
| DELETE | `/api/expense-claims/{id}` | Delete claim |
| POST | `/api/expense-claims/{id}/submit` | Submit for approval |
| POST | `/api/expense-claims/{id}/approve` | Approve claim |
| POST | `/api/expense-claims/{id}/reject` | Reject claim |
| POST | `/api/expense-claims/{id}/reimburse` | Mark reimbursed |
| POST | `/api/expense-claims/{id}/attachments` | Add attachment |

### Manager Portal
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/manager/expense-approvals` | Pending approvals |
| POST | `/api/manager/expense-approvals/{id}/approve` | Approve |
| POST | `/api/manager/expense-approvals/{id}/reject` | Reject |

### Employee Portal
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/portal/expenses/my-claims` | My claims |
| POST | `/api/portal/expenses` | Submit claim |
| GET | `/api/portal/expenses/{id}` | Get my claim |

---

## Business Rules

### Expense Claim Status Flow
```
draft → submitted → approved → reimbursed
                  → rejected → [re-submit as draft]
```

### Common Expense Categories
| Category | GST | ITC | Receipt | Max Amount |
|----------|-----|-----|---------|------------|
| Travel - Flight | Yes | Yes | Yes | - |
| Travel - Hotel | Yes | Yes | Yes | ₹7,500/day |
| Travel - Local Conveyance | No | No | No | ₹2,000/day |
| Meals - Client | Yes | No | Yes | ₹3,000 |
| Meals - Employee | Yes | No | No | ₹500/day |
| Communication | Yes | Yes | No | - |
| Office Supplies | Yes | Yes | Yes | ₹5,000 |
| Professional Fees | Yes | Yes | Yes | - |
| Subscriptions | Yes | Yes | Yes | - |

### GST ITC Eligibility (Section 17(5) Blocked)
| Category | ITC Status |
|----------|------------|
| Employee travel (flight/train) | Eligible |
| Hotel accommodation | Eligible |
| Food & beverages | Blocked |
| Personal motor vehicle | Blocked |
| Club membership | Blocked |
| Health/life insurance | Blocked |
| Office equipment/supplies | Eligible |

### Validation Rules
- Receipt required based on category setting
- Amount cannot exceed category `max_amount`
- GSTIN format validation if provided
- Expense date cannot be future
- Currency conversion if foreign expense

### Approval Threshold (Example)
| Amount | Approver |
|--------|----------|
| ≤ ₹5,000 | Reporting Manager |
| ₹5,000 - ₹25,000 | Department Head |
| > ₹25,000 | Finance Head |

---

## Ledger Integration

### Expense Posting (on Approval)
```
DR: Expense Account (category-wise)    Rs. Base Amount
DR: GST Input Credit (if ITC eligible) Rs. GST Amount
    CR: Employee Reimbursement Payable Rs. Total Amount
```

### Reimbursement Posting
```
DR: Employee Reimbursement Payable  Rs. Amount
    CR: Bank Account                Rs. Amount
```

---

## GST Treatment

### Capturing GST Details
For ITC-eligible expenses:
1. Capture vendor GSTIN
2. Enter invoice number and date
3. Select supply type (intra/inter state)
4. System calculates CGST/SGST or IGST split

### ITC Claiming
- ITC eligible expenses flow to GSTR-3B Table 4
- Match with GSTR-2B for verification
- 180-day payment rule applies

---

## Reports

### Expense Summary
- Employee-wise expense totals
- Category-wise breakdown
- Department-wise comparison
- Period-wise trends

### Pending Reimbursements
- Approved but not reimbursed
- Aging analysis
- Employee-wise outstanding

### ITC from Expenses
- GST-registered vendor expenses
- ITC eligible vs claimed
- GSTR-2B matching status

### Policy Compliance
- Over-limit expenses
- Missing receipts
- Category usage patterns

---

## Current Gaps / TODO

- [ ] OCR receipt scanning
- [ ] Corporate credit card integration
- [ ] Mileage calculator for travel
- [ ] Per diem automation
- [ ] Expense policy enforcement engine
- [ ] Duplicate expense detection
- [ ] Currency conversion automation
- [ ] Mobile app for quick submission
- [ ] Batch reimbursement processing

---

## Related Modules

- [Approvals](14-APPROVALS.md) - Approval workflow
- [Banking](04-BANKING.md) - Reimbursement reconciliation
- [GST Compliance](09-GST-COMPLIANCE.md) - ITC tracking
- [Ledger](05-LEDGER.md) - Expense posting
- [Payroll](06-PAYROLL.md) - Reimbursement through salary

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Core expense workflow operational
- GST/ITC tracking implemented
- Bank reconciliation integrated
