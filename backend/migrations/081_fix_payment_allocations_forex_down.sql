-- Rollback: Remove posted tracking from payments
-- Note: Cannot perfectly rollback the data fixes

DROP INDEX IF EXISTS idx_payments_is_posted;
ALTER TABLE payments DROP COLUMN IF EXISTS posted_at;
ALTER TABLE payments DROP COLUMN IF EXISTS posted_journal_id;
ALTER TABLE payments DROP COLUMN IF EXISTS is_posted;

-- Reset exchange rates to 1.0 (original incorrect state)
UPDATE payment_allocations SET exchange_rate = 1.000000, amount_in_inr = NULL;

-- Clear invoice forex fields
UPDATE invoices SET
    invoice_exchange_rate = NULL,
    invoice_amount_inr = NULL,
    realization_due_date = NULL
WHERE currency = 'USD' OR invoice_type = 'export';
