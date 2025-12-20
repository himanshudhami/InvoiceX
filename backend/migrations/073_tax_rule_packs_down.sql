-- Down Migration: 100_tax_rule_packs_down.sql
-- Rollback tax rule packs schema

-- Remove columns from existing tables
ALTER TABLE payments DROP COLUMN IF EXISTS tds_rule_pack_id;
ALTER TABLE contractor_payments DROP COLUMN IF EXISTS rule_pack_id;
ALTER TABLE payroll_runs DROP COLUMN IF EXISTS rules_snapshot;
ALTER TABLE payroll_runs DROP COLUMN IF EXISTS rule_pack_id;

-- Drop view
DROP VIEW IF EXISTS v_active_rule_packs;

-- Drop functions
DROP FUNCTION IF EXISTS get_financial_year(DATE);
DROP FUNCTION IF EXISTS get_active_rule_pack(VARCHAR);

-- Drop tables in order (respecting foreign keys)
DROP TABLE IF EXISTS tds_section_rates;
DROP TABLE IF EXISTS rule_pack_usage_log;
DROP TABLE IF EXISTS tax_rule_packs;
