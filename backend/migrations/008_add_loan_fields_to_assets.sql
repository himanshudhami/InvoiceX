-- 008_add_loan_fields_to_assets.sql
-- Adds loan-related fields to assets table for loan-financed purchases

ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS linked_loan_id UUID REFERENCES loans(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS down_payment_amount NUMERIC(14,2),
    ADD COLUMN IF NOT EXISTS gst_amount NUMERIC(14,2),
    ADD COLUMN IF NOT EXISTS gst_rate NUMERIC(5,2),
    ADD COLUMN IF NOT EXISTS itc_eligible BOOLEAN DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS tds_on_interest NUMERIC(14,2);

CREATE INDEX IF NOT EXISTS idx_assets_linked_loan_id ON assets(linked_loan_id);

-- Add constraint for GST rate if provided
ALTER TABLE assets
    ADD CONSTRAINT assets_gst_rate_check CHECK (gst_rate IS NULL OR (gst_rate >= 0 AND gst_rate <= 100));





