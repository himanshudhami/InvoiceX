-- Add serial tracking flag to stock_items
ALTER TABLE stock_items ADD COLUMN IF NOT EXISTS is_serial_enabled BOOLEAN DEFAULT false;

-- Serial Numbers
CREATE TABLE serial_numbers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    stock_item_id UUID NOT NULL REFERENCES stock_items(id),
    serial_no VARCHAR(100) NOT NULL,
    warehouse_id UUID REFERENCES warehouses(id),
    batch_id UUID REFERENCES stock_batches(id),
    status VARCHAR(20) NOT NULL DEFAULT 'available',
    manufacturing_date DATE,
    warranty_expiry DATE,
    production_order_id UUID REFERENCES production_orders(id),
    sold_at TIMESTAMPTZ,
    sold_invoice_id UUID REFERENCES invoices(id),
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(company_id, stock_item_id, serial_no)
);

CREATE INDEX idx_serial_company ON serial_numbers(company_id);
CREATE INDEX idx_serial_item ON serial_numbers(stock_item_id);
CREATE INDEX idx_serial_warehouse ON serial_numbers(warehouse_id);
CREATE INDEX idx_serial_status ON serial_numbers(status);
CREATE INDEX idx_serial_production_order ON serial_numbers(production_order_id);
CREATE INDEX idx_serial_batch ON serial_numbers(batch_id);
