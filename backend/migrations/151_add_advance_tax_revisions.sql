-- Migration: Add advance_tax_revisions table for quarterly re-estimation workflow
-- Phase 4: Track revision history with variance analysis

-- Revisions table - tracks each quarterly revision
CREATE TABLE IF NOT EXISTS advance_tax_revisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id UUID NOT NULL REFERENCES advance_tax_assessments(id) ON DELETE CASCADE,

    -- Revision context
    revision_number INT NOT NULL DEFAULT 1,
    revision_quarter INT NOT NULL CHECK (revision_quarter BETWEEN 1 AND 4),
    revision_date DATE NOT NULL DEFAULT CURRENT_DATE,

    -- Snapshot of key values BEFORE revision
    previous_projected_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    previous_projected_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
    previous_taxable_income DECIMAL(18,2) NOT NULL DEFAULT 0,
    previous_total_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
    previous_net_tax_payable DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Snapshot of key values AFTER revision
    revised_projected_revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    revised_projected_expenses DECIMAL(18,2) NOT NULL DEFAULT 0,
    revised_taxable_income DECIMAL(18,2) NOT NULL DEFAULT 0,
    revised_total_tax_liability DECIMAL(18,2) NOT NULL DEFAULT 0,
    revised_net_tax_payable DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Variance analysis
    revenue_variance DECIMAL(18,2) GENERATED ALWAYS AS (revised_projected_revenue - previous_projected_revenue) STORED,
    expense_variance DECIMAL(18,2) GENERATED ALWAYS AS (revised_projected_expenses - previous_projected_expenses) STORED,
    taxable_income_variance DECIMAL(18,2) GENERATED ALWAYS AS (revised_taxable_income - previous_taxable_income) STORED,
    tax_liability_variance DECIMAL(18,2) GENERATED ALWAYS AS (revised_total_tax_liability - previous_total_tax_liability) STORED,
    net_payable_variance DECIMAL(18,2) GENERATED ALWAYS AS (revised_net_tax_payable - previous_net_tax_payable) STORED,

    -- Reason and notes
    revision_reason TEXT,
    notes TEXT,

    -- Audit
    revised_by UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_atr_assessment ON advance_tax_revisions(assessment_id);
CREATE INDEX idx_atr_quarter ON advance_tax_revisions(assessment_id, revision_quarter);
CREATE INDEX idx_atr_date ON advance_tax_revisions(revision_date);

-- Add revision tracking columns to assessments
ALTER TABLE advance_tax_assessments
    ADD COLUMN IF NOT EXISTS revision_count INT NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS last_revision_date DATE,
    ADD COLUMN IF NOT EXISTS last_revision_quarter INT;

-- Comment
COMMENT ON TABLE advance_tax_revisions IS 'Tracks quarterly revisions to advance tax assessments with variance analysis';
COMMENT ON COLUMN advance_tax_revisions.revision_quarter IS 'Quarter when revision was made (1=Q1 after Jun 15, 2=Q2 after Sep 15, etc.)';
COMMENT ON COLUMN advance_tax_revisions.revision_reason IS 'Reason for revision: actuals_update, projection_change, tax_planning, etc.';
