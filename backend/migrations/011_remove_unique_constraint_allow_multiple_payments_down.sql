-- Rollback: Restore unique constraint (if needed for backward compatibility)

DROP INDEX IF EXISTS idx_employee_salary_transactions_employee_month_year;

-- Recreate unique index (only if you want to enforce single payment per month again)
-- Note: This will fail if there are existing duplicate records
-- CREATE UNIQUE INDEX IF NOT EXISTS idx_employee_salary_unique 
-- ON employee_salary_transactions(employee_id, salary_month, salary_year);





