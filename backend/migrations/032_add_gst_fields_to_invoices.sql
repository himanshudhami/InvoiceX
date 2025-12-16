-- Migration: Add GST fields to invoices table
-- Phase 2: GST Invoice Support - Invoices Enhancement
-- Purpose: Support GST invoice types, e-invoicing, and shipping details

-- GST Classification
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS invoice_type VARCHAR(50) DEFAULT 'export';
-- Values: 'export', 'domestic_b2b', 'domestic_b2c', 'sez', 'deemed_export'

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS supply_type VARCHAR(20);
-- Values: 'intra_state', 'inter_state', 'export'

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS place_of_supply VARCHAR(50);
-- State code (e.g., '27' for Maharashtra) or 'export'

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS reverse_charge BOOLEAN DEFAULT false;

-- GST Totals (sum of all line items)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total_cgst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total_sgst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total_igst DECIMAL(18,2) DEFAULT 0;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS total_cess DECIMAL(18,2) DEFAULT 0;

-- E-invoicing fields (for B2B invoices > 5cr threshold)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_applicable BOOLEAN DEFAULT false;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_irn VARCHAR(100);
-- IRN = Invoice Reference Number (64 characters)

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_ack_number VARCHAR(100);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_ack_date TIMESTAMP;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_qr_code TEXT;

-- Shipping details (primarily for goods, but useful for documentation)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS shipping_address TEXT;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS transporter_name VARCHAR(255);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS vehicle_number VARCHAR(50);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS eway_bill_number VARCHAR(50);

-- Add indexes for common queries
CREATE INDEX IF NOT EXISTS idx_invoices_invoice_type ON invoices(invoice_type);
CREATE INDEX IF NOT EXISTS idx_invoices_supply_type ON invoices(supply_type);
CREATE INDEX IF NOT EXISTS idx_invoices_e_invoice_irn ON invoices(e_invoice_irn);

-- Add comments for documentation
COMMENT ON COLUMN invoices.invoice_type IS 'Type of invoice: export, domestic_b2b, domestic_b2c, sez, deemed_export';
COMMENT ON COLUMN invoices.supply_type IS 'Supply type: intra_state (CGST+SGST), inter_state (IGST), export';
COMMENT ON COLUMN invoices.place_of_supply IS 'State code for domestic, or export for foreign';
COMMENT ON COLUMN invoices.reverse_charge IS 'Whether reverse charge mechanism applies';
COMMENT ON COLUMN invoices.total_cgst IS 'Total CGST amount for the invoice';
COMMENT ON COLUMN invoices.total_sgst IS 'Total SGST amount for the invoice';
COMMENT ON COLUMN invoices.total_igst IS 'Total IGST amount for the invoice';
COMMENT ON COLUMN invoices.total_cess IS 'Total Cess amount for the invoice';
COMMENT ON COLUMN invoices.e_invoice_applicable IS 'Whether e-invoicing is applicable (B2B > 5cr)';
COMMENT ON COLUMN invoices.e_invoice_irn IS 'Invoice Reference Number from e-invoice portal';
COMMENT ON COLUMN invoices.e_invoice_ack_number IS 'Acknowledgement number from e-invoice portal';
COMMENT ON COLUMN invoices.e_invoice_ack_date IS 'Acknowledgement date from e-invoice portal';
COMMENT ON COLUMN invoices.e_invoice_qr_code IS 'QR code data for e-invoice';
COMMENT ON COLUMN invoices.shipping_address IS 'Shipping/delivery address for goods';
COMMENT ON COLUMN invoices.transporter_name IS 'Name of transporter for e-way bill';
COMMENT ON COLUMN invoices.vehicle_number IS 'Vehicle number for e-way bill';
COMMENT ON COLUMN invoices.eway_bill_number IS 'E-way bill number for goods transport';
