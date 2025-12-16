-- 016_create_tax_parameters_down.sql
-- Rolls back the tax_parameters table creation

DROP INDEX IF EXISTS idx_tax_params_active;
DROP INDEX IF EXISTS idx_tax_params_code;
DROP INDEX IF EXISTS idx_tax_params_fy_regime;
DROP TABLE IF EXISTS tax_parameters CASCADE;
