-- Rollback: Remove transaction_type column from employee_salary_transactions table

DROP INDEX IF EXISTS idx_salary_transactions_employee_month_year_type;
DROP INDEX IF EXISTS idx_salary_transactions_transaction_type;

ALTER TABLE employee_salary_transactions 
DROP COLUMN IF EXISTS transaction_type;



