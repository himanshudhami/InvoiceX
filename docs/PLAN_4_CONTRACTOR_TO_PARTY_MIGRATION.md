# Plan 4: Contractor Payments - Employee to Party Migration

## Overview

Migrate `contractor_payments` from linking to `employees` table to linking directly to `parties` table. This eliminates duplicate records and provides a unified vendor transaction view.

## Current State (Problem)

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│    parties      │     │   employees     │     │ contractor_     │
│ (CONSULTANTS)   │     │ (contractors)   │     │   payments      │
│                 │     │                 │     │                 │
│ id: abc-123     │     │ id: xyz-789     │     │ employee_id:    │
│ name: "Haleema" │ ╳   │ name: "Haleema" │◄────│   xyz-789       │
│ TDS: 194J @ 10% │     │ pan: NULL       │     │                 │
│ tally_guid: X   │     │ bank: NULL      │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘
     NO LINK              Empty shell
```

**Issues:**
- Duplicate records for same person (parties + employees)
- No FK relationship between party and employee
- Vendor page cannot show contractor payments
- Employee records are empty shells (no PAN, no bank)

## Target State (Solution)

```
┌─────────────────┐     ┌─────────────────┐
│    parties      │     │ contractor_     │
│ (CONSULTANTS)   │     │   payments      │
│                 │     │                 │
│ id: abc-123     │◄────│ party_id:       │
│ name: "Haleema" │     │   abc-123       │
│ TDS: 194J @ 10% │     │                 │
│ pan: XXXXX1234X │     │                 │
└─────────────────┘     └─────────────────┘

employees table = ONLY actual salaried staff (empty for now)
```

---

## Phase 1: Database Migration

### Migration File: `139_contractor_payments_party_id.sql`

```sql
-- Add party_id column to contractor_payments
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS party_id UUID REFERENCES parties(id);

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_contractor_payments_party_id
ON contractor_payments(party_id) WHERE party_id IS NOT NULL;

-- Drop employee_id FK constraint (keep column for now)
ALTER TABLE contractor_payments
DROP CONSTRAINT IF EXISTS contractor_payments_employee_id_fkey;

-- Make employee_id nullable (will be removed in Phase 2)
ALTER TABLE contractor_payments
ALTER COLUMN employee_id DROP NOT NULL;
```

### Migration File: `140_cleanup_contractor_employees.sql` (Phase 2)

```sql
-- After code migration is complete, remove employee_id
ALTER TABLE contractor_payments DROP COLUMN IF EXISTS employee_id;

-- Delete contractor records from employees
DELETE FROM employees WHERE employment_type = 'contractor';
```

---

## Phase 2: Backend Changes

### 2.1 Entity: `ContractorPayment.cs`

**Changes:**
- Replace `EmployeeId` with `PartyId`
- Replace navigation property `Employee` with `Party`

```csharp
// BEFORE
public Guid EmployeeId { get; set; }
public Employees? Employee { get; set; }

// AFTER
public Guid PartyId { get; set; }
public Party? Party { get; set; }
```

### 2.2 Repository: `IContractorPaymentRepository.cs`

**Changes:**
- Replace all `employeeId` parameters with `partyId`
- Update method signatures

```csharp
// BEFORE
Task<IEnumerable<ContractorPayment>> GetByEmployeeIdAsync(Guid employeeId);
Task<ContractorPayment?> GetByEmployeeAndMonthAsync(Guid employeeId, int month, int year);

// AFTER
Task<IEnumerable<ContractorPayment>> GetByPartyIdAsync(Guid partyId);
Task<ContractorPayment?> GetByPartyAndMonthAsync(Guid partyId, int month, int year);
```

### 2.3 Repository Implementation: `ContractorPaymentRepository.cs`

**Changes:**
- Update all SQL queries to use `party_id`
- Join with `parties` instead of `employees` for name resolution
- Update paged query to include party name

```csharp
// Add party name to SELECT for display
var sql = @"
    SELECT cp.*, p.name as PartyName
    FROM contractor_payments cp
    LEFT JOIN parties p ON p.id = cp.party_id
    WHERE ...";
```

### 2.4 DTOs: `ContractorPaymentDto.cs`

**Changes:**
- Replace `EmployeeId`/`EmployeeName` with `PartyId`/`PartyName`

```csharp
public Guid PartyId { get; set; }
public string? PartyName { get; set; }  // For display
```

### 2.5 Tally Mapper: `TallyContractorPaymentMapper.cs`

**Changes:**
- Remove employee creation logic
- Use party directly from classification result

```csharp
// BEFORE: Creates employee record
var contractorId = await ResolveOrCreateContractorAsync(...);
EmployeeId = contractorId,

// AFTER: Uses party directly
PartyId = classification.PartyId!.Value,
```

### 2.6 Controller: `ContractorPaymentsController.cs`

**Changes:**
- Update parameter names and documentation
- Ensure party-based filtering

---

## Phase 3: Frontend Changes

### 3.1 Types: `payroll.ts`

**Changes:**
```typescript
// BEFORE
interface ContractorPayment {
  employeeId: string;
  employeeName?: string;
}

// AFTER
interface ContractorPayment {
  partyId: string;
  partyName?: string;
}
```

### 3.2 Hooks: `useContractorPayments.ts`

**Changes:**
- Update parameter names from `employeeId` to `partyId`

### 3.3 Page: `ContractorPaymentsPage.tsx`

**Changes:**
- Update column accessors from `employeeName` to `partyName`
- Update filter parameters

### 3.4 Form: `ContractorPaymentForm.tsx`

**Changes:**
- Use party selector instead of employee selector
- Filter parties by `is_vendor = true` and `tally_group_name = 'CONSULTANTS'`

### 3.5 Service: `payrollService.ts`

**Changes:**
- Update API parameter names

---

## Phase 4: Vendor Transaction View (Bonus)

After migration, add unified transaction view to vendor page.

### 4.1 Backend: Add `GetVendorTransactionsAsync` to `IVendorsService`

```csharp
Task<VendorTransactionSummary> GetVendorTransactionsAsync(Guid partyId);

public class VendorTransactionSummary
{
    public List<VendorTransaction> Transactions { get; set; }
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class VendorTransaction
{
    public DateTime Date { get; set; }
    public string Type { get; set; }  // "invoice", "vendor_payment", "contractor_payment"
    public string Reference { get; set; }
    public decimal? Debit { get; set; }
    public decimal? Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
```

### 4.2 Frontend: Vendor Ledger Tab

Add "Transactions" tab to vendor detail view showing unified ledger.

---

## Files to Modify

### Database
- [ ] `migrations/139_contractor_payments_party_id.sql` (new)
- [ ] `migrations/140_cleanup_contractor_employees.sql` (new, Phase 2)

### Backend - Core
- [ ] `Core/Entities/Payroll/ContractorPayment.cs`
- [ ] `Core/Interfaces/Payroll/IContractorPaymentRepository.cs`

### Backend - Infrastructure
- [ ] `Infrastructure/Data/Payroll/ContractorPaymentRepository.cs`

### Backend - Application
- [ ] `Application/DTOs/Payroll/ContractorPaymentDto.cs`
- [ ] `Application/Services/Migration/TallyContractorPaymentMapper.cs`
- [ ] `Application/Interfaces/Migration/ITallyContractorPaymentMapper.cs`

### Backend - WebApi
- [ ] `WebApi/Controllers/Payroll/ContractorPaymentsController.cs`

### Frontend
- [ ] `features/payroll/types/payroll.ts`
- [ ] `features/payroll/hooks/useContractorPayments.ts`
- [ ] `pages/hr/payroll/ContractorPaymentsPage.tsx`
- [ ] `components/forms/ContractorPaymentForm.tsx`
- [ ] `services/api/hr/payroll/payrollService.ts`

---

## Implementation Order

1. **Database Migration** - Add party_id column
2. **Backend Entity** - Update ContractorPayment.cs
3. **Backend Repository Interface** - Update IContractorPaymentRepository
4. **Backend Repository Implementation** - Update SQL queries
5. **Backend DTOs** - Update ContractorPaymentDto
6. **Backend Tally Mapper** - Remove employee creation
7. **Frontend Types** - Update TypeScript interfaces
8. **Frontend Hooks** - Update parameter names
9. **Frontend Page** - Update column accessors
10. **Frontend Form** - Use party selector
11. **Test Import** - Verify Tally import works
12. **Cleanup Migration** - Remove employee_id column

---

## Testing Checklist

- [ ] Fresh DB import from Tally creates contractor_payments with party_id
- [ ] No new records created in employees table for contractors
- [ ] Contractor payments page shows party name correctly
- [ ] Create/Edit contractor payment works with party selector
- [ ] Vendor page shows contractor payments in transaction history
- [ ] TDS calculations work correctly (from party's vendor profile)
