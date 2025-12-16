-- Rollback: Remove rejection workflow fields from employee_tax_declarations

-- Drop index
DROP INDEX IF EXISTS idx_tax_declaration_rejected;

-- Remove columns
ALTER TABLE employee_tax_declarations
    DROP COLUMN IF EXISTS rejected_at,
    DROP COLUMN IF EXISTS rejected_by,
    DROP COLUMN IF EXISTS rejection_reason,
    DROP COLUMN IF EXISTS revision_count;

-- Restore original status constraint (without 'rejected')
ALTER TABLE employee_tax_declarations
    DROP CONSTRAINT IF EXISTS employee_tax_declarations_status_check;

ALTER TABLE employee_tax_declarations
    ADD CONSTRAINT employee_tax_declarations_status_check
    CHECK (status IN ('draft', 'submitted', 'verified', 'locked'));
