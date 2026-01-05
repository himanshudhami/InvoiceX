-- Rollback migration: Remove bank transaction reconciliation tracking from expense tables

-- Drop indexes first
DROP INDEX IF EXISTS idx_payroll_txn_bank_txn;
DROP INDEX IF EXISTS idx_contractor_bank_txn;
DROP INDEX IF EXISTS idx_subscription_bank_txn;
DROP INDEX IF EXISTS idx_loan_txn_bank_txn;
DROP INDEX IF EXISTS idx_asset_maint_bank_txn;

-- Restore the employee_salary_transactions view without reconciliation columns
CREATE OR REPLACE VIEW employee_salary_transactions AS
SELECT
    pt.id,
    pt.employee_id,
    pr.company_id,
    pt.payroll_month AS salary_month,
    pt.payroll_year AS salary_year,
    pt.basic_earned AS basic_salary,
    pt.hra_earned AS hra,
    pt.conveyance_earned AS conveyance,
    pt.medical_earned AS medical_allowance,
    pt.special_allowance_earned AS special_allowance,
    pt.lta_paid AS lta,
    pt.other_allowances_earned + pt.arrears + pt.reimbursements + pt.incentives + pt.other_earnings AS other_allowances,
    pt.gross_earnings AS gross_salary,
    pt.pf_employee,
    pt.pf_employer,
    pt.professional_tax AS pt,
    pt.tds_deducted AS income_tax,
    pt.loan_recovery + pt.advance_recovery + pt.other_deductions AS other_deductions,
    pt.net_payable AS net_salary,
    pt.payment_date,
    pt.payment_method,
    pt.payment_reference,
    pt.status,
    pt.remarks,
    'INR' AS currency,
    CASE
        WHEN pt.payroll_type = 'contractor' THEN 'consulting'
        ELSE 'salary'
    END AS transaction_type,
    pt.created_at,
    pt.updated_at,
    NULL::VARCHAR(255) AS created_by,
    NULL::VARCHAR(255) AS updated_by
FROM payroll_transactions pt
INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id;

-- Remove columns from payroll_transactions
ALTER TABLE payroll_transactions
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

-- Remove columns from contractor_payments
ALTER TABLE contractor_payments
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

-- Remove columns from subscriptions
ALTER TABLE subscriptions
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

-- Remove columns from loan_transactions
ALTER TABLE loan_transactions
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;

-- Remove columns from asset_maintenance
ALTER TABLE asset_maintenance
DROP COLUMN IF EXISTS bank_transaction_id,
DROP COLUMN IF EXISTS reconciled_at,
DROP COLUMN IF EXISTS reconciled_by;
