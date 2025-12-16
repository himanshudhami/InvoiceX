-- Rollback: Remove GST fields from invoice_items table

DROP INDEX IF EXISTS idx_invoice_items_hsn_sac;

ALTER TABLE invoice_items DROP COLUMN IF EXISTS hsn_sac_code;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS is_service;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS cgst_rate;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS cgst_amount;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS sgst_rate;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS sgst_amount;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS igst_rate;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS igst_amount;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS cess_rate;
ALTER TABLE invoice_items DROP COLUMN IF EXISTS cess_amount;
