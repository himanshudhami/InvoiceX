-- Migration: 040_create_users_and_refresh_tokens
-- Description: Creates users and refresh_tokens tables for authentication
-- Date: 2025-12-17

-- Users table for authentication
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'Employee',
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    employee_id UUID REFERENCES employees(id) ON DELETE SET NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMP,
    failed_login_attempts INTEGER NOT NULL DEFAULT 0,
    lockout_end_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255),
    updated_by VARCHAR(255)
);

-- Unique index on email (case-insensitive)
CREATE UNIQUE INDEX idx_users_email_unique ON users (LOWER(email));

-- Index for company lookup
CREATE INDEX idx_users_company_id ON users (company_id);

-- Index for employee lookup
CREATE INDEX idx_users_employee_id ON users (employee_id);

-- Index for role filtering
CREATE INDEX idx_users_role ON users (role);

-- Index for active users
CREATE INDEX idx_users_is_active ON users (is_active) WHERE is_active = TRUE;

-- Refresh tokens table for JWT refresh token rotation
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token VARCHAR(512) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    is_revoked BOOLEAN NOT NULL DEFAULT FALSE,
    revoked_at TIMESTAMP,
    revoked_reason VARCHAR(255),
    created_by_ip VARCHAR(50),
    created_by_user_agent TEXT,
    replaced_by_token_id UUID REFERENCES refresh_tokens(id),
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Unique index on token
CREATE UNIQUE INDEX idx_refresh_tokens_token ON refresh_tokens (token);

-- Index for user lookup
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens (user_id);

-- Index for active token lookup
CREATE INDEX idx_refresh_tokens_active ON refresh_tokens (user_id, is_revoked, expires_at)
    WHERE is_revoked = FALSE;

-- Index for cleanup job (expired tokens)
CREATE INDEX idx_refresh_tokens_expires_at ON refresh_tokens (expires_at);

-- Add check constraint for valid roles
ALTER TABLE users ADD CONSTRAINT chk_users_role
    CHECK (role IN ('Admin', 'HR', 'Accountant', 'Manager', 'Employee'));

-- Comment on tables
COMMENT ON TABLE users IS 'User accounts for authentication';
COMMENT ON TABLE refresh_tokens IS 'JWT refresh tokens for token rotation';

-- Comment on important columns
COMMENT ON COLUMN users.role IS 'User role: Admin, HR, Accountant, Manager, Employee';
COMMENT ON COLUMN users.employee_id IS 'Link to employee record (null for admin-only users)';
COMMENT ON COLUMN users.failed_login_attempts IS 'Counter for failed login attempts (for lockout)';
COMMENT ON COLUMN users.lockout_end_at IS 'Account locked until this timestamp';
COMMENT ON COLUMN refresh_tokens.token IS 'Refresh token string (hashed)';
COMMENT ON COLUMN refresh_tokens.replaced_by_token_id IS 'Points to the new token if this was rotated';
