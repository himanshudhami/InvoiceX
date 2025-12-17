-- 042_fix_new_regime_2025_26_budget_2025_down.sql
-- Rollback Budget 2025 new regime slabs to pre-budget placeholder values

-- Delete Budget 2025 new regime slabs
DELETE FROM tax_slabs
WHERE regime = 'new'
  AND financial_year = '2025-26'
  AND applicable_to_category = 'all';

-- Restore original placeholder slabs from migration 020
INSERT INTO tax_slabs (id, regime, financial_year, applicable_to_category, min_income, max_income, rate, cess_rate, is_active, created_at, updated_at)
VALUES
  (gen_random_uuid(), 'new', '2025-26', 'all', 0, 300000, 0, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 300001, 700000, 5, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 700001, 1000000, 10, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 1000001, 1200000, 15, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 1200001, 1500000, 20, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 1500001, NULL, 30, 4, true, NOW(), NOW());

-- Restore original tax parameter values
UPDATE tax_parameters
SET parameter_value = 700000,
    description = 'Taxable income threshold for 87A rebate eligibility under new regime',
    updated_at = NOW()
WHERE financial_year = '2025-26'
  AND regime = 'new'
  AND parameter_code = 'REBATE_87A_THRESHOLD';

UPDATE tax_parameters
SET parameter_value = 25000,
    description = 'Maximum rebate amount under Section 87A for new regime',
    updated_at = NOW()
WHERE financial_year = '2025-26'
  AND regime = 'new'
  AND parameter_code = 'REBATE_87A_MAX';
