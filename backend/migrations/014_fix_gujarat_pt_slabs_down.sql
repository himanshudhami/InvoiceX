-- 014_fix_gujarat_pt_slabs_down.sql
-- Rollback: Restore original Gujarat PT entry (0)

-- Delete the corrected Gujarat entries
DELETE FROM professional_tax_slabs WHERE state = 'Gujarat';

-- Restore original (incorrect) entry
INSERT INTO professional_tax_slabs (state, min_monthly_income, max_monthly_income, monthly_tax) VALUES
('Gujarat', 0, NULL, 0);




