-- Add transaction_type column to employee_salary_transactions table
-- This allows distinguishing between salary, consulting, bonus, reimbursement, and gift payments

ALTER TABLE employee_salary_transactions 
ADD COLUMN transaction_type VARCHAR(20) DEFAULT 'salary'
CHECK (transaction_type IN ('salary', 'consulting', 'bonus', 'reimbursement', 'gift'));

-- Update existing records to have 'salary' type (already default, but explicit update for clarity)
UPDATE employee_salary_transactions 
SET transaction_type = 'salary' 
WHERE transaction_type IS NULL OR transaction_type = '';

-- Create index for efficient queries by transaction type
CREATE INDEX IF NOT EXISTS idx_salary_transactions_transaction_type 
ON employee_salary_transactions(transaction_type);

-- Create composite index for efficient queries by employee, month, year, and type
CREATE INDEX IF NOT EXISTS idx_salary_transactions_employee_month_year_type 
ON employee_salary_transactions(employee_id, salary_month, salary_year, transaction_type);



