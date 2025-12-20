-- Migration: 066_create_expense_tables_down
-- Description: Rollback expense tables migration
-- Date: 2025-12-20

-- Drop functions
DROP FUNCTION IF EXISTS seed_default_expense_categories(UUID);
DROP FUNCTION IF EXISTS generate_expense_claim_number(UUID);

-- Drop tables in reverse order (respecting foreign keys)
DROP TABLE IF EXISTS expense_attachments;
DROP TABLE IF EXISTS expense_claims;
DROP TABLE IF EXISTS expense_categories;
