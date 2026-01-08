-- Migration: 130_fix_parties_column_sizes.sql
-- Description: Increase column sizes in parties table to handle Tally data variations
-- Issue: Tally data may have longer values than expected (e.g., international postcodes, invalid PANs)

-- Increase pincode to handle international postal codes
ALTER TABLE parties ALTER COLUMN pincode TYPE VARCHAR(20);

-- Increase pan_number to handle invalid/formatted PAN data from Tally
ALTER TABLE parties ALTER COLUMN pan_number TYPE VARCHAR(20);

-- Increase state_code to handle longer codes
ALTER TABLE parties ALTER COLUMN state_code TYPE VARCHAR(10);

-- Increase gst_state_code as well
ALTER TABLE parties ALTER COLUMN gst_state_code TYPE VARCHAR(10);

-- Also fix party_vendor_profiles columns if needed
ALTER TABLE party_vendor_profiles ALTER COLUMN default_tds_section TYPE VARCHAR(20);
ALTER TABLE party_vendor_profiles ALTER COLUMN msme_category TYPE VARCHAR(20);
ALTER TABLE party_vendor_profiles ALTER COLUMN bank_ifsc_code TYPE VARCHAR(20);
ALTER TABLE party_vendor_profiles ALTER COLUMN tan_number TYPE VARCHAR(20);

-- Fix tds_section_rules column
ALTER TABLE tds_section_rules ALTER COLUMN tds_section TYPE VARCHAR(20);

COMMENT ON COLUMN parties.pincode IS 'Postal code - increased to handle international formats';
COMMENT ON COLUMN parties.pan_number IS 'PAN - increased to handle invalid Tally data';
