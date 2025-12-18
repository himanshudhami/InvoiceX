-- Remove unique constraint to allow multiple payments per month for all transaction types
-- This enables consultants, bonuses, reimbursements, and gifts to have multiple payments per month

-- Drop the unique index that prevents multiple payments per month
DROP INDEX IF EXISTS idx_employee_salary_unique;

-- Create a non-unique index for performance (allows duplicates)
CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_employee_month_year 
ON employee_salary_transactions(employee_id, salary_month, salary_year);





