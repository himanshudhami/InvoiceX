-- Migration: 093_add_attachment_type_column.sql
-- Description: Add attachment_type column to expense_attachments to distinguish
--              between employee receipts and admin reimbursement proof

-- Add attachment_type column
ALTER TABLE expense_attachments
ADD COLUMN IF NOT EXISTS attachment_type VARCHAR(50) DEFAULT 'employee_receipt';

-- Add uploaded_by column to track who uploaded the attachment
ALTER TABLE expense_attachments
ADD COLUMN IF NOT EXISTS uploaded_by UUID REFERENCES users(id);

-- Update existing records to have the default type
UPDATE expense_attachments SET attachment_type = 'employee_receipt' WHERE attachment_type IS NULL;

-- Add comment for documentation
COMMENT ON COLUMN expense_attachments.attachment_type IS 'Type of attachment: employee_receipt, reimbursement_proof, approval_note';
COMMENT ON COLUMN expense_attachments.uploaded_by IS 'User who uploaded the attachment (employee or admin)';

-- Create index for filtering by type
CREATE INDEX IF NOT EXISTS idx_expense_attachments_type ON expense_attachments(attachment_type);
