-- Migration: 122_stock_transfers.sql
-- Description: Creates Stock Transfers (Inter-warehouse) and Stock Transfer Items tables
-- Date: 2026-01-06

-- Stock Transfers (Inter-warehouse transfer header)
CREATE TABLE IF NOT EXISTS stock_transfers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    transfer_number VARCHAR(100) NOT NULL,
    transfer_date DATE NOT NULL,
    from_warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    to_warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    status VARCHAR(20) DEFAULT 'draft', -- draft, in_transit, completed, cancelled
    total_quantity DECIMAL(18,4) DEFAULT 0,
    total_value DECIMAL(18,4) DEFAULT 0,
    notes TEXT,
    tally_voucher_guid VARCHAR(100),
    created_by UUID,
    approved_by UUID,
    approved_at TIMESTAMP,
    completed_by UUID,
    completed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT chk_different_warehouses CHECK (from_warehouse_id != to_warehouse_id)
);

-- Stock Transfer Items (line items for each transfer)
CREATE TABLE IF NOT EXISTS stock_transfer_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_transfer_id UUID NOT NULL REFERENCES stock_transfers(id) ON DELETE CASCADE,
    stock_item_id UUID NOT NULL REFERENCES stock_items(id),
    batch_id UUID REFERENCES stock_batches(id),
    quantity DECIMAL(18,4) NOT NULL,
    rate DECIMAL(18,4),
    value DECIMAL(18,4),
    received_quantity DECIMAL(18,4), -- for partial receipts
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Indexes for stock transfers
CREATE INDEX IF NOT EXISTS idx_stock_transfers_company ON stock_transfers(company_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_number ON stock_transfers(transfer_number);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_date ON stock_transfers(transfer_date);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_from ON stock_transfers(from_warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_to ON stock_transfers(to_warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_status ON stock_transfers(status);
CREATE INDEX IF NOT EXISTS idx_stock_transfers_company_status ON stock_transfers(company_id, status);

-- Indexes for stock transfer items
CREATE INDEX IF NOT EXISTS idx_stock_transfer_items_transfer ON stock_transfer_items(stock_transfer_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfer_items_item ON stock_transfer_items(stock_item_id);
CREATE INDEX IF NOT EXISTS idx_stock_transfer_items_batch ON stock_transfer_items(batch_id);
