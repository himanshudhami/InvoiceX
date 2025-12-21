-- Migration 076 Down: Remove backfilled payment allocations
DELETE FROM payment_allocations
WHERE notes = 'Backfilled from existing payment';
