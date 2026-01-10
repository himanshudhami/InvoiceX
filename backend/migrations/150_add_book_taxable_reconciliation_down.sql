-- Rollback Phase 3: Book Profit to Taxable Income Reconciliation

ALTER TABLE advance_tax_assessments
    DROP COLUMN IF EXISTS book_profit,
    DROP COLUMN IF EXISTS add_book_depreciation,
    DROP COLUMN IF EXISTS add_disallowed_40a3,
    DROP COLUMN IF EXISTS add_disallowed_40a7,
    DROP COLUMN IF EXISTS add_disallowed_43b,
    DROP COLUMN IF EXISTS add_other_disallowances,
    DROP COLUMN IF EXISTS total_additions,
    DROP COLUMN IF EXISTS less_it_depreciation,
    DROP COLUMN IF EXISTS less_deductions_80c,
    DROP COLUMN IF EXISTS less_deductions_80d,
    DROP COLUMN IF EXISTS less_other_deductions,
    DROP COLUMN IF EXISTS total_deductions;
