-- Rollback: Remove 'resigned' status from employees table

-- Remove resignation tracking fields
ALTER TABLE employees
DROP COLUMN IF EXISTS resigned_at,
DROP COLUMN IF EXISTS resignation_reason;

-- Restore original status constraint (without 'resigned')
ALTER TABLE employees
DROP CONSTRAINT IF EXISTS employees_status_check;

ALTER TABLE employees
ADD CONSTRAINT employees_status_check
CHECK (status IN ('active', 'inactive', 'terminated', 'permanent'));
