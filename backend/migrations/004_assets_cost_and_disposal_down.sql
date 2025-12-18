-- 004_assets_cost_and_disposal_down.sql
-- Rolls back cost/disposal changes

DROP TABLE IF EXISTS asset_disposals;

ALTER TABLE assets DROP CONSTRAINT IF EXISTS assets_purchase_type_check;
ALTER TABLE assets DROP COLUMN IF EXISTS purchase_type;
ALTER TABLE assets DROP COLUMN IF EXISTS invoice_reference;
ALTER TABLE assets DROP COLUMN IF EXISTS depreciation_start_date;

DROP INDEX IF EXISTS idx_assets_purchase_type;




