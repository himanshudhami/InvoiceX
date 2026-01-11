-- Migration: 154_seed_posting_rules_for_all_companies.sql
-- Description: Seed posting rules for all existing companies that don't have them
-- Purpose: Fix the gap where create_default_posting_rules() wasn't called for existing companies

-- First, verify the function exists from migration 068b
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_proc
        WHERE proname = 'seed_default_posting_rules'
    ) THEN
        RAISE EXCEPTION 'Function seed_default_posting_rules does not exist. Run migration 068b first.';
    END IF;
END $$;

-- Execute seed_default_posting_rules for companies missing core invoice rules
DO $$
DECLARE
    v_company RECORD;
    v_total INTEGER := 0;
BEGIN
    FOR v_company IN
        SELECT id, name FROM companies
        WHERE NOT EXISTS (
            SELECT 1 FROM posting_rules pr
            WHERE pr.company_id = companies.id
            AND pr.rule_code = 'INV_DOM_INTRA_B2B'
        )
    LOOP
        PERFORM seed_default_posting_rules(v_company.id);
        v_total := v_total + 1;
        RAISE NOTICE 'Created rules for company: %', v_company.name;
    END LOOP;

    RAISE NOTICE 'Total companies processed: %', v_total;
END $$;

-- Also ensure vendor invoice rules from migration 122 exist for all companies
-- Re-run vendor invoice rules insertions

-- VINV_INTRA - Intra-State Purchase
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_INTRA', 'Vendor Invoice - Intra-State (CGST + SGST)',
    'vendor_invoice', 'on_finalize',
    '{"is_intra_state": true, "tds_applicable": false, "rcm_applicable": false}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number}", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1141", "amountField": "total_cgst", "description": "CGST Input"},
        {"side": "debit", "accountCode": "1142", "amountField": "total_sgst", "description": "SGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "total_amount", "description": "Trade Payables"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 100, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTRA');

-- VINV_INTER - Inter-State Purchase
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_INTER', 'Vendor Invoice - Inter-State (IGST)',
    'vendor_invoice', 'on_finalize',
    '{"is_intra_state": false, "tds_applicable": false, "rcm_applicable": false}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number}", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1143", "amountField": "total_igst", "description": "IGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "total_amount", "description": "Trade Payables"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 110, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTER');

-- VINV_INTRA_TDS - Intra-State with TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_INTRA_TDS', 'Vendor Invoice - Intra-State with TDS',
    'vendor_invoice', 'on_finalize',
    '{"is_intra_state": true, "tds_applicable": true, "rcm_applicable": false}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number} (TDS)", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1141", "amountField": "total_cgst", "description": "CGST Input"},
        {"side": "debit", "accountCode": "1142", "amountField": "total_sgst", "description": "SGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "net_payable", "description": "Trade Payables (Net)"},
        {"side": "credit", "accountCode": "2212", "amountField": "tds_amount", "description": "TDS Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 90, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTRA_TDS');

-- VINV_INTER_TDS - Inter-State with TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_INTER_TDS', 'Vendor Invoice - Inter-State with TDS',
    'vendor_invoice', 'on_finalize',
    '{"is_intra_state": false, "tds_applicable": true, "rcm_applicable": false}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number} (TDS)", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1143", "amountField": "total_igst", "description": "IGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "net_payable", "description": "Trade Payables (Net)"},
        {"side": "credit", "accountCode": "2212", "amountField": "tds_amount", "description": "TDS Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 95, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTER_TDS');

-- VINV_RCM_INTRA - RCM Intra-State
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_RCM_INTRA', 'Vendor Invoice - RCM Intra-State',
    'vendor_invoice', 'on_finalize',
    '{"rcm_applicable": true, "is_intra_state": true}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number} (RCM)", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1146", "amountField": "total_cgst", "description": "RCM CGST Claimable"},
        {"side": "debit", "accountCode": "1147", "amountField": "total_sgst", "description": "RCM SGST Claimable"},
        {"side": "credit", "accountCode": "2100", "amountField": "subtotal", "description": "Trade Payables"},
        {"side": "credit", "accountCode": "2256", "amountField": "total_cgst", "description": "RCM CGST Payable"},
        {"side": "credit", "accountCode": "2257", "amountField": "total_sgst", "description": "RCM SGST Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 80, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_RCM_INTRA');

-- VINV_RCM_INTER - RCM Inter-State
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_RCM_INTER', 'Vendor Invoice - RCM Inter-State',
    'vendor_invoice', 'on_finalize',
    '{"rcm_applicable": true, "is_intra_state": false}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number} (RCM)", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1148", "amountField": "total_igst", "description": "RCM IGST Claimable"},
        {"side": "credit", "accountCode": "2100", "amountField": "subtotal", "description": "Trade Payables"},
        {"side": "credit", "accountCode": "2258", "amountField": "total_igst", "description": "RCM IGST Payable"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 85, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_RCM_INTER');

-- VINV_DEFAULT - Fallback for unmatched vendor invoices
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_DEFAULT', 'Vendor Invoice - Default (Fallback)',
    'vendor_invoice', 'on_finalize',
    '{}'::jsonb,
    '{"descriptionTemplate": "Purchase Invoice {source_number}", "lines": [
        {"side": "debit", "accountCode": "5100", "amountField": "subtotal", "description": "Purchases"},
        {"side": "debit", "accountCode": "1143", "amountField": "total_igst", "description": "IGST Input"},
        {"side": "debit", "accountCode": "1141", "amountField": "total_cgst", "description": "CGST Input"},
        {"side": "debit", "accountCode": "1142", "amountField": "total_sgst", "description": "SGST Input"},
        {"side": "credit", "accountCode": "2100", "amountField": "total_amount", "description": "Trade Payables"}
    ]}'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (SELECT 1 FROM posting_rules pr WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_DEFAULT');

-- Log results
DO $$
DECLARE
    v_total_rules INTEGER;
    v_vendor_rules INTEGER;
    v_invoice_rules INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_total_rules FROM posting_rules WHERE is_active = true;
    SELECT COUNT(*) INTO v_vendor_rules FROM posting_rules WHERE source_type = 'vendor_invoice' AND is_active = true;
    SELECT COUNT(*) INTO v_invoice_rules FROM posting_rules WHERE source_type = 'invoice' AND is_active = true;

    RAISE NOTICE '=== Posting Rules Summary ===';
    RAISE NOTICE 'Total active rules: %', v_total_rules;
    RAISE NOTICE 'Vendor invoice rules: %', v_vendor_rules;
    RAISE NOTICE 'Invoice rules: %', v_invoice_rules;
END $$;
