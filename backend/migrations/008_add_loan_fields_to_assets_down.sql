-- 008_add_loan_fields_to_assets_down.sql
-- Rolls back the loan fields added to assets table

DROP INDEX IF EXISTS idx_assets_linked_loan_id;

ALTER TABLE assets
    DROP CONSTRAINT IF EXISTS assets_gst_rate_check,
    DROP COLUMN IF EXISTS linked_loan_id,
    DROP COLUMN IF EXISTS down_payment_amount,
    DROP COLUMN IF EXISTS gst_amount,
    DROP COLUMN IF EXISTS gst_rate,
    DROP COLUMN IF EXISTS itc_eligible,
    DROP COLUMN IF EXISTS tds_on_interest;





