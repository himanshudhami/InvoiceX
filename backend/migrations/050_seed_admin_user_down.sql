-- Down Migration: Remove seeded admin users

DELETE FROM refresh_tokens WHERE user_id IN (
    SELECT id FROM users WHERE email IN ('admin@company.com', 'hr@company.com')
);

DELETE FROM users WHERE email IN ('admin@company.com', 'hr@company.com');
