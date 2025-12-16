-- 006_add_subscription_cost_tracking.sql
-- Adds cost per seat and pause/resume/cancel date tracking to subscriptions

-- Add cost_per_seat column
ALTER TABLE subscriptions
    ADD COLUMN IF NOT EXISTS cost_per_seat NUMERIC(14,2);

-- Add pause/resume/cancel date tracking
ALTER TABLE subscriptions
    ADD COLUMN IF NOT EXISTS paused_on DATE;

ALTER TABLE subscriptions
    ADD COLUMN IF NOT EXISTS resumed_on DATE;

ALTER TABLE subscriptions
    ADD COLUMN IF NOT EXISTS cancelled_on DATE;

-- Create index for status-based queries
CREATE INDEX IF NOT EXISTS idx_subscriptions_status_active ON subscriptions(company_id, status) WHERE status = 'active';




