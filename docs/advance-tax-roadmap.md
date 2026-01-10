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
- [x] **TDS/TCS Integration** (Phase 5 complete)
- [x] **Quarterly Re-estimation Workflow** (Phase 4 complete)
- [x] **MAT (Minimum Alternate Tax) Computation** (Phase 6 complete)
- [x] **Form 280 (Challan) Generation** (Phase 7 complete)
- [x] **Compliance Dashboard** (Phase 8 complete)

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

## Phase 4: Quarterly Re-estimation Workflow (COMPLETED)

### Goal
CAs revise estimates each quarter as actuals become clearer.

### Implementation (Done)
1. **Database Migration**: `151_add_advance_tax_revisions.sql`
   - Created `advance_tax_revisions` table with variance tracking
   - Added `revision_count`, `last_revision_date`, `last_revision_quarter` to `advance_tax_assessments`

2. **Backend Changes**:
   - Created `AdvanceTaxRevision` entity with full variance tracking
   - Added revision methods to repository interface and implementation
   - Added `AdvanceTaxRevisionDto`, `CreateRevisionDto`, `RevisionStatusDto`
   - Added service methods: `CreateRevisionAsync`, `GetRevisionsAsync`, `GetRevisionStatusAsync`
   - Added API endpoints: `POST /revision`, `GET /revisions/{id}`, `GET /revision-status/{id}`

3. **Frontend Changes**:
   - Added TypeScript types for revisions
   - Added API service methods and React Query hooks
   - Added Revision Status Alert (when revision is recommended)
   - Added Revision History section with DataTable
   - Added Create Revision modal with full form

### Features
1. **Revision History**
   - Track each quarterly revision with before/after values
   - Show variance (revenue, expenses, taxable income, tax liability)
   - Full audit trail for compliance

2. **Auto-Prompt for Revision**
   - `RevisionStatus` API checks if revision is recommended
   - Recommends revision when variance > 10% or past quarter due date
   - Shows alert with "Create Revision" button

3. **Revised Schedule Computation**
   - Creating a revision updates the assessment with new projections
   - Schedules are automatically recalculated

### Data Model
```
advance_tax_revisions:
  id UUID PRIMARY KEY
  assessment_id UUID REFERENCES advance_tax_assessments
  revision_number INT
  revision_quarter INT (1-4)
  revision_date DATE
  -- Before values
  previous_projected_revenue DECIMAL
  previous_projected_expenses DECIMAL
  previous_taxable_income DECIMAL
  previous_total_tax_liability DECIMAL
  previous_net_tax_payable DECIMAL
  -- After values
  revised_projected_revenue DECIMAL
  revised_projected_expenses DECIMAL
  revised_taxable_income DECIMAL
  revised_total_tax_liability DECIMAL
  revised_net_tax_payable DECIMAL
  -- Variance (computed)
  revenue_variance DECIMAL
  expense_variance DECIMAL
  taxable_income_variance DECIMAL
  tax_liability_variance DECIMAL
  net_payable_variance DECIMAL
  -- Metadata
  revision_reason TEXT
  notes TEXT
  revised_by UUID
  created_at TIMESTAMPTZ

advance_tax_assessments:
  + revision_count INT
  + last_revision_date DATE
  + last_revision_quarter INT
```

### UI: Revision History
```
┌─────────────────────────────────────────────────────────────────┐
│  REVISION HISTORY                                               │
│  Quarterly re-estimation audit trail • 3 revisions recorded    │
├─────────────────────────────────────────────────────────────────┤
│  Date          │ Tax Variance    │ Revised Net │ Reason         │
│  15 Sep 2024   │ +₹50,000 ↑      │ ₹2,00,000   │ Q2 actuals...  │
│  15 Dec 2024   │ -₹30,000 ↓      │ ₹1,70,000   │ Tax saving...  │
│  15 Mar 2025   │ +₹20,000 ↑      │ ₹1,90,000   │ Final review   │
└─────────────────────────────────────────────────────────────────┘
```

---

## Phase 5: Integration with TDS/TCS Modules (COMPLETED)

### Goal
Auto-fetch TDS receivable and TCS credit from existing modules.

### Implementation (Done)
1. **Repository Layer**:
   - Added `GetTdsReceivableAsync(companyId, financialYear)` - queries `tds_receivable` table
   - Added `GetTcsCreditAsync(companyId, financialYear)` - queries `tcs_transactions` where `transaction_type = 'paid'`

2. **Service Layer**:
   - `ComputeAssessmentAsync` now auto-fetches TDS/TCS when not provided in request
   - Added `RefreshTdsTcsAsync` - refresh TDS/TCS from modules for existing assessment
   - Added `GetTdsTcsPreviewAsync` - preview values before refresh

3. **API Endpoints**:
   - `POST /api/tax/advance-tax/assessment/{id}/refresh-tds-tcs` - refresh TDS/TCS
   - `GET /api/tax/advance-tax/tds-tcs-preview/{companyId}/{financialYear}` - preview

4. **Frontend**:
   - Added `TdsTcsPreview` type
   - Added `useRefreshTdsTcs` and `useTdsTcsPreview` hooks
   - Added "Refresh TDS/TCS from modules" button in Tax Calculation section

### Behavior
- When creating assessment: Auto-fetches TDS/TCS if not manually provided
- After creation: User can click "Refresh TDS/TCS from modules" to sync latest values
- Manual override: User can still enter custom values in the update form

---

## Phase 6: MAT (Minimum Alternate Tax) Computation (COMPLETED)

### Goal
For companies where normal tax < 15% of book profit, MAT applies (Section 115JB).

### Implementation (Done)
1. **Database Migration**: `152_add_mat_credit_register.sql`
   - Created `mat_credit_register` table for tracking MAT credit entries
   - Created `mat_credit_utilizations` table for tracking credit usage history
   - Added MAT-related columns to `advance_tax_assessments`

2. **Backend Changes**:
   - Created `MatCreditRegister` and `MatCreditUtilization` entities
   - Updated `AdvanceTaxAssessment` entity with MAT fields
   - Added MAT repository methods: `GetMatCreditByIdAsync`, `GetAvailableMatCreditsAsync`, `CreateMatCreditAsync`, etc.
   - Added DTOs: `MatCreditRegisterDto`, `MatCreditUtilizationDto`, `MatComputationDto`, `MatCreditSummaryDto`
   - Added service methods: `GetMatComputationAsync`, `GetMatCreditSummaryAsync`, `GetMatCreditsAsync`, `GetMatCreditUtilizationsAsync`
   - Added private helpers: `ComputeAndApplyMatAsync`, `ComputeMatInternal`, `CreateOrUpdateMatCreditEntryAsync`, `RecordMatCreditUtilizationAsync`
   - Added API endpoints: `GET /mat-computation/{id}`, `GET /mat-credit-summary/{companyId}/{fy}`, `GET /mat-credits/{companyId}`, `GET /mat-credit-utilizations/{matCreditId}`

3. **Frontend Changes**:
   - Updated TypeScript types with MAT interfaces
   - Added `getMatComputation`, `getMatCreditSummary`, `getMatCredits`, `getMatCreditUtilizations` to API service
   - Added `matCredit` query keys
   - Added React Query hooks: `useMatComputation`, `useMatCreditSummary`, `useMatCredits`, `useMatCreditUtilizations`
   - Added MAT Computation section in UI with:
     - MAT vs Normal Tax comparison
     - MAT calculation breakdown
     - MAT Credit status (available, utilized, created)
     - Visual tax comparison bar chart
     - Expiring credit alerts

### Logic
```
If (Normal Tax < MAT on Book Profit):
    Tax Payable = MAT (15% of Book Profit + Surcharge + Cess)
    MAT Credit Created = MAT - Normal Tax (carry forward 15 years per Section 115JAA)
Else:
    Tax Payable = Normal Tax
    Utilize available MAT Credit (FIFO - oldest first)
    MAT Credit Utilized = min(Available Credit, Normal Tax - MAT)
```

### Data Model
```
mat_credit_register:
  id UUID PRIMARY KEY
  company_id UUID
  financial_year VARCHAR(10)
  assessment_year VARCHAR(10)
  book_profit DECIMAL(18,2)
  mat_rate DECIMAL(5,2) DEFAULT 15.00
  mat_on_book_profit DECIMAL(18,2)
  mat_surcharge DECIMAL(18,2)
  mat_cess DECIMAL(18,2)
  total_mat DECIMAL(18,2)
  normal_tax DECIMAL(18,2)
  mat_credit_created DECIMAL(18,2)
  mat_credit_utilized DECIMAL(18,2)
  mat_credit_balance DECIMAL(18,2)
  expiry_year VARCHAR(10)  -- Created year + 15
  is_expired BOOLEAN
  status VARCHAR(20)
  created_at, updated_at TIMESTAMPTZ

mat_credit_utilizations:
  id UUID PRIMARY KEY
  mat_credit_id UUID REFERENCES mat_credit_register
  utilization_year VARCHAR(10)
  assessment_id UUID REFERENCES advance_tax_assessments
  amount_utilized DECIMAL(18,2)
  balance_after DECIMAL(18,2)
  notes TEXT
  created_at TIMESTAMPTZ

advance_tax_assessments:
  + is_mat_applicable BOOLEAN
  + mat_book_profit DECIMAL(18,2)
  + mat_rate DECIMAL(5,2) DEFAULT 15.00
  + mat_on_book_profit DECIMAL(18,2)
  + mat_surcharge DECIMAL(18,2)
  + mat_cess DECIMAL(18,2)
  + total_mat DECIMAL(18,2)
  + mat_credit_available DECIMAL(18,2)
  + mat_credit_to_utilize DECIMAL(18,2)
  + mat_credit_created_this_year DECIMAL(18,2)
  + tax_payable_after_mat DECIMAL(18,2)
```

### UI: MAT Computation Section
```
┌─────────────────────────────────────────────────────────────────────────┐
│  MAT COMPUTATION (Section 115JB)                    [Normal Tax Applies]│
│  Minimum Alternate Tax on Book Profit                                   │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐        │
│  │ Tax Comparison   │ │ MAT Calculation  │ │ MAT Credit       │        │
│  │                  │ │                  │ │ (Section 115JAA) │        │
│  │ Normal Tax:      │ │ Book Profit:     │ │                  │        │
│  │   ₹2,50,000      │ │   ₹10,00,000     │ │ Available:       │        │
│  │ MAT @ 15%:       │ │ MAT @ 15%:       │ │   ₹50,000        │        │
│  │   ₹1,75,170      │ │   ₹1,50,000      │ │                  │        │
│  │ Difference:      │ │ H&E Cess @ 4%:   │ │ Credit Utilized: │        │
│  │   +₹74,830       │ │   ₹25,170        │ │   (₹30,000)      │        │
│  │ ─────────────    │ │ ─────────────    │ │                  │        │
│  │ Tax Payable:     │ │ Total MAT:       │ │ ⚠ ₹20,000        │        │
│  │   ₹2,50,000      │ │   ₹1,75,170      │ │ expiring soon    │        │
│  └──────────────────┘ └──────────────────┘ └──────────────────┘        │
│                                                                         │
│  Tax Comparison Visualization:                                          │
│  Normal Tax ████████████████████████████████████████ ₹2,50,000         │
│  MAT        ██████████████████████████████           ₹1,75,170         │
│                                                                         │
│  → You pay the normal tax amount                                        │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Phase 7: Form 280 (Challan) Generation (COMPLETED)

### Goal
Generate pre-filled Form 280 (ITNS 280) challan for advance tax payment.

### Implementation (Done)
1. **DTOs Added to `AdvanceTaxDtos.cs`**:
   - `GenerateForm280Request` - Request to generate challan (assessmentId, quarter, amount, bank details)
   - `Form280ChallanDto` - Complete challan data (taxpayer info, assessment details, payment codes, amounts)
   - `BsrCodeDto` - Bank BSR code lookup entry

2. **PDF Service**: `Form280PdfService.cs`
   - Using QuestPDF library for modern PDF generation
   - Complete ITNS 280 challan format with:
     - Header with form type and tax code checkboxes
     - Taxpayer details section (PAN, TAN, address)
     - Assessment details section (AY, FY, Major/Minor Head codes)
     - Payment details with tax breakdown and amount in words
     - Bank details section (for office use)
     - Instructions section
     - Indian numbering format (Lakh/Crore)

3. **Service Implementation**: Added to `AdvanceTaxService.cs`
   - `GetForm280DataAsync` - Generates pre-filled challan data from assessment
   - `GenerateForm280PdfAsync` - Generates PDF using Form280PdfService
   - `ConvertAmountToWords` - Converts amount to Indian number words (Crore/Lakh/Thousand)

4. **API Endpoints**:
   - `POST /api/tax/advance-tax/form280/data` - Get challan data as JSON
   - `POST /api/tax/advance-tax/form280/pdf` - Download challan as PDF

5. **Frontend Changes**:
   - Added `GenerateForm280Request` and `Form280Challan` TypeScript types
   - Added `getForm280Data` and `downloadForm280Pdf` to API service
   - Added `form280` query keys
   - Added `useForm280Data` and `useDownloadForm280Pdf` hooks
   - Added Download icon button in Quarterly Payment Schedule table
   - Challan downloads for quarters with outstanding payments

### Features
- Pre-filled with company PAN, TAN, address from database
- Auto-calculates amount from schedule shortfall
- Major Head: 0020 (Corporation Tax)
- Minor Head: 100 (Advance Tax)
- Amount displayed in both figures and Indian words
- Quarter-specific due dates and cumulative percentages
- Tax breakdown showing liability, credits, and net payable
- Professional PDF layout matching official ITNS 280 format

### UI Integration
```
┌─────────────────────────────────────────────────────────────────────────┐
│  QUARTERLY PAYMENT SCHEDULE                                             │
├────────┬──────────┬──────────┬──────────┬──────────┬──────────┬────────┤
│ Quarter│ Cum %    │ Payable  │ Paid     │ Shortfall│ Status   │ Actions│
├────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┤
│ Q1     │ 15%      │ ₹30,000  │ ₹30,000  │ -        │ Paid     │        │
│ Q2     │ 45%      │ ₹60,000  │ ₹40,000  │ ₹20,000  │ Partial  │ 💳 📥  │
│ Q3     │ 75%      │ ₹60,000  │ ₹0       │ ₹60,000  │ Pending  │ 💳 📥  │
│ Q4     │ 100%     │ ₹50,000  │ ₹0       │ ₹50,000  │ Pending  │ 💳 📥  │
└────────┴──────────┴──────────┴──────────┴──────────┴──────────┴────────┘
                                                        💳 = Record Payment
                                                        📥 = Download Form 280
```

---

## Phase 8: Compliance Dashboard (COMPLETED)

### Goal
Bird's-eye view of advance tax status across all companies.

### Implementation (Done)
1. **DTOs Added to `AdvanceTaxDtos.cs`**:
   - `ComplianceDashboardDto` - Multi-company summary with totals, status counts, alerts
   - `CompanyComplianceStatusDto` - Individual company compliance status (on_track, at_risk, overdue, no_assessment)
   - `UpcomingDueDateDto` - Aggregated upcoming due dates across companies
   - `CompanyDueDto` - Company due amount for specific date
   - `ComplianceAlertDto` - Alert with type, severity (critical, warning, info), message
   - `YearOnYearComparisonDto` - YoY comparison with growth rates
   - `YearlyTaxSummaryDto` - Yearly tax summary
   - `ComplianceDashboardRequest` - Request with FY and optional company IDs
   - `YearOnYearComparisonRequest` - Request with company ID and number of years

2. **Service Implementation**: Added to `AdvanceTaxService.cs`
   - `GetComplianceDashboardAsync` - Builds multi-company dashboard with:
     - Company-wise compliance statuses
     - Total tax liability, paid, outstanding across all companies
     - Upcoming due dates aggregated by quarter
     - Alerts generation (overdue, upcoming, high interest, missing assessments)
   - `GetYearOnYearComparisonAsync` - Compares tax data across years with:
     - Tax growth rate calculations
     - Compliance rate tracking
     - Average tax liability and interest
   - Private helpers: `BuildCompanyComplianceStatus`, `GenerateAlertsForCompany`, `BuildUpcomingDueDates`, `GetNextQuarterDueDate`

3. **API Endpoints**:
   - `POST /api/tax/advance-tax/compliance-dashboard` - Get multi-company dashboard
   - `POST /api/tax/advance-tax/year-on-year-comparison` - Get YoY comparison

4. **Frontend Changes**:
   - Added TypeScript types for all dashboard interfaces
   - Added `getComplianceDashboard` and `getYearOnYearComparison` to API service
   - Added `complianceDashboard` and `yoyComparison` query keys
   - Added React Query hooks: `useComplianceDashboard`, `useYearOnYearComparison`
   - Created `AdvanceTaxComplianceDashboard.tsx` page with:
     - Overview tab with summary stats, upcoming due dates, alerts
     - Company Status tab with detailed table
     - Year-on-Year tab with comparison table and trend analysis

### Features
1. **Multi-company view**
   - Summary stats: Total companies, tax liability, paid, outstanding
   - Company-wise status: on_track, at_risk, overdue, no_assessment
   - Click-through to company details

2. **Due date calendar with reminders**
   - Aggregated upcoming due dates by quarter
   - Days until due countdown
   - Total amount due across companies

3. **Interest liability alerts**
   - Alert severity: critical (overdue), warning (upcoming), info
   - Alert types: payment_overdue, interest_accruing, upcoming_due, no_assessment
   - Company-specific alert messages

4. **Year-on-year comparison**
   - 5-year historical comparison
   - Tax growth rate calculation
   - Compliance rate tracking
   - Total interest paid across years

### Data Model
```
ComplianceDashboardDto:
  financialYear VARCHAR
  totalCompanies INT
  companiesWithAssessments INT
  companiesWithoutAssessments INT
  companiesFullyPaid INT
  companiesPartiallyPaid INT
  companiesOverdue INT
  totalTaxLiability DECIMAL
  totalTaxPaid DECIMAL
  totalOutstanding DECIMAL
  totalInterestLiability DECIMAL
  currentQuarter INT
  nextDueDate DATE
  daysUntilNextDue INT
  nextQuarterTotalDue DECIMAL
  companyStatuses List<CompanyComplianceStatusDto>
  upcomingDueDates List<UpcomingDueDateDto>
  alerts List<ComplianceAlertDto>

CompanyComplianceStatusDto:
  companyId UUID
  companyName VARCHAR
  status VARCHAR (on_track, at_risk, overdue, no_assessment)
  currentQuarter INT
  totalTaxLiability DECIMAL
  totalTaxPaid DECIMAL
  totalOutstanding DECIMAL
  totalInterest234C DECIMAL
  nextDueDate DATE
  daysUntilNextDue INT
  nextQuarterDue DECIMAL
  assessmentId UUID (nullable)

YearOnYearComparisonDto:
  companyId UUID
  companyName VARCHAR
  yearlySummaries List<YearlyTaxSummaryDto>
  averageTaxLiability DECIMAL
  totalInterestPaid DECIMAL
  complianceRate DECIMAL
  averageTaxGrowth DECIMAL (nullable)
```

### UI: Compliance Dashboard
```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ADVANCE TAX COMPLIANCE                              FY [2024-25 ▼]         │
│  Track advance tax payments across all companies                            │
├─────────────────────────────────────────────────────────────────────────────┤
│  [Overview]  [Company Status]  [Year-on-Year]                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐   │
│  │ 5       │ │₹50L     │ │₹35L     │ │₹15L     │ │₹2.5L    │ │ Q3      │   │
│  │Companies│ │Liability│ │Paid     │ │Due      │ │Interest │ │Quarter  │   │
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘ └─────────┘   │
│                                                                             │
│  ┌───────────────────────────────┐ ┌───────────────────────────────────┐   │
│  │ UPCOMING DUE DATES            │ │ ALERTS                            │   │
│  │ Q3 - 15 Dec 2024              │ │ ⚠ CRITICAL: ABC Ltd - Q2 overdue  │   │
│  │   3 companies • ₹15L due      │ │ ⚠ WARNING: XYZ Pvt - Due in 5 days│   │
│  │   7 days left                 │ │ ℹ INFO: New Co - No assessment    │   │
│  └───────────────────────────────┘ └───────────────────────────────────┘   │
│                                                                             │
│  COMPANIES BY STATUS                                                        │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐                       │
│  │  3       │ │  1       │ │  1       │ │  0       │                       │
│  │Fully Paid│ │ Partial  │ │ Overdue  │ │No Assess │                       │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

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
4. ~~**Phase 5** - TDS/TCS integration~~ ✅ DONE
5. ~~**Phase 4** - Quarterly re-estimation~~ ✅ DONE
6. ~~**Phase 6** - MAT computation~~ ✅ DONE
7. ~~**Phase 7** - Form 280 generation~~ ✅ DONE
8. ~~**Phase 8** - Compliance dashboard~~ ✅ DONE

---

## All Phases Complete!

The Advance Tax Engine is now fully implemented with:
- Auto-fetch YTD actuals from ledger
- YTD vs Projection split (editable projections, locked actuals)
- Book Profit to Taxable Income reconciliation
- TDS/TCS integration from existing modules
- Quarterly re-estimation workflow with audit trail
- MAT (Minimum Alternate Tax) computation with credit register
- Form 280 (ITNS 280) challan generation as PDF
- Multi-company compliance dashboard with YoY comparison
