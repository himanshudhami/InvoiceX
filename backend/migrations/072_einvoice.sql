-- E-Invoice IRP Integration Schema
-- Supports both domestic (B2B, B2C, SEZ) and export invoices
-- GSP Providers: ClearTax (IRP 4), IRIS (IRP 6)

-- ============================================================================
-- E-Invoice Credentials (per company, encrypted secrets)
-- ============================================================================
CREATE TABLE IF NOT EXISTS einvoice_credentials (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- GSP Provider Configuration
    gsp_provider VARCHAR(50) NOT NULL DEFAULT 'cleartax', -- cleartax, iris, nic_direct
    environment VARCHAR(20) NOT NULL DEFAULT 'sandbox', -- sandbox, production

    -- API Credentials (secrets should be encrypted at application level)
    client_id VARCHAR(255),
    client_secret VARCHAR(500), -- Encrypted
    username VARCHAR(255),
    password VARCHAR(500), -- Encrypted

    -- Token Management
    auth_token TEXT, -- Encrypted JWT token
    token_expiry TIMESTAMP WITH TIME ZONE,
    sek VARCHAR(500), -- Session Encryption Key (for NIC direct)

    -- Configuration
    auto_generate_irn BOOLEAN DEFAULT FALSE,
    auto_cancel_on_void BOOLEAN DEFAULT FALSE,
    generate_eway_bill BOOLEAN DEFAULT FALSE,
    einvoice_threshold DECIMAL(18,2) DEFAULT 500000000, -- 5 Cr default (in paise or smallest unit)

    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_einvoice_creds_company_env UNIQUE(company_id, environment)
);

CREATE INDEX idx_einvoice_creds_company ON einvoice_credentials(company_id);
CREATE INDEX idx_einvoice_creds_active ON einvoice_credentials(is_active) WHERE is_active = TRUE;

-- ============================================================================
-- E-Invoice Audit Log (immutable audit trail)
-- ============================================================================
CREATE TABLE IF NOT EXISTS einvoice_audit_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    invoice_id UUID REFERENCES invoices(id) ON DELETE SET NULL,

    -- Action Details
    action_type VARCHAR(50) NOT NULL, -- generate_irn, cancel_irn, get_irn_by_docno, get_ewaybill, auth
    request_timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Request/Response (for debugging and compliance)
    request_payload JSONB,
    request_hash VARCHAR(64), -- SHA256 of request for integrity
    response_status VARCHAR(20), -- success, error, timeout
    response_payload JSONB,
    response_time_ms INTEGER,

    -- IRN Details (for quick lookup)
    irn VARCHAR(100),
    ack_number VARCHAR(50),
    ack_date TIMESTAMP WITH TIME ZONE,

    -- Error Tracking
    error_code VARCHAR(50),
    error_message TEXT,

    -- Metadata
    gsp_provider VARCHAR(50),
    environment VARCHAR(20),
    api_version VARCHAR(20),
    user_id UUID,
    ip_address VARCHAR(50),

    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_einvoice_audit_company ON einvoice_audit_log(company_id);
CREATE INDEX idx_einvoice_audit_invoice ON einvoice_audit_log(invoice_id);
CREATE INDEX idx_einvoice_audit_irn ON einvoice_audit_log(irn) WHERE irn IS NOT NULL;
CREATE INDEX idx_einvoice_audit_action ON einvoice_audit_log(action_type, created_at DESC);
CREATE INDEX idx_einvoice_audit_errors ON einvoice_audit_log(error_code) WHERE error_code IS NOT NULL;

-- ============================================================================
-- E-Invoice Queue (async processing with retry)
-- ============================================================================
CREATE TABLE IF NOT EXISTS einvoice_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    invoice_id UUID NOT NULL REFERENCES invoices(id) ON DELETE CASCADE,

    -- Queue Management
    action_type VARCHAR(50) NOT NULL, -- generate_irn, cancel_irn, retry_failed
    priority INTEGER NOT NULL DEFAULT 5, -- 1=highest, 10=lowest
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, processing, completed, failed, cancelled

    -- Retry Logic
    retry_count INTEGER DEFAULT 0,
    max_retries INTEGER DEFAULT 3,
    next_retry_at TIMESTAMP WITH TIME ZONE,

    -- Processing Info
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    processor_id VARCHAR(100), -- Worker ID for distributed processing

    -- Error Tracking
    error_code VARCHAR(50),
    error_message TEXT,

    -- Payload
    request_payload JSONB,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_einvoice_queue_pending ON einvoice_queue(status, priority, created_at)
    WHERE status IN ('pending', 'processing');
CREATE INDEX idx_einvoice_queue_retry ON einvoice_queue(next_retry_at)
    WHERE status = 'pending' AND next_retry_at IS NOT NULL;
CREATE INDEX idx_einvoice_queue_invoice ON einvoice_queue(invoice_id);
CREATE INDEX idx_einvoice_queue_company ON einvoice_queue(company_id);

-- ============================================================================
-- Additional Invoice Fields for E-Invoice
-- ============================================================================

-- Signed Invoice JSON from IRP
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_signed_json JSONB;

-- E-Invoice status tracking
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_status VARCHAR(30) DEFAULT 'not_applicable';
-- Values: not_applicable, pending, generated, cancelled, failed

-- Cancellation details
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_cancel_date TIMESTAMP WITH TIME ZONE;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS e_invoice_cancel_reason VARCHAR(255);

-- E-way bill (generated along with e-invoice)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS eway_bill_number VARCHAR(50);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS eway_bill_date TIMESTAMP WITH TIME ZONE;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS eway_bill_valid_until TIMESTAMP WITH TIME ZONE;

-- Export-specific fields (for EXPWP, EXPWOP invoice types)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS export_type VARCHAR(30);
-- Values: EXPWP (with payment), EXPWOP (without payment/under bond)

ALTER TABLE invoices ADD COLUMN IF NOT EXISTS port_code VARCHAR(10);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS shipping_bill_number VARCHAR(50);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS shipping_bill_date DATE;
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS export_duty DECIMAL(18,2) DEFAULT 0;

-- Foreign currency for exports
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS foreign_currency VARCHAR(10);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS exchange_rate DECIMAL(18,6);
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS foreign_currency_amount DECIMAL(18,2);

-- SEZ-specific fields
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS sez_category VARCHAR(30);
-- Values: SEZWP (with payment), SEZWOP (without payment)

-- B2C specific (for e-invoice exemption tracking)
ALTER TABLE invoices ADD COLUMN IF NOT EXISTS b2c_large BOOLEAN DEFAULT FALSE;
-- TRUE if B2C invoice > 2.5L (requires special handling)

-- Comments for documentation
COMMENT ON TABLE einvoice_credentials IS 'Stores GSP API credentials for e-invoice integration (secrets encrypted)';
COMMENT ON TABLE einvoice_audit_log IS 'Immutable audit log for all e-invoice API interactions';
COMMENT ON TABLE einvoice_queue IS 'Queue for async e-invoice processing with retry support';

COMMENT ON COLUMN invoices.e_invoice_status IS 'E-invoice status: not_applicable, pending, generated, cancelled, failed';
COMMENT ON COLUMN invoices.export_type IS 'Export invoice type: EXPWP (with payment), EXPWOP (without payment/under bond)';
COMMENT ON COLUMN invoices.sez_category IS 'SEZ invoice category: SEZWP (with payment), SEZWOP (without payment)';
COMMENT ON COLUMN invoices.b2c_large IS 'TRUE if B2C invoice exceeds 2.5 lakh threshold';

-- ============================================================================
-- Trigger to update updated_at
-- ============================================================================
CREATE OR REPLACE FUNCTION update_einvoice_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_einvoice_credentials_updated_at
    BEFORE UPDATE ON einvoice_credentials
    FOR EACH ROW EXECUTE FUNCTION update_einvoice_updated_at();

CREATE TRIGGER trigger_einvoice_queue_updated_at
    BEFORE UPDATE ON einvoice_queue
    FOR EACH ROW EXECUTE FUNCTION update_einvoice_updated_at();

-- ============================================================================
-- E-Invoice Type Mapping Reference (for documentation)
-- ============================================================================
-- | Invoice Type      | supply_type  | e_invoice_type | Notes                    |
-- |-------------------|--------------|----------------|--------------------------|
-- | domestic_b2b      | intra_state  | B2B            | CGST + SGST              |
-- | domestic_b2b      | inter_state  | B2B            | IGST                     |
-- | domestic_b2c      | intra_state  | B2C            | No e-invoice (< 5Cr)     |
-- | domestic_b2c      | inter_state  | B2CL           | Large invoice > 2.5L     |
-- | sez               | sez          | SEZWP/SEZWOP   | Zero-rated, LUT/Bond     |
-- | export            | export       | EXPWP/EXPWOP   | Zero-rated, shipping bill|
-- | deemed_export     | deemed       | DEXP           | Deemed export            |
-- ============================================================================
