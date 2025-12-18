-- 057_create_approval_workflow_tables.sql
-- Create tables for the generic approval workflow engine

-- Workflow Templates (configurable per company and activity type)
CREATE TABLE IF NOT EXISTS approval_workflow_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    activity_type VARCHAR(50) NOT NULL,  -- 'leave', 'asset_request', 'expense', 'travel', etc.
    name VARCHAR(200) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,  -- default template for this activity type in this company
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Ensure only one default template per company per activity type
CREATE UNIQUE INDEX IF NOT EXISTS idx_approval_templates_default
ON approval_workflow_templates (company_id, activity_type)
WHERE is_default = TRUE;

CREATE INDEX IF NOT EXISTS idx_approval_templates_company ON approval_workflow_templates(company_id);
CREATE INDEX IF NOT EXISTS idx_approval_templates_activity ON approval_workflow_templates(company_id, activity_type);

-- Workflow Steps (ordered steps within a template)
CREATE TABLE IF NOT EXISTS approval_workflow_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    template_id UUID NOT NULL REFERENCES approval_workflow_templates(id) ON DELETE CASCADE,
    step_order INTEGER NOT NULL,
    name VARCHAR(100) NOT NULL,
    approver_type VARCHAR(50) NOT NULL,  -- 'direct_manager', 'skip_level_manager', 'role', 'specific_user', 'department_head'
    approver_role VARCHAR(50),           -- if approver_type='role': 'HR', 'Finance', 'Admin', etc.
    approver_user_id UUID REFERENCES employees(id) ON DELETE SET NULL,  -- if approver_type='specific_user'
    is_required BOOLEAN NOT NULL DEFAULT TRUE,
    can_skip BOOLEAN NOT NULL DEFAULT FALSE,
    auto_approve_after_days INTEGER,     -- optional: auto-approve if no action after N days
    conditions_json JSONB,               -- optional: conditions like {"min_amount": 10000, "min_days": 5}
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_template_step_order UNIQUE(template_id, step_order),
    CONSTRAINT chk_approver_type CHECK (approver_type IN ('direct_manager', 'skip_level_manager', 'role', 'specific_user', 'department_head'))
);

CREATE INDEX IF NOT EXISTS idx_workflow_steps_template ON approval_workflow_steps(template_id);

-- Approval Requests (instances for each submitted activity)
CREATE TABLE IF NOT EXISTS approval_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    template_id UUID NOT NULL REFERENCES approval_workflow_templates(id),
    activity_type VARCHAR(50) NOT NULL,
    activity_id UUID NOT NULL,           -- FK to the actual entity (leave_applications.id, asset_requests.id, etc.)
    requestor_id UUID NOT NULL REFERENCES employees(id),
    current_step INTEGER NOT NULL DEFAULT 1,
    status VARCHAR(30) NOT NULL DEFAULT 'in_progress',  -- 'in_progress', 'approved', 'rejected', 'cancelled'
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITHOUT TIME ZONE,
    CONSTRAINT chk_request_status CHECK (status IN ('in_progress', 'approved', 'rejected', 'cancelled'))
);

CREATE INDEX IF NOT EXISTS idx_approval_requests_activity ON approval_requests(activity_type, activity_id);
CREATE INDEX IF NOT EXISTS idx_approval_requests_status ON approval_requests(status) WHERE status = 'in_progress';
CREATE INDEX IF NOT EXISTS idx_approval_requests_requestor ON approval_requests(requestor_id);
CREATE INDEX IF NOT EXISTS idx_approval_requests_company ON approval_requests(company_id);

-- Approval Request Steps (tracks each step's progress)
CREATE TABLE IF NOT EXISTS approval_request_steps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID NOT NULL REFERENCES approval_requests(id) ON DELETE CASCADE,
    step_order INTEGER NOT NULL,
    step_name VARCHAR(100) NOT NULL,
    approver_type VARCHAR(50) NOT NULL,
    assigned_to_id UUID REFERENCES employees(id) ON DELETE SET NULL,  -- resolved approver
    status VARCHAR(30) NOT NULL DEFAULT 'pending',  -- 'pending', 'approved', 'rejected', 'skipped', 'auto_approved'
    action_by_id UUID REFERENCES employees(id) ON DELETE SET NULL,  -- who took action (may differ from assigned if escalated)
    action_at TIMESTAMP WITHOUT TIME ZONE,
    comments TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT chk_step_status CHECK (status IN ('pending', 'approved', 'rejected', 'skipped', 'auto_approved'))
);

CREATE INDEX IF NOT EXISTS idx_approval_request_steps_request ON approval_request_steps(request_id);
CREATE INDEX IF NOT EXISTS idx_approval_request_steps_pending ON approval_request_steps(assigned_to_id, status)
    WHERE status = 'pending';
CREATE INDEX IF NOT EXISTS idx_approval_request_steps_assigned ON approval_request_steps(assigned_to_id);

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_approval_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for updated_at
DROP TRIGGER IF EXISTS trg_approval_templates_updated ON approval_workflow_templates;
CREATE TRIGGER trg_approval_templates_updated
    BEFORE UPDATE ON approval_workflow_templates
    FOR EACH ROW
    EXECUTE FUNCTION update_approval_updated_at();

DROP TRIGGER IF EXISTS trg_workflow_steps_updated ON approval_workflow_steps;
CREATE TRIGGER trg_workflow_steps_updated
    BEFORE UPDATE ON approval_workflow_steps
    FOR EACH ROW
    EXECUTE FUNCTION update_approval_updated_at();

DROP TRIGGER IF EXISTS trg_approval_requests_updated ON approval_requests;
CREATE TRIGGER trg_approval_requests_updated
    BEFORE UPDATE ON approval_requests
    FOR EACH ROW
    EXECUTE FUNCTION update_approval_updated_at();

-- Comments for documentation
COMMENT ON TABLE approval_workflow_templates IS 'Configurable approval workflow templates per company and activity type';
COMMENT ON TABLE approval_workflow_steps IS 'Ordered steps within an approval workflow template';
COMMENT ON TABLE approval_requests IS 'Active approval request instances for submitted activities';
COMMENT ON TABLE approval_request_steps IS 'Progress tracking for each step in an approval request';

COMMENT ON COLUMN approval_workflow_templates.activity_type IS 'Type of activity: leave, asset_request, expense, travel, etc.';
COMMENT ON COLUMN approval_workflow_templates.is_default IS 'If true, this is the default template for this activity type in this company';
COMMENT ON COLUMN approval_workflow_steps.approver_type IS 'How to resolve the approver: direct_manager, skip_level_manager, role, specific_user, department_head';
COMMENT ON COLUMN approval_workflow_steps.conditions_json IS 'JSON conditions for conditional steps, e.g. {"min_days": 5, "leave_type": "sick"}';
COMMENT ON COLUMN approval_requests.activity_id IS 'Reference to the actual entity being approved (e.g., leave_applications.id)';
COMMENT ON COLUMN approval_request_steps.action_by_id IS 'The employee who took action - may differ from assigned_to_id if escalated or delegated';
