-- Rollback Migration: Payment Allocations and Bank Transaction Matching
-- Reverses changes from 067_payment_allocations.sql

-- ============================================
-- DROP VIEWS
-- ============================================
DROP VIEW IF EXISTS v_invoice_payment_status;
DROP VIEW IF EXISTS v_payment_allocation_summary;

-- ============================================
-- DROP TRIGGERS
-- ============================================
DROP TRIGGER IF EXISTS trg_update_bank_tx_reconciliation ON bank_transaction_matches;
DROP TRIGGER IF EXISTS trg_update_payment_unallocated ON payment_allocations;

-- ============================================
-- DROP FUNCTIONS
-- ============================================
DROP FUNCTION IF EXISTS update_bank_tx_reconciliation();
DROP FUNCTION IF EXISTS calculate_bank_tx_matched_amount(UUID);
DROP FUNCTION IF EXISTS update_payment_unallocated_amount();
DROP FUNCTION IF EXISTS calculate_unallocated_amount(UUID);

-- ============================================
-- DROP INDEXES
-- ============================================
DROP INDEX IF EXISTS idx_payments_is_reconciled;
DROP INDEX IF EXISTS idx_payments_bank_account_id;
DROP INDEX IF EXISTS idx_bank_tx_matches_company_id;
DROP INDEX IF EXISTS idx_bank_tx_matches_matched;
DROP INDEX IF EXISTS idx_bank_tx_matches_transaction_id;
DROP INDEX IF EXISTS idx_payment_allocations_company_date;
DROP INDEX IF EXISTS idx_payment_allocations_type;
DROP INDEX IF EXISTS idx_payment_allocations_date;
DROP INDEX IF EXISTS idx_payment_allocations_company_id;
DROP INDEX IF EXISTS idx_payment_allocations_invoice_id;
DROP INDEX IF EXISTS idx_payment_allocations_payment_id;

-- ============================================
-- REMOVE COLUMNS FROM PAYMENTS
-- ============================================
ALTER TABLE payments DROP COLUMN IF EXISTS unallocated_amount;
ALTER TABLE payments DROP COLUMN IF EXISTS reconciled_by;
ALTER TABLE payments DROP COLUMN IF EXISTS reconciled_at;
ALTER TABLE payments DROP COLUMN IF EXISTS is_reconciled;
ALTER TABLE payments DROP COLUMN IF EXISTS bank_account_id;

-- ============================================
-- DROP TABLES
-- ============================================
DROP TABLE IF EXISTS bank_transaction_matches;
DROP TABLE IF EXISTS payment_allocations;
