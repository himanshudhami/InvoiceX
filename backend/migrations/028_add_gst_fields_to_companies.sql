-- Migration: Add GST/PAN fields to companies table
-- Phase B: GST Compliance - enables Indian tax compliance for companies

-- Add GSTIN (15 character alphanumeric)
ALTER TABLE companies ADD COLUMN IF NOT EXISTS gstin VARCHAR(20);

-- Add GST state code (2 digit code, first 2 chars of GSTIN)
ALTER TABLE companies ADD COLUMN IF NOT EXISTS gst_state_code VARCHAR(5);

-- Add PAN number (10 character alphanumeric)
ALTER TABLE companies ADD COLUMN IF NOT EXISTS pan_number VARCHAR(15);

-- Add CIN (Corporate Identity Number - 21 chars for companies)
ALTER TABLE companies ADD COLUMN IF NOT EXISTS cin_number VARCHAR(25);

-- Add GST registration type
ALTER TABLE companies ADD COLUMN IF NOT EXISTS gst_registration_type VARCHAR(50) DEFAULT 'regular';
-- Values: 'regular', 'composition', 'unregistered', 'overseas'

-- Index for GSTIN lookups
CREATE INDEX IF NOT EXISTS idx_companies_gstin ON companies(gstin) WHERE gstin IS NOT NULL;

-- Index for PAN lookups
CREATE INDEX IF NOT EXISTS idx_companies_pan ON companies(pan_number) WHERE pan_number IS NOT NULL;

-- Log migration
DO $$
BEGIN
    RAISE NOTICE 'Added GST/PAN fields to companies table for Indian tax compliance';
END $$;
