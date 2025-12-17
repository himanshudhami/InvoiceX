-- 042_fix_new_regime_2025_26_budget_2025.sql
-- Update New Tax Regime slabs for FY 2025-26 as per Union Budget 2025
-- Previous migration (020) used placeholder rates; this migration applies actual Budget 2025 rates

-- ============================================================================
-- NEW TAX REGIME FY 2025-26 - BUDGET 2025 UPDATES
-- ============================================================================
-- Key changes in Budget 2025:
-- - Basic exemption increased from ₹3L to ₹4L
-- - New 25% slab introduced (₹20L-₹24L)
-- - Slab boundaries changed across all brackets

-- Delete existing 2025-26 new regime slabs (placeholder rates from migration 020)
DELETE FROM tax_slabs
WHERE regime = 'new'
  AND financial_year = '2025-26'
  AND applicable_to_category = 'all';

-- Insert correct Budget 2025 new regime slabs
INSERT INTO tax_slabs (id, regime, financial_year, applicable_to_category, min_income, max_income, rate, cess_rate, is_active, created_at, updated_at)
VALUES
  (gen_random_uuid(), 'new', '2025-26', 'all', 0, 400000, 0, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 400001, 800000, 5, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 800001, 1200000, 10, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 1200001, 1600000, 15, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 1600001, 2000000, 20, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 2000001, 2400000, 25, 4, true, NOW(), NOW()),
  (gen_random_uuid(), 'new', '2025-26', 'all', 2400001, NULL, 30, 4, true, NOW(), NOW());

-- Update tax parameters for new regime FY 2025-26 per Budget 2025
-- Note: Standard deduction increased to ₹75,000 (already correct in migration 020)
-- Rebate threshold increased to ₹12L (was ₹7L)
UPDATE tax_parameters
SET parameter_value = 1200000,
    description = 'Income threshold up to which full tax rebate is available under new regime (Budget 2025)',
    updated_at = NOW()
WHERE financial_year = '2025-26'
  AND regime = 'new'
  AND parameter_code = 'REBATE_87A_THRESHOLD';

-- Update max rebate amount (increased to ₹60,000 for income up to ₹12L)
UPDATE tax_parameters
SET parameter_value = 60000,
    description = 'Maximum rebate amount under Section 87A for new regime (Budget 2025)',
    updated_at = NOW()
WHERE financial_year = '2025-26'
  AND regime = 'new'
  AND parameter_code = 'REBATE_87A_MAX';
