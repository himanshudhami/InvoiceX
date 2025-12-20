-- Migration: 066_create_expense_tables
-- Description: Creates expense management tables (categories, claims, attachments)
-- Date: 2025-12-20

-- ==================== Expense Categories ====================
-- Admin-definable expense categories (Travel, Food, Office Supplies, etc.)
CREATE TABLE IF NOT EXISTS expense_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    max_amount DECIMAL(15, 2),
    requires_receipt BOOLEAN NOT NULL DEFAULT TRUE,
    requires_approval BOOLEAN NOT NULL DEFAULT TRUE,
    gl_account_code VARCHAR(50),
    display_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_expense_category_code UNIQUE(company_id, code)
);

-- Index for company categories lookup
CREATE INDEX idx_expense_categories_company ON expense_categories(company_id);

-- Index for active categories
CREATE INDEX idx_expense_categories_active ON expense_categories(company_id, is_active)
    WHERE is_active = TRUE;

COMMENT ON TABLE expense_categories IS 'Admin-definable expense categories for reimbursement claims';
COMMENT ON COLUMN expense_categories.code IS 'Unique code per company (e.g., travel, food, office_supplies)';
COMMENT ON COLUMN expense_categories.max_amount IS 'Maximum claimable amount per expense (NULL = no limit)';
COMMENT ON COLUMN expense_categories.requires_receipt IS 'Whether receipt/invoice is mandatory';
COMMENT ON COLUMN expense_categories.requires_approval IS 'Whether manager approval is required';
COMMENT ON COLUMN expense_categories.gl_account_code IS 'General ledger account code for accounting integration';

-- ==================== Expense Claims ====================
-- Employee expense reimbursement claims
CREATE TABLE IF NOT EXISTS expense_claims (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,
    claim_number VARCHAR(50) NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    category_id UUID NOT NULL REFERENCES expense_categories(id) ON DELETE RESTRICT,
    expense_date DATE NOT NULL,
    amount DECIMAL(15, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'INR',

    -- Status workflow
    status VARCHAR(30) NOT NULL DEFAULT 'draft',

    -- Approval workflow integration
    approval_request_id UUID REFERENCES approval_requests(id) ON DELETE SET NULL,

    -- Submission tracking
    submitted_at TIMESTAMP,

    -- Approval/rejection tracking
    approved_at TIMESTAMP,
    approved_by UUID REFERENCES employees(id) ON DELETE SET NULL,
    rejected_at TIMESTAMP,
    rejected_by UUID REFERENCES employees(id) ON DELETE SET NULL,
    rejection_reason TEXT,

    -- Reimbursement tracking
    reimbursed_at TIMESTAMP,
    reimbursement_reference VARCHAR(100),
    reimbursement_notes TEXT,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_expense_status CHECK (
        status IN ('draft', 'submitted', 'pending_approval', 'approved', 'rejected', 'reimbursed', 'cancelled')
    ),
    CONSTRAINT chk_expense_amount CHECK (amount > 0),
    CONSTRAINT uq_expense_claim_number UNIQUE(company_id, claim_number)
);

-- Index for employee expense lookup
CREATE INDEX idx_expense_claims_employee ON expense_claims(employee_id);

-- Index for company + status queries
CREATE INDEX idx_expense_claims_company_status ON expense_claims(company_id, status);

-- Index for date range queries
CREATE INDEX idx_expense_claims_date ON expense_claims(company_id, expense_date);

-- Index for approval pending claims
CREATE INDEX idx_expense_claims_pending ON expense_claims(company_id, status)
    WHERE status IN ('submitted', 'pending_approval');

-- Index for category reporting
CREATE INDEX idx_expense_claims_category ON expense_claims(category_id);

COMMENT ON TABLE expense_claims IS 'Employee expense reimbursement claims with approval workflow';
COMMENT ON COLUMN expense_claims.claim_number IS 'Auto-generated claim number (e.g., EXP-2025-0001)';
COMMENT ON COLUMN expense_claims.status IS 'Claim workflow status: draft -> submitted -> pending_approval -> approved/rejected -> reimbursed';
COMMENT ON COLUMN expense_claims.approval_request_id IS 'Link to approval workflow when submitted';
COMMENT ON COLUMN expense_claims.reimbursement_reference IS 'Payment reference (bank transfer ID, cheque number, etc.)';

-- ==================== Expense Attachments ====================
-- Receipt/invoice attachments for expense claims
CREATE TABLE IF NOT EXISTS expense_attachments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    expense_id UUID NOT NULL REFERENCES expense_claims(id) ON DELETE CASCADE,
    file_storage_id UUID NOT NULL REFERENCES file_storage(id) ON DELETE RESTRICT,
    description TEXT,
    is_primary BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Index for expense attachments lookup
CREATE INDEX idx_expense_attachments_expense ON expense_attachments(expense_id);

-- Index for file storage lookup
CREATE INDEX idx_expense_attachments_file ON expense_attachments(file_storage_id);

COMMENT ON TABLE expense_attachments IS 'Receipt and invoice attachments for expense claims';
COMMENT ON COLUMN expense_attachments.file_storage_id IS 'Reference to secure file storage';
COMMENT ON COLUMN expense_attachments.is_primary IS 'Primary receipt/invoice for the expense';

-- ==================== Claim Number Sequence ====================
-- Function to generate expense claim numbers
CREATE OR REPLACE FUNCTION generate_expense_claim_number(p_company_id UUID)
RETURNS VARCHAR(50) AS $$
DECLARE
    v_year TEXT;
    v_next_number INT;
    v_claim_number VARCHAR(50);
BEGIN
    v_year := TO_CHAR(CURRENT_DATE, 'YYYY');

    -- Get the next sequence number for this company and year
    SELECT COALESCE(MAX(
        CAST(SUBSTRING(claim_number FROM 'EXP-' || v_year || '-(\d+)') AS INT)
    ), 0) + 1 INTO v_next_number
    FROM expense_claims
    WHERE company_id = p_company_id
      AND claim_number LIKE 'EXP-' || v_year || '-%';

    v_claim_number := 'EXP-' || v_year || '-' || LPAD(v_next_number::TEXT, 6, '0');

    RETURN v_claim_number;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION generate_expense_claim_number IS 'Generates sequential expense claim numbers per company per year';

-- ==================== Seed Default Expense Categories ====================
CREATE OR REPLACE FUNCTION seed_default_expense_categories(p_company_id UUID)
RETURNS void AS $$
BEGIN
    INSERT INTO expense_categories (company_id, name, code, description, max_amount, requires_receipt, display_order)
    VALUES
        (p_company_id, 'Travel', 'travel', 'Business travel expenses (flights, trains, taxis)', NULL, TRUE, 1),
        (p_company_id, 'Accommodation', 'accommodation', 'Hotel and lodging expenses', NULL, TRUE, 2),
        (p_company_id, 'Meals', 'meals', 'Business meals and entertainment', 5000, TRUE, 3),
        (p_company_id, 'Fuel/Transport', 'fuel', 'Fuel and local transport expenses', NULL, TRUE, 4),
        (p_company_id, 'Office Supplies', 'office_supplies', 'Stationery and office consumables', 2000, TRUE, 5),
        (p_company_id, 'Communication', 'communication', 'Phone, internet, and communication expenses', 1000, TRUE, 6),
        (p_company_id, 'Training', 'training', 'Training and certification expenses', NULL, TRUE, 7),
        (p_company_id, 'Equipment', 'equipment', 'Small equipment and tools', 10000, TRUE, 8),
        (p_company_id, 'Medical', 'medical', 'Medical reimbursements not covered by insurance', 5000, TRUE, 9),
        (p_company_id, 'Other', 'other', 'Miscellaneous expenses', 2000, TRUE, 99)
    ON CONFLICT (company_id, code) DO NOTHING;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION seed_default_expense_categories IS 'Seeds default expense categories for a company';

-- Seed expense categories for all existing companies
DO $$
DECLARE
    company_record RECORD;
BEGIN
    FOR company_record IN SELECT id FROM companies
    LOOP
        PERFORM seed_default_expense_categories(company_record.id);
    END LOOP;
END $$;
