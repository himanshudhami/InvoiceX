-- Down Migration: 101_seed_fy_2025_26_down.sql
-- Remove seeded tax rule packs

-- Delete TDS section rates for seeded packs
DELETE FROM tds_section_rates
WHERE rule_pack_id IN (
    SELECT id FROM tax_rule_packs
    WHERE pack_code IN ('FY_2025_26_V1', 'FY_2024_25_V1')
);

-- Delete seeded tax rule packs
DELETE FROM tax_rule_packs
WHERE pack_code IN ('FY_2025_26_V1', 'FY_2024_25_V1');

-- Drop index if exists
DROP INDEX IF EXISTS idx_tax_rule_packs_fy_active;
