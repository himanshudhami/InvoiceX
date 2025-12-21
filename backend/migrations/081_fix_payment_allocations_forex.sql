-- Migration: Fix payment_allocations exchange_rate and amount_in_inr
-- Purpose: Correct forex data for proper reconciliation

-- Add posted tracking to payments (similar to invoices)
ALTER TABLE payments ADD COLUMN IF NOT EXISTS is_posted BOOLEAN DEFAULT FALSE;
ALTER TABLE payments ADD COLUMN IF NOT EXISTS posted_journal_id UUID REFERENCES journal_entries(id);
ALTER TABLE payments ADD COLUMN IF NOT EXISTS posted_at TIMESTAMP;

-- Create index for unposted payments
CREATE INDEX IF NOT EXISTS idx_payments_is_posted ON payments(is_posted) WHERE is_posted = FALSE;

-- Fix exchange_rate on payment_allocations
-- Calculate actual rate from payment's amount_in_inr / amount
UPDATE payment_allocations pa
SET
    exchange_rate = CASE
        WHEN p.amount > 0 AND p.amount_in_inr > 0
        THEN ROUND(p.amount_in_inr / p.amount, 6)
        ELSE 1.000000
    END,
    amount_in_inr = CASE
        WHEN p.amount > 0 AND p.amount_in_inr > 0
        THEN ROUND(pa.allocated_amount * (p.amount_in_inr / p.amount), 2)
        ELSE pa.allocated_amount
    END
FROM payments p
WHERE pa.payment_id = p.id
  AND p.currency = 'USD'
  AND p.amount_in_inr IS NOT NULL
  AND p.amount_in_inr > 0;

-- Backfill invoice forex fields from payment data
-- Use payment exchange rate as proxy for invoice rate (best available data)
UPDATE invoices i
SET
    invoice_exchange_rate = COALESCE(
        (SELECT ROUND(p.amount_in_inr / NULLIF(p.amount, 0), 6)
         FROM payments p
         WHERE p.invoice_id = i.id
         AND p.amount_in_inr IS NOT NULL
         AND p.amount > 0
         LIMIT 1),
        i.exchange_rate
    ),
    invoice_amount_inr = COALESCE(
        (SELECT ROUND(i.total_amount * (p.amount_in_inr / NULLIF(p.amount, 0)), 2)
         FROM payments p
         WHERE p.invoice_id = i.id
         AND p.amount_in_inr IS NOT NULL
         AND p.amount > 0
         LIMIT 1),
        i.total_amount
    ),
    -- Set realization due date (9 months from invoice date)
    realization_due_date = CASE
        WHEN i.invoice_type = 'export' AND i.status NOT IN ('paid', 'cancelled')
        THEN i.invoice_date + INTERVAL '9 months'
        ELSE NULL
    END
WHERE i.currency = 'USD' OR i.invoice_type = 'export';

-- Comments
COMMENT ON COLUMN payments.is_posted IS 'Whether this payment has been posted to the general ledger';
COMMENT ON COLUMN payments.posted_journal_id IS 'Reference to the journal entry created for this payment';
