-- Migration: 162_add_loan_payment_posting_rules.sql
-- Description: Add posting rules for loan EMI payments, prepayments, and disbursements

-- Add description column to posting_rules if it doesn't exist
ALTER TABLE posting_rules ADD COLUMN IF NOT EXISTS description TEXT;

-- Function to create loan posting rules for a company
CREATE OR REPLACE FUNCTION create_loan_posting_rules(p_company_id UUID)
RETURNS INTEGER AS $$
DECLARE
    v_rule_count INTEGER := 0;
    v_fy VARCHAR(10) := (SELECT get_financial_year(CURRENT_DATE));
BEGIN
    -- =============================================================
    -- LOAN EMI PAYMENT RULE
    -- Debits: Loan Principal (liability reduction) + Interest Expense
    -- Credits: Bank Account
    -- =============================================================
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, description, source_type, trigger_event,
        conditions_json, posting_template, financial_year, priority, is_active
    ) VALUES (
        p_company_id,
        'LOAN_EMI_PAYMENT',
        'Loan EMI Payment',
        'Auto-post journal entry when EMI payment is recorded. Debits loan principal and interest expense, credits bank account.',
        'loan_payment',
        'on_create',
        '{}'::JSONB,
        '{
            "narration_template": "EMI Payment - {loan_name} - EMI #{emi_number}",
            "lines": [
                {
                    "account_code_field": "loan_account_code",
                    "debit_field": "principal_amount",
                    "description_template": "Principal repayment - {loan_name}"
                },
                {
                    "account_code_field": "interest_account_code",
                    "debit_field": "interest_amount",
                    "description_template": "Interest on loan - {loan_name}"
                },
                {
                    "account_code_field": "bank_account_code",
                    "credit_field": "total_amount",
                    "description_template": "EMI payment via {payment_method}"
                }
            ]
        }'::JSONB,
        v_fy,
        100,
        true
    )
    ON CONFLICT (company_id, rule_code, financial_year) DO UPDATE SET
        posting_template = EXCLUDED.posting_template,
        description = EXCLUDED.description,
        updated_at = NOW();
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- LOAN PREPAYMENT RULE
    -- Debits: Loan Principal (liability reduction)
    -- Credits: Bank Account
    -- =============================================================
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, description, source_type, trigger_event,
        conditions_json, posting_template, financial_year, priority, is_active
    ) VALUES (
        p_company_id,
        'LOAN_PREPAYMENT',
        'Loan Prepayment',
        'Auto-post journal entry when loan prepayment is made. Debits loan principal, credits bank account.',
        'loan_prepayment',
        'on_create',
        '{}'::JSONB,
        '{
            "narration_template": "Prepayment - {loan_name}",
            "lines": [
                {
                    "account_code_field": "loan_account_code",
                    "debit_field": "amount",
                    "description_template": "Prepayment towards principal - {loan_name}"
                },
                {
                    "account_code_field": "bank_account_code",
                    "credit_field": "amount",
                    "description_template": "Prepayment via {payment_method}"
                }
            ]
        }'::JSONB,
        v_fy,
        100,
        true
    )
    ON CONFLICT (company_id, rule_code, financial_year) DO UPDATE SET
        posting_template = EXCLUDED.posting_template,
        description = EXCLUDED.description,
        updated_at = NOW();
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- LOAN DISBURSEMENT RULE
    -- Debits: Bank Account (money received)
    -- Credits: Loan Liability
    -- =============================================================
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, description, source_type, trigger_event,
        conditions_json, posting_template, financial_year, priority, is_active
    ) VALUES (
        p_company_id,
        'LOAN_DISBURSEMENT',
        'Loan Disbursement',
        'Auto-post journal entry when loan is disbursed. Debits bank account, credits loan liability.',
        'loan_disbursement',
        'on_create',
        '{}'::JSONB,
        '{
            "narration_template": "Loan Disbursement - {loan_name}",
            "lines": [
                {
                    "account_code_field": "bank_account_code",
                    "debit_field": "amount",
                    "description_template": "Loan disbursement received - {loan_name}"
                },
                {
                    "account_code_field": "loan_account_code",
                    "credit_field": "amount",
                    "description_template": "Loan liability - {lender_name}"
                }
            ]
        }'::JSONB,
        v_fy,
        100,
        true
    )
    ON CONFLICT (company_id, rule_code, financial_year) DO UPDATE SET
        posting_template = EXCLUDED.posting_template,
        description = EXCLUDED.description,
        updated_at = NOW();
    v_rule_count := v_rule_count + 1;

    RETURN v_rule_count;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION create_loan_posting_rules IS 'Creates loan posting rules (EMI payment, prepayment, disbursement) for a company. Returns count of rules created.';

-- Create loan posting rules for all existing companies
DO $$
DECLARE
    v_company RECORD;
    v_count INTEGER;
BEGIN
    FOR v_company IN SELECT id, name FROM companies LOOP
        SELECT create_loan_posting_rules(v_company.id) INTO v_count;
        RAISE NOTICE 'Created % loan posting rules for company: %', v_count, v_company.name;
    END LOOP;
END $$;
