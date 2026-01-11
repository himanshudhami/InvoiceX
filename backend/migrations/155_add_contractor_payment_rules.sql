-- Migration: 155_add_contractor_payment_rules.sql
-- Description: Add posting rules for contractor payments and fix any trigger event issues

-- ============================================================================
-- CONTRACTOR PAYMENT POSTING RULES
-- ============================================================================

-- CONTRACTOR_PMT_WITH_TDS - Contractor Payment with TDS Deduction
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'CONTRACTOR_PMT_TDS', 'Contractor Payment with TDS',
    'contractor_payment', 'on_create',
    '{"tds_applicable": true}'::jsonb,
    '{"descriptionTemplate": "Contractor Payment - {party_name}", "lines": [
        {"side": "debit", "accountCode": "5310", "amountField": "gross_amount", "description": "Professional Fees"},
        {"side": "debit", "accountCode": "1141", "amountField": "cgst_amount", "description": "CGST Input"},
        {"side": "debit", "accountCode": "1142", "amountField": "sgst_amount", "description": "SGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "net_payable", "description": "Contractor Payable"},
        {"side": "credit", "accountCode": "2212", "amountField": "tds_amount", "description": "TDS Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 100, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'CONTRACTOR_PMT_TDS');

-- CONTRACTOR_PMT_NO_TDS - Contractor Payment without TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'CONTRACTOR_PMT_NO_TDS', 'Contractor Payment without TDS',
    'contractor_payment', 'on_create',
    '{"tds_applicable": false}'::jsonb,
    '{"descriptionTemplate": "Contractor Payment - {party_name}", "lines": [
        {"side": "debit", "accountCode": "5310", "amountField": "gross_amount", "description": "Professional Fees"},
        {"side": "debit", "accountCode": "1141", "amountField": "cgst_amount", "description": "CGST Input"},
        {"side": "debit", "accountCode": "1142", "amountField": "sgst_amount", "description": "SGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "gross_amount", "description": "Contractor Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 110, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'CONTRACTOR_PMT_NO_TDS');

-- CONTRACTOR_PMT_DEFAULT - Fallback for contractor payments
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'CONTRACTOR_PMT_DEFAULT', 'Contractor Payment - Default',
    'contractor_payment', 'on_create',
    '{}'::jsonb,
    '{"descriptionTemplate": "Contractor Payment - {party_name}", "lines": [
        {"side": "debit", "accountCode": "5310", "amountField": "gross_amount", "description": "Professional Fees"},
        {"side": "credit", "accountCode": "2100", "amountField": "net_payable", "description": "Contractor Payable"},
        {"side": "credit", "accountCode": "2212", "amountField": "tds_amount", "description": "TDS Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'CONTRACTOR_PMT_DEFAULT');

-- ============================================================================
-- FIX TRIGGER EVENTS
-- ============================================================================

-- Ensure all invoice rules use on_finalize
UPDATE posting_rules
SET trigger_event = 'on_finalize', updated_at = NOW()
WHERE source_type = 'invoice'
AND trigger_event NOT IN ('on_finalize');

-- Ensure all vendor_invoice rules use on_finalize
UPDATE posting_rules
SET trigger_event = 'on_finalize', updated_at = NOW()
WHERE source_type = 'vendor_invoice'
AND trigger_event NOT IN ('on_finalize');

-- Ensure payment rules use on_create
UPDATE posting_rules
SET trigger_event = 'on_create', updated_at = NOW()
WHERE source_type IN ('payment', 'vendor_payment')
AND trigger_event NOT IN ('on_create', 'on_finalize');

-- ============================================================================
-- ADD PROFESSIONAL FEES ACCOUNT IF NOT EXISTS
-- ============================================================================

INSERT INTO chart_of_accounts (
    id, company_id, account_code, account_name, account_type, account_subtype,
    parent_account_id, is_control_account, is_system_account,
    is_active, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, '5310', 'Professional Fees', 'expense', 'operating_expense',
    (SELECT id FROM chart_of_accounts WHERE company_id = c.id AND account_code = '5000' LIMIT 1),
    false, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '5310'
)
AND EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '5000'
);

-- Log results
DO $$
DECLARE
    v_contractor_rules INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_contractor_rules
    FROM posting_rules
    WHERE source_type = 'contractor_payment' AND is_active = true;

    RAISE NOTICE 'Contractor payment rules created: %', v_contractor_rules;
END $$;
