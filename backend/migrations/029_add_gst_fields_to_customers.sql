-- Migration: Add GST fields to customers table
-- Phase B: GST Compliance - enables customer GST tracking for domestic invoices

-- Add GSTIN (15 character alphanumeric)
ALTER TABLE customers ADD COLUMN IF NOT EXISTS gstin VARCHAR(20);

-- Add GST state code (2 digit code)
ALTER TABLE customers ADD COLUMN IF NOT EXISTS gst_state_code VARCHAR(5);

-- Add customer type for GST classification
ALTER TABLE customers ADD COLUMN IF NOT EXISTS customer_type VARCHAR(20) DEFAULT 'overseas';
-- Values: 'b2b' (GST registered business), 'b2c' (unregistered/consumer), 'overseas' (export), 'sez' (SEZ unit)

-- Add GST registration flag
ALTER TABLE customers ADD COLUMN IF NOT EXISTS is_gst_registered BOOLEAN DEFAULT false;

-- Add PAN number (for TDS purposes)
ALTER TABLE customers ADD COLUMN IF NOT EXISTS pan_number VARCHAR(15);

-- Index for GSTIN lookups
CREATE INDEX IF NOT EXISTS idx_customers_gstin ON customers(gstin) WHERE gstin IS NOT NULL;

-- Index for customer type filtering
CREATE INDEX IF NOT EXISTS idx_customers_type ON customers(customer_type);

-- Index for PAN lookups
CREATE INDEX IF NOT EXISTS idx_customers_pan ON customers(pan_number) WHERE pan_number IS NOT NULL;

-- Log migration
DO $$
BEGIN
    RAISE NOTICE 'Added GST fields to customers table for Indian tax compliance';
END $$;
