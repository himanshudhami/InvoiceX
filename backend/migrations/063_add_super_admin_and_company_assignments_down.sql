-- Rollback: Remove Super Admin flag and User-Company Assignments table

-- Drop trigger first
DROP TRIGGER IF EXISTS trigger_update_user_company_assignments_updated_at ON user_company_assignments;
DROP FUNCTION IF EXISTS update_user_company_assignments_updated_at();

-- Drop junction table
DROP TABLE IF EXISTS user_company_assignments;

-- Remove is_super_admin column from users
ALTER TABLE users DROP COLUMN IF EXISTS is_super_admin;
