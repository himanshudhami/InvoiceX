-- 009_add_company_id_to_salary_transactions.sql
-- Adds company_id column to employee_salary_transactions table for direct filtering

ALTER TABLE employee_salary_transactions
    ADD COLUMN IF NOT EXISTS company_id UUID REFERENCES companies(id) ON DELETE SET NULL;

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_company_id ON employee_salary_transactions(company_id);

-- Backfill existing records with company_id from employees table
UPDATE employee_salary_transactions st
SET company_id = e.company_id
FROM employees e
WHERE st.employee_id = e.id
  AND st.company_id IS NULL
  AND e.company_id IS NOT NULL;




