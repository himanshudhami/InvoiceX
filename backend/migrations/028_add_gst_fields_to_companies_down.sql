-- Rollback: Remove GST/PAN fields from companies table

DROP INDEX IF EXISTS idx_companies_gstin;
DROP INDEX IF EXISTS idx_companies_pan;

ALTER TABLE companies DROP COLUMN IF EXISTS gstin;
ALTER TABLE companies DROP COLUMN IF EXISTS gst_state_code;
ALTER TABLE companies DROP COLUMN IF EXISTS pan_number;
ALTER TABLE companies DROP COLUMN IF EXISTS cin_number;
ALTER TABLE companies DROP COLUMN IF EXISTS gst_registration_type;

DO $$
BEGIN
    RAISE NOTICE 'Removed GST/PAN fields from companies table';
END $$;
