-- Migration 165: Fix voucher type routing for Sales vouchers
-- Issue: Sales vouchers for parties classified as vendors were posting to Trade Payables
-- Fix: Route based on VOUCHER TYPE, not party classification
-- Sales/Receipt/Debit Note → Trade Receivables (1120)
-- Purchase/Payment/Credit Note → Trade Payables (2100)

-- Step 1: Fix Sales/Receipt/Debit Note vouchers incorrectly posted to Trade Payables
-- These should be in Trade Receivables
UPDATE journal_entry_lines jel
SET
    account_id = coa_receivables.id,
    subledger_type = 'customer'
FROM journal_entries je
CROSS JOIN (SELECT id FROM chart_of_accounts WHERE account_code = '1120' LIMIT 1) coa_receivables
WHERE jel.journal_entry_id = je.id
AND LOWER(je.tally_voucher_type) IN ('sales', 'receipt', 'debit_note', 'debit note')
AND jel.account_id IN (SELECT id FROM chart_of_accounts WHERE account_code = '2100')
AND jel.subledger_id IS NOT NULL;

-- Step 2: Fix Purchase/Payment/Credit Note vouchers incorrectly posted to Trade Receivables
-- These should be in Trade Payables
UPDATE journal_entry_lines jel
SET
    account_id = coa_payables.id,
    subledger_type = 'vendor'
FROM journal_entries je
CROSS JOIN (SELECT id FROM chart_of_accounts WHERE account_code = '2100' LIMIT 1) coa_payables
WHERE jel.journal_entry_id = je.id
AND LOWER(je.tally_voucher_type) IN ('purchase', 'payment', 'credit_note', 'credit note')
AND jel.account_id IN (SELECT id FROM chart_of_accounts WHERE account_code = '1120')
AND jel.subledger_id IS NOT NULL;

-- Step 3: Update party flags for dual-role parties
-- Parties with customer-type transactions in Trade Receivables should have is_customer=true
UPDATE parties p
SET
    is_customer = true,
    updated_at = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 FROM journal_entry_lines jel
    JOIN journal_entries je ON je.id = jel.journal_entry_id
    JOIN chart_of_accounts coa ON coa.id = jel.account_id
    WHERE jel.subledger_id = p.id
    AND coa.account_code = '1120'
    AND LOWER(je.tally_voucher_type) IN ('sales', 'receipt', 'debit_note', 'debit note')
)
AND p.is_customer = false;

-- Parties with vendor-type transactions in Trade Payables should have is_vendor=true
UPDATE parties p
SET
    is_vendor = true,
    updated_at = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 FROM journal_entry_lines jel
    JOIN journal_entries je ON je.id = jel.journal_entry_id
    JOIN chart_of_accounts coa ON coa.id = jel.account_id
    WHERE jel.subledger_id = p.id
    AND coa.account_code = '2100'
    AND LOWER(je.tally_voucher_type) IN ('purchase', 'payment', 'credit_note', 'credit note')
)
AND p.is_vendor = false;
