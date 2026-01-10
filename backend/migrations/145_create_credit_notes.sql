-- Credit Notes Schema - GST Compliant (Section 34 CGST Act)
-- This migration creates tables for credit notes linked to invoices

-- Create credit_notes table
CREATE TABLE IF NOT EXISTS credit_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    party_id UUID REFERENCES parties(id) ON DELETE SET NULL,

    -- Credit Note identification
    credit_note_number VARCHAR(50) NOT NULL,
    credit_note_date DATE NOT NULL,

    -- Original invoice reference (MANDATORY as per GST)
    original_invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
    original_invoice_number VARCHAR(50) NOT NULL,
    original_invoice_date DATE NOT NULL,

    -- Reason for credit note (required for GST compliance)
    reason VARCHAR(50) NOT NULL,  -- goods_returned, post_sale_discount, deficiency_in_services, excess_amount_charged, excess_tax_charged, change_in_pos, export_refund, other
    reason_description TEXT,      -- Detailed description, especially for 'other'

    -- Status
    status VARCHAR(20) DEFAULT 'draft',  -- draft, issued, cancelled

    -- Financial details
    subtotal NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_amount NUMERIC(12,2) DEFAULT 0,
    discount_amount NUMERIC(12,2) DEFAULT 0,
    total_amount NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',

    -- Additional details
    notes TEXT,
    terms TEXT,

    -- Timestamps
    issued_at TIMESTAMP WITH TIME ZONE,
    cancelled_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- GST Classification (inherited from original invoice)
    invoice_type VARCHAR(30),          -- export, domestic_b2b, domestic_b2c, sez, deemed_export
    supply_type VARCHAR(30),           -- intra_state, inter_state, export
    place_of_supply VARCHAR(10),       -- State code or 'export'
    reverse_charge BOOLEAN DEFAULT FALSE,

    -- GST Totals
    total_cgst NUMERIC(12,2) DEFAULT 0,
    total_sgst NUMERIC(12,2) DEFAULT 0,
    total_igst NUMERIC(12,2) DEFAULT 0,
    total_cess NUMERIC(12,2) DEFAULT 0,

    -- E-invoicing fields (credit notes also need IRN for applicable businesses)
    e_invoice_applicable BOOLEAN DEFAULT FALSE,
    irn VARCHAR(100),
    irn_generated_at TIMESTAMP WITH TIME ZONE,
    irn_cancelled_at TIMESTAMP WITH TIME ZONE,
    qr_code_data TEXT,
    e_invoice_signed_json JSONB,
    e_invoice_status VARCHAR(30) DEFAULT 'not_applicable',  -- not_applicable, pending, generated, cancelled, error

    -- Forex (for export invoices)
    foreign_currency VARCHAR(3),
    exchange_rate NUMERIC(15,6),
    amount_in_inr NUMERIC(15,2),

    -- 2025 Amendment: ITC Reversal Tracking (Section 34(2) CGST Act as amended)
    itc_reversal_required BOOLEAN DEFAULT FALSE,
    itc_reversal_confirmed BOOLEAN DEFAULT FALSE,
    itc_reversal_date DATE,
    itc_reversal_certificate TEXT,     -- CA/CMA certificate reference for tax > 5L

    -- GSTR-1 Reporting
    reported_in_gstr1 BOOLEAN DEFAULT FALSE,
    gstr1_period VARCHAR(6),           -- YYYYMM format e.g., '202501'
    gstr1_filing_date DATE,

    -- Time limit tracking (must be issued before 30th Nov of next FY from original invoice)
    time_barred_date DATE,
    is_time_barred BOOLEAN DEFAULT FALSE,

    -- Constraints
    CONSTRAINT credit_notes_status_check CHECK (status IN ('draft', 'issued', 'cancelled')),
    CONSTRAINT credit_notes_reason_check CHECK (reason IN (
        'goods_returned', 'post_sale_discount', 'deficiency_in_services',
        'excess_amount_charged', 'excess_tax_charged', 'change_in_pos',
        'export_refund', 'other'
    )),
    CONSTRAINT credit_notes_e_invoice_status_check CHECK (e_invoice_status IN (
        'not_applicable', 'pending', 'generated', 'cancelled', 'error'
    )),
    CONSTRAINT credit_notes_unique_number_company UNIQUE (company_id, credit_note_number)
);

-- Create credit_note_items table
CREATE TABLE IF NOT EXISTS credit_note_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    credit_note_id UUID NOT NULL REFERENCES credit_notes(id) ON DELETE CASCADE,
    original_invoice_item_id UUID REFERENCES invoice_items(id) ON DELETE SET NULL,
    product_id UUID REFERENCES products(id) ON DELETE SET NULL,

    description VARCHAR(500) NOT NULL,
    quantity NUMERIC(12,3) NOT NULL DEFAULT 1,
    unit_price NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_rate NUMERIC(5,2) DEFAULT 0,
    discount_rate NUMERIC(5,2) DEFAULT 0,
    line_total NUMERIC(12,2) NOT NULL DEFAULT 0,
    sort_order INTEGER DEFAULT 0,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- GST Compliance fields
    hsn_sac_code VARCHAR(8),           -- HSN code (goods) or SAC code (services)
    is_service BOOLEAN DEFAULT FALSE,  -- True for SAC code (services), false for HSN code (goods)
    cgst_rate NUMERIC(5,2) DEFAULT 0,
    cgst_amount NUMERIC(12,2) DEFAULT 0,
    sgst_rate NUMERIC(5,2) DEFAULT 0,
    sgst_amount NUMERIC(12,2) DEFAULT 0,
    igst_rate NUMERIC(5,2) DEFAULT 0,
    igst_amount NUMERIC(12,2) DEFAULT 0,
    cess_rate NUMERIC(5,2) DEFAULT 0,
    cess_amount NUMERIC(12,2) DEFAULT 0
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_credit_notes_company ON credit_notes(company_id);
CREATE INDEX IF NOT EXISTS idx_credit_notes_party ON credit_notes(party_id);
CREATE INDEX IF NOT EXISTS idx_credit_notes_original_invoice ON credit_notes(original_invoice_id);
CREATE INDEX IF NOT EXISTS idx_credit_notes_status ON credit_notes(status);
CREATE INDEX IF NOT EXISTS idx_credit_notes_date ON credit_notes(credit_note_date);
CREATE INDEX IF NOT EXISTS idx_credit_notes_number ON credit_notes(credit_note_number);
CREATE INDEX IF NOT EXISTS idx_credit_notes_gstr1 ON credit_notes(reported_in_gstr1, gstr1_period);

CREATE INDEX IF NOT EXISTS idx_credit_note_items_credit_note ON credit_note_items(credit_note_id);
CREATE INDEX IF NOT EXISTS idx_credit_note_items_product ON credit_note_items(product_id);

-- Add column to invoices to track credit notes issued against them
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS credit_note_total NUMERIC(12,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS has_credit_notes BOOLEAN DEFAULT FALSE;

-- Function to calculate time barred date (30th Nov of next FY from original invoice)
CREATE OR REPLACE FUNCTION calculate_time_barred_date(invoice_date DATE)
RETURNS DATE AS $$
DECLARE
    fiscal_year_end DATE;
    next_fy_end DATE;
BEGIN
    -- Indian FY ends on 31st March
    -- If invoice is between Apr-Mar, FY ends on 31st March of next calendar year
    IF EXTRACT(MONTH FROM invoice_date) >= 4 THEN
        fiscal_year_end := DATE_TRUNC('year', invoice_date) + INTERVAL '1 year' + INTERVAL '2 months' + INTERVAL '30 days';
    ELSE
        fiscal_year_end := DATE_TRUNC('year', invoice_date) + INTERVAL '2 months' + INTERVAL '30 days';
    END IF;

    -- Time barred date is 30th Nov of the FY following the invoice FY
    -- So if FY ends 31-Mar-2025, time barred is 30-Nov-2025
    RETURN (fiscal_year_end + INTERVAL '8 months')::DATE;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Trigger to auto-calculate time barred date on insert
CREATE OR REPLACE FUNCTION set_credit_note_time_barred()
RETURNS TRIGGER AS $$
BEGIN
    NEW.time_barred_date := calculate_time_barred_date(NEW.original_invoice_date);
    NEW.is_time_barred := (CURRENT_DATE > NEW.time_barred_date);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS credit_note_time_barred_trigger ON credit_notes;
CREATE TRIGGER credit_note_time_barred_trigger
    BEFORE INSERT OR UPDATE ON credit_notes
    FOR EACH ROW
    EXECUTE FUNCTION set_credit_note_time_barred();

-- Trigger to update invoice credit_note_total when credit notes are created/updated
CREATE OR REPLACE FUNCTION update_invoice_credit_note_total()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        UPDATE invoices
        SET credit_note_total = COALESCE((
            SELECT SUM(total_amount)
            FROM credit_notes
            WHERE original_invoice_id = NEW.original_invoice_id
            AND status != 'cancelled'
        ), 0),
        has_credit_notes = EXISTS (
            SELECT 1 FROM credit_notes
            WHERE original_invoice_id = NEW.original_invoice_id
            AND status != 'cancelled'
        )
        WHERE id = NEW.original_invoice_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE invoices
        SET credit_note_total = COALESCE((
            SELECT SUM(total_amount)
            FROM credit_notes
            WHERE original_invoice_id = OLD.original_invoice_id
            AND status != 'cancelled'
        ), 0),
        has_credit_notes = EXISTS (
            SELECT 1 FROM credit_notes
            WHERE original_invoice_id = OLD.original_invoice_id
            AND status != 'cancelled'
        )
        WHERE id = OLD.original_invoice_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS credit_note_invoice_total_trigger ON credit_notes;
CREATE TRIGGER credit_note_invoice_total_trigger
    AFTER INSERT OR UPDATE OR DELETE ON credit_notes
    FOR EACH ROW
    EXECUTE FUNCTION update_invoice_credit_note_total();

-- Comments for documentation
COMMENT ON TABLE credit_notes IS 'Credit notes for invoices as per Section 34 of CGST Act. Must reference original invoice.';
COMMENT ON COLUMN credit_notes.reason IS 'Reason for credit note as per GST rules: goods_returned, post_sale_discount, deficiency_in_services, excess_amount_charged, excess_tax_charged, change_in_pos, export_refund, other';
COMMENT ON COLUMN credit_notes.time_barred_date IS 'Deadline for issuing credit note (30th Nov of next FY from original invoice date)';
COMMENT ON COLUMN credit_notes.itc_reversal_confirmed IS 'Per 2025 amendment to Section 34(2), supplier can only reduce output tax if recipient has reversed ITC';
COMMENT ON COLUMN credit_notes.itc_reversal_certificate IS 'For tax amount > 5 Lakh, CA/CMA certificate from recipient is required as per Circular 212/6/2024-GST';
