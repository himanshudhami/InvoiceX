-- Rollback Migration: General Ledger System
-- Reverses changes from 068_general_ledger.sql

-- ============================================
-- DROP VIEWS
-- ============================================
DROP VIEW IF EXISTS v_balance_sheet;
DROP VIEW IF EXISTS v_income_statement;
DROP VIEW IF EXISTS v_account_ledger;
DROP VIEW IF EXISTS v_trial_balance;

-- ============================================
-- DROP TRIGGERS
-- ============================================
DROP TRIGGER IF EXISTS trg_update_balance_on_post ON journal_entries;

-- ============================================
-- DROP FUNCTIONS
-- ============================================
DROP FUNCTION IF EXISTS update_balance_on_post();
DROP FUNCTION IF EXISTS update_account_balance();
DROP FUNCTION IF EXISTS get_fy_period_month(DATE);
DROP FUNCTION IF EXISTS generate_journal_number(UUID, VARCHAR);

-- ============================================
-- DROP INDEXES
-- ============================================
-- Period Balances
DROP INDEX IF EXISTS idx_apb_period;
DROP INDEX IF EXISTS idx_apb_account;

-- Posting Rules
DROP INDEX IF EXISTS idx_pr_active;
DROP INDEX IF EXISTS idx_pr_source;
DROP INDEX IF EXISTS idx_pr_company;

-- Journal Entry Lines
DROP INDEX IF EXISTS idx_jel_subledger;
DROP INDEX IF EXISTS idx_jel_account;
DROP INDEX IF EXISTS idx_jel_entry;

-- Journal Entries
DROP INDEX IF EXISTS idx_je_posted;
DROP INDEX IF EXISTS idx_je_source;
DROP INDEX IF EXISTS idx_je_status;
DROP INDEX IF EXISTS idx_je_fy_period;
DROP INDEX IF EXISTS idx_je_date;
DROP INDEX IF EXISTS idx_je_company;

-- Chart of Accounts
DROP INDEX IF EXISTS idx_coa_active;
DROP INDEX IF EXISTS idx_coa_type;
DROP INDEX IF EXISTS idx_coa_parent;
DROP INDEX IF EXISTS idx_coa_company;

-- ============================================
-- DROP TABLES (in correct order due to foreign keys)
-- ============================================
DROP TABLE IF EXISTS account_period_balances;
DROP TABLE IF EXISTS posting_rule_usage_log;
DROP TABLE IF EXISTS posting_rules;
DROP TABLE IF EXISTS journal_entry_lines;
DROP TABLE IF EXISTS journal_entries;
DROP TABLE IF EXISTS chart_of_accounts;
