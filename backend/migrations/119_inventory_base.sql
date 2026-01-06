-- Migration: 119_inventory_base.sql
-- Description: Creates base inventory tables - Warehouses, Stock Groups, Units of Measure
-- Date: 2026-01-06

-- Warehouses (Godowns)
CREATE TABLE IF NOT EXISTS warehouses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    pin_code VARCHAR(20),
    is_default BOOLEAN DEFAULT FALSE,
    parent_warehouse_id UUID REFERENCES warehouses(id),
    tally_godown_guid VARCHAR(100),
    tally_godown_name VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Stock Groups (hierarchical categorization)
CREATE TABLE IF NOT EXISTS stock_groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    name VARCHAR(255) NOT NULL,
    parent_stock_group_id UUID REFERENCES stock_groups(id),
    tally_stock_group_guid VARCHAR(100),
    tally_stock_group_name VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Units of Measure
CREATE TABLE IF NOT EXISTS units_of_measure (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id), -- NULL for system units
    name VARCHAR(100) NOT NULL,
    symbol VARCHAR(20) NOT NULL,
    decimal_places INT DEFAULT 2,
    is_system_unit BOOLEAN DEFAULT FALSE,
    tally_unit_guid VARCHAR(100),
    tally_unit_name VARCHAR(100),
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- Seed common system units
INSERT INTO units_of_measure (name, symbol, decimal_places, is_system_unit) VALUES
('Pieces', 'Pcs', 0, TRUE),
('Numbers', 'Nos', 0, TRUE),
('Kilograms', 'Kg', 3, TRUE),
('Grams', 'g', 2, TRUE),
('Liters', 'L', 3, TRUE),
('Milliliters', 'mL', 0, TRUE),
('Meters', 'm', 2, TRUE),
('Centimeters', 'cm', 1, TRUE),
('Boxes', 'Box', 0, TRUE),
('Cartons', 'Ctn', 0, TRUE),
('Dozens', 'Doz', 0, TRUE),
('Pairs', 'Pr', 0, TRUE),
('Sets', 'Set', 0, TRUE),
('Bags', 'Bag', 0, TRUE),
('Bundles', 'Bdl', 0, TRUE)
ON CONFLICT DO NOTHING;

-- Indexes
CREATE INDEX IF NOT EXISTS idx_warehouses_company ON warehouses(company_id);
CREATE INDEX IF NOT EXISTS idx_warehouses_parent ON warehouses(parent_warehouse_id);
CREATE INDEX IF NOT EXISTS idx_warehouses_active ON warehouses(company_id, is_active);

CREATE INDEX IF NOT EXISTS idx_stock_groups_company ON stock_groups(company_id);
CREATE INDEX IF NOT EXISTS idx_stock_groups_parent ON stock_groups(parent_stock_group_id);
CREATE INDEX IF NOT EXISTS idx_stock_groups_active ON stock_groups(company_id, is_active);

CREATE INDEX IF NOT EXISTS idx_units_of_measure_company ON units_of_measure(company_id);
CREATE INDEX IF NOT EXISTS idx_units_of_measure_symbol ON units_of_measure(symbol);
