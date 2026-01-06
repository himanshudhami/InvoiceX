-- Migration: Form 16 (TDS Certificate) for Salary
-- Purpose: Store Form 16 Part A (TDS summary) and Part B (salary computation)
-- Reference: Income Tax Act Section 192, Rule 31

-- ============================================
-- Form 16 Table
-- TDS Certificate issued to employees
-- ============================================

CREATE TABLE IF NOT EXISTS form_16 (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- ==================== Company & Employee Linking ====================
    company_id UUID NOT NULL REFERENCES companies(id),
    employee_id UUID NOT NULL REFERENCES employees(id),
    financial_year VARCHAR(10) NOT NULL,           -- '2024-25' format
    certificate_number VARCHAR(100) NOT NULL,       -- Unique per company/FY

    -- ==================== Part A - Deductor Details ====================
    tan VARCHAR(20) NOT NULL,                       -- Tax Deduction Account Number
    deductor_pan VARCHAR(20) NOT NULL,              -- PAN of employer
    deductor_name VARCHAR(255) NOT NULL,
    deductor_address TEXT,
    deductor_city VARCHAR(100),
    deductor_state VARCHAR(100),
    deductor_pincode VARCHAR(10),
    deductor_email VARCHAR(255),
    deductor_phone VARCHAR(20),

    -- ==================== Part A - Employee Details ====================
    employee_pan VARCHAR(20) NOT NULL,
    employee_name VARCHAR(255) NOT NULL,
    employee_address TEXT,
    employee_city VARCHAR(100),
    employee_state VARCHAR(100),
    employee_pincode VARCHAR(10),
    employee_email VARCHAR(255),

    -- ==================== Part A - Employment Period ====================
    period_from DATE NOT NULL,
    period_to DATE NOT NULL,

    -- ==================== Part A - Quarterly TDS Summary ====================
    q1_tds_deducted DECIMAL(18,2) DEFAULT 0,
    q1_tds_deposited DECIMAL(18,2) DEFAULT 0,
    q1_challan_details JSONB,                       -- [{challanNo, bsrCode, depositDate, amount}]

    q2_tds_deducted DECIMAL(18,2) DEFAULT 0,
    q2_tds_deposited DECIMAL(18,2) DEFAULT 0,
    q2_challan_details JSONB,

    q3_tds_deducted DECIMAL(18,2) DEFAULT 0,
    q3_tds_deposited DECIMAL(18,2) DEFAULT 0,
    q3_challan_details JSONB,

    q4_tds_deducted DECIMAL(18,2) DEFAULT 0,
    q4_tds_deposited DECIMAL(18,2) DEFAULT 0,
    q4_challan_details JSONB,

    total_tds_deducted DECIMAL(18,2) DEFAULT 0,
    total_tds_deposited DECIMAL(18,2) DEFAULT 0,

    -- ==================== Part B - Salary Details (Section 17) ====================
    gross_salary DECIMAL(18,2) DEFAULT 0,           -- Section 17(1)
    perquisites DECIMAL(18,2) DEFAULT 0,            -- Section 17(2)
    profits_in_lieu DECIMAL(18,2) DEFAULT 0,        -- Section 17(3)
    total_salary DECIMAL(18,2) DEFAULT 0,

    -- ==================== Part B - Exemptions (Section 10) ====================
    hra_exemption DECIMAL(18,2) DEFAULT 0,          -- Section 10(13A)
    lta_exemption DECIMAL(18,2) DEFAULT 0,          -- Section 10(5)
    other_exemptions DECIMAL(18,2) DEFAULT 0,
    total_exemptions DECIMAL(18,2) DEFAULT 0,

    -- ==================== Part B - Deductions ====================
    standard_deduction DECIMAL(18,2) DEFAULT 0,     -- Section 16(ia)
    entertainment_allowance DECIMAL(18,2) DEFAULT 0, -- Section 16(ii)
    professional_tax DECIMAL(18,2) DEFAULT 0,       -- Section 16(iii)

    -- Chapter VI-A deductions
    section_80c DECIMAL(18,2) DEFAULT 0,            -- PPF, ELSS, LIC, etc. (Max 1.5L)
    section_80ccc DECIMAL(18,2) DEFAULT 0,          -- Pension contribution
    section_80ccd1 DECIMAL(18,2) DEFAULT 0,         -- NPS employee (10% of salary)
    section_80ccd1b DECIMAL(18,2) DEFAULT 0,        -- Additional NPS (Max 50K)
    section_80ccd2 DECIMAL(18,2) DEFAULT 0,         -- Employer NPS contribution
    section_80d DECIMAL(18,2) DEFAULT 0,            -- Health insurance
    section_80e DECIMAL(18,2) DEFAULT 0,            -- Education loan interest
    section_80g DECIMAL(18,2) DEFAULT 0,            -- Donations
    section_80tta DECIMAL(18,2) DEFAULT 0,          -- Savings interest (Max 10K)
    section_24 DECIMAL(18,2) DEFAULT 0,             -- Home loan interest (Max 2L)
    other_deductions DECIMAL(18,2) DEFAULT 0,
    total_deductions DECIMAL(18,2) DEFAULT 0,

    -- ==================== Part B - Tax Computation ====================
    tax_regime VARCHAR(10) DEFAULT 'new' CHECK (tax_regime IN ('old', 'new')),
    taxable_income DECIMAL(18,2) DEFAULT 0,
    tax_on_income DECIMAL(18,2) DEFAULT 0,          -- As per slab rates
    rebate_87a DECIMAL(18,2) DEFAULT 0,             -- Section 87A rebate
    tax_after_rebate DECIMAL(18,2) DEFAULT 0,
    surcharge DECIMAL(18,2) DEFAULT 0,              -- For income > 50L
    cess DECIMAL(18,2) DEFAULT 0,                   -- Health & Education Cess 4%
    total_tax_liability DECIMAL(18,2) DEFAULT 0,
    relief_89 DECIMAL(18,2) DEFAULT 0,              -- Relief for arrears
    net_tax_payable DECIMAL(18,2) DEFAULT 0,

    -- ==================== Part B - Other Income ====================
    previous_employer_income DECIMAL(18,2) DEFAULT 0,
    previous_employer_tds DECIMAL(18,2) DEFAULT 0,
    other_income DECIMAL(18,2) DEFAULT 0,

    -- ==================== Verification & Signature ====================
    verified_by_name VARCHAR(255),
    verified_by_designation VARCHAR(255),
    verified_by_pan VARCHAR(20),
    place VARCHAR(100),
    signature_date DATE,

    -- ==================== Status & Workflow ====================
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN ('draft', 'generated', 'verified', 'issued', 'cancelled')),
    generated_at TIMESTAMP,
    generated_by UUID,
    verified_at TIMESTAMP,
    verified_by UUID,
    issued_at TIMESTAMP,
    issued_by UUID,
    cancelled_at TIMESTAMP,
    cancelled_by UUID,
    cancellation_reason TEXT,

    -- ==================== PDF & Storage ====================
    pdf_path VARCHAR(500),

    -- ==================== Detailed Breakdown JSON ====================
    salary_breakdown_json JSONB,                    -- Detailed salary components
    tax_computation_json JSONB,                     -- Detailed tax calculation

    -- ==================== Audit Fields ====================
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_by UUID,

    -- ==================== Constraints ====================
    CONSTRAINT chk_period_valid CHECK (period_to >= period_from),
    CONSTRAINT chk_tax_values CHECK (
        total_tds_deducted >= 0 AND
        total_tds_deposited >= 0 AND
        gross_salary >= 0 AND
        taxable_income >= 0
    )
);

-- ============================================
-- Indexes for Form 16
-- ============================================

CREATE INDEX IF NOT EXISTS idx_form16_company ON form_16(company_id);
CREATE INDEX IF NOT EXISTS idx_form16_employee ON form_16(employee_id);
CREATE INDEX IF NOT EXISTS idx_form16_fy ON form_16(financial_year);
CREATE INDEX IF NOT EXISTS idx_form16_status ON form_16(status);
CREATE INDEX IF NOT EXISTS idx_form16_pan ON form_16(employee_pan);
CREATE INDEX IF NOT EXISTS idx_form16_generated ON form_16(generated_at);
CREATE INDEX IF NOT EXISTS idx_form16_issued ON form_16(issued_at);

-- Unique constraint: One Form 16 per employee per FY
CREATE UNIQUE INDEX IF NOT EXISTS idx_form16_unique_emp_fy
ON form_16(company_id, employee_id, financial_year)
WHERE status != 'cancelled';

-- Unique certificate number per company
CREATE UNIQUE INDEX IF NOT EXISTS idx_form16_cert_unique
ON form_16(company_id, certificate_number);

-- ============================================
-- Trigger: Auto-update timestamp
-- ============================================

CREATE OR REPLACE FUNCTION update_form16_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS form16_update ON form_16;
CREATE TRIGGER form16_update
    BEFORE UPDATE ON form_16
    FOR EACH ROW
    EXECUTE FUNCTION update_form16_timestamp();

-- ============================================
-- Function: Generate certificate number
-- Format: TAN/FY/SERIAL (e.g., BLRA12345E/2425/0001)
-- ============================================

CREATE OR REPLACE FUNCTION generate_form16_cert_number(
    p_company_id UUID,
    p_financial_year VARCHAR(10)
)
RETURNS VARCHAR(100) AS $$
DECLARE
    v_tan VARCHAR(20);
    v_fy_short VARCHAR(4);
    v_serial INTEGER;
    v_cert_number VARCHAR(100);
BEGIN
    -- Get company TAN
    SELECT tan INTO v_tan
    FROM company_statutory_config
    WHERE company_id = p_company_id
    LIMIT 1;

    IF v_tan IS NULL THEN
        v_tan := 'NOTANSET';
    END IF;

    -- Convert FY (e.g., '2024-25' -> '2425')
    v_fy_short := REPLACE(p_financial_year, '-', '');
    v_fy_short := SUBSTRING(v_fy_short FROM 3 FOR 4);

    -- Get next serial
    SELECT COALESCE(MAX(
        CAST(NULLIF(SPLIT_PART(certificate_number, '/', 3), '') AS INTEGER)
    ), 0) + 1
    INTO v_serial
    FROM form_16
    WHERE company_id = p_company_id
      AND financial_year = p_financial_year;

    -- Generate certificate number
    v_cert_number := v_tan || '/' || v_fy_short || '/' || LPAD(v_serial::TEXT, 4, '0');

    RETURN v_cert_number;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- View: Form 16 Summary with Employee Details
-- ============================================

CREATE OR REPLACE VIEW v_form16_summary AS
SELECT
    f.id,
    f.company_id,
    f.employee_id,
    e.employee_id as employee_code,
    e.employee_name as employee_full_name,
    f.employee_pan,
    f.financial_year,
    f.certificate_number,
    f.gross_salary,
    f.total_exemptions,
    f.total_deductions,
    f.taxable_income,
    f.tax_regime,
    f.total_tax_liability,
    f.net_tax_payable,
    f.total_tds_deducted,
    f.total_tds_deposited,
    (f.total_tds_deducted - f.total_tds_deposited) as tds_variance,
    f.status,
    f.generated_at,
    f.verified_at,
    f.issued_at,
    f.pdf_path IS NOT NULL as has_pdf,
    f.created_at,
    f.updated_at
FROM form_16 f
JOIN employees e ON f.employee_id = e.id;

-- ============================================
-- View: Form 16 Statistics by Company/FY
-- ============================================

CREATE OR REPLACE VIEW v_form16_statistics AS
SELECT
    f.company_id,
    f.financial_year,
    COUNT(*) as total_forms,
    COUNT(*) FILTER (WHERE f.status = 'draft') as draft_count,
    COUNT(*) FILTER (WHERE f.status = 'generated') as generated_count,
    COUNT(*) FILTER (WHERE f.status = 'verified') as verified_count,
    COUNT(*) FILTER (WHERE f.status = 'issued') as issued_count,
    COUNT(*) FILTER (WHERE f.status = 'cancelled') as cancelled_count,
    SUM(f.gross_salary) as total_gross_salary,
    SUM(f.taxable_income) as total_taxable_income,
    SUM(f.total_tds_deducted) as total_tds_deducted,
    SUM(f.total_tds_deposited) as total_tds_deposited,
    COUNT(*) FILTER (WHERE f.total_tds_deducted > 0) as employees_with_tds,
    COUNT(*) FILTER (WHERE f.total_tds_deducted = 0) as employees_without_tds,
    COUNT(*) FILTER (WHERE f.tax_regime = 'new') as new_regime_count,
    COUNT(*) FILTER (WHERE f.tax_regime = 'old') as old_regime_count
FROM form_16 f
WHERE f.status != 'cancelled'
GROUP BY f.company_id, f.financial_year;

-- ============================================
-- Function: Get employees pending Form 16
-- ============================================

CREATE OR REPLACE FUNCTION get_employees_pending_form16(
    p_company_id UUID,
    p_financial_year VARCHAR(10)
)
RETURNS TABLE (
    emp_id UUID,
    emp_code VARCHAR(50),
    emp_name VARCHAR(255),
    emp_pan VARCHAR(20),
    has_payroll_data BOOLEAN,
    total_tds_deducted DECIMAL(18,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        e.id as emp_id,
        e.employee_id as emp_code,
        e.employee_name as emp_name,
        epi.pan as emp_pan,
        EXISTS (
            SELECT 1 FROM payroll_transactions pt
            JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
            WHERE pt.employee_id = e.id
              AND pr.company_id = p_company_id
              AND pt.financial_year = p_financial_year
              AND pr.status IN ('approved', 'paid')
        ) as has_payroll_data,
        COALESCE((
            SELECT SUM(pt.tds_deducted)
            FROM payroll_transactions pt
            JOIN payroll_runs pr ON pt.payroll_run_id = pr.id
            WHERE pt.employee_id = e.id
              AND pr.company_id = p_company_id
              AND pt.financial_year = p_financial_year
              AND pr.status IN ('approved', 'paid')
        ), 0) as total_tds_deducted
    FROM employees e
    LEFT JOIN employee_payroll_info epi ON e.id = epi.employee_id
    WHERE e.company_id = p_company_id
      AND e.status IN ('active', 'resigned')
      AND NOT EXISTS (
          SELECT 1 FROM form_16 f
          WHERE f.employee_id = e.id
            AND f.company_id = p_company_id
            AND f.financial_year = p_financial_year
            AND f.status != 'cancelled'
      )
    ORDER BY e.employee_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Comments
-- ============================================

COMMENT ON TABLE form_16 IS 'Form 16 TDS Certificate for salary (Section 192) - Part A (TDS summary) and Part B (salary computation)';
COMMENT ON COLUMN form_16.tan IS 'Tax Deduction Account Number of the employer';
COMMENT ON COLUMN form_16.certificate_number IS 'Unique certificate number format: TAN/FYFY/SERIAL';
COMMENT ON COLUMN form_16.q1_challan_details IS 'JSON array of challan details: [{challanNo, bsrCode, depositDate, amount}]';
COMMENT ON COLUMN form_16.salary_breakdown_json IS 'Detailed salary breakdown for Part B computation';
COMMENT ON COLUMN form_16.tax_computation_json IS 'Detailed tax calculation slab-wise breakdown';
COMMENT ON FUNCTION generate_form16_cert_number IS 'Generate unique Form 16 certificate number';
COMMENT ON FUNCTION get_employees_pending_form16 IS 'Get list of employees who need Form 16 generation';
