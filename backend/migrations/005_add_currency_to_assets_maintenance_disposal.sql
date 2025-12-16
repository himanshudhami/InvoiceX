-- 005_add_currency_to_assets_maintenance_disposal.sql
-- Adds currency support to asset maintenance and disposal records

-- Add currency to asset_maintenance
ALTER TABLE asset_maintenance
    ADD COLUMN IF NOT EXISTS currency VARCHAR(10);

-- Add currency to asset_disposals
ALTER TABLE asset_disposals
    ADD COLUMN IF NOT EXISTS currency VARCHAR(10);

-- Update existing records to use asset currency (if asset has currency)
UPDATE asset_maintenance am
SET currency = a.currency
FROM assets a
WHERE am.asset_id = a.id AND a.currency IS NOT NULL AND am.currency IS NULL;

UPDATE asset_disposals ad
SET currency = a.currency
FROM assets a
WHERE ad.asset_id = a.id AND a.currency IS NOT NULL AND ad.currency IS NULL;



