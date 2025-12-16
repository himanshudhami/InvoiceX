-- Rollback: Remove GST fields from products table

DROP INDEX IF EXISTS idx_products_hsn_sac;

ALTER TABLE products DROP COLUMN IF EXISTS hsn_sac_code;
ALTER TABLE products DROP COLUMN IF EXISTS is_service;
ALTER TABLE products DROP COLUMN IF EXISTS default_gst_rate;
ALTER TABLE products DROP COLUMN IF EXISTS cess_rate;
