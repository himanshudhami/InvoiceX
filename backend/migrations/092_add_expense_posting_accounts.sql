-- Migration: 092_add_expense_posting_accounts.sql
-- Description: Add missing chart of accounts entries required for expense claim posting rules
-- These accounts are referenced in posting rules from migration 091

-- Add General Expenses account (5100) if not exists
INSERT INTO chart_of_accounts (
    id, company_id, account_code, account_name, account_type,
    parent_account_id, depth_level, normal_balance, is_active,
    description, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    '5100',
    'General Expenses',
    'expense',
    (SELECT id FROM chart_of_accounts WHERE company_id = c.id AND account_code = '5000'),
    2,
    'debit',
    true,
    'General business expenses and employee reimbursements',
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts
    WHERE company_id = c.id AND account_code = '5100'
)
AND EXISTS (
    SELECT 1 FROM chart_of_accounts
    WHERE company_id = c.id AND account_code = '5000'
);

-- Add Employee Reimbursements Payable account (2102) if not exists
INSERT INTO chart_of_accounts (
    id, company_id, account_code, account_name, account_type,
    parent_account_id, depth_level, normal_balance, is_active,
    description, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    '2102',
    'Employee Reimbursements Payable',
    'liability',
    (SELECT id FROM chart_of_accounts WHERE company_id = c.id AND account_code = '2100'),
    2,
    'credit',
    true,
    'Pending expense reimbursements payable to employees',
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts
    WHERE company_id = c.id AND account_code = '2102'
)
AND EXISTS (
    SELECT 1 FROM chart_of_accounts
    WHERE company_id = c.id AND account_code = '2100'
);
