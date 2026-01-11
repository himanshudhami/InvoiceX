-- Rollback: Remove added columns from tally_migration_batches and tally_field_mappings

ALTER TABLE tally_migration_batches
DROP COLUMN IF EXISTS skipped_stock_items,
DROP COLUMN IF EXISTS failed_stock_items,
DROP COLUMN IF EXISTS total_cost_centers,
DROP COLUMN IF EXISTS imported_cost_centers,
DROP COLUMN IF EXISTS skipped_cost_centers,
DROP COLUMN IF EXISTS failed_cost_centers,
DROP COLUMN IF EXISTS total_godowns,
DROP COLUMN IF EXISTS imported_godowns,
DROP COLUMN IF EXISTS total_units,
DROP COLUMN IF EXISTS imported_units,
DROP COLUMN IF EXISTS total_stock_groups,
DROP COLUMN IF EXISTS imported_stock_groups,
DROP COLUMN IF EXISTS parsing_started_at,
DROP COLUMN IF EXISTS validation_started_at,
DROP COLUMN IF EXISTS validation_completed_at,
DROP COLUMN IF EXISTS import_started_at;

ALTER TABLE tally_field_mappings
DROP COLUMN IF EXISTS target_account_subtype,
DROP COLUMN IF EXISTS default_account_name,
DROP COLUMN IF EXISTS tag_assignments;

ALTER TABLE tally_migration_logs
DROP COLUMN IF EXISTS error_code,
DROP COLUMN IF EXISTS amount_difference,
DROP COLUMN IF EXISTS processing_duration_ms;

DROP INDEX IF EXISTS idx_bank_accounts_tally_guid;

ALTER TABLE bank_accounts
DROP COLUMN IF EXISTS linked_account_id,
DROP COLUMN IF EXISTS tally_ledger_guid,
DROP COLUMN IF EXISTS tally_ledger_name,
DROP COLUMN IF EXISTS tally_migration_batch_id;

DROP INDEX IF EXISTS idx_bank_tx_tally_guid;
DROP INDEX IF EXISTS idx_bank_tx_matched;

ALTER TABLE bank_transactions
DROP COLUMN IF EXISTS reconciliation_difference_amount,
DROP COLUMN IF EXISTS reconciliation_difference_type,
DROP COLUMN IF EXISTS reconciliation_difference_notes,
DROP COLUMN IF EXISTS reconciliation_tds_section,
DROP COLUMN IF EXISTS reconciliation_adjustment_journal_id,
DROP COLUMN IF EXISTS reconciled_journal_entry_id,
DROP COLUMN IF EXISTS reconciled_je_line_id,
DROP COLUMN IF EXISTS paired_transaction_id,
DROP COLUMN IF EXISTS pair_type,
DROP COLUMN IF EXISTS reversal_journal_entry_id,
DROP COLUMN IF EXISTS is_reversal_transaction,
DROP COLUMN IF EXISTS source_voucher_type,
DROP COLUMN IF EXISTS matched_entity_type,
DROP COLUMN IF EXISTS matched_entity_id,
DROP COLUMN IF EXISTS tally_voucher_guid,
DROP COLUMN IF EXISTS tally_voucher_number,
DROP COLUMN IF EXISTS tally_migration_batch_id;
