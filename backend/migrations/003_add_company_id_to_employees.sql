-- 003_add_company_id_to_employees.sql
-- Adds company_id foreign key to employees table and migrates existing data

-- Add company_id column
ALTER TABLE employees ADD COLUMN IF NOT EXISTS company_id UUID REFERENCES companies(id) ON DELETE SET NULL;

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_employees_company_id ON employees(company_id);

-- Migrate existing data by matching company names to company IDs
-- This uses fuzzy matching to handle cases where company names don't match exactly
UPDATE employees e
SET company_id = (
    SELECT c.id 
    FROM companies c 
    WHERE 
        -- Exact match (case insensitive, trimmed)
        LOWER(TRIM(e.company)) = LOWER(TRIM(c.name))
        -- Match first word of company name
        OR LOWER(TRIM(e.company)) = LOWER(TRIM(SPLIT_PART(c.name, ' ', 1)))
        -- Company name starts with employee company
        OR LOWER(TRIM(c.name)) LIKE LOWER(TRIM(e.company)) || '%'
        -- Employee company starts with company name
        OR LOWER(TRIM(e.company)) LIKE LOWER(TRIM(c.name)) || '%'
        -- Handle "Ascener" -> "Ascent Factory Pvt Ltd" type matches
        OR LOWER(TRIM(e.company)) LIKE LOWER(TRIM(SPLIT_PART(c.name, ' ', 1))) || '%'
    ORDER BY 
        -- Prioritize exact matches
        CASE WHEN LOWER(TRIM(e.company)) = LOWER(TRIM(c.name)) THEN 1 ELSE 2 END
    LIMIT 1
)
WHERE e.company IS NOT NULL AND e.company_id IS NULL;

-- Add comment to document the migration
COMMENT ON COLUMN employees.company_id IS 'Foreign key to companies table. Migrated from company (string) field.';


