-- Migration: 117_form_24q_filings.sql
-- Description: Create Form 24Q quarterly TDS filings table for salary TDS returns
-- Date: 2025-01-06

-- Form 24Q Quarterly TDS Filings for Salary
CREATE TABLE IF NOT EXISTS form_24q_filings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Filing identification
    company_id UUID NOT NULL REFERENCES companies(id),
    financial_year VARCHAR(10) NOT NULL,    -- '2024-25' format
    quarter VARCHAR(2) NOT NULL CHECK (quarter IN ('Q1', 'Q2', 'Q3', 'Q4')),
    tan VARCHAR(20) NOT NULL,

    -- Filing type
    form_type VARCHAR(20) DEFAULT 'regular' CHECK (form_type IN ('regular', 'correction')),
    original_filing_id UUID REFERENCES form_24q_filings(id),  -- For corrections
    revision_number INTEGER DEFAULT 0,

    -- Summary data
    total_employees INTEGER DEFAULT 0,
    total_salary_paid NUMERIC(18,2) DEFAULT 0,
    total_tds_deducted NUMERIC(18,2) DEFAULT 0,
    total_tds_deposited NUMERIC(18,2) DEFAULT 0,
    variance NUMERIC(18,2) DEFAULT 0,

    -- Annexure data (JSONB for flexibility)
    annexure1_data JSONB,                   -- Challan details (all quarters)
    annexure2_data JSONB,                   -- Employee annual details (Q4 only)
    employee_records JSONB,                 -- Quarterly employee records
    challan_records JSONB,                  -- Linked challan records

    -- Status workflow
    status VARCHAR(20) DEFAULT 'draft' CHECK (status IN (
        'draft',          -- Initial creation
        'validated',      -- Passed validation
        'fvu_generated',  -- FVU file generated
        'submitted',      -- Submitted to NSDL
        'acknowledged',   -- Received acknowledgement
        'rejected',       -- Rejected by NSDL
        'revised'         -- Superseded by correction
    )),

    -- Validation
    validation_errors JSONB,
    validation_warnings JSONB,
    validated_at TIMESTAMP,
    validated_by UUID,

    -- FVU file
    fvu_file_path VARCHAR(500),
    fvu_generated_at TIMESTAMP,
    fvu_version VARCHAR(10),                -- e.g., '7.8'

    -- Filing details (after submission)
    filing_date DATE,
    acknowledgement_number VARCHAR(50),
    token_number VARCHAR(50),
    provisional_receipt_number VARCHAR(50),

    -- Submission tracking
    submitted_at TIMESTAMP,
    submitted_by UUID,

    -- Rejection handling
    rejection_reason TEXT,
    rejected_at TIMESTAMP,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_by UUID,

    -- Constraints
    CONSTRAINT chk_correction_has_original CHECK (
        (form_type = 'regular' AND original_filing_id IS NULL) OR
        (form_type = 'correction' AND original_filing_id IS NOT NULL)
    )
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_form24q_company ON form_24q_filings(company_id);
CREATE INDEX IF NOT EXISTS idx_form24q_fy_quarter ON form_24q_filings(financial_year, quarter);
CREATE INDEX IF NOT EXISTS idx_form24q_status ON form_24q_filings(status);
CREATE INDEX IF NOT EXISTS idx_form24q_tan ON form_24q_filings(tan);
CREATE INDEX IF NOT EXISTS idx_form24q_filing_date ON form_24q_filings(filing_date);
CREATE INDEX IF NOT EXISTS idx_form24q_created_at ON form_24q_filings(created_at);

-- Unique constraint: One active regular filing per company/FY/quarter
CREATE UNIQUE INDEX IF NOT EXISTS idx_form24q_unique_regular
ON form_24q_filings(company_id, financial_year, quarter)
WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected');

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_form24q_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS form24q_filings_update ON form_24q_filings;
CREATE TRIGGER form24q_filings_update
    BEFORE UPDATE ON form_24q_filings
    FOR EACH ROW
    EXECUTE FUNCTION update_form24q_timestamp();

-- View for filing summary
CREATE OR REPLACE VIEW v_form24q_filing_summary AS
SELECT
    f.id,
    f.company_id,
    c.name as company_name,
    f.financial_year,
    f.quarter,
    f.tan,
    f.form_type,
    f.revision_number,
    f.total_employees,
    f.total_salary_paid,
    f.total_tds_deducted,
    f.total_tds_deposited,
    f.variance,
    f.status,
    f.fvu_file_path IS NOT NULL as has_fvu_file,
    f.acknowledgement_number,
    f.filing_date,
    f.created_at,
    f.updated_at
FROM form_24q_filings f
JOIN companies c ON f.company_id = c.id;

-- View for filing statistics per financial year
CREATE OR REPLACE VIEW v_form24q_statistics AS
SELECT
    company_id,
    financial_year,
    COUNT(*) FILTER (WHERE form_type = 'regular') as total_filings,
    COUNT(*) FILTER (WHERE status = 'draft') as draft_count,
    COUNT(*) FILTER (WHERE status = 'validated') as validated_count,
    COUNT(*) FILTER (WHERE status = 'fvu_generated') as fvu_generated_count,
    COUNT(*) FILTER (WHERE status = 'submitted') as submitted_count,
    COUNT(*) FILTER (WHERE status = 'acknowledged') as acknowledged_count,
    COUNT(*) FILTER (WHERE status = 'rejected') as rejected_count,
    SUM(total_tds_deducted) FILTER (WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected')) as total_tds_deducted,
    SUM(total_tds_deposited) FILTER (WHERE form_type = 'regular' AND status NOT IN ('revised', 'rejected')) as total_tds_deposited
FROM form_24q_filings
GROUP BY company_id, financial_year;

-- Helper function to get due date for a quarter
CREATE OR REPLACE FUNCTION get_form24q_due_date(fy VARCHAR(10), qtr VARCHAR(2))
RETURNS DATE AS $$
DECLARE
    start_year INTEGER;
BEGIN
    start_year := CAST(SUBSTRING(fy FROM 1 FOR 4) AS INTEGER);

    RETURN CASE qtr
        WHEN 'Q1' THEN MAKE_DATE(start_year, 7, 31)      -- July 31
        WHEN 'Q2' THEN MAKE_DATE(start_year, 10, 31)     -- October 31
        WHEN 'Q3' THEN MAKE_DATE(start_year + 1, 1, 31)  -- January 31
        WHEN 'Q4' THEN MAKE_DATE(start_year + 1, 5, 31)  -- May 31
    END;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Helper function to check if filing is overdue
CREATE OR REPLACE FUNCTION is_form24q_overdue(fy VARCHAR(10), qtr VARCHAR(2), current_status VARCHAR(20))
RETURNS BOOLEAN AS $$
BEGIN
    IF current_status IN ('acknowledged', 'revised') THEN
        RETURN FALSE;
    END IF;

    RETURN CURRENT_DATE > get_form24q_due_date(fy, qtr);
END;
$$ LANGUAGE plpgsql STABLE;

-- Add comment
COMMENT ON TABLE form_24q_filings IS 'Form 24Q quarterly TDS returns for salary payments. Tracks filing lifecycle from draft to acknowledgement.';
