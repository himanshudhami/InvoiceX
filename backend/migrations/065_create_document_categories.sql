-- Migration: 065_create_document_categories
-- Description: Creates document categories for custom HR document types and enhances employee_documents
-- Date: 2025-12-20

-- ==================== Document Categories ====================
-- Admin-definable document categories (replacing hardcoded document_type constraint)
CREATE TABLE IF NOT EXISTS document_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL,
    description TEXT,
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    requires_financial_year BOOLEAN NOT NULL DEFAULT FALSE,
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_doc_category_code UNIQUE(company_id, code)
);

-- Index for company categories lookup
CREATE INDEX idx_doc_categories_company ON document_categories(company_id);

-- Index for active categories
CREATE INDEX idx_doc_categories_active ON document_categories(company_id, is_active)
    WHERE is_active = TRUE;

COMMENT ON TABLE document_categories IS 'Admin-definable document categories for HR documents';
COMMENT ON COLUMN document_categories.code IS 'Unique code per company (e.g., offer_letter, form16)';
COMMENT ON COLUMN document_categories.is_system IS 'TRUE for system-defined categories that cannot be deleted';
COMMENT ON COLUMN document_categories.requires_financial_year IS 'TRUE for tax documents that need FY selection';
COMMENT ON COLUMN document_categories.display_order IS 'Order for display in dropdowns';

-- ==================== Enhance Employee Documents ====================
-- Add category and file_storage references

-- Add category reference (nullable initially for migration)
ALTER TABLE employee_documents
    ADD COLUMN IF NOT EXISTS category_id UUID REFERENCES document_categories(id) ON DELETE SET NULL;

-- Add file_storage reference for secure file handling
ALTER TABLE employee_documents
    ADD COLUMN IF NOT EXISTS file_storage_id UUID REFERENCES file_storage(id) ON DELETE SET NULL;

-- Create index for category lookup
CREATE INDEX IF NOT EXISTS idx_emp_docs_category ON employee_documents(category_id)
    WHERE category_id IS NOT NULL;

-- Create index for file_storage lookup
CREATE INDEX IF NOT EXISTS idx_emp_docs_file_storage ON employee_documents(file_storage_id)
    WHERE file_storage_id IS NOT NULL;

COMMENT ON COLUMN employee_documents.category_id IS 'Reference to document category (replaces document_type constraint)';
COMMENT ON COLUMN employee_documents.file_storage_id IS 'Reference to secure file storage for document content';

-- ==================== Seed Default Categories ====================
-- Note: These will be inserted per company when companies are created or via admin setup
-- The function below can be called to seed categories for a company

CREATE OR REPLACE FUNCTION seed_default_document_categories(p_company_id UUID)
RETURNS void AS $$
BEGIN
    -- Insert system categories if they don't exist
    INSERT INTO document_categories (company_id, name, code, description, is_system, requires_financial_year, display_order)
    VALUES
        (p_company_id, 'Offer Letter', 'offer_letter', 'Employment offer letter', TRUE, FALSE, 1),
        (p_company_id, 'Appointment Letter', 'appointment_letter', 'Official appointment letter', TRUE, FALSE, 2),
        (p_company_id, 'Form 16', 'form16', 'Annual TDS certificate', TRUE, TRUE, 3),
        (p_company_id, 'Form 12BB', 'form12bb', 'Investment declaration form', TRUE, TRUE, 4),
        (p_company_id, 'Salary Certificate', 'salary_certificate', 'Salary confirmation letter', TRUE, FALSE, 5),
        (p_company_id, 'Experience Certificate', 'experience_certificate', 'Work experience certificate', TRUE, FALSE, 6),
        (p_company_id, 'Relieving Letter', 'relieving_letter', 'Employment release letter', TRUE, FALSE, 7),
        (p_company_id, 'Company Policy', 'policy', 'Company policies and guidelines', TRUE, FALSE, 8),
        (p_company_id, 'Employee Handbook', 'handbook', 'Employee handbook and guidelines', TRUE, FALSE, 9),
        (p_company_id, 'NDA', 'nda', 'Non-disclosure agreement', TRUE, FALSE, 10),
        (p_company_id, 'Agreement', 'agreement', 'Employment or service agreement', TRUE, FALSE, 11),
        (p_company_id, 'Payslip', 'payslip', 'Monthly salary slip', TRUE, TRUE, 12),
        (p_company_id, 'Bonus Letter', 'bonus_letter', 'Annual bonus communication', TRUE, TRUE, 13),
        (p_company_id, 'Salary Revision Letter', 'salary_revision', 'Annual salary increment letter', TRUE, TRUE, 14),
        (p_company_id, 'Other', 'other', 'Other documents', TRUE, FALSE, 99)
    ON CONFLICT (company_id, code) DO NOTHING;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION seed_default_document_categories IS 'Seeds default document categories for a company';

-- Seed categories for all existing companies
DO $$
DECLARE
    company_record RECORD;
BEGIN
    FOR company_record IN SELECT id FROM companies
    LOOP
        PERFORM seed_default_document_categories(company_record.id);
    END LOOP;
END $$;

-- ==================== Migrate existing document_type to category_id ====================
-- Update employee_documents to link to the new categories based on document_type
UPDATE employee_documents ed
SET category_id = dc.id
FROM document_categories dc
WHERE dc.company_id = ed.company_id
  AND dc.code = ed.document_type
  AND ed.category_id IS NULL;
