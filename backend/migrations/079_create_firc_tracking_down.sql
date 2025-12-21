-- Rollback: Drop FIRC tracking tables

DROP TRIGGER IF EXISTS trigger_update_firc_tracking_updated_at ON firc_tracking;
DROP FUNCTION IF EXISTS update_firc_tracking_updated_at();
DROP TABLE IF EXISTS firc_invoice_links;
DROP TABLE IF EXISTS firc_tracking;
