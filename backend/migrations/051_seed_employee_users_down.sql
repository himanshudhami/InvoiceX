-- 051_seed_employee_users_down.sql
-- Description: Removes auto-generated employee user accounts

-- Delete refresh tokens for employee users created by system
DELETE FROM refresh_tokens WHERE user_id IN (
    SELECT id FROM users WHERE role = 'Employee' AND created_by = 'system'
);

-- Delete employee users created by system
DELETE FROM users WHERE role = 'Employee' AND created_by = 'system';
