-- Migration: 153_create_audit_trail.sql
-- Description: MCA-compliant generic audit trail for entity CRUD operations
-- Date: 2026-01-10

-- Generic MCA-compliant audit trail for entity changes
CREATE TABLE IF NOT EXISTS audit_trail (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Entity identification
    entity_type VARCHAR(100) NOT NULL,      -- 'invoice', 'payment', 'vendor', 'journal_entry', etc.
    entity_id UUID NOT NULL,
    entity_display_name VARCHAR(255),       -- Human-readable: "INV-2024-0001", "John Doe"

    -- Operation details
    operation VARCHAR(20) NOT NULL,         -- 'create', 'update', 'delete'

    -- Before/After values (JSON for flexibility)
    old_values JSONB,                       -- NULL for create operations
    new_values JSONB,                       -- NULL for delete operations
    changed_fields TEXT[],                  -- Array of field names that changed (for updates)

    -- Actor information (MCA requirement - who performed the action)
    actor_id UUID NOT NULL REFERENCES users(id),
    actor_name VARCHAR(255),                -- Denormalized for audit reports
    actor_email VARCHAR(255),               -- Denormalized for audit reports
    actor_ip VARCHAR(45),                   -- IPv4 or IPv6 address
    user_agent TEXT,                        -- Browser/client information

    -- Request context (for traceability)
    correlation_id VARCHAR(100),            -- Links to request logs via X-Correlation-Id
    request_path VARCHAR(500),              -- API endpoint called
    request_method VARCHAR(10),             -- HTTP method: GET, POST, PUT, DELETE

    -- Timestamps (MCA requires precise timestamps)
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Integrity (for tamper detection - MCA compliance)
    checksum VARCHAR(64),                   -- SHA256 of key fields for integrity verification

    -- Constraint for valid operations
    CONSTRAINT chk_audit_operation CHECK (operation IN ('create', 'update', 'delete'))
);

-- Indexes for common query patterns
CREATE INDEX idx_audit_trail_company ON audit_trail(company_id);
CREATE INDEX idx_audit_trail_entity ON audit_trail(entity_type, entity_id);
CREATE INDEX idx_audit_trail_actor ON audit_trail(actor_id);
CREATE INDEX idx_audit_trail_created ON audit_trail(created_at DESC);
CREATE INDEX idx_audit_trail_operation ON audit_trail(company_id, operation);
CREATE INDEX idx_audit_trail_correlation ON audit_trail(correlation_id) WHERE correlation_id IS NOT NULL;

-- Composite index for entity history queries (most common use case)
CREATE INDEX idx_audit_trail_entity_history ON audit_trail(entity_type, entity_id, created_at DESC);

-- Composite index for company + date range queries (compliance reports)
CREATE INDEX idx_audit_trail_company_date ON audit_trail(company_id, created_at DESC);

-- GIN index for changed_fields array search
CREATE INDEX idx_audit_trail_changed_fields ON audit_trail USING GIN (changed_fields) WHERE changed_fields IS NOT NULL;

-- Comments for documentation
COMMENT ON TABLE audit_trail IS 'MCA-compliant generic audit trail for entity CRUD operations with before/after values';
COMMENT ON COLUMN audit_trail.entity_type IS 'Type of entity: invoice, payment, vendor, vendor_invoice, journal_entry, customer, etc.';
COMMENT ON COLUMN audit_trail.old_values IS 'JSON snapshot of entity state before change (NULL for create operations)';
COMMENT ON COLUMN audit_trail.new_values IS 'JSON snapshot of entity state after change (NULL for delete operations)';
COMMENT ON COLUMN audit_trail.changed_fields IS 'Array of field names that were modified (for update operations only)';
COMMENT ON COLUMN audit_trail.checksum IS 'SHA256 hash of (entity_type + entity_id + operation + old_values + new_values) for tamper detection';
COMMENT ON COLUMN audit_trail.correlation_id IS 'Request correlation ID from X-Correlation-Id header for distributed tracing';
