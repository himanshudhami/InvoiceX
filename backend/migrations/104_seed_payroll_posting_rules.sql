-- ============================================================================
-- Migration 104: Seed Payroll Posting Rules
-- Description: Add posting rules for payroll journal entries
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Add payroll posting rules for each company
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    company_rec RECORD;
    current_fy VARCHAR(10);
BEGIN
    -- Determine current financial year
    IF EXTRACT(MONTH FROM CURRENT_DATE) >= 4 THEN
        current_fy := EXTRACT(YEAR FROM CURRENT_DATE)::TEXT || '-' ||
                      SUBSTRING((EXTRACT(YEAR FROM CURRENT_DATE) + 1)::TEXT FROM 3 FOR 2);
    ELSE
        current_fy := (EXTRACT(YEAR FROM CURRENT_DATE) - 1)::TEXT || '-' ||
                      SUBSTRING(EXTRACT(YEAR FROM CURRENT_DATE)::TEXT FROM 3 FOR 2);
    END IF;

    FOR company_rec IN SELECT id FROM companies LOOP

        -- =====================================================================
        -- Rule 1: PAYROLL_ACCRUAL - Salary Expense Recognition (on approval)
        -- =====================================================================
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at, updated_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'PAYROLL_ACCRUAL',
            'Payroll Salary Accrual',
            'payroll_run',
            'on_approval',
            '{"status": ["approved", "paid"]}',
            '{
                "description_template": "Salary accrual for {month_name} {payroll_year}",
                "narration_template": "Being salary and statutory contributions accrued for the month of {month_name} {payroll_year} as per payroll run {payroll_run_number}",
                "lines": [
                    {
                        "account_code": "5210",
                        "debit_field": "total_gross_salary",
                        "description": "Salaries and Wages Expense"
                    },
                    {
                        "account_code": "5220",
                        "debit_field": "total_employer_pf",
                        "description": "Employer PF Contribution Expense"
                    },
                    {
                        "account_code": "5230",
                        "debit_field": "total_employer_esi",
                        "description": "Employer ESI Contribution Expense"
                    },
                    {
                        "account_code": "5250",
                        "debit_field": "total_gratuity",
                        "description": "Gratuity Expense (Monthly Provision)"
                    },
                    {
                        "account_code": "2110",
                        "credit_field": "total_net_salary",
                        "description": "Net Salary Payable to Employees"
                    },
                    {
                        "account_code": "2212",
                        "credit_field": "total_tds",
                        "description": "TDS Payable on Salary (Section 192)"
                    },
                    {
                        "account_code": "2221",
                        "credit_field": "total_employee_pf",
                        "description": "Employee PF Contribution Payable"
                    },
                    {
                        "account_code": "2222",
                        "credit_field": "total_employer_pf",
                        "description": "Employer PF Contribution Payable"
                    },
                    {
                        "account_code": "2231",
                        "credit_field": "total_employee_esi",
                        "description": "Employee ESI Contribution Payable"
                    },
                    {
                        "account_code": "2232",
                        "credit_field": "total_employer_esi",
                        "description": "Employer ESI Contribution Payable"
                    },
                    {
                        "account_code": "2240",
                        "credit_field": "total_professional_tax",
                        "description": "Professional Tax Payable"
                    },
                    {
                        "account_code": "2250",
                        "credit_field": "total_gratuity",
                        "description": "Gratuity Payable"
                    }
                ]
            }',
            current_fy,
            10,
            true,
            true,
            NOW(),
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- =====================================================================
        -- Rule 2: PAYROLL_DISBURSEMENT - Salary Payment (on payment)
        -- =====================================================================
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at, updated_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'PAYROLL_DISBURSEMENT',
            'Payroll Salary Disbursement',
            'payroll_run',
            'on_payment',
            '{"status": "paid"}',
            '{
                "description_template": "Salary payment for {month_name} {payroll_year}",
                "narration_template": "Being net salary paid to employees for {month_name} {payroll_year} via {payment_mode}. Reference: {payment_reference}",
                "lines": [
                    {
                        "account_code": "2110",
                        "debit_field": "total_net_salary",
                        "description": "Clear Net Salary Payable"
                    },
                    {
                        "account_code_field": "bank_account_code",
                        "account_code_fallback": "1112",
                        "credit_field": "total_net_salary",
                        "description": "Bank Payment - Salary Transfer",
                        "subledger_type": "bank",
                        "subledger_id_field": "bank_account_id"
                    }
                ]
            }',
            current_fy,
            20,
            true,
            true,
            NOW(),
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- =====================================================================
        -- Rule 3: STATUTORY_REMITTANCE - Statutory Payment (on challan)
        -- =====================================================================
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at, updated_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'STATUTORY_REMITTANCE',
            'Statutory Payment Remittance',
            'statutory_payment',
            'on_payment',
            '{"status": "paid"}',
            '{
                "description_template": "{payment_type_name} for {period_month}/{period_year}",
                "narration_template": "Being {payment_type_name} remitted for the period {period_month}/{period_year}. Challan: {reference_number}. Bank Ref: {bank_reference}",
                "lines": [
                    {
                        "account_code_field": "payable_account_code",
                        "debit_field": "total_amount",
                        "description_template": "Clear {payment_type_name} Payable"
                    },
                    {
                        "account_code_field": "bank_account_code",
                        "account_code_fallback": "1112",
                        "credit_field": "total_amount",
                        "description_template": "Bank Payment - {payment_type_name}",
                        "subledger_type": "bank",
                        "subledger_id_field": "bank_account_id"
                    }
                ]
            }',
            current_fy,
            30,
            true,
            true,
            NOW(),
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- =====================================================================
        -- Rule 4: STATUTORY_TDS_REMITTANCE - TDS Specific (with interest/penalty)
        -- =====================================================================
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at, updated_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'STATUTORY_TDS_REMITTANCE',
            'TDS Payment with Interest/Penalty',
            'statutory_payment',
            'on_payment',
            '{"status": "paid", "payment_type": ["TDS_192", "TDS_194C", "TDS_194J"]}',
            '{
                "description_template": "{payment_type_name} for {period_month}/{period_year}",
                "narration_template": "Being TDS remitted for {period_month}/{period_year}. BSR: {bsr_code}. CIN: {receipt_number}",
                "lines": [
                    {
                        "account_code_field": "payable_account_code",
                        "debit_field": "principal_amount",
                        "description": "Clear TDS Payable"
                    },
                    {
                        "account_code": "5620",
                        "debit_field": "interest_amount",
                        "description": "Interest on Delayed TDS Payment",
                        "skip_if_zero": true
                    },
                    {
                        "account_code": "5100",
                        "debit_field": "penalty_amount",
                        "description": "Penalty on TDS Default",
                        "skip_if_zero": true
                    },
                    {
                        "account_code_field": "bank_account_code",
                        "account_code_fallback": "1112",
                        "credit_field": "total_amount",
                        "description": "Bank Payment - TDS",
                        "subledger_type": "bank",
                        "subledger_id_field": "bank_account_id"
                    }
                ]
            }',
            current_fy,
            25,  -- Higher priority than generic statutory
            true,
            true,
            NOW(),
            NOW()
        ) ON CONFLICT DO NOTHING;

        -- =====================================================================
        -- Rule 5: STATUTORY_PF_REMITTANCE - PF Specific (with damages)
        -- =====================================================================
        INSERT INTO posting_rules (
            id, company_id, rule_code, rule_name, source_type, trigger_event,
            conditions_json, posting_template, financial_year,
            priority, is_active, is_system_rule, created_at, updated_at
        ) VALUES (
            gen_random_uuid(),
            company_rec.id,
            'STATUTORY_PF_REMITTANCE',
            'PF Payment with Damages',
            'statutory_payment',
            'on_payment',
            '{"status": "paid", "payment_type": "PF"}',
            '{
                "description_template": "Provident Fund remittance for {period_month}/{period_year}",
                "narration_template": "Being PF contribution remitted to EPFO for {period_month}/{period_year}. TRRN: {trrn}",
                "lines": [
                    {
                        "account_code": "2221",
                        "debit_field": "employee_pf_amount",
                        "description": "Clear Employee PF Payable"
                    },
                    {
                        "account_code": "2222",
                        "debit_field": "employer_pf_amount",
                        "description": "Clear Employer PF Payable"
                    },
                    {
                        "account_code": "5620",
                        "debit_field": "interest_amount",
                        "description": "PF Late Payment Damages",
                        "skip_if_zero": true
                    },
                    {
                        "account_code_field": "bank_account_code",
                        "account_code_fallback": "1112",
                        "credit_field": "total_amount",
                        "description": "Bank Payment - PF",
                        "subledger_type": "bank",
                        "subledger_id_field": "bank_account_id"
                    }
                ]
            }',
            current_fy,
            25,
            true,
            true,
            NOW(),
            NOW()
        ) ON CONFLICT DO NOTHING;

    END LOOP;
END $$;
