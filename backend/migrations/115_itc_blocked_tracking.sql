-- Migration: ITC Blocked Categories Tracking
-- Purpose: Track blocked Input Tax Credit per Section 17(5) CGST Act
-- Reference: Section 17(5) CGST Act, 2017

-- ============================================
-- ITC Blocked Categories Master Table
-- Per Section 17(5) CGST Act
-- ============================================

CREATE TABLE IF NOT EXISTS itc_blocked_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_code VARCHAR(50) NOT NULL UNIQUE,
    category_name VARCHAR(255) NOT NULL,
    description TEXT,

    -- Section reference
    section_reference VARCHAR(50) NOT NULL,  -- '17(5)(a)', '17(5)(b)', etc.
    sub_clause VARCHAR(20),                   -- 'i', 'ii', etc.

    -- Applicability
    applicable_goods TEXT[],                  -- Array of goods/services
    hsn_sac_codes TEXT[],                     -- Applicable HSN/SAC codes
    expense_categories TEXT[],                -- Matching expense category codes

    -- Exceptions (when ITC is allowed)
    exceptions TEXT,

    -- Validity
    effective_from DATE NOT NULL DEFAULT '2017-07-01',
    effective_to DATE,
    is_active BOOLEAN DEFAULT TRUE,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_itc_blocked_code ON itc_blocked_categories(category_code);
CREATE INDEX IF NOT EXISTS idx_itc_blocked_section ON itc_blocked_categories(section_reference);

-- ============================================
-- Seed ITC Blocked Categories per Section 17(5)
-- ============================================

INSERT INTO itc_blocked_categories (category_code, category_name, description, section_reference,
    sub_clause, applicable_goods, expense_categories, exceptions, effective_from) VALUES

-- Section 17(5)(a) - Motor vehicles
('MOTOR_VEHICLE', 'Motor Vehicles', 'Motor vehicles and other conveyances except when used for specified purposes',
 '17(5)(a)', NULL,
 ARRAY['Motor vehicles', 'Cars', 'Two-wheelers', 'Aircraft', 'Vessels'],
 ARRAY['vehicle_purchase', 'car_lease', 'vehicle_maintenance'],
 'Allowed when: (i) for further supply, (ii) for transportation of passengers, (iii) for imparting training on driving/flying/navigating, (iv) transportation of goods',
 '2017-07-01'),

-- Section 17(5)(aa) - Vessels and aircraft (added post-GST)
('VESSEL_AIRCRAFT', 'Vessels and Aircraft', 'Vessels and aircraft except when used for specified purposes',
 '17(5)(aa)', NULL,
 ARRAY['Aircraft', 'Vessels', 'Ships', 'Boats'],
 ARRAY['aircraft_purchase', 'vessel_purchase'],
 'Allowed when used for making taxable supply of: (i) further supply of such vessels/aircraft, (ii) transportation of passengers, (iii) imparting training, (iv) transportation of goods',
 '2019-02-01'),

-- Section 17(5)(b)(i) - Food and beverages
('FOOD_BEVERAGE', 'Food and Beverages', 'Food and beverages, outdoor catering, beauty treatment, health services, cosmetic and plastic surgery',
 '17(5)(b)', 'i',
 ARRAY['Food', 'Beverages', 'Catering', 'Restaurant services'],
 ARRAY['meals', 'food_expense', 'catering', 'restaurant'],
 'Allowed when: (i) inward supply is used for making outward taxable supply of same category, (ii) as part of taxable composite/mixed supply',
 '2017-07-01'),

-- Section 17(5)(b)(ii) - Beauty treatment, health services
('BEAUTY_HEALTH', 'Beauty and Health Services', 'Beauty treatment, health services, cosmetic and plastic surgery',
 '17(5)(b)', 'ii',
 ARRAY['Beauty treatment', 'Spa', 'Health services', 'Cosmetic surgery', 'Plastic surgery'],
 ARRAY['spa', 'beauty', 'cosmetic', 'health_wellness'],
 'Allowed when making outward taxable supply of same category or as part of composite/mixed supply',
 '2017-07-01'),

-- Section 17(5)(b)(iii) - Membership of club/fitness
('CLUB_MEMBERSHIP', 'Club and Fitness Membership', 'Membership of a club, health and fitness centre',
 '17(5)(b)', 'iii',
 ARRAY['Club membership', 'Gym membership', 'Health club', 'Fitness centre'],
 ARRAY['club_membership', 'gym', 'fitness'],
 'Allowed when making outward taxable supply of same category or as part of composite/mixed supply',
 '2017-07-01'),

-- Section 17(5)(b)(iv) - Rent-a-cab, life insurance, health insurance
('RENTACAB_INSURANCE', 'Rent-a-cab and Insurance', 'Rent-a-cab, life insurance and health insurance except when mandatory by employer',
 '17(5)(b)', 'iv',
 ARRAY['Rent-a-cab', 'Cab services', 'Life insurance', 'Health insurance'],
 ARRAY['cab_hire', 'taxi', 'life_insurance', 'health_insurance'],
 'Allowed when: (i) Government notifies as obligatory for employer to provide, (ii) for making taxable supply of same category, (iii) as part of composite/mixed supply',
 '2017-07-01'),

-- Section 17(5)(b)(v) - Travel benefits to employees
('TRAVEL_BENEFITS', 'Employee Travel Benefits', 'Travel benefits extended to employees on vacation such as leave or home travel concession',
 '17(5)(b)', 'v',
 ARRAY['LTC', 'Leave travel', 'Home travel', 'Vacation travel'],
 ARRAY['ltc', 'leave_travel', 'employee_vacation'],
 'None - always blocked',
 '2017-07-01'),

-- Section 17(5)(c) - Works contract for construction
('WORKS_CONTRACT_CONSTRUCTION', 'Works Contract for Immovable Property', 'Works contract services for construction of immovable property (other than P&M)',
 '17(5)(c)', NULL,
 ARRAY['Works contract', 'Construction services', 'Civil construction'],
 ARRAY['construction', 'civil_works', 'building_construction'],
 'Allowed when: (i) input service for further supply of works contract, (ii) construction of plant and machinery',
 '2017-07-01'),

-- Section 17(5)(d) - Goods/services for construction
('CONSTRUCTION_GOODS', 'Construction of Immovable Property', 'Goods or services received for construction of immovable property on own account',
 '17(5)(d)', NULL,
 ARRAY['Construction materials', 'Building materials', 'Cement', 'Steel', 'Construction services'],
 ARRAY['construction_material', 'building_material'],
 'Allowed when: (i) it is plant and machinery, (ii) for further supply',
 '2017-07-01'),

-- Section 17(5)(e) - Composition dealer supplies
('COMPOSITION_SUPPLY', 'Composition Dealer Supply', 'Goods or services or both on which tax has been paid under composition scheme',
 '17(5)(e)', NULL,
 ARRAY['Any goods/services from composition dealer'],
 ARRAY['composition_purchase'],
 'None - ITC never available on composition supplies',
 '2017-07-01'),

-- Section 17(5)(f) - Non-resident taxable person
('NON_RESIDENT_SUPPLY', 'Non-Resident Taxable Person', 'Goods or services received by non-resident taxable person except goods imported',
 '17(5)(f)', NULL,
 ARRAY['Any goods/services by NRTP'],
 NULL,
 'Goods imported by NRTP are allowed ITC',
 '2017-07-01'),

-- Section 17(5)(g) - Personal consumption
('PERSONAL_CONSUMPTION', 'Personal Consumption', 'Goods or services or both used for personal consumption',
 '17(5)(g)', NULL,
 ARRAY['Personal use items', 'Gift to employees', 'Personal consumption'],
 ARRAY['personal_expense', 'employee_gift'],
 'None - always blocked when for personal consumption',
 '2017-07-01'),

-- Section 17(5)(h) - Lost, stolen, destroyed, written off, disposed by gift
('LOST_DESTROYED', 'Lost, Stolen, Destroyed, Written Off', 'Goods lost, stolen, destroyed, written off or disposed of by way of gift or free samples',
 '17(5)(h)', NULL,
 ARRAY['Lost goods', 'Stolen goods', 'Destroyed goods', 'Written off', 'Gifts', 'Free samples'],
 ARRAY['inventory_loss', 'theft_loss', 'goods_writeoff', 'free_samples', 'promotional_gifts'],
 'Proportionate reversal required when goods used partly for taxable and exempt supplies',
 '2017-07-01'),

-- Section 17(5)(i) - Tax paid due to fraud/suppression
('TAX_FRAUD', 'Tax Paid on Fraud/Suppression', 'Any tax paid in accordance with sections 74, 129 and 130',
 '17(5)(i)', NULL,
 ARRAY['Penalty tax', 'Fraud recovery', 'Seizure related tax'],
 ARRAY['penalty', 'fraud_recovery'],
 'None - tax paid on fraud/suppression never eligible for ITC',
 '2017-07-01')

ON CONFLICT (category_code) DO UPDATE SET
    category_name = EXCLUDED.category_name,
    description = EXCLUDED.description,
    exceptions = EXCLUDED.exceptions,
    updated_at = CURRENT_TIMESTAMP;

-- ============================================
-- ITC Blocked Transactions Table
-- Track individual blocked ITC instances
-- ============================================

CREATE TABLE IF NOT EXISTS itc_blocked_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),

    -- Period
    financial_year VARCHAR(10) NOT NULL,
    return_period VARCHAR(20) NOT NULL,

    -- Source document
    source_type VARCHAR(50) NOT NULL,         -- 'expense_claim', 'vendor_invoice', 'manual'
    source_id UUID,
    source_number VARCHAR(100),

    -- Vendor details
    vendor_name VARCHAR(255),
    vendor_gstin VARCHAR(20),
    invoice_number VARCHAR(100),
    invoice_date DATE,

    -- Blocked category
    blocked_category_id UUID REFERENCES itc_blocked_categories(id),
    blocked_category_code VARCHAR(50) NOT NULL,
    section_reference VARCHAR(50) NOT NULL,

    -- Amounts
    taxable_value DECIMAL(18,2) NOT NULL,
    cgst_blocked DECIMAL(18,2) DEFAULT 0,
    sgst_blocked DECIMAL(18,2) DEFAULT 0,
    igst_blocked DECIMAL(18,2) DEFAULT 0,
    cess_blocked DECIMAL(18,2) DEFAULT 0,
    total_itc_blocked DECIMAL(18,2) NOT NULL,

    -- Expense posting
    expense_account_code VARCHAR(10),          -- Account where GST was capitalized
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- GSTR-3B tracking
    gstr3b_period VARCHAR(20),
    gstr3b_table VARCHAR(20),                  -- '4(B)(1)' for blocked ITC
    gstr3b_filed BOOLEAN DEFAULT FALSE,

    -- Reason and notes
    blocking_reason TEXT,
    notes TEXT,

    -- Audit
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by UUID
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_itc_blocked_trans_company ON itc_blocked_transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_itc_blocked_trans_period ON itc_blocked_transactions(company_id, return_period);
CREATE INDEX IF NOT EXISTS idx_itc_blocked_trans_category ON itc_blocked_transactions(blocked_category_code);
CREATE INDEX IF NOT EXISTS idx_itc_blocked_trans_source ON itc_blocked_transactions(source_type, source_id);

-- ============================================
-- Function: Check if ITC is blocked
-- ============================================

CREATE OR REPLACE FUNCTION check_itc_blocked(
    p_expense_category VARCHAR(50),
    p_hsn_sac_code VARCHAR(20) DEFAULT NULL,
    p_description TEXT DEFAULT NULL
)
RETURNS TABLE (
    is_blocked BOOLEAN,
    blocked_category_code VARCHAR(50),
    blocked_category_name VARCHAR(255),
    section_reference VARCHAR(50),
    exceptions TEXT,
    blocking_reason TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        TRUE as is_blocked,
        bc.category_code as blocked_category_code,
        bc.category_name as blocked_category_name,
        bc.section_reference,
        bc.exceptions,
        'ITC blocked per ' || bc.section_reference || ' - ' || bc.category_name as blocking_reason
    FROM itc_blocked_categories bc
    WHERE bc.is_active = TRUE
      AND CURRENT_DATE BETWEEN bc.effective_from AND COALESCE(bc.effective_to, '9999-12-31')
      AND (
          -- Match by expense category
          (bc.expense_categories IS NOT NULL AND p_expense_category = ANY(bc.expense_categories))
          OR
          -- Match by HSN/SAC code
          (bc.hsn_sac_codes IS NOT NULL AND p_hsn_sac_code = ANY(bc.hsn_sac_codes))
          OR
          -- Match by description keywords (case insensitive)
          (bc.applicable_goods IS NOT NULL AND EXISTS (
              SELECT 1 FROM unnest(bc.applicable_goods) AS g
              WHERE p_description ILIKE '%' || g || '%'
          ))
      )
    LIMIT 1;

    -- If no match found, return not blocked
    IF NOT FOUND THEN
        RETURN QUERY
        SELECT
            FALSE as is_blocked,
            NULL::VARCHAR(50) as blocked_category_code,
            NULL::VARCHAR(255) as blocked_category_name,
            NULL::VARCHAR(50) as section_reference,
            NULL::TEXT as exceptions,
            NULL::TEXT as blocking_reason;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- View: ITC Blocked Summary by Period
-- ============================================

CREATE OR REPLACE VIEW v_itc_blocked_summary AS
SELECT
    company_id,
    return_period,
    blocked_category_code,
    bc.category_name as blocked_category_name,
    bc.section_reference,
    COUNT(*) as transaction_count,
    SUM(taxable_value) as total_taxable_value,
    SUM(cgst_blocked) as total_cgst_blocked,
    SUM(sgst_blocked) as total_sgst_blocked,
    SUM(igst_blocked) as total_igst_blocked,
    SUM(total_itc_blocked) as total_itc_blocked
FROM itc_blocked_transactions t
LEFT JOIN itc_blocked_categories bc ON t.blocked_category_code = bc.category_code
GROUP BY company_id, return_period, blocked_category_code, bc.category_name, bc.section_reference;

-- ============================================
-- Update gst_input_credit table for blocked tracking
-- ============================================

-- Add columns if not exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'gst_input_credit' AND column_name = 'is_blocked') THEN
        ALTER TABLE gst_input_credit ADD COLUMN is_blocked BOOLEAN DEFAULT FALSE;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'gst_input_credit' AND column_name = 'blocked_category_code') THEN
        ALTER TABLE gst_input_credit ADD COLUMN blocked_category_code VARCHAR(50);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'gst_input_credit' AND column_name = 'blocked_section') THEN
        ALTER TABLE gst_input_credit ADD COLUMN blocked_section VARCHAR(50);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'gst_input_credit' AND column_name = 'blocked_reason') THEN
        ALTER TABLE gst_input_credit ADD COLUMN blocked_reason TEXT;
    END IF;
END $$;

-- Create index for blocked ITC queries
CREATE INDEX IF NOT EXISTS idx_gst_input_credit_blocked
ON gst_input_credit(company_id, is_blocked) WHERE is_blocked = TRUE;

COMMENT ON TABLE itc_blocked_categories IS 'ITC blocked categories per Section 17(5) CGST Act';
COMMENT ON TABLE itc_blocked_transactions IS 'Individual blocked ITC transactions for GSTR-3B reporting';
COMMENT ON FUNCTION check_itc_blocked IS 'Check if ITC should be blocked for an expense based on category/HSN/description';
COMMENT ON VIEW v_itc_blocked_summary IS 'Summary of blocked ITC by period and category';
