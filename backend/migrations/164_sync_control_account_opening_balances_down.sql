-- Rollback: Reset control account opening balances to zero
-- Note: This loses the synced balances - only use if you need to re-import

UPDATE chart_of_accounts
SET
    opening_balance = 0,
    current_balance = 0,
    updated_at = NOW()
WHERE is_control_account = true
  AND account_code IN ('1120', '2100');
