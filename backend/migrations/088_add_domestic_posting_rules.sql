-- Migration: Add Domestic Posting Rules for Invoices and Payments
-- CA-Approved journal entries per ICAI guidelines and GST Act

-- Insert posting rules for each company
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'INV_DOMESTIC_INTRA',
    'Domestic Invoice - Intra-State (CGST + SGST)',
    'invoice',
    'invoice_created',
    '{"is_export": false, "is_intra_state": true}'::jsonb,
    '{
        "descriptionTemplate": "Sales Invoice {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1120",
                "amountField": "total_amount",
                "description": "Trade Receivables"
            },
            {
                "side": "credit",
                "accountCode": "4100",
                "amountField": "subtotal",
                "description": "Sales Revenue"
            },
            {
                "side": "credit",
                "accountCode": "2251",
                "amountField": "total_cgst",
                "description": "CGST Output"
            },
            {
                "side": "credit",
                "accountCode": "2252",
                "amountField": "total_sgst",
                "description": "SGST Output"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'INV_DOMESTIC_INTRA'
);

-- Domestic Invoice - Inter-State (IGST)
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'INV_DOMESTIC_INTER',
    'Domestic Invoice - Inter-State (IGST)',
    'invoice',
    'invoice_created',
    '{"is_export": false, "is_intra_state": false}'::jsonb,
    '{
        "descriptionTemplate": "Sales Invoice {source_number}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1120",
                "amountField": "total_amount",
                "description": "Trade Receivables"
            },
            {
                "side": "credit",
                "accountCode": "4100",
                "amountField": "subtotal",
                "description": "Sales Revenue"
            },
            {
                "side": "credit",
                "accountCode": "2253",
                "amountField": "total_igst",
                "description": "IGST Output"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    20,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'INV_DOMESTIC_INTER'
);

-- Payment Received - No TDS (Domestic)
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'PMT_DOMESTIC',
    'Payment Received - Domestic (No TDS)',
    'payment',
    'payment_received',
    '{"tds_applicable": false, "is_advance": false}'::jsonb,
    '{
        "descriptionTemplate": "Payment Received {payment_reference}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1112",
                "amountField": "amount",
                "description": "Bank Receipt"
            },
            {
                "side": "credit",
                "accountCode": "1120",
                "amountField": "amount",
                "description": "Trade Receivables"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'PMT_DOMESTIC'
);

-- Payment Received - With TDS Deducted
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'PMT_DOMESTIC_TDS',
    'Payment Received - With TDS Deducted',
    'payment',
    'payment_received',
    '{"tds_applicable": true, "is_advance": false}'::jsonb,
    '{
        "descriptionTemplate": "Payment Received with TDS {payment_reference}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1112",
                "amountField": "net_amount",
                "description": "Bank Receipt (Net of TDS)"
            },
            {
                "side": "debit",
                "accountCode": "1130",
                "amountField": "tds_amount",
                "description": "TDS Receivable"
            },
            {
                "side": "credit",
                "accountCode": "1120",
                "amountField": "gross_amount",
                "description": "Trade Receivables"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    5,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'PMT_DOMESTIC_TDS'
);

-- Advance Payment Received from Customer
INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'PMT_ADVANCE',
    'Advance Payment Received from Customer',
    'payment',
    'payment_received',
    '{"is_advance": true}'::jsonb,
    '{
        "descriptionTemplate": "Advance Received from Customer {customer_name}",
        "lines": [
            {
                "side": "debit",
                "accountCode": "1112",
                "amountField": "amount",
                "description": "Bank Receipt"
            },
            {
                "side": "credit",
                "accountCode": "2310",
                "amountField": "amount",
                "description": "Advance from Customers"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    1,
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'PMT_ADVANCE'
);

-- Log the created rules
DO $$
DECLARE
    v_count integer;
BEGIN
    SELECT COUNT(DISTINCT rule_code) INTO v_count
    FROM posting_rules
    WHERE rule_code IN ('INV_DOMESTIC_INTRA', 'INV_DOMESTIC_INTER', 'PMT_DOMESTIC', 'PMT_DOMESTIC_TDS', 'PMT_ADVANCE');

    RAISE NOTICE 'Domestic posting rules created: % rule types', v_count;
END $$;
