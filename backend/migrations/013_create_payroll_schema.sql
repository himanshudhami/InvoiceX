-- 013_create_payroll_schema.sql
-- New Payroll Module Schema - Indian Tax Compliant
-- Replaces old employee_salary_transactions with proper payroll structure

-- ============================================================================
-- DROP OLD SCHEMA
-- ============================================================================

-- Drop old salary transactions table (starting fresh)
DROP TABLE IF EXISTS employee_salary_transactions CASCADE;

-- ============================================================================
-- TAX CONFIGURATION TABLES
-- ============================================================================

-- Income Tax Slabs (Old and New Regime)
CREATE TABLE IF NOT EXISTS tax_slabs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    regime VARCHAR(10) NOT NULL CHECK (regime IN ('old', 'new')),
    financial_year VARCHAR(10) NOT NULL, -- '2024-25'
    min_income NUMERIC(15,2) NOT NULL,
    max_income NUMERIC(15,2), -- NULL means no upper limit
    rate NUMERIC(5,2) NOT NULL, -- percentage
    cess_rate NUMERIC(5,2) NOT NULL DEFAULT 4.00, -- 4% health & education cess
    surcharge_threshold NUMERIC(15,2), -- income above which surcharge applies
    surcharge_rate NUMERIC(5,2), -- surcharge percentage
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Professional Tax Slabs (State-wise)
CREATE TABLE IF NOT EXISTS professional_tax_slabs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    state VARCHAR(50) NOT NULL,
    min_monthly_income NUMERIC(12,2) NOT NULL,
    max_monthly_income NUMERIC(12,2), -- NULL means no upper limit
    monthly_tax NUMERIC(10,2) NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================================
-- COMPANY STATUTORY CONFIGURATION
-- ============================================================================

-- Statutory configuration per company (PF, ESI, PT settings)
CREATE TABLE IF NOT EXISTS company_statutory_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- PF Configuration
    pf_enabled BOOLEAN DEFAULT TRUE,
    pf_registration_number VARCHAR(50),
    pf_employee_rate NUMERIC(5,2) DEFAULT 12.00, -- 12% of basic
    pf_employer_rate NUMERIC(5,2) DEFAULT 12.00, -- 12% of basic
    pf_admin_charges_rate NUMERIC(5,2) DEFAULT 0.50, -- Admin charges
    pf_edli_rate NUMERIC(5,2) DEFAULT 0.50, -- EDLI contribution
    pf_wage_ceiling NUMERIC(12,2) DEFAULT 15000.00, -- PF calculated on max this amount
    pf_include_special_allowance BOOLEAN DEFAULT FALSE, -- Include special allowance in PF wage

    -- ESI Configuration
    esi_enabled BOOLEAN DEFAULT FALSE,
    esi_registration_number VARCHAR(50),
    esi_employee_rate NUMERIC(5,2) DEFAULT 0.75, -- 0.75% of gross
    esi_employer_rate NUMERIC(5,2) DEFAULT 3.25, -- 3.25% of gross
    esi_gross_ceiling NUMERIC(12,2) DEFAULT 21000.00, -- ESI applicable if gross <= this

    -- PT Configuration
    pt_enabled BOOLEAN DEFAULT TRUE,
    pt_state VARCHAR(50) DEFAULT 'Karnataka',
    pt_registration_number VARCHAR(50),

    -- TDS Configuration
    tan_number VARCHAR(20),
    default_tax_regime VARCHAR(10) DEFAULT 'new' CHECK (default_tax_regime IN ('old', 'new')),

    -- Gratuity
    gratuity_enabled BOOLEAN DEFAULT TRUE,
    gratuity_rate NUMERIC(5,2) DEFAULT 4.81, -- 4.81% of basic

    -- Effective dates
    effective_from DATE NOT NULL DEFAULT CURRENT_DATE,
    effective_to DATE, -- NULL means current config

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),

    CONSTRAINT unique_company_config UNIQUE (company_id, effective_from)
);

-- ============================================================================
-- EMPLOYEE PAYROLL INFO (Separate from main employees table)
-- ============================================================================

CREATE TABLE IF NOT EXISTS employee_payroll_info (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Statutory Numbers
    uan VARCHAR(20), -- Universal Account Number (PF)
    pf_account_number VARCHAR(30), -- PF Account Number
    esi_number VARCHAR(20), -- ESI IP Number
    pan_number VARCHAR(15), -- PAN Number

    -- Statutory Applicability
    is_pf_applicable BOOLEAN DEFAULT TRUE,
    is_esi_applicable BOOLEAN DEFAULT FALSE,
    is_pt_applicable BOOLEAN DEFAULT TRUE,

    -- Tax Information
    tax_regime VARCHAR(10) DEFAULT 'new' CHECK (tax_regime IN ('old', 'new')),

    -- Employment Type for payroll
    payroll_type VARCHAR(20) DEFAULT 'employee' CHECK (payroll_type IN ('employee', 'contractor')),

    -- Bank Details
    bank_account_number VARCHAR(50),
    bank_name VARCHAR(100),
    bank_ifsc VARCHAR(20),

    -- Employment Dates
    date_of_joining DATE,
    date_of_leaving DATE,

    -- Status
    is_active BOOLEAN DEFAULT TRUE,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_employee_payroll UNIQUE (employee_id)
);

-- ============================================================================
-- SALARY STRUCTURE (CTC Breakdown - Effective Dated)
-- ============================================================================

CREATE TABLE IF NOT EXISTS employee_salary_structures (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Effective Dating (for salary revisions)
    effective_from DATE NOT NULL,
    effective_to DATE, -- NULL means current structure

    -- Annual CTC
    annual_ctc NUMERIC(15,2) NOT NULL,

    -- Monthly Fixed Components (Earnings)
    basic_salary NUMERIC(12,2) NOT NULL, -- Base for PF calculation
    hra NUMERIC(12,2) DEFAULT 0, -- House Rent Allowance
    dearness_allowance NUMERIC(12,2) DEFAULT 0, -- DA (if applicable)
    conveyance_allowance NUMERIC(12,2) DEFAULT 0,
    medical_allowance NUMERIC(12,2) DEFAULT 0,
    special_allowance NUMERIC(12,2) DEFAULT 0, -- Balancing figure
    other_allowances NUMERIC(12,2) DEFAULT 0,

    -- Annual Components (may be paid monthly or lump sum)
    lta_annual NUMERIC(12,2) DEFAULT 0, -- Leave Travel Allowance
    bonus_annual NUMERIC(12,2) DEFAULT 0, -- Statutory/Performance bonus

    -- Employer Contributions (Part of CTC, not paid to employee)
    pf_employer_monthly NUMERIC(12,2) DEFAULT 0,
    esi_employer_monthly NUMERIC(12,2) DEFAULT 0,
    gratuity_monthly NUMERIC(12,2) DEFAULT 0,

    -- Calculated Monthly Gross (sum of earnings)
    monthly_gross NUMERIC(12,2) NOT NULL,

    -- Flags
    is_active BOOLEAN DEFAULT TRUE,

    -- Metadata
    revision_reason VARCHAR(255), -- 'Annual Increment', 'Promotion', etc.
    approved_by VARCHAR(255),
    approved_at TIMESTAMP WITHOUT TIME ZONE,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),

    -- Ensure no overlapping periods per employee
    CONSTRAINT check_dates CHECK (effective_to IS NULL OR effective_to > effective_from)
);

-- ============================================================================
-- TAX DECLARATIONS (80C, 80D, HRA Exemption)
-- ============================================================================

CREATE TABLE IF NOT EXISTS employee_tax_declarations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    financial_year VARCHAR(10) NOT NULL, -- '2024-25'

    -- Tax Regime Choice
    tax_regime VARCHAR(10) NOT NULL DEFAULT 'new' CHECK (tax_regime IN ('old', 'new')),

    -- Section 80C (Max 1,50,000 combined)
    sec_80c_ppf NUMERIC(12,2) DEFAULT 0,
    sec_80c_elss NUMERIC(12,2) DEFAULT 0,
    sec_80c_life_insurance NUMERIC(12,2) DEFAULT 0,
    sec_80c_home_loan_principal NUMERIC(12,2) DEFAULT 0,
    sec_80c_children_tuition NUMERIC(12,2) DEFAULT 0,
    sec_80c_nsc NUMERIC(12,2) DEFAULT 0,
    sec_80c_sukanya_samriddhi NUMERIC(12,2) DEFAULT 0,
    sec_80c_fixed_deposit NUMERIC(12,2) DEFAULT 0,
    sec_80c_others NUMERIC(12,2) DEFAULT 0,

    -- Section 80CCD(1B) - NPS (Additional 50,000)
    sec_80ccd_nps NUMERIC(12,2) DEFAULT 0,

    -- Section 80D - Health Insurance
    sec_80d_self_family NUMERIC(12,2) DEFAULT 0, -- Max 25,000 (50,000 if senior)
    sec_80d_parents NUMERIC(12,2) DEFAULT 0, -- Max 25,000 (50,000 if senior)
    sec_80d_preventive_checkup NUMERIC(12,2) DEFAULT 0, -- Max 5,000
    sec_80d_self_senior_citizen BOOLEAN DEFAULT FALSE,
    sec_80d_parents_senior_citizen BOOLEAN DEFAULT FALSE,

    -- Section 80E - Education Loan Interest
    sec_80e_education_loan NUMERIC(12,2) DEFAULT 0, -- No limit

    -- Section 24 - Home Loan Interest
    sec_24_home_loan_interest NUMERIC(12,2) DEFAULT 0, -- Max 2,00,000

    -- Section 80G - Donations
    sec_80g_donations NUMERIC(12,2) DEFAULT 0,

    -- Section 80TTA/80TTB - Savings Interest
    sec_80tta_savings_interest NUMERIC(12,2) DEFAULT 0, -- Max 10,000

    -- HRA Exemption Calculation Inputs
    hra_rent_paid_annual NUMERIC(12,2) DEFAULT 0,
    hra_metro_city BOOLEAN DEFAULT FALSE, -- Mumbai, Delhi, Chennai, Kolkata
    hra_landlord_pan VARCHAR(20), -- Required if rent > 1L/year
    hra_landlord_name VARCHAR(255),

    -- Other Income (for TDS calculation)
    other_income_annual NUMERIC(12,2) DEFAULT 0, -- Rental, FD interest, etc.

    -- Previous Employer Details (if joined mid-year)
    prev_employer_income NUMERIC(12,2) DEFAULT 0,
    prev_employer_tds NUMERIC(12,2) DEFAULT 0,
    prev_employer_pf NUMERIC(12,2) DEFAULT 0,
    prev_employer_pt NUMERIC(12,2) DEFAULT 0,

    -- Workflow Status
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'submitted', 'verified', 'locked')),
    submitted_at TIMESTAMP WITHOUT TIME ZONE,
    verified_by VARCHAR(255),
    verified_at TIMESTAMP WITHOUT TIME ZONE,
    locked_at TIMESTAMP WITHOUT TIME ZONE, -- After Form 16 generation

    -- Proof Documents (JSON array of document references)
    proof_documents JSONB,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_employee_declaration UNIQUE (employee_id, financial_year)
);

-- ============================================================================
-- PAYROLL RUN (Monthly Batch Processing)
-- ============================================================================

CREATE TABLE IF NOT EXISTS payroll_runs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Period
    payroll_month INTEGER NOT NULL CHECK (payroll_month >= 1 AND payroll_month <= 12),
    payroll_year INTEGER NOT NULL CHECK (payroll_year >= 2000 AND payroll_year <= 2100),
    financial_year VARCHAR(10) NOT NULL, -- '2024-25'

    -- Status Workflow
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN (
        'draft',       -- Initial state
        'processing',  -- Calculation in progress
        'computed',    -- Ready for review
        'approved',    -- Approved by finance
        'paid',        -- Salaries disbursed
        'cancelled'    -- Cancelled
    )),

    -- Summary Totals
    total_employees INTEGER DEFAULT 0,
    total_contractors INTEGER DEFAULT 0,
    total_gross_salary NUMERIC(15,2) DEFAULT 0,
    total_deductions NUMERIC(15,2) DEFAULT 0,
    total_net_salary NUMERIC(15,2) DEFAULT 0,
    total_employer_pf NUMERIC(15,2) DEFAULT 0,
    total_employer_esi NUMERIC(15,2) DEFAULT 0,
    total_employer_cost NUMERIC(15,2) DEFAULT 0, -- Gross + employer contributions

    -- Workflow Audit
    computed_by VARCHAR(255),
    computed_at TIMESTAMP WITHOUT TIME ZONE,
    approved_by VARCHAR(255),
    approved_at TIMESTAMP WITHOUT TIME ZONE,
    paid_by VARCHAR(255),
    paid_at TIMESTAMP WITHOUT TIME ZONE,
    payment_reference VARCHAR(255),
    payment_mode VARCHAR(50), -- 'neft_batch', 'manual', etc.

    -- Notes
    remarks TEXT,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255),

    CONSTRAINT unique_payroll_run UNIQUE (company_id, payroll_month, payroll_year)
);

-- ============================================================================
-- PAYROLL TRANSACTIONS (Individual Salary Records)
-- ============================================================================

CREATE TABLE IF NOT EXISTS payroll_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payroll_run_id UUID NOT NULL REFERENCES payroll_runs(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    salary_structure_id UUID REFERENCES employee_salary_structures(id),

    -- Period
    payroll_month INTEGER NOT NULL,
    payroll_year INTEGER NOT NULL,

    -- Employee Type
    payroll_type VARCHAR(20) NOT NULL CHECK (payroll_type IN ('employee', 'contractor')),

    -- Attendance (for proration)
    working_days INTEGER DEFAULT 30,
    present_days INTEGER DEFAULT 30,
    lop_days INTEGER DEFAULT 0, -- Loss of Pay

    -- ========== EARNINGS ==========

    -- Fixed Components
    basic_earned NUMERIC(12,2) DEFAULT 0,
    hra_earned NUMERIC(12,2) DEFAULT 0,
    da_earned NUMERIC(12,2) DEFAULT 0,
    conveyance_earned NUMERIC(12,2) DEFAULT 0,
    medical_earned NUMERIC(12,2) DEFAULT 0,
    special_allowance_earned NUMERIC(12,2) DEFAULT 0,
    other_allowances_earned NUMERIC(12,2) DEFAULT 0,

    -- Variable/One-time Earnings
    lta_paid NUMERIC(12,2) DEFAULT 0,
    bonus_paid NUMERIC(12,2) DEFAULT 0,
    arrears NUMERIC(12,2) DEFAULT 0,
    reimbursements NUMERIC(12,2) DEFAULT 0,
    incentives NUMERIC(12,2) DEFAULT 0,
    other_earnings NUMERIC(12,2) DEFAULT 0,

    -- Gross (Sum of all earnings)
    gross_earnings NUMERIC(12,2) NOT NULL DEFAULT 0,

    -- ========== DEDUCTIONS ==========

    -- Statutory Deductions
    pf_employee NUMERIC(12,2) DEFAULT 0, -- 12% of PF wage
    esi_employee NUMERIC(12,2) DEFAULT 0, -- 0.75% if applicable
    professional_tax NUMERIC(12,2) DEFAULT 0, -- State-wise
    tds_deducted NUMERIC(12,2) DEFAULT 0, -- Income tax

    -- Other Deductions
    loan_recovery NUMERIC(12,2) DEFAULT 0,
    advance_recovery NUMERIC(12,2) DEFAULT 0,
    other_deductions NUMERIC(12,2) DEFAULT 0,

    -- Total Deductions
    total_deductions NUMERIC(12,2) NOT NULL DEFAULT 0,

    -- ========== NET PAY ==========
    net_payable NUMERIC(12,2) NOT NULL DEFAULT 0,

    -- ========== EMPLOYER CONTRIBUTIONS (Not deducted, for cost tracking) ==========
    pf_employer NUMERIC(12,2) DEFAULT 0,
    pf_admin_charges NUMERIC(12,2) DEFAULT 0,
    pf_edli NUMERIC(12,2) DEFAULT 0,
    esi_employer NUMERIC(12,2) DEFAULT 0,
    gratuity_provision NUMERIC(12,2) DEFAULT 0,
    total_employer_cost NUMERIC(12,2) DEFAULT 0,

    -- ========== TDS CALCULATION DETAILS (for audit) ==========
    tds_calculation JSONB, -- Stores detailed TDS calculation breakdown
    /*
    {
        "projected_annual_income": 1200000,
        "standard_deduction": 50000,
        "sec_80c_total": 150000,
        "sec_80d_total": 25000,
        "hra_exemption": 120000,
        "taxable_income": 855000,
        "tax_on_income": 52500,
        "cess": 2100,
        "total_tax_liability": 54600,
        "tds_already_deducted": 36400,
        "remaining_months": 4,
        "monthly_tds": 4550,
        "hr_override": null,
        "override_reason": null
    }
    */

    -- HR Override (if manually adjusted)
    tds_hr_override NUMERIC(12,2), -- If HR manually overrode TDS
    tds_override_reason TEXT,

    -- ========== PAYMENT INFO ==========
    status VARCHAR(20) DEFAULT 'computed' CHECK (status IN (
        'computed', 'approved', 'paid', 'cancelled', 'on_hold'
    )),
    payment_date DATE,
    payment_method VARCHAR(50),
    payment_reference VARCHAR(255),
    bank_account VARCHAR(50), -- Employee's bank account used

    -- Notes
    remarks TEXT,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT unique_payroll_transaction UNIQUE (payroll_run_id, employee_id)
);

-- ============================================================================
-- CONTRACTOR PAYMENTS (Simplified - for consulting/contract work)
-- ============================================================================

CREATE TABLE IF NOT EXISTS contractor_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Period
    payment_month INTEGER NOT NULL CHECK (payment_month >= 1 AND payment_month <= 12),
    payment_year INTEGER NOT NULL CHECK (payment_year >= 2000 AND payment_year <= 2100),

    -- Invoice/Contract Reference
    invoice_number VARCHAR(100),
    contract_reference VARCHAR(255),

    -- Payment Details
    gross_amount NUMERIC(12,2) NOT NULL,
    tds_rate NUMERIC(5,2) DEFAULT 10.00, -- Section 194J: 10%
    tds_amount NUMERIC(12,2) DEFAULT 0,
    other_deductions NUMERIC(12,2) DEFAULT 0,
    net_payable NUMERIC(12,2) NOT NULL,

    -- GST (if applicable)
    gst_applicable BOOLEAN DEFAULT FALSE,
    gst_rate NUMERIC(5,2) DEFAULT 18.00,
    gst_amount NUMERIC(12,2) DEFAULT 0,
    total_invoice_amount NUMERIC(12,2), -- Gross + GST

    -- Payment Status
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN (
        'pending', 'approved', 'paid', 'cancelled'
    )),

    -- Payment Info
    payment_date DATE,
    payment_method VARCHAR(50),
    payment_reference VARCHAR(255),

    -- Notes
    description TEXT,
    remarks TEXT,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Tax Slabs
CREATE INDEX IF NOT EXISTS idx_tax_slabs_regime_year ON tax_slabs(regime, financial_year);
CREATE INDEX IF NOT EXISTS idx_tax_slabs_active ON tax_slabs(is_active);

-- Professional Tax Slabs
CREATE INDEX IF NOT EXISTS idx_pt_slabs_state ON professional_tax_slabs(state);
CREATE INDEX IF NOT EXISTS idx_pt_slabs_active ON professional_tax_slabs(is_active);

-- Company Statutory Config
CREATE INDEX IF NOT EXISTS idx_company_statutory_company ON company_statutory_configs(company_id);
CREATE INDEX IF NOT EXISTS idx_company_statutory_active ON company_statutory_configs(is_active);

-- Employee Payroll Info
CREATE INDEX IF NOT EXISTS idx_employee_payroll_employee ON employee_payroll_info(employee_id);
CREATE INDEX IF NOT EXISTS idx_employee_payroll_company ON employee_payroll_info(company_id);
CREATE INDEX IF NOT EXISTS idx_employee_payroll_type ON employee_payroll_info(payroll_type);
CREATE INDEX IF NOT EXISTS idx_employee_payroll_active ON employee_payroll_info(is_active);

-- Salary Structures
CREATE INDEX IF NOT EXISTS idx_salary_structure_employee ON employee_salary_structures(employee_id);
CREATE INDEX IF NOT EXISTS idx_salary_structure_company ON employee_salary_structures(company_id);
CREATE INDEX IF NOT EXISTS idx_salary_structure_effective ON employee_salary_structures(effective_from, effective_to);
CREATE INDEX IF NOT EXISTS idx_salary_structure_active ON employee_salary_structures(is_active);

-- Tax Declarations
CREATE INDEX IF NOT EXISTS idx_tax_declaration_employee ON employee_tax_declarations(employee_id);
CREATE INDEX IF NOT EXISTS idx_tax_declaration_fy ON employee_tax_declarations(financial_year);
CREATE INDEX IF NOT EXISTS idx_tax_declaration_status ON employee_tax_declarations(status);

-- Payroll Runs
CREATE INDEX IF NOT EXISTS idx_payroll_run_company ON payroll_runs(company_id);
CREATE INDEX IF NOT EXISTS idx_payroll_run_period ON payroll_runs(payroll_year, payroll_month);
CREATE INDEX IF NOT EXISTS idx_payroll_run_status ON payroll_runs(status);

-- Payroll Transactions
CREATE INDEX IF NOT EXISTS idx_payroll_tx_run ON payroll_transactions(payroll_run_id);
CREATE INDEX IF NOT EXISTS idx_payroll_tx_employee ON payroll_transactions(employee_id);
CREATE INDEX IF NOT EXISTS idx_payroll_tx_period ON payroll_transactions(payroll_year, payroll_month);
CREATE INDEX IF NOT EXISTS idx_payroll_tx_status ON payroll_transactions(status);

-- Contractor Payments
CREATE INDEX IF NOT EXISTS idx_contractor_payment_employee ON contractor_payments(employee_id);
CREATE INDEX IF NOT EXISTS idx_contractor_payment_company ON contractor_payments(company_id);
CREATE INDEX IF NOT EXISTS idx_contractor_payment_period ON contractor_payments(payment_year, payment_month);
CREATE INDEX IF NOT EXISTS idx_contractor_payment_status ON contractor_payments(status);

-- ============================================================================
-- SEED DATA: Tax Slabs FY 2024-25
-- ============================================================================

-- New Tax Regime (Default)
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate) VALUES
('new', '2024-25', 0, 300000, 0, 4),
('new', '2024-25', 300001, 700000, 5, 4),
('new', '2024-25', 700001, 1000000, 10, 4),
('new', '2024-25', 1000001, 1200000, 15, 4),
('new', '2024-25', 1200001, 1500000, 20, 4),
('new', '2024-25', 1500001, NULL, 30, 4);

-- Old Tax Regime
INSERT INTO tax_slabs (regime, financial_year, min_income, max_income, rate, cess_rate) VALUES
('old', '2024-25', 0, 250000, 0, 4),
('old', '2024-25', 250001, 500000, 5, 4),
('old', '2024-25', 500001, 1000000, 20, 4),
('old', '2024-25', 1000001, NULL, 30, 4);

-- ============================================================================
-- SEED DATA: Professional Tax Slabs (Karnataka)
-- ============================================================================

INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Karnataka', 0, 15000, 0),
('Karnataka', 15001, NULL, 200);

-- Maharashtra
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Maharashtra', 0, 7500, 0),
('Maharashtra', 7501, 10000, 175),
('Maharashtra', 10001, NULL, 200); -- 300 in Feb

-- Tamil Nadu
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Tamil Nadu', 0, 21000, 0),
('Tamil Nadu', 21001, 30000, 100),
('Tamil Nadu', 30001, 45000, 235),
('Tamil Nadu', 45001, 60000, 510),
('Tamil Nadu', 60001, 75000, 760),
('Tamil Nadu', 75001, NULL, 1095);

-- Gujarat (No PT)
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Gujarat', 0, NULL, 0);

-- Delhi (No PT)
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Delhi', 0, NULL, 0);
