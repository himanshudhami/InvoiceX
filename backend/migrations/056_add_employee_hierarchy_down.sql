-- 056_add_employee_hierarchy_down.sql
-- Rollback: Remove hierarchy fields from employees table

-- Drop triggers first
DROP TRIGGER IF EXISTS trg_check_employee_hierarchy ON employees;
DROP TRIGGER IF EXISTS trg_update_is_manager ON employees;
DROP TRIGGER IF EXISTS trg_employee_deletion_hierarchy ON employees;
DROP TRIGGER IF EXISTS trg_update_reporting_level ON employees;

-- Drop functions
DROP FUNCTION IF EXISTS check_employee_hierarchy_circular_reference();
DROP FUNCTION IF EXISTS update_is_manager_flag();
DROP FUNCTION IF EXISTS handle_employee_deletion_hierarchy();
DROP FUNCTION IF EXISTS calculate_reporting_level(UUID);
DROP FUNCTION IF EXISTS update_reporting_level();
DROP FUNCTION IF EXISTS cascade_update_reporting_levels(UUID);

-- Drop indexes
DROP INDEX IF EXISTS idx_employees_manager_id;
DROP INDEX IF EXISTS idx_employees_company_manager;
DROP INDEX IF EXISTS idx_employees_is_manager;

-- Drop columns
ALTER TABLE employees
DROP COLUMN IF EXISTS manager_id,
DROP COLUMN IF EXISTS reporting_level,
DROP COLUMN IF EXISTS is_manager;
