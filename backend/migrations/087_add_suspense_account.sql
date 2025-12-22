-- Migration: Add Suspense Account (2900) for Bank Reconciliation
-- Per ICAI/CGA guidelines, suspense accounts are temporary holding accounts
-- for unidentified transactions pending reconciliation.
-- Items should be cleared within 30-90 days.

-- Insert suspense account for each company that has a Current Liabilities (2000) account
INSERT INTO chart_of_accounts (
    id, company_id, account_code, account_name, account_type, account_subtype,
    parent_account_id, depth_level, normal_balance, sort_order, is_active,
    is_system_account, description, created_at, updated_at
)
SELECT
    gen_random_uuid(),
    cl.company_id,
    '2900',
    'Suspense Account',
    'liability',
    'current_liability',
    cl.id,  -- parent_account_id (Current Liabilities)
    1,      -- depth_level
    'credit',
    900,    -- sort_order
    true,   -- is_active
    true,   -- is_system_account
    'Temporary holding account for unidentified transactions pending reconciliation. Per ICAI/CGA guidelines, items should be cleared within 30-90 days.',
    NOW(),
    NOW()
FROM chart_of_accounts cl
WHERE cl.account_code = '2000'
  AND cl.account_name = 'Current Liabilities'
  AND NOT EXISTS (
      SELECT 1 FROM chart_of_accounts
      WHERE company_id = cl.company_id AND account_code = '2900'
  );

-- Log the created accounts
DO $$
DECLARE
    v_count integer;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM chart_of_accounts
    WHERE account_code = '2900';

    RAISE NOTICE 'Suspense accounts created/verified: % companies', v_count;
END $$;
