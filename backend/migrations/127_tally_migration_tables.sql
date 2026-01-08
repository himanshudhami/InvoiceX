-- Migration: 127_tally_migration_tables.sql
-- Description: Create Tally migration tracking tables
-- Purpose: Track import batches, individual record logs, and user field mappings

-- ============================================================================
-- TALLY MIGRATION BATCHES
-- Tracks each import session with status, counts, and timestamps
-- ============================================================================

CREATE TABLE IF NOT EXISTS tally_migration_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Batch Identification
    batch_number VARCHAR(50) NOT NULL,
    import_type VARCHAR(20) NOT NULL DEFAULT 'full', -- 'full', 'incremental'

    -- Source File Info
    source_file_name VARCHAR(255),
    source_file_size BIGINT,
    source_format VARCHAR(10) NOT NULL DEFAULT 'xml', -- 'xml', 'json'
    source_checksum VARCHAR(64), -- SHA-256 for duplicate file detection

    -- Tally Company Info (extracted from file)
    tally_company_name VARCHAR(255),
    tally_company_guid VARCHAR(100),
    tally_from_date DATE,
    tally_to_date DATE,
    tally_financial_year VARCHAR(20),

    -- Status
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    -- Values: 'pending', 'uploading', 'parsing', 'validating', 'preview',
    --         'mapping', 'importing', 'posting', 'completed', 'failed', 'rolled_back', 'cancelled'

    -- Master Counts
    total_ledgers INT DEFAULT 0,
    imported_ledgers INT DEFAULT 0,
    skipped_ledgers INT DEFAULT 0,
    failed_ledgers INT DEFAULT 0,

    total_stock_items INT DEFAULT 0,
    imported_stock_items INT DEFAULT 0,
    skipped_stock_items INT DEFAULT 0,
    failed_stock_items INT DEFAULT 0,

    total_cost_centers INT DEFAULT 0,
    imported_cost_centers INT DEFAULT 0,
    skipped_cost_centers INT DEFAULT 0,
    failed_cost_centers INT DEFAULT 0,

    total_godowns INT DEFAULT 0,
    imported_godowns INT DEFAULT 0,

    total_units INT DEFAULT 0,
    imported_units INT DEFAULT 0,

    total_stock_groups INT DEFAULT 0,
    imported_stock_groups INT DEFAULT 0,

    -- Voucher Counts
    total_vouchers INT DEFAULT 0,
    imported_vouchers INT DEFAULT 0,
    skipped_vouchers INT DEFAULT 0,
    failed_vouchers INT DEFAULT 0,

    -- Voucher Type Breakdown (JSONB for flexibility)
    voucher_counts JSONB DEFAULT '{}',
    -- Example: {"sales": 234, "purchase": 189, "receipt": 78, "payment": 92, "journal": 45}

    -- Suspense Tracking
    suspense_entries_created INT DEFAULT 0,
    suspense_total_amount DECIMAL(18,2) DEFAULT 0,

    -- Timing
    upload_started_at TIMESTAMP WITH TIME ZONE,
    parsing_started_at TIMESTAMP WITH TIME ZONE,
    parsing_completed_at TIMESTAMP WITH TIME ZONE,
    validation_started_at TIMESTAMP WITH TIME ZONE,
    validation_completed_at TIMESTAMP WITH TIME ZONE,
    import_started_at TIMESTAMP WITH TIME ZONE,
    import_completed_at TIMESTAMP WITH TIME ZONE,

    -- Error Info
    error_message TEXT,
    error_details JSONB, -- Structured error info

    -- User Config (snapshot of mapping settings used)
    mapping_config JSONB DEFAULT '{}',

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT uq_tally_batch_number UNIQUE (company_id, batch_number),
    CONSTRAINT chk_import_type CHECK (import_type IN ('full', 'incremental')),
    CONSTRAINT chk_source_format CHECK (source_format IN ('xml', 'json')),
    CONSTRAINT chk_batch_status CHECK (status IN (
        'pending', 'uploading', 'parsing', 'validating', 'preview',
        'mapping', 'importing', 'posting', 'completed', 'failed', 'rolled_back', 'cancelled'
    ))
);

-- Indexes
CREATE INDEX idx_tally_batches_company ON tally_migration_batches(company_id);
CREATE INDEX idx_tally_batches_status ON tally_migration_batches(company_id, status);
CREATE INDEX idx_tally_batches_created ON tally_migration_batches(company_id, created_at DESC);

COMMENT ON TABLE tally_migration_batches IS 'Tracks Tally import batches with status, counts, and timing';

-- ============================================================================
-- TALLY MIGRATION LOGS
-- Detailed log of each imported/skipped/failed record
-- ============================================================================

CREATE TABLE IF NOT EXISTS tally_migration_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id UUID NOT NULL REFERENCES tally_migration_batches(id) ON DELETE CASCADE,

    -- Record Identification
    record_type VARCHAR(50) NOT NULL,
    -- Values: 'ledger', 'stock_item', 'stock_group', 'godown', 'unit', 'cost_center',
    --         'voucher_sales', 'voucher_purchase', 'voucher_receipt', 'voucher_payment',
    --         'voucher_journal', 'voucher_contra', 'voucher_credit_note', 'voucher_debit_note',
    --         'voucher_stock_journal', 'voucher_physical_stock', 'opening_balance'

    tally_guid VARCHAR(100),
    tally_name VARCHAR(255),
    tally_parent_name VARCHAR(255), -- For hierarchical items
    tally_date DATE, -- For vouchers

    -- Target Entity Info
    target_entity VARCHAR(50), -- 'customers', 'vendors', 'chart_of_accounts', 'invoices', etc.
    target_id UUID, -- ID of created/updated record

    -- Status
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    -- Values: 'pending', 'success', 'skipped', 'failed', 'mapped_to_suspense', 'duplicate'

    -- Skip/Fail Reason
    skip_reason VARCHAR(100),
    error_message TEXT,
    error_code VARCHAR(50),

    -- Validation Issues (non-blocking warnings)
    validation_warnings JSONB DEFAULT '[]',

    -- Raw Data (for debugging and re-processing)
    raw_data JSONB,

    -- Amounts (for reconciliation)
    tally_amount DECIMAL(18,2),
    imported_amount DECIMAL(18,2),
    amount_difference DECIMAL(18,2),

    -- Processing Info
    processing_order INT, -- Order within batch
    processing_duration_ms INT, -- Time taken to process this record

    -- Timestamps
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    -- Constraints
    CONSTRAINT chk_log_record_type CHECK (record_type IN (
        'ledger', 'stock_item', 'stock_group', 'godown', 'unit', 'cost_center',
        'voucher_sales', 'voucher_purchase', 'voucher_receipt', 'voucher_payment',
        'voucher_journal', 'voucher_contra', 'voucher_credit_note', 'voucher_debit_note',
        'voucher_stock_journal', 'voucher_physical_stock', 'opening_balance', 'cost_allocation'
    )),
    CONSTRAINT chk_log_status CHECK (status IN (
        'pending', 'success', 'skipped', 'failed', 'mapped_to_suspense', 'duplicate'
    ))
);

-- Indexes for efficient querying
CREATE INDEX idx_tally_logs_batch ON tally_migration_logs(batch_id);
CREATE INDEX idx_tally_logs_batch_type ON tally_migration_logs(batch_id, record_type);
CREATE INDEX idx_tally_logs_batch_status ON tally_migration_logs(batch_id, status);
CREATE INDEX idx_tally_logs_tally_guid ON tally_migration_logs(tally_guid) WHERE tally_guid IS NOT NULL;
CREATE INDEX idx_tally_logs_target ON tally_migration_logs(target_entity, target_id) WHERE target_id IS NOT NULL;

COMMENT ON TABLE tally_migration_logs IS 'Detailed log of each record processed during Tally import';

-- ============================================================================
-- TALLY FIELD MAPPINGS
-- User-configurable mapping overrides for ledger groups and specific ledgers
-- ============================================================================

CREATE TABLE IF NOT EXISTS tally_field_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Mapping Scope
    mapping_type VARCHAR(30) NOT NULL DEFAULT 'ledger_group',
    -- Values: 'ledger_group', 'ledger', 'stock_group', 'voucher_type', 'cost_category'

    -- Source (Tally)
    tally_group_name VARCHAR(255), -- For group-level mappings
    tally_name VARCHAR(255) NOT NULL DEFAULT '', -- For specific item mappings ('' = applies to whole group)

    -- Target (Our System)
    target_entity VARCHAR(50) NOT NULL,
    -- Values: 'vendors', 'customers', 'bank_accounts', 'chart_of_accounts', 'tags', 'stock_groups'

    target_account_id UUID REFERENCES chart_of_accounts(id),
    target_account_type VARCHAR(30), -- 'asset', 'liability', 'income', 'expense'
    target_account_subtype VARCHAR(50), -- More specific classification

    -- For ledger-to-COA mappings
    default_account_code VARCHAR(20),
    default_account_name VARCHAR(200),

    -- For cost center mappings
    target_tag_group VARCHAR(50), -- 'department', 'project', 'cost_center', etc.

    -- Priority (for overlapping rules)
    priority INT DEFAULT 100, -- Lower = higher priority

    -- Status
    is_active BOOLEAN DEFAULT true,
    is_system_default BOOLEAN DEFAULT false, -- System-provided vs user-created

    -- Audit
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    -- Constraints
    CONSTRAINT uq_tally_mapping UNIQUE (company_id, mapping_type, tally_group_name, tally_name),
    CONSTRAINT chk_mapping_type CHECK (mapping_type IN (
        'ledger_group', 'ledger', 'stock_group', 'voucher_type', 'cost_category'
    )),
    CONSTRAINT chk_target_entity CHECK (target_entity IN (
        'vendors', 'customers', 'bank_accounts', 'chart_of_accounts', 'tags',
        'stock_groups', 'suspense', 'skip'
    ))
);

-- Indexes
CREATE INDEX idx_tally_mappings_company ON tally_field_mappings(company_id);
CREATE INDEX idx_tally_mappings_active ON tally_field_mappings(company_id, is_active)
WHERE is_active = true;
CREATE INDEX idx_tally_mappings_type ON tally_field_mappings(company_id, mapping_type);

COMMENT ON TABLE tally_field_mappings IS 'User-configurable mapping rules for Tally data import';

-- ============================================================================
-- SEED DEFAULT MAPPINGS
-- Standard Tally ledger group to entity mappings
-- ============================================================================

-- Function to seed default mappings for a company
CREATE OR REPLACE FUNCTION seed_tally_default_mappings(p_company_id UUID)
RETURNS void AS $$
BEGIN
    -- Sundry Creditors -> Vendors
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Sundry Debtors -> Customers
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Bank Accounts -> Bank Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank Accounts', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Bank OD A/c -> Bank Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OD A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ========================================================================
    -- TDS-APPLICABLE VENDOR CATEGORIES
    -- These are party ledger groups requiring TDS deduction - import as vendors
    -- ========================================================================

    -- CONSULTANTS -> Vendors (TDS 194J @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONSULTANTS', '', 'vendors', 'tds_section', '["TDS:194J-Professional", "Vendor:Consultant"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Consultants', '', 'vendors', 'tds_section', '["TDS:194J-Professional", "Vendor:Consultant"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- CONTRACTORS -> Vendors (TDS 194C @ 1-2%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONTRACTORS', '', 'vendors', 'tds_section', '["TDS:194C-Contractor", "Vendor:Contractor"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Contractors', '', 'vendors', 'tds_section', '["TDS:194C-Contractor", "Vendor:Contractor"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- PROFESSIONAL FEES -> Vendors (TDS 194J @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PROFESSIONAL FEES', '', 'vendors', 'tds_section', '["TDS:194J-Professional", "Vendor:Consultant"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Professional Fees', '', 'vendors', 'tds_section', '["TDS:194J-Professional", "Vendor:Consultant"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- RENT PAYABLE -> Vendors (TDS 194I @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'RENT PAYABLE', '', 'vendors', 'tds_section', '["TDS:194I-Rent-Land", "Vendor:Landlord"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Rent Payable', '', 'vendors', 'tds_section', '["TDS:194I-Rent-Land", "Vendor:Landlord"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- COMMISSION PAYABLE -> Vendors (TDS 194H @ 5%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'COMMISSION PAYABLE', '', 'vendors', 'tds_section', '["TDS:194H-Commission", "Vendor:Service"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Commission Payable', '', 'vendors', 'tds_section', '["TDS:194H-Commission", "Vendor:Service"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- INTEREST PAYABLE -> Vendors (TDS 194A @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'INTEREST PAYABLE', '', 'vendors', 'tds_section', '["TDS:194A-Interest"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Interest Payable', '', 'vendors', 'tds_section', '["TDS:194A-Interest"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- BROKERAGE PAYABLE -> Vendors (TDS 194H @ 5%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'BROKERAGE PAYABLE', '', 'vendors', 'tds_section', '["TDS:194H-Commission", "Vendor:Service"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_tag_group, tag_assignments, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Brokerage Payable', '', 'vendors', 'tds_section', '["TDS:194H-Commission", "Vendor:Service"]', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- ========================================================================
    -- CHART OF ACCOUNTS MAPPINGS
    -- ========================================================================

    -- Cash-in-hand -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Cash-in-hand', '', 'chart_of_accounts', 'asset', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Purchase Accounts -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Purchase Accounts', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Sales Accounts -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sales Accounts', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Direct Expenses -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Indirect Expenses -> Chart of Accounts (Expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Direct Incomes -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Indirect Incomes -> Chart of Accounts (Income)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Duties & Taxes -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Duties & Taxes', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Fixed Assets -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Fixed Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Investments -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Investments', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Current Assets -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Current Liabilities -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Liabilities', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Loans (Liability) -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans (Liability)', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Loans & Advances (Asset) -> Chart of Accounts (Asset)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans & Advances (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Capital Account -> Chart of Accounts (Equity)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Capital Account', '', 'chart_of_accounts', 'equity', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Reserves & Surplus -> Chart of Accounts (Equity)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Reserves & Surplus', '', 'chart_of_accounts', 'equity', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Stock-in-hand -> Chart of Accounts (Asset) -- inventory account
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stock-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Provisions -> Chart of Accounts (Liability)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Provisions', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Suspense A/c -> Chart of Accounts (special suspense account)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Suspense A/c', '', 'suspense', NULL, true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

    -- Branch / Divisions -> Chart of Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Branch / Divisions', '', 'chart_of_accounts', 'asset', true, 30)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;

END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- SUSPENSE ACCOUNTS (Created per company during first import)
-- ============================================================================

-- Function to create suspense accounts for a company
CREATE OR REPLACE FUNCTION create_tally_suspense_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS void AS $$
BEGIN
    -- Asset Suspense (normal_balance = debit for assets)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, is_active, is_system_account, created_by)
    VALUES (p_company_id, '9990', 'Tally Import Suspense - Assets', 'asset', 'current_asset', 'debit', true, true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Liability Suspense (normal_balance = credit for liabilities)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, is_active, is_system_account, created_by)
    VALUES (p_company_id, '9991', 'Tally Import Suspense - Liabilities', 'liability', 'current_liability', 'credit', true, true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Income Suspense (normal_balance = credit for income)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, is_active, is_system_account, created_by)
    VALUES (p_company_id, '9992', 'Tally Import Suspense - Income', 'income', 'other_income', 'credit', true, true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Expense Suspense (normal_balance = debit for expenses)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, is_active, is_system_account, created_by)
    VALUES (p_company_id, '9993', 'Tally Import Suspense - Expenses', 'expense', 'other_expense', 'debit', true, true, p_created_by)
    ON CONFLICT DO NOTHING;

    -- Equity Suspense (normal_balance = credit for equity)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, is_active, is_system_account, created_by)
    VALUES (p_company_id, '9994', 'Tally Import Suspense - Equity', 'equity', 'retained_earnings', 'credit', true, true, p_created_by)
    ON CONFLICT DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- UPDATE TIMESTAMP TRIGGERS
-- ============================================================================

CREATE OR REPLACE FUNCTION update_tally_migration_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_tally_batches_updated_at
    BEFORE UPDATE ON tally_migration_batches
    FOR EACH ROW
    EXECUTE FUNCTION update_tally_migration_updated_at();

CREATE TRIGGER trg_tally_mappings_updated_at
    BEFORE UPDATE ON tally_field_mappings
    FOR EACH ROW
    EXECUTE FUNCTION update_tally_migration_updated_at();

-- ============================================================================
-- COMMENTS
-- ============================================================================

COMMENT ON FUNCTION seed_tally_default_mappings IS 'Seeds default Tally ledger group to entity mappings for a company';
COMMENT ON FUNCTION create_tally_suspense_accounts IS 'Creates suspense accounts for unmapped Tally data during import';
