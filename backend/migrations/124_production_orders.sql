-- Production Orders
CREATE TABLE production_orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    order_number VARCHAR(50) NOT NULL,
    bom_id UUID NOT NULL REFERENCES bill_of_materials(id),
    finished_good_id UUID NOT NULL REFERENCES stock_items(id),
    warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    planned_quantity DECIMAL(18,4) NOT NULL,
    actual_quantity DECIMAL(18,4) DEFAULT 0,
    planned_start_date DATE,
    planned_end_date DATE,
    actual_start_date TIMESTAMPTZ,
    actual_end_date TIMESTAMPTZ,
    status VARCHAR(20) NOT NULL DEFAULT 'draft',
    notes TEXT,
    released_by UUID REFERENCES users(id),
    released_at TIMESTAMPTZ,
    started_by UUID REFERENCES users(id),
    started_at TIMESTAMPTZ,
    completed_by UUID REFERENCES users(id),
    completed_at TIMESTAMPTZ,
    cancelled_by UUID REFERENCES users(id),
    cancelled_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(company_id, order_number)
);

CREATE TABLE production_order_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id UUID NOT NULL REFERENCES production_orders(id) ON DELETE CASCADE,
    component_id UUID NOT NULL REFERENCES stock_items(id),
    planned_quantity DECIMAL(18,4) NOT NULL,
    consumed_quantity DECIMAL(18,4) DEFAULT 0,
    unit_id UUID REFERENCES units_of_measure(id),
    batch_id UUID REFERENCES stock_batches(id),
    warehouse_id UUID REFERENCES warehouses(id),
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_prod_order_company ON production_orders(company_id);
CREATE INDEX idx_prod_order_status ON production_orders(status);
CREATE INDEX idx_prod_order_bom ON production_orders(bom_id);
CREATE INDEX idx_prod_order_finished_good ON production_orders(finished_good_id);
CREATE INDEX idx_prod_order_warehouse ON production_orders(warehouse_id);
CREATE INDEX idx_prod_order_items ON production_order_items(production_order_id);
CREATE INDEX idx_prod_order_items_component ON production_order_items(component_id);
