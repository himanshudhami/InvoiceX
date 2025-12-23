-- ============================================================================
-- Migration 102: Payroll Journal Linkage
-- Description: Add journal entry references to payroll tables, add idempotency support
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Add journal entry references to payroll_runs
-- -----------------------------------------------------------------------------
ALTER TABLE payroll_runs
ADD COLUMN IF NOT EXISTS accrual_journal_entry_id UUID REFERENCES journal_entries(id),
ADD COLUMN IF NOT EXISTS disbursement_journal_entry_id UUID REFERENCES journal_entries(id);

-- Add comments for documentation
COMMENT ON COLUMN payroll_runs.accrual_journal_entry_id IS
    'Journal entry created on payroll approval (expense recognition)';
COMMENT ON COLUMN payroll_runs.disbursement_journal_entry_id IS
    'Journal entry created on salary disbursement (liability settlement)';

-- -----------------------------------------------------------------------------
-- 2. Add idempotency key to journal_entries
-- -----------------------------------------------------------------------------
ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS idempotency_key VARCHAR(100);

-- Create unique index for idempotency (only on non-null values)
CREATE UNIQUE INDEX IF NOT EXISTS idx_journal_entries_idempotency_key
ON journal_entries(idempotency_key)
WHERE idempotency_key IS NOT NULL;

COMMENT ON COLUMN journal_entries.idempotency_key IS
    'Unique key to prevent duplicate journal entries for same source event';

-- -----------------------------------------------------------------------------
-- 3. Add correction tracking to journal_entries
-- -----------------------------------------------------------------------------
ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS correction_of_id UUID REFERENCES journal_entries(id);

COMMENT ON COLUMN journal_entries.correction_of_id IS
    'Reference to original entry that this entry corrects (for audit trail)';

-- Create index for correction lookups
CREATE INDEX IF NOT EXISTS idx_journal_entries_correction_of
ON journal_entries(correction_of_id)
WHERE correction_of_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 4. Add rule_code to journal_entries for easier tracking
-- -----------------------------------------------------------------------------
ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS rule_code VARCHAR(50);

COMMENT ON COLUMN journal_entries.rule_code IS
    'Posting rule code used to create this entry (e.g., PAYROLL_ACCRUAL, PAYROLL_DISBURSEMENT)';

-- Create index for rule_code queries
CREATE INDEX IF NOT EXISTS idx_journal_entries_rule_code
ON journal_entries(rule_code)
WHERE rule_code IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 5. Add indexes for payroll-journal joins
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_payroll_runs_accrual_je
ON payroll_runs(accrual_journal_entry_id)
WHERE accrual_journal_entry_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_payroll_runs_disbursement_je
ON payroll_runs(disbursement_journal_entry_id)
WHERE disbursement_journal_entry_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 6. Add compound index for source lookups on journal_entries
-- -----------------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_journal_entries_source
ON journal_entries(source_type, source_id)
WHERE source_type IS NOT NULL AND source_id IS NOT NULL;
