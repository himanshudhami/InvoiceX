-- Rollback: Remove Suspense Account (2900)
-- Only delete if there are no journal entries using this account

DELETE FROM chart_of_accounts
WHERE account_code = '2900'
  AND account_name = 'Suspense Account'
  AND NOT EXISTS (
      SELECT 1 FROM journal_entry_lines jel
      WHERE jel.account_id = chart_of_accounts.id
  );
