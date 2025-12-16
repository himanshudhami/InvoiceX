-- Migration: Fix Karnataka Professional Tax slabs per April 2025 revision
-- Karnataka Act No. 33 of 2025, effective from April 1, 2025
-- Reference: https://greythr.freshdesk.com/support/solutions/articles/1060000142762
--
-- OLD SLABS (Before April 2025):
--   Rs 0 - Rs 15,000: NIL
--   Rs 15,001+: Rs 200
--
-- NEW SLABS (After April 2025):
--   Rs 0 - Rs 24,999: NIL (Middle-income relief)
--   Rs 25,000+: Rs 200 (Rs 300 in February to reach annual cap of Rs 2,500)

-- Delete existing Karnataka slabs
DELETE FROM professional_tax_slabs WHERE state = 'Karnataka';

-- Insert new Karnataka slabs per April 2025 revision
INSERT INTO professional_tax_slabs (id, state, min_monthly_income, max_monthly_income, monthly_tax, is_active, created_at, updated_at)
VALUES
  (gen_random_uuid(), 'Karnataka', 0, 24999, 0, true, NOW(), NOW()),
  (gen_random_uuid(), 'Karnataka', 25000, NULL, 200, true, NOW(), NOW());

-- Add comment for reference
COMMENT ON TABLE professional_tax_slabs IS 'State-wise Professional Tax slabs. Karnataka updated per Act No. 33 of 2025 (effective April 1, 2025)';
