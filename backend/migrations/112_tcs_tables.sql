-- Migration: TCS (Tax Collected at Source) Tables and Accounts
-- Purpose: Implement TCS per Section 206C of Income Tax Act
-- Reference: Finance Act 2020, Section 206C(1H) for sale > 50L

-- ============================================
-- Function to add TCS accounts
-- ============================================

CREATE OR REPLACE FUNCTION add_tcs_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER := 0;
    v_current_assets_parent_id UUID;
    v_statutory_dues_parent_id UUID;
BEGIN
    -- Get parent account IDs
    SELECT id INTO v_current_assets_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '1000';

    SELECT id INTO v_statutory_dues_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '2200';

    -- ========================================
    -- TCS RECEIVABLE (Asset - when we pay and TCS is collected from us)
    -- ========================================

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '1160', 'TCS Receivable', 'asset', 'current_asset',
        v_current_assets_parent_id, 1, 'debit', 1160, 'II(A)(c)',
        'Tax Collected at Source receivable - claimable while filing ITR', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1160'),
           2, 'debit', ord, 'II(A)(c)', descr, p_created_by
    FROM (VALUES
        ('1161', 'TCS Receivable - 206C(1H) Sale', 1161, 'TCS on purchase from seller with turnover > 10Cr'),
        ('1162', 'TCS Receivable - 206C(1G) Foreign', 1162, 'TCS on foreign remittance/tour package'),
        ('1163', 'TCS Receivable - 206C(1F) Motor Vehicle', 1163, 'TCS on motor vehicle purchase > 10L')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 3;

    -- ========================================
    -- TCS PAYABLE (Liability - when we sell and must collect TCS)
    -- ========================================

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '2280', 'TCS Payable', 'liability', 'current_liability',
        v_statutory_dues_parent_id, 2, 'credit', 2280, 'II(c)',
        'Tax Collected at Source payable to government', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2280'),
           3, 'credit', ord, 'II(c)', descr, p_created_by
    FROM (VALUES
        ('2281', 'TCS Payable - 206C(1) Scrap', 2281, 'TCS on sale of scrap - 1%'),
        ('2282', 'TCS Payable - 206C(1H) Sale > 50L', 2282, 'TCS on sale of goods > 50L per buyer - 0.1%'),
        ('2283', 'TCS Payable - 206C(1F) Motor Vehicle', 2283, 'TCS on sale of motor vehicle > 10L - 1%'),
        ('2284', 'TCS Payable - 206C(1G) Foreign Remittance', 2284, 'TCS on LRS remittance - 5%/20%'),
        ('2285', 'TCS Payable - 206C(1G) Tour Package', 2285, 'TCS on overseas tour package - 5%/20%'),
        ('2286', 'TCS Payable - 206C Liquor', 2286, 'TCS on sale of liquor for human consumption - 1%'),
        ('2287', 'TCS Payable - 206C Forest Produce', 2287, 'TCS on sale of timber, tendu leaves, etc. - 2.5%')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 7;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Apply TCS accounts to all existing companies
-- ============================================
DO $$
DECLARE
    company_record RECORD;
    accounts_added INTEGER;
BEGIN
    FOR company_record IN SELECT id FROM companies LOOP
        SELECT add_tcs_accounts(company_record.id) INTO accounts_added;
        RAISE NOTICE 'Added % TCS accounts to company %', accounts_added, company_record.id;
    END LOOP;
END $$;

-- ============================================
-- TCS Sections Reference Table
-- ============================================

CREATE TABLE IF NOT EXISTS tcs_sections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    section_code VARCHAR(30) NOT NULL UNIQUE,
    section_name VARCHAR(255) NOT NULL,
    description TEXT,

    -- Rates
    default_rate DECIMAL(5,2) NOT NULL,
    rate_without_pan DECIMAL(5,2), -- Higher rate if collectee has no PAN
    rate_non_filer DECIMAL(5,2),   -- Rate for non-ITR filers

    -- Thresholds
    threshold_per_transaction DECIMAL(18,2),
    threshold_annual DECIMAL(18,2),

    -- Applicable goods/services
    applicable_goods TEXT[], -- Array of applicable goods/services
    hsn_codes TEXT[],        -- Applicable HSN codes

    -- Account mapping
    payable_account_code VARCHAR(10),
    receivable_account_code VARCHAR(10),

    -- Validity
    effective_from DATE NOT NULL DEFAULT '2024-04-01',
    effective_to DATE,
    is_active BOOLEAN DEFAULT TRUE,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_tcs_sections_code ON tcs_sections(section_code);

-- ============================================
-- Seed TCS Sections (FY 2024-25 Rates)
-- ============================================

INSERT INTO tcs_sections (section_code, section_name, description, default_rate, rate_without_pan,
    threshold_annual, applicable_goods, payable_account_code, receivable_account_code, effective_from) VALUES

-- Sale of goods > 50L (most common TCS)
('206C(1H)', 'Sale of Goods > 50L', 'TCS on sale of goods where receipt exceeds 50L from a buyer',
 0.10, 1.00, 5000000.00, ARRAY['All goods'], '2282', '1161', '2024-04-01'),

-- Sale of scrap
('206C(1)', 'Sale of Scrap', 'TCS on sale of scrap',
 1.00, 5.00, NULL, ARRAY['Scrap', 'Waste'], '2281', '1160', '2024-04-01'),

-- Motor vehicle sale
('206C(1F)', 'Motor Vehicle Sale', 'TCS on sale of motor vehicle > 10L',
 1.00, 5.00, 1000000.00, ARRAY['Motor vehicles', 'Cars', 'Two-wheelers'], '2283', '1163', '2024-04-01'),

-- Foreign remittance (LRS)
('206C(1G)_REMIT', 'Foreign Remittance', 'TCS on remittance under LRS scheme',
 5.00, 10.00, 700000.00, ARRAY['Foreign remittance', 'LRS'], '2284', '1162', '2024-04-01'),

-- Overseas tour package
('206C(1G)_TOUR', 'Overseas Tour Package', 'TCS on overseas tour package',
 5.00, 10.00, NULL, ARRAY['Tour package', 'International travel'], '2285', '1162', '2024-04-01'),

-- Liquor
('206C(1)(i)', 'Liquor Sale', 'TCS on sale of liquor for human consumption',
 1.00, 5.00, NULL, ARRAY['Liquor', 'Alcoholic beverages'], '2286', '1160', '2024-04-01'),

-- Forest produce
('206C(1)(ii)', 'Forest Produce', 'TCS on sale of timber, tendu leaves, forest produce',
 2.50, 5.00, NULL, ARRAY['Timber', 'Tendu leaves', 'Forest produce'], '2287', '1160', '2024-04-01')

ON CONFLICT (section_code) DO UPDATE SET
    section_name = EXCLUDED.section_name,
    default_rate = EXCLUDED.default_rate,
    threshold_annual = EXCLUDED.threshold_annual,
    updated_at = CURRENT_TIMESTAMP;

-- ============================================
-- TCS Transactions Table
-- ============================================

CREATE TABLE IF NOT EXISTS tcs_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Transaction type
    transaction_type VARCHAR(20) NOT NULL CHECK (transaction_type IN ('collected', 'paid')),
    -- 'collected': When we sell and collect TCS
    -- 'paid': When we buy and TCS is collected from us

    -- TCS Section
    section_code VARCHAR(30) NOT NULL,
    section_id UUID REFERENCES tcs_sections(id),

    -- Date and Period
    transaction_date DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL,  -- '2024-25'
    quarter VARCHAR(5) NOT NULL,          -- 'Q1', 'Q2', 'Q3', 'Q4'

    -- Party details (collectee/collector)
    party_type VARCHAR(20) NOT NULL CHECK (party_type IN ('customer', 'vendor')),
    party_id UUID,
    party_name VARCHAR(255) NOT NULL,
    party_pan VARCHAR(20),
    party_gstin VARCHAR(20),

    -- Amounts
    transaction_value DECIMAL(18,2) NOT NULL,
    tcs_rate DECIMAL(5,2) NOT NULL,
    tcs_amount DECIMAL(18,2) NOT NULL,

    -- Cumulative tracking (for 50L threshold)
    cumulative_value_fy DECIMAL(18,2), -- Total value from this party in FY
    threshold_amount DECIMAL(18,2),     -- Applicable threshold

    -- Linked documents
    invoice_id UUID,
    payment_id UUID,
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Status and Remittance
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'collected', 'remitted', 'filed', 'cancelled')),
    collected_at TIMESTAMP,
    remitted_at TIMESTAMP,
    challan_number VARCHAR(50),
    bsr_code VARCHAR(10),

    -- Form 27EQ tracking
    form_27eq_quarter VARCHAR(20),
    form_27eq_filed BOOLEAN DEFAULT FALSE,
    form_27eq_acknowledgement VARCHAR(100),

    -- Notes
    notes TEXT,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID
);

-- Indexes for TCS transactions
CREATE INDEX IF NOT EXISTS idx_tcs_trans_company ON tcs_transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_tcs_trans_party ON tcs_transactions(party_pan, financial_year);
CREATE INDEX IF NOT EXISTS idx_tcs_trans_quarter ON tcs_transactions(company_id, financial_year, quarter);
CREATE INDEX IF NOT EXISTS idx_tcs_trans_status ON tcs_transactions(company_id, status);
CREATE INDEX IF NOT EXISTS idx_tcs_trans_invoice ON tcs_transactions(invoice_id);
CREATE INDEX IF NOT EXISTS idx_tcs_trans_payment ON tcs_transactions(payment_id);

-- Trigger to update timestamp
CREATE OR REPLACE FUNCTION update_tcs_transaction_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS tcs_transaction_update ON tcs_transactions;
CREATE TRIGGER tcs_transaction_update
    BEFORE UPDATE ON tcs_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_tcs_transaction_timestamp();

-- ============================================
-- View: TCS Summary by Quarter
-- ============================================

CREATE OR REPLACE VIEW v_tcs_quarterly_summary AS
SELECT
    company_id,
    financial_year,
    quarter,
    transaction_type,
    section_code,
    COUNT(*) as transaction_count,
    SUM(transaction_value) as total_transaction_value,
    SUM(tcs_amount) as total_tcs_amount,
    SUM(CASE WHEN status = 'remitted' THEN tcs_amount ELSE 0 END) as tcs_remitted,
    SUM(CASE WHEN status = 'pending' THEN tcs_amount ELSE 0 END) as tcs_pending
FROM tcs_transactions
WHERE status != 'cancelled'
GROUP BY company_id, financial_year, quarter, transaction_type, section_code;

COMMENT ON TABLE tcs_transactions IS 'TCS transactions tracking - both collected (on sales) and paid (on purchases)';
COMMENT ON VIEW v_tcs_quarterly_summary IS 'Quarterly TCS summary for Form 27EQ filing';
COMMENT ON FUNCTION add_tcs_accounts IS 'Adds TCS related accounts per Section 206C';
