-- ============================================================================
-- Migration 129: Recreate Invoice and Payment Tables with party_id
-- Replaces customer_id with party_id (references parties where is_customer=true)
-- Replaces vendor_id with party_id (references parties where is_vendor=true)
-- ============================================================================

-- ============================================================================
-- PHASE 1: CUSTOMER INVOICES (Sales Invoices)
-- ============================================================================

CREATE TABLE IF NOT EXISTS invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id),  -- Customer party (is_customer = true)

    -- Invoice Details
    invoice_number VARCHAR(50) NOT NULL,
    invoice_date DATE NOT NULL,
    due_date DATE NOT NULL,
    status VARCHAR(30) DEFAULT 'draft',

    -- Amounts
    subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    discount_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    paid_amount DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',
    notes TEXT,
    terms TEXT,
    payment_instructions TEXT,
    po_number VARCHAR(100),
    project_name VARCHAR(255),

    -- Timestamps
    sent_at TIMESTAMP WITH TIME ZONE,
    viewed_at TIMESTAMP WITH TIME ZONE,
    paid_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- GST Classification
    invoice_type VARCHAR(30) DEFAULT 'export',
    supply_type VARCHAR(30),
    place_of_supply VARCHAR(10),
    reverse_charge BOOLEAN DEFAULT false,

    -- GST Totals
    total_cgst DECIMAL(18,2) DEFAULT 0,
    total_sgst DECIMAL(18,2) DEFAULT 0,
    total_igst DECIMAL(18,2) DEFAULT 0,
    total_cess DECIMAL(18,2) DEFAULT 0,

    -- E-invoicing
    e_invoice_applicable BOOLEAN DEFAULT false,
    e_invoice_irn VARCHAR(100),
    e_invoice_ack_number VARCHAR(50),
    e_invoice_ack_date TIMESTAMP WITH TIME ZONE,
    e_invoice_qr_code TEXT,
    e_invoice_signed_json TEXT,
    e_invoice_status VARCHAR(30) DEFAULT 'not_applicable',
    e_invoice_cancel_date TIMESTAMP WITH TIME ZONE,
    e_invoice_cancel_reason TEXT,

    -- E-way bill
    eway_bill_number VARCHAR(50),
    eway_bill_date TIMESTAMP WITH TIME ZONE,
    eway_bill_valid_until TIMESTAMP WITH TIME ZONE,

    -- Export fields
    export_type VARCHAR(10),
    port_code VARCHAR(10),
    shipping_bill_number VARCHAR(50),
    shipping_bill_date DATE,
    export_duty DECIMAL(18,2) DEFAULT 0,
    foreign_currency VARCHAR(3),
    exchange_rate DECIMAL(18,6),
    foreign_currency_amount DECIMAL(18,2),

    -- Forex accounting
    invoice_exchange_rate DECIMAL(18,6),
    invoice_amount_inr DECIMAL(18,2),

    -- LUT
    lut_number VARCHAR(50),
    lut_valid_from DATE,
    lut_valid_to DATE,

    -- FEMA
    purpose_code VARCHAR(10),
    ad_bank_name VARCHAR(255),
    realization_due_date DATE,

    -- Ledger posting
    is_posted BOOLEAN DEFAULT false,
    posted_journal_id UUID,
    posted_at TIMESTAMP WITH TIME ZONE,

    -- SEZ
    sez_category VARCHAR(10),

    -- B2C
    b2c_large BOOLEAN DEFAULT false,

    -- Shipping
    shipping_address TEXT,
    transporter_name VARCHAR(255),
    vehicle_number VARCHAR(50),

    -- Tally Migration
    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_voucher_type VARCHAR(50),
    tally_migration_batch_id UUID
);

CREATE INDEX idx_invoices_company ON invoices(company_id);
CREATE INDEX idx_invoices_party ON invoices(party_id);
CREATE INDEX idx_invoices_status ON invoices(company_id, status);
CREATE INDEX idx_invoices_date ON invoices(company_id, invoice_date);
CREATE INDEX idx_invoices_tally ON invoices(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- ============================================================================
-- PHASE 2: INVOICE ITEMS
-- ============================================================================

CREATE TABLE IF NOT EXISTS invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,
    product_id UUID,
    description TEXT NOT NULL,
    quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    unit_price DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,2) DEFAULT 0,
    discount_rate DECIMAL(5,2) DEFAULT 0,
    line_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sort_order INT DEFAULT 0,

    -- GST
    hsn_sac_code VARCHAR(10),
    is_service BOOLEAN DEFAULT false,
    cgst_rate DECIMAL(5,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,
    sgst_rate DECIMAL(5,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,
    igst_rate DECIMAL(5,2) DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,
    cess_rate DECIMAL(5,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_invoice_items_invoice ON invoice_items(invoice_id);

-- ============================================================================
-- PHASE 3: CUSTOMER PAYMENTS (Receipts)
-- ============================================================================

CREATE TABLE IF NOT EXISTS payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID REFERENCES invoices(id),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id),  -- Customer party (is_customer = true)

    -- Payment Details
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    amount_in_inr DECIMAL(18,2),
    currency VARCHAR(3) DEFAULT 'INR',
    payment_method VARCHAR(30),
    reference_number VARCHAR(100),
    notes TEXT,
    description TEXT,

    -- Payment Classification
    payment_type VARCHAR(30),
    income_category VARCHAR(50),

    -- TDS Tracking
    tds_applicable BOOLEAN DEFAULT false,
    tds_section VARCHAR(10),
    tds_rate DECIMAL(5,2),
    tds_amount DECIMAL(18,2),
    gross_amount DECIMAL(18,2),

    -- Financial Year
    financial_year VARCHAR(10),

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Tally Migration
    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_migration_batch_id UUID
);

CREATE INDEX idx_payments_company ON payments(company_id);
CREATE INDEX idx_payments_party ON payments(party_id);
CREATE INDEX idx_payments_invoice ON payments(invoice_id);
CREATE INDEX idx_payments_date ON payments(company_id, payment_date);
CREATE INDEX idx_payments_tally ON payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- ============================================================================
-- PHASE 4: VENDOR INVOICES (Purchase Bills)
-- ============================================================================

CREATE TABLE IF NOT EXISTS vendor_invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id),  -- Vendor party (is_vendor = true)

    -- Invoice Details
    invoice_number VARCHAR(100) NOT NULL,
    internal_reference VARCHAR(100),
    invoice_date DATE NOT NULL,
    due_date DATE NOT NULL,
    received_date DATE,
    status VARCHAR(30) DEFAULT 'draft',

    -- Amounts
    subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    discount_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    paid_amount DECIMAL(18,2) DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',
    notes TEXT,
    po_number VARCHAR(100),

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- GST Classification
    invoice_type VARCHAR(30) DEFAULT 'purchase_b2b',
    supply_type VARCHAR(30),
    place_of_supply VARCHAR(10),
    reverse_charge BOOLEAN DEFAULT false,
    rcm_applicable BOOLEAN DEFAULT false,

    -- GST Totals
    total_cgst DECIMAL(18,2) DEFAULT 0,
    total_sgst DECIMAL(18,2) DEFAULT 0,
    total_igst DECIMAL(18,2) DEFAULT 0,
    total_cess DECIMAL(18,2) DEFAULT 0,

    -- ITC
    itc_eligible BOOLEAN DEFAULT true,
    itc_claimed_amount DECIMAL(18,2) DEFAULT 0,
    itc_ineligible_reason VARCHAR(50),
    matched_with_gstr2b BOOLEAN DEFAULT false,
    gstr2b_period VARCHAR(10),

    -- TDS
    tds_applicable BOOLEAN DEFAULT false,
    tds_section VARCHAR(10),
    tds_rate DECIMAL(5,2),
    tds_amount DECIMAL(18,2),

    -- Import fields
    bill_of_entry_number VARCHAR(50),
    bill_of_entry_date DATE,
    port_code VARCHAR(10),
    foreign_currency_amount DECIMAL(18,2),
    foreign_currency VARCHAR(3),
    exchange_rate DECIMAL(18,6),

    -- Ledger posting
    is_posted BOOLEAN DEFAULT false,
    posted_journal_id UUID,
    posted_at TIMESTAMP WITH TIME ZONE,
    expense_account_id UUID,

    -- Approval
    approved_by UUID,
    approved_at TIMESTAMP WITH TIME ZONE,
    approval_notes TEXT,

    -- Tally Migration
    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_migration_batch_id UUID
);

CREATE INDEX idx_vendor_invoices_company ON vendor_invoices(company_id);
CREATE INDEX idx_vendor_invoices_party ON vendor_invoices(party_id);
CREATE INDEX idx_vendor_invoices_status ON vendor_invoices(company_id, status);
CREATE INDEX idx_vendor_invoices_date ON vendor_invoices(company_id, invoice_date);
CREATE INDEX idx_vendor_invoices_tally ON vendor_invoices(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- ============================================================================
-- PHASE 5: VENDOR INVOICE ITEMS
-- ============================================================================

CREATE TABLE IF NOT EXISTS vendor_invoice_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_invoice_id UUID NOT NULL REFERENCES vendor_invoices(id) ON DELETE CASCADE,
    product_id UUID,
    description TEXT NOT NULL,
    quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    unit_price DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,2) DEFAULT 0,
    discount_rate DECIMAL(5,2) DEFAULT 0,
    line_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sort_order INT DEFAULT 0,

    -- GST
    hsn_sac_code VARCHAR(10),
    is_service BOOLEAN DEFAULT false,
    cgst_rate DECIMAL(5,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,
    sgst_rate DECIMAL(5,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,
    igst_rate DECIMAL(5,2) DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,
    cess_rate DECIMAL(5,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,

    -- ITC
    itc_eligible BOOLEAN DEFAULT true,
    itc_category VARCHAR(30),

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_vendor_invoice_items_invoice ON vendor_invoice_items(vendor_invoice_id);

-- ============================================================================
-- PHASE 6: VENDOR PAYMENTS
-- ============================================================================

CREATE TABLE IF NOT EXISTS vendor_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id),  -- Vendor party (is_vendor = true)
    bank_account_id UUID,

    -- Payment Details
    payment_date DATE NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    gross_amount DECIMAL(18,2),
    amount_in_inr DECIMAL(18,2),
    currency VARCHAR(3) DEFAULT 'INR',
    payment_method VARCHAR(30),
    reference_number VARCHAR(100),
    cheque_number VARCHAR(50),
    cheque_date DATE,
    notes TEXT,
    description TEXT,

    -- Payment Classification
    payment_type VARCHAR(30),
    status VARCHAR(30) DEFAULT 'draft',

    -- TDS
    tds_applicable BOOLEAN DEFAULT false,
    tds_section VARCHAR(10),
    tds_rate DECIMAL(5,2),
    tds_amount DECIMAL(18,2),
    tds_deposited BOOLEAN DEFAULT false,
    tds_challan_number VARCHAR(50),
    tds_deposit_date DATE,

    -- Financial Year
    financial_year VARCHAR(10),

    -- Ledger posting
    is_posted BOOLEAN DEFAULT false,
    posted_journal_id UUID,
    posted_at TIMESTAMP WITH TIME ZONE,

    -- Bank Reconciliation
    bank_transaction_id UUID,
    is_reconciled BOOLEAN DEFAULT false,
    reconciled_at TIMESTAMP WITH TIME ZONE,

    -- Approval
    approved_by UUID,
    approved_at TIMESTAMP WITH TIME ZONE,

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Tally Migration
    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_migration_batch_id UUID
);

CREATE INDEX idx_vendor_payments_company ON vendor_payments(company_id);
CREATE INDEX idx_vendor_payments_party ON vendor_payments(party_id);
CREATE INDEX idx_vendor_payments_status ON vendor_payments(company_id, status);
CREATE INDEX idx_vendor_payments_date ON vendor_payments(company_id, payment_date);
CREATE INDEX idx_vendor_payments_tally ON vendor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- ============================================================================
-- PHASE 7: VENDOR PAYMENT ALLOCATIONS
-- ============================================================================

CREATE TABLE IF NOT EXISTS vendor_payment_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_payment_id UUID NOT NULL REFERENCES vendor_payments(id) ON DELETE CASCADE,
    vendor_invoice_id UUID REFERENCES vendor_invoices(id),
    allocated_amount DECIMAL(18,2) NOT NULL,
    tds_allocated DECIMAL(18,2) DEFAULT 0,
    allocation_type VARCHAR(30) DEFAULT 'bill_settlement',
    tally_bill_ref VARCHAR(255),

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_vendor_payment_allocations_payment ON vendor_payment_allocations(vendor_payment_id);
CREATE INDEX idx_vendor_payment_allocations_invoice ON vendor_payment_allocations(vendor_invoice_id);

-- ============================================================================
-- PHASE 8: RECREATE QUOTES TABLE WITH PARTY_ID
-- ============================================================================

-- Drop old quotes if exists
DROP TABLE IF EXISTS quote_items CASCADE;
DROP TABLE IF EXISTS quotes CASCADE;

CREATE TABLE IF NOT EXISTS quotes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id),  -- Customer party

    -- Quote Details
    quote_number VARCHAR(50) NOT NULL,
    quote_date DATE NOT NULL,
    valid_until DATE,
    status VARCHAR(30) DEFAULT 'draft',

    -- Amounts
    subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
    tax_amount DECIMAL(18,2) DEFAULT 0,
    discount_amount DECIMAL(18,2) DEFAULT 0,
    total_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',
    notes TEXT,
    terms TEXT,

    -- Converted to Invoice
    converted_to_invoice_id UUID REFERENCES invoices(id),
    converted_at TIMESTAMP WITH TIME ZONE,

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_quotes_company ON quotes(company_id);
CREATE INDEX idx_quotes_party ON quotes(party_id);
CREATE INDEX idx_quotes_status ON quotes(company_id, status);

CREATE TABLE IF NOT EXISTS quote_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quote_id UUID NOT NULL REFERENCES quotes(id) ON DELETE CASCADE,
    product_id UUID,
    description TEXT NOT NULL,
    quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    unit_price DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_rate DECIMAL(5,2) DEFAULT 0,
    discount_rate DECIMAL(5,2) DEFAULT 0,
    line_total DECIMAL(18,2) NOT NULL DEFAULT 0,
    sort_order INT DEFAULT 0,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_quote_items_quote ON quote_items(quote_id);

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON COLUMN invoices.party_id IS 'Customer party (references parties where is_customer = true)';
COMMENT ON COLUMN payments.party_id IS 'Customer party (references parties where is_customer = true)';
COMMENT ON COLUMN vendor_invoices.party_id IS 'Vendor party (references parties where is_vendor = true)';
COMMENT ON COLUMN vendor_payments.party_id IS 'Vendor party (references parties where is_vendor = true)';
COMMENT ON COLUMN quotes.party_id IS 'Customer party (references parties where is_customer = true)';
