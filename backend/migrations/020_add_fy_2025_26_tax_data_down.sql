-- 020_add_fy_2025_26_tax_data_down.sql
-- Rollback: Remove FY 2025-26 tax data

DELETE FROM tax_slabs WHERE financial_year = '2025-26';
DELETE FROM tax_parameters WHERE financial_year = '2025-26';
