-- Rollback Migration: Seed Posting Rules
-- Reverses changes from 070_seed_posting_rules.sql

-- ============================================
-- DROP FUNCTION
-- ============================================
DROP FUNCTION IF EXISTS create_default_posting_rules(UUID, UUID);
