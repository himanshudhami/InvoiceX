-- Migration: 055_create_employee_documents
-- Description: Creates employee documents repository
-- Date: 2025-12-17

-- ==================== Employee Documents ====================
-- Store employee-specific and company-wide documents
CREATE TABLE IF NOT EXISTS employee_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID REFERENCES employees(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    document_type VARCHAR(50) NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    file_url TEXT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_size INTEGER,
    mime_type VARCHAR(100),
    financial_year VARCHAR(10),
    is_company_wide BOOLEAN NOT NULL DEFAULT FALSE,
    uploaded_by UUID REFERENCES users(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_document_type CHECK (
        document_type IN (
            'offer_letter', 'appointment_letter', 'form16', 'form12bb',
            'salary_certificate', 'experience_certificate', 'relieving_letter',
            'policy', 'handbook', 'nda', 'agreement', 'payslip', 'other'
        )
    ),
    CONSTRAINT chk_employee_or_company_wide CHECK (
        (is_company_wide = TRUE AND employee_id IS NULL) OR
        (is_company_wide = FALSE AND employee_id IS NOT NULL)
    )
);

-- Index for employee's documents
CREATE INDEX idx_emp_docs_employee ON employee_documents(employee_id)
    WHERE employee_id IS NOT NULL;

-- Index for company-wide documents
CREATE INDEX idx_emp_docs_company_wide ON employee_documents(company_id)
    WHERE is_company_wide = TRUE;

-- Index for document type lookup
CREATE INDEX idx_emp_docs_type ON employee_documents(employee_id, document_type)
    WHERE employee_id IS NOT NULL;

-- Index for financial year documents (Form 16, etc.)
CREATE INDEX idx_emp_docs_fy ON employee_documents(employee_id, financial_year)
    WHERE financial_year IS NOT NULL;

COMMENT ON TABLE employee_documents IS 'Employee personal documents and company-wide policies';
COMMENT ON COLUMN employee_documents.employee_id IS 'NULL for company-wide documents';
COMMENT ON COLUMN employee_documents.document_type IS 'Document category type';
COMMENT ON COLUMN employee_documents.is_company_wide IS 'TRUE for policies/handbooks visible to all employees';
COMMENT ON COLUMN employee_documents.financial_year IS 'Applicable FY for tax documents (e.g., 2024-25)';

-- ==================== Document Requests ====================
-- Employee requests for document generation
CREATE TABLE IF NOT EXISTS document_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    document_type VARCHAR(50) NOT NULL,
    purpose TEXT,
    additional_info JSONB,
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    processed_by UUID REFERENCES users(id) ON DELETE SET NULL,
    processed_at TIMESTAMP,
    rejection_reason TEXT,
    document_id UUID REFERENCES employee_documents(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT chk_request_status CHECK (
        status IN ('pending', 'processing', 'completed', 'rejected')
    )
);

-- Index for employee's document requests
CREATE INDEX idx_doc_requests_employee ON document_requests(employee_id);

-- Index for pending requests
CREATE INDEX idx_doc_requests_pending ON document_requests(company_id, status)
    WHERE status = 'pending';

-- Index for processing requests
CREATE INDEX idx_doc_requests_processing ON document_requests(processed_by)
    WHERE status = 'processing';

COMMENT ON TABLE document_requests IS 'Employee requests for document generation';
COMMENT ON COLUMN document_requests.document_type IS 'Type of document requested';
COMMENT ON COLUMN document_requests.purpose IS 'Reason for requesting the document';
COMMENT ON COLUMN document_requests.additional_info IS 'Extra details needed for document generation';
COMMENT ON COLUMN document_requests.document_id IS 'Reference to generated document when completed';
