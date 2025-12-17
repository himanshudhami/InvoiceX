-- 051_seed_employee_users.sql
-- Description: Creates user accounts for all active employees with emails
-- Default password: Welcome@123 (BCrypt hashed with cost factor 11)
-- Note: Using chr(36) to build the hash to avoid DbUp variable substitution

-- Insert user accounts for all active employees who have emails and don't have a user account yet
INSERT INTO users (
    id,
    email,
    password_hash,
    display_name,
    role,
    company_id,
    employee_id,
    is_active,
    created_at,
    updated_at,
    created_by
)
SELECT
    gen_random_uuid(),
    e.email,
    -- BCrypt hash for "Welcome@123" with cost 11
    chr(36) || '2a' || chr(36) || '11' || chr(36) || 'Vtq6tierASBhLj.9KU7yHeDHo6TX2sfHGaXG8S4iwa6P3jAImqnZS',
    e.employee_name,
    'Employee',
    e.company_id,
    e.id,
    TRUE,
    NOW(),
    NOW(),
    'system'
FROM employees e
WHERE e.email IS NOT NULL
  AND e.email != ''
  AND (e.status = 'active' OR e.status IS NULL)
  AND NOT EXISTS (
      SELECT 1 FROM users u WHERE LOWER(u.email) = LOWER(e.email)
  );
