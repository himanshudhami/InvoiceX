-- Rollback Migration: 040_create_users_and_refresh_tokens
-- Description: Drops users and refresh_tokens tables

-- Drop refresh_tokens table first (has FK to users)
DROP TABLE IF EXISTS refresh_tokens CASCADE;

-- Drop users table
DROP TABLE IF EXISTS users CASCADE;
