-- Phase 3: Book Profit to Taxable Income Reconciliation
-- Adds adjustment fields for proper tax computation per Income Tax Act

-- Add reconciliation columns to advance_tax_assessments
ALTER TABLE advance_tax_assessments
    -- Book Profit (computed from P&L)
    ADD COLUMN IF NOT EXISTS book_profit DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Additions to book profit (expenses disallowed)
    ADD COLUMN IF NOT EXISTS add_book_depreciation DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS add_disallowed_40a3 DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS add_disallowed_40a7 DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS add_disallowed_43b DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS add_other_disallowances DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_additions DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Deductions from book profit
    ADD COLUMN IF NOT EXISTS less_it_depreciation DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS less_deductions_80c DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS less_deductions_80d DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS less_other_deductions DECIMAL(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS total_deductions DECIMAL(18,2) NOT NULL DEFAULT 0;

-- Add comments for documentation
COMMENT ON COLUMN advance_tax_assessments.book_profit IS 'Profit as per books (P&L)';
COMMENT ON COLUMN advance_tax_assessments.add_book_depreciation IS 'Add back: Depreciation as per books';
COMMENT ON COLUMN advance_tax_assessments.add_disallowed_40a3 IS 'Add back: Cash payments > Rs 10,000 (Sec 40A(3))';
COMMENT ON COLUMN advance_tax_assessments.add_disallowed_40a7 IS 'Add back: Provision for gratuity (Sec 40A(7))';
COMMENT ON COLUMN advance_tax_assessments.add_disallowed_43b IS 'Add back: Unpaid statutory dues (Sec 43B)';
COMMENT ON COLUMN advance_tax_assessments.add_other_disallowances IS 'Add back: Other disallowed expenses';
COMMENT ON COLUMN advance_tax_assessments.total_additions IS 'Total additions to book profit';
COMMENT ON COLUMN advance_tax_assessments.less_it_depreciation IS 'Less: Depreciation as per IT Act';
COMMENT ON COLUMN advance_tax_assessments.less_deductions_80c IS 'Less: Deductions u/s 80C';
COMMENT ON COLUMN advance_tax_assessments.less_deductions_80d IS 'Less: Deductions u/s 80D';
COMMENT ON COLUMN advance_tax_assessments.less_other_deductions IS 'Less: Other deductions';
COMMENT ON COLUMN advance_tax_assessments.total_deductions IS 'Total deductions from book profit';
