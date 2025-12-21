-- Migration: 083_backfill_forex_transactions.sql
-- Purpose: Backfill forex transactions for existing export invoices and payments
-- This creates forex_transactions records for Ind AS 21 compliance retroactively

-- ============================================================================
-- STEP 1: Create forex BOOKING transactions for all export invoices
-- ============================================================================
-- These represent the original foreign currency receivable at invoice date
-- exchange_rate is derived from payment data where available

INSERT INTO forex_transactions (
    id, company_id, transaction_date, financial_year,
    source_type, source_id, source_number,
    currency, foreign_amount, exchange_rate, inr_amount,
    transaction_type, forex_gain_loss, gain_loss_type,
    is_posted, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    i.company_id,
    i.invoice_date,
    CASE
        WHEN EXTRACT(MONTH FROM i.invoice_date) >= 4
        THEN CONCAT(EXTRACT(YEAR FROM i.invoice_date)::text, '-', LPAD(((EXTRACT(YEAR FROM i.invoice_date) + 1) % 100)::text, 2, '0'))
        ELSE CONCAT((EXTRACT(YEAR FROM i.invoice_date) - 1)::text, '-', LPAD((EXTRACT(YEAR FROM i.invoice_date) % 100)::text, 2, '0'))
    END as financial_year,
    'invoice',
    i.id,
    i.invoice_number,
    COALESCE(i.currency, 'USD'),
    i.total_amount,
    -- Use invoice_exchange_rate if set, otherwise derive from payment
    COALESCE(
        i.invoice_exchange_rate,
        (SELECT p.amount_in_inr / NULLIF(p.amount, 0)
         FROM payments p WHERE p.invoice_id = i.id LIMIT 1),
        83.00  -- Default fallback rate
    ),
    -- INR amount at booking
    i.total_amount * COALESCE(
        i.invoice_exchange_rate,
        (SELECT p.amount_in_inr / NULLIF(p.amount, 0)
         FROM payments p WHERE p.invoice_id = i.id LIMIT 1),
        83.00
    ),
    'booking',
    NULL, -- No gain/loss on booking
    NULL,
    false,
    NOW(),
    NOW()
FROM invoices i
WHERE i.invoice_type = 'export'
   OR i.supply_type = 'export'
   OR (i.currency IS NOT NULL AND i.currency != 'INR')
   -- Only for invoices without existing forex transactions
   AND NOT EXISTS (
       SELECT 1 FROM forex_transactions ft
       WHERE ft.source_type = 'invoice' AND ft.source_id = i.id
   );

-- ============================================================================
-- STEP 2: Create forex SETTLEMENT transactions for payments
-- ============================================================================
-- These represent the settlement of the receivable at payment date
-- with realized forex gain/loss calculated

INSERT INTO forex_transactions (
    id, company_id, transaction_date, financial_year,
    source_type, source_id, source_number,
    currency, foreign_amount, exchange_rate, inr_amount,
    transaction_type, forex_gain_loss, gain_loss_type,
    related_forex_id, is_posted, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    p.company_id,
    p.payment_date,
    CASE
        WHEN EXTRACT(MONTH FROM p.payment_date) >= 4
        THEN CONCAT(EXTRACT(YEAR FROM p.payment_date)::text, '-', LPAD(((EXTRACT(YEAR FROM p.payment_date) + 1) % 100)::text, 2, '0'))
        ELSE CONCAT((EXTRACT(YEAR FROM p.payment_date) - 1)::text, '-', LPAD((EXTRACT(YEAR FROM p.payment_date) % 100)::text, 2, '0'))
    END as financial_year,
    'payment',
    p.id,
    COALESCE(p.reference_number, LEFT(p.id::text, 8)),
    COALESCE(p.currency, 'USD'),
    p.amount,
    -- Settlement exchange rate from payment
    COALESCE(p.amount_in_inr / NULLIF(p.amount, 0), 83.00),
    -- INR amount at settlement
    COALESCE(p.amount_in_inr, p.amount * 83.00),
    'settlement',
    -- Forex gain/loss = Settlement INR - Booking INR
    COALESCE(p.amount_in_inr, p.amount * 83.00) - (
        SELECT ft.inr_amount
        FROM forex_transactions ft
        WHERE ft.source_type = 'invoice'
          AND ft.source_id = p.invoice_id
        LIMIT 1
    ),
    'realized',
    -- Link to booking transaction
    (SELECT ft.id
     FROM forex_transactions ft
     WHERE ft.source_type = 'invoice'
       AND ft.source_id = p.invoice_id
     LIMIT 1),
    false,
    NOW(),
    NOW()
FROM payments p
JOIN invoices i ON p.invoice_id = i.id
WHERE (i.invoice_type = 'export'
   OR i.supply_type = 'export'
   OR (i.currency IS NOT NULL AND i.currency != 'INR'))
  AND p.invoice_id IS NOT NULL
  AND p.amount_in_inr IS NOT NULL
  -- Only for payments without existing forex transactions
  AND NOT EXISTS (
      SELECT 1 FROM forex_transactions ft
      WHERE ft.source_type = 'payment' AND ft.source_id = p.id
  );

-- ============================================================================
-- STEP 3: Update invoices with forex fields where missing
-- ============================================================================

-- Update invoice_exchange_rate from booking transactions
UPDATE invoices i
SET
    invoice_exchange_rate = ft.exchange_rate,
    invoice_amount_inr = ft.inr_amount,
    foreign_currency_amount = ft.foreign_amount,
    updated_at = NOW()
FROM forex_transactions ft
WHERE ft.source_type = 'invoice'
  AND ft.source_id = i.id
  AND i.invoice_exchange_rate IS NULL;

-- Set realization_due_date (9 months from invoice date) where missing
UPDATE invoices
SET realization_due_date = invoice_date + INTERVAL '9 months'
WHERE realization_due_date IS NULL
  AND (invoice_type = 'export' OR supply_type = 'export' OR (currency IS NOT NULL AND currency != 'INR'));

-- ============================================================================
-- STEP 4: Summary of backfilled data
-- ============================================================================

DO $$
DECLARE
    v_booking_count INT;
    v_settlement_count INT;
    v_total_gain NUMERIC;
    v_total_loss NUMERIC;
BEGIN
    SELECT COUNT(*) INTO v_booking_count
    FROM forex_transactions WHERE transaction_type = 'booking';

    SELECT COUNT(*) INTO v_settlement_count
    FROM forex_transactions WHERE transaction_type = 'settlement';

    SELECT
        COALESCE(SUM(forex_gain_loss) FILTER (WHERE forex_gain_loss > 0), 0),
        COALESCE(ABS(SUM(forex_gain_loss) FILTER (WHERE forex_gain_loss < 0)), 0)
    INTO v_total_gain, v_total_loss
    FROM forex_transactions
    WHERE gain_loss_type = 'realized';

    RAISE NOTICE 'Forex Backfill Summary:';
    RAISE NOTICE '  Booking transactions created: %', v_booking_count;
    RAISE NOTICE '  Settlement transactions created: %', v_settlement_count;
    RAISE NOTICE '  Total realized forex gain: ₹%', v_total_gain;
    RAISE NOTICE '  Total realized forex loss: ₹%', v_total_loss;
    RAISE NOTICE '  Net forex impact: ₹%', v_total_gain - v_total_loss;
END $$;

-- Add index for performance on forex queries
CREATE INDEX IF NOT EXISTS idx_forex_transactions_source ON forex_transactions(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_forex_transactions_company_fy ON forex_transactions(company_id, financial_year);
CREATE INDEX IF NOT EXISTS idx_forex_transactions_related ON forex_transactions(related_forex_id) WHERE related_forex_id IS NOT NULL;
