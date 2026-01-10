-- Rollback Phase 2: YTD vs Projection Split

ALTER TABLE advance_tax_assessments
    DROP COLUMN IF EXISTS ytd_revenue,
    DROP COLUMN IF EXISTS ytd_expenses,
    DROP COLUMN IF EXISTS ytd_through_date,
    DROP COLUMN IF EXISTS projected_additional_revenue,
    DROP COLUMN IF EXISTS projected_additional_expenses;
