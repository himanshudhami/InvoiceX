-- ============================================================================
-- Migration 131: CONSOLIDATED TALLY IMPORT AND MAPPING SCHEMA
-- ============================================================================
-- Purpose: Fresh database setup with complete Tally integration
-- Consolidates: 131, 133, 135, 136, 138, 140, 163, 165
-- Run order: After all core tables (chart_of_accounts, parties, journal_entries)
-- Dependencies: tally_migration_batches, tally_field_mappings, tally_migration_logs
-- ============================================================================

-- ============================================================================
-- SECTION 1: TALLY MIGRATION INFRASTRUCTURE
-- (Assumes tally_migration_batches and related tables exist from core schema)
-- ============================================================================

-- Add missing tracking columns to existing tally_migration_batches
ALTER TABLE tally_migration_batches
ADD COLUMN IF NOT EXISTS skipped_stock_items INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS failed_stock_items INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS skipped_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS failed_cost_centers INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_godowns INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_godowns INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_units INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_units INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS total_stock_groups INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS imported_stock_groups INT DEFAULT 0,
ADD COLUMN IF NOT EXISTS parsing_started_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS validation_started_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS validation_completed_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS import_started_at TIMESTAMP WITH TIME ZONE;

-- Add missing columns to tally_field_mappings
ALTER TABLE tally_field_mappings
ADD COLUMN IF NOT EXISTS target_account_subtype VARCHAR(50),
ADD COLUMN IF NOT EXISTS default_account_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tag_assignments JSONB DEFAULT '[]';

-- Add missing columns to tally_migration_logs
ALTER TABLE tally_migration_logs
ADD COLUMN IF NOT EXISTS error_code VARCHAR(50),
ADD COLUMN IF NOT EXISTS amount_difference DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS processing_duration_ms INT;

-- ============================================================================
-- SECTION 2: PAYMENTS TABLE - TALLY VOUCHER TRACKING
-- ============================================================================

ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50);

COMMENT ON COLUMN payments.tally_voucher_type IS 'Tally voucher type (Receipt, Payment, etc.)';

-- ============================================================================
-- SECTION 3: VENDOR PAYMENTS TABLE - TALLY MIGRATION FIELDS
-- ============================================================================

ALTER TABLE vendor_payments
ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_vendor_payments_tally ON vendor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_vendor_payments_tally_batch ON vendor_payments(tally_migration_batch_id) WHERE tally_migration_batch_id IS NOT NULL;

COMMENT ON COLUMN vendor_payments.tally_voucher_type IS 'Tally voucher type (Payment, etc.)';

-- ============================================================================
-- SECTION 4: CONTRACTOR PAYMENTS & EMPLOYEES - TALLY TRACKING
-- ============================================================================

ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_contractor_payments_tally_guid
ON contractor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_contractor_payments_tally_batch
ON contractor_payments(tally_migration_batch_id) WHERE tally_migration_batch_id IS NOT NULL;

-- Employee-level Tally tracking
ALTER TABLE employees
ADD COLUMN IF NOT EXISTS employment_type VARCHAR(20) DEFAULT 'employee',
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100);

CREATE INDEX IF NOT EXISTS idx_employees_tally_guid
ON employees(tally_ledger_guid) WHERE tally_ledger_guid IS NOT NULL;

-- ============================================================================
-- SECTION 5: STATUTORY PAYMENTS - TALLY MIGRATION FIELDS
-- ============================================================================

ALTER TABLE statutory_payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_statutory_payments_tally_guid
ON statutory_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_statutory_payments_tally_batch
ON statutory_payments(tally_migration_batch_id) WHERE tally_migration_batch_id IS NOT NULL;

-- ============================================================================
-- SECTION 6: BANK ACCOUNTS - TALLY INTEGRATION
-- ============================================================================

ALTER TABLE bank_accounts
ADD COLUMN IF NOT EXISTS linked_account_id UUID REFERENCES chart_of_accounts(id),
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_ledger_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID REFERENCES tally_migration_batches(id);

CREATE INDEX IF NOT EXISTS idx_bank_accounts_tally_guid
ON bank_accounts(company_id, tally_ledger_guid);

-- ============================================================================
-- SECTION 7: BANK TRANSACTIONS - TALLY & RECONCILIATION INTEGRATION
-- ============================================================================

ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS reconciliation_difference_amount DECIMAL(18,2),
ADD COLUMN IF NOT EXISTS reconciliation_difference_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS reconciliation_difference_notes TEXT,
ADD COLUMN IF NOT EXISTS reconciliation_tds_section VARCHAR(20),
ADD COLUMN IF NOT EXISTS reconciliation_adjustment_journal_id UUID,
ADD COLUMN IF NOT EXISTS reconciled_journal_entry_id UUID,
ADD COLUMN IF NOT EXISTS reconciled_je_line_id UUID,
ADD COLUMN IF NOT EXISTS paired_transaction_id UUID,
ADD COLUMN IF NOT EXISTS pair_type VARCHAR(20),
ADD COLUMN IF NOT EXISTS reversal_journal_entry_id UUID,
ADD COLUMN IF NOT EXISTS is_reversal_transaction BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS source_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS matched_entity_id UUID,
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_bank_tx_tally_guid ON bank_transactions(tally_voucher_guid);
CREATE INDEX IF NOT EXISTS idx_bank_tx_matched ON bank_transactions(matched_entity_type, matched_entity_id);
CREATE INDEX IF NOT EXISTS idx_bank_tx_source_voucher_type ON bank_transactions(source_voucher_type);
CREATE INDEX IF NOT EXISTS idx_bank_transactions_matched_entity ON bank_transactions(matched_entity_type, matched_entity_id) WHERE matched_entity_id IS NOT NULL;

COMMENT ON COLUMN bank_transactions.matched_entity_type IS
    'Source entity type from Tally import: vendor_payments, contractor_payments, statutory_payments, journal_entries';
COMMENT ON COLUMN bank_transactions.matched_entity_id IS
    'Source entity ID from Tally import - the business entity this bank transaction was created from';
COMMENT ON COLUMN bank_transactions.source_voucher_type IS
    'Tally voucher type that created this transaction: payment, receipt, contra, journal, sales, purchase';

-- ============================================================================
-- SECTION 8: TALLY FIELD MAPPINGS FUNCTION
-- ============================================================================
-- Comprehensive Indian accounting ledger group mappings
-- Based on: Tally ERP9/TallyPrime, Indian tax laws (GST, TDS), ICAI guidelines
-- ============================================================================

CREATE OR REPLACE FUNCTION seed_tally_default_mappings(p_company_id UUID)
RETURNS void AS $$
BEGIN
    -- ========================================================================
    -- PARTY MAPPINGS (Priority 10 - Highest)
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'customers';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank Accounts', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OD A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OCC A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    -- ========================================================================
    -- TDS-APPLICABLE VENDOR CATEGORIES (Priority 10)
    -- Section 194J, 194C, 194H, 194I, 194A per Indian Income Tax Act
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONSULTANTS', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Consultants', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONTRACTORS', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Contractors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PROFESSIONAL FEES', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Professional Fees', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'RENT PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Rent Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'COMMISSION PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Commission Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'BROKERAGE PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Brokerage Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'INTEREST PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Interest Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- ========================================================================
    -- GST ACCOUNTS (Priority 15) - Per Indian GST Law
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Duties & Taxes', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'SGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'IGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'UTGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input CGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input SGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input IGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output CGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output SGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output IGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- TDS PAYABLE ACCOUNTS (Priority 15) - Per Indian Income Tax Act
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Salary', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Contract', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Consulting', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Rent', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Interest', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Commission', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Professional', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- PAYROLL ACCOUNTS (Priority 15) - Per Indian labor laws
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salaries', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary Account', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Employer Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers PF Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers EPS Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'EPS Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers EDLI Contribution', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'EDLI Contribution', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers PF Admin Charges', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Admin Charges', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employee Bonus', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bonus', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Internship-Stipend', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stipend', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Full & Final Settlement', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'ESI Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- CAPITAL ACCOUNT (Priority 15) - Directors/Partners/Proprietors
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Capital Account', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Reserves & Surplus', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Share Capital', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Partners Capital', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    -- ========================================================================
    -- STANDARD CHART OF ACCOUNTS MAPPINGS (Priority 20)
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Cash-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Purchase Accounts', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sales Accounts', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Fixed Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Investments', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Liabilities', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans (Liability)', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Unsecured Loans', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Secured Loans', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans & Advances (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stock-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Provisions', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Misc. Expenses (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

    -- ========================================================================
    -- SYSTEM/FALLBACK ACCOUNTS (Priority 100 - Lowest)
    -- ========================================================================

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Profit & Loss A/c', '', 'skip', true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'skip';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Primary', '', 'skip', true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'skip';

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Suspense A/c', '', 'suspense', true, 100)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Branch / Divisions', '', 'chart_of_accounts', 'asset', true, 30)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION seed_tally_default_mappings IS 'Seeds comprehensive Tally ledger group mappings based on Indian accounting standards (GST, TDS, EPF, Companies Act)';

-- ============================================================================
-- SECTION 9: VOUCHER TYPE ROUTING FIXES
-- ============================================================================
-- Fix voucher type routing for Sales vouchers
-- Route based on VOUCHER TYPE, not party classification
-- ============================================================================

-- Step 1: Fix Sales/Receipt/Debit Note vouchers incorrectly posted to Trade Payables
UPDATE journal_entry_lines jel
SET
    account_id = coa_receivables.id,
    subledger_type = 'customer'
FROM journal_entries je
CROSS JOIN (SELECT id FROM chart_of_accounts WHERE account_code = '1120' LIMIT 1) coa_receivables
WHERE jel.journal_entry_id = je.id
AND LOWER(je.tally_voucher_type) IN ('sales', 'receipt', 'debit_note', 'debit note')
AND jel.account_id IN (SELECT id FROM chart_of_accounts WHERE account_code = '2100')
AND jel.subledger_id IS NOT NULL;

-- Step 2: Fix Purchase/Payment/Credit Note vouchers incorrectly posted to Trade Receivables
UPDATE journal_entry_lines jel
SET
    account_id = coa_payables.id,
    subledger_type = 'vendor'
FROM journal_entries je
CROSS JOIN (SELECT id FROM chart_of_accounts WHERE account_code = '2100' LIMIT 1) coa_payables
WHERE jel.journal_entry_id = je.id
AND LOWER(je.tally_voucher_type) IN ('purchase', 'payment', 'credit_note', 'credit note')
AND jel.account_id IN (SELECT id FROM chart_of_accounts WHERE account_code = '1120')
AND jel.subledger_id IS NOT NULL;

-- Step 3: Update party flags for dual-role parties
UPDATE parties p
SET
    is_customer = true,
    updated_at = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 FROM journal_entry_lines jel
    JOIN journal_entries je ON je.id = jel.journal_entry_id
    JOIN chart_of_accounts coa ON coa.id = jel.account_id
    WHERE jel.subledger_id = p.id
    AND coa.account_code = '1120'
    AND LOWER(je.tally_voucher_type) IN ('sales', 'receipt', 'debit_note', 'debit note')
)
AND p.is_customer = false;

UPDATE parties p
SET
    is_vendor = true,
    updated_at = CURRENT_TIMESTAMP
WHERE EXISTS (
    SELECT 1 FROM journal_entry_lines jel
    JOIN journal_entries je ON je.id = jel.journal_entry_id
    JOIN chart_of_accounts coa ON coa.id = jel.account_id
    WHERE jel.subledger_id = p.id
    AND coa.account_code = '2100'
    AND LOWER(je.tally_voucher_type) IN ('purchase', 'payment', 'credit_note', 'credit note')
)
AND p.is_vendor = false;
