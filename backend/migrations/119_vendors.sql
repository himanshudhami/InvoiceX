-- Migration: Vendors/Suppliers Table
-- Purpose: Create vendors table for Accounts Payable tracking (Sundry Creditors in Tally)
-- Mirrors customers table with additional vendor-specific fields (TDS, MSME, Bank details)

-- ============================================
-- VENDORS TABLE
-- ============================================
-- Core vendor/supplier master data

CREATE TABLE IF NOT EXISTS vendors (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,

    -- Basic Information
    name VARCHAR(255) NOT NULL,
    company_name VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    address_line_1 VARCHAR(500),
    address_line_2 VARCHAR(500),
    city VARCHAR(100),
    state VARCHAR(100),
    zip_code VARCHAR(20),
    country VARCHAR(100) DEFAULT 'India',
    tax_number VARCHAR(50),
    notes TEXT,
    credit_limit DECIMAL(18,2),
    payment_terms INTEGER, -- days
    is_active BOOLEAN DEFAULT TRUE,

    -- ==================== Indian GST Compliance ====================

    -- GSTIN (15 characters: 2 state + 10 PAN + 1 entity + 1 check + 1 default)
    gstin VARCHAR(20),

    -- GST State Code (first 2 digits of GSTIN)
    gst_state_code VARCHAR(5),

    -- Vendor type for GST classification
    -- b2b: GST registered, b2c: unregistered, import: overseas, rcm_applicable: reverse charge
    vendor_type VARCHAR(30),

    -- Whether vendor is GST registered
    is_gst_registered BOOLEAN DEFAULT FALSE,

    -- PAN Number (10 characters) - Required for TDS
    pan_number VARCHAR(15),

    -- ==================== TDS Compliance ====================

    -- TAN Number (Tax Deduction Account Number)
    tan_number VARCHAR(15),

    -- Default TDS Section: 194C, 194J, 194H, 194I, 194A, 194Q
    default_tds_section VARCHAR(10),

    -- Default TDS Rate percentage
    default_tds_rate DECIMAL(5,2),

    -- Whether TDS is applicable for payments to this vendor
    tds_applicable BOOLEAN DEFAULT FALSE,

    -- Lower/Nil TDS Certificate Number
    lower_tds_certificate VARCHAR(50),

    -- Lower TDS Rate from certificate
    lower_tds_rate DECIMAL(5,2),

    -- Certificate validity end date
    lower_tds_certificate_valid_till DATE,

    -- ==================== MSME Compliance ====================

    -- Whether vendor is MSME registered
    msme_registered BOOLEAN DEFAULT FALSE,

    -- Udyam Registration Number (UDYAM-XX-00-0000000)
    msme_registration_number VARCHAR(30),

    -- MSME Category: micro, small, medium
    msme_category VARCHAR(20),

    -- ==================== Bank Details ====================

    -- Bank account number
    bank_account_number VARCHAR(30),

    -- IFSC Code for NEFT/RTGS
    bank_ifsc_code VARCHAR(15),

    -- Bank name
    bank_name VARCHAR(100),

    -- Branch name
    bank_branch VARCHAR(100),

    -- Account holder name
    bank_account_holder_name VARCHAR(255),

    -- ==================== Default Accounts ====================

    -- Default expense account for purchases
    default_expense_account_id UUID REFERENCES chart_of_accounts(id),

    -- Default payables account (usually Trade Payables)
    default_payable_account_id UUID REFERENCES chart_of_accounts(id),

    -- ==================== Tally Migration ====================

    -- Original Tally Ledger GUID
    tally_ledger_guid VARCHAR(100),

    -- Original Tally Ledger Name
    tally_ledger_name VARCHAR(255),

    -- ==================== Timestamps ====================

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- ==================== Constraints ====================

    CONSTRAINT chk_vendor_type CHECK (vendor_type IS NULL OR vendor_type IN ('b2b', 'b2c', 'import', 'rcm_applicable')),
    CONSTRAINT chk_tds_section CHECK (default_tds_section IS NULL OR default_tds_section IN ('194A', '194C', '194H', '194I', '194J', '194Q', '194R', '194S')),
    CONSTRAINT chk_msme_category CHECK (msme_category IS NULL OR msme_category IN ('micro', 'small', 'medium'))
);

-- ============================================
-- INDEXES
-- ============================================

CREATE INDEX IF NOT EXISTS idx_vendors_company_id ON vendors(company_id);
CREATE INDEX IF NOT EXISTS idx_vendors_gstin ON vendors(gstin) WHERE gstin IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_vendors_pan ON vendors(pan_number) WHERE pan_number IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_vendors_name ON vendors(company_id, name);
CREATE INDEX IF NOT EXISTS idx_vendors_msme ON vendors(company_id, msme_registered) WHERE msme_registered = TRUE;
CREATE INDEX IF NOT EXISTS idx_vendors_tds_applicable ON vendors(company_id, tds_applicable) WHERE tds_applicable = TRUE;
CREATE INDEX IF NOT EXISTS idx_vendors_is_active ON vendors(company_id, is_active);
CREATE INDEX IF NOT EXISTS idx_vendors_tally_guid ON vendors(tally_ledger_guid) WHERE tally_ledger_guid IS NOT NULL;

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE vendors IS 'Vendor/Supplier master data for Accounts Payable (Sundry Creditors in Tally)';
COMMENT ON COLUMN vendors.gstin IS 'GST Identification Number - 15 character alphanumeric';
COMMENT ON COLUMN vendors.vendor_type IS 'b2b: GST registered, b2c: unregistered, import: overseas supplier, rcm_applicable: reverse charge';
COMMENT ON COLUMN vendors.default_tds_section IS 'Default TDS section for payments: 194C (Contractors), 194J (Professional), 194H (Commission), etc.';
COMMENT ON COLUMN vendors.msme_category IS 'MSME classification: micro, small, medium - affects payment terms under MSME Act';
COMMENT ON COLUMN vendors.tally_ledger_guid IS 'Original Tally GUID for migration tracking and deduplication';

-- ============================================
-- TRIGGER FOR updated_at
-- ============================================

CREATE OR REPLACE FUNCTION update_vendors_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_vendors_updated_at ON vendors;
CREATE TRIGGER trg_vendors_updated_at
    BEFORE UPDATE ON vendors
    FOR EACH ROW
    EXECUTE FUNCTION update_vendors_updated_at();
