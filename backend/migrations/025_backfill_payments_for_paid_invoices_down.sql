-- Rollback: Remove auto-created payment records from backfill migration
-- Only removes payments that were automatically created by migration 025

DELETE FROM payments
WHERE notes = 'Auto-created from paid invoice during migration';
