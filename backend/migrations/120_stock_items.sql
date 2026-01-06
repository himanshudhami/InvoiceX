-- Migration: 120_stock_items.sql
-- Description: Creates Stock Items and Unit Conversions tables
-- Date: 2026-01-06

-- Stock Items (Inventory items with tracking)
CREATE TABLE IF NOT EXISTS stock_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    name VARCHAR(255) NOT NULL,
    sku VARCHAR(100),
    description TEXT,
    stock_group_id UUID REFERENCES stock_groups(id),
    base_unit_id UUID NOT NULL REFERENCES units_of_measure(id),
    hsn_sac_code VARCHAR(20),
    gst_rate DECIMAL(5,2) DEFAULT 18,
    opening_quantity DECIMAL(18,4) DEFAULT 0,
    opening_value DECIMAL(18,4) DEFAULT 0,
    current_quantity DECIMAL(18,4) DEFAULT 0,
    current_value DECIMAL(18,4) DEFAULT 0,
    reorder_level DECIMAL(18,4),
    reorder_quantity DECIMAL(18,4),
    minimum_stock DECIMAL(18,4),
    maximum_stock DECIMAL(18,4),
    is_batch_enabled BOOLEAN DEFAULT FALSE,
    valuation_method VARCHAR(20) DEFAULT 'weighted_avg', -- fifo, lifo, weighted_avg
    cost_price DECIMAL(18,4),
    selling_price DECIMAL(18,4),
    mrp DECIMAL(18,4),
    tally_stock_item_guid VARCHAR(100),
    tally_stock_item_name VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Unit Conversions (alternate units per item, e.g., 1 Box = 12 Pcs)
CREATE TABLE IF NOT EXISTS unit_conversions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_item_id UUID NOT NULL REFERENCES stock_items(id) ON DELETE CASCADE,
    from_unit_id UUID NOT NULL REFERENCES units_of_measure(id),
    to_unit_id UUID NOT NULL REFERENCES units_of_measure(id),
    conversion_factor DECIMAL(18,6) NOT NULL, -- 1 from_unit = X to_unit
    created_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT uq_unit_conversion UNIQUE (stock_item_id, from_unit_id, to_unit_id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_stock_items_company ON stock_items(company_id);
CREATE INDEX IF NOT EXISTS idx_stock_items_group ON stock_items(stock_group_id);
CREATE INDEX IF NOT EXISTS idx_stock_items_sku ON stock_items(sku);
CREATE INDEX IF NOT EXISTS idx_stock_items_hsn ON stock_items(hsn_sac_code);
CREATE INDEX IF NOT EXISTS idx_stock_items_active ON stock_items(company_id, is_active);
CREATE INDEX IF NOT EXISTS idx_stock_items_reorder ON stock_items(company_id, current_quantity, reorder_level)
    WHERE is_active = TRUE AND reorder_level IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_unit_conversions_item ON unit_conversions(stock_item_id);
