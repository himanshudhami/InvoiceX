-- Reconcile Tally Bill Allocations to Invoices
-- Run this after Tally import if allocations weren't linked due to import order
-- (payments imported before invoices)

BEGIN;

-- ============================================
-- 1. Link payment_allocations to invoices
-- ============================================

-- Update payment_allocations where tally_bill_ref matches invoice_number
UPDATE payment_allocations pa
SET invoice_id = i.id,
    updated_at = NOW()
FROM invoices i
WHERE pa.invoice_id IS NULL
  AND pa.notes LIKE 'Tally:%'
  AND pa.company_id = i.company_id
  AND (
    -- Match by tally_bill_ref if it exists
    (pa.notes LIKE '%- ' || i.invoice_number || '%')
    OR (pa.notes LIKE '%- ' || i.invoice_number)
  );

-- Log how many were updated
DO $$
DECLARE
    updated_count INTEGER;
BEGIN
    GET DIAGNOSTICS updated_count = ROW_COUNT;
    RAISE NOTICE 'Payment allocations linked to invoices: %', updated_count;
END $$;

-- ============================================
-- 2. Link vendor_payment_allocations to vendor_invoices
-- ============================================

-- Update vendor_payment_allocations where tally_bill_ref matches vendor invoice_number
UPDATE vendor_payment_allocations vpa
SET vendor_invoice_id = vi.id,
    updated_at = NOW()
FROM vendor_invoices vi
WHERE vpa.vendor_invoice_id IS NULL
  AND vpa.tally_bill_ref IS NOT NULL
  AND vpa.tally_bill_ref != ''
  AND vpa.company_id = vi.company_id
  AND vpa.tally_bill_ref = vi.invoice_number;

-- Also try matching via notes field
UPDATE vendor_payment_allocations vpa
SET vendor_invoice_id = vi.id,
    updated_at = NOW()
FROM vendor_invoices vi
WHERE vpa.vendor_invoice_id IS NULL
  AND vpa.notes LIKE 'Tally:%'
  AND vpa.company_id = vi.company_id
  AND (
    vpa.notes LIKE '%- ' || vi.invoice_number || '%'
    OR vpa.notes LIKE '%- ' || vi.invoice_number
  );

-- ============================================
-- 3. Update invoice statuses based on allocations
-- ============================================

-- Update invoices to 'paid' where fully allocated
UPDATE invoices i
SET status = 'paid',
    updated_at = NOW()
FROM (
    SELECT
        pa.invoice_id,
        SUM(pa.allocated_amount) as total_allocated
    FROM payment_allocations pa
    WHERE pa.invoice_id IS NOT NULL
    GROUP BY pa.invoice_id
) alloc
WHERE i.id = alloc.invoice_id
  AND alloc.total_allocated >= i.total_amount
  AND i.status != 'paid';

-- Update invoices to 'partially_paid' where partially allocated
UPDATE invoices i
SET status = 'partially_paid',
    updated_at = NOW()
FROM (
    SELECT
        pa.invoice_id,
        SUM(pa.allocated_amount) as total_allocated
    FROM payment_allocations pa
    WHERE pa.invoice_id IS NOT NULL
    GROUP BY pa.invoice_id
) alloc
WHERE i.id = alloc.invoice_id
  AND alloc.total_allocated > 0
  AND alloc.total_allocated < i.total_amount
  AND i.status NOT IN ('paid', 'partially_paid');

-- ============================================
-- 4. Update vendor invoice statuses
-- ============================================

-- Update vendor_invoices to 'paid' where fully allocated
UPDATE vendor_invoices vi
SET status = 'paid',
    updated_at = NOW()
FROM (
    SELECT
        vpa.vendor_invoice_id,
        SUM(vpa.allocated_amount) as total_allocated
    FROM vendor_payment_allocations vpa
    WHERE vpa.vendor_invoice_id IS NOT NULL
    GROUP BY vpa.vendor_invoice_id
) alloc
WHERE vi.id = alloc.vendor_invoice_id
  AND alloc.total_allocated >= vi.total_amount
  AND vi.status != 'paid';

-- Update vendor_invoices to 'partially_paid' where partially allocated
UPDATE vendor_invoices vi
SET status = 'partially_paid',
    updated_at = NOW()
FROM (
    SELECT
        vpa.vendor_invoice_id,
        SUM(vpa.allocated_amount) as total_allocated
    FROM vendor_payment_allocations vpa
    WHERE vpa.vendor_invoice_id IS NOT NULL
    GROUP BY vpa.vendor_invoice_id
) alloc
WHERE vi.id = alloc.vendor_invoice_id
  AND alloc.total_allocated > 0
  AND alloc.total_allocated < vi.total_amount
  AND vi.status NOT IN ('paid', 'partially_paid');

-- ============================================
-- 5. Summary report
-- ============================================

SELECT 'Payment Allocations' as type,
       COUNT(*) FILTER (WHERE invoice_id IS NOT NULL) as linked,
       COUNT(*) FILTER (WHERE invoice_id IS NULL AND notes LIKE 'Tally:%') as unlinked_tally,
       COUNT(*) as total
FROM payment_allocations

UNION ALL

SELECT 'Vendor Payment Allocations' as type,
       COUNT(*) FILTER (WHERE vendor_invoice_id IS NOT NULL) as linked,
       COUNT(*) FILTER (WHERE vendor_invoice_id IS NULL AND (tally_bill_ref IS NOT NULL OR notes LIKE 'Tally:%')) as unlinked_tally,
       COUNT(*) as total
FROM vendor_payment_allocations;

COMMIT;
