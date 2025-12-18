-- 006_add_subscription_cost_tracking_down.sql
-- Rollback migration for subscription cost tracking

-- Drop index
DROP INDEX IF EXISTS idx_subscriptions_status_active;

-- Remove columns
ALTER TABLE subscriptions
    DROP COLUMN IF EXISTS cancelled_on;

ALTER TABLE subscriptions
    DROP COLUMN IF EXISTS resumed_on;

ALTER TABLE subscriptions
    DROP COLUMN IF EXISTS paused_on;

ALTER TABLE subscriptions
    DROP COLUMN IF EXISTS cost_per_seat;





