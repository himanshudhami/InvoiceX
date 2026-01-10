-- GSTR-3B Filing Pack tables
-- Supports consolidated GSTR-3B data with drill-down to source documents

-- ============================================
-- Table: gstr3b_filings
-- Main filing record for a return period
-- ============================================
CREATE TABLE gstr3b_filings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    gstin VARCHAR(15) NOT NULL,
    return_period VARCHAR(7) NOT NULL,      -- 'Jan-2025' format
    financial_year VARCHAR(7) NOT NULL,     -- '2024-25' format

    -- Filing status
    status VARCHAR(20) DEFAULT 'draft',     -- draft, generated, reviewed, filed, amended
    generated_at TIMESTAMP WITH TIME ZONE,
    generated_by UUID REFERENCES users(id),
    reviewed_at TIMESTAMP WITH TIME ZONE,
    reviewed_by UUID REFERENCES users(id),
    filed_at TIMESTAMP WITH TIME ZONE,
    filed_by UUID REFERENCES users(id),

    -- GSTN Filing details
    arn VARCHAR(50),
    filing_date DATE,

    -- Table summaries (JSONB for quick access)
    table_3_1 JSONB,        -- Outward supplies
    table_3_2 JSONB,        -- Interstate supplies to unregistered
    table_4 JSONB,          -- ITC summary
    table_5 JSONB,          -- Exempt/nil-rated supplies
    table_6_1 JSONB,        -- Tax payment

    -- Comparison with previous period
    previous_period_variance JSONB,

    -- Notes
    notes TEXT,

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),

    CONSTRAINT uq_gstr3b_company_period UNIQUE (company_id, return_period)
);

-- ============================================
-- Table: gstr3b_line_items
-- Individual line items with drill-down support
-- ============================================
CREATE TABLE gstr3b_line_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    filing_id UUID NOT NULL REFERENCES gstr3b_filings(id) ON DELETE CASCADE,

    -- Table reference (e.g., '3.1(a)', '4(A)(1)', '5(a)')
    table_code VARCHAR(20) NOT NULL,
    row_order INT DEFAULT 0,
    description VARCHAR(255) NOT NULL,

    -- Amounts
    taxable_value DECIMAL(18,2) DEFAULT 0,
    igst DECIMAL(18,2) DEFAULT 0,
    cgst DECIMAL(18,2) DEFAULT 0,
    sgst DECIMAL(18,2) DEFAULT 0,
    cess DECIMAL(18,2) DEFAULT 0,

    -- Source tracking for drill-down
    source_count INT DEFAULT 0,
    source_type VARCHAR(50),            -- 'invoice', 'vendor_invoice', 'rcm_transaction', 'manual'
    source_ids JSONB,                   -- Array of source transaction IDs

    -- Computation notes
    computation_notes TEXT,

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- Table: gstr3b_source_documents
-- Links line items to source documents for drill-down
-- ============================================
CREATE TABLE gstr3b_source_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    line_item_id UUID NOT NULL REFERENCES gstr3b_line_items(id) ON DELETE CASCADE,

    -- Source reference
    source_type VARCHAR(50) NOT NULL,       -- 'invoice', 'vendor_invoice', 'rcm_transaction', 'credit_note', 'debit_note'
    source_id UUID NOT NULL,
    source_number VARCHAR(100),
    source_date DATE,

    -- Amounts contributed to line item
    taxable_value DECIMAL(18,2) DEFAULT 0,
    igst DECIMAL(18,2) DEFAULT 0,
    cgst DECIMAL(18,2) DEFAULT 0,
    sgst DECIMAL(18,2) DEFAULT 0,
    cess DECIMAL(18,2) DEFAULT 0,

    -- Party details
    party_name VARCHAR(255),
    party_gstin VARCHAR(15),

    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- ============================================
-- Indexes
-- ============================================
CREATE INDEX idx_gstr3b_filings_company ON gstr3b_filings(company_id);
CREATE INDEX idx_gstr3b_filings_period ON gstr3b_filings(return_period);
CREATE INDEX idx_gstr3b_filings_status ON gstr3b_filings(status);
CREATE INDEX idx_gstr3b_filings_fy ON gstr3b_filings(financial_year);

CREATE INDEX idx_gstr3b_line_items_filing ON gstr3b_line_items(filing_id);
CREATE INDEX idx_gstr3b_line_items_table ON gstr3b_line_items(table_code);

CREATE INDEX idx_gstr3b_source_docs_line ON gstr3b_source_documents(line_item_id);
CREATE INDEX idx_gstr3b_source_docs_source ON gstr3b_source_documents(source_type, source_id);

-- ============================================
-- Comments
-- ============================================
COMMENT ON TABLE gstr3b_filings IS 'GSTR-3B filing records with table summaries';
COMMENT ON TABLE gstr3b_line_items IS 'Individual line items in GSTR-3B tables';
COMMENT ON TABLE gstr3b_source_documents IS 'Source documents linked to line items for drill-down';

COMMENT ON COLUMN gstr3b_filings.table_3_1 IS 'Table 3.1: Outward supplies (taxable, zero-rated, nil, RCM, non-GST)';
COMMENT ON COLUMN gstr3b_filings.table_4 IS 'Table 4: ITC Available, Reversed, Ineligible';
COMMENT ON COLUMN gstr3b_filings.table_5 IS 'Table 5: Exempt, nil-rated, non-GST inward supplies';
COMMENT ON COLUMN gstr3b_line_items.table_code IS 'Reference like 3.1(a), 4(A)(1), 5(a)';
