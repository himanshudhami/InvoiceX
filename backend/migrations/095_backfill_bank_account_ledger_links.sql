-- Migration 095: Backfill bank account ledger links
-- Links bank_accounts to their corresponding chart_of_accounts entries
-- This enables the hybrid reconciliation BRS generation

-- First, let's try to link by exact account name match
-- Bank accounts typically have ledger accounts with similar names under code 111x
UPDATE bank_accounts ba
SET linked_account_id = coa.id
FROM chart_of_accounts coa
WHERE ba.company_id = coa.company_id
  AND ba.linked_account_id IS NULL
  AND coa.account_code LIKE '111%'
  AND (
    -- Try exact name match
    LOWER(coa.account_name) = LOWER(ba.account_name)
    OR LOWER(coa.account_name) = LOWER(ba.bank_name || ' - ' || ba.account_type)
    OR LOWER(coa.account_name) LIKE '%' || LOWER(ba.account_name) || '%'
    -- Try bank name + account type pattern
    OR (
      LOWER(coa.account_name) LIKE '%' || LOWER(ba.bank_name) || '%'
      AND LOWER(coa.account_name) LIKE '%' || LOWER(ba.account_type) || '%'
    )
  );

-- Also try matching by last 4 digits of account number (common pattern)
UPDATE bank_accounts ba
SET linked_account_id = coa.id
FROM chart_of_accounts coa
WHERE ba.company_id = coa.company_id
  AND ba.linked_account_id IS NULL
  AND coa.account_code LIKE '111%'
  AND ba.account_number IS NOT NULL
  AND LENGTH(ba.account_number) >= 4
  AND LOWER(coa.account_name) LIKE '%' || RIGHT(ba.account_number, 4) || '%';

-- Log how many bank accounts are still unlinked (for manual review)
-- This is a SELECT that can be run to check the status
/*
SELECT
    ba.id,
    ba.account_name,
    ba.bank_name,
    ba.account_number,
    ba.company_id,
    ba.linked_account_id
FROM bank_accounts ba
WHERE ba.linked_account_id IS NULL
ORDER BY ba.company_id, ba.account_name;
*/

-- Note: Any bank accounts that couldn't be auto-linked should be
-- manually linked via the UI by updating the linked_account_id field
