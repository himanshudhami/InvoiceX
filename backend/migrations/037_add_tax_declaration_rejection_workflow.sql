-- Migration: Add rejection workflow fields to employee_tax_declarations
-- Purpose: Support rejection with reason and revision tracking

-- Add rejection workflow columns
ALTER TABLE employee_tax_declarations
    ADD COLUMN IF NOT EXISTS rejected_at TIMESTAMP WITHOUT TIME ZONE,
    ADD COLUMN IF NOT EXISTS rejected_by VARCHAR(255),
    ADD COLUMN IF NOT EXISTS rejection_reason TEXT,
    ADD COLUMN IF NOT EXISTS revision_count INTEGER DEFAULT 0;

-- Update status constraint to include 'rejected'
ALTER TABLE employee_tax_declarations
    DROP CONSTRAINT IF EXISTS employee_tax_declarations_status_check;

ALTER TABLE employee_tax_declarations
    ADD CONSTRAINT employee_tax_declarations_status_check
    CHECK (status IN ('draft', 'submitted', 'verified', 'rejected', 'locked'));

-- Create index for efficient rejected declarations lookup
CREATE INDEX IF NOT EXISTS idx_tax_declaration_rejected
    ON employee_tax_declarations(rejected_at)
    WHERE rejected_at IS NOT NULL;

-- Add comment for documentation
COMMENT ON COLUMN employee_tax_declarations.rejected_at IS 'Timestamp when declaration was rejected';
COMMENT ON COLUMN employee_tax_declarations.rejected_by IS 'User who rejected the declaration';
COMMENT ON COLUMN employee_tax_declarations.rejection_reason IS 'Reason for rejection';
COMMENT ON COLUMN employee_tax_declarations.revision_count IS 'Number of times declaration has been revised after rejection';
