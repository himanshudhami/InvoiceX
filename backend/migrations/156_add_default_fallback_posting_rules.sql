-- Migration: 156_add_default_fallback_posting_rules.sql
-- Description: Add default fallback posting rules with no conditions for all source types.
-- These rules ensure transactions are NEVER left unposted due to missing rule conditions.
-- Priority 999 ensures specific rules are tried first.

-- ============================================================================
-- DEFAULT INVOICE RULE (Fallback for any invoice that doesn't match specific rules)
-- ============================================================================
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'INV_DEFAULT', 'Invoice - Default (Fallback)',
    'invoice', 'on_finalize',
    NULL, -- No conditions - matches everything
    '{
        "descriptionTemplate": "Sales Invoice - {source_number}",
        "lines": [
            {"side": "debit", "accountCode": "1300", "amountField": "total_amount", "description": "Accounts Receivable"},
            {"side": "credit", "accountCode": "4000", "amountField": "subtotal", "description": "Sales Revenue"},
            {"side": "credit", "accountCode": "2251", "amountField": "total_cgst", "description": "CGST Output"},
            {"side": "credit", "accountCode": "2252", "amountField": "total_sgst", "description": "SGST Output"},
            {"side": "credit", "accountCode": "2253", "amountField": "total_igst", "description": "IGST Output"}
        ]
    }'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'INV_DEFAULT'
);

-- ============================================================================
-- DEFAULT VENDOR INVOICE RULE (Fallback)
-- ============================================================================
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VINV_DEFAULT', 'Vendor Invoice - Default (Fallback)',
    'vendor_invoice', 'on_finalize',
    NULL, -- No conditions - matches everything
    '{
        "descriptionTemplate": "Purchase Invoice - {source_number}",
        "lines": [
            {"side": "debit", "accountCode": "5000", "amountField": "subtotal", "description": "Purchases"},
            {"side": "debit", "accountCode": "1141", "amountField": "total_cgst", "description": "CGST Input"},
            {"side": "debit", "accountCode": "1142", "amountField": "total_sgst", "description": "SGST Input"},
            {"side": "debit", "accountCode": "1143", "amountField": "total_igst", "description": "IGST Input"},
            {"side": "credit", "accountCode": "2100", "amountField": "total_amount", "description": "Accounts Payable"}
        ]
    }'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_DEFAULT'
);

-- ============================================================================
-- DEFAULT PAYMENT RULE (Fallback)
-- ============================================================================
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'PMT_DEFAULT', 'Payment - Default (Fallback)',
    'payment', 'on_create',
    NULL, -- No conditions - matches everything
    '{
        "descriptionTemplate": "Payment Received - {source_number}",
        "lines": [
            {"side": "debit", "accountCode": "1000", "amountField": "amount", "description": "Bank/Cash"},
            {"side": "credit", "accountCode": "1300", "amountField": "amount", "description": "Accounts Receivable"}
        ]
    }'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'PMT_DEFAULT'
);

-- ============================================================================
-- DEFAULT VENDOR PAYMENT RULE (Fallback)
-- ============================================================================
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule, created_at, updated_at
)
SELECT
    gen_random_uuid(), c.id, 'VPMT_DEFAULT', 'Vendor Payment - Default (Fallback)',
    'vendor_payment', 'on_create',
    NULL, -- No conditions - matches everything
    '{
        "descriptionTemplate": "Vendor Payment - {source_number}",
        "lines": [
            {"side": "debit", "accountCode": "2100", "amountField": "amount", "description": "Accounts Payable"},
            {"side": "credit", "accountCode": "1000", "amountField": "amount", "description": "Bank/Cash"}
        ]
    }'::jsonb,
    '2024-25', '2024-04-01', 999, true, true, NOW(), NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VPMT_DEFAULT'
);

-- ============================================================================
-- VERIFICATION
-- ============================================================================
DO $$
DECLARE
    v_default_rules INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_default_rules
    FROM posting_rules
    WHERE rule_code LIKE '%_DEFAULT'
    AND is_active = true;

    RAISE NOTICE 'Default fallback rules created: %', v_default_rules;
END $$;
