-- Phase 2: YTD vs Projection Split
-- Separates actual YTD values (locked) from projected future values (editable)

-- Add YTD actual columns to advance_tax_assessments
ALTER TABLE advance_tax_assessments
    ADD COLUMN IF NOT EXISTS ytd_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS ytd_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS ytd_through_date DATE,
    ADD COLUMN IF NOT EXISTS projected_additional_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS projected_additional_expenses DECIMAL(18,2) NOT NULL DEFAULT 0;

-- Add comment for documentation
COMMENT ON COLUMN advance_tax_assessments.ytd_revenue IS 'Actual revenue from ledger (Apr to ytd_through_date)';
COMMENT ON COLUMN advance_tax_assessments.ytd_expenses IS 'Actual expenses from ledger (Apr to ytd_through_date)';
COMMENT ON COLUMN advance_tax_assessments.ytd_through_date IS 'Last date of actuals (locked)';
COMMENT ON COLUMN advance_tax_assessments.projected_additional_revenue IS 'User projection for remaining FY (editable)';
COMMENT ON COLUMN advance_tax_assessments.projected_additional_expenses IS 'User projection for remaining FY (editable)';
