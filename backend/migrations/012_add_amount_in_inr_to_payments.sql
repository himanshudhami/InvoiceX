-- Add amount_in_inr column to payments table
-- This allows recording the actual INR amount received when payment is made
-- Critical for accurate cash flow statements and bank reconciliation

ALTER TABLE payments 
ADD COLUMN amount_in_inr NUMERIC(14,2);

-- Add comment explaining the field
COMMENT ON COLUMN payments.amount_in_inr IS 'Actual INR amount received at payment time. Used for accurate cash flow and bank reconciliation when invoice currency differs from INR.';

-- Create index for efficient queries
CREATE INDEX IF NOT EXISTS idx_payments_amount_in_inr ON payments(amount_in_inr) WHERE amount_in_inr IS NOT NULL;




