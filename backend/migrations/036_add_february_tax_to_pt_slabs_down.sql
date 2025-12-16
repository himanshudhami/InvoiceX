-- Rollback: Remove february_tax and effective date columns

DROP INDEX IF EXISTS idx_pt_slabs_effective;

ALTER TABLE professional_tax_slabs
DROP COLUMN IF EXISTS february_tax;

ALTER TABLE professional_tax_slabs
DROP COLUMN IF EXISTS effective_from;

ALTER TABLE professional_tax_slabs
DROP COLUMN IF EXISTS effective_to;
