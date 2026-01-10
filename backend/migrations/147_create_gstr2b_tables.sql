-- Migration: 147_create_gstr2b_tables
-- Description: Create tables for GSTR-2B ingestion and reconciliation
-- Date: 2025-01-10

-- ==================== GSTR-2B Imports Table ====================
-- Stores metadata about each GSTR-2B file import
CREATE TABLE IF NOT EXISTS gstr2b_imports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    return_period VARCHAR(20) NOT NULL,  -- Format: 'Jan-2025'
    gstin VARCHAR(15) NOT NULL,

    -- Import source
    import_source VARCHAR(20) NOT NULL DEFAULT 'file_upload',  -- file_upload, api, manual
    file_name VARCHAR(255),
    file_hash VARCHAR(64),  -- SHA-256 hash for deduplication

    -- Import status
    import_status VARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending, processing, completed, failed
    error_message TEXT,

    -- Summary counts
    total_invoices INTEGER DEFAULT 0,
    matched_invoices INTEGER DEFAULT 0,
    unmatched_invoices INTEGER DEFAULT 0,
    partially_matched_invoices INTEGER DEFAULT 0,

    -- ITC Summary
    total_itc_igst DECIMAL(18,2) DEFAULT 0,
    total_itc_cgst DECIMAL(18,2) DEFAULT 0,
    total_itc_sgst DECIMAL(18,2) DEFAULT 0,
    total_itc_cess DECIMAL(18,2) DEFAULT 0,
    matched_itc_amount DECIMAL(18,2) DEFAULT 0,

    -- Raw data storage
    raw_json JSONB,

    -- Audit
    imported_by UUID REFERENCES users(id),
    imported_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMP WITH TIME ZONE,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_gstr2b_import_company_period_hash UNIQUE (company_id, return_period, file_hash)
);

-- ==================== GSTR-2B Invoices Table ====================
-- Stores individual invoice records from GSTR-2B
CREATE TABLE IF NOT EXISTS gstr2b_invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    import_id UUID NOT NULL REFERENCES gstr2b_imports(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id),
    return_period VARCHAR(20) NOT NULL,

    -- Supplier details
    supplier_gstin VARCHAR(15) NOT NULL,
    supplier_name VARCHAR(255),
    supplier_trade_name VARCHAR(255),

    -- Invoice details
    invoice_number VARCHAR(50) NOT NULL,
    invoice_date DATE NOT NULL,
    invoice_type VARCHAR(20),  -- B2B, B2BA, CDNR, CDNRA, ISD, ISDA, IMPG, IMPGSEZ
    document_type VARCHAR(20),  -- Invoice, Credit Note, Debit Note

    -- Amounts
    taxable_value DECIMAL(18,2) NOT NULL DEFAULT 0,
    igst_amount DECIMAL(18,2) DEFAULT 0,
    cgst_amount DECIMAL(18,2) DEFAULT 0,
    sgst_amount DECIMAL(18,2) DEFAULT 0,
    cess_amount DECIMAL(18,2) DEFAULT 0,
    total_gst DECIMAL(18,2) GENERATED ALWAYS AS (igst_amount + cgst_amount + sgst_amount + cess_amount) STORED,
    total_invoice_value DECIMAL(18,2) DEFAULT 0,

    -- ITC eligibility
    itc_eligible BOOLEAN DEFAULT true,
    itc_igst DECIMAL(18,2) DEFAULT 0,
    itc_cgst DECIMAL(18,2) DEFAULT 0,
    itc_sgst DECIMAL(18,2) DEFAULT 0,
    itc_cess DECIMAL(18,2) DEFAULT 0,

    -- Place of supply
    place_of_supply VARCHAR(2),  -- State code
    supply_type VARCHAR(20),  -- intra_state, inter_state
    reverse_charge BOOLEAN DEFAULT false,

    -- Matching
    match_status VARCHAR(20) DEFAULT 'pending',  -- pending, matched, partial_match, unmatched, accepted, rejected
    matched_vendor_invoice_id UUID REFERENCES vendor_invoices(id),
    match_confidence INTEGER,  -- 0-100 percentage
    match_details JSONB,  -- Stores match algorithm details
    match_discrepancies JSONB,  -- Stores any discrepancies found

    -- User actions
    action_status VARCHAR(20),  -- accepted, rejected, pending_review
    action_by UUID REFERENCES users(id),
    action_at TIMESTAMP WITH TIME ZONE,
    action_notes TEXT,

    -- Source tracking (for amendments)
    original_invoice_number VARCHAR(50),
    original_invoice_date DATE,
    amendment_type VARCHAR(20),  -- original, amended

    -- Raw JSON from 2B
    raw_json JSONB,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ==================== Reconciliation Rules Table ====================
-- Configurable matching rules
CREATE TABLE IF NOT EXISTS gstr2b_reconciliation_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id),  -- NULL means global rule

    rule_name VARCHAR(100) NOT NULL,
    rule_code VARCHAR(50) NOT NULL UNIQUE,
    priority INTEGER NOT NULL DEFAULT 100,
    is_active BOOLEAN DEFAULT true,

    -- Match criteria
    match_gstin BOOLEAN DEFAULT true,
    match_invoice_number BOOLEAN DEFAULT true,
    match_invoice_date BOOLEAN DEFAULT false,
    match_amount BOOLEAN DEFAULT true,

    -- Tolerances
    invoice_number_fuzzy_threshold INTEGER DEFAULT 2,  -- Levenshtein distance
    date_tolerance_days INTEGER DEFAULT 3,
    amount_tolerance_percentage DECIMAL(5,2) DEFAULT 1.00,  -- 1%
    amount_tolerance_absolute DECIMAL(18,2) DEFAULT 100.00,  -- Rs. 100

    -- Confidence scores
    confidence_score INTEGER NOT NULL,  -- Score when this rule matches

    description TEXT,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Insert default reconciliation rules
INSERT INTO gstr2b_reconciliation_rules (rule_code, rule_name, priority, confidence_score, description,
    match_gstin, match_invoice_number, match_invoice_date, match_amount,
    invoice_number_fuzzy_threshold, date_tolerance_days, amount_tolerance_percentage)
VALUES
    ('EXACT_MATCH', 'Exact Match', 1, 100,
     'GSTIN + Invoice Number + Exact Amount match',
     true, true, false, true, 0, 0, 0),

    ('FUZZY_INVOICE', 'Fuzzy Invoice Number', 2, 90,
     'GSTIN + Fuzzy Invoice Number (Levenshtein <= 2) + Amount match',
     true, true, false, true, 2, 0, 0),

    ('AMOUNT_TOLERANCE', 'Amount Tolerance Match', 3, 85,
     'GSTIN + Invoice Number + Amount within 1%',
     true, true, false, true, 0, 0, 1.00),

    ('DATE_TOLERANCE', 'Date Tolerance Match', 4, 80,
     'GSTIN + Invoice Number + Date within +/- 3 days',
     true, true, true, true, 0, 3, 0),

    ('GSTIN_AMOUNT_ONLY', 'GSTIN + Amount Match', 5, 70,
     'GSTIN + Amount match (for missing invoice numbers)',
     true, false, false, true, 0, 0, 0.50);

-- ==================== Indexes ====================

-- Imports
CREATE INDEX idx_gstr2b_imports_company ON gstr2b_imports(company_id);
CREATE INDEX idx_gstr2b_imports_period ON gstr2b_imports(return_period);
CREATE INDEX idx_gstr2b_imports_status ON gstr2b_imports(import_status);
CREATE INDEX idx_gstr2b_imports_company_period ON gstr2b_imports(company_id, return_period);

-- Invoices
CREATE INDEX idx_gstr2b_invoices_import ON gstr2b_invoices(import_id);
CREATE INDEX idx_gstr2b_invoices_company ON gstr2b_invoices(company_id);
CREATE INDEX idx_gstr2b_invoices_period ON gstr2b_invoices(return_period);
CREATE INDEX idx_gstr2b_invoices_supplier_gstin ON gstr2b_invoices(supplier_gstin);
CREATE INDEX idx_gstr2b_invoices_match_status ON gstr2b_invoices(match_status);
CREATE INDEX idx_gstr2b_invoices_action_status ON gstr2b_invoices(action_status);
CREATE INDEX idx_gstr2b_invoices_invoice_number ON gstr2b_invoices(invoice_number);
CREATE INDEX idx_gstr2b_invoices_matched_vendor ON gstr2b_invoices(matched_vendor_invoice_id);

-- Composite indexes for reconciliation
CREATE INDEX idx_gstr2b_invoices_reconciliation ON gstr2b_invoices(company_id, supplier_gstin, invoice_number);
CREATE INDEX idx_gstr2b_invoices_company_period_status ON gstr2b_invoices(company_id, return_period, match_status);

-- ==================== Comments ====================

COMMENT ON TABLE gstr2b_imports IS 'GSTR-2B file imports - tracks each upload/import of GSTR-2B data';
COMMENT ON TABLE gstr2b_invoices IS 'Individual invoice records from GSTR-2B for ITC reconciliation';
COMMENT ON TABLE gstr2b_reconciliation_rules IS 'Configurable rules for matching GSTR-2B invoices with vendor invoices';

COMMENT ON COLUMN gstr2b_invoices.match_status IS 'pending=not yet matched, matched=exact match found, partial_match=match with discrepancies, unmatched=no match, accepted=user accepted, rejected=user rejected';
COMMENT ON COLUMN gstr2b_invoices.match_confidence IS 'Confidence score 0-100 based on matching algorithm';
COMMENT ON COLUMN gstr2b_invoices.invoice_type IS 'B2B=Business supplies, CDNR=Credit/Debit notes, ISD=Input Service Distributor, IMPG=Import of goods';
