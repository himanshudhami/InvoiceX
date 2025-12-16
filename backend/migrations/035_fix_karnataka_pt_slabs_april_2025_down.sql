-- Rollback: Restore old Karnataka PT slabs (pre-April 2025)

DELETE FROM professional_tax_slabs WHERE state = 'Karnataka';

-- Restore original Karnataka slabs
INSERT INTO professional_tax_slabs (id, state, min_monthly_income, max_monthly_income, monthly_tax, is_active, created_at, updated_at)
VALUES
  (gen_random_uuid(), 'Karnataka', 0, 15000, 0, true, NOW(), NOW()),
  (gen_random_uuid(), 'Karnataka', 15001, NULL, 200, true, NOW(), NOW());
