-- Rollback: Remove senior citizen tax slab support

-- Drop index
DROP INDEX IF EXISTS idx_tax_slabs_category;

-- Remove senior citizen slabs
DELETE FROM tax_slabs WHERE applicable_to_category IN ('senior', 'super_senior');

-- Remove category column
ALTER TABLE tax_slabs DROP COLUMN IF EXISTS applicable_to_category;
