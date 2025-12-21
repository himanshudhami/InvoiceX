-- Migration: 082_seed_forex_posting_rules.sql
-- Purpose: Create posting rules for export invoices and forex payments (Ind AS 21 compliant)
-- These rules enable auto-posting of journal entries for foreign currency transactions

-- Account codes kept within 20 character limit:
-- AR_FOREX (8), REVENUE_EXPORT (14), BANK_USD (8)
-- FX_GAIN_REAL (12), FX_LOSS_REAL (12), FX_GAIN_UNREAL (14), FX_LOSS_UNREAL (14)

-- ============================================================================
-- POSTING RULE 1: Export Invoice (Foreign Currency)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'INV_EXPORT_USD',
    'Export Invoice - USD',
    'invoice',
    'on_finalize',
    '{"is_export": true}'::jsonb,
    '{
        "descriptionTemplate": "Export Invoice {source_number}",
        "lines": [
            {
                "accountCode": "AR_FOREX",
                "side": "debit",
                "amountField": "invoice_amount_inr",
                "description": "Trade Receivables (USD)"
            },
            {
                "accountCode": "REVENUE_EXPORT",
                "side": "credit",
                "amountField": "invoice_amount_inr",
                "description": "Export Services Revenue"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- POSTING RULE 2: Export Payment Receipt (with Forex Gain)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'PMT_EXPORT_FOREX_GAIN',
    'Export Payment - Forex Gain',
    'payment',
    'on_finalize',
    '{"is_export": true, "has_forex_gain": true}'::jsonb,
    '{
        "descriptionTemplate": "Export Payment Receipt - Forex Gain {source_number}",
        "lines": [
            {
                "accountCode": "BANK_USD",
                "side": "debit",
                "amountField": "payment_amount_inr",
                "description": "Bank Receipt (USD converted)"
            },
            {
                "accountCode": "AR_FOREX",
                "side": "credit",
                "amountField": "booking_amount_inr",
                "description": "Settlement of Trade Receivables"
            },
            {
                "accountCode": "FX_GAIN_REAL",
                "side": "credit",
                "amountField": "forex_gain",
                "description": "Realized Forex Gain"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- POSTING RULE 3: Export Payment Receipt (with Forex Loss)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'PMT_EXPORT_FOREX_LOSS',
    'Export Payment - Forex Loss',
    'payment',
    'on_finalize',
    '{"is_export": true, "has_forex_loss": true}'::jsonb,
    '{
        "descriptionTemplate": "Export Payment Receipt - Forex Loss {source_number}",
        "lines": [
            {
                "accountCode": "BANK_USD",
                "side": "debit",
                "amountField": "payment_amount_inr",
                "description": "Bank Receipt (USD converted)"
            },
            {
                "accountCode": "FX_LOSS_REAL",
                "side": "debit",
                "amountField": "forex_loss",
                "description": "Realized Forex Loss"
            },
            {
                "accountCode": "AR_FOREX",
                "side": "credit",
                "amountField": "booking_amount_inr",
                "description": "Settlement of Trade Receivables"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- POSTING RULE 4: Export Payment Receipt (No Forex Impact)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'PMT_EXPORT_NO_FOREX',
    'Export Payment - No Forex Impact',
    'payment',
    'on_finalize',
    '{"is_export": true, "has_forex_gain": false, "has_forex_loss": false}'::jsonb,
    '{
        "descriptionTemplate": "Export Payment Receipt {source_number}",
        "lines": [
            {
                "accountCode": "BANK_USD",
                "side": "debit",
                "amountField": "payment_amount_inr",
                "description": "Bank Receipt"
            },
            {
                "accountCode": "AR_FOREX",
                "side": "credit",
                "amountField": "payment_amount_inr",
                "description": "Settlement of Trade Receivables"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    20,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- POSTING RULE 5: Month-End Forex Revaluation (Gain)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'REVAL_FOREX_GAIN',
    'Forex Revaluation - Gain',
    'revaluation',
    'on_finalize',
    '{"has_unrealized_gain": true}'::jsonb,
    '{
        "descriptionTemplate": "Month-End Forex Revaluation - Gain {source_number}",
        "lines": [
            {
                "accountCode": "AR_FOREX",
                "side": "debit",
                "amountField": "unrealized_gain",
                "description": "Revaluation Adjustment"
            },
            {
                "accountCode": "FX_GAIN_UNREAL",
                "side": "credit",
                "amountField": "unrealized_gain",
                "description": "Unrealized Forex Gain"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- POSTING RULE 6: Month-End Forex Revaluation (Loss)
-- ============================================================================

INSERT INTO posting_rules (
    id, company_id, rule_code, rule_name,
    source_type, trigger_event, conditions_json,
    posting_template, financial_year, effective_from,
    priority, is_active, is_system_rule, created_at, updated_at
) VALUES (
    gen_random_uuid(),
    NULL,
    'REVAL_FOREX_LOSS',
    'Forex Revaluation - Loss',
    'revaluation',
    'on_finalize',
    '{"has_unrealized_loss": true}'::jsonb,
    '{
        "descriptionTemplate": "Month-End Forex Revaluation - Loss {source_number}",
        "lines": [
            {
                "accountCode": "FX_LOSS_UNREAL",
                "side": "debit",
                "amountField": "unrealized_loss",
                "description": "Unrealized Forex Loss"
            },
            {
                "accountCode": "AR_FOREX",
                "side": "credit",
                "amountField": "unrealized_loss",
                "description": "Revaluation Adjustment"
            }
        ]
    }'::jsonb,
    '2025-26',
    '2025-04-01',
    10,
    true,
    true,
    NOW(),
    NOW()
);

-- ============================================================================
-- Add required Chart of Accounts entries if they don't exist
-- Account codes: max 20 chars
-- ============================================================================

DO $$
DECLARE
    v_company_id UUID;
    v_max_sort INT;
BEGIN
    SELECT id INTO v_company_id FROM companies LIMIT 1;

    IF v_company_id IS NOT NULL THEN
        -- Get max sort_order for new accounts
        SELECT COALESCE(MAX(sort_order), 0) INTO v_max_sort FROM chart_of_accounts WHERE company_id = v_company_id;

        -- Trade Receivables - Foreign Currency (AR_FOREX = 8 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account, is_control_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'AR_FOREX',
            'Trade Receivables - Foreign Currency', 'asset',
            3, true, true, true,
            'debit', v_max_sort + 1, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'AR_FOREX'
        );

        -- Export Revenue (REVENUE_EXPORT = 14 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'REVENUE_EXPORT',
            'Export Services Revenue', 'income',
            3, true, true,
            'credit', v_max_sort + 2, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'REVENUE_EXPORT'
        );

        -- Bank Account - USD (BANK_USD = 8 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account, is_bank_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'BANK_USD',
            'Bank Account - USD', 'asset',
            3, true, true, true,
            'debit', v_max_sort + 3, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'BANK_USD'
        );

        -- Forex Gain - Realized (FX_GAIN_REAL = 12 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'FX_GAIN_REAL',
            'Foreign Exchange Gain - Realized', 'income',
            3, true, true,
            'credit', v_max_sort + 4, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'FX_GAIN_REAL'
        );

        -- Forex Loss - Realized (FX_LOSS_REAL = 12 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'FX_LOSS_REAL',
            'Foreign Exchange Loss - Realized', 'expense',
            3, true, true,
            'debit', v_max_sort + 5, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'FX_LOSS_REAL'
        );

        -- Forex Gain - Unrealized (FX_GAIN_UNREAL = 14 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'FX_GAIN_UNREAL',
            'Foreign Exchange Gain - Unrealized', 'income',
            3, true, true,
            'credit', v_max_sort + 6, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'FX_GAIN_UNREAL'
        );

        -- Forex Loss - Unrealized (FX_LOSS_UNREAL = 14 chars)
        INSERT INTO chart_of_accounts (
            id, company_id, account_code, account_name, account_type,
            depth_level, is_active, is_system_account,
            normal_balance, sort_order, created_at, updated_at
        )
        SELECT
            gen_random_uuid(), v_company_id, 'FX_LOSS_UNREAL',
            'Foreign Exchange Loss - Unrealized', 'expense',
            3, true, true,
            'debit', v_max_sort + 7, NOW(), NOW()
        WHERE NOT EXISTS (
            SELECT 1 FROM chart_of_accounts
            WHERE company_id = v_company_id AND account_code = 'FX_LOSS_UNREAL'
        );
    END IF;
END $$;
