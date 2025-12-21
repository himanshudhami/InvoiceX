-- Rollback: Drop LUT register table

DROP TRIGGER IF EXISTS trigger_update_lut_register_updated_at ON lut_register;
DROP FUNCTION IF EXISTS update_lut_register_updated_at();
DROP INDEX IF EXISTS idx_lut_company_fy_active;
DROP TABLE IF EXISTS lut_register;
