-- Migration 076: Backfill payment_allocations for existing payments linked to invoices
-- This creates allocation records for payments that were created before auto-allocation was implemented

-- Insert allocation for each payment that has an invoice_id but no existing allocation
INSERT INTO payment_allocations (
    id,
    company_id,
    payment_id,
    invoice_id,
    allocated_amount,
    currency,
    amount_in_inr,
    allocation_date,
    allocation_type,
    notes,
    created_at
)
SELECT
    gen_random_uuid(),
    p.company_id,
    p.id,
    p.invoice_id,
    COALESCE(p.gross_amount, p.amount),
    COALESCE(p.currency, 'INR'),
    p.amount_in_inr,
    p.payment_date,
    'invoice_settlement',
    'Backfilled from existing payment',
    NOW()
FROM payments p
WHERE p.invoice_id IS NOT NULL
  AND p.company_id IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM payment_allocations pa
    WHERE pa.payment_id = p.id AND pa.invoice_id = p.invoice_id
  );

-- Now sync invoice.paid_amount with the total of all allocations for each invoice
-- This ensures the Balance Due shown on invoices matches the Payment Status
UPDATE invoices i
SET
    paid_amount = alloc_totals.total_allocated,
    status = CASE
        WHEN alloc_totals.total_allocated >= i.total_amount THEN 'paid'
        WHEN alloc_totals.total_allocated > 0 THEN 'partially_paid'
        ELSE i.status
    END,
    updated_at = NOW()
FROM (
    SELECT
        invoice_id,
        SUM(allocated_amount) as total_allocated
    FROM payment_allocations
    WHERE invoice_id IS NOT NULL
    GROUP BY invoice_id
) alloc_totals
WHERE i.id = alloc_totals.invoice_id
  AND (i.paid_amount IS NULL OR i.paid_amount != alloc_totals.total_allocated);

-- Log count of backfilled records
DO $$
DECLARE
    backfilled_allocations INTEGER;
    synced_invoices INTEGER;
BEGIN
    SELECT COUNT(*) INTO backfilled_allocations
    FROM payment_allocations
    WHERE notes = 'Backfilled from existing payment'
      AND created_at > NOW() - INTERVAL '1 minute';

    GET DIAGNOSTICS synced_invoices = ROW_COUNT;

    RAISE NOTICE 'Backfilled % payment allocations', backfilled_allocations;
    RAISE NOTICE 'Synced % invoice paid_amount values', synced_invoices;
END $$;
