-- Migration: Create audit history table for tax declarations
-- Purpose: Track all changes to tax declarations for compliance and audit

CREATE TABLE IF NOT EXISTS employee_tax_declaration_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    declaration_id UUID NOT NULL REFERENCES employee_tax_declarations(id) ON DELETE CASCADE,

    -- Action tracking
    action VARCHAR(20) NOT NULL CHECK (action IN ('created', 'updated', 'submitted', 'verified', 'rejected', 'locked', 'unlocked', 'revised')),
    changed_by VARCHAR(255) NOT NULL,
    changed_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Snapshot of values (JSONB for flexibility)
    previous_values JSONB,
    new_values JSONB,

    -- For rejection workflow
    rejection_reason TEXT,
    rejection_comments TEXT,

    -- Audit metadata
    ip_address VARCHAR(45),
    user_agent TEXT,

    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for efficient querying
CREATE INDEX idx_declaration_history_declaration ON employee_tax_declaration_history(declaration_id);
CREATE INDEX idx_declaration_history_action ON employee_tax_declaration_history(action);
CREATE INDEX idx_declaration_history_changed_at ON employee_tax_declaration_history(changed_at);
CREATE INDEX idx_declaration_history_changed_by ON employee_tax_declaration_history(changed_by);

-- Composite index for common queries (declaration + time-based)
CREATE INDEX idx_declaration_history_declaration_time ON employee_tax_declaration_history(declaration_id, changed_at DESC);

-- Add comments for documentation
COMMENT ON TABLE employee_tax_declaration_history IS 'Audit trail for employee tax declaration changes';
COMMENT ON COLUMN employee_tax_declaration_history.action IS 'Type of action: created, updated, submitted, verified, rejected, locked, unlocked, revised';
COMMENT ON COLUMN employee_tax_declaration_history.previous_values IS 'JSON snapshot of field values before the change';
COMMENT ON COLUMN employee_tax_declaration_history.new_values IS 'JSON snapshot of field values after the change';
COMMENT ON COLUMN employee_tax_declaration_history.rejection_reason IS 'Reason for rejection (when action=rejected)';
COMMENT ON COLUMN employee_tax_declaration_history.rejection_comments IS 'Additional comments from reviewer (when action=rejected)';
