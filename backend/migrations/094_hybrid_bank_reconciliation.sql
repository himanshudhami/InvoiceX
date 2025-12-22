-- Migration: 094_hybrid_bank_reconciliation.sql
-- Description: Add journal entry links to bank transactions for hybrid reconciliation
-- This enables reconciliation against both source documents AND journal entries
-- Validated by CA and Architect perspectives for Indian compliance

-- ============================================================================
-- PHASE 1: Add JE links to bank_transactions table
-- ============================================================================

-- Add reconciled_journal_entry_id - links to the parent journal entry
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS reconciled_journal_entry_id UUID REFERENCES journal_entries(id);

-- Add reconciled_je_line_id - links to the specific JE line affecting bank account
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS reconciled_je_line_id UUID REFERENCES journal_entry_lines(id);

-- Add comment for documentation
COMMENT ON COLUMN bank_transactions.reconciled_journal_entry_id IS
    'Journal Entry ID for the source document JE (enables BRS from ledger perspective)';
COMMENT ON COLUMN bank_transactions.reconciled_je_line_id IS
    'Specific JE line ID that affects the bank account (for audit trail)';

-- ============================================================================
-- PHASE 2: Add ledger account link to bank_accounts (if not exists)
-- ============================================================================

-- Check if column exists before adding (idempotent)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'bank_accounts' AND column_name = 'linked_account_id'
    ) THEN
        ALTER TABLE bank_accounts
        ADD COLUMN linked_account_id UUID REFERENCES chart_of_accounts(id);

        COMMENT ON COLUMN bank_accounts.linked_account_id IS
            'Linked Chart of Account ID for this bank account (required for BRS generation)';
    END IF;
END $$;

-- ============================================================================
-- PHASE 3: Create indexes for efficient lookups
-- ============================================================================

-- Index for finding bank transactions by JE
CREATE INDEX IF NOT EXISTS idx_bank_tx_je_id
ON bank_transactions(reconciled_journal_entry_id)
WHERE reconciled_journal_entry_id IS NOT NULL;

-- Index for finding bank transactions by JE line
CREATE INDEX IF NOT EXISTS idx_bank_tx_je_line_id
ON bank_transactions(reconciled_je_line_id)
WHERE reconciled_je_line_id IS NOT NULL;

-- Index for finding bank account's ledger account
CREATE INDEX IF NOT EXISTS idx_bank_accounts_linked_account
ON bank_accounts(linked_account_id)
WHERE linked_account_id IS NOT NULL;

-- Index for JE source lookup (may already exist from migration 068)
CREATE INDEX IF NOT EXISTS idx_je_source
ON journal_entries(source_type, source_id);

-- Index for finding JE lines by account
CREATE INDEX IF NOT EXISTS idx_jel_account_id
ON journal_entry_lines(account_id);

-- ============================================================================
-- PHASE 4: Update chart_of_accounts for bank account identification
-- ============================================================================

-- Add is_bank_account flag if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'chart_of_accounts' AND column_name = 'is_bank_account'
    ) THEN
        ALTER TABLE chart_of_accounts
        ADD COLUMN is_bank_account BOOLEAN DEFAULT FALSE;

        COMMENT ON COLUMN chart_of_accounts.is_bank_account IS
            'Quick flag to identify bank accounts in chart of accounts';
    END IF;
END $$;

-- Add linked_bank_account_id for reverse lookup if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'chart_of_accounts' AND column_name = 'linked_bank_account_id'
    ) THEN
        ALTER TABLE chart_of_accounts
        ADD COLUMN linked_bank_account_id UUID REFERENCES bank_accounts(id);

        COMMENT ON COLUMN chart_of_accounts.linked_bank_account_id IS
            'Reverse link to bank_accounts table for bank-type accounts';
    END IF;
END $$;

-- Index for bank accounts in COA
CREATE INDEX IF NOT EXISTS idx_coa_bank_accounts
ON chart_of_accounts(is_bank_account)
WHERE is_bank_account = true;

-- ============================================================================
-- PHASE 5: Add JE columns to bank_transaction_matches for split reconciliation
-- ============================================================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'bank_transaction_matches' AND column_name = 'journal_entry_id'
    ) THEN
        ALTER TABLE bank_transaction_matches
        ADD COLUMN journal_entry_id UUID REFERENCES journal_entries(id),
        ADD COLUMN journal_entry_line_id UUID REFERENCES journal_entry_lines(id);

        COMMENT ON COLUMN bank_transaction_matches.journal_entry_id IS
            'Journal Entry ID for this match (for split reconciliation with JE links)';
        COMMENT ON COLUMN bank_transaction_matches.journal_entry_line_id IS
            'Journal Entry Line ID for this match';
    END IF;
END $$;

-- ============================================================================
-- DONE
-- ============================================================================
-- Next step: Run migration 095 to backfill existing data
