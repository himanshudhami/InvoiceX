-- Migration: Lower Deduction Certificates (Form 13)
-- Purpose: Track TDS Lower/NIL deduction certificates per Section 197
-- Reference: Income Tax Act Section 197, Rule 28AA

-- ============================================
-- Lower Deduction Certificates Table
-- Form 13 certificates for reduced TDS rates
-- ============================================

CREATE TABLE IF NOT EXISTS lower_deduction_certificates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Certificate details
    certificate_number VARCHAR(100) NOT NULL,
    certificate_date DATE NOT NULL,
    valid_from DATE NOT NULL,
    valid_to DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL,   -- '2024-25'

    -- Certificate type
    certificate_type VARCHAR(20) NOT NULL CHECK (certificate_type IN ('lower', 'nil')),
    -- 'lower': Reduced rate certificate
    -- 'nil': No deduction certificate

    -- Deductee details (the vendor/contractor who has the certificate)
    deductee_type VARCHAR(20) NOT NULL CHECK (deductee_type IN ('vendor', 'contractor', 'landlord', 'other')),
    deductee_id UUID,                      -- Reference to vendors/contractors table
    deductee_name VARCHAR(255) NOT NULL,
    deductee_pan VARCHAR(20) NOT NULL,
    deductee_address TEXT,

    -- TDS Section details
    tds_section VARCHAR(20) NOT NULL,      -- '194C', '194J', '194I', etc.
    normal_rate DECIMAL(5,2) NOT NULL,     -- Standard TDS rate
    certificate_rate DECIMAL(5,2) NOT NULL, -- Rate as per certificate (0 for NIL)

    -- Limits (if any)
    threshold_amount DECIMAL(18,2),        -- Maximum amount covered
    utilized_amount DECIMAL(18,2) DEFAULT 0, -- Amount already utilized

    -- Issuing authority
    assessing_officer VARCHAR(255),
    ao_designation VARCHAR(255),
    ao_office_address TEXT,

    -- Document storage
    certificate_document_id UUID,          -- Reference to attachments

    -- Status
    status VARCHAR(20) DEFAULT 'active' CHECK (status IN ('active', 'expired', 'revoked', 'exhausted')),
    revoked_at TIMESTAMP,
    revocation_reason TEXT,

    -- Notes
    notes TEXT,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID,

    -- Constraints
    CONSTRAINT chk_valid_dates CHECK (valid_to >= valid_from),
    CONSTRAINT chk_certificate_rate CHECK (
        (certificate_type = 'nil' AND certificate_rate = 0) OR
        (certificate_type = 'lower' AND certificate_rate < normal_rate)
    )
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_ldc_company ON lower_deduction_certificates(company_id);
CREATE INDEX IF NOT EXISTS idx_ldc_pan ON lower_deduction_certificates(deductee_pan);
CREATE INDEX IF NOT EXISTS idx_ldc_section ON lower_deduction_certificates(tds_section);
CREATE INDEX IF NOT EXISTS idx_ldc_validity ON lower_deduction_certificates(valid_from, valid_to, status);
CREATE INDEX IF NOT EXISTS idx_ldc_deductee ON lower_deduction_certificates(deductee_id);

-- Unique constraint: One active certificate per PAN per section per FY
CREATE UNIQUE INDEX IF NOT EXISTS idx_ldc_unique_active
ON lower_deduction_certificates(company_id, deductee_pan, tds_section, financial_year)
WHERE status = 'active';

-- Trigger to update timestamp
CREATE OR REPLACE FUNCTION update_ldc_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;

    -- Auto-update status based on validity and utilization
    IF NEW.status = 'active' THEN
        IF CURRENT_DATE > NEW.valid_to THEN
            NEW.status = 'expired';
        ELSIF NEW.threshold_amount IS NOT NULL AND NEW.utilized_amount >= NEW.threshold_amount THEN
            NEW.status = 'exhausted';
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS ldc_update ON lower_deduction_certificates;
CREATE TRIGGER ldc_update
    BEFORE UPDATE ON lower_deduction_certificates
    FOR EACH ROW
    EXECUTE FUNCTION update_ldc_timestamp();

-- ============================================
-- LDC Usage Log (track certificate utilization)
-- ============================================

CREATE TABLE IF NOT EXISTS ldc_usage_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    certificate_id UUID NOT NULL REFERENCES lower_deduction_certificates(id),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Transaction details
    transaction_date DATE NOT NULL,
    transaction_type VARCHAR(50) NOT NULL,  -- 'contractor_payment', 'rent_payment', etc.
    transaction_id UUID,
    transaction_number VARCHAR(100),

    -- Amounts
    gross_amount DECIMAL(18,2) NOT NULL,
    normal_tds_amount DECIMAL(18,2) NOT NULL,    -- TDS at normal rate
    actual_tds_amount DECIMAL(18,2) NOT NULL,    -- TDS at certificate rate
    tds_savings DECIMAL(18,2) NOT NULL,          -- Difference (savings)

    -- Running totals
    cumulative_utilized DECIMAL(18,2) NOT NULL,  -- Total utilized after this transaction
    remaining_threshold DECIMAL(18,2),            -- Remaining if threshold exists

    -- Notes
    notes TEXT,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID
);

CREATE INDEX IF NOT EXISTS idx_ldc_usage_cert ON ldc_usage_log(certificate_id);
CREATE INDEX IF NOT EXISTS idx_ldc_usage_trans ON ldc_usage_log(transaction_type, transaction_id);

-- ============================================
-- Function: Get valid certificate for deductee
-- ============================================

CREATE OR REPLACE FUNCTION get_valid_ldc(
    p_company_id UUID,
    p_deductee_pan VARCHAR(20),
    p_tds_section VARCHAR(20),
    p_transaction_date DATE,
    p_amount DECIMAL(18,2) DEFAULT NULL
)
RETURNS TABLE (
    certificate_id UUID,
    certificate_number VARCHAR(100),
    certificate_type VARCHAR(20),
    normal_rate DECIMAL(5,2),
    certificate_rate DECIMAL(5,2),
    remaining_threshold DECIMAL(18,2),
    is_valid BOOLEAN,
    validation_message VARCHAR(255)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        ldc.id as certificate_id,
        ldc.certificate_number,
        ldc.certificate_type,
        ldc.normal_rate,
        ldc.certificate_rate,
        CASE
            WHEN ldc.threshold_amount IS NOT NULL THEN ldc.threshold_amount - ldc.utilized_amount
            ELSE NULL
        END as remaining_threshold,
        CASE
            WHEN ldc.status != 'active' THEN FALSE
            WHEN p_transaction_date < ldc.valid_from THEN FALSE
            WHEN p_transaction_date > ldc.valid_to THEN FALSE
            WHEN ldc.threshold_amount IS NOT NULL AND ldc.utilized_amount >= ldc.threshold_amount THEN FALSE
            WHEN p_amount IS NOT NULL AND ldc.threshold_amount IS NOT NULL
                 AND ldc.utilized_amount + p_amount > ldc.threshold_amount THEN FALSE
            ELSE TRUE
        END as is_valid,
        CASE
            WHEN ldc.status != 'active' THEN 'Certificate is not active (status: ' || ldc.status || ')'
            WHEN p_transaction_date < ldc.valid_from THEN 'Transaction date is before certificate validity'
            WHEN p_transaction_date > ldc.valid_to THEN 'Certificate has expired'
            WHEN ldc.threshold_amount IS NOT NULL AND ldc.utilized_amount >= ldc.threshold_amount
                THEN 'Certificate threshold exhausted'
            WHEN p_amount IS NOT NULL AND ldc.threshold_amount IS NOT NULL
                 AND ldc.utilized_amount + p_amount > ldc.threshold_amount
                THEN 'Amount exceeds remaining threshold'
            ELSE 'Valid'
        END as validation_message
    FROM lower_deduction_certificates ldc
    WHERE ldc.company_id = p_company_id
      AND ldc.deductee_pan = p_deductee_pan
      AND ldc.tds_section = p_tds_section
      AND ldc.status = 'active'
    ORDER BY ldc.valid_from DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Function: Record LDC usage
-- ============================================

CREATE OR REPLACE FUNCTION record_ldc_usage(
    p_certificate_id UUID,
    p_company_id UUID,
    p_transaction_date DATE,
    p_transaction_type VARCHAR(50),
    p_transaction_id UUID,
    p_transaction_number VARCHAR(100),
    p_gross_amount DECIMAL(18,2),
    p_normal_tds_rate DECIMAL(5,2),
    p_actual_tds_rate DECIMAL(5,2),
    p_created_by UUID DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    v_log_id UUID;
    v_normal_tds DECIMAL(18,2);
    v_actual_tds DECIMAL(18,2);
    v_savings DECIMAL(18,2);
    v_new_utilized DECIMAL(18,2);
    v_threshold DECIMAL(18,2);
BEGIN
    -- Calculate TDS amounts
    v_normal_tds := ROUND(p_gross_amount * p_normal_tds_rate / 100, 0);
    v_actual_tds := ROUND(p_gross_amount * p_actual_tds_rate / 100, 0);
    v_savings := v_normal_tds - v_actual_tds;

    -- Update certificate utilized amount
    UPDATE lower_deduction_certificates
    SET utilized_amount = utilized_amount + p_gross_amount
    WHERE id = p_certificate_id
    RETURNING utilized_amount, threshold_amount INTO v_new_utilized, v_threshold;

    -- Insert usage log
    INSERT INTO ldc_usage_log (
        certificate_id, company_id, transaction_date, transaction_type,
        transaction_id, transaction_number, gross_amount,
        normal_tds_amount, actual_tds_amount, tds_savings,
        cumulative_utilized, remaining_threshold, created_by
    ) VALUES (
        p_certificate_id, p_company_id, p_transaction_date, p_transaction_type,
        p_transaction_id, p_transaction_number, p_gross_amount,
        v_normal_tds, v_actual_tds, v_savings,
        v_new_utilized,
        CASE WHEN v_threshold IS NOT NULL THEN v_threshold - v_new_utilized ELSE NULL END,
        p_created_by
    )
    RETURNING id INTO v_log_id;

    RETURN v_log_id;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- View: Active certificates with utilization
-- ============================================

CREATE OR REPLACE VIEW v_active_ldc_summary AS
SELECT
    ldc.id,
    ldc.company_id,
    ldc.certificate_number,
    ldc.certificate_type,
    ldc.deductee_name,
    ldc.deductee_pan,
    ldc.tds_section,
    ldc.normal_rate,
    ldc.certificate_rate,
    ldc.valid_from,
    ldc.valid_to,
    ldc.threshold_amount,
    ldc.utilized_amount,
    CASE
        WHEN ldc.threshold_amount IS NOT NULL THEN ldc.threshold_amount - ldc.utilized_amount
        ELSE NULL
    END as remaining_threshold,
    CASE
        WHEN ldc.threshold_amount IS NOT NULL
        THEN ROUND((ldc.utilized_amount / ldc.threshold_amount) * 100, 2)
        ELSE NULL
    END as utilization_percentage,
    CURRENT_DATE <= ldc.valid_to as is_not_expired,
    (ldc.valid_to - CURRENT_DATE) as days_remaining,
    ldc.status
FROM lower_deduction_certificates ldc
WHERE ldc.status = 'active';

COMMENT ON TABLE lower_deduction_certificates IS 'TDS Lower/NIL deduction certificates per Section 197 (Form 13)';
COMMENT ON TABLE ldc_usage_log IS 'Usage log for lower deduction certificates tracking utilization';
COMMENT ON FUNCTION get_valid_ldc IS 'Get valid lower deduction certificate for a deductee/section/date';
COMMENT ON FUNCTION record_ldc_usage IS 'Record usage of a lower deduction certificate';
