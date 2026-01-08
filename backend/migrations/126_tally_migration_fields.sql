-- Migration: 126_tally_migration_fields.sql
-- Description: Add Tally migration tracking fields to existing tables
-- Purpose: Enable tracking of imported records from Tally ERP for deduplication and audit

-- ============================================================================
-- CHART OF ACCOUNTS - Add Tally Ledger tracking
-- ============================================================================

ALTER TABLE chart_of_accounts
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_ledger_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_group_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

-- Index for fast lookup by Tally GUID (for deduplication during import)
CREATE INDEX IF NOT EXISTS idx_coa_tally_guid
ON chart_of_accounts(tally_ledger_guid)
WHERE tally_ledger_guid IS NOT NULL;

COMMENT ON COLUMN chart_of_accounts.tally_ledger_guid IS 'Original Tally Ledger GUID for migration tracking and deduplication';
COMMENT ON COLUMN chart_of_accounts.tally_ledger_name IS 'Original Tally Ledger Name at time of import';
COMMENT ON COLUMN chart_of_accounts.tally_group_name IS 'Tally parent group name for mapping reference';
COMMENT ON COLUMN chart_of_accounts.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- CUSTOMERS - Add Tally Ledger tracking (Sundry Debtors)
-- ============================================================================

ALTER TABLE customers
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_ledger_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_customers_tally_guid
ON customers(tally_ledger_guid)
WHERE tally_ledger_guid IS NOT NULL;

COMMENT ON COLUMN customers.tally_ledger_guid IS 'Original Tally Ledger GUID (from Sundry Debtors group)';
COMMENT ON COLUMN customers.tally_ledger_name IS 'Original Tally Ledger Name at time of import';
COMMENT ON COLUMN customers.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- INVOICES (Sales) - Add Tally Voucher tracking
-- ============================================================================

ALTER TABLE invoices
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_invoices_tally_guid
ON invoices(tally_voucher_guid)
WHERE tally_voucher_guid IS NOT NULL;

COMMENT ON COLUMN invoices.tally_voucher_guid IS 'Original Tally Sales Voucher GUID';
COMMENT ON COLUMN invoices.tally_voucher_number IS 'Original Tally Voucher Number';
COMMENT ON COLUMN invoices.tally_voucher_type IS 'Tally voucher type (Sales, Credit Note, etc.)';
COMMENT ON COLUMN invoices.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- PAYMENTS (Receipts) - Add Tally Voucher tracking
-- ============================================================================

ALTER TABLE payments
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_payments_tally_guid
ON payments(tally_voucher_guid)
WHERE tally_voucher_guid IS NOT NULL;

COMMENT ON COLUMN payments.tally_voucher_guid IS 'Original Tally Receipt Voucher GUID';
COMMENT ON COLUMN payments.tally_voucher_number IS 'Original Tally Voucher Number';
COMMENT ON COLUMN payments.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- JOURNAL ENTRIES - Add Tally Voucher tracking
-- ============================================================================

ALTER TABLE journal_entries
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_type VARCHAR(50),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_journal_entries_tally_guid
ON journal_entries(tally_voucher_guid)
WHERE tally_voucher_guid IS NOT NULL;

COMMENT ON COLUMN journal_entries.tally_voucher_guid IS 'Original Tally Voucher GUID (Journal, Contra, etc.)';
COMMENT ON COLUMN journal_entries.tally_voucher_number IS 'Original Tally Voucher Number';
COMMENT ON COLUMN journal_entries.tally_voucher_type IS 'Tally voucher type (Journal, Contra, etc.)';
COMMENT ON COLUMN journal_entries.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- BANK ACCOUNTS - Add Tally Ledger tracking
-- ============================================================================

ALTER TABLE bank_accounts
ADD COLUMN IF NOT EXISTS tally_ledger_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_ledger_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_bank_accounts_tally_guid
ON bank_accounts(tally_ledger_guid)
WHERE tally_ledger_guid IS NOT NULL;

COMMENT ON COLUMN bank_accounts.tally_ledger_guid IS 'Original Tally Bank Ledger GUID';
COMMENT ON COLUMN bank_accounts.tally_ledger_name IS 'Original Tally Ledger Name';
COMMENT ON COLUMN bank_accounts.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- BANK TRANSACTIONS - Add Tally Voucher tracking
-- ============================================================================

ALTER TABLE bank_transactions
ADD COLUMN IF NOT EXISTS tally_voucher_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_voucher_number VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_bank_transactions_tally_guid
ON bank_transactions(tally_voucher_guid)
WHERE tally_voucher_guid IS NOT NULL;

COMMENT ON COLUMN bank_transactions.tally_voucher_guid IS 'Original Tally Voucher GUID (Payment, Receipt, Contra)';
COMMENT ON COLUMN bank_transactions.tally_voucher_number IS 'Original Tally Voucher Number';
COMMENT ON COLUMN bank_transactions.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- PRODUCTS - Add Tally Stock Item tracking (if not already linked via stock_items)
-- ============================================================================

ALTER TABLE products
ADD COLUMN IF NOT EXISTS tally_stock_item_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_stock_item_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

CREATE INDEX IF NOT EXISTS idx_products_tally_guid
ON products(tally_stock_item_guid)
WHERE tally_stock_item_guid IS NOT NULL;

COMMENT ON COLUMN products.tally_stock_item_guid IS 'Original Tally Stock Item GUID (links to stock_items)';
COMMENT ON COLUMN products.tally_stock_item_name IS 'Original Tally Stock Item Name';
COMMENT ON COLUMN products.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- Add tally_migration_batch_id to tables that already have tally_*_guid fields
-- for consistent batch tracking
-- ============================================================================

-- Stock Items (already has tally_stock_item_guid)
ALTER TABLE stock_items
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

COMMENT ON COLUMN stock_items.tally_migration_batch_id IS 'Migration batch that imported this record';

-- Stock Groups (already has tally_stock_group_guid)
ALTER TABLE stock_groups
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

COMMENT ON COLUMN stock_groups.tally_migration_batch_id IS 'Migration batch that imported this record';

-- Warehouses (already has tally_godown_guid)
ALTER TABLE warehouses
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

COMMENT ON COLUMN warehouses.tally_migration_batch_id IS 'Migration batch that imported this record';

-- Units of Measure (already has tally_unit_guid)
ALTER TABLE units_of_measure
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

COMMENT ON COLUMN units_of_measure.tally_migration_batch_id IS 'Migration batch that imported this record';

-- Tags (already has tally_cost_center_guid)
ALTER TABLE tags
ADD COLUMN IF NOT EXISTS tally_migration_batch_id UUID;

COMMENT ON COLUMN tags.tally_migration_batch_id IS 'Migration batch that imported this record';

-- ============================================================================
-- INVOICE ITEMS - Track source stock item for inventory reconciliation
-- ============================================================================

ALTER TABLE invoice_items
ADD COLUMN IF NOT EXISTS tally_stock_item_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_batch_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_godown_guid VARCHAR(100);

COMMENT ON COLUMN invoice_items.tally_stock_item_guid IS 'Original Tally Stock Item reference for inventory linking';
COMMENT ON COLUMN invoice_items.tally_batch_guid IS 'Original Tally Batch reference';
COMMENT ON COLUMN invoice_items.tally_godown_guid IS 'Original Tally Godown/Warehouse reference';

-- ============================================================================
-- VENDOR INVOICE ITEMS - Track source stock item (already may exist)
-- ============================================================================

ALTER TABLE vendor_invoice_items
ADD COLUMN IF NOT EXISTS tally_stock_item_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_batch_guid VARCHAR(100),
ADD COLUMN IF NOT EXISTS tally_godown_guid VARCHAR(100);

COMMENT ON COLUMN vendor_invoice_items.tally_stock_item_guid IS 'Original Tally Stock Item reference for inventory linking';
COMMENT ON COLUMN vendor_invoice_items.tally_batch_guid IS 'Original Tally Batch reference';
COMMENT ON COLUMN vendor_invoice_items.tally_godown_guid IS 'Original Tally Godown/Warehouse reference';
