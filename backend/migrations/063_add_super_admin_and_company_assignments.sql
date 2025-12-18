-- Migration: Add Super Admin flag and User-Company Assignments table
-- Purpose: Enable granular multi-tenancy with Super Admin (all companies) and Company Admin (assigned companies only)

-- Add is_super_admin flag to users table
ALTER TABLE users ADD COLUMN IF NOT EXISTS is_super_admin BOOLEAN DEFAULT FALSE;

-- Create user_company_assignments junction table for many-to-many relationship
CREATE TABLE IF NOT EXISTS user_company_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    role VARCHAR(50) NOT NULL DEFAULT 'Admin',   -- Role within this company (Admin, HR, etc.)
    is_primary BOOLEAN DEFAULT FALSE,             -- Primary company for UI default
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    created_by VARCHAR(255),
    updated_by VARCHAR(255),
    UNIQUE(user_id, company_id)
);

-- Create indexes for efficient lookups
CREATE INDEX IF NOT EXISTS idx_user_company_user ON user_company_assignments(user_id);
CREATE INDEX IF NOT EXISTS idx_user_company_company ON user_company_assignments(company_id);
CREATE INDEX IF NOT EXISTS idx_user_company_role ON user_company_assignments(role);

-- Create updated_at trigger for user_company_assignments
CREATE OR REPLACE FUNCTION update_user_company_assignments_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_user_company_assignments_updated_at ON user_company_assignments;
CREATE TRIGGER trigger_update_user_company_assignments_updated_at
    BEFORE UPDATE ON user_company_assignments
    FOR EACH ROW
    EXECUTE FUNCTION update_user_company_assignments_updated_at();

-- Migration: Mark existing admin@company.com as Super Admin
UPDATE users SET is_super_admin = TRUE WHERE email = 'admin@company.com';

-- Comment on columns for documentation
COMMENT ON COLUMN users.is_super_admin IS 'Super Admin has access to ALL companies. Takes precedence over company assignments.';
COMMENT ON TABLE user_company_assignments IS 'Junction table for assigning specific companies to Admin/HR users (Company Admins)';
COMMENT ON COLUMN user_company_assignments.role IS 'The users role within this specific company (Admin, HR, etc.)';
COMMENT ON COLUMN user_company_assignments.is_primary IS 'Marks the primary company for UI default selection';
