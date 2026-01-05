-- Migration: Add bank transaction reconciliation tracking to expense tables
-- This allows tracking which bank transactions are linked to which outgoing payments

-- Add reconciliation columns to payroll_transactions (the underlying table for employee_salary_transactions view)
ALTER TABLE payroll_transactions
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Add reconciliation columns to contractor_payments
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Add reconciliation columns to subscriptions
ALTER TABLE subscriptions
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Add reconciliation columns to loan_transactions
ALTER TABLE loan_transactions
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Add reconciliation columns to asset_maintenance
ALTER TABLE asset_maintenance
ADD COLUMN IF NOT EXISTS bank_transaction_id UUID REFERENCES bank_transactions(id),
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP,
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Create indexes for faster lookups
CREATE INDEX IF NOT EXISTS idx_payroll_txn_bank_txn ON payroll_transactions(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_contractor_bank_txn ON contractor_payments(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_subscription_bank_txn ON subscriptions(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_loan_txn_bank_txn ON loan_transactions(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_asset_maint_bank_txn ON asset_maintenance(bank_transaction_id) WHERE bank_transaction_id IS NOT NULL;

-- Update the employee_salary_transactions view to include reconciliation columns
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
    NULL::VARCHAR(255) AS updated_by,
    -- New reconciliation columns
    pt.bank_transaction_id,
    pt.reconciled_at,
    pt.reconciled_by
FROM payroll_transactions pt
INNER JOIN payroll_runs pr ON pt.payroll_run_id = pr.id;

-- Comments for documentation
COMMENT ON COLUMN payroll_transactions.bank_transaction_id IS 'Reference to the bank transaction this salary payment is reconciled with';
COMMENT ON COLUMN payroll_transactions.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN payroll_transactions.reconciled_by IS 'User who performed the reconciliation';

COMMENT ON COLUMN contractor_payments.bank_transaction_id IS 'Reference to the bank transaction this contractor payment is reconciled with';
COMMENT ON COLUMN contractor_payments.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN contractor_payments.reconciled_by IS 'User who performed the reconciliation';

COMMENT ON COLUMN subscriptions.bank_transaction_id IS 'Reference to the bank transaction this subscription payment is reconciled with';
COMMENT ON COLUMN subscriptions.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN subscriptions.reconciled_by IS 'User who performed the reconciliation';

COMMENT ON COLUMN loan_transactions.bank_transaction_id IS 'Reference to the bank transaction this loan payment is reconciled with';
COMMENT ON COLUMN loan_transactions.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN loan_transactions.reconciled_by IS 'User who performed the reconciliation';

COMMENT ON COLUMN asset_maintenance.bank_transaction_id IS 'Reference to the bank transaction this maintenance payment is reconciled with';
COMMENT ON COLUMN asset_maintenance.reconciled_at IS 'Timestamp when the reconciliation was done';
COMMENT ON COLUMN asset_maintenance.reconciled_by IS 'User who performed the reconciliation';
