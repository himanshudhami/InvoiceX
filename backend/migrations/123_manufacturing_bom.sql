-- Bill of Materials
CREATE TABLE bill_of_materials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id),
    finished_good_id UUID NOT NULL REFERENCES stock_items(id),
    name VARCHAR(200) NOT NULL,
    code VARCHAR(50),
    version VARCHAR(20) DEFAULT '1.0',
    effective_from DATE,
    effective_to DATE,
    output_quantity DECIMAL(18,4) NOT NULL DEFAULT 1,
    output_unit_id UUID REFERENCES units_of_measure(id),
    is_active BOOLEAN NOT NULL DEFAULT true,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    UNIQUE(company_id, code)
);

CREATE TABLE bom_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bom_id UUID NOT NULL REFERENCES bill_of_materials(id) ON DELETE CASCADE,
    component_id UUID NOT NULL REFERENCES stock_items(id),
    quantity DECIMAL(18,4) NOT NULL,
    unit_id UUID REFERENCES units_of_measure(id),
    scrap_percentage DECIMAL(5,2) DEFAULT 0,
    is_optional BOOLEAN DEFAULT false,
    sequence INT DEFAULT 0,
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_bom_company ON bill_of_materials(company_id);
CREATE INDEX idx_bom_finished_good ON bill_of_materials(finished_good_id);
CREATE INDEX idx_bom_active ON bill_of_materials(is_active) WHERE is_active = true;
CREATE INDEX idx_bom_items_bom ON bom_items(bom_id);
CREATE INDEX idx_bom_items_component ON bom_items(component_id);
