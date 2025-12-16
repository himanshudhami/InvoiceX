-- 014_fix_gujarat_pt_slabs.sql
-- Fix Gujarat Professional Tax slabs (Gujarat has PT, not 0)

-- Delete the incorrect Gujarat entry
DELETE FROM professional_tax_slabs WHERE state = 'Gujarat';

-- Insert correct Gujarat PT slabs
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Gujarat', 0, 5999, 0),
('Gujarat', 6000, 8999, 80),
('Gujarat', 9000, 11999, 150),
('Gujarat', 12000, NULL, 200);


