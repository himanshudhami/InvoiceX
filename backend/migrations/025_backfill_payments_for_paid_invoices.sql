-- Migration: Backfill payments for existing paid invoices
-- This migration creates payment records for invoices that are marked as 'paid'
-- but don't have corresponding records in the payments table
-- This ensures the dashboard revenue calculations work correctly after switching
-- from invoice status to payments table

-- Insert payment records for paid invoices without existing payments
INSERT INTO payments (
    id,
    invoice_id,
    company_id,
    customer_id,
    payment_date,
    amount,
    amount_in_inr,
    currency,
    payment_method,
    reference_number,
    notes,
    description,
    payment_type,
    income_category,
    tds_applicable,
    financial_year,
    created_at,
    updated_at
)
SELECT
    gen_random_uuid() as id,
    i.id as invoice_id,
    i.company_id as company_id,
    i.customer_id as customer_id,
    -- Use paid_at if available, otherwise invoice_date
    COALESCE(i.paid_at::date, i.invoice_date) as payment_date,
    -- Use paid_amount if > 0, otherwise total_amount (paid_amount may be 0 even for paid invoices)
    CASE WHEN COALESCE(i.paid_amount, 0) > 0 THEN i.paid_amount ELSE i.total_amount END as amount,
    -- For INR invoices, amount_in_inr equals amount; for foreign currency, estimate
    CASE
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
    END as amount_in_inr,
    COALESCE(i.currency, 'INR') as currency,
    'bank_transfer' as payment_method,  -- Default method for historical payments
    NULL as reference_number,
    'Auto-created from paid invoice during migration' as notes,
    NULL as description,
    'invoice_payment' as payment_type,
    -- Determine income category based on customer country
    CASE
        WHEN c.country IS NOT NULL AND UPPER(c.country) NOT IN ('INDIA', 'IN', 'IND') THEN 'export_services'
        ELSE 'domestic_services'
    END as income_category,
    false as tds_applicable,  -- Default to false, user can update if needed
    -- Calculate financial year from payment date
    CASE
        WHEN EXTRACT(MONTH FROM COALESCE(i.paid_at, i.invoice_date::timestamp)) >= 4
        THEN CONCAT(
            EXTRACT(YEAR FROM COALESCE(i.paid_at, i.invoice_date::timestamp))::text,
            '-',
            RIGHT((EXTRACT(YEAR FROM COALESCE(i.paid_at, i.invoice_date::timestamp)) + 1)::text, 2)
        )
        ELSE CONCAT(
            (EXTRACT(YEAR FROM COALESCE(i.paid_at, i.invoice_date::timestamp)) - 1)::text,
            '-',
            RIGHT(EXTRACT(YEAR FROM COALESCE(i.paid_at, i.invoice_date::timestamp))::text, 2)
        )
    END as financial_year,
    COALESCE(i.paid_at, NOW()) as created_at,
    NOW() as updated_at
FROM invoices i
LEFT JOIN customers c ON i.customer_id = c.id
WHERE i.status = 'paid'
AND NOT EXISTS (
    SELECT 1 FROM payments p WHERE p.invoice_id = i.id
);

-- Log how many records were created
DO $$
DECLARE
    backfilled_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO backfilled_count
    FROM payments p
    WHERE p.notes = 'Auto-created from paid invoice during migration';

    RAISE NOTICE 'Backfilled % payment records for existing paid invoices', backfilled_count;
END $$;
