# Active Work Tracker

## Purpose
This file tracks current session focus areas and in-progress work. Read this at the start of each session for context continuity.

---

## Current Focus Area
**Branch**: `ap_ar` (Accounts Payable / Accounts Receivable)

### Active Features
1. **Bank Transactions** - Enhanced reconciliation, source voucher tracking
2. **Contractor Payments** - TDS calculation, GST, bank matching
3. **Vendor Payments** - Payment processing and allocation

---

## Recent Git Activity

### Modified Files (Uncommitted)
```
M apps/admin-portal/src/components/ui/DataTable.tsx
M apps/admin-portal/src/features/payroll/types/payroll.ts
M apps/admin-portal/src/pages/finance/banking/BankTransactionsPage.tsx
M apps/admin-portal/src/pages/hr/payroll/ContractorPaymentsPage.tsx
M backend/src/Application/DTOs/Payroll/ContractorPaymentDto.cs
M backend/src/Core/Entities/Payroll/ContractorPayment.cs
M backend/src/Infrastructure/Data/BankAccountRepository.cs
M backend/src/Infrastructure/Data/Payroll/ContractorPaymentRepository.cs
```

### Recent Commits
- `ae4c4eb` - bank transactions
- `f734e75` - reconcile
- `43b250a` - icon pallette
- `57cabc2` - fixes
- `2834298` - fixes

---

## Implementation Plans in Progress

### Completed
- [x] Plan 1: Contractor Payments Import - Tally import classification
- [x] Plan 2: Statutory Payments Import - EPF/ESI/TDS/PT import
- [x] Plan 3: Bank Transactions Import - Bank reconciliation setup

### In Progress
- [ ] Plan 4: Contractor to Party Migration - `employee_id` → `party_id`

### Implementation Files
- `docs/PLAN_1_CONTRACTOR_PAYMENTS_IMPORT.md`
- `docs/PLAN_2_STATUTORY_PAYMENTS_IMPORT.md`
- `docs/PLAN_3_BANK_TRANSACTIONS_IMPORT.md`
- `docs/PLAN_4_CONTRACTOR_TO_PARTY_MIGRATION.md`

---

## Next Steps

### Immediate (This Session)
1. Complete any uncommitted work in `ap_ar` branch
2. Review Plan 4 (Contractor to Party Migration) status
3. Test bank transaction reconciliation workflow

### Short Term
1. Complete party migration for contractor payments
2. Enhance bank transaction matching algorithm
3. Add vendor transaction view on party details

---

## Blocked Items
None currently.

---

## Session Notes

### 2026-01-09 (Session 3)
- Enhanced ARCHITECTURE.md with comprehensive best practices
- Added: SOLID principles table (Backend .NET + Frontend React)
- Added: Separation of Concerns (SoC) diagram and rules
- Added: Complete technology stack tables (Frontend + Backend)
- Added: TanStack Query best practices (useEffect alternatives)
- Added: nuqs URL state management patterns
- Added: TanStack Table integration patterns
- Added: React Hook Form + Zod validation patterns
- Added: Anti-Patterns to Avoid section (Frontend + Backend)
- Added: Code Smell Checklist for pre-commit verification

### 2026-01-09 (Session 2)
- Completed all 16 module documentation files
- Created: 02-ACCOUNTS-PAYABLE.md, 03-CONTRACTOR-PAYMENTS.md, 04-BANKING.md
- Created: 06-PAYROLL.md, 07-INVENTORY.md, 08-MANUFACTURING.md
- Created: 10-TDS-TCS.md, 11-EXPORTS-FOREX.md, 12-ASSETS.md
- Created: 13-LEAVE-MANAGEMENT.md, 14-APPROVALS.md, 15-EXPENSES.md, 16-ADMINISTRATION.md
- Full documentation structure now complete

### 2026-01-09 (Session 1)
- Created comprehensive documentation structure
- Module docs: ARCHITECTURE.md, 01-BILLING.md, 05-LEDGER.md, 09-GST-COMPLIANCE.md
- Documentation plan approved - core modules first approach

---

## Quick Reference

### Key Directories
- Backend: `backend/src/`
- Frontend: `apps/admin-portal/src/`
- Docs: `docs/`
- Migrations: `backend/migrations/`

### Run Commands
```bash
# Backend
cd backend && dotnet run --project src/WebApi

# Frontend
cd apps/admin-portal && npm run dev

# Database migrations
cd backend && ./scripts/run-migrations.sh
```

### Key Pages
- Contractor Payments: `/hr/payroll/contractor-payments`
- Bank Transactions: `/finance/banking/transactions`
- Vendor Payments: `/finance/ap/vendor-payments`
