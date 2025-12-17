-- 050_seed_admin_user.sql
-- Description: Seeds initial admin and HR users for authentication
-- Password for both: Admin@123 (BCrypt hashed with cost factor 11)
-- Note: Using chr(36) to build the hash to avoid DbUp variable substitution

-- Insert admin user if not exists
INSERT INTO users (
    id,
    email,
    password_hash,
    display_name,
    role,
    company_id,
    is_active,
    created_at,
    updated_at,
    created_by
)
SELECT
    gen_random_uuid(),
    'admin@company.com',
    chr(36) || '2a' || chr(36) || '11' || chr(36) || 'aGfRAO4nx8UlUzogzP49tO1lTwTzRJyvmrHJiia7a1S3i5.GHFsyO',
    'System Administrator',
    'Admin',
    (SELECT id FROM companies ORDER BY created_at LIMIT 1),
    TRUE,
    NOW(),
    NOW(),
    'system'
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE LOWER(email) = 'admin@company.com'
)
AND EXISTS (
    SELECT 1 FROM companies
);

-- Insert HR user if not exists
INSERT INTO users (
    id,
    email,
    password_hash,
    display_name,
    role,
    company_id,
    is_active,
    created_at,
    updated_at,
    created_by
)
SELECT
    gen_random_uuid(),
    'hr@company.com',
    chr(36) || '2a' || chr(36) || '11' || chr(36) || 'aGfRAO4nx8UlUzogzP49tO1lTwTzRJyvmrHJiia7a1S3i5.GHFsyO',
    'HR Manager',
    'HR',
    (SELECT id FROM companies ORDER BY created_at LIMIT 1),
    TRUE,
    NOW(),
    NOW(),
    'system'
WHERE NOT EXISTS (
    SELECT 1 FROM users WHERE LOWER(email) = 'hr@company.com'
)
AND EXISTS (
    SELECT 1 FROM companies
);
