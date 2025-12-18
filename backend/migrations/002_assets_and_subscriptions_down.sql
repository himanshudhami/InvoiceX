-- 002_assets_and_subscriptions_down.sql
-- Rolls back asset management and subscription tracking schema

DROP TABLE IF EXISTS subscription_events CASCADE;
DROP TABLE IF EXISTS subscription_assignments CASCADE;
DROP TABLE IF EXISTS subscriptions CASCADE;
DROP TABLE IF EXISTS asset_depreciation CASCADE;
DROP TABLE IF EXISTS asset_events CASCADE;
DROP TABLE IF EXISTS asset_documents CASCADE;
DROP TABLE IF EXISTS asset_maintenance CASCADE;
DROP TABLE IF EXISTS asset_assignments CASCADE;
DROP TABLE IF EXISTS assets CASCADE;
DROP TABLE IF EXISTS asset_models CASCADE;
DROP TABLE IF EXISTS asset_categories CASCADE;








