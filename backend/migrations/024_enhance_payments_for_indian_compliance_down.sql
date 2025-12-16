-- Rollback: Remove Indian compliance enhancements from payments table

-- Drop triggers
DROP TRIGGER IF EXISTS trg_payments_set_financial_year ON payments;

-- Drop functions
DROP FUNCTION IF EXISTS payments_set_financial_year();
DROP FUNCTION IF EXISTS get_financial_year(DATE);

-- Drop indexes
DROP INDEX IF EXISTS idx_payments_company_id;
DROP INDEX IF EXISTS idx_payments_customer_id;
DROP INDEX IF EXISTS idx_payments_payment_type;
DROP INDEX IF EXISTS idx_payments_financial_year;
DROP INDEX IF EXISTS idx_payments_tds;
DROP INDEX IF EXISTS idx_payments_income_report;

-- Drop columns (reverse order of addition)
ALTER TABLE payments DROP COLUMN IF EXISTS description;
ALTER TABLE payments DROP COLUMN IF EXISTS financial_year;
ALTER TABLE payments DROP COLUMN IF EXISTS gross_amount;
ALTER TABLE payments DROP COLUMN IF EXISTS tds_amount;
ALTER TABLE payments DROP COLUMN IF EXISTS tds_rate;
ALTER TABLE payments DROP COLUMN IF EXISTS tds_section;
ALTER TABLE payments DROP COLUMN IF EXISTS tds_applicable;
ALTER TABLE payments DROP COLUMN IF EXISTS currency;
ALTER TABLE payments DROP COLUMN IF EXISTS income_category;
ALTER TABLE payments DROP COLUMN IF EXISTS payment_type;
ALTER TABLE payments DROP COLUMN IF EXISTS customer_id;
ALTER TABLE payments DROP COLUMN IF EXISTS company_id;
