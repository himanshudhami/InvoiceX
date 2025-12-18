-- Migration: 058_create_asset_requests.sql
-- Description: Creates the asset_requests table for employee asset request management
-- Date: 2024-12-18

-- Asset requests table
CREATE TABLE IF NOT EXISTS asset_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    employee_id UUID NOT NULL REFERENCES employees(id) ON DELETE CASCADE,

    -- Request details
    asset_type VARCHAR(100) NOT NULL,
    category VARCHAR(100) NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    justification TEXT,
    specifications TEXT,
    priority VARCHAR(20) NOT NULL DEFAULT 'normal',
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    quantity INTEGER NOT NULL DEFAULT 1,
    estimated_budget DECIMAL(12, 2),
    requested_by_date TIMESTAMP WITH TIME ZONE,

    -- Timestamps
    requested_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    -- Approval tracking
    approved_by UUID REFERENCES employees(id) ON DELETE SET NULL,
    approved_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    cancelled_at TIMESTAMP WITH TIME ZONE,
    cancellation_reason TEXT,

    -- Fulfillment tracking
    assigned_asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    fulfilled_by UUID REFERENCES employees(id) ON DELETE SET NULL,
    fulfilled_at TIMESTAMP WITH TIME ZONE,
    fulfillment_notes TEXT,

    -- Constraints
    CONSTRAINT chk_asset_request_status CHECK (status IN ('pending', 'in_progress', 'approved', 'rejected', 'fulfilled', 'cancelled')),
    CONSTRAINT chk_asset_request_priority CHECK (priority IN ('low', 'normal', 'high', 'urgent')),
    CONSTRAINT chk_asset_request_quantity CHECK (quantity > 0)
);

-- Indexes for common queries
CREATE INDEX IF NOT EXISTS idx_asset_requests_company ON asset_requests(company_id);
CREATE INDEX IF NOT EXISTS idx_asset_requests_employee ON asset_requests(employee_id);
CREATE INDEX IF NOT EXISTS idx_asset_requests_status ON asset_requests(status);
CREATE INDEX IF NOT EXISTS idx_asset_requests_company_status ON asset_requests(company_id, status);
CREATE INDEX IF NOT EXISTS idx_asset_requests_pending ON asset_requests(company_id) WHERE status = 'pending';
CREATE INDEX IF NOT EXISTS idx_asset_requests_unfulfilled ON asset_requests(company_id) WHERE status = 'approved' AND fulfilled_at IS NULL;

-- Trigger to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_asset_requests_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_asset_requests_updated_at ON asset_requests;
CREATE TRIGGER trg_asset_requests_updated_at
    BEFORE UPDATE ON asset_requests
    FOR EACH ROW
    EXECUTE FUNCTION update_asset_requests_updated_at();

-- Comments
COMMENT ON TABLE asset_requests IS 'Employee asset requests for IT equipment, furniture, etc.';
COMMENT ON COLUMN asset_requests.asset_type IS 'Type of asset requested (e.g., laptop, monitor, desk)';
COMMENT ON COLUMN asset_requests.category IS 'Category of asset (IT Equipment, Office Furniture, Peripherals, etc.)';
COMMENT ON COLUMN asset_requests.status IS 'Request status: pending, in_progress, approved, rejected, fulfilled, cancelled';
COMMENT ON COLUMN asset_requests.priority IS 'Request priority: low, normal, high, urgent';
COMMENT ON COLUMN asset_requests.assigned_asset_id IS 'The asset assigned to fulfill this request (after fulfillment)';
