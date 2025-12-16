-- Migration: Enhance payments table for Indian tax compliance
-- Purpose: Support non-invoice payments, TDS tracking, and proper income categorization
--
-- Key Changes:
-- 1. Add company_id and customer_id for non-invoice payments
-- 2. Add payment_type to categorize payments (invoice, advance, direct income)
-- 3. Add TDS fields for tracking TDS deducted by customers (194J, 194C, etc.)
-- 4. Add income_category for financial reporting
-- 5. Add financial_year for tax year tracking

-- ============================================
-- COMPANY AND CUSTOMER LINKING
-- ============================================

-- Add company_id to track which company received the payment
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS company_id UUID REFERENCES companies(id);

-- Add customer_id for direct payments (not linked to invoice)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS customer_id UUID REFERENCES customers(id);

COMMENT ON COLUMN payments.company_id IS 'Company that received this payment';
COMMENT ON COLUMN payments.customer_id IS 'Customer who made this payment (for non-invoice payments)';

-- ============================================
-- PAYMENT CLASSIFICATION
-- ============================================

-- Payment type classification
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS payment_type VARCHAR(50) DEFAULT 'invoice_payment';

COMMENT ON COLUMN payments.payment_type IS 'Type: invoice_payment, advance_received, direct_income, refund_received';

-- Income category for P&L reporting
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS income_category VARCHAR(100);

COMMENT ON COLUMN payments.income_category IS 'Category: export_services, domestic_services, product_sale, interest, other';

-- Original currency of payment (before INR conversion)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS currency VARCHAR(10) DEFAULT 'INR';

COMMENT ON COLUMN payments.currency IS 'Original currency of payment';

-- ============================================
-- TDS TRACKING (when customer deducts TDS)
-- ============================================

-- TDS applicable flag
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tds_applicable BOOLEAN DEFAULT false;

-- TDS section (194J for professionals, 194C for contractors, etc.)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tds_section VARCHAR(20);

-- TDS rate applied
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tds_rate NUMERIC(5,2) DEFAULT 0;

-- TDS amount deducted
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tds_amount NUMERIC(18,2) DEFAULT 0;

-- Gross amount before TDS deduction (Net received = Gross - TDS)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS gross_amount NUMERIC(18,2);

COMMENT ON COLUMN payments.tds_applicable IS 'Whether TDS was deducted by the payer';
COMMENT ON COLUMN payments.tds_section IS 'TDS section: 194J (10%), 194C (1-2%), 194H (5%), 194O (1%)';
COMMENT ON COLUMN payments.tds_rate IS 'TDS rate percentage applied';
COMMENT ON COLUMN payments.tds_amount IS 'TDS amount deducted by payer';
COMMENT ON COLUMN payments.gross_amount IS 'Gross amount before TDS. Net received = gross_amount - tds_amount';

-- ============================================
-- FINANCIAL YEAR TRACKING
-- ============================================

-- Financial year for tax reporting (e.g., '2024-25')
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS financial_year VARCHAR(10);

COMMENT ON COLUMN payments.financial_year IS 'Indian financial year: 2024-25 format';

-- ============================================
-- DESCRIPTION FOR NON-INVOICE PAYMENTS
-- ============================================

-- Description field for non-invoice payments
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS description TEXT;

COMMENT ON COLUMN payments.description IS 'Description of payment (especially for non-invoice payments)';

-- ============================================
-- INDEXES
-- ============================================

-- Index for company filtering
CREATE INDEX IF NOT EXISTS idx_payments_company_id ON payments(company_id);

-- Index for customer filtering
CREATE INDEX IF NOT EXISTS idx_payments_customer_id ON payments(customer_id);

-- Index for payment type filtering
CREATE INDEX IF NOT EXISTS idx_payments_payment_type ON payments(payment_type);

-- Index for financial year filtering
CREATE INDEX IF NOT EXISTS idx_payments_financial_year ON payments(financial_year);

-- Index for TDS payments (for TDS reporting)
CREATE INDEX IF NOT EXISTS idx_payments_tds ON payments(tds_applicable, tds_section)
WHERE tds_applicable = true;

-- Composite index for income reports by company/year
CREATE INDEX IF NOT EXISTS idx_payments_income_report ON payments(company_id, financial_year, payment_type);

-- ============================================
-- FUNCTION: Calculate financial year from date
-- ============================================

CREATE OR REPLACE FUNCTION get_financial_year(payment_date DATE)
RETURNS VARCHAR(10) AS $$
DECLARE
    year_start INT;
    year_end INT;
BEGIN
    -- Indian FY runs Apr 1 to Mar 31
    IF EXTRACT(MONTH FROM payment_date) >= 4 THEN
        year_start := EXTRACT(YEAR FROM payment_date)::INT;
        year_end := year_start + 1;
    ELSE
        year_end := EXTRACT(YEAR FROM payment_date)::INT;
        year_start := year_end - 1;
    END IF;

    RETURN year_start::TEXT || '-' || RIGHT(year_end::TEXT, 2);
END;
$$ LANGUAGE plpgsql IMMUTABLE;

COMMENT ON FUNCTION get_financial_year(DATE) IS 'Returns Indian financial year (e.g., 2024-25) for a given date';

-- ============================================
-- TRIGGER: Auto-populate financial_year
-- ============================================

CREATE OR REPLACE FUNCTION payments_set_financial_year()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.financial_year IS NULL AND NEW.payment_date IS NOT NULL THEN
        NEW.financial_year := get_financial_year(NEW.payment_date);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_payments_set_financial_year ON payments;

CREATE TRIGGER trg_payments_set_financial_year
BEFORE INSERT OR UPDATE ON payments
FOR EACH ROW
EXECUTE FUNCTION payments_set_financial_year();

-- ============================================
-- BACKFILL: Set company_id from linked invoices
-- ============================================

UPDATE payments p
SET company_id = i.company_id
FROM invoices i
WHERE p.invoice_id = i.id
AND p.company_id IS NULL;

-- ============================================
-- BACKFILL: Set customer_id from linked invoices
-- ============================================

UPDATE payments p
SET customer_id = i.customer_id
FROM invoices i
WHERE p.invoice_id = i.id
AND p.customer_id IS NULL;

-- ============================================
-- BACKFILL: Set financial_year for existing payments
-- ============================================

UPDATE payments
SET financial_year = get_financial_year(payment_date)
WHERE financial_year IS NULL AND payment_date IS NOT NULL;

-- ============================================
-- BACKFILL: Set currency from linked invoices
-- ============================================

UPDATE payments p
SET currency = COALESCE(i.currency, 'INR')
FROM invoices i
WHERE p.invoice_id = i.id
AND p.currency IS NULL;

-- Set default currency for payments without invoices
UPDATE payments
SET currency = 'INR'
WHERE currency IS NULL;
