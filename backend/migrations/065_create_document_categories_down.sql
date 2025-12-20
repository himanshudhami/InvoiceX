-- Migration: 065_create_document_categories_down
-- Description: Rollback document categories migration
-- Date: 2025-12-20

-- Remove columns from employee_documents
ALTER TABLE employee_documents
    DROP COLUMN IF EXISTS file_storage_id,
    DROP COLUMN IF EXISTS category_id;

-- Drop the seeding function
DROP FUNCTION IF EXISTS seed_default_document_categories(UUID);

-- Drop the document categories table
DROP TABLE IF EXISTS document_categories;
