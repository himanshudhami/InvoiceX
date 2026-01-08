-- Prepare database for Tally import
-- Run this AFTER migrations but BEFORE Tally import
-- Keeps only essential system accounts that Tally doesn't have

BEGIN;

-- ============================================
-- Delete seeded COA that will conflict with Tally
-- Keep only essential system accounts
-- ============================================

-- List of account codes to KEEP (system/infrastructure accounts)
-- These don't exist in Tally and are needed for system operations

-- Essential accounts to keep:
-- SUSPENSE-IMPORT: For unmapped Tally data
-- AR_FOREX, BANK_USD, REVENUE_EXPORT: For forex transactions
-- FX_GAIN_REAL, FX_LOSS_REAL, FX_GAIN_UNREAL, FX_LOSS_UNREAL: Forex P&L
-- 3210: Retained Earnings (system account for period close)
-- 3220: Current Year Profit/Loss (system account)

-- Delete all seeded accounts EXCEPT essential system ones
DELETE FROM chart_of_accounts
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
AND tally_ledger_guid IS NULL  -- Only delete non-Tally accounts
AND account_code NOT IN (
    -- Forex accounts (needed for export invoices)
    'AR_FOREX', 'REVENUE_EXPORT', 'BANK_USD',
    'FX_GAIN_REAL', 'FX_LOSS_REAL', 'FX_GAIN_UNREAL', 'FX_LOSS_UNREAL',
    -- Suspense account (needed for unmapped Tally data)
    'SUSPENSE-IMPORT',
    -- Essential equity accounts
    '3210', '3220'
);

-- Clear any failed migration attempts
DELETE FROM tally_migration_logs;
DELETE FROM tally_migration_batches;

-- Reset field mappings to clean state
DELETE FROM tally_field_mappings
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

COMMIT;

-- Verify what's left
SELECT
    'Remaining accounts' as check_type,
    account_code,
    account_name,
    account_type
FROM chart_of_accounts
WHERE company_id = '43e030dc-d522-49e0-819a-31744383b2e2'
ORDER BY account_code;
