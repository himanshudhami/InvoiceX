-- Migration: Add forex tracking fields to invoices for export accounting
-- Purpose: Enable proper Ind AS 21 compliance for foreign currency invoices

-- Add invoice-date exchange rate for forex accounting
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS invoice_exchange_rate NUMERIC(18,6);

-- Add INR equivalent at invoice date (for ledger posting)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS invoice_amount_inr NUMERIC(18,2);

-- LUT (Letter of Undertaking) tracking for zero-rated exports
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS lut_number VARCHAR(50);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS lut_valid_from DATE;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS lut_valid_to DATE;

-- RBI purpose code for forex remittance (P0802 for software services)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS purpose_code VARCHAR(10);

-- AD Bank (Authorized Dealer) for forex receipts
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS ad_bank_name VARCHAR(100);

-- Track if invoice has been posted to ledger
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS is_posted BOOLEAN DEFAULT FALSE;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS posted_journal_id UUID REFERENCES journal_entries(id);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS posted_at TIMESTAMP;

-- FEMA realization deadline tracking (9 months from invoice date)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS realization_due_date DATE;

-- Create index for unposted invoices (for batch posting)
CREATE INDEX IF NOT EXISTS idx_invoices_is_posted ON invoices(is_posted) WHERE is_posted = FALSE;

-- Create index for realization tracking
CREATE INDEX IF NOT EXISTS idx_invoices_realization_due ON invoices(realization_due_date)
WHERE status NOT IN ('paid', 'cancelled') AND realization_due_date IS NOT NULL;

-- Add comments for documentation
COMMENT ON COLUMN invoices.invoice_exchange_rate IS 'RBI reference rate on invoice date for INR conversion';
COMMENT ON COLUMN invoices.invoice_amount_inr IS 'Total amount in INR at invoice-date rate (for ledger posting)';
COMMENT ON COLUMN invoices.lut_number IS 'LUT number for zero-rated export without payment of IGST';
COMMENT ON COLUMN invoices.purpose_code IS 'RBI purpose code for remittance (e.g., P0802 for software)';
COMMENT ON COLUMN invoices.ad_bank_name IS 'Authorized Dealer bank for forex receipts';
COMMENT ON COLUMN invoices.is_posted IS 'Whether this invoice has been posted to the general ledger';
COMMENT ON COLUMN invoices.posted_journal_id IS 'Reference to the journal entry created for this invoice';
COMMENT ON COLUMN invoices.realization_due_date IS 'FEMA deadline for export realization (9 months from invoice)';
