-- Migration: 134_tag_driven_tds_system.sql
-- Description: Tag-driven TDS system - tags drive TDS behavior instead of hard-coded vendor types
-- Features: TDS section tags, tax rules per FY 2024-25, auto-tagging from Tally groups

-- ============================================================================
-- 1. EXTEND TAGS TABLE
-- Add columns for system tags and TDS-specific tag groups
-- ============================================================================

-- Add is_system column if not exists (for system-seeded tags that shouldn't be deleted)
ALTER TABLE tags ADD COLUMN IF NOT EXISTS is_system BOOLEAN NOT NULL DEFAULT false;

-- Extend tag_group to include tds_section and compliance
ALTER TABLE tags DROP CONSTRAINT IF EXISTS chk_tags_group;
ALTER TABLE tags ADD CONSTRAINT chk_tags_group CHECK (tag_group IN (
    'department', 'project', 'client', 'region', 'cost_center', 'custom',
    'tds_section', 'party_type', 'compliance'
));

-- Index for TDS section tags
CREATE INDEX IF NOT EXISTS idx_tags_tds_section ON tags(company_id, tag_group) WHERE tag_group = 'tds_section';

-- ============================================================================
-- 2. TDS TAG RULES TABLE
-- Links tags to TDS configuration with FY 2024-25 compliant rates
-- ============================================================================

CREATE TABLE IF NOT EXISTS tds_tag_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    -- TDS Section Info (per Income Tax Act, 1961)
    tds_section VARCHAR(10) NOT NULL,          -- '194C', '194J', '194H', '194I', '194A', '194Q', '195', '194M', 'EXEMPT'
    tds_section_clause VARCHAR(20),            -- '194J(a)', '194J(ba)', '194I(a)', '194I(b)' for sub-clauses

    -- Rate Configuration (FY 2024-25 rates as per CBDT)
    tds_rate_with_pan DECIMAL(5,2) NOT NULL,           -- Rate when valid PAN provided
    tds_rate_without_pan DECIMAL(5,2) NOT NULL DEFAULT 20.00,  -- Rate when PAN not provided (Section 206AA)
    tds_rate_individual DECIMAL(5,2),                  -- Specific rate for individuals (e.g., 194C: 1%)
    tds_rate_company DECIMAL(5,2),                     -- Specific rate for companies (e.g., 194C: 2%)

    -- Thresholds (as per Income Tax Act)
    threshold_single_payment DECIMAL(18,2),   -- Per payment threshold (e.g., 194C: Rs 30,000)
    threshold_annual DECIMAL(18,2) NOT NULL,  -- Annual aggregate threshold

    -- Applicability by Entity Type (4th char of PAN)
    applies_to_individual BOOLEAN NOT NULL DEFAULT true,   -- P
    applies_to_huf BOOLEAN NOT NULL DEFAULT true,          -- H
    applies_to_company BOOLEAN NOT NULL DEFAULT true,      -- C
    applies_to_firm BOOLEAN NOT NULL DEFAULT true,         -- F
    applies_to_llp BOOLEAN NOT NULL DEFAULT true,          -- L
    applies_to_trust BOOLEAN NOT NULL DEFAULT true,        -- T
    applies_to_aop_boi BOOLEAN NOT NULL DEFAULT true,      -- A, B
    applies_to_government BOOLEAN NOT NULL DEFAULT false,  -- G (usually exempt)

    -- Special Provisions
    lower_certificate_allowed BOOLEAN NOT NULL DEFAULT true,   -- Form 13 lower TDS
    nil_certificate_allowed BOOLEAN NOT NULL DEFAULT true,     -- Nil deduction certificate

    -- Exemptions/Notes
    exemption_notes TEXT,  -- Special exemptions or notes

    -- Validity Period (for rate changes across FYs)
    effective_from DATE NOT NULL DEFAULT '2024-04-01',  -- FY start
    effective_to DATE,  -- NULL = currently effective

    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_tds_tag_rules UNIQUE (company_id, tag_id, effective_from)
);

-- Indexes
CREATE INDEX idx_tds_tag_rules_company ON tds_tag_rules(company_id);
CREATE INDEX idx_tds_tag_rules_tag ON tds_tag_rules(tag_id);
CREATE INDEX idx_tds_tag_rules_section ON tds_tag_rules(tds_section);
CREATE INDEX idx_tds_tag_rules_active ON tds_tag_rules(company_id, is_active) WHERE is_active = true;
CREATE INDEX idx_tds_tag_rules_effective ON tds_tag_rules(company_id, effective_from, effective_to);

-- Trigger for updated_at
CREATE OR REPLACE FUNCTION update_tds_tag_rules_timestamp()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_tds_tag_rules_timestamp ON tds_tag_rules;
CREATE TRIGGER trg_tds_tag_rules_timestamp
    BEFORE UPDATE ON tds_tag_rules
    FOR EACH ROW
    EXECUTE FUNCTION update_tds_tag_rules_timestamp();

-- ============================================================================
-- 3. TALLY GROUP TO TAG MAPPING
-- Map Tally ledger groups to tags for auto-classification during import
-- ============================================================================

-- Extend tally_field_mappings to support tag assignments
ALTER TABLE tally_field_mappings ADD COLUMN IF NOT EXISTS tag_assignments JSONB DEFAULT '[]';

-- ============================================================================
-- 4. SEED FUNCTION: Create default TDS tags and rules for a company
-- ============================================================================

CREATE OR REPLACE FUNCTION seed_tds_system(p_company_id UUID)
RETURNS void AS $$
DECLARE
    v_tag_id UUID;
    v_tag_name VARCHAR(100);
    v_tds_section VARCHAR(10);
    v_rate_with_pan DECIMAL(5,2);
    v_rate_individual DECIMAL(5,2);
    v_rate_company DECIMAL(5,2);
    v_threshold_annual DECIMAL(18,2);
    v_threshold_single DECIMAL(18,2);
    v_section_clause VARCHAR(20);
    v_exemption_notes TEXT;
BEGIN
    -- ========== TDS SECTION TAGS ==========

    -- 194C: Contractor Payments
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194C-Contractor', '194C', 'tds_section', '#FF6B6B',
            'Contractors - TDS u/s 194C @ 1%/2%', '/TDS:194C-Contractor', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               tds_rate_individual, tds_rate_company, threshold_single_payment, threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194C', 1.00, 20.00, 1.00, 2.00, 30000.00, 100000.00,
            'Exempt: Transporters owning 10 or fewer goods carriages who furnish PAN/declaration')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 1.00, tds_rate_individual = 1.00, tds_rate_company = 2.00,
        threshold_single_payment = 30000.00, threshold_annual = 100000.00;

    -- 194J: Professional Services (10% rate)
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194J-Professional', '194J', 'tds_section', '#4ECDC4',
            'Professional/Legal/Medical fees - TDS u/s 194J @ 10%', '/TDS:194J-Professional', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194J', '194J(a)', 10.00, 20.00, 50000.00,
            'Covers: Legal, medical, engineering, architectural, accountancy, technical consultancy, interior decoration, advertising, sports, board exam services')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 10.00, threshold_annual = 50000.00;

    -- 194J(ba): Technical Services (2% rate - per Finance Act 2020)
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194J-Technical', '194JT', 'tds_section', '#45B7D1',
            'Technical services/Royalty - TDS u/s 194J(ba) @ 2%', '/TDS:194J-Technical', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194J', '194J(ba)', 2.00, 20.00, 50000.00,
            'Covers: Technical services, royalty for sale/distribution of cinematographic films. Reduced rate from Finance Act 2020.')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 2.00, threshold_annual = 50000.00;

    -- 194H: Commission/Brokerage
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194H-Commission', '194H', 'tds_section', '#96CEB4',
            'Commission/Brokerage - TDS u/s 194H @ 5%', '/TDS:194H-Commission', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194H', 5.00, 20.00, 15000.00,
            'Covers: Commission or brokerage. Excludes: Insurance commission (covered under 194D)')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 5.00, threshold_annual = 15000.00;

    -- 194I(a): Rent - Land/Building/Furniture
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194I-Rent-Land', '194IA', 'tds_section', '#FFEAA7',
            'Rent for land/building/furniture - TDS u/s 194I(a) @ 10%', '/TDS:194I-Rent-Land', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194I', '194I(a)', 10.00, 20.00, 240000.00,
            'Covers: Rent of land, building, land appurtenant to building, furniture, fittings')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 10.00, threshold_annual = 240000.00;

    -- 194I(b): Rent - Plant/Machinery/Equipment
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194I-Rent-Equipment', '194IB', 'tds_section', '#DDA0DD',
            'Rent for plant/machinery/equipment - TDS u/s 194I(b) @ 2%', '/TDS:194I-Rent-Equipment', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_section_clause, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194I', '194I(b)', 2.00, 20.00, 240000.00,
            'Covers: Rent of plant, machinery, equipment')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 2.00, threshold_annual = 240000.00;

    -- 194A: Interest (other than on securities)
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194A-Interest', '194A', 'tds_section', '#98D8C8',
            'Interest payments - TDS u/s 194A @ 10%', '/TDS:194A-Interest', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194A', 10.00, 20.00, 50000.00,
            'Covers: Interest other than interest on securities. Senior citizen threshold: Rs 50,000 (bank), Rs 50,000 (others). Form 15G/15H available.')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 10.00, threshold_annual = 50000.00;

    -- 194Q: Purchase of Goods
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194Q-Purchase', '194Q', 'tds_section', '#F7DC6F',
            'Purchase of goods - TDS u/s 194Q @ 0.1%', '/TDS:194Q-Purchase', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes)
    VALUES (p_company_id, v_tag_id, '194Q', 0.10, 5.00, 5000000.00,
            'Applicable only for buyers with turnover > Rs 10 Cr in preceding FY. Not applicable if seller liable for TCS u/s 206C(1H). TDS on amount exceeding Rs 50 lakh.')
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 0.10, threshold_annual = 5000000.00;

    -- 195: Foreign Payments
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:195-Foreign', '195', 'tds_section', '#BB8FCE',
            'Foreign payments - TDS u/s 195 @ DTAA rates', '/TDS:195-Foreign', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes, applies_to_government)
    VALUES (p_company_id, v_tag_id, '195', 10.00, 20.00, 0.00,
            'Rate varies by DTAA. Default 10% for royalties/FTS. Get CA certificate (Form 15CB/CA) for DTAA rates. File Form 15CA for remittances.',
            false)
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 10.00, threshold_annual = 0.00;

    -- 194M: Payments to Individuals/HUF
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:194M-Individual', '194M', 'tds_section', '#F8B500',
            'Payments to individuals/HUF - TDS u/s 194M @ 5%', '/TDS:194M-Individual', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes, applies_to_company, applies_to_firm, applies_to_llp, applies_to_trust, applies_to_aop_boi)
    VALUES (p_company_id, v_tag_id, '194M', 5.00, 20.00, 5000000.00,
            'For individuals/HUF not liable for tax audit u/s 44AB. Threshold: Rs 50 lakh aggregate in FY. Covers: Commission/brokerage, contractual work, professional fees.',
            false, false, false, false, false)
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 5.00, threshold_annual = 5000000.00;

    -- TDS Exempt
    INSERT INTO tags (company_id, name, code, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES (p_company_id, 'TDS:Exempt', 'EXEMPT', 'tds_section', '#27AE60',
            'TDS not applicable', '/TDS:Exempt', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true
    RETURNING id INTO v_tag_id;

    INSERT INTO tds_tag_rules (company_id, tag_id, tds_section, tds_rate_with_pan, tds_rate_without_pan,
                               threshold_annual, exemption_notes, applies_to_government)
    VALUES (p_company_id, v_tag_id, 'EXEMPT', 0.00, 0.00, 0.00,
            'Covers: Government entities, income exempt under specific sections, payments below all thresholds',
            true)
    ON CONFLICT (company_id, tag_id, effective_from) DO UPDATE
    SET tds_rate_with_pan = 0.00, threshold_annual = 0.00;

    -- ========== PARTY TYPE TAGS ==========

    -- Vendor Types
    INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES
        (p_company_id, 'Vendor:Supplier', 'party_type', '#3498DB', 'Regular goods supplier', '/Vendor:Supplier', 0, true, true),
        (p_company_id, 'Vendor:Contractor', 'party_type', '#E74C3C', 'Works contractor', '/Vendor:Contractor', 0, true, true),
        (p_company_id, 'Vendor:Consultant', 'party_type', '#9B59B6', 'Professional consultant', '/Vendor:Consultant', 0, true, true),
        (p_company_id, 'Vendor:Landlord', 'party_type', '#F39C12', 'Property/asset lessor', '/Vendor:Landlord', 0, true, true),
        (p_company_id, 'Vendor:Service', 'party_type', '#1ABC9C', 'Service provider', '/Vendor:Service', 0, true, true),
        (p_company_id, 'Vendor:Foreign', 'party_type', '#34495E', 'Non-resident vendor', '/Vendor:Foreign', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true;

    -- Customer Types
    INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES
        (p_company_id, 'Customer:B2B', 'party_type', '#2ECC71', 'Business customer (registered)', '/Customer:B2B', 0, true, true),
        (p_company_id, 'Customer:B2C', 'party_type', '#27AE60', 'Consumer customer', '/Customer:B2C', 0, true, true),
        (p_company_id, 'Customer:Export', 'party_type', '#16A085', 'Export customer (LUT/Bond)', '/Customer:Export', 0, true, true),
        (p_company_id, 'Customer:SEZ', 'party_type', '#1E8449', 'SEZ customer', '/Customer:SEZ', 0, true, true),
        (p_company_id, 'Customer:Government', 'party_type', '#145A32', 'Government entity', '/Customer:Government', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true;

    -- ========== COMPLIANCE TAGS ==========

    INSERT INTO tags (company_id, name, tag_group, color, description, full_path, level, is_system, is_active)
    VALUES
        (p_company_id, 'MSME:Micro', 'compliance', '#FF9F43', 'MSME Micro enterprise (Inv < Rs 1Cr, TO < Rs 5Cr)', '/MSME:Micro', 0, true, true),
        (p_company_id, 'MSME:Small', 'compliance', '#FECA57', 'MSME Small enterprise (Inv < Rs 10Cr, TO < Rs 50Cr)', '/MSME:Small', 0, true, true),
        (p_company_id, 'MSME:Medium', 'compliance', '#EE5A24', 'MSME Medium enterprise (Inv < Rs 50Cr, TO < Rs 250Cr)', '/MSME:Medium', 0, true, true),
        (p_company_id, 'GST:Composition', 'compliance', '#0ABDE3', 'GST Composition scheme dealer', '/GST:Composition', 0, true, true),
        (p_company_id, 'GST:Exempt', 'compliance', '#10AC84', 'GST exempt supplier', '/GST:Exempt', 0, true, true),
        (p_company_id, 'PAN:Verified', 'compliance', '#2ECC71', 'PAN verified via IT portal', '/PAN:Verified', 0, true, true),
        (p_company_id, 'PAN:Invalid', 'compliance', '#E74C3C', 'PAN invalid/not provided (higher TDS)', '/PAN:Invalid', 0, true, true)
    ON CONFLICT (company_id, name, parent_tag_id) DO UPDATE SET is_active = true;

    -- ========== TALLY GROUP TO TAG MAPPINGS ==========
    -- Insert default mappings for Tally ledger groups with tag_assignments

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, tag_assignments, priority, is_system_default, is_active)
    VALUES
        -- Vendor Groups with TDS
        (p_company_id, 'ledger_group', 'CONSULTANTS', '', 'vendors',
         '["TDS:194J-Professional", "Vendor:Consultant"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'CONTRACTORS', '', 'vendors',
         '["TDS:194C-Contractor", "Vendor:Contractor"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'PROFESSIONAL FEES', '', 'vendors',
         '["TDS:194J-Professional", "Vendor:Consultant"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'RENT PAYABLE', '', 'vendors',
         '["TDS:194I-Rent-Land", "Vendor:Landlord"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'COMMISSION PAYABLE', '', 'vendors',
         '["TDS:194H-Commission", "Vendor:Service"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'INTEREST PAYABLE', '', 'vendors',
         '["TDS:194A-Interest"]'::jsonb, 10, true, true),

        -- Standard Vendor Groups (no TDS by default)
        (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors',
         '["Vendor:Supplier"]'::jsonb, 20, true, true),

        -- Customer Groups
        (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers',
         '["Customer:B2B"]'::jsonb, 20, true, true),

        (p_company_id, 'ledger_group', 'EXPORT DEBTORS', '', 'customers',
         '["Customer:Export"]'::jsonb, 10, true, true),

        (p_company_id, 'ledger_group', 'GOVERNMENT', '', 'customers',
         '["Customer:Government", "TDS:Exempt"]'::jsonb, 10, true, true)

    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET tag_assignments = EXCLUDED.tag_assignments;

END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 5. PARTY TAGS TABLE - Add missing columns if table exists
-- Junction table for party-tag many-to-many relationship
-- ============================================================================

-- Add confidence_score column if not exists (for AI suggestions)
ALTER TABLE party_tags ADD COLUMN IF NOT EXISTS confidence_score INT;

-- Note: We use created_at instead of assigned_at for compatibility with migration 128
-- The code references pt.created_at for ordering

-- Indexes (IF NOT EXISTS for safety)
CREATE INDEX IF NOT EXISTS idx_party_tags_party ON party_tags(party_id);
CREATE INDEX IF NOT EXISTS idx_party_tags_tag ON party_tags(tag_id);
CREATE INDEX IF NOT EXISTS idx_party_tags_source ON party_tags(source);

-- ============================================================================
-- 6. VIEW: Party TDS Configuration
-- Denormalized view for quick TDS lookup
-- ============================================================================

CREATE OR REPLACE VIEW v_party_tds_config AS
SELECT
    p.id AS party_id,
    p.company_id,
    p.name AS party_name,
    p.pan_number AS pan,
    pvp.tds_applicable AS manual_tds_applicable,
    pvp.default_tds_section AS manual_tds_section,
    pvp.default_tds_rate AS manual_tds_rate,
    -- Tag-based TDS (takes precedence if manual not set)
    t.id AS tds_tag_id,
    t.name AS tds_tag_name,
    ttr.tds_section AS tag_tds_section,
    ttr.tds_section_clause,
    ttr.tds_rate_with_pan,
    ttr.tds_rate_without_pan,
    ttr.tds_rate_individual,
    ttr.tds_rate_company,
    ttr.threshold_annual,
    ttr.threshold_single_payment,
    ttr.exemption_notes,
    -- Resolved TDS (manual overrides tag)
    COALESCE(pvp.default_tds_section, ttr.tds_section) AS effective_tds_section,
    COALESCE(pvp.default_tds_rate, ttr.tds_rate_with_pan) AS effective_tds_rate,
    CASE
        WHEN pvp.tds_applicable IS NOT NULL THEN 'manual'
        WHEN ttr.id IS NOT NULL THEN 'tag'
        ELSE 'none'
    END AS tds_source
FROM parties p
LEFT JOIN party_vendor_profiles pvp ON pvp.party_id = p.id
LEFT JOIN party_tags pt ON pt.party_id = p.id
LEFT JOIN tags t ON t.id = pt.tag_id AND t.tag_group = 'tds_section'
LEFT JOIN tds_tag_rules ttr ON ttr.tag_id = t.id
    AND ttr.is_active = true
    AND ttr.effective_from <= CURRENT_DATE
    AND (ttr.effective_to IS NULL OR ttr.effective_to >= CURRENT_DATE)
WHERE p.is_vendor = true;

-- ============================================================================
-- 7. FUNCTION: Detect TDS for a party
-- Returns the effective TDS configuration based on tags and manual settings
-- ============================================================================

CREATE OR REPLACE FUNCTION detect_party_tds(
    p_party_id UUID,
    p_payment_amount DECIMAL DEFAULT NULL
)
RETURNS TABLE (
    is_applicable BOOLEAN,
    tds_section VARCHAR(10),
    tds_section_clause VARCHAR(20),
    tds_rate DECIMAL(5,2),
    threshold_annual DECIMAL(18,2),
    threshold_single DECIMAL(18,2),
    is_below_threshold BOOLEAN,
    detection_method VARCHAR(20),
    matched_tag_id UUID,
    matched_tag_name VARCHAR(100),
    exemption_notes TEXT
) AS $$
DECLARE
    v_party RECORD;
    v_tds_tag RECORD;
    v_pan_type CHAR(1);
    v_rate DECIMAL(5,2);
BEGIN
    -- Get party details
    SELECT p.*, pvp.tds_applicable, pvp.default_tds_section, pvp.default_tds_rate,
           pvp.lower_tds_certificate, pvp.lower_tds_rate, pvp.lower_tds_valid_till
    INTO v_party
    FROM parties p
    LEFT JOIN party_vendor_profiles pvp ON pvp.party_id = p.id
    WHERE p.id = p_party_id;

    IF NOT FOUND THEN
        RETURN QUERY SELECT false, NULL::VARCHAR, NULL::VARCHAR, 0::DECIMAL,
                            NULL::DECIMAL, NULL::DECIMAL, false, 'not_found'::VARCHAR,
                            NULL::UUID, NULL::VARCHAR, NULL::TEXT;
        RETURN;
    END IF;

    -- Check manual TDS setting first
    IF v_party.tds_applicable = false THEN
        RETURN QUERY SELECT false, NULL::VARCHAR, NULL::VARCHAR, 0::DECIMAL,
                            NULL::DECIMAL, NULL::DECIMAL, false, 'manual_exempt'::VARCHAR,
                            NULL::UUID, NULL::VARCHAR, 'Manually marked as TDS not applicable'::TEXT;
        RETURN;
    END IF;

    IF v_party.default_tds_section IS NOT NULL THEN
        -- Check lower certificate
        IF v_party.lower_tds_certificate IS NOT NULL AND v_party.lower_tds_valid_till >= CURRENT_DATE THEN
            v_rate := v_party.lower_tds_rate;
        ELSE
            v_rate := v_party.default_tds_rate;
        END IF;

        RETURN QUERY SELECT true, v_party.default_tds_section::VARCHAR, NULL::VARCHAR, v_rate,
                            NULL::DECIMAL, NULL::DECIMAL, false, 'manual'::VARCHAR,
                            NULL::UUID, NULL::VARCHAR, NULL::TEXT;
        RETURN;
    END IF;

    -- Find TDS tag for this party
    SELECT t.id, t.name, ttr.*
    INTO v_tds_tag
    FROM party_tags pt
    JOIN tags t ON t.id = pt.tag_id
    JOIN tds_tag_rules ttr ON ttr.tag_id = t.id
    WHERE pt.party_id = p_party_id
      AND t.tag_group = 'tds_section'
      AND t.is_active = true
      AND ttr.is_active = true
      AND ttr.effective_from <= CURRENT_DATE
      AND (ttr.effective_to IS NULL OR ttr.effective_to >= CURRENT_DATE)
    ORDER BY ttr.tds_section  -- Prefer specific sections
    LIMIT 1;

    IF NOT FOUND THEN
        RETURN QUERY SELECT false, NULL::VARCHAR, NULL::VARCHAR, 0::DECIMAL,
                            NULL::DECIMAL, NULL::DECIMAL, false, 'no_tag'::VARCHAR,
                            NULL::UUID, NULL::VARCHAR, 'No TDS tag assigned to party'::TEXT;
        RETURN;
    END IF;

    -- Check for TDS exempt tag
    IF v_tds_tag.tds_section = 'EXEMPT' THEN
        RETURN QUERY SELECT false, 'EXEMPT'::VARCHAR, NULL::VARCHAR, 0::DECIMAL,
                            NULL::DECIMAL, NULL::DECIMAL, false, 'tag_exempt'::VARCHAR,
                            v_tds_tag.id, v_tds_tag.name::VARCHAR, v_tds_tag.exemption_notes;
        RETURN;
    END IF;

    -- Determine rate based on PAN availability and entity type
    IF v_party.pan_number IS NULL OR LENGTH(v_party.pan_number) < 10 THEN
        v_rate := v_tds_tag.tds_rate_without_pan;
    ELSE
        v_pan_type := SUBSTRING(v_party.pan_number FROM 4 FOR 1);
        v_rate := CASE v_pan_type
            WHEN 'P' THEN COALESCE(v_tds_tag.tds_rate_individual, v_tds_tag.tds_rate_with_pan)  -- Individual
            WHEN 'H' THEN COALESCE(v_tds_tag.tds_rate_individual, v_tds_tag.tds_rate_with_pan)  -- HUF
            WHEN 'C' THEN COALESCE(v_tds_tag.tds_rate_company, v_tds_tag.tds_rate_with_pan)     -- Company
            WHEN 'F' THEN COALESCE(v_tds_tag.tds_rate_company, v_tds_tag.tds_rate_with_pan)     -- Firm
            WHEN 'L' THEN COALESCE(v_tds_tag.tds_rate_company, v_tds_tag.tds_rate_with_pan)     -- LLP
            WHEN 'T' THEN COALESCE(v_tds_tag.tds_rate_company, v_tds_tag.tds_rate_with_pan)     -- Trust
            ELSE v_tds_tag.tds_rate_with_pan
        END;
    END IF;

    -- Check lower certificate
    IF v_party.lower_tds_certificate IS NOT NULL AND v_party.lower_tds_valid_till >= CURRENT_DATE THEN
        v_rate := LEAST(v_rate, v_party.lower_tds_rate);
    END IF;

    RETURN QUERY SELECT
        true,
        v_tds_tag.tds_section::VARCHAR,
        v_tds_tag.tds_section_clause::VARCHAR,
        v_rate,
        v_tds_tag.threshold_annual,
        v_tds_tag.threshold_single_payment,
        CASE WHEN p_payment_amount IS NOT NULL
             AND v_tds_tag.threshold_single_payment IS NOT NULL
             AND p_payment_amount < v_tds_tag.threshold_single_payment
        THEN true ELSE false END,
        'tag'::VARCHAR,
        v_tds_tag.id,
        v_tds_tag.name::VARCHAR,
        v_tds_tag.exemption_notes;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- 8. CLEANUP: Remove old tds_section_rules if migrating to tag-based system
-- ============================================================================

-- We keep the old table for now but will deprecate it
-- ALTER TABLE tds_section_rules RENAME TO tds_section_rules_deprecated;

COMMENT ON TABLE tds_tag_rules IS 'Tag-driven TDS configuration. FY 2024-25 rates per CBDT circular. Tags drive TDS behavior, not hard-coded vendor types.';
