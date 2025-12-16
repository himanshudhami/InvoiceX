-- Migration: Create HSN/SAC codes reference table
-- Common codes for IT services, software, and consulting

CREATE TABLE IF NOT EXISTS hsn_sac_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(20) NOT NULL UNIQUE,
    description VARCHAR(500) NOT NULL,
    type VARCHAR(10) NOT NULL CHECK (type IN ('HSN', 'SAC')),
    category VARCHAR(100),
    gst_rate DECIMAL(5,2) DEFAULT 18,
    is_common BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX idx_hsn_sac_code ON hsn_sac_codes(code);
CREATE INDEX idx_hsn_sac_type ON hsn_sac_codes(type);
CREATE INDEX idx_hsn_sac_category ON hsn_sac_codes(category);
CREATE INDEX idx_hsn_sac_common ON hsn_sac_codes(is_common);

-- Insert common SAC codes for IT Services
INSERT INTO hsn_sac_codes (code, description, type, category, gst_rate, is_common) VALUES
-- IT Services (Chapter 9983)
('9983', 'Other professional, technical and business services', 'SAC', 'IT Services', 18, true),
('998311', 'Management consulting and management services', 'SAC', 'Consulting', 18, true),
('998312', 'Business consulting services', 'SAC', 'Consulting', 18, true),
('998313', 'Information technology (IT) consulting and support services', 'SAC', 'IT Services', 18, true),
('998314', 'Information technology (IT) design and development services', 'SAC', 'IT Services', 18, true),
('998315', 'Hosting and IT infrastructure provisioning services', 'SAC', 'IT Services', 18, true),
('998316', 'IT infrastructure and network management services', 'SAC', 'IT Services', 18, true),
('998319', 'Other information technology services n.e.c.', 'SAC', 'IT Services', 18, true),

-- Software Licensing
('997331', 'Licensing services for the right to use computer software', 'SAC', 'Software', 18, true),
('997332', 'Licensing services for the right to use databases', 'SAC', 'Software', 18, true),

-- Engineering & Technical Services
('998331', 'Engineering services', 'SAC', 'Engineering', 18, true),
('998332', 'Engineering services for buildings and civil engineering', 'SAC', 'Engineering', 18, true),
('998333', 'Engineering services for industrial projects', 'SAC', 'Engineering', 18, true),
('998334', 'Engineering services for transportation projects', 'SAC', 'Engineering', 18, true),
('998335', 'Project management services for construction projects', 'SAC', 'Engineering', 18, true),

-- Scientific & Technical Consulting
('998341', 'Geological and geophysical consulting services', 'SAC', 'Consulting', 18, false),
('998342', 'Subsurface surveying services', 'SAC', 'Consulting', 18, false),
('998343', 'Surface surveying and map-making services', 'SAC', 'Consulting', 18, false),
('998344', 'Weather forecasting and meteorological services', 'SAC', 'Consulting', 18, false),
('998345', 'Scientific advisory and consulting services n.e.c.', 'SAC', 'Consulting', 18, false),

-- Accounting & Legal
('998211', 'Financial auditing services', 'SAC', 'Accounting', 18, true),
('998212', 'Accounting review services', 'SAC', 'Accounting', 18, true),
('998213', 'Compilation of financial statements services', 'SAC', 'Accounting', 18, true),
('998214', 'Bookkeeping services except tax returns', 'SAC', 'Accounting', 18, true),
('998215', 'Payroll services', 'SAC', 'Accounting', 18, true),
('998221', 'Corporate tax consulting and preparation services', 'SAC', 'Tax Services', 18, true),
('998222', 'Individual tax preparation and planning services', 'SAC', 'Tax Services', 18, true),

-- Education & Training
('999291', 'Commercial training and coaching services', 'SAC', 'Training', 18, true),
('999292', 'Educational support services', 'SAC', 'Training', 18, true),

-- Common HSN codes for goods
('8523', 'Recorded media for sound or other recorded phenomena', 'HSN', 'Software (Physical)', 18, true),
('85234100', 'Optical media - Unrecorded', 'HSN', 'Software (Physical)', 18, false),
('85234920', 'Optical media - Pre-recorded software', 'HSN', 'Software (Physical)', 18, true),
('8471', 'Automatic data processing machines (computers)', 'HSN', 'Hardware', 18, true),
('8473', 'Parts and accessories for computers', 'HSN', 'Hardware', 18, true),
('8517', 'Telephone sets and other communication equipment', 'HSN', 'Hardware', 18, false),

-- Miscellaneous Services
('999611', 'Services furnished by business and employers organisations', 'SAC', 'Membership', 18, false),
('999612', 'Services furnished by professional organisations', 'SAC', 'Membership', 18, false),
('998599', 'Other support services n.e.c.', 'SAC', 'Support', 18, false);

-- Add comment for documentation
COMMENT ON TABLE hsn_sac_codes IS 'Reference table for HSN (goods) and SAC (services) codes with GST rates';
