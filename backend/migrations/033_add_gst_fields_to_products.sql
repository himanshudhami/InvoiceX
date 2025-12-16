-- Migration: Add GST fields to products table
-- Phase 2: GST Invoice Support - Products Enhancement
-- Purpose: Add HSN/SAC codes and default GST rates to products

-- HSN/SAC code for GST compliance
ALTER TABLE products ADD COLUMN IF NOT EXISTS hsn_sac_code VARCHAR(20);

-- Service indicator (true=SAC code for services, false=HSN code for goods)
ALTER TABLE products ADD COLUMN IF NOT EXISTS is_service BOOLEAN DEFAULT true;

-- Default GST rate for this product/service
-- Common rates: 0, 5, 12, 18, 28
ALTER TABLE products ADD COLUMN IF NOT EXISTS default_gst_rate DECIMAL(5,2) DEFAULT 18;

-- Cess rate for specific goods (tobacco, aerated drinks, luxury cars, etc.)
ALTER TABLE products ADD COLUMN IF NOT EXISTS cess_rate DECIMAL(5,2) DEFAULT 0;

-- Add index for HSN/SAC code lookups
CREATE INDEX IF NOT EXISTS idx_products_hsn_sac ON products(hsn_sac_code);

-- Add comments for documentation
COMMENT ON COLUMN products.hsn_sac_code IS 'HSN code (goods) or SAC code (services) for GST classification';
COMMENT ON COLUMN products.is_service IS 'True for services (SAC), false for goods (HSN)';
COMMENT ON COLUMN products.default_gst_rate IS 'Default GST rate (0, 5, 12, 18, 28) for this product';
COMMENT ON COLUMN products.cess_rate IS 'Cess rate for specific goods (e.g., tobacco, aerated drinks)';
