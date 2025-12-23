-- Migration: TDS Comprehensive Accounts
-- Purpose: Add all TDS section accounts per Income Tax Act FY 2024-25
-- Reference: Income Tax Act Sections 192-206C

-- ============================================
-- Function to add comprehensive TDS accounts
-- ============================================

CREATE OR REPLACE FUNCTION add_tds_comprehensive_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER := 0;
    v_tds_receivable_parent_id UUID;
    v_statutory_dues_parent_id UUID;
BEGIN
    -- Get parent account IDs
    SELECT id INTO v_tds_receivable_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '1130';

    SELECT id INTO v_statutory_dues_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '2200';

    -- ========================================
    -- TDS RECEIVABLE ACCOUNTS (Assets 113x)
    -- When customer/payer deducts TDS from our payments
    -- ========================================

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           v_tds_receivable_parent_id, 2, 'debit', ord, 'II(A)(c)', descr, p_created_by
    FROM (VALUES
        ('1135', 'TDS Receivable - 194A Interest', 1135, 'TDS deducted on interest income - Section 194A (10%)'),
        ('1136', 'TDS Receivable - 194I Rent', 1136, 'TDS deducted on rent income - Section 194I (2%/10%)'),
        ('1137', 'TDS Receivable - 194IB', 1137, 'TDS deducted on rent by individual/HUF - Section 194IB (5%)'),
        ('1138', 'TDS Receivable - 195 Non-Resident', 1138, 'TDS deducted on payments to non-residents - Section 195')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 4;

    -- ========================================
    -- TDS PAYABLE ACCOUNTS (Liabilities 22xx)
    -- When we deduct TDS from vendor/contractor payments
    -- ========================================

    -- Note: 2212 (TDS Salary), 2213 (194J), 2214 (194C) already exist

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           v_statutory_dues_parent_id, 2, 'credit', ord, 'II(c)', descr, p_created_by
    FROM (VALUES
        -- Interest TDS
        ('2215', 'TDS Payable - 194A Interest', 2215, 'TDS on interest paid (FD, loan interest) - 10%'),

        -- Rent TDS - Split by type
        ('2216', 'TDS Payable - 194I(a) Rent Plant/Machinery', 2216, 'TDS on rent for plant, machinery, equipment - 2%'),
        ('2217', 'TDS Payable - 194I(b) Rent Land/Building', 2217, 'TDS on rent for land, building, furniture - 10%'),
        ('2218', 'TDS Payable - 194IB Rent by Individual', 2218, 'TDS on rent paid by individual/HUF > 50K/month - 5%'),

        -- Contractor TDS - Individual payers
        ('2219', 'TDS Payable - 194M Contractor Individual', 2219, 'TDS by individual/HUF on contractor > 50L/year - 5%'),

        -- Special TDS sections
        ('2220', 'TDS Payable - 194B Lottery/Gambling', 2220, 'TDS on lottery, crossword, gambling winnings - 30%'),
        ('2221', 'TDS Payable - 194D Insurance Commission', 2221, 'TDS on insurance commission - 5%'),
        ('2222', 'TDS Payable - 194E Non-Resident Sports', 2222, 'TDS on payments to non-resident sportsperson - 20%'),

        -- Cash withdrawal TDS
        ('2223', 'TDS Payable - 194N Cash Withdrawal', 2223, 'TDS on cash withdrawal > 1Cr (2%) or > 20L non-filer (2%)'),

        -- E-commerce TDS
        ('2224', 'TDS Payable - 194O E-commerce', 2224, 'TDS by e-commerce operator on seller payments - 1%'),

        -- Purchase TDS
        ('2225', 'TDS Payable - 194Q Purchase of Goods', 2225, 'TDS on purchase of goods > 50L - 0.1%'),

        -- Non-resident TDS
        ('2226', 'TDS Payable - 195 Non-Resident', 2226, 'TDS on payments to non-residents - varies by DTAA'),

        -- Commission/Brokerage
        ('2227', 'TDS Payable - 194H Commission', 2227, 'TDS on commission/brokerage - 5%'),

        -- Property transaction TDS
        ('2228', 'TDS Payable - 194IA Property', 2228, 'TDS on purchase of immovable property > 50L - 1%'),

        -- Perquisites TDS
        ('2229', 'TDS Payable - 194R Perquisites', 2229, 'TDS on business perquisites/benefits - 10%'),

        -- Crypto/VDA TDS
        ('2230', 'TDS Payable - 194S Crypto/VDA', 2230, 'TDS on transfer of virtual digital assets - 1%')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 16;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Apply to all existing companies
-- ============================================
DO $$
DECLARE
    company_record RECORD;
    accounts_added INTEGER;
BEGIN
    FOR company_record IN SELECT id FROM companies LOOP
        SELECT add_tds_comprehensive_accounts(company_record.id) INTO accounts_added;
        RAISE NOTICE 'Added % TDS comprehensive accounts to company %', accounts_added, company_record.id;
    END LOOP;
END $$;

-- ============================================
-- TDS Section Reference Table (Lookup)
-- ============================================

CREATE TABLE IF NOT EXISTS tds_sections (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    section_code VARCHAR(20) NOT NULL UNIQUE,
    section_name VARCHAR(255) NOT NULL,
    description TEXT,

    -- Rates
    default_rate DECIMAL(5,2) NOT NULL,
    individual_rate DECIMAL(5,2),           -- For sections with different rates
    company_rate DECIMAL(5,2),              -- For sections with different rates
    no_pan_rate DECIMAL(5,2) DEFAULT 20.00, -- Section 206AA penalty rate

    -- Thresholds
    threshold_per_transaction DECIMAL(18,2),
    threshold_annual DECIMAL(18,2),

    -- Applicability
    applicable_to VARCHAR(50)[], -- ['individual', 'company', 'firm', 'aop', 'trust']
    deductor_type VARCHAR(50)[], -- ['company', 'individual', 'government']

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

-- Create index for lookups
CREATE INDEX IF NOT EXISTS idx_tds_sections_code ON tds_sections(section_code);
CREATE INDEX IF NOT EXISTS idx_tds_sections_active ON tds_sections(is_active, effective_from, effective_to);

-- ============================================
-- Seed TDS Sections (FY 2024-25 Rates)
-- ============================================

INSERT INTO tds_sections (section_code, section_name, description, default_rate, individual_rate, company_rate,
    threshold_per_transaction, threshold_annual, applicable_to, deductor_type,
    payable_account_code, receivable_account_code, effective_from) VALUES

-- Salary TDS
('192', 'Salary', 'TDS on salary income', 0.00, NULL, NULL, NULL, NULL,
 ARRAY['individual'], ARRAY['company', 'individual', 'government'], '2212', NULL, '2024-04-01'),

-- Interest TDS
('194A', 'Interest other than securities', 'TDS on interest from banks, deposits, loans', 10.00, NULL, NULL,
 NULL, 40000.00, ARRAY['individual', 'company', 'firm', 'aop'], ARRAY['company', 'individual'],
 '2215', '1135', '2024-04-01'),

-- Contractor TDS
('194C', 'Contractor/Sub-contractor', 'TDS on contractor payments', 1.00, 1.00, 2.00,
 30000.00, 100000.00, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual', 'government'],
 '2214', '1132', '2024-04-01'),

-- Commission TDS
('194H', 'Commission/Brokerage', 'TDS on commission and brokerage', 5.00, NULL, NULL,
 15000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['company'],
 '2227', '1133', '2024-04-01'),

-- Rent TDS - Plant/Machinery
('194I_A', 'Rent - Plant/Machinery', 'TDS on rent of plant, machinery, equipment', 2.00, NULL, NULL,
 NULL, 240000.00, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual', 'government'],
 '2216', '1136', '2024-04-01'),

-- Rent TDS - Land/Building
('194I_B', 'Rent - Land/Building', 'TDS on rent of land, building, furniture', 10.00, NULL, NULL,
 NULL, 240000.00, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual', 'government'],
 '2217', '1136', '2024-04-01'),

-- Rent by Individual
('194IB', 'Rent by Individual/HUF', 'TDS on rent paid by individual/HUF exceeding 50K/month', 5.00, NULL, NULL,
 50000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['individual'],
 '2218', '1137', '2024-04-01'),

-- Professional/Technical fees
('194J', 'Professional/Technical Services', 'TDS on professional and technical services', 10.00, NULL, NULL,
 30000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual', 'government'],
 '2213', '1131', '2024-04-01'),

-- Contractor by Individual
('194M', 'Payment by Individual/HUF', 'TDS on contractor payment by individual/HUF', 5.00, NULL, NULL,
 NULL, 5000000.00, ARRAY['individual', 'company', 'firm'], ARRAY['individual'],
 '2219', NULL, '2024-04-01'),

-- Cash Withdrawal
('194N', 'Cash Withdrawal', 'TDS on cash withdrawal exceeding thresholds', 2.00, NULL, NULL,
 10000000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['company'],
 '2223', NULL, '2024-04-01'),

-- E-commerce
('194O', 'E-commerce Operator', 'TDS by e-commerce operator on seller payments', 1.00, NULL, NULL,
 500000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['company'],
 '2224', NULL, '2024-04-01'),

-- Purchase of Goods
('194Q', 'Purchase of Goods', 'TDS on purchase of goods exceeding 50L', 0.10, NULL, NULL,
 NULL, 5000000.00, ARRAY['individual', 'company', 'firm'], ARRAY['company'],
 '2225', NULL, '2024-04-01'),

-- Non-Resident
('195', 'Non-Resident', 'TDS on payments to non-residents', 20.00, NULL, NULL,
 NULL, NULL, ARRAY['non_resident'], ARRAY['company', 'individual', 'government'],
 '2226', '1138', '2024-04-01'),

-- Property Purchase
('194IA', 'Immovable Property', 'TDS on purchase of immovable property > 50L', 1.00, NULL, NULL,
 5000000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['individual', 'company'],
 '2228', NULL, '2024-04-01'),

-- Perquisites
('194R', 'Perquisites', 'TDS on business perquisites and benefits', 10.00, NULL, NULL,
 20000.00, NULL, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual'],
 '2229', NULL, '2024-04-01'),

-- Crypto/VDA
('194S', 'Virtual Digital Assets', 'TDS on transfer of crypto/VDA', 1.00, NULL, NULL,
 10000.00, 50000.00, ARRAY['individual', 'company', 'firm'], ARRAY['company', 'individual'],
 '2230', NULL, '2024-04-01')

ON CONFLICT (section_code) DO UPDATE SET
    section_name = EXCLUDED.section_name,
    default_rate = EXCLUDED.default_rate,
    threshold_per_transaction = EXCLUDED.threshold_per_transaction,
    threshold_annual = EXCLUDED.threshold_annual,
    updated_at = CURRENT_TIMESTAMP;

COMMENT ON TABLE tds_sections IS 'TDS section master with rates and thresholds per Income Tax Act FY 2024-25';
COMMENT ON FUNCTION add_tds_comprehensive_accounts IS 'Adds comprehensive TDS accounts for all sections per Income Tax Act';
