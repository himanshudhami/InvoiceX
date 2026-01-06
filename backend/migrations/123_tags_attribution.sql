-- Migration: 123_tags_attribution.sql
-- Description: Modern tagging system - flexible replacement for Tally's Cost Centers
-- Features: Hierarchical tags, multi-tag transactions, auto-attribution rules

-- ============================================================================
-- TAGS TABLE
-- Flexible labeling system with hierarchy support
-- ============================================================================

CREATE TABLE IF NOT EXISTS tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Basic Info
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50),
    tag_group VARCHAR(50) NOT NULL DEFAULT 'custom',  -- department, project, client, region, cost_center, custom
    description TEXT,

    -- Hierarchy
    parent_tag_id UUID REFERENCES tags(id) ON DELETE SET NULL,
    full_path VARCHAR(500),  -- Auto-computed: "Engineering / Frontend / React"
    level INT NOT NULL DEFAULT 0,

    -- UI/Display
    color VARCHAR(7),  -- Hex color code
    icon VARCHAR(50),
    sort_order INT NOT NULL DEFAULT 0,

    -- Budgeting (Optional)
    budget_amount DECIMAL(18,2),
    budget_period VARCHAR(20),  -- annual, quarterly, monthly
    budget_year VARCHAR(10),    -- 2024-25

    -- Tally Migration
    tally_cost_center_guid VARCHAR(100),
    tally_cost_center_name VARCHAR(200),

    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_tags_company_name_parent UNIQUE (company_id, name, parent_tag_id),
    CONSTRAINT chk_tags_group CHECK (tag_group IN ('department', 'project', 'client', 'region', 'cost_center', 'custom'))
);

-- Indexes for tags
CREATE INDEX idx_tags_company ON tags(company_id);
-- Partial unique index for code (only when code is not null)
CREATE UNIQUE INDEX uq_tags_company_code ON tags(company_id, code) WHERE code IS NOT NULL;
CREATE INDEX idx_tags_parent ON tags(parent_tag_id);
CREATE INDEX idx_tags_group ON tags(company_id, tag_group);
CREATE INDEX idx_tags_active ON tags(company_id, is_active);
CREATE INDEX idx_tags_tally_guid ON tags(tally_cost_center_guid) WHERE tally_cost_center_guid IS NOT NULL;

-- ============================================================================
-- ATTRIBUTION RULES TABLE
-- Auto-tagging rules based on conditions
-- ============================================================================

CREATE TABLE IF NOT EXISTS attribution_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Basic Info
    name VARCHAR(200) NOT NULL,
    description TEXT,

    -- Rule Definition
    rule_type VARCHAR(50) NOT NULL,  -- vendor, customer, account, product, keyword, amount_range, employee, composite
    applies_to JSONB NOT NULL DEFAULT '["*"]',  -- Transaction types this rule applies to
    conditions JSONB NOT NULL DEFAULT '{}',  -- Matching conditions

    -- Tag Assignment
    tag_assignments JSONB NOT NULL DEFAULT '[]',  -- Tags to apply with allocation config
    allocation_method VARCHAR(50) NOT NULL DEFAULT 'single',  -- single, split_equal, split_percentage, split_by_metric
    split_metric VARCHAR(50),  -- headcount, revenue, sqft, custom

    -- Execution Control
    priority INT NOT NULL DEFAULT 100,  -- Lower = higher priority
    stop_on_match BOOLEAN NOT NULL DEFAULT true,
    overwrite_existing BOOLEAN NOT NULL DEFAULT false,

    -- Scope
    effective_from DATE,
    effective_to DATE,

    -- Statistics
    times_applied INT NOT NULL DEFAULT 0,
    last_applied_at TIMESTAMP WITH TIME ZONE,
    total_amount_tagged DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Status
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    updated_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT chk_attribution_rule_type CHECK (rule_type IN (
        'vendor', 'customer', 'account', 'product', 'keyword',
        'amount_range', 'employee', 'composite'
    )),
    CONSTRAINT chk_attribution_allocation CHECK (allocation_method IN (
        'single', 'split_equal', 'split_percentage', 'split_by_metric'
    ))
);

-- Indexes for attribution_rules
CREATE INDEX idx_attribution_rules_company ON attribution_rules(company_id);
CREATE INDEX idx_attribution_rules_active ON attribution_rules(company_id, is_active);
CREATE INDEX idx_attribution_rules_type ON attribution_rules(company_id, rule_type);
CREATE INDEX idx_attribution_rules_priority ON attribution_rules(company_id, priority);

-- ============================================================================
-- TRANSACTION TAGS TABLE
-- Many-to-many: Any transaction can have multiple tags
-- ============================================================================

CREATE TABLE IF NOT EXISTS transaction_tags (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Transaction Reference (polymorphic)
    transaction_id UUID NOT NULL,
    transaction_type VARCHAR(50) NOT NULL,  -- invoice, vendor_invoice, payment, vendor_payment, expense_claim, journal_entry, journal_line, bank_transaction, salary_transaction, asset, subscription

    -- Tag Reference
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    -- Allocation
    allocated_amount DECIMAL(18,2),  -- NULL = full amount
    allocation_percentage DECIMAL(5,2),  -- Alternative to amount
    allocation_method VARCHAR(20) NOT NULL DEFAULT 'full',  -- full, amount, percentage, split_equal

    -- Source
    source VARCHAR(20) NOT NULL DEFAULT 'manual',  -- manual, rule, ai_suggested, imported
    attribution_rule_id UUID REFERENCES attribution_rules(id) ON DELETE SET NULL,
    confidence_score INT,  -- 0-100 for AI suggestions

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_transaction_tags UNIQUE (transaction_id, transaction_type, tag_id),
    CONSTRAINT chk_transaction_type CHECK (transaction_type IN (
        'invoice', 'vendor_invoice', 'payment', 'vendor_payment',
        'expense_claim', 'journal_entry', 'journal_line', 'bank_transaction',
        'salary_transaction', 'asset', 'subscription', 'contractor_payment'
    )),
    CONSTRAINT chk_allocation_method CHECK (allocation_method IN ('full', 'amount', 'percentage', 'split_equal')),
    CONSTRAINT chk_source CHECK (source IN ('manual', 'rule', 'ai_suggested', 'imported')),
    CONSTRAINT chk_allocation_percentage CHECK (allocation_percentage IS NULL OR (allocation_percentage >= 0 AND allocation_percentage <= 100))
);

-- Indexes for transaction_tags
CREATE INDEX idx_transaction_tags_transaction ON transaction_tags(transaction_id, transaction_type);
CREATE INDEX idx_transaction_tags_tag ON transaction_tags(tag_id);
CREATE INDEX idx_transaction_tags_rule ON transaction_tags(attribution_rule_id) WHERE attribution_rule_id IS NOT NULL;
CREATE INDEX idx_transaction_tags_source ON transaction_tags(source);

-- ============================================================================
-- TAG METRICS TABLE (for split_by_metric allocations)
-- Stores metrics like headcount, sqft per tag for proportional splits
-- ============================================================================

CREATE TABLE IF NOT EXISTS tag_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    tag_id UUID NOT NULL REFERENCES tags(id) ON DELETE CASCADE,

    -- Metric Info
    metric_name VARCHAR(50) NOT NULL,  -- headcount, sqft, revenue, custom
    metric_value DECIMAL(18,4) NOT NULL,

    -- Time Period
    effective_from DATE NOT NULL,
    effective_to DATE,

    -- Status
    is_current BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_tag_metrics_current UNIQUE (tag_id, metric_name, effective_from)
);

-- Indexes for tag_metrics
CREATE INDEX idx_tag_metrics_tag ON tag_metrics(tag_id);
CREATE INDEX idx_tag_metrics_current ON tag_metrics(tag_id, metric_name, is_current) WHERE is_current = true;

-- ============================================================================
-- FUNCTIONS
-- ============================================================================

-- Function to update tag full_path when hierarchy changes
CREATE OR REPLACE FUNCTION update_tag_full_path()
RETURNS TRIGGER AS $$
DECLARE
    parent_path VARCHAR(500);
    parent_level INT;
BEGIN
    IF NEW.parent_tag_id IS NULL THEN
        NEW.full_path := NEW.name;
        NEW.level := 0;
    ELSE
        SELECT full_path, level INTO parent_path, parent_level
        FROM tags WHERE id = NEW.parent_tag_id;

        NEW.full_path := COALESCE(parent_path, '') || ' / ' || NEW.name;
        NEW.level := COALESCE(parent_level, 0) + 1;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to auto-update full_path
CREATE TRIGGER trg_tags_update_path
    BEFORE INSERT OR UPDATE OF name, parent_tag_id ON tags
    FOR EACH ROW
    EXECUTE FUNCTION update_tag_full_path();

-- Function to update timestamps
CREATE OR REPLACE FUNCTION update_tags_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for updated_at
CREATE TRIGGER trg_tags_updated_at
    BEFORE UPDATE ON tags
    FOR EACH ROW
    EXECUTE FUNCTION update_tags_updated_at();

CREATE TRIGGER trg_attribution_rules_updated_at
    BEFORE UPDATE ON attribution_rules
    FOR EACH ROW
    EXECUTE FUNCTION update_tags_updated_at();

-- ============================================================================
-- SEED DATA: Default Tag Groups
-- ============================================================================

-- This will be company-specific, but we can create a function to seed defaults
CREATE OR REPLACE FUNCTION seed_default_tags(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS void AS $$
BEGIN
    -- Default Departments
    INSERT INTO tags (company_id, name, tag_group, color, sort_order, created_by)
    VALUES
        (p_company_id, 'Engineering', 'department', '#3B82F6', 1, p_created_by),
        (p_company_id, 'Sales', 'department', '#10B981', 2, p_created_by),
        (p_company_id, 'Marketing', 'department', '#F59E0B', 3, p_created_by),
        (p_company_id, 'Operations', 'department', '#6366F1', 4, p_created_by),
        (p_company_id, 'Finance', 'department', '#EC4899', 5, p_created_by),
        (p_company_id, 'HR', 'department', '#8B5CF6', 6, p_created_by),
        (p_company_id, 'Admin', 'department', '#6B7280', 7, p_created_by)
    ON CONFLICT (company_id, name, parent_tag_id) DO NOTHING;

    -- Default Regions (India)
    INSERT INTO tags (company_id, name, tag_group, color, sort_order, created_by)
    VALUES
        (p_company_id, 'North', 'region', '#EF4444', 1, p_created_by),
        (p_company_id, 'South', 'region', '#22C55E', 2, p_created_by),
        (p_company_id, 'East', 'region', '#3B82F6', 3, p_created_by),
        (p_company_id, 'West', 'region', '#F59E0B', 4, p_created_by)
    ON CONFLICT (company_id, name, parent_tag_id) DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- VIEWS
-- ============================================================================

-- View: Tag summary with transaction counts and amounts
CREATE OR REPLACE VIEW v_tag_summary AS
SELECT
    t.id,
    t.company_id,
    t.name,
    t.tag_group,
    t.full_path,
    t.color,
    t.is_active,
    t.budget_amount,
    COUNT(DISTINCT tt.id) as transaction_count,
    COALESCE(SUM(tt.allocated_amount), 0) as total_allocated_amount,
    MAX(tt.created_at) as last_used_at
FROM tags t
LEFT JOIN transaction_tags tt ON t.id = tt.tag_id
GROUP BY t.id, t.company_id, t.name, t.tag_group, t.full_path, t.color, t.is_active, t.budget_amount;

-- View: Attribution rule performance
CREATE OR REPLACE VIEW v_attribution_rule_performance AS
SELECT
    ar.id,
    ar.company_id,
    ar.name,
    ar.rule_type,
    ar.priority,
    ar.is_active,
    ar.times_applied,
    ar.total_amount_tagged,
    ar.last_applied_at,
    COUNT(DISTINCT tt.id) as current_tags_count
FROM attribution_rules ar
LEFT JOIN transaction_tags tt ON ar.id = tt.attribution_rule_id
GROUP BY ar.id, ar.company_id, ar.name, ar.rule_type, ar.priority, ar.is_active,
         ar.times_applied, ar.total_amount_tagged, ar.last_applied_at;

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON TABLE tags IS 'Flexible tagging system - modern replacement for Tally Cost Centers. Supports hierarchy, budgets, and multi-dimensional analysis.';
COMMENT ON TABLE attribution_rules IS 'Auto-tagging rules. When transactions match conditions, tags are automatically applied.';
COMMENT ON TABLE transaction_tags IS 'Many-to-many relationship between any transaction type and tags. Supports split allocations.';
COMMENT ON TABLE tag_metrics IS 'Metrics per tag for proportional allocation (headcount, sqft, etc.)';

COMMENT ON COLUMN tags.tag_group IS 'Categorization: department, project, client, region, cost_center, custom';
COMMENT ON COLUMN attribution_rules.conditions IS 'JSONB conditions for matching. Structure varies by rule_type.';
COMMENT ON COLUMN transaction_tags.source IS 'How tag was applied: manual, rule (auto), ai_suggested, imported (from Tally)';
