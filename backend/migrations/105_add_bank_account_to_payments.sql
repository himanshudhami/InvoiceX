-- ============================================================================
-- Migration 105: Add Bank Account to Payroll and Contractor Payments
-- Description: Add bank_account_id column for payment tracking and journal entries
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Add bank_account_id to payroll_runs
-- -----------------------------------------------------------------------------
ALTER TABLE payroll_runs
ADD COLUMN IF NOT EXISTS bank_account_id UUID REFERENCES bank_accounts(id);

COMMENT ON COLUMN payroll_runs.bank_account_id IS
    'Bank account used for salary disbursement';

-- Create index for bank account lookups
CREATE INDEX IF NOT EXISTS idx_payroll_runs_bank_account
ON payroll_runs(bank_account_id)
WHERE bank_account_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 2. Add bank_account_id to contractor_payments
-- -----------------------------------------------------------------------------
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS bank_account_id UUID REFERENCES bank_accounts(id);

COMMENT ON COLUMN contractor_payments.bank_account_id IS
    'Bank account used for contractor payment';

-- Create index for bank account lookups
CREATE INDEX IF NOT EXISTS idx_contractor_payments_bank_account
ON contractor_payments(bank_account_id)
WHERE bank_account_id IS NOT NULL;
