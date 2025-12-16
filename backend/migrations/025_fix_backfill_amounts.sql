-- Fix backfilled payments that have amount = 0
-- Run this manually to correct the data

UPDATE payments p
SET
    amount = CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END,
    amount_in_inr = CASE
        WHEN UPPER(i.currency) = 'INR' OR i.currency IS NULL THEN
            CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END
        WHEN UPPER(i.currency) = 'USD' THEN
            (CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END) * 83
        WHEN UPPER(i.currency) = 'EUR' THEN
            (CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END) * 90
        WHEN UPPER(i.currency) = 'GBP' THEN
            (CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END) * 105
        ELSE
            (CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END) * 83
    END
FROM invoices i
WHERE p.invoice_id = i.id
AND p.notes = 'Auto-created from paid invoice during migration'
AND p.amount = 0;
