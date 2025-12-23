-- Migration: RCM (Reverse Charge Mechanism) Transaction Table
-- Purpose: Track RCM transactions per GST Act Section 9(3), 9(4)
-- Reference: Notification 13/2017 - Central Tax (Rate)

-- ============================================
-- RCM Categories Master Table
-- Per Notification 13/2017 and subsequent amendments
-- ============================================

CREATE TABLE IF NOT EXISTS rcm_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_code VARCHAR(50) NOT NULL UNIQUE,
    category_name VARCHAR(255) NOT NULL,
    description TEXT,

    -- GST Section reference
    gst_section VARCHAR(20) NOT NULL, -- '9(3)', '9(4)', '5(3)', '5(4)'
    notification_ref VARCHAR(100),     -- 'Notification 13/2017'

    -- Default GST rates for this category
    default_gst_rate DECIMAL(5,2) NOT NULL,
    cgst_rate DECIMAL(5,2),
    sgst_rate DECIMAL(5,2),
    igst_rate DECIMAL(5,2),

    -- SAC/HSN codes (if applicable)
    sac_hsn_codes TEXT[],

    -- Supplier type (unregistered, specific registered)
    supplier_type VARCHAR(50), -- 'unregistered', 'registered', 'any'

    -- Validity
    effective_from DATE NOT NULL DEFAULT '2017-07-01',
    effective_to DATE,
    is_active BOOLEAN DEFAULT TRUE,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_rcm_categories_code ON rcm_categories(category_code);

-- ============================================
-- Seed RCM Categories (Per Notification 13/2017)
-- ============================================

INSERT INTO rcm_categories (category_code, category_name, description, gst_section, notification_ref,
    default_gst_rate, cgst_rate, sgst_rate, igst_rate, sac_hsn_codes, supplier_type, effective_from) VALUES

-- Legal Services (most common RCM)
('LEGAL', 'Legal Services', 'Legal services by advocate or firm of advocates',
 '9(3)', 'Notification 13/2017 Sr. 2', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998211', '998212'], 'any', '2017-07-01'),

-- Security Services (very common)
('SECURITY', 'Security Services', 'Security services by any person (other than body corporate)',
 '9(3)', 'Notification 13/2017 Sr. 14', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998529'], 'unregistered', '2017-07-01'),

-- GTA (Goods Transport Agency)
('GTA', 'Goods Transport Agency', 'Services by GTA in relation to transportation of goods',
 '9(3)', 'Notification 13/2017 Sr. 1', 5.00, 2.50, 2.50, 5.00,
 ARRAY['996511', '996512'], 'any', '2017-07-01'),

-- Import of Services
('IMPORT_SERVICE', 'Import of Services', 'Services received from outside India',
 '5(3)', 'IGST Act Section 5(3)', 18.00, NULL, NULL, 18.00,
 NULL, 'any', '2017-07-01'),

-- Recovery Agent
('RECOVERY_AGENT', 'Recovery Agent Services', 'Services by recovery agent to banking company/financial institution',
 '9(3)', 'Notification 13/2017 Sr. 8', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998596'], 'any', '2017-07-01'),

-- Director Services
('DIRECTOR', 'Director Services', 'Services by director to company (other than employee)',
 '9(3)', 'Notification 13/2017 Sr. 7', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998399'], 'any', '2017-07-01'),

-- Insurance Agent
('INSURANCE_AGENT', 'Insurance Agent Services', 'Services by insurance agent to insurance company',
 '9(3)', 'Notification 13/2017 Sr. 9', 18.00, 9.00, 9.00, 18.00,
 ARRAY['997113', '997114'], 'any', '2017-07-01'),

-- Author/Music Composer
('AUTHOR', 'Author/Music Composer Services', 'Services by author, music composer, photographer etc.',
 '9(3)', 'Notification 13/2017 Sr. 3', 18.00, 9.00, 9.00, 18.00,
 ARRAY['999611', '999612'], 'any', '2017-07-01'),

-- Sponsorship Services
('SPONSORSHIP', 'Sponsorship Services', 'Sponsorship services to any body corporate/firm',
 '9(3)', 'Notification 13/2017 Sr. 4', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998397'], 'any', '2017-07-01'),

-- Government Services (Renting)
('GOVT_RENT', 'Government Renting', 'Renting of immovable property by Government/local authority',
 '9(3)', 'Notification 13/2017 Sr. 5', 18.00, 9.00, 9.00, 18.00,
 ARRAY['997212'], 'any', '2017-07-01'),

-- Manpower Supply
('MANPOWER', 'Manpower Supply', 'Services by way of supply of manpower (security, cleaning)',
 '9(3)', 'Notification 29/2018 CT(R)', 18.00, 9.00, 9.00, 18.00,
 ARRAY['998519', '998529'], 'any', '2019-01-01'),

-- Unregistered Person (Generic)
('UNREGISTERED', 'Purchase from Unregistered Person', 'Any taxable supply from unregistered person above threshold',
 '9(4)', 'Section 9(4) CGST Act', 18.00, 9.00, 9.00, 18.00,
 NULL, 'unregistered', '2017-07-01')

ON CONFLICT (category_code) DO UPDATE SET
    category_name = EXCLUDED.category_name,
    description = EXCLUDED.description,
    default_gst_rate = EXCLUDED.default_gst_rate,
    updated_at = CURRENT_TIMESTAMP;

-- ============================================
-- RCM Transactions Table
-- ============================================

CREATE TABLE IF NOT EXISTS rcm_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Financial period
    financial_year VARCHAR(10) NOT NULL,  -- '2024-25'
    return_period VARCHAR(20) NOT NULL,    -- 'Jan-2025', 'Q4-2024'

    -- Source document
    source_type VARCHAR(50) NOT NULL,      -- 'expense_claim', 'vendor_invoice', 'manual'
    source_id UUID,
    source_number VARCHAR(100),

    -- Vendor/Supplier details
    vendor_name VARCHAR(255) NOT NULL,
    vendor_gstin VARCHAR(20),              -- Null for unregistered
    vendor_pan VARCHAR(20),
    vendor_state_code VARCHAR(5),
    vendor_invoice_number VARCHAR(100),
    vendor_invoice_date DATE,

    -- RCM Category
    rcm_category_id UUID REFERENCES rcm_categories(id),
    rcm_category_code VARCHAR(50) NOT NULL,
    rcm_notification VARCHAR(100),         -- Notification reference

    -- Supply details
    place_of_supply VARCHAR(5) NOT NULL,   -- State code
    supply_type VARCHAR(20) NOT NULL CHECK (supply_type IN ('intra_state', 'inter_state', 'import')),
    hsn_sac_code VARCHAR(20),
    description TEXT,

    -- Amounts
    taxable_value DECIMAL(18,2) NOT NULL,
    cgst_rate DECIMAL(5,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,
    sgst_rate DECIMAL(5,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,
    igst_rate DECIMAL(5,2) DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,
    cess_rate DECIMAL(5,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,
    total_rcm_tax DECIMAL(18,2) NOT NULL,

    -- RCM Liability Recognition (Stage 1)
    liability_recognized BOOLEAN DEFAULT FALSE,
    liability_recognized_at TIMESTAMP,
    liability_journal_id UUID REFERENCES journal_entries(id),

    -- RCM Payment Status (Stage 2)
    rcm_paid BOOLEAN DEFAULT FALSE,
    rcm_payment_date DATE,
    rcm_payment_journal_id UUID REFERENCES journal_entries(id),
    rcm_payment_reference VARCHAR(100),    -- Payment reference/UTR

    -- ITC Claim (After RCM payment)
    itc_eligible BOOLEAN DEFAULT TRUE,
    itc_claimed BOOLEAN DEFAULT FALSE,
    itc_claim_date DATE,
    itc_claim_journal_id UUID REFERENCES journal_entries(id),
    itc_claim_period VARCHAR(20),          -- Return period when claimed

    -- ITC Blocked (if not eligible)
    itc_blocked BOOLEAN DEFAULT FALSE,
    itc_blocked_reason VARCHAR(255),       -- Section 17(5) reference

    -- GSTR-3B Integration
    gstr3b_period VARCHAR(20),             -- Period in which reported
    gstr3b_table VARCHAR(20),              -- '3.1(d)' for RCM
    gstr3b_filed BOOLEAN DEFAULT FALSE,

    -- Status
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN (
        'pending',           -- RCM identified, liability not recognized
        'liability_created', -- Stage 1: Liability journal created
        'rcm_paid',         -- Stage 2: RCM tax paid
        'itc_claimed',      -- ITC claimed after payment
        'itc_blocked',      -- ITC not available (Section 17(5))
        'cancelled'
    )),

    -- Notes
    notes TEXT,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_rcm_trans_company ON rcm_transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_rcm_trans_period ON rcm_transactions(company_id, return_period);
CREATE INDEX IF NOT EXISTS idx_rcm_trans_status ON rcm_transactions(company_id, status);
CREATE INDEX IF NOT EXISTS idx_rcm_trans_source ON rcm_transactions(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_rcm_trans_vendor ON rcm_transactions(vendor_gstin);
CREATE INDEX IF NOT EXISTS idx_rcm_trans_category ON rcm_transactions(rcm_category_code);

-- Trigger to update timestamp
CREATE OR REPLACE FUNCTION update_rcm_transaction_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS rcm_transaction_update ON rcm_transactions;
CREATE TRIGGER rcm_transaction_update
    BEFORE UPDATE ON rcm_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_rcm_transaction_timestamp();

-- ============================================
-- View: Pending RCM Payments
-- ============================================

CREATE OR REPLACE VIEW v_pending_rcm_payments AS
SELECT
    r.id,
    r.company_id,
    r.return_period,
    r.vendor_name,
    r.vendor_gstin,
    r.rcm_category_code,
    c.category_name as rcm_category_name,
    r.taxable_value,
    r.cgst_amount,
    r.sgst_amount,
    r.igst_amount,
    r.total_rcm_tax,
    r.status,
    r.vendor_invoice_number,
    r.vendor_invoice_date,
    r.liability_recognized_at,
    CASE
        WHEN r.status = 'pending' THEN 'Liability not created'
        WHEN r.status = 'liability_created' THEN 'Awaiting RCM payment'
        WHEN r.status = 'rcm_paid' THEN 'Awaiting ITC claim'
        ELSE r.status
    END as status_description
FROM rcm_transactions r
LEFT JOIN rcm_categories c ON r.rcm_category_code = c.category_code
WHERE r.status NOT IN ('itc_claimed', 'itc_blocked', 'cancelled');

-- ============================================
-- View: RCM Summary by Period
-- ============================================

CREATE OR REPLACE VIEW v_rcm_period_summary AS
SELECT
    company_id,
    return_period,
    rcm_category_code,
    COUNT(*) as transaction_count,
    SUM(taxable_value) as total_taxable_value,
    SUM(cgst_amount) as total_cgst,
    SUM(sgst_amount) as total_sgst,
    SUM(igst_amount) as total_igst,
    SUM(total_rcm_tax) as total_rcm_tax,
    SUM(CASE WHEN rcm_paid THEN total_rcm_tax ELSE 0 END) as rcm_paid_amount,
    SUM(CASE WHEN NOT rcm_paid AND status != 'cancelled' THEN total_rcm_tax ELSE 0 END) as rcm_pending_amount,
    SUM(CASE WHEN itc_claimed THEN total_rcm_tax ELSE 0 END) as itc_claimed_amount
FROM rcm_transactions
WHERE status != 'cancelled'
GROUP BY company_id, return_period, rcm_category_code;

-- ============================================
-- Function: Check if expense requires RCM
-- ============================================

CREATE OR REPLACE FUNCTION check_rcm_applicability(
    p_supplier_gstin VARCHAR(20),
    p_service_type VARCHAR(50),
    p_company_state VARCHAR(5),
    p_supplier_state VARCHAR(5)
)
RETURNS TABLE (
    is_rcm_applicable BOOLEAN,
    rcm_category_code VARCHAR(50),
    rcm_category_name VARCHAR(255),
    gst_rate DECIMAL(5,2),
    cgst_rate DECIMAL(5,2),
    sgst_rate DECIMAL(5,2),
    igst_rate DECIMAL(5,2),
    notification_ref VARCHAR(100)
) AS $$
BEGIN
    -- Check if RCM applies based on service type and supplier registration
    RETURN QUERY
    SELECT
        TRUE as is_rcm_applicable,
        c.category_code as rcm_category_code,
        c.category_name as rcm_category_name,
        c.default_gst_rate as gst_rate,
        CASE WHEN p_company_state = p_supplier_state THEN c.cgst_rate ELSE NULL END as cgst_rate,
        CASE WHEN p_company_state = p_supplier_state THEN c.sgst_rate ELSE NULL END as sgst_rate,
        CASE WHEN p_company_state != p_supplier_state OR p_supplier_state IS NULL THEN c.igst_rate ELSE NULL END as igst_rate,
        c.notification_ref
    FROM rcm_categories c
    WHERE c.is_active = TRUE
      AND c.category_code = p_service_type
      AND (c.supplier_type = 'any'
           OR (c.supplier_type = 'unregistered' AND (p_supplier_gstin IS NULL OR p_supplier_gstin = ''))
           OR (c.supplier_type = 'registered' AND p_supplier_gstin IS NOT NULL))
      AND CURRENT_DATE BETWEEN c.effective_from AND COALESCE(c.effective_to, '9999-12-31');
END;
$$ LANGUAGE plpgsql;

COMMENT ON TABLE rcm_transactions IS 'RCM transactions tracking with two-stage journal model per GST Act Section 9(3), 9(4)';
COMMENT ON TABLE rcm_categories IS 'RCM categories per Notification 13/2017 - Central Tax (Rate)';
COMMENT ON VIEW v_pending_rcm_payments IS 'RCM transactions pending payment or ITC claim';
COMMENT ON VIEW v_rcm_period_summary IS 'RCM summary by return period for GSTR-3B filing';
