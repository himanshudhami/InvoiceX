-- Migration 166: Recreate v_invoice_payment_status view
-- The view was lost when invoices table was recreated in migration 129
-- Updated to use party_id instead of customer_id
-- Fixed currency to default to 'INR' when null

DROP VIEW IF EXISTS v_invoice_payment_status;

CREATE VIEW v_invoice_payment_status AS
SELECT
    i.id AS invoice_id,
    i.company_id,
    i.party_id,
    i.invoice_number,
    i.total_amount AS invoice_total,
    COALESCE(i.currency, 'INR')::VARCHAR(3) AS currency,
    i.status AS invoice_status,
    i.due_date,
    GREATEST(COALESCE(i.paid_amount, 0), COALESCE(SUM(pa.allocated_amount), 0)) AS total_paid,
    i.total_amount - GREATEST(COALESCE(i.paid_amount, 0), COALESCE(SUM(pa.allocated_amount), 0)) AS balance_due,
    CASE
        WHEN GREATEST(COALESCE(i.paid_amount, 0), COALESCE(SUM(pa.allocated_amount), 0)) = 0 THEN 'unpaid'
        WHEN GREATEST(COALESCE(i.paid_amount, 0), COALESCE(SUM(pa.allocated_amount), 0)) >= i.total_amount THEN 'paid'
        ELSE 'partial'
    END AS payment_status,
    COUNT(pa.id) AS payment_count,
    MAX(pa.allocation_date) AS last_payment_date
FROM invoices i
LEFT JOIN payment_allocations pa ON pa.invoice_id = i.id
GROUP BY i.id, i.company_id, i.party_id, i.invoice_number, i.total_amount,
         i.currency, i.status, i.due_date, i.paid_amount;

COMMENT ON VIEW v_invoice_payment_status IS
'Invoice payment status view that combines legacy paid_amount field with payment_allocations.
Uses GREATEST to handle migration period where some invoices have paid_amount set directly
while others use the new payment allocation system.';
