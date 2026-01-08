-- Migration: 128_unified_party_model.sql
-- Description: Unified Party Management System (Clean Slate)
-- Purpose: Replace separate vendors/customers tables with unified Party model
-- Inspired by: SAP S/4HANA Business Partner, Odoo res.partner

-- ============================================================================
-- PHASE 1: DROP OLD TABLES (Clean Slate)
-- ============================================================================

-- Drop dependent tables first (reverse order of creation)
DROP TABLE IF EXISTS vendor_payment_allocations CASCADE;
DROP TABLE IF EXISTS vendor_payments CASCADE;
DROP TABLE IF EXISTS vendor_invoice_items CASCADE;
DROP TABLE IF EXISTS vendor_invoices CASCADE;
DROP TABLE IF EXISTS vendors CASCADE;

DROP TABLE IF EXISTS payments CASCADE;
DROP TABLE IF EXISTS invoice_items CASCADE;
DROP TABLE IF EXISTS invoices CASCADE;
DROP TABLE IF EXISTS customers CASCADE;

-- ============================================================================
-- PHASE 2: CREATE UNIFIED PARTY TABLE
-- Core entity for all business relationships (customers, vendors, employees)
-- ============================================================================

CREATE TABLE IF NOT EXISTS parties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Core Identity
    name VARCHAR(255) NOT NULL,
    display_name VARCHAR(255),           -- Short name for display
    legal_name VARCHAR(255),             -- Registered legal name
    party_code VARCHAR(50),              -- Internal reference code (auto-generated or manual)

    -- Role Flags (a party can have multiple roles)
    is_customer BOOLEAN DEFAULT false,
    is_vendor BOOLEAN DEFAULT false,
    is_employee BOOLEAN DEFAULT false,

    -- Contact Information
    email VARCHAR(255),
    phone VARCHAR(50),
    mobile VARCHAR(50),
    website VARCHAR(255),
    contact_person VARCHAR(255),

    -- Address (primary)
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    state_code VARCHAR(5),               -- GST state code (e.g., '29' for Karnataka)
    pincode VARCHAR(10),
    country VARCHAR(100) DEFAULT 'India',

    -- Indian Tax Identifiers (shared across roles)
    pan_number VARCHAR(10),              -- 10-character PAN
    gstin VARCHAR(15),                   -- 15-character GSTIN
    is_gst_registered BOOLEAN DEFAULT false,
    gst_state_code VARCHAR(2),           -- First 2 chars of GSTIN

    -- Classification
    party_type VARCHAR(30),              -- individual, company, firm, llp, trust, huf, government, foreign

    -- Status
    is_active BOOLEAN DEFAULT true,
    notes TEXT,

    -- Tally Migration Tracking
    tally_ledger_guid VARCHAR(100),
    tally_ledger_name VARCHAR(255),
    tally_group_name VARCHAR(255),       -- Original Tally ledger group (e.g., 'CONSULTANTS')
    tally_migration_batch_id UUID REFERENCES tally_migration_batches(id),

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_party_code UNIQUE (company_id, party_code),
    CONSTRAINT uq_party_tally_guid UNIQUE (company_id, tally_ledger_guid),
    CONSTRAINT chk_party_type CHECK (party_type IS NULL OR party_type IN (
        'individual', 'company', 'firm', 'llp', 'trust', 'huf', 'government', 'foreign'
    ))
);

-- Indexes for parties
CREATE INDEX idx_parties_company ON parties(company_id);
CREATE INDEX idx_parties_name ON parties(company_id, name);
CREATE INDEX idx_parties_gstin ON parties(company_id, gstin) WHERE gstin IS NOT NULL;
CREATE INDEX idx_parties_pan ON parties(company_id, pan_number) WHERE pan_number IS NOT NULL;
CREATE INDEX idx_parties_roles ON parties(company_id, is_customer, is_vendor);
CREATE INDEX idx_parties_vendor ON parties(company_id) WHERE is_vendor = true;
CREATE INDEX idx_parties_customer ON parties(company_id) WHERE is_customer = true;
CREATE INDEX idx_parties_active ON parties(company_id, is_active) WHERE is_active = true;
CREATE INDEX idx_parties_tally_guid ON parties(tally_ledger_guid) WHERE tally_ledger_guid IS NOT NULL;

COMMENT ON TABLE parties IS 'Unified Party master - single source of truth for customers, vendors, and employees';

-- ============================================================================
-- PHASE 3: CREATE VENDOR PROFILE TABLE (Role Extension)
-- Contains vendor-specific fields: TDS, MSME, Bank Details
-- ============================================================================

CREATE TABLE IF NOT EXISTS party_vendor_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Vendor Classification
    vendor_type VARCHAR(30),             -- b2b, b2c, import, rcm_applicable

    -- ==================== TDS Configuration ====================
    tds_applicable BOOLEAN DEFAULT false,
    default_tds_section VARCHAR(10),     -- 194C, 194J, 194H, 194I, 194IA, 194IB, 194A, 194Q, 194O, 194N, 194M, 195
    default_tds_rate DECIMAL(5,2),       -- Default TDS rate %
    tan_number VARCHAR(15),              -- Vendor's TAN (if any)

    -- Lower/Nil TDS Certificate
    lower_tds_certificate VARCHAR(50),
    lower_tds_rate DECIMAL(5,2),
    lower_tds_valid_from DATE,
    lower_tds_valid_till DATE,

    -- ==================== MSME Compliance ====================
    msme_registered BOOLEAN DEFAULT false,
    msme_registration_number VARCHAR(50),  -- Udyam format: UDYAM-XX-00-0000000
    msme_category VARCHAR(10),             -- micro, small, medium

    -- ==================== Bank Details (for payments) ====================
    bank_account_number VARCHAR(50),
    bank_ifsc_code VARCHAR(15),
    bank_name VARCHAR(100),
    bank_branch VARCHAR(100),
    bank_account_holder VARCHAR(255),
    bank_account_type VARCHAR(20),       -- savings, current, cc, od

    -- ==================== Default Accounts ====================
    default_expense_account_id UUID REFERENCES chart_of_accounts(id),
    default_payable_account_id UUID REFERENCES chart_of_accounts(id),

    -- ==================== Payment Terms ====================
    payment_terms_days INT,
    credit_limit DECIMAL(18,2),

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_party_vendor UNIQUE (party_id),
    CONSTRAINT chk_vendor_type CHECK (vendor_type IS NULL OR vendor_type IN ('b2b', 'b2c', 'import', 'rcm_applicable')),
    CONSTRAINT chk_tds_section CHECK (default_tds_section IS NULL OR default_tds_section IN (
        '194C', '194J', '194H', '194I', '194IA', '194IB', '194A', '194Q', '194O', '194N', '194M', '195'
    )),
    CONSTRAINT chk_msme_category CHECK (msme_category IS NULL OR msme_category IN ('micro', 'small', 'medium'))
);

-- Indexes for vendor profiles
CREATE INDEX idx_party_vendor_profiles_party ON party_vendor_profiles(party_id);
CREATE INDEX idx_party_vendor_profiles_company ON party_vendor_profiles(company_id);
CREATE INDEX idx_party_vendor_profiles_tds ON party_vendor_profiles(company_id, tds_applicable) WHERE tds_applicable = true;
CREATE INDEX idx_party_vendor_profiles_msme ON party_vendor_profiles(company_id, msme_registered) WHERE msme_registered = true;

COMMENT ON TABLE party_vendor_profiles IS 'Vendor-specific profile: TDS, MSME, Bank details for parties with is_vendor=true';

-- ============================================================================
-- PHASE 4: CREATE CUSTOMER PROFILE TABLE (Role Extension)
-- Contains customer-specific fields: Credit terms, E-invoicing
-- ============================================================================

CREATE TABLE IF NOT EXISTS party_customer_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Customer Classification
    customer_type VARCHAR(30),           -- b2b, b2c, overseas, sez

    -- Credit Terms
    credit_limit DECIMAL(18,2),
    payment_terms_days INT,

    -- Default Accounts
    default_revenue_account_id UUID REFERENCES chart_of_accounts(id),
    default_receivable_account_id UUID REFERENCES chart_of_accounts(id),

    -- E-Invoicing (GST India)
    e_invoice_applicable BOOLEAN DEFAULT false,
    e_way_bill_applicable BOOLEAN DEFAULT false,

    -- Pricing & Discounts
    default_discount_percent DECIMAL(5,2),
    price_list_id UUID,                  -- Future: Link to price lists

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_party_customer UNIQUE (party_id),
    CONSTRAINT chk_customer_type CHECK (customer_type IS NULL OR customer_type IN ('b2b', 'b2c', 'overseas', 'sez'))
);

-- Indexes for customer profiles
CREATE INDEX idx_party_customer_profiles_party ON party_customer_profiles(party_id);
CREATE INDEX idx_party_customer_profiles_company ON party_customer_profiles(company_id);

COMMENT ON TABLE party_customer_profiles IS 'Customer-specific profile: Credit terms, E-invoicing for parties with is_customer=true';

-- ============================================================================
-- PHASE 5: CREATE PARTY TAGS TABLE (Classification via Tags)
-- Many-to-many relationship between parties and tags
-- ============================================================================

CREATE TABLE IF NOT EXISTS party_tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    party_id UUID NOT NULL REFERENCES parties(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    -- Source tracking
    source VARCHAR(20) DEFAULT 'manual', -- manual, migration, rule, ai_suggested

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_party_tag UNIQUE (party_id, tag_id)
);

-- Indexes for party tags
CREATE INDEX idx_party_tags_party ON party_tags(party_id);
CREATE INDEX idx_party_tags_tag ON party_tags(tag_id);
CREATE INDEX idx_party_tags_source ON party_tags(source);

COMMENT ON TABLE party_tags IS 'Party classification via tags - links parties to tag groups like party_type';

-- ============================================================================
-- PHASE 6: CREATE TDS SECTION RULES TABLE
-- Auto-detection rules for TDS section based on party tags/groups
-- ============================================================================

CREATE TABLE IF NOT EXISTS tds_section_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Rule Name
    name VARCHAR(100) NOT NULL,
    description TEXT,

    -- Matching Criteria (at least one should be set)
    tag_id UUID REFERENCES tags(id),         -- Match by party tag
    party_name_pattern VARCHAR(255),          -- Regex pattern for name matching
    tally_group_name VARCHAR(255),            -- Match by Tally ledger group

    -- TDS Configuration
    tds_section VARCHAR(10) NOT NULL,
    tds_rate DECIMAL(5,2) NOT NULL,           -- Normal rate (with PAN)
    tds_rate_no_pan DECIMAL(5,2),             -- Higher rate (without PAN), typically 20%

    -- Thresholds
    threshold_amount DECIMAL(18,2),           -- TDS applies above this amount per year
    single_payment_threshold DECIMAL(18,2),   -- TDS applies if single payment exceeds this

    -- Rule Control
    is_active BOOLEAN DEFAULT true,
    priority INT DEFAULT 100,                 -- Lower = higher priority

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT chk_tds_section_rule CHECK (tds_section IN (
        '194C', '194J', '194H', '194I', '194IA', '194IB', '194A', '194Q', '194O', '194N', '194M', '195'
    ))
);

-- Indexes for TDS rules
CREATE INDEX idx_tds_section_rules_company ON tds_section_rules(company_id);
CREATE INDEX idx_tds_section_rules_active ON tds_section_rules(company_id, is_active, priority) WHERE is_active = true;
CREATE INDEX idx_tds_section_rules_tag ON tds_section_rules(tag_id) WHERE tag_id IS NOT NULL;
CREATE INDEX idx_tds_section_rules_tally ON tds_section_rules(tally_group_name) WHERE tally_group_name IS NOT NULL;

COMMENT ON TABLE tds_section_rules IS 'Auto-detection rules for TDS section based on party classification';

-- ============================================================================
-- PHASE 7: SEED PARTY CLASSIFICATION TAGS
-- Add new tag_group = 'party_type' with common party classifications
-- ============================================================================

-- Function to seed party type tags for a company
CREATE OR REPLACE FUNCTION seed_party_type_tags(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS void AS $$
BEGIN
    -- Consultant (TDS 194J - 10%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Consultant', 'party_type', 'Professional consultants - TDS Section 194J (10%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Contractor (TDS 194C - 1%/2%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Contractor', 'party_type', 'Contractors - TDS Section 194C (1-2%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Professional (TDS 194J - 10%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Professional', 'party_type', 'Professionals (CA, Lawyer, Doctor) - TDS Section 194J (10%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rent (TDS 194I - 10%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Rent', 'party_type', 'Rent/Lease payments - TDS Section 194I (10%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Commission Agent (TDS 194H - 5%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Commission Agent', 'party_type', 'Commission agents/brokers - TDS Section 194H (5%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Interest Payer (TDS 194A - 10%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Interest Payer', 'party_type', 'Interest payments - TDS Section 194A (10%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Goods Supplier (TDS 194Q - 0.1%)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Goods Supplier', 'party_type', 'Goods suppliers (above Rs.50L) - TDS Section 194Q (0.1%)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Service Provider (General - may or may not have TDS)
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Service Provider', 'party_type', 'General service providers', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Government
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Government', 'party_type', 'Government entities (no TDS)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- MSME Vendor
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'MSME Vendor', 'party_type', 'MSME registered vendors (45-day payment rule)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Employee
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Employee', 'party_type', 'Employees - TDS per slab rates', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Related Party
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Related Party', 'party_type', 'Related party transactions (disclosure required)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Importer
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Importer', 'party_type', 'Import suppliers (Form 15CA/CB)', true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Exporter
    INSERT INTO tags (company_id, name, tag_group, description, is_active, created_by)
    VALUES (p_company_id, 'Exporter', 'party_type', 'Export customers (LUT/Bond)', true, p_created_by)
    ON CONFLICT DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- PHASE 8: SEED DEFAULT TDS RULES
-- Create default TDS section rules based on party tags and Tally groups
-- ============================================================================

-- Function to seed default TDS rules for a company
CREATE OR REPLACE FUNCTION seed_default_tds_rules(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS void AS $$
DECLARE
    v_tag_id UUID;
BEGIN
    -- Rule 1: Consultants (194J - 10%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Consultant' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Consultant TDS', v_tag_id, 'CONSULTANTS', '194J', 10.0, 20.0, 30000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 2: Contractors (194C - 2% for company, 1% for individual)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Contractor' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, single_payment_threshold, priority, created_by)
    VALUES (p_company_id, 'Contractor TDS', v_tag_id, 'CONTRACTORS', '194C', 2.0, 20.0, 100000, 30000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 3: Professionals (194J - 10%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Professional' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Professional TDS', v_tag_id, 'Professional Fees', '194J', 10.0, 20.0, 30000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 4: Rent (194I - 10%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Rent' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Rent TDS', v_tag_id, 'Rent Payable', '194I', 10.0, 20.0, 240000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 5: Commission (194H - 5%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Commission Agent' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Commission TDS', v_tag_id, 'Commission Payable', '194H', 5.0, 20.0, 15000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 6: Interest (194A - 10%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Interest Payer' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Interest TDS', v_tag_id, 'Interest Payable', '194A', 10.0, 20.0, 40000, 10, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Rule 7: Goods Supplier (194Q - 0.1%)
    SELECT id INTO v_tag_id FROM tags WHERE company_id = p_company_id AND name = 'Goods Supplier' AND tag_group = 'party_type';
    INSERT INTO tds_section_rules (company_id, name, tag_id, tds_section, tds_rate, tds_rate_no_pan, threshold_amount, priority, created_by)
    VALUES (p_company_id, 'Goods Purchase TDS', v_tag_id, '194Q', 0.1, 5.0, 5000000, 20, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Additional Tally group mappings (without tag, just by group name)
    INSERT INTO tds_section_rules (company_id, name, tally_group_name, tds_section, tds_rate, tds_rate_no_pan, priority, created_by)
    VALUES
        (p_company_id, 'Consultants (Tally)', 'Consultants', '194J', 10.0, 20.0, 50, p_created_by),
        (p_company_id, 'Contractors (Tally)', 'Contractors', '194C', 2.0, 20.0, 50, p_created_by),
        (p_company_id, 'Professional Fees (Tally)', 'PROFESSIONAL FEES', '194J', 10.0, 20.0, 50, p_created_by),
        (p_company_id, 'Rent (Tally)', 'RENT PAYABLE', '194I', 10.0, 20.0, 50, p_created_by)
    ON CONFLICT DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- PHASE 9: UPDATE TALLY FIELD MAPPINGS
-- Add CONSULTANTS and similar groups to vendor mapping
-- ============================================================================

-- Update the seed_tally_default_mappings function to include more vendor groups
CREATE OR REPLACE FUNCTION seed_tally_default_mappings(p_company_id UUID)
RETURNS void AS $$
BEGIN
    -- ==================== VENDOR MAPPINGS (Sundry Creditors and sub-groups) ====================

    -- Sundry Creditors -> Vendors (main group)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- CONSULTANTS -> Vendors (TDS 194J)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'CONSULTANTS', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Consultants', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- CONTRACTORS -> Vendors (TDS 194C)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'CONTRACTORS', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Contractors', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Professional Fees -> Vendors (TDS 194J)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'PROFESSIONAL FEES', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Professional Fees', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Rent Payable -> Vendors (TDS 194I)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'RENT PAYABLE', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Rent Payable', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Commission Payable -> Vendors (TDS 194H)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'COMMISSION PAYABLE', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Commission Payable', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Interest Payable -> Vendors (TDS 194A)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'INTEREST PAYABLE', 'vendors', true, 10),
        (p_company_id, 'ledger_group', 'Interest Payable', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Office Space Sellers -> Vendors
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Office Space Sellers', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Manpower Expenses -> Vendors
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Manpower Expenses', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ==================== CUSTOMER MAPPINGS ====================

    -- Sundry Debtors -> Customers
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ==================== BANK MAPPINGS ====================

    -- Bank Accounts -> Bank Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank Accounts', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Bank OD A/c -> Bank Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OD A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ==================== CHART OF ACCOUNTS MAPPINGS ====================

    -- Cash-in-hand -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Cash-in-hand', '', 'chart_of_accounts', 'asset', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Purchase Accounts -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Purchase Accounts', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Sales Accounts -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sales Accounts', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Direct Expenses -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Indirect Expenses -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Direct Incomes -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Indirect Incomes -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Duties & Taxes -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Duties & Taxes', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Fixed Assets -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Fixed Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Investments -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Investments', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Current Assets -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Current Liabilities -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Liabilities', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Loans (Liability) -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans (Liability)', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Loans & Advances (Asset) -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans & Advances (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Capital Account -> Chart of Accounts (Equity)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Capital Account', '', 'chart_of_accounts', 'equity', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Reserves & Surplus -> Chart of Accounts (Equity)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Reserves & Surplus', '', 'chart_of_accounts', 'equity', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Stock-in-hand -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stock-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Provisions -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Provisions', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ==================== GST/TAX MAPPINGS (to COA, not suspense) ====================

    -- Input Tax -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input Tax', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Output Tax -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output Tax', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Good and Service Tax -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Good and Service Tax', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- TDS -> Chart of Accounts (Asset/Liability based on context)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ==================== SUSPENSE (only for truly unmapped) ====================

    -- Suspense A/c -> Suspense (special handling)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Suspense A/c', '', 'suspense', true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Branch / Divisions -> Chart of Accounts (for now)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Branch / Divisions', '', 'chart_of_accounts', 'asset', true, 30)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- PHASE 10: TRIGGERS FOR AUDIT
-- ============================================================================

-- Update timestamp trigger for parties
CREATE OR REPLACE FUNCTION update_parties_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_parties_updated_at
    BEFORE UPDATE ON parties
    FOR EACH ROW
    EXECUTE FUNCTION update_parties_updated_at();

CREATE TRIGGER trg_party_vendor_profiles_updated_at
    BEFORE UPDATE ON party_vendor_profiles
    FOR EACH ROW
    EXECUTE FUNCTION update_parties_updated_at();

CREATE TRIGGER trg_party_customer_profiles_updated_at
    BEFORE UPDATE ON party_customer_profiles
    FOR EACH ROW
    EXECUTE FUNCTION update_parties_updated_at();

-- ============================================================================
-- PHASE 11: COMMENTS
-- ============================================================================

COMMENT ON FUNCTION seed_party_type_tags IS 'Seeds default party classification tags (Consultant, Contractor, etc.) for a company';
COMMENT ON FUNCTION seed_default_tds_rules IS 'Seeds default TDS section rules based on party tags and Tally groups';
COMMENT ON FUNCTION seed_tally_default_mappings IS 'Seeds default Tally ledger group to entity mappings - now includes CONSULTANTS, CONTRACTORS, etc.';
