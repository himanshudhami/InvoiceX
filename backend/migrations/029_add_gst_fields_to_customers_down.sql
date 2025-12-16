-- Rollback: Remove GST fields from customers table

DROP INDEX IF EXISTS idx_customers_gstin;
DROP INDEX IF EXISTS idx_customers_type;
DROP INDEX IF EXISTS idx_customers_pan;

ALTER TABLE customers DROP COLUMN IF EXISTS gstin;
ALTER TABLE customers DROP COLUMN IF EXISTS gst_state_code;
ALTER TABLE customers DROP COLUMN IF EXISTS customer_type;
ALTER TABLE customers DROP COLUMN IF EXISTS is_gst_registered;
ALTER TABLE customers DROP COLUMN IF EXISTS pan_number;

DO $$
BEGIN
    RAISE NOTICE 'Removed GST fields from customers table';
END $$;
