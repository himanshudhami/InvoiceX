-- Migration: 121_stock_movements.sql
-- Description: Creates Stock Batches and Stock Movements (Stock Ledger) tables
-- Date: 2026-01-06

-- Stock Batches (for batch/lot tracking with expiry)
CREATE TABLE IF NOT EXISTS stock_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_item_id UUID NOT NULL REFERENCES stock_items(id),
    warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    batch_number VARCHAR(100) NOT NULL,
    manufacturing_date DATE,
    expiry_date DATE,
    quantity DECIMAL(18,4) DEFAULT 0,
    value DECIMAL(18,4) DEFAULT 0,
    cost_rate DECIMAL(18,4),
    tally_batch_guid VARCHAR(100),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT uq_batch_item_warehouse UNIQUE (stock_item_id, warehouse_id, batch_number)
);

-- Stock Movements (Stock Ledger entries)
CREATE TABLE IF NOT EXISTS stock_movements (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    stock_item_id UUID NOT NULL REFERENCES stock_items(id),
    warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    batch_id UUID REFERENCES stock_batches(id),
    movement_date DATE NOT NULL,
    movement_type VARCHAR(50) NOT NULL, -- purchase, sale, transfer_in, transfer_out, adjustment, opening, return_in, return_out
    quantity DECIMAL(18,4) NOT NULL, -- positive for in, negative for out
    rate DECIMAL(18,4),
    value DECIMAL(18,4),
    source_type VARCHAR(50), -- sales_invoice, purchase_invoice, stock_journal, stock_transfer, credit_note, debit_note
    source_id UUID,
    source_number VARCHAR(100),
    journal_entry_id UUID REFERENCES journal_entries(id),
    tally_voucher_guid VARCHAR(100),
    running_quantity DECIMAL(18,4), -- running total for stock ledger report
    running_value DECIMAL(18,4), -- running value for stock ledger report
    notes TEXT,
    created_by UUID,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for stock batches
CREATE INDEX IF NOT EXISTS idx_stock_batches_item ON stock_batches(stock_item_id);
CREATE INDEX IF NOT EXISTS idx_stock_batches_warehouse ON stock_batches(warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_batches_expiry ON stock_batches(expiry_date) WHERE expiry_date IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_stock_batches_active ON stock_batches(stock_item_id, warehouse_id, is_active);

-- Indexes for stock movements
CREATE INDEX IF NOT EXISTS idx_stock_movements_company ON stock_movements(company_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_item ON stock_movements(stock_item_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_warehouse ON stock_movements(warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_batch ON stock_movements(batch_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_date ON stock_movements(movement_date);
CREATE INDEX IF NOT EXISTS idx_stock_movements_type ON stock_movements(movement_type);
CREATE INDEX IF NOT EXISTS idx_stock_movements_source ON stock_movements(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_stock_movements_item_date ON stock_movements(stock_item_id, movement_date);
CREATE INDEX IF NOT EXISTS idx_stock_movements_ledger ON stock_movements(company_id, stock_item_id, warehouse_id, movement_date);
