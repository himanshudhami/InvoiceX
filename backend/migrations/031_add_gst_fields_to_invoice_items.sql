-- Migration: Add GST fields to invoice_items table
-- Phase 2: GST Invoice Support - Invoice Items Enhancement
-- Purpose: Support HSN/SAC codes and GST breakup for line items

-- HSN/SAC code for GST compliance
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS hsn_sac_code VARCHAR(20);

-- Service indicator (true=SAC code, false=HSN code)
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS is_service BOOLEAN DEFAULT true;

-- CGST (Central GST) - for intra-state supplies
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS cgst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS cgst_amount DECIMAL(18,2) DEFAULT 0;

-- SGST (State GST) - for intra-state supplies
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS sgst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS sgst_amount DECIMAL(18,2) DEFAULT 0;

-- IGST (Integrated GST) - for inter-state supplies
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS igst_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS igst_amount DECIMAL(18,2) DEFAULT 0;

-- Cess - for specific goods (e.g., tobacco, aerated drinks)
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS cess_rate DECIMAL(5,2) DEFAULT 0;
ALTER TABLE invoice_items ADD COLUMN IF NOT EXISTS cess_amount DECIMAL(18,2) DEFAULT 0;

-- Add index for HSN/SAC code lookups
CREATE INDEX IF NOT EXISTS idx_invoice_items_hsn_sac ON invoice_items(hsn_sac_code);

-- Add comments for documentation
COMMENT ON COLUMN invoice_items.hsn_sac_code IS 'HSN code (goods) or SAC code (services) for GST';
COMMENT ON COLUMN invoice_items.is_service IS 'True for SAC code (services), false for HSN code (goods)';
COMMENT ON COLUMN invoice_items.cgst_rate IS 'Central GST rate percentage (0, 2.5, 6, 9, 14)';
COMMENT ON COLUMN invoice_items.cgst_amount IS 'Central GST amount calculated';
COMMENT ON COLUMN invoice_items.sgst_rate IS 'State GST rate percentage (0, 2.5, 6, 9, 14)';
COMMENT ON COLUMN invoice_items.sgst_amount IS 'State GST amount calculated';
COMMENT ON COLUMN invoice_items.igst_rate IS 'Integrated GST rate percentage (0, 5, 12, 18, 28)';
COMMENT ON COLUMN invoice_items.igst_amount IS 'Integrated GST amount calculated';
COMMENT ON COLUMN invoice_items.cess_rate IS 'Cess rate percentage for specific goods';
COMMENT ON COLUMN invoice_items.cess_amount IS 'Cess amount calculated';
