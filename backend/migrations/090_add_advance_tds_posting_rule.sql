-- Migration: Add Advance Payment with TDS posting rule
-- For cases where advance is received with TDS deducted

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name, source_type, trigger_event,
    conditions_json, posting_template, financial_year,
    effective_from, priority, is_active, is_system_rule,
    created_at, updated_at
)
SELECT
    gen_random_uuid(),
    c.id,
    'PMT_ADVANCE_TDS',
    'Advance Payment with TDS Deducted',
    'payment',
    'on_finalize',
    '{"is_advance": true, "tds_applicable": true}'::jsonb,
    '{
        "descriptionTemplate": "Advance Received with TDS from {customer_name}",
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
                "accountCode": "2310",
                "amountField": "gross_amount",
                "description": "Advance from Customers"
            }
        ]
    }'::jsonb,
    '2024-25',
    '2024-04-01',
    0,  -- Highest priority (before PMT_ADVANCE)
    true,
    true,
    NOW(),
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM posting_rules pr
    WHERE pr.company_id = c.id AND pr.rule_code = 'PMT_ADVANCE_TDS'
);

-- Update PMT_ADVANCE to explicitly require tds_applicable = false
UPDATE posting_rules
SET conditions_json = '{"is_advance": true, "tds_applicable": false}'::jsonb
WHERE rule_code = 'PMT_ADVANCE';
