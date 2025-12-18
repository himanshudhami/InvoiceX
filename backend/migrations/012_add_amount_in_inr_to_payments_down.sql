-- Rollback: Remove amount_in_inr column from payments table

DROP INDEX IF EXISTS idx_payments_amount_in_inr;

ALTER TABLE payments 
DROP COLUMN IF EXISTS amount_in_inr;




