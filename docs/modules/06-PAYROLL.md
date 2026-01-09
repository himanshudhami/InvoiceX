# Payroll Module

## Status
- **Current State**: Working
- **Last Updated**: 2026-01-09
- **Active Issues**: None critical

---

## Overview

The Payroll module handles employee salary processing, tax calculations (TDS under Section 192), statutory deductions (PF, ESI, PT), and compliance. Supports both old and new tax regimes with configurable rule packs.

### Key Features
- Employee salary structure management
- Monthly payroll runs with calculation engine
- Tax declaration and computation (Old/New regime)
- Statutory deductions (PF, ESI, Professional Tax)
- Payslip generation
- Form 16 / Form 24Q support

### Key Entities
- **Employees** - Employee master
- **Employee Salary Structures** - CTC breakdown
- **Employee Tax Declarations** - 80C, 80D, HRA, etc.
- **Payroll Runs** - Monthly payroll batches
- **Payroll Transactions** - Per-employee payroll records
- **Payroll Calculation Lines** - Detailed calculation breakdown
- **Salary Components** - Configurable salary heads
- **Statutory Payments** - PF/ESI/TDS deposit tracking

---

## Database Schema

### employees
Employee master (salaried staff only, contractors use parties).

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `employee_id` | VARCHAR | Employee code |
| `employee_name` | VARCHAR | Full name |
| `email` | VARCHAR | Email |
| `phone` | VARCHAR | Phone |
| `department` | VARCHAR | Department |
| `designation` | VARCHAR | Job title |
| `hire_date` | DATE | Joining date |
| `status` | VARCHAR | `active`, `inactive`, `terminated` |
| `employment_type` | VARCHAR | `full_time`, `part_time`, `contractor` |
| **Bank Details** |
| `bank_account_number` | VARCHAR | Bank account |
| `bank_name` | VARCHAR | Bank name |
| `ifsc_code` | VARCHAR | IFSC |
| **Tax** |
| `pan_number` | VARCHAR | PAN |
| **Address** |
| `address_line1`, `address_line2`, `city`, `state`, `zip_code`, `country` | VARCHAR | Address |
| **Hierarchy** |
| `manager_id` | UUID | FK to employees (manager) |
| `reporting_level` | INTEGER | Hierarchy level |
| `is_manager` | BOOLEAN | Manager flag |
| **Resignation** |
| `resigned_at` | TIMESTAMP | Resignation date |
| `resignation_reason` | TEXT | Reason |

### employee_salary_structures
CTC breakdown with revision history.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `employee_id` | UUID | FK to employees |
| `company_id` | UUID | FK to companies |
| `effective_from` | DATE | Structure start date |
| `effective_to` | DATE | Structure end date |
| `is_active` | BOOLEAN | Current structure |
| **CTC Components** |
| `annual_ctc` | NUMERIC | Annual CTC |
| `basic_salary` | NUMERIC | Monthly basic |
| `hra` | NUMERIC | House Rent Allowance |
| `dearness_allowance` | NUMERIC | DA |
| `conveyance_allowance` | NUMERIC | Conveyance |
| `medical_allowance` | NUMERIC | Medical |
| `special_allowance` | NUMERIC | Special allowance |
| `other_allowances` | NUMERIC | Other allowances |
| `lta_annual` | NUMERIC | Leave Travel Allowance |
| `bonus_annual` | NUMERIC | Annual bonus |
| **Employer Contributions** |
| `pf_employer_monthly` | NUMERIC | Employer PF |
| `esi_employer_monthly` | NUMERIC | Employer ESI |
| `gratuity_monthly` | NUMERIC | Gratuity provision |
| `monthly_gross` | NUMERIC | Monthly gross |
| **Approval** |
| `revision_reason` | VARCHAR | Reason for revision |
| `approved_by` | VARCHAR | Approver |
| `approved_at` | TIMESTAMP | Approval timestamp |

### employee_tax_declarations
Tax investment declarations for TDS calculation.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `employee_id` | UUID | FK to employees |
| `financial_year` | VARCHAR | FY (e.g., `2024-25`) |
| `tax_regime` | VARCHAR | `old`, `new` |
| **Section 80C** |
| `sec_80c_ppf` | NUMERIC | PPF |
| `sec_80c_elss` | NUMERIC | ELSS mutual funds |
| `sec_80c_life_insurance` | NUMERIC | Life insurance |
| `sec_80c_home_loan_principal` | NUMERIC | Home loan principal |
| `sec_80c_children_tuition` | NUMERIC | Children education |
| `sec_80c_nsc` | NUMERIC | NSC |
| `sec_80c_sukanya_samriddhi` | NUMERIC | SSY |
| `sec_80c_fixed_deposit` | NUMERIC | Tax-saving FD |
| `sec_80c_others` | NUMERIC | Other 80C |
| **Section 80CCD** |
| `sec_80ccd_nps` | NUMERIC | NPS (additional 50K) |
| **Section 80D** |
| `sec_80d_self_family` | NUMERIC | Health insurance self/family |
| `sec_80d_parents` | NUMERIC | Health insurance parents |
| `sec_80d_preventive_checkup` | NUMERIC | Preventive checkup |
| `sec_80d_self_senior_citizen` | BOOLEAN | Self is senior citizen |
| `sec_80d_parents_senior_citizen` | BOOLEAN | Parents are senior citizens |
| **Other Sections** |
| `sec_80e_education_loan` | NUMERIC | Education loan interest |
| `sec_24_home_loan_interest` | NUMERIC | Home loan interest |
| `sec_80g_donations` | NUMERIC | Donations |
| `sec_80tta_savings_interest` | NUMERIC | Savings interest exemption |
| **HRA** |
| `hra_rent_paid_annual` | NUMERIC | Annual rent paid |
| `hra_metro_city` | BOOLEAN | Metro city (50% vs 40%) |
| `hra_landlord_pan` | VARCHAR | Landlord PAN (if rent > 1L) |
| `hra_landlord_name` | VARCHAR | Landlord name |
| **Other Income** |
| `other_income_annual` | NUMERIC | Other taxable income |
| `prev_employer_income` | NUMERIC | Previous employer income |
| `prev_employer_tds` | NUMERIC | TDS from previous employer |
| `prev_employer_pf` | NUMERIC | PF from previous employer |
| `prev_employer_pt` | NUMERIC | PT from previous employer |
| **Other TDS/TCS Credits** |
| `other_tds_interest` | NUMERIC | TDS on interest (194A) |
| `other_tds_dividend` | NUMERIC | TDS on dividend |
| `other_tds_commission` | NUMERIC | TDS on commission |
| `other_tds_rent` | NUMERIC | TDS on rent |
| `other_tds_professional` | NUMERIC | TDS on professional fees |
| `other_tds_others` | NUMERIC | Other TDS |
| `tcs_foreign_remittance` | NUMERIC | TCS on forex |
| `tcs_overseas_tour` | NUMERIC | TCS on tours |
| `tcs_vehicle_purchase` | NUMERIC | TCS on vehicle |
| `tcs_others` | NUMERIC | Other TCS |
| **Status** |
| `status` | VARCHAR | `draft`, `submitted`, `verified`, `locked`, `rejected` |
| `submitted_at`, `verified_at`, `locked_at` | TIMESTAMP | Timestamps |
| `verified_by` | VARCHAR | Verified by |
| `rejection_reason` | TEXT | Reason if rejected |
| `revision_count` | INTEGER | Revision count |
| `proof_documents` | JSONB | Uploaded proofs |

### payroll_runs
Monthly payroll batch processing.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `company_id` | UUID | FK to companies |
| `payroll_month` | INTEGER | Month (1-12) |
| `payroll_year` | INTEGER | Year |
| `financial_year` | VARCHAR | FY |
| `status` | VARCHAR | `draft`, `computed`, `approved`, `paid`, `cancelled` |
| **Totals** |
| `total_employees` | INTEGER | Employee count |
| `total_contractors` | INTEGER | Contractor count |
| `total_gross_salary` | NUMERIC | Gross salary |
| `total_deductions` | NUMERIC | Total deductions |
| `total_net_salary` | NUMERIC | Net salary |
| `total_employer_pf` | NUMERIC | Employer PF |
| `total_employer_esi` | NUMERIC | Employer ESI |
| `total_employer_cost` | NUMERIC | Total employer cost |
| **Workflow** |
| `computed_by`, `computed_at` | - | Computed by/at |
| `approved_by`, `approved_at` | - | Approved by/at |
| `paid_by`, `paid_at` | - | Paid by/at |
| `payment_reference` | VARCHAR | Payment reference |
| `payment_mode` | VARCHAR | `bank_transfer`, `cheque` |
| `remarks` | TEXT | Remarks |

### payroll_calculation_lines
Detailed calculation breakdown for audit trail.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID | Primary key |
| `transaction_id` | UUID | FK to payroll_transactions |
| `line_type` | VARCHAR | `earning`, `deduction`, `employer` |
| `line_sequence` | INTEGER | Sequence |
| `rule_code` | VARCHAR | Calculation rule |
| `description` | VARCHAR | Line description |
| `base_amount` | NUMERIC | Base for calculation |
| `rate` | NUMERIC | Rate/percentage |
| `computed_amount` | NUMERIC | Computed amount |
| `config_version` | VARCHAR | Rule version |
| `config_snapshot` | JSONB | Rule config snapshot |
| `notes` | TEXT | Calculation notes |

---

## Backend Structure

### Entities
- `Core/Entities/Employee.cs`
- `Core/Entities/EmployeeSalaryStructure.cs`
- `Core/Entities/EmployeeTaxDeclaration.cs`
- `Core/Entities/Payroll/PayrollRun.cs`
- `Core/Entities/Payroll/PayrollTransaction.cs`
- `Core/Entities/Payroll/PayrollCalculationLine.cs`
- `Core/Entities/Payroll/SalaryComponent.cs`

### Services
- `Application/Services/Payroll/PayrollCalculationService.cs`
- `Application/Services/Payroll/TaxCalculationService.cs`

### Controllers
- `WebApi/Controllers/EmployeesController.cs`
- `WebApi/Controllers/Payroll/PayrollRunsController.cs`
- `WebApi/Controllers/Payroll/SalaryStructuresController.cs`
- `WebApi/Controllers/Payroll/TaxDeclarationsController.cs`

---

## Frontend Structure

### Pages
- `pages/employees/EmployeesPage.tsx` - Employee list
- `pages/employees/EmployeeDetailPage.tsx` - Employee profile
- `pages/payroll/PayrollRunsPage.tsx` - Payroll runs
- `pages/payroll/PayrollRunDetailPage.tsx` - Run detail
- `pages/payroll/SalaryStructuresPage.tsx` - Salary structures
- `pages/payroll/TaxDeclarationsPage.tsx` - Tax declarations

### Services
- `services/api/hr/employees/employeeService.ts`
- `services/api/hr/payroll/payrollService.ts`
- `services/api/hr/payroll/salaryStructureService.ts`
- `services/api/hr/payroll/taxDeclarationService.ts`

---

## API Endpoints

### Employees
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/employees` | List employees |
| GET | `/api/employees/{id}` | Get employee |
| POST | `/api/employees` | Create employee |
| PUT | `/api/employees/{id}` | Update employee |

### Payroll Runs
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/payroll-runs` | List runs |
| POST | `/api/payroll-runs` | Create run |
| POST | `/api/payroll-runs/{id}/compute` | Calculate payroll |
| POST | `/api/payroll-runs/{id}/approve` | Approve run |
| POST | `/api/payroll-runs/{id}/process-payment` | Mark as paid |

### Tax Declarations
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tax-declarations/{employeeId}/{fy}` | Get declaration |
| POST | `/api/tax-declarations` | Submit declaration |
| PUT | `/api/tax-declarations/{id}` | Update declaration |
| POST | `/api/tax-declarations/{id}/verify` | Verify declaration |

---

## Business Rules

### Tax Calculation (New Regime FY 2024-25)
| Income Slab | Rate |
|-------------|------|
| 0 - 3L | 0% |
| 3L - 7L | 5% |
| 7L - 10L | 10% |
| 10L - 12L | 15% |
| 12L - 15L | 20% |
| Above 15L | 30% |

Standard deduction: Rs. 75,000

### Old Regime (Basic)
| Income Slab | Rate |
|-------------|------|
| 0 - 2.5L | 0% |
| 2.5L - 5L | 5% |
| 5L - 10L | 20% |
| Above 10L | 30% |

### Statutory Rates (FY 2024-25)
| Component | Employee | Employer |
|-----------|----------|----------|
| PF | 12% of basic | 12% of basic |
| ESI | 0.75% | 3.25% |
| PT | State-specific | - |

### PF Applicability
- Basic + DA ≤ Rs. 15,000: Mandatory
- Above: Optional (can be limited to 15K)
- Employer: 12% (3.67% PF + 8.33% EPS)

### ESI Applicability
- Gross ≤ Rs. 21,000: Applicable
- Above: Not applicable

---

## Current Gaps / TODO

- [ ] Form 16 generation
- [ ] Form 24Q preparation
- [ ] Payslip PDF generation
- [ ] Arrears calculation
- [ ] Bonus calculation
- [ ] FnF settlement

---

## Related Modules

- [TDS/TCS](10-TDS-TCS.md) - TDS returns
- [Ledger](05-LEDGER.md) - Payroll posting
- [Contractor Payments](03-CONTRACTOR-PAYMENTS.md) - Contractors

---

## Session Notes

### 2026-01-09
- Initial documentation created
- Core payroll calculation operational
- Tax regime support (old/new) functional
