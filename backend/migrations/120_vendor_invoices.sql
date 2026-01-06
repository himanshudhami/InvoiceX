-- Migration: Vendor Invoices (Purchase Bills)
-- Purpose: Create vendor invoice tables for Accounts Payable tracking (Purchase voucher in Tally)
-- Includes full GST/ITC compliance and TDS deduction tracking

-- ============================================
-- VENDOR INVOICES TABLE
-- ============================================
-- Purchase bills/invoices from vendors

CREATE TABLE IF NOT EXISTS vendor_invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    vendor_id UUID REFERENCES vendors(id) ON DELETE SET NULL,

    -- ==================== Invoice Details ====================

    -- Vendor's invoice number (as printed on their bill)
    invoice_number VARCHAR(100) NOT NULL,

    -- Internal reference number
    internal_reference VARCHAR(100),

    -- Invoice date on vendor's bill
    invoice_date DATE NOT NULL,

    -- Payment due date
    due_date DATE NOT NULL,

    -- Date when bill was received
    received_date DATE,

    -- Status: draft, pending_approval, approved, partially_paid, paid, cancelled
    status VARCHAR(30) DEFAULT 'draft',

    -- ==================== Amounts ====================

    subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    discount_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    paid_amount DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',
    notes TEXT,
    po_number VARCHAR(100),

    -- ==================== GST Classification ====================

    -- Invoice type: purchase_b2b, purchase_import, purchase_rcm, purchase_sez
    invoice_type VARCHAR(30) DEFAULT 'purchase_b2b',

    -- Supply type: intra_state, inter_state, import
    supply_type VARCHAR(30),

    -- Place of supply (state code)
    place_of_supply VARCHAR(5),

    -- Reverse Charge Mechanism
    reverse_charge BOOLEAN DEFAULT FALSE,

    -- RCM applicable for unregistered vendors
    rcm_applicable BOOLEAN DEFAULT FALSE,

    -- ==================== GST Totals ====================

    total_cgst DECIMAL(18,2) DEFAULT 0,
    total_sgst DECIMAL(18,2) DEFAULT 0,
    total_igst DECIMAL(18,2) DEFAULT 0,
    total_cess DECIMAL(18,2) DEFAULT 0,

    -- ==================== ITC (Input Tax Credit) ====================

    -- Whether ITC is eligible
    itc_eligible BOOLEAN DEFAULT TRUE,

    -- Amount of ITC claimed/claimable
    itc_claimed_amount DECIMAL(18,2) DEFAULT 0,

    -- Reason if ITC ineligible: blocked_17_5, missing_gstin, late_filing, not_reflected_gstr2b
    itc_ineligible_reason VARCHAR(50),

    -- GSTR-2B matching
    matched_with_gstr2b BOOLEAN DEFAULT FALSE,
    gstr2b_period VARCHAR(10), -- e.g., '042025' for April 2025

    -- ==================== TDS Deduction ====================

    -- Whether TDS is applicable
    tds_applicable BOOLEAN DEFAULT FALSE,

    -- TDS Section: 194C, 194J, 194H, 194I, 194A, 194Q
    tds_section VARCHAR(10),

    -- TDS Rate
    tds_rate DECIMAL(5,2),

    -- TDS Amount deducted
    tds_amount DECIMAL(18,2) DEFAULT 0,

    -- ==================== Import Fields ====================

    -- Bill of Entry number
    bill_of_entry_number VARCHAR(50),

    -- Bill of Entry date
    bill_of_entry_date DATE,

    -- Port code
    port_code VARCHAR(10),

    -- Foreign currency amount
    foreign_currency_amount DECIMAL(18,2),

    -- Foreign currency code
    foreign_currency VARCHAR(3),

    -- Exchange rate at invoice date
    exchange_rate DECIMAL(18,6) DEFAULT 1,

    -- ==================== Ledger Posting ====================

    -- Whether posted to general ledger
    is_posted BOOLEAN DEFAULT FALSE,

    -- Journal entry ID after posting
    posted_journal_id UUID REFERENCES journal_entries(id),

    -- When posted to ledger
    posted_at TIMESTAMP,

    -- Default expense account
    expense_account_id UUID REFERENCES chart_of_accounts(id),

    -- ==================== Approval Workflow ====================

    -- Who approved this invoice
    approved_by UUID,

    -- When approved
    approved_at TIMESTAMP,

    -- Approval notes
    approval_notes TEXT,

    -- ==================== Tally Migration ====================

    -- Original Tally Voucher GUID
    tally_voucher_guid VARCHAR(100),

    -- Original Tally Voucher Number
    tally_voucher_number VARCHAR(100),

    -- Migration batch ID
    tally_migration_batch_id UUID,

    -- ==================== Timestamps ====================

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- ==================== Constraints ====================

    CONSTRAINT chk_vi_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'partially_paid', 'paid', 'cancelled')),
    CONSTRAINT chk_vi_invoice_type CHECK (invoice_type IN ('purchase_b2b', 'purchase_import', 'purchase_rcm', 'purchase_sez')),
    CONSTRAINT chk_vi_supply_type CHECK (supply_type IS NULL OR supply_type IN ('intra_state', 'inter_state', 'import')),
    CONSTRAINT chk_vi_tds_section CHECK (tds_section IS NULL OR tds_section IN ('194A', '194C', '194H', '194I', '194J', '194Q', '194R', '194S'))
);

-- ============================================
-- VENDOR INVOICE ITEMS TABLE
-- ============================================
-- Line items on vendor invoices

CREATE TABLE IF NOT EXISTS vendor_invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_invoice_id UUID NOT NULL REFERENCES vendor_invoices(id) ON DELETE CASCADE,
    product_id UUID REFERENCES products(id) ON DELETE SET NULL,

    -- ==================== Item Details ====================

    description VARCHAR(500) NOT NULL,
    quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    unit_price DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,2),
    discount_rate DECIMAL(5,2),
    line_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sort_order INTEGER DEFAULT 0,

    -- ==================== GST Compliance ====================

    -- HSN code for goods, SAC code for services
    hsn_sac_code VARCHAR(20),

    -- Whether this is a service (SAC) or goods (HSN)
    is_service BOOLEAN DEFAULT TRUE,

    -- CGST (Central GST) - for intra-state supplies
    cgst_rate DECIMAL(5,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,

    -- SGST (State GST) - for intra-state supplies
    sgst_rate DECIMAL(5,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,

    -- IGST (Integrated GST) - for inter-state supplies
    igst_rate DECIMAL(5,2) DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,

    -- Cess - for specific goods
    cess_rate DECIMAL(5,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,

    -- ==================== ITC Eligibility ====================

    -- Whether ITC is eligible for this line item
    itc_eligible BOOLEAN DEFAULT TRUE,

    -- ITC category: capital_goods, inputs, input_services
    itc_category VARCHAR(30),

    -- Reason if ITC ineligible
    itc_ineligible_reason VARCHAR(100),

    -- ==================== Expense Account ====================

    -- Specific expense account for this line item
    expense_account_id UUID REFERENCES chart_of_accounts(id),

    -- ==================== Cost Center ====================

    -- Cost center allocation
    cost_center_id UUID,

    -- ==================== Timestamps ====================

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- ==================== Constraints ====================

    CONSTRAINT chk_vii_itc_category CHECK (itc_category IS NULL OR itc_category IN ('capital_goods', 'inputs', 'input_services'))
);

-- ============================================
-- INDEXES
-- ============================================

-- Vendor Invoices
CREATE INDEX IF NOT EXISTS idx_vi_company_id ON vendor_invoices(company_id);
CREATE INDEX IF NOT EXISTS idx_vi_vendor_id ON vendor_invoices(vendor_id);
CREATE INDEX IF NOT EXISTS idx_vi_invoice_date ON vendor_invoices(company_id, invoice_date);
CREATE INDEX IF NOT EXISTS idx_vi_due_date ON vendor_invoices(company_id, due_date);
CREATE INDEX IF NOT EXISTS idx_vi_status ON vendor_invoices(company_id, status);
CREATE INDEX IF NOT EXISTS idx_vi_invoice_number ON vendor_invoices(company_id, invoice_number);
CREATE INDEX IF NOT EXISTS idx_vi_is_posted ON vendor_invoices(company_id, is_posted);
CREATE INDEX IF NOT EXISTS idx_vi_itc_eligible ON vendor_invoices(company_id, itc_eligible) WHERE itc_eligible = TRUE;
CREATE INDEX IF NOT EXISTS idx_vi_gstr2b ON vendor_invoices(company_id, matched_with_gstr2b) WHERE matched_with_gstr2b = FALSE;
CREATE INDEX IF NOT EXISTS idx_vi_tally_guid ON vendor_invoices(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- Vendor Invoice Items
CREATE INDEX IF NOT EXISTS idx_vii_vendor_invoice_id ON vendor_invoice_items(vendor_invoice_id);
CREATE INDEX IF NOT EXISTS idx_vii_product_id ON vendor_invoice_items(product_id) WHERE product_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_vii_hsn_sac ON vendor_invoice_items(hsn_sac_code) WHERE hsn_sac_code IS NOT NULL;

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE vendor_invoices IS 'Purchase bills/invoices from vendors for Accounts Payable (Purchase voucher in Tally)';
COMMENT ON COLUMN vendor_invoices.invoice_type IS 'GST classification: purchase_b2b, purchase_import, purchase_rcm, purchase_sez';
COMMENT ON COLUMN vendor_invoices.itc_eligible IS 'Whether Input Tax Credit can be claimed - may be blocked under Section 17(5)';
COMMENT ON COLUMN vendor_invoices.matched_with_gstr2b IS 'Whether this invoice is reflected in GSTR-2B for ITC claim';
COMMENT ON COLUMN vendor_invoices.tally_voucher_guid IS 'Original Tally GUID for migration tracking';

COMMENT ON TABLE vendor_invoice_items IS 'Line items on vendor purchase bills with GST breakdown';
COMMENT ON COLUMN vendor_invoice_items.itc_category IS 'ITC category: capital_goods (spread over 3 years), inputs, input_services';

-- ============================================
-- TRIGGERS FOR updated_at
-- ============================================

CREATE OR REPLACE FUNCTION update_vendor_invoices_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_vendor_invoices_updated_at ON vendor_invoices;
CREATE TRIGGER trg_vendor_invoices_updated_at
    BEFORE UPDATE ON vendor_invoices
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_invoices_updated_at();

CREATE OR REPLACE FUNCTION update_vendor_invoice_items_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_vendor_invoice_items_updated_at ON vendor_invoice_items;
CREATE TRIGGER trg_vendor_invoice_items_updated_at
    BEFORE UPDATE ON vendor_invoice_items
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_invoice_items_updated_at();

-- ============================================
-- TRIGGER TO UPDATE VENDOR INVOICE TOTALS
-- ============================================

CREATE OR REPLACE FUNCTION update_vendor_invoice_totals()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE vendor_invoices
    SET
        subtotal = COALESCE((
            SELECT SUM(line_total)
            FROM vendor_invoice_items
            WHERE vendor_invoice_id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id)
        ), 0),
        total_cgst = COALESCE((
            SELECT SUM(cgst_amount)
            FROM vendor_invoice_items
            WHERE vendor_invoice_id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id)
        ), 0),
        total_sgst = COALESCE((
            SELECT SUM(sgst_amount)
            FROM vendor_invoice_items
            WHERE vendor_invoice_id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id)
        ), 0),
        total_igst = COALESCE((
            SELECT SUM(igst_amount)
            FROM vendor_invoice_items
            WHERE vendor_invoice_id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id)
        ), 0),
        total_cess = COALESCE((
            SELECT SUM(cess_amount)
            FROM vendor_invoice_items
            WHERE vendor_invoice_id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id)
        ), 0),
        updated_at = NOW()
    WHERE id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id);

    -- Update total_amount (subtotal + GST - discount)
    UPDATE vendor_invoices
    SET
        total_amount = subtotal + total_cgst + total_sgst + total_igst + total_cess - COALESCE(discount_amount, 0),
        tax_amount = total_cgst + total_sgst + total_igst + total_cess
    WHERE id = COALESCE(NEW.vendor_invoice_id, OLD.vendor_invoice_id);

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_vendor_invoice_totals ON vendor_invoice_items;
CREATE TRIGGER trg_update_vendor_invoice_totals
    AFTER INSERT OR UPDATE OR DELETE ON vendor_invoice_items
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_invoice_totals();
