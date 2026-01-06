-- Migration: Vendor Payments and Bill-wise Allocations
-- Purpose: Track outgoing payments to vendors with TDS deduction and bill-wise allocation (Payment voucher in Tally)

-- ============================================
-- VENDOR PAYMENTS TABLE
-- ============================================
-- Outgoing payments to vendors

CREATE TABLE IF NOT EXISTS vendor_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    vendor_id UUID REFERENCES vendors(id) ON DELETE SET NULL,
    bank_account_id UUID REFERENCES bank_accounts(id) ON DELETE SET NULL,

    -- ==================== Payment Details ====================

    payment_date DATE NOT NULL,

    -- Net amount paid (after TDS)
    amount DECIMAL(18,2) NOT NULL,

    -- Gross amount before TDS
    gross_amount DECIMAL(18,2),

    -- Amount in INR (for forex)
    amount_in_inr DECIMAL(18,2),

    -- Currency
    currency VARCHAR(3) DEFAULT 'INR',

    -- Payment method: bank_transfer, cheque, cash, neft, rtgs, upi, demand_draft
    payment_method VARCHAR(30),

    -- UTR number, cheque number, or reference
    reference_number VARCHAR(100),

    -- Cheque details
    cheque_number VARCHAR(20),
    cheque_date DATE,

    -- Description/narration
    notes TEXT,
    description TEXT,

    -- ==================== Payment Classification ====================

    -- Type: bill_payment, advance_paid, expense_reimbursement, refund_paid
    payment_type VARCHAR(30) DEFAULT 'bill_payment',

    -- Status: draft, pending_approval, approved, processed, cancelled
    status VARCHAR(30) DEFAULT 'draft',

    -- ==================== TDS Deduction ====================

    -- Whether TDS was deducted
    tds_applicable BOOLEAN DEFAULT FALSE,

    -- TDS Section: 194C, 194J, 194H, 194I, 194A, 194Q
    tds_section VARCHAR(10),

    -- TDS Rate
    tds_rate DECIMAL(5,2),

    -- TDS Amount deducted
    tds_amount DECIMAL(18,2) DEFAULT 0,

    -- TDS deposited to government
    tds_deposited BOOLEAN DEFAULT FALSE,

    -- TDS challan number
    tds_challan_number VARCHAR(50),

    -- TDS deposit date
    tds_deposit_date DATE,

    -- ==================== Financial Year ====================

    -- Indian financial year: 2024-25 format
    financial_year VARCHAR(10),

    -- ==================== Ledger Posting ====================

    is_posted BOOLEAN DEFAULT FALSE,
    posted_journal_id UUID REFERENCES journal_entries(id),
    posted_at TIMESTAMP,

    -- ==================== Bank Reconciliation ====================

    bank_transaction_id UUID REFERENCES bank_transactions(id),
    is_reconciled BOOLEAN DEFAULT FALSE,
    reconciled_at TIMESTAMP,

    -- ==================== Approval Workflow ====================

    approved_by UUID,
    approved_at TIMESTAMP,

    -- ==================== Tally Migration ====================

    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_migration_batch_id UUID,

    -- ==================== Timestamps ====================

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- ==================== Constraints ====================

    CONSTRAINT chk_vp_payment_type CHECK (payment_type IN ('bill_payment', 'advance_paid', 'expense_reimbursement', 'refund_paid')),
    CONSTRAINT chk_vp_status CHECK (status IN ('draft', 'pending_approval', 'approved', 'processed', 'cancelled')),
    CONSTRAINT chk_vp_tds_section CHECK (tds_section IS NULL OR tds_section IN ('194A', '194C', '194H', '194I', '194J', '194Q', '194R', '194S')),
    CONSTRAINT chk_vp_payment_method CHECK (payment_method IS NULL OR payment_method IN ('bank_transfer', 'cheque', 'cash', 'neft', 'rtgs', 'upi', 'demand_draft', 'imps'))
);

-- ============================================
-- VENDOR PAYMENT ALLOCATIONS TABLE
-- ============================================
-- Bill-wise allocation (mirrors Tally's bill allocation)

CREATE TABLE IF NOT EXISTS vendor_payment_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    vendor_payment_id UUID NOT NULL REFERENCES vendor_payments(id) ON DELETE CASCADE,
    vendor_invoice_id UUID REFERENCES vendor_invoices(id) ON DELETE SET NULL,

    -- ==================== Allocation Amount ====================

    -- Amount allocated to this invoice
    allocated_amount DECIMAL(18,2) NOT NULL,

    -- TDS portion allocated
    tds_allocated DECIMAL(18,2) DEFAULT 0,

    -- Currency
    currency VARCHAR(3) DEFAULT 'INR',

    -- Amount in INR (for forex)
    amount_in_inr DECIMAL(18,2),

    -- Exchange rate
    exchange_rate DECIMAL(18,6) DEFAULT 1,

    -- ==================== Allocation Details ====================

    -- Date of allocation
    allocation_date DATE NOT NULL,

    -- Type: bill_settlement (Agst Ref), advance_adjustment (Advance), debit_note, on_account
    allocation_type VARCHAR(30) DEFAULT 'bill_settlement',

    -- Tally bill reference name (for migration)
    tally_bill_ref VARCHAR(255),

    -- Notes
    notes TEXT,

    -- ==================== Audit ====================

    created_by UUID,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    -- ==================== Constraints ====================

    CONSTRAINT chk_vpa_allocation_type CHECK (allocation_type IN ('bill_settlement', 'advance_adjustment', 'debit_note', 'on_account'))
);

-- ============================================
-- INDEXES
-- ============================================

-- Vendor Payments
CREATE INDEX IF NOT EXISTS idx_vp_company_id ON vendor_payments(company_id);
CREATE INDEX IF NOT EXISTS idx_vp_vendor_id ON vendor_payments(vendor_id);
CREATE INDEX IF NOT EXISTS idx_vp_bank_account_id ON vendor_payments(bank_account_id);
CREATE INDEX IF NOT EXISTS idx_vp_payment_date ON vendor_payments(company_id, payment_date);
CREATE INDEX IF NOT EXISTS idx_vp_status ON vendor_payments(company_id, status);
CREATE INDEX IF NOT EXISTS idx_vp_is_posted ON vendor_payments(company_id, is_posted);
CREATE INDEX IF NOT EXISTS idx_vp_is_reconciled ON vendor_payments(company_id, is_reconciled);
CREATE INDEX IF NOT EXISTS idx_vp_financial_year ON vendor_payments(company_id, financial_year);
CREATE INDEX IF NOT EXISTS idx_vp_tds_applicable ON vendor_payments(company_id, tds_applicable) WHERE tds_applicable = TRUE;
CREATE INDEX IF NOT EXISTS idx_vp_tds_deposited ON vendor_payments(company_id, tds_deposited) WHERE tds_applicable = TRUE AND tds_deposited = FALSE;
CREATE INDEX IF NOT EXISTS idx_vp_tally_guid ON vendor_payments(tally_voucher_guid) WHERE tally_voucher_guid IS NOT NULL;

-- Vendor Payment Allocations
CREATE INDEX IF NOT EXISTS idx_vpa_company_id ON vendor_payment_allocations(company_id);
CREATE INDEX IF NOT EXISTS idx_vpa_vendor_payment_id ON vendor_payment_allocations(vendor_payment_id);
CREATE INDEX IF NOT EXISTS idx_vpa_vendor_invoice_id ON vendor_payment_allocations(vendor_invoice_id);
CREATE INDEX IF NOT EXISTS idx_vpa_allocation_type ON vendor_payment_allocations(allocation_type);

-- ============================================
-- COMMENTS
-- ============================================

COMMENT ON TABLE vendor_payments IS 'Outgoing payments to vendors with TDS deduction tracking (Payment voucher in Tally)';
COMMENT ON COLUMN vendor_payments.amount IS 'Net amount paid = gross_amount - tds_amount';
COMMENT ON COLUMN vendor_payments.tds_deposited IS 'Whether TDS has been deposited to government via challan';
COMMENT ON COLUMN vendor_payments.financial_year IS 'Indian FY format: 2024-25 (April to March)';

COMMENT ON TABLE vendor_payment_allocations IS 'Bill-wise allocation of payments to vendor invoices (mirrors Tally bill allocation)';
COMMENT ON COLUMN vendor_payment_allocations.allocation_type IS 'bill_settlement=Agst Ref, advance_adjustment=Advance, on_account=On Account in Tally';
COMMENT ON COLUMN vendor_payment_allocations.tally_bill_ref IS 'Original Tally bill reference name for migration';

-- ============================================
-- TRIGGERS FOR updated_at
-- ============================================

CREATE OR REPLACE FUNCTION update_vendor_payments_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_vendor_payments_updated_at ON vendor_payments;
CREATE TRIGGER trg_vendor_payments_updated_at
    BEFORE UPDATE ON vendor_payments
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_payments_updated_at();

CREATE OR REPLACE FUNCTION update_vendor_payment_allocations_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_vendor_payment_allocations_updated_at ON vendor_payment_allocations;
CREATE TRIGGER trg_vendor_payment_allocations_updated_at
    BEFORE UPDATE ON vendor_payment_allocations
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_payment_allocations_updated_at();

-- ============================================
-- TRIGGER TO AUTO-CALCULATE FINANCIAL YEAR
-- ============================================

CREATE OR REPLACE FUNCTION set_vendor_payment_financial_year()
RETURNS TRIGGER AS $$
BEGIN
    -- Indian FY: April to March
    IF EXTRACT(MONTH FROM NEW.payment_date) >= 4 THEN
        NEW.financial_year := EXTRACT(YEAR FROM NEW.payment_date)::TEXT || '-' ||
            LPAD(((EXTRACT(YEAR FROM NEW.payment_date)::INTEGER + 1) % 100)::TEXT, 2, '0');
    ELSE
        NEW.financial_year := (EXTRACT(YEAR FROM NEW.payment_date)::INTEGER - 1)::TEXT || '-' ||
            LPAD((EXTRACT(YEAR FROM NEW.payment_date)::INTEGER % 100)::TEXT, 2, '0');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_set_vendor_payment_fy ON vendor_payments;
CREATE TRIGGER trg_set_vendor_payment_fy
    BEFORE INSERT OR UPDATE OF payment_date ON vendor_payments
    FOR EACH ROW
    EXECUTE FUNCTION set_vendor_payment_financial_year();

-- ============================================
-- TRIGGER TO UPDATE VENDOR INVOICE PAID AMOUNT
-- ============================================

CREATE OR REPLACE FUNCTION update_vendor_invoice_paid_amount()
RETURNS TRIGGER AS $$
DECLARE
    v_invoice_id UUID;
BEGIN
    -- Get the invoice ID (use NEW for insert/update, OLD for delete)
    IF TG_OP = 'DELETE' THEN
        v_invoice_id := OLD.vendor_invoice_id;
    ELSE
        v_invoice_id := NEW.vendor_invoice_id;
    END IF;

    -- Update paid amount on vendor invoice
    IF v_invoice_id IS NOT NULL THEN
        UPDATE vendor_invoices
        SET
            paid_amount = COALESCE((
                SELECT SUM(allocated_amount + tds_allocated)
                FROM vendor_payment_allocations vpa
                JOIN vendor_payments vp ON vpa.vendor_payment_id = vp.id
                WHERE vpa.vendor_invoice_id = v_invoice_id
                AND vp.status NOT IN ('draft', 'cancelled')
            ), 0),
            status = CASE
                WHEN COALESCE((
                    SELECT SUM(allocated_amount + tds_allocated)
                    FROM vendor_payment_allocations vpa
                    JOIN vendor_payments vp ON vpa.vendor_payment_id = vp.id
                    WHERE vpa.vendor_invoice_id = v_invoice_id
                    AND vp.status NOT IN ('draft', 'cancelled')
                ), 0) >= total_amount THEN 'paid'
                WHEN COALESCE((
                    SELECT SUM(allocated_amount + tds_allocated)
                    FROM vendor_payment_allocations vpa
                    JOIN vendor_payments vp ON vpa.vendor_payment_id = vp.id
                    WHERE vpa.vendor_invoice_id = v_invoice_id
                    AND vp.status NOT IN ('draft', 'cancelled')
                ), 0) > 0 THEN 'partially_paid'
                ELSE status
            END,
            updated_at = NOW()
        WHERE id = v_invoice_id;
    END IF;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_vendor_invoice_paid ON vendor_payment_allocations;
CREATE TRIGGER trg_update_vendor_invoice_paid
    AFTER INSERT OR UPDATE OR DELETE ON vendor_payment_allocations
    FOR EACH ROW
    EXECUTE FUNCTION update_vendor_invoice_paid_amount();
