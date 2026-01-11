-- Down migration for 165: Revert voucher type routing fix
-- Note: This cannot fully restore original state as we don't track original values
-- Manual review required after rollback

-- This is a data migration - rollback requires manual intervention
-- The original issue was incorrect routing, so rolling back would restore the bug
RAISE NOTICE 'This migration cannot be automatically rolled back. Manual intervention required.';
