-- 004_assets_cost_and_disposal.sql
-- Adds cost management fields and disposal tracking for assets

-- Add cost/depreciation related columns
ALTER TABLE assets
    ADD COLUMN IF NOT EXISTS purchase_type VARCHAR(10) DEFAULT 'capex',
    ADD COLUMN IF NOT EXISTS invoice_reference VARCHAR(150),
    ADD COLUMN IF NOT EXISTS depreciation_start_date DATE;

ALTER TABLE assets
    ADD CONSTRAINT assets_purchase_type_check CHECK (purchase_type IN ('capex','opex'));

CREATE INDEX IF NOT EXISTS idx_assets_purchase_type ON assets(purchase_type);

-- Disposal tracking
CREATE TABLE IF NOT EXISTS asset_disposals (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES assets(id) ON DELETE CASCADE,
    disposed_on DATE NOT NULL DEFAULT CURRENT_DATE,
    method VARCHAR(30) NOT NULL DEFAULT 'retired',
    proceeds NUMERIC(14,2),
    disposal_cost NUMERIC(14,2),
    buyer VARCHAR(200),
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT asset_disposals_method_check CHECK (method IN ('sold','retired','recycled','donated','lost'))
);

CREATE INDEX IF NOT EXISTS idx_asset_disposals_asset ON asset_disposals(asset_id);



