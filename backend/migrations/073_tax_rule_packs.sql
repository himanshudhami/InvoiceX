-- Migration: 100_tax_rule_packs.sql
-- Description: Create tax rule packs for versioned, FY-specific tax parameters
-- This enables tax rate updates without code changes

-- Tax Rule Packs - FY-versioned tax configurations
CREATE TABLE IF NOT EXISTS tax_rule_packs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pack_code VARCHAR(50) NOT NULL,
    pack_name VARCHAR(200) NOT NULL,
    financial_year VARCHAR(10) NOT NULL,  -- e.g., '2025-26'
    version INTEGER NOT NULL DEFAULT 1,
    source_notification VARCHAR(255),      -- Government notification reference
    description TEXT,
    status VARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, active, superseded, archived

    -- Income Tax Configuration (JSONB for flexibility)
    income_tax_slabs JSONB,  -- { "new": [...], "old": [...] }
    standard_deductions JSONB,  -- { "new": 75000, "old": 50000 }
    rebate_thresholds JSONB,  -- { "new": { "income_threshold": 1200000, "max_rebate": 60000 } }
    cess_rates JSONB,  -- { "health_education": 4 }
    surcharge_rates JSONB,  -- { "50L_1Cr": 10, "1Cr_2Cr": 15, ... }

    -- TDS Rates
    tds_rates JSONB,  -- { "194J_rate": 10, "194J_threshold": 50000, ... }

    -- PF/ESI Rates
    pf_esi_rates JSONB,  -- { "employee_pf": 12, "employer_pf": 12, "esi_employee": 0.75, ... }

    -- Professional Tax (state-wise)
    professional_tax_config JSONB,  -- { "KA": [...slabs...], "MH": [...], ... }

    -- GST Rates
    gst_rates JSONB,  -- { "standard": [0, 5, 12, 18, 28], "cess_applicable_categories": [...] }

    -- Audit fields
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(255),
    activated_at TIMESTAMP,
    activated_by VARCHAR(255),

    CONSTRAINT uq_tax_rule_pack UNIQUE(financial_year, version)
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_tax_rule_packs_fy ON tax_rule_packs(financial_year);
CREATE INDEX IF NOT EXISTS idx_tax_rule_packs_status ON tax_rule_packs(status);
CREATE INDEX IF NOT EXISTS idx_tax_rule_packs_active ON tax_rule_packs(financial_year, status) WHERE status = 'active';

-- Rule Pack Usage Log - Audit trail for tax computations
CREATE TABLE IF NOT EXISTS rule_pack_usage_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_pack_id UUID NOT NULL REFERENCES tax_rule_packs(id) ON DELETE RESTRICT,
    company_id UUID REFERENCES companies(id) ON DELETE SET NULL,

    computation_type VARCHAR(50) NOT NULL,  -- 'payroll_tds', 'contractor_tds', 'gst_calculation', 'income_tax'
    computation_id UUID NOT NULL,           -- ID of the entity being computed (payroll_run_id, payment_id, etc.)
    computation_date DATE NOT NULL,

    -- Snapshot of rules used (for audit immutability)
    rules_snapshot JSONB NOT NULL,

    -- Computation results summary
    input_amount DECIMAL(18,2),
    computed_tax DECIMAL(18,2),
    effective_rate DECIMAL(8,4),

    computed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    computed_by VARCHAR(255)
);

-- Indexes for usage log
CREATE INDEX IF NOT EXISTS idx_rule_pack_usage_pack ON rule_pack_usage_log(rule_pack_id);
CREATE INDEX IF NOT EXISTS idx_rule_pack_usage_company ON rule_pack_usage_log(company_id);
CREATE INDEX IF NOT EXISTS idx_rule_pack_usage_type ON rule_pack_usage_log(computation_type);
CREATE INDEX IF NOT EXISTS idx_rule_pack_usage_date ON rule_pack_usage_log(computation_date);

-- TDS Section Rates - Detailed TDS section configuration
CREATE TABLE IF NOT EXISTS tds_section_rates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_pack_id UUID NOT NULL REFERENCES tax_rule_packs(id) ON DELETE CASCADE,

    section_code VARCHAR(20) NOT NULL,      -- '194J', '194C', '194H', '194T', etc.
    section_name VARCHAR(200) NOT NULL,     -- 'Professional/Technical Services'

    -- Rate configuration
    rate_individual DECIMAL(5,2) NOT NULL,  -- Rate for individuals/HUF
    rate_company DECIMAL(5,2),              -- Rate for companies (if different)
    rate_no_pan DECIMAL(5,2),               -- Rate when PAN not provided (usually 20%)

    -- Thresholds
    threshold_amount DECIMAL(18,2),         -- Exemption threshold
    threshold_type VARCHAR(20) DEFAULT 'per_transaction',  -- 'per_transaction', 'annual'

    -- Applicability
    payee_types VARCHAR[] DEFAULT ARRAY['individual', 'company', 'partnership'],
    is_active BOOLEAN DEFAULT TRUE,

    notes TEXT,
    effective_from DATE NOT NULL,
    effective_to DATE,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_tds_section_rate UNIQUE(rule_pack_id, section_code, effective_from)
);

-- Index for TDS section lookups
CREATE INDEX IF NOT EXISTS idx_tds_section_rates_pack ON tds_section_rates(rule_pack_id);
CREATE INDEX IF NOT EXISTS idx_tds_section_rates_section ON tds_section_rates(section_code);
CREATE INDEX IF NOT EXISTS idx_tds_section_rates_effective ON tds_section_rates(effective_from, effective_to);

-- Add rule_pack_id reference to existing tables for tracking which rules were used

-- Add to payroll_runs if not exists
ALTER TABLE payroll_runs ADD COLUMN IF NOT EXISTS rule_pack_id UUID REFERENCES tax_rule_packs(id);
ALTER TABLE payroll_runs ADD COLUMN IF NOT EXISTS rules_snapshot JSONB;

-- Add to contractor_payments if not exists
ALTER TABLE contractor_payments ADD COLUMN IF NOT EXISTS rule_pack_id UUID REFERENCES tax_rule_packs(id);

-- Add to payments for TDS tracking
ALTER TABLE payments ADD COLUMN IF NOT EXISTS tds_rule_pack_id UUID REFERENCES tax_rule_packs(id);

-- Function to get active rule pack for a financial year
CREATE OR REPLACE FUNCTION get_active_rule_pack(p_financial_year VARCHAR(10))
RETURNS UUID AS $$
DECLARE
    v_pack_id UUID;
BEGIN
    SELECT id INTO v_pack_id
    FROM tax_rule_packs
    WHERE financial_year = p_financial_year
      AND status = 'active'
    ORDER BY version DESC
    LIMIT 1;

    RETURN v_pack_id;
END;
$$ LANGUAGE plpgsql;

-- Function to determine financial year from a date
-- Note: Using same parameter name as migration 024 to avoid PostgreSQL error
CREATE OR REPLACE FUNCTION get_financial_year(payment_date DATE)
RETURNS VARCHAR(10) AS $$
BEGIN
    IF EXTRACT(MONTH FROM payment_date) >= 4 THEN
        RETURN EXTRACT(YEAR FROM payment_date)::VARCHAR || '-' ||
               SUBSTRING((EXTRACT(YEAR FROM payment_date) + 1)::VARCHAR, 3, 2);
    ELSE
        RETURN (EXTRACT(YEAR FROM payment_date) - 1)::VARCHAR || '-' ||
               SUBSTRING(EXTRACT(YEAR FROM payment_date)::VARCHAR, 3, 2);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- View for easy access to current active rule packs
CREATE OR REPLACE VIEW v_active_rule_packs AS
SELECT
    trp.*,
    (SELECT COUNT(*) FROM rule_pack_usage_log WHERE rule_pack_id = trp.id) as usage_count
FROM tax_rule_packs trp
WHERE trp.status = 'active'
ORDER BY trp.financial_year DESC, trp.version DESC;

-- Comments for documentation
COMMENT ON TABLE tax_rule_packs IS 'Versioned tax rule configurations by financial year - enables tax updates without code changes';
COMMENT ON TABLE rule_pack_usage_log IS 'Audit trail of tax computations with rule snapshot for immutability';
COMMENT ON TABLE tds_section_rates IS 'Detailed TDS section-wise rates with thresholds';
COMMENT ON FUNCTION get_active_rule_pack IS 'Returns the active rule pack ID for a given financial year';
COMMENT ON FUNCTION get_financial_year IS 'Determines Indian financial year (Apr-Mar) from a date';
