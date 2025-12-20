-- File Storage and Audit Log tables for Document Management System
-- Migration: 064_create_file_storage.sql

-- ============================================================================
-- FILE STORAGE TABLE
-- Stores metadata about uploaded files with abstraction for multiple providers
-- ============================================================================
CREATE TABLE IF NOT EXISTS file_storage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    original_filename VARCHAR(255) NOT NULL,
    stored_filename VARCHAR(255) NOT NULL,
    storage_path TEXT NOT NULL,
    storage_provider VARCHAR(20) NOT NULL DEFAULT 'local',
    file_size BIGINT NOT NULL,
    mime_type VARCHAR(100) NOT NULL,
    checksum VARCHAR(64),
    uploaded_by UUID REFERENCES users(id),
    entity_type VARCHAR(50),  -- 'employee_document', 'expense', 'asset', etc.
    entity_id UUID,           -- Reference to the entity this file belongs to
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMP,
    deleted_by UUID REFERENCES users(id),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- Constraints
    CONSTRAINT chk_storage_provider CHECK (storage_provider IN ('local', 's3', 'azure')),
    CONSTRAINT chk_file_size CHECK (file_size > 0 AND file_size <= 26214400), -- Max 25 MB
    CONSTRAINT chk_mime_type CHECK (mime_type IN (
        'application/pdf',
        'image/png',
        'image/jpeg',
        'image/jpg',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    ))
);

-- Indexes for file_storage
CREATE INDEX idx_file_storage_company ON file_storage(company_id);
CREATE INDEX idx_file_storage_entity ON file_storage(entity_type, entity_id);
CREATE INDEX idx_file_storage_provider ON file_storage(storage_provider);
CREATE INDEX idx_file_storage_uploaded_by ON file_storage(uploaded_by);
CREATE INDEX idx_file_storage_created ON file_storage(created_at DESC);
CREATE INDEX idx_file_storage_not_deleted ON file_storage(company_id) WHERE is_deleted = FALSE;

-- ============================================================================
-- DOCUMENT AUDIT LOG TABLE
-- Tracks all document-related actions for compliance and security
-- ============================================================================
CREATE TABLE IF NOT EXISTS document_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    document_id UUID,                    -- Reference to employee_documents if applicable
    file_storage_id UUID REFERENCES file_storage(id),
    action VARCHAR(30) NOT NULL,
    actor_id UUID NOT NULL REFERENCES users(id),
    actor_ip VARCHAR(45),                -- IPv4 or IPv6
    user_agent TEXT,                     -- Browser/client info
    metadata JSONB,                      -- Additional context (file name, size, etc.)
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- Constraint for valid actions
    CONSTRAINT chk_audit_action CHECK (
        action IN ('upload', 'download', 'view', 'delete', 'update', 'share', 'restore')
    )
);

-- Indexes for document_audit_log
CREATE INDEX idx_doc_audit_company ON document_audit_log(company_id);
CREATE INDEX idx_doc_audit_document ON document_audit_log(document_id);
CREATE INDEX idx_doc_audit_file_storage ON document_audit_log(file_storage_id);
CREATE INDEX idx_doc_audit_actor ON document_audit_log(actor_id);
CREATE INDEX idx_doc_audit_action ON document_audit_log(company_id, action);
CREATE INDEX idx_doc_audit_created ON document_audit_log(created_at DESC);

-- ============================================================================
-- COMMENTS
-- ============================================================================
COMMENT ON TABLE file_storage IS 'Centralized file storage metadata with support for multiple storage providers (local, S3, Azure)';
COMMENT ON COLUMN file_storage.storage_provider IS 'Storage backend: local (disk), s3 (AWS S3), azure (Azure Blob)';
COMMENT ON COLUMN file_storage.storage_path IS 'Relative path within the storage provider (e.g., companyId/2024/01/uuid.pdf)';
COMMENT ON COLUMN file_storage.entity_type IS 'Type of entity this file is attached to (employee_document, expense, asset)';
COMMENT ON COLUMN file_storage.entity_id IS 'ID of the entity this file is attached to';
COMMENT ON COLUMN file_storage.checksum IS 'SHA256 hash for file integrity verification';

COMMENT ON TABLE document_audit_log IS 'Audit trail for all document operations for compliance and security';
COMMENT ON COLUMN document_audit_log.action IS 'Type of action: upload, download, view, delete, update, share, restore';
COMMENT ON COLUMN document_audit_log.metadata IS 'JSON object with additional context (original filename, file size, etc.)';
