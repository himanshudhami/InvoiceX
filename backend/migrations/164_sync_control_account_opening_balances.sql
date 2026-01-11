-- Migration: Sync control account opening balances from subledger (tally_ledger_mapping) totals
-- Part of COA Modernization - ensures Trade Receivables/Payables reflect sum of party balances
-- This is a ONE-TIME fix for existing data. Future Tally imports will do this automatically.

-- Update control account opening balances from subledger totals
UPDATE chart_of_accounts coa
SET
    opening_balance = subq.subledger_total,
    current_balance = subq.subledger_total,
    updated_at = NOW()
FROM (
    SELECT
        tlm.control_account_id,
        SUM(tlm.opening_balance) as subledger_total
    FROM tally_ledger_mapping tlm
    WHERE tlm.is_active = true
    GROUP BY tlm.control_account_id
    HAVING SUM(tlm.opening_balance) != 0
) subq
WHERE coa.id = subq.control_account_id
  AND coa.is_control_account = true;

-- Verify the fix
SELECT
    coa.account_code,
    coa.account_name,
    coa.opening_balance as control_opening_balance,
    COALESCE(SUM(tlm.opening_balance), 0) as subledger_total,
    coa.opening_balance - COALESCE(SUM(tlm.opening_balance), 0) as difference
FROM chart_of_accounts coa
LEFT JOIN tally_ledger_mapping tlm ON tlm.control_account_id = coa.id AND tlm.is_active = true
WHERE coa.is_control_account = true
GROUP BY coa.id, coa.account_code, coa.account_name, coa.opening_balance
ORDER BY coa.account_code;
