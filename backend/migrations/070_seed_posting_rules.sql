-- Migration: 070_seed_posting_rules.sql
-- Description: Seed default posting rules for auto-posting journal entries
-- Created: 2025-01-XX

-- Function to create default posting rules for a company
CREATE OR REPLACE FUNCTION create_default_posting_rules(
    p_company_id UUID,
    p_created_by UUID DEFAULT NULL
)
RETURNS INTEGER AS $$
DECLARE
    v_rule_count INTEGER := 0;
    v_fy VARCHAR(10) := (SELECT get_financial_year(CURRENT_DATE));
BEGIN
    -- =============================================================
    -- INVOICE POSTING RULES
    -- =============================================================

    -- INV-001: Invoice Finalization - Domestic B2B Intra-state (CGST + SGST)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'INV_DOM_INTRA_B2B',
        'Invoice - Domestic B2B Intra-state (CGST+SGST)',
        'invoice',
        'on_finalize',
        '{"invoice_type": "b2b", "is_export": false, "is_intra_state": true}'::JSONB,
        '{
            "description_template": "Invoice #{source_number} - {customer_name}",
            "lines": [
                {"account_code": "1120", "side": "debit", "amount_field": "total_amount", "description": "Trade Receivables"},
                {"account_code": "4110", "side": "credit", "amount_field": "subtotal", "description": "Sales - Domestic"},
                {"account_code": "2251", "side": "credit", "amount_field": "total_cgst", "description": "CGST Output"},
                {"account_code": "2252", "side": "credit", "amount_field": "total_sgst", "description": "SGST Output"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- INV-002: Invoice Finalization - Domestic B2B Inter-state (IGST)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'INV_DOM_INTER_B2B',
        'Invoice - Domestic B2B Inter-state (IGST)',
        'invoice',
        'on_finalize',
        '{"invoice_type": "b2b", "is_export": false, "is_intra_state": false}'::JSONB,
        '{
            "description_template": "Invoice #{source_number} - {customer_name}",
            "lines": [
                {"account_code": "1120", "side": "debit", "amount_field": "total_amount", "description": "Trade Receivables"},
                {"account_code": "4110", "side": "credit", "amount_field": "subtotal", "description": "Sales - Domestic"},
                {"account_code": "2253", "side": "credit", "amount_field": "total_igst", "description": "IGST Output"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- INV-003: Invoice Finalization - B2C Intra-state
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'INV_DOM_INTRA_B2C',
        'Invoice - Domestic B2C Intra-state',
        'invoice',
        'on_finalize',
        '{"invoice_type": "b2c", "is_export": false, "is_intra_state": true}'::JSONB,
        '{
            "description_template": "Invoice #{source_number} - {customer_name}",
            "lines": [
                {"account_code": "1120", "side": "debit", "amount_field": "total_amount", "description": "Trade Receivables"},
                {"account_code": "4110", "side": "credit", "amount_field": "subtotal", "description": "Sales - Domestic"},
                {"account_code": "2251", "side": "credit", "amount_field": "total_cgst", "description": "CGST Output"},
                {"account_code": "2252", "side": "credit", "amount_field": "total_sgst", "description": "SGST Output"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- INV-004: Invoice Finalization - Export (Zero-rated)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'INV_EXPORT',
        'Invoice - Export (Zero-rated)',
        'invoice',
        'on_finalize',
        '{"is_export": true}'::JSONB,
        '{
            "description_template": "Export Invoice #{source_number} - {customer_name}",
            "lines": [
                {"account_code": "1120", "side": "debit", "amount_field": "total_amount", "description": "Trade Receivables"},
                {"account_code": "4120", "side": "credit", "amount_field": "total_amount", "description": "Sales - Export"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        90
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- INV-005: Invoice Finalization - SEZ Supply (with IGST)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'INV_SEZ_WITH_IGST',
        'Invoice - SEZ Supply with IGST',
        'invoice',
        'on_finalize',
        '{"invoice_type": "sez_with_payment"}'::JSONB,
        '{
            "description_template": "SEZ Invoice #{source_number} - {customer_name}",
            "lines": [
                {"account_code": "1120", "side": "debit", "amount_field": "total_amount", "description": "Trade Receivables"},
                {"account_code": "4120", "side": "credit", "amount_field": "subtotal", "description": "Sales - Export/SEZ"},
                {"account_code": "2253", "side": "credit", "amount_field": "total_igst", "description": "IGST Output"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        95
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- PAYMENT RECEIPT POSTING RULES
    -- =============================================================

    -- PMT-001: Payment Receipt - Without TDS
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_RECEIPT_NO_TDS',
        'Payment Receipt - Without TDS',
        'payment',
        'on_create',
        '{"tds_applicable": false}'::JSONB,
        '{
            "description_template": "Payment from {customer_name} - {payment_reference}",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "amount", "description": "Bank Account"},
                {"account_code": "1120", "side": "credit", "amount_field": "amount", "description": "Trade Receivables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-002: Payment Receipt - With TDS 194J (Professional Services)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_RECEIPT_TDS_194J',
        'Payment Receipt - With TDS 194J',
        'payment',
        'on_create',
        '{"tds_applicable": true, "tds_section": "194J"}'::JSONB,
        '{
            "description_template": "Payment from {customer_name} - {payment_reference} (TDS 194J)",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "net_amount", "description": "Bank Account"},
                {"account_code": "1131", "side": "debit", "amount_field": "tds_amount", "description": "TDS Receivable - 194J"},
                {"account_code": "1120", "side": "credit", "amount_field": "gross_amount", "description": "Trade Receivables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        90
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-003: Payment Receipt - With TDS 194C (Contractor)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_RECEIPT_TDS_194C',
        'Payment Receipt - With TDS 194C',
        'payment',
        'on_create',
        '{"tds_applicable": true, "tds_section": "194C"}'::JSONB,
        '{
            "description_template": "Payment from {customer_name} - {payment_reference} (TDS 194C)",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "net_amount", "description": "Bank Account"},
                {"account_code": "1132", "side": "debit", "amount_field": "tds_amount", "description": "TDS Receivable - 194C"},
                {"account_code": "1120", "side": "credit", "amount_field": "gross_amount", "description": "Trade Receivables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        90
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-004: Payment Receipt - With TDS 194H (Commission)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_RECEIPT_TDS_194H',
        'Payment Receipt - With TDS 194H',
        'payment',
        'on_create',
        '{"tds_applicable": true, "tds_section": "194H"}'::JSONB,
        '{
            "description_template": "Payment from {customer_name} - {payment_reference} (TDS 194H)",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "net_amount", "description": "Bank Account"},
                {"account_code": "1133", "side": "debit", "amount_field": "tds_amount", "description": "TDS Receivable - 194H"},
                {"account_code": "1120", "side": "credit", "amount_field": "gross_amount", "description": "Trade Receivables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        90
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-005: Advance Payment Received (Before Invoice)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_ADVANCE_RECEIVED',
        'Advance Payment Received',
        'payment',
        'on_create',
        '{"is_advance": true}'::JSONB,
        '{
            "description_template": "Advance from {customer_name} - {payment_reference}",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "amount", "description": "Bank Account"},
                {"account_code": "2130", "side": "credit", "amount_field": "amount", "description": "Advances from Customers"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        80
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- PAYROLL POSTING RULES
    -- =============================================================

    -- PAY-001: Salary Processing
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PAYROLL_SALARY',
        'Payroll - Monthly Salary',
        'payroll_run',
        'on_approval',
        NULL,
        '{
            "description_template": "Payroll - {payroll_month} {payroll_year}",
            "lines": [
                {"account_code": "5210", "side": "debit", "amount_field": "total_gross_salary", "description": "Salaries & Wages"},
                {"account_code": "5220", "side": "debit", "amount_field": "employer_pf", "description": "Employer PF Contribution"},
                {"account_code": "5221", "side": "debit", "amount_field": "employer_esi", "description": "Employer ESI Contribution"},
                {"account_code": "2121", "side": "credit", "amount_field": "total_net_salary", "description": "Salaries Payable"},
                {"account_code": "2220", "side": "credit", "amount_field": "total_pf", "description": "PF Payable"},
                {"account_code": "2221", "side": "credit", "amount_field": "total_esi", "description": "ESI Payable"},
                {"account_code": "2211", "side": "credit", "amount_field": "total_tds", "description": "TDS Payable - Salary"},
                {"account_code": "2222", "side": "credit", "amount_field": "total_professional_tax", "description": "Professional Tax Payable"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PAY-002: Salary Disbursement
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PAYROLL_DISBURSEMENT',
        'Payroll - Salary Disbursement',
        'payroll_run',
        'on_payment',
        NULL,
        '{
            "description_template": "Salary Disbursement - {payroll_month} {payroll_year}",
            "lines": [
                {"account_code": "2121", "side": "debit", "amount_field": "total_net_salary", "description": "Salaries Payable"},
                {"account_code": "1112", "side": "credit", "amount_field": "total_net_salary", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- EXPENSE POSTING RULES
    -- =============================================================

    -- EXP-001: Expense with GST (Intra-state)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'EXP_GST_INTRA',
        'Expense - With GST Intra-state',
        'expense',
        'on_create',
        '{"has_gst": true, "is_intra_state": true}'::JSONB,
        '{
            "description_template": "Expense - {vendor_name} - {description}",
            "lines": [
                {"account_code": "expense_account", "side": "debit", "amount_field": "base_amount", "description": "Expense"},
                {"account_code": "1211", "side": "debit", "amount_field": "cgst_amount", "description": "CGST Input Credit"},
                {"account_code": "1212", "side": "debit", "amount_field": "sgst_amount", "description": "SGST Input Credit"},
                {"account_code": "2110", "side": "credit", "amount_field": "total_amount", "description": "Trade Payables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- EXP-002: Expense with GST (Inter-state)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'EXP_GST_INTER',
        'Expense - With GST Inter-state',
        'expense',
        'on_create',
        '{"has_gst": true, "is_intra_state": false}'::JSONB,
        '{
            "description_template": "Expense - {vendor_name} - {description}",
            "lines": [
                {"account_code": "expense_account", "side": "debit", "amount_field": "base_amount", "description": "Expense"},
                {"account_code": "1213", "side": "debit", "amount_field": "igst_amount", "description": "IGST Input Credit"},
                {"account_code": "2110", "side": "credit", "amount_field": "total_amount", "description": "Trade Payables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- EXP-003: Contractor Payment with TDS
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'EXP_CONTRACTOR_TDS',
        'Contractor Payment with TDS',
        'contractor_payment',
        'on_create',
        '{"tds_applicable": true}'::JSONB,
        '{
            "description_template": "Contractor Payment - {contractor_name} - {description}",
            "lines": [
                {"account_code": "5310", "side": "debit", "amount_field": "gross_amount", "description": "Professional Fees"},
                {"account_code": "1211", "side": "debit", "amount_field": "cgst_amount", "description": "CGST Input Credit"},
                {"account_code": "1212", "side": "debit", "amount_field": "sgst_amount", "description": "SGST Input Credit"},
                {"account_code": "2110", "side": "credit", "amount_field": "net_payable", "description": "Trade Payables"},
                {"account_code": "2212", "side": "credit", "amount_field": "tds_amount", "description": "TDS Payable - 194J/194C"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- PAYMENT OUTWARD RULES
    -- =============================================================

    -- PMT-OUT-001: Vendor Payment
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_VENDOR',
        'Vendor Payment',
        'vendor_payment',
        'on_create',
        NULL,
        '{
            "description_template": "Payment to {vendor_name} - {payment_reference}",
            "lines": [
                {"account_code": "2110", "side": "debit", "amount_field": "amount", "description": "Trade Payables"},
                {"account_code": "1112", "side": "credit", "amount_field": "amount", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-OUT-002: Statutory Payment (PF)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_STATUTORY_PF',
        'PF Remittance',
        'statutory_payment',
        'on_create',
        '{"payment_type": "pf"}'::JSONB,
        '{
            "description_template": "PF Remittance - {period}",
            "lines": [
                {"account_code": "2220", "side": "debit", "amount_field": "amount", "description": "PF Payable"},
                {"account_code": "1112", "side": "credit", "amount_field": "amount", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-OUT-003: Statutory Payment (TDS)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_STATUTORY_TDS',
        'TDS Remittance',
        'statutory_payment',
        'on_create',
        '{"payment_type": "tds"}'::JSONB,
        '{
            "description_template": "TDS Remittance - {period} - {tds_section}",
            "lines": [
                {"account_code": "tds_payable_account", "side": "debit", "amount_field": "amount", "description": "TDS Payable"},
                {"account_code": "1112", "side": "credit", "amount_field": "amount", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- PMT-OUT-004: GST Payment
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'PMT_STATUTORY_GST',
        'GST Payment',
        'statutory_payment',
        'on_create',
        '{"payment_type": "gst"}'::JSONB,
        '{
            "description_template": "GST Payment - {period}",
            "lines": [
                {"account_code": "2250", "side": "debit", "amount_field": "amount", "description": "GST Payable (Net)"},
                {"account_code": "1112", "side": "credit", "amount_field": "amount", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- BANK RECONCILIATION RULES
    -- =============================================================

    -- BANK-001: Bank Charges
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'BANK_CHARGES',
        'Bank Charges',
        'bank_transaction',
        'on_match',
        '{"transaction_type": "bank_charges"}'::JSONB,
        '{
            "description_template": "Bank Charges - {description}",
            "lines": [
                {"account_code": "5610", "side": "debit", "amount_field": "amount", "description": "Bank Charges"},
                {"account_code": "1112", "side": "credit", "amount_field": "amount", "description": "Bank Account"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- BANK-002: Bank Interest Received
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'BANK_INTEREST_INCOME',
        'Bank Interest Income',
        'bank_transaction',
        'on_match',
        '{"transaction_type": "interest_credit"}'::JSONB,
        '{
            "description_template": "Interest Received - {description}",
            "lines": [
                {"account_code": "1112", "side": "debit", "amount_field": "amount", "description": "Bank Account"},
                {"account_code": "4310", "side": "credit", "amount_field": "amount", "description": "Interest Income"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- =============================================================
    -- CREDIT NOTE / DEBIT NOTE RULES
    -- =============================================================

    -- CN-001: Credit Note (Sales Return)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'CREDIT_NOTE_INTRA',
        'Credit Note - Intra-state',
        'credit_note',
        'on_finalize',
        '{"is_intra_state": true}'::JSONB,
        '{
            "description_template": "Credit Note #{source_number} against Invoice #{original_invoice}",
            "lines": [
                {"account_code": "4190", "side": "debit", "amount_field": "subtotal", "description": "Sales Returns"},
                {"account_code": "2251", "side": "debit", "amount_field": "total_cgst", "description": "CGST Output (Reversal)"},
                {"account_code": "2252", "side": "debit", "amount_field": "total_sgst", "description": "SGST Output (Reversal)"},
                {"account_code": "1120", "side": "credit", "amount_field": "total_amount", "description": "Trade Receivables"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    -- DN-001: Debit Note (Purchase Return)
    INSERT INTO posting_rules (
        company_id, rule_code, rule_name, source_type, trigger_event,
        conditions_json, posting_template, financial_year, effective_from, priority
    ) VALUES (
        p_company_id,
        'DEBIT_NOTE_INTRA',
        'Debit Note - Intra-state',
        'debit_note',
        'on_finalize',
        '{"is_intra_state": true}'::JSONB,
        '{
            "description_template": "Debit Note #{source_number} against Bill #{original_bill}",
            "lines": [
                {"account_code": "2110", "side": "debit", "amount_field": "total_amount", "description": "Trade Payables"},
                {"account_code": "expense_account", "side": "credit", "amount_field": "subtotal", "description": "Expense (Reversal)"},
                {"account_code": "1211", "side": "credit", "amount_field": "cgst_amount", "description": "CGST Input (Reversal)"},
                {"account_code": "1212", "side": "credit", "amount_field": "sgst_amount", "description": "SGST Input (Reversal)"}
            ]
        }'::JSONB,
        v_fy,
        '2024-04-01',
        100
    ) ON CONFLICT DO NOTHING;
    v_rule_count := v_rule_count + 1;

    RETURN v_rule_count;
END;
$$ LANGUAGE plpgsql;

-- Add comment
COMMENT ON FUNCTION create_default_posting_rules IS 'Creates default posting rules for a company based on Indian accounting standards. Returns count of rules created.';
