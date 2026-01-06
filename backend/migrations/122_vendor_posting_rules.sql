-- Migration: Add Vendor Invoice and Vendor Payment Posting Rules
-- CA-Approved journal entries per ICAI guidelines and GST Act
-- Follows same pattern as 088_add_domestic_posting_rules.sql

-- ============================================================================
-- VENDOR INVOICE (PURCHASE) POSTING RULES
-- ============================================================================

-- Vendor Invoice - Intra-State (CGST + SGST) - No TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_INTRA',
    'Vendor Invoice - Intra-State (CGST + SGST)',
    'vendor_invoice',
    'on_finalize',
    '{"is_intra_state": true, "tds_applicable": false, "is_rcm": false}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1131",
                "amountField": "total_cgst",
                "description": "CGST Input Credit"
            },
            {
                "side": "debit",
                "accountCode": "1132",
                "amountField": "total_sgst",
                "description": "SGST Input Credit"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "total_amount",
                "description": "Trade Payables (Creditors)"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    100,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTRA'
);

-- Vendor Invoice - Inter-State (IGST) - No TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_INTER',
    'Vendor Invoice - Inter-State (IGST)',
    'vendor_invoice',
    'on_finalize',
    '{"is_intra_state": false, "tds_applicable": false, "is_rcm": false}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1133",
                "amountField": "total_igst",
                "description": "IGST Input Credit"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "total_amount",
                "description": "Trade Payables (Creditors)"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    110,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTER'
);

-- Vendor Invoice - Intra-State with TDS Deductible
-- Net payable = total_amount - tds_amount
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_INTRA_TDS',
    'Vendor Invoice - Intra-State with TDS',
    'vendor_invoice',
    'on_finalize',
    '{"is_intra_state": true, "tds_applicable": true, "is_rcm": false}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number} (TDS Applicable)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1131",
                "amountField": "total_cgst",
                "description": "CGST Input Credit"
            },
            {
                "side": "debit",
                "accountCode": "1132",
                "amountField": "total_sgst",
                "description": "SGST Input Credit"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "net_payable",
                "description": "Trade Payables (Net of TDS)"
            },
            {
                "side": "credit",
                "accountCode": "2260",
                "amountField": "tds_amount",
                "description": "TDS Payable"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    90,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTRA_TDS'
);

-- Vendor Invoice - Inter-State with TDS Deductible
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_INTER_TDS',
    'Vendor Invoice - Inter-State with TDS',
    'vendor_invoice',
    'on_finalize',
    '{"is_intra_state": false, "tds_applicable": true, "is_rcm": false}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number} (TDS Applicable)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1133",
                "amountField": "total_igst",
                "description": "IGST Input Credit"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "net_payable",
                "description": "Trade Payables (Net of TDS)"
            },
            {
                "side": "credit",
                "accountCode": "2260",
                "amountField": "tds_amount",
                "description": "TDS Payable"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    95,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_INTER_TDS'
);

-- Vendor Invoice - Reverse Charge Mechanism (RCM)
-- Under RCM, recipient is liable to pay GST
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_RCM_INTRA',
    'Vendor Invoice - RCM Intra-State',
    'vendor_invoice',
    'on_finalize',
    '{"is_rcm": true, "is_intra_state": true}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number} (RCM)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1131",
                "amountField": "total_cgst",
                "description": "CGST Input (RCM)"
            },
            {
                "side": "debit",
                "accountCode": "1132",
                "amountField": "total_sgst",
                "description": "SGST Input (RCM)"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "subtotal",
                "description": "Trade Payables (Vendor Amount)"
            },
            {
                "side": "credit",
                "accountCode": "2251",
                "amountField": "total_cgst",
                "description": "CGST Output (RCM Liability)"
            },
            {
                "side": "credit",
                "accountCode": "2252",
                "amountField": "total_sgst",
                "description": "SGST Output (RCM Liability)"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    80,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_RCM_INTRA'
);

-- Vendor Invoice - RCM Inter-State
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VINV_RCM_INTER',
    'Vendor Invoice - RCM Inter-State',
    'vendor_invoice',
    'on_finalize',
    '{"is_rcm": true, "is_intra_state": false}'::jsonb,
    '{
        "descriptionTemplate": "Purchase Invoice {source_number} (RCM)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "5100",
                "amountField": "subtotal",
                "description": "Purchases / Cost of Goods"
            },
            {
                "side": "debit",
                "accountCode": "1133",
                "amountField": "total_igst",
                "description": "IGST Input (RCM)"
            },
            {
                "side": "credit",
                "accountCode": "2100",
                "amountField": "subtotal",
                "description": "Trade Payables (Vendor Amount)"
            },
            {
                "side": "credit",
                "accountCode": "2253",
                "amountField": "total_igst",
                "description": "IGST Output (RCM Liability)"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    85,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VINV_RCM_INTER'
);

-- ============================================================================
-- VENDOR PAYMENT (OUTGOING) POSTING RULES
-- ============================================================================

-- Vendor Payment - Regular Bill Payment (No TDS)
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VPMT_REGULAR',
    'Vendor Payment - Regular (No TDS)',
    'vendor_payment',
    'on_finalize',
    '{"tds_applicable": false, "is_advance": false}'::jsonb,
    '{
        "descriptionTemplate": "Payment to Vendor {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "2100",
                "amountField": "amount",
                "description": "Trade Payables (Creditors)"
            },
            {
                "side": "credit",
                "accountCode": "1112",
                "amountField": "amount",
                "description": "Bank Payment"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    100,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VPMT_REGULAR'
);

-- Vendor Payment - With TDS Already Deducted on Invoice
-- When TDS was deducted at invoice time, payment is just net_amount
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VPMT_TDS_DEDUCTED',
    'Vendor Payment - TDS Already Deducted',
    'vendor_payment',
    'on_finalize',
    '{"tds_applicable": true, "is_advance": false}'::jsonb,
    '{
        "descriptionTemplate": "Payment to Vendor {source_number} (Net of TDS)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "2100",
                "amountField": "net_amount",
                "description": "Trade Payables (Net Payment)"
            },
            {
                "side": "credit",
                "accountCode": "1112",
                "amountField": "net_amount",
                "description": "Bank Payment"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    90,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VPMT_TDS_DEDUCTED'
);

-- Advance Payment to Vendor
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VPMT_ADVANCE',
    'Advance Payment to Vendor',
    'vendor_payment',
    'on_finalize',
    '{"is_advance": true}'::jsonb,
    '{
        "descriptionTemplate": "Advance to Vendor {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1140",
                "amountField": "amount",
                "description": "Advance to Suppliers"
            },
            {
                "side": "credit",
                "accountCode": "1112",
                "amountField": "amount",
                "description": "Bank Payment"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    80,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VPMT_ADVANCE'
);

-- Advance Payment to Vendor with TDS
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'VPMT_ADVANCE_TDS',
    'Advance Payment to Vendor with TDS',
    'vendor_payment',
    'on_finalize',
    '{"is_advance": true, "tds_applicable": true}'::jsonb,
    '{
        "descriptionTemplate": "Advance to Vendor {source_number} (TDS Deducted)",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1140",
                "amountField": "gross_amount",
                "description": "Advance to Suppliers (Gross)"
            },
            {
                "side": "credit",
                "accountCode": "1112",
                "amountField": "net_amount",
                "description": "Bank Payment (Net of TDS)"
            },
            {
                "side": "credit",
                "accountCode": "2260",
                "amountField": "tds_amount",
                "description": "TDS Payable"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    75,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'VPMT_ADVANCE_TDS'
);

-- TDS Deposit to Government
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'TDS_DEPOSIT',
    'TDS Deposit to Government',
    'tds_deposit',
    'on_finalize',
    '{}'::jsonb,
    '{
        "descriptionTemplate": "TDS Deposit - Challan {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "2260",
                "amountField": "amount",
                "description": "TDS Payable (Cleared)"
            },
            {
                "side": "credit",
                "accountCode": "1112",
                "amountField": "amount",
                "description": "Bank Payment"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    100,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'TDS_DEPOSIT'
);

-- ============================================================================
-- Add missing Chart of Account entries for AP module
-- ============================================================================

-- Add Advance to Suppliers account (1140) if not exists
INSERT INTO chart_of_accounts (
    id, company_id, account_code, account_name, account_type,
    parent_account_id, is_control_account, is_system_account,
    is_active, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    '1140',
    'Advances to Suppliers',
    'asset',
    (SELECT id FROM chart_of_accounts WHERE company_id = c.id AND account_code = '1100' LIMIT 1),
    false,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '1140'
)
AND EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '1100'
);

-- Log the created rules
DO $$
DECLARE
    v_invoice_count integer;
    v_payment_count integer;
BEGIN
    SELECT COUNT(DISTINCT rule_code) INTO v_invoice_count
    FROM posting_rules
    WHERE rule_code LIKE 'VINV_%';

    SELECT COUNT(DISTINCT rule_code) INTO v_payment_count
    FROM posting_rules
    WHERE rule_code LIKE 'VPMT_%' OR rule_code = 'TDS_DEPOSIT';

    RAISE NOTICE 'Vendor posting rules created: % invoice rules, % payment rules', v_invoice_count, v_payment_count;
END $$;
