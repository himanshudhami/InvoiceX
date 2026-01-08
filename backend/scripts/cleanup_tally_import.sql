-- Cleanup script to delete all Tally-imported data for a fresh re-import
-- Run this in order due to foreign key constraints

BEGIN;

-- 1. Delete journal entry lines first (FK to journal_entries)
DELETE FROM journal_entry_lines
WHERE journal_entry_id IN (
    SELECT id FROM journal_entries WHERE tally_voucher_guid IS NOT NULL
);

-- 2. Delete Tally-imported journal entries
DELETE FROM journal_entries WHERE tally_voucher_guid IS NOT NULL;

-- 3. Delete vendor payment allocations (FK to vendor_payments)
DELETE FROM vendor_payment_allocations
WHERE vendor_payment_id IN (
    SELECT id FROM vendor_payments WHERE tally_voucher_guid IS NOT NULL
);

-- 4. Delete Tally-imported vendor payments
DELETE FROM vendor_payments WHERE tally_voucher_guid IS NOT NULL;

-- 5. Delete vendor invoice items (FK to vendor_invoices)
DELETE FROM vendor_invoice_items
WHERE vendor_invoice_id IN (
    SELECT id FROM vendor_invoices WHERE tally_voucher_guid IS NOT NULL
);

-- 6. Delete Tally-imported vendor invoices
DELETE FROM vendor_invoices WHERE tally_voucher_guid IS NOT NULL;

-- 7. Delete Tally-imported vendors
DELETE FROM vendors WHERE tally_ledger_guid IS NOT NULL;

-- 8. Delete Tally-imported chart of accounts (be careful - only delete those with tally_ledger_guid)
DELETE FROM chart_of_accounts WHERE tally_ledger_guid IS NOT NULL;

-- 9. Delete migration logs
DELETE FROM tally_migration_logs;

-- 10. Delete migration batches
DELETE FROM tally_migration_batches;

-- 11. Fix the field mappings for next import
UPDATE tally_field_mappings SET target_entity = 'vendors' WHERE tally_group_name = 'CONSULTANTS';
UPDATE tally_field_mappings SET target_entity = 'chart_of_accounts' WHERE tally_group_name = 'Input Tax';
UPDATE tally_field_mappings SET target_entity = 'chart_of_accounts' WHERE tally_group_name = 'Output Tax';

-- 12. Delete any mappings pointing to suspense and let the code handle them with updated logic
DELETE FROM tally_field_mappings WHERE target_entity = 'suspense';

COMMIT;

-- Verify cleanup
SELECT 'Remaining Tally vendors' as check_type, COUNT(*) as count FROM vendors WHERE tally_ledger_guid IS NOT NULL
UNION ALL
SELECT 'Remaining Tally invoices', COUNT(*) FROM vendor_invoices WHERE tally_voucher_guid IS NOT NULL
UNION ALL
SELECT 'Remaining Tally payments', COUNT(*) FROM vendor_payments WHERE tally_voucher_guid IS NOT NULL
UNION ALL
SELECT 'Remaining Tally journals', COUNT(*) FROM journal_entries WHERE tally_voucher_guid IS NOT NULL
UNION ALL
SELECT 'Migration batches', COUNT(*) FROM tally_migration_batches
UNION ALL
SELECT 'Migration logs', COUNT(*) FROM tally_migration_logs;
