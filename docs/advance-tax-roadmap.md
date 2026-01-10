# Advance Tax Engine - Full Implementation Roadmap

## Overview

Advance Tax (Section 207) is a forward-looking tax estimation for corporates. This document outlines the complete CA-grade implementation plan.

---

## Current State (MVP)

- [x] Database schema (assessments, schedules, payments, scenarios)
- [x] Basic CRUD operations
- [x] Quarterly schedule generation (15%, 45%, 75%, 100%)
- [x] Interest calculation (234B, 234C)
- [x] Payment recording with challan details
- [x] What-if scenario analysis
- [x] Frontend dashboard
- [x] **Auto-fetch YTD actuals from ledger** (Phase 1 complete)
- [x] **YTD vs Projection split** (Phase 2 complete)
- [x] **Book Profit → Taxable Income reconciliation** (Phase 3 complete)

---

## Phase 1: Auto-Fetch YTD Actuals (COMPLETED)

### Goal
When creating assessment, auto-populate projected income/expenses from ledger data.

### Implementation (Done)
1. Added `GetYtdFinancialsFromLedgerAsync` to `IAdvanceTaxRepository`
2. Query `journal_entry_lines` joined with `chart_of_accounts`
3. Filter by `company_id` and FY date range (April 1 → Current Date)
4. Sum by account type:
   - `income` accounts → Projected Revenue
   - `expense` accounts → Projected Expenses
5. Modified `AdvanceTaxService.ComputeAssessmentAsync` to auto-fetch when values not provided

### Query Logic
```sql
SELECT
  COALESCE(SUM(CASE WHEN coa.account_type = 'income'
    THEN jel.credit_amount - jel.debit_amount ELSE 0 END), 0) as YtdIncome,
  COALESCE(SUM(CASE WHEN coa.account_type = 'expense'
    THEN jel.debit_amount - jel.credit_amount ELSE 0 END), 0) as YtdExpenses
FROM journal_entry_lines jel
JOIN journal_entries je ON jel.journal_entry_id = je.id
JOIN chart_of_accounts coa ON jel.account_id = coa.id
WHERE je.company_id = @CompanyId
  AND je.journal_date >= @FyStartDate
  AND je.journal_date <= @CurrentDate
  AND je.status = 'posted'
  AND coa.account_type IN ('income', 'expense')
```

### Dependency: Tally Migration Fixes (COMPLETED)

The auto-fetch was returning incorrect values due to Tally migration bugs. Fixed:

| File | Issue | Fix |
|------|-------|-----|
| `TallyVoucherMappingService.cs` | Debit/Credit columns swapped | Tally convention: `Amount < 0` = Debit, `Amount > 0` = Credit |
| `TallyVoucherDtos.cs` | `IsDebit` property wrong | Changed from `Amount > 0` to `Amount < 0` |
| `TallyMasterMappingService.cs` | `NormalBalance` never set | Added `GetNormalBalance()` - income/liability/equity = "credit" |
| `TallyXmlParserService.cs` | Dr/Cr suffix ignored | Now applies sign from " Dr"/" Cr" suffix |

**Tally XML Convention Discovered:**
```xml
<AMOUNT>-7000.00</AMOUNT>  <!-- Negative = DEBIT -->
<AMOUNT>8260.00</AMOUNT>   <!-- Positive = CREDIT -->
```

---

## Phase 2: YTD vs Projection Split (COMPLETED)

### Goal
Show actuals (locked) vs projections (editable) separately.

### Implementation (Done)
1. **Database Migration**: `149_add_ytd_projection_split.sql`
   - Added `ytd_revenue`, `ytd_expenses`, `ytd_through_date`
   - Added `projected_additional_revenue`, `projected_additional_expenses`

2. **Backend Changes**:
   - Updated `AdvanceTaxAssessment` entity with new fields
   - Updated `AdvanceTaxAssessmentDto` and `UpdateAdvanceTaxAssessmentDto`
   - Added `RefreshYtdRequest` and `YtdFinancialsDto`
   - Updated repository INSERT/UPDATE statements
   - Modified `ComputeAssessmentAsync` to auto-calculate trend projections
   - Modified `UpdateAssessmentAsync` to use projected additional fields
   - Added `RefreshYtdAsync` - refreshes YTD from ledger
   - Added `GetYtdFinancialsPreviewAsync` - preview with trend suggestions
   - Added API endpoints: `POST /assessment/refresh-ytd`, `GET /ytd-preview/{companyId}/{fy}`

3. **Frontend Changes**:
   - Updated TypeScript types for new fields
   - Added `refreshYtd()` and `getYtdFinancialsPreview()` to API service
   - Added `useRefreshYtd` and `useYtdFinancialsPreview` hooks
   - Updated UI to show YTD (locked) and Projected (editable) separately
   - Added "Refresh YTD" button in Tax Computation section

### Data Model Changes
```
advance_tax_assessments:
  + ytd_revenue DECIMAL(18,2)           -- Actual Apr-CurrentMonth
  + ytd_expenses DECIMAL(18,2)          -- Actual Apr-CurrentMonth
  + ytd_through_date DATE               -- Last date of actuals
  + projected_additional_revenue DECIMAL(18,2)  -- User estimate for remaining
  + projected_additional_expenses DECIMAL(18,2)
```

### UI Changes
```
┌─────────────────────────────────────────────────────────┐
│  Income                                                 │
│  ├─ YTD Actual (Apr-Dec):     ₹2,02,26,266  [locked]   │
│  ├─ Projected (Jan-Mar):      ₹  50,00,000  [editable] │
│  └─ Full Year Estimate:       ₹2,52,26,266             │
├─────────────────────────────────────────────────────────┤
│  Expenses                                               │
│  ├─ YTD Actual (Apr-Dec):     ₹2,01,46,422  [locked]   │
│  ├─ Projected (Jan-Mar):      ₹  40,00,000  [editable] │
│  └─ Full Year Estimate:       ₹2,41,46,422             │
└─────────────────────────────────────────────────────────┘
```

### Features
- "Refresh YTD" button to re-fetch actuals
- Trend-based auto-projection option (avg monthly × remaining months)
- Previous year comparison tooltip

---

## Phase 3: Book Profit → Taxable Income Reconciliation (COMPLETED)

### Goal
Proper tax computation requires adjustments between book profit and taxable income.

### Implementation (Done)
1. **Database Migration**: `150_add_book_taxable_reconciliation.sql`
   - Added all reconciliation columns to `advance_tax_assessments`

2. **Backend Changes**:
   - Updated `AdvanceTaxAssessment` entity with reconciliation fields
   - Updated `AdvanceTaxAssessmentDto` and `UpdateAdvanceTaxAssessmentDto`
   - Updated repository INSERT/UPDATE statements
   - Modified `ComputeAssessmentAsync` to set `bookProfit = projectedProfitBeforeTax`
   - Modified `UpdateAssessmentAsync` to calculate totals and taxable income

3. **Frontend Changes**:
   - Updated TypeScript types for reconciliation fields
   - Added "Book Profit to Taxable Income Reconciliation" section in UI
   - Shows additions (expenses disallowed) and deductions
   - Formula: `Taxable Income = Book Profit + Total Additions - Total Deductions`

### Data Model Changes
```
advance_tax_assessments:
  -- Additions to book profit
  + book_profit DECIMAL(18,2)
  + add_book_depreciation DECIMAL(18,2)
  + add_disallowed_40a3 DECIMAL(18,2)      -- Cash payments > 10K
  + add_disallowed_40a7 DECIMAL(18,2)      -- Gratuity provision
  + add_disallowed_43b DECIMAL(18,2)       -- Unpaid statutory dues
  + add_other_disallowances DECIMAL(18,2)
  + total_additions DECIMAL(18,2)

  -- Deductions from book profit
  + less_it_depreciation DECIMAL(18,2)     -- As per IT Act rates
  + less_deductions_80c DECIMAL(18,2)
  + less_deductions_80d DECIMAL(18,2)
  + less_other_deductions DECIMAL(18,2)
  + total_deductions DECIMAL(18,2)
```

### UI: Reconciliation Statement
```
┌─────────────────────────────────────────────────────────┐
│  BOOK PROFIT TO TAXABLE INCOME RECONCILIATION          │
├─────────────────────────────────────────────────────────┤
│  Book Profit (as per P&L)              ₹  10,79,844    │
│                                                         │
│  ADD: Expenses disallowed                               │
│    Depreciation as per books           ₹   2,50,000    │
│    Cash payments > ₹10,000 (40A(3))    ₹      15,000   │
│    Provision for gratuity (40A(7))     ₹   1,00,000    │
│    Unpaid statutory dues (43B)         ₹      50,000   │
│                                        ─────────────    │
│    Total Additions                     ₹   4,15,000    │
│                                                         │
│  LESS: Deductions allowed                               │
│    Depreciation as per IT Act          ₹   3,00,000    │
│    Deduction u/s 80C                   ₹   1,50,000    │
│                                        ─────────────    │
│    Total Deductions                    ₹   4,50,000    │
│                                                         │
│  TAXABLE INCOME                        ₹  10,44,844    │
└─────────────────────────────────────────────────────────┘
```

---

## Phase 4: Quarterly Re-estimation Workflow

### Goal
CAs revise estimates each quarter as actuals become clearer.

### Features
1. **Revision History**
   - Track each quarterly revision
   - Show variance from previous estimate
   - Audit trail for compliance

2. **Auto-Prompt for Revision**
   - After Q1 due date (Jun 15), prompt to revise for Q2
   - Show actual vs estimated variance

3. **Revised Schedule Computation**
   - If Q1 was underpaid, catch up in Q2-Q4
   - Recalculate interest implications

### Data Model
```
advance_tax_revisions:
  id UUID PRIMARY KEY
  assessment_id UUID REFERENCES advance_tax_assessments
  revision_quarter INT (1-4)
  revision_date DATE
  previous_taxable_income DECIMAL
  revised_taxable_income DECIMAL
  variance DECIMAL
  reason TEXT
  revised_by UUID
  created_at TIMESTAMPTZ
```

---

## Phase 5: Integration with TDS/TCS Modules

### Goal
Auto-fetch TDS receivable and TCS credit from existing modules.

### Implementation
1. Query `tds_receivables` for company/FY → Sum as TDS credit
2. Query TCS credits if applicable
3. Auto-populate in assessment, allow manual override

---

## Phase 6: MAT (Minimum Alternate Tax) Computation

### Goal
For companies where normal tax < 15% of book profit, MAT applies.

### Logic
```
If (Normal Tax < 15% of Book Profit):
    Tax Payable = 15% of Book Profit + Surcharge + Cess
    MAT Credit = MAT Paid - Normal Tax (carry forward 15 years)
Else:
    Tax Payable = Normal Tax
    Utilize available MAT Credit
```

### Data Model
```
mat_credit_register:
  id UUID PRIMARY KEY
  company_id UUID
  financial_year VARCHAR(10)
  mat_credit_created DECIMAL
  mat_credit_utilized DECIMAL
  mat_credit_balance DECIMAL
  expiry_year VARCHAR(10)  -- Created year + 15
```

---

## Phase 7: Form 280 (Challan) Generation

### Goal
Generate pre-filled Form 280 for advance tax payment.

### Features
- PDF generation with company details, TAN, amount
- Integration with e-payment gateway (future)
- BSR code lookup

---

## Phase 8: Compliance Dashboard

### Goal
Bird's-eye view of advance tax status across all companies.

### Features
1. **Multi-company view** (for CA firms / groups)
2. **Due date calendar** with reminders
3. **Interest liability alerts**
4. **Year-on-year comparison**

---

## Technical Notes

### Tax Rates Reference (AY 2025-26)
| Regime | Base Rate | Surcharge (if income > 1Cr) | Cess |
|--------|-----------|----------------------------|------|
| Normal | 25% | 7% | 4% |
| 115BAA | 22% | 10% | 4% |
| 115BAB | 15% | 10% | 4% |

### Due Dates (Section 211)
| Quarter | Period | Due Date | Cumulative % |
|---------|--------|----------|--------------|
| Q1 | Apr-Jun | 15 June | 15% |
| Q2 | Jul-Sep | 15 September | 45% |
| Q3 | Oct-Dec | 15 December | 75% |
| Q4 | Jan-Mar | 15 March | 100% |

### Interest Rates
- **234B** (Shortfall): 1% per month on (Assessed Tax - Advance Tax Paid) if < 90%
- **234C** (Deferment): 1% per month on quarterly shortfall

---

## Priority Order

1. ~~**Phase 1** - Auto-fetch YTD~~ ✅ DONE
2. ~~**Phase 2** - YTD vs Projection split~~ ✅ DONE
3. ~~**Phase 3** - Book → Taxable reconciliation~~ ✅ DONE
4. **Phase 5** - TDS/TCS integration ← NEXT
5. **Phase 4** - Quarterly re-estimation
6. **Phase 6** - MAT computation
7. **Phase 7** - Form 280 generation
8. **Phase 8** - Compliance dashboard
