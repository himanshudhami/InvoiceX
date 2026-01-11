-- Migration: Add missing columns to tally_migration_batches and tally_field_mappings
-- These columns were in the entities but missing from the original schema

-- tally_migration_batches missing columns
ALTER TABLE tally_migration_batches
ADD COLUMN IF NOT EXISTS skipped_stock_items INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS failed_stock_items INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS skipped_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS failed_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_godowns INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_godowns INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_units INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_units INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_stock_groups INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_stock_groups INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS parsing_started_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS validation_started_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS validation_completed_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS import_started_at TIMESTAMP WITH TIME ZONE;

-- tally_field_mappings missing columns
ALTER TABLE tally_field_mappings
ADD COLUMN IF NOT EXISTS target_account_subtype VARCHAR(50),
ADD COLUMN IF NOT EXISTS default_account_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tag_assignments JSONB DEFAULT '[]';

-- tally_migration_logs missing columns
ALTER TABLE tally_migration_logs
ADD COLUMN IF NOT EXISTS error_code VARCHAR(50),
ADD COLUMN IF NOT EXISTS amount_difference DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS processing_duration_ms INT;

-- bank_accounts missing columns
ALTER TABLE bank_accounts
ADD COLUMN IF NOT EXISTS linked_account_id UUID REFERENCES chart_of_accounts(id),
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_ledger_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_bank_accounts_tally_guid ON bank_accounts(company_id, tally_ledger_guid);

-- bank_transactions missing columns (reconciliation + tally)
ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS reconciliation_difference_amount DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS reconciliation_difference_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS reconciliation_difference_notes TEXT,
ADD COLUMN IF NOT EXISTS reconciliation_tds_section VARCHAR(20),
ADD COLUMN IF NOT EXISTS reconciliation_adjustment_journal_id UUID,
ADD COLUMN IF NOT EXISTS reconciled_journal_entry_id UUID,
ADD COLUMN IF NOT EXISTS reconciled_je_line_id UUID,
ADD COLUMN IF NOT EXISTS paired_transaction_id UUID,
ADD COLUMN IF NOT EXISTS pair_type VARCHAR(20),
ADD COLUMN IF NOT EXISTS reversal_journal_entry_id UUID,
ADD COLUMN IF NOT EXISTS is_reversal_transaction BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS source_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_id UUID,
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_bank_tx_tally_guid ON bank_transactions(tally_voucher_guid);
CREATE INDEX IF NOT EXISTS idx_bank_tx_matched ON bank_transactions(matched_entity_type, matched_entity_id);
