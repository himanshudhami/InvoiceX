-- Migration 075 Down: Revert v_invoice_payment_status to original definition

CREATE OR REPLACE VIEW v_invoice_payment_status AS
SELECT
    i.id AS invoice_id,
    i.company_id,
    i.customer_id,
    i.invoice_number,
    i.total_amount AS invoice_total,
    i.currency,
    i.status AS invoice_status,
    i.due_date,
    COALESCE(SUM(pa.allocated_amount), 0) AS total_paid,
    i.total_amount - COALESCE(SUM(pa.allocated_amount), 0) AS balance_due,
    CASE
        WHEN COALESCE(SUM(pa.allocated_amount), 0) = 0 THEN 'unpaid'
        WHEN COALESCE(SUM(pa.allocated_amount), 0) >= i.total_amount THEN 'paid'
        ELSE 'partial'
    END AS payment_status,
    COUNT(pa.id) AS payment_count,
    MAX(pa.allocation_date) AS last_payment_date
FROM invoices i
LEFT JOIN payment_allocations pa ON pa.invoice_id = i.id
GROUP BY i.id, i.company_id, i.customer_id, i.invoice_number, i.total_amount,
         i.currency, i.status, i.due_date;
