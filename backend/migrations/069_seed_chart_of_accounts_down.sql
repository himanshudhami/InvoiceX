-- Rollback Migration: Seed Chart of Accounts
-- Reverses changes from 069_seed_chart_of_accounts.sql

-- ============================================
-- DROP TRIGGER (if it was enabled)
-- ============================================
DROP TRIGGER IF EXISTS trg_auto_create_coa ON companies;

-- ============================================
-- DROP FUNCTIONS
-- ============================================
DROP FUNCTION IF EXISTS auto_create_coa_for_company();
DROP FUNCTION IF EXISTS create_default_chart_of_accounts(UUID, UUID);
