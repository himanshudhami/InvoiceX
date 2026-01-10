-- Add missing columns to vendor_payment_allocations for full Tally import support
ALTER TABLE vendor_payment_allocations
    ADD COLUMN IF NOT EXISTS company_id UUID REFERENCES companies(id),
    ADD COLUMN IF NOT EXISTS allocation_date DATE,
    ADD COLUMN IF NOT EXISTS currency VARCHAR(10) DEFAULT 'INR',
    ADD COLUMN IF NOT EXISTS amount_in_inr NUMERIC(18, 2),
    ADD COLUMN IF NOT EXISTS exchange_rate NUMERIC(18, 6) DEFAULT 1,
    ADD COLUMN IF NOT EXISTS notes TEXT,
    ADD COLUMN IF NOT EXISTS created_by UUID REFERENCES users(id);

-- Backfill company_id from vendor_payments
UPDATE vendor_payment_allocations vpa
SET company_id = vp.company_id
FROM vendor_payments vp
WHERE vpa.vendor_payment_id = vp.id
  AND vpa.company_id IS NULL;

-- Backfill allocation_date from vendor_payments
UPDATE vendor_payment_allocations vpa
SET allocation_date = vp.payment_date
FROM vendor_payments vp
WHERE vpa.vendor_payment_id = vp.id
  AND vpa.allocation_date IS NULL;

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_vendor_payment_allocations_company
    ON vendor_payment_allocations(company_id);
CREATE INDEX IF NOT EXISTS idx_vendor_payment_allocations_invoice
    ON vendor_payment_allocations(vendor_invoice_id);
