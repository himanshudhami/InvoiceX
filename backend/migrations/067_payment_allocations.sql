-- Migration: Payment Allocations and Bank Transaction Matching
-- Purpose: Enable partial payment tracking, advance allocations, and split bank reconciliation
--
-- Key Changes:
-- 1. payment_allocations: Track how payments are allocated to invoices (partial payments, advances)
-- 2. bank_transaction_matches: Enable split reconciliation of bank transactions
-- 3. Add bank_account_id to payments for bank linkage
-- 4. Add reconciliation fields to payments

-- ============================================
-- PAYMENT ALLOCATIONS TABLE
-- ============================================
-- Tracks how each payment is allocated to invoices
-- Supports: partial payments, advance payments, over-payments, refunds

CREATE TABLE IF NOT EXISTS payment_allocations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    payment_id UUID NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    invoice_id UUID REFERENCES invoices(id) ON DELETE SET NULL,

    -- Amount allocated to this invoice
    allocated_amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(10) DEFAULT 'INR',
    amount_in_inr DECIMAL(18,2),
    exchange_rate DECIMAL(18,6) DEFAULT 1,

    -- Allocation details
    allocation_date DATE NOT NULL DEFAULT CURRENT_DATE,
    allocation_type VARCHAR(50) NOT NULL DEFAULT 'invoice_settlement',
    -- Types: invoice_settlement, advance_adjustment, credit_note, refund, write_off

    -- TDS handling (when TDS is allocated separately)
    tds_allocated DECIMAL(18,2) DEFAULT 0,

    -- Audit fields
    notes TEXT,
    created_by UUID,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Ensure positive allocation amounts
    CONSTRAINT chk_allocation_amount_positive CHECK (allocated_amount > 0),
    CONSTRAINT chk_tds_non_negative CHECK (tds_allocated >= 0)
);

COMMENT ON TABLE payment_allocations IS 'Tracks allocation of payments to invoices for partial payment and advance tracking';
COMMENT ON COLUMN payment_allocations.allocation_type IS 'Types: invoice_settlement, advance_adjustment, credit_note, refund, write_off';
COMMENT ON COLUMN payment_allocations.tds_allocated IS 'TDS portion of this allocation (from payment.tds_amount)';

-- ============================================
-- BANK TRANSACTION MATCHES TABLE
-- ============================================
-- Enables split reconciliation of bank transactions to multiple payments/expenses

CREATE TABLE IF NOT EXISTS bank_transaction_matches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    bank_transaction_id UUID NOT NULL REFERENCES bank_transactions(id) ON DELETE CASCADE,

    -- What this match is linked to
    matched_type VARCHAR(50) NOT NULL,
    -- Types: payment, expense, transfer, tax_payment, salary, contractor_payment
    matched_id UUID NOT NULL,

    -- Match amount (allows partial matches)
    matched_amount DECIMAL(18,2) NOT NULL,

    -- Match metadata
    matched_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    matched_by VARCHAR(255),
    match_method VARCHAR(50) DEFAULT 'manual',
    -- Methods: manual, auto_reference, auto_amount, rule_based
    confidence_score DECIMAL(5,2),

    -- Audit
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,

    -- Ensure positive match amounts
    CONSTRAINT chk_match_amount_positive CHECK (matched_amount > 0)
);

COMMENT ON TABLE bank_transaction_matches IS 'Links bank transactions to payments/expenses for split reconciliation';
COMMENT ON COLUMN bank_transaction_matches.matched_type IS 'Types: payment, expense, transfer, tax_payment, salary, contractor_payment';
COMMENT ON COLUMN bank_transaction_matches.match_method IS 'Methods: manual, auto_reference, auto_amount, rule_based';

-- ============================================
-- ENHANCE PAYMENTS TABLE
-- ============================================

-- Add bank account linkage
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS bank_account_id UUID REFERENCES bank_accounts(id);

-- Add reconciliation tracking
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS is_reconciled BOOLEAN DEFAULT false;

ALTER TABLE payments
ADD COLUMN IF NOT EXISTS reconciled_at TIMESTAMP;

ALTER TABLE payments
ADD COLUMN IF NOT EXISTS reconciled_by VARCHAR(255);

-- Add unallocated amount tracking (calculated field for convenience)
ALTER TABLE payments
ADD COLUMN IF NOT EXISTS unallocated_amount DECIMAL(18,2);

COMMENT ON COLUMN payments.bank_account_id IS 'Bank account that received this payment';
COMMENT ON COLUMN payments.is_reconciled IS 'Whether this payment is reconciled with bank statement';
COMMENT ON COLUMN payments.reconciled_at IS 'When the payment was reconciled';
COMMENT ON COLUMN payments.unallocated_amount IS 'Amount not yet allocated to invoices (amount - sum of allocations)';

-- ============================================
-- INDEXES
-- ============================================

-- Payment allocations indexes
CREATE INDEX IF NOT EXISTS idx_payment_allocations_payment_id
    ON payment_allocations(payment_id);

CREATE INDEX IF NOT EXISTS idx_payment_allocations_invoice_id
    ON payment_allocations(invoice_id);

CREATE INDEX IF NOT EXISTS idx_payment_allocations_company_id
    ON payment_allocations(company_id);

CREATE INDEX IF NOT EXISTS idx_payment_allocations_date
    ON payment_allocations(allocation_date);

CREATE INDEX IF NOT EXISTS idx_payment_allocations_type
    ON payment_allocations(allocation_type);

-- Composite index for company reporting
CREATE INDEX IF NOT EXISTS idx_payment_allocations_company_date
    ON payment_allocations(company_id, allocation_date);

-- Bank transaction matches indexes
CREATE INDEX IF NOT EXISTS idx_bank_tx_matches_transaction_id
    ON bank_transaction_matches(bank_transaction_id);

CREATE INDEX IF NOT EXISTS idx_bank_tx_matches_matched
    ON bank_transaction_matches(matched_type, matched_id);

CREATE INDEX IF NOT EXISTS idx_bank_tx_matches_company_id
    ON bank_transaction_matches(company_id);

-- Payments indexes for new columns
CREATE INDEX IF NOT EXISTS idx_payments_bank_account_id
    ON payments(bank_account_id);

CREATE INDEX IF NOT EXISTS idx_payments_is_reconciled
    ON payments(is_reconciled)
    WHERE is_reconciled = false;

-- ============================================
-- FUNCTION: Calculate unallocated amount
-- ============================================

CREATE OR REPLACE FUNCTION calculate_unallocated_amount(p_payment_id UUID)
RETURNS DECIMAL(18,2) AS $$
DECLARE
    total_allocated DECIMAL(18,2);
    payment_amount DECIMAL(18,2);
BEGIN
    -- Get payment amount
    SELECT COALESCE(amount, 0) INTO payment_amount
    FROM payments WHERE id = p_payment_id;

    -- Get total allocated
    SELECT COALESCE(SUM(allocated_amount), 0) INTO total_allocated
    FROM payment_allocations WHERE payment_id = p_payment_id;

    RETURN payment_amount - total_allocated;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION calculate_unallocated_amount(UUID) IS 'Calculates remaining unallocated amount for a payment';

-- ============================================
-- TRIGGER: Update unallocated amount on payments
-- ============================================

CREATE OR REPLACE FUNCTION update_payment_unallocated_amount()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        UPDATE payments
        SET unallocated_amount = calculate_unallocated_amount(OLD.payment_id),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = OLD.payment_id;
        RETURN OLD;
    ELSE
        UPDATE payments
        SET unallocated_amount = calculate_unallocated_amount(NEW.payment_id),
            updated_at = CURRENT_TIMESTAMP
        WHERE id = NEW.payment_id;
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_payment_unallocated ON payment_allocations;

CREATE TRIGGER trg_update_payment_unallocated
AFTER INSERT OR UPDATE OR DELETE ON payment_allocations
FOR EACH ROW
EXECUTE FUNCTION update_payment_unallocated_amount();

-- ============================================
-- FUNCTION: Calculate bank transaction matched amount
-- ============================================

CREATE OR REPLACE FUNCTION calculate_bank_tx_matched_amount(p_bank_transaction_id UUID)
RETURNS DECIMAL(18,2) AS $$
DECLARE
    total_matched DECIMAL(18,2);
BEGIN
    SELECT COALESCE(SUM(matched_amount), 0) INTO total_matched
    FROM bank_transaction_matches WHERE bank_transaction_id = p_bank_transaction_id;

    RETURN total_matched;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION calculate_bank_tx_matched_amount(UUID) IS 'Calculates total matched amount for a bank transaction';

-- ============================================
-- TRIGGER: Update bank transaction reconciliation status
-- ============================================

CREATE OR REPLACE FUNCTION update_bank_tx_reconciliation()
RETURNS TRIGGER AS $$
DECLARE
    tx_amount DECIMAL(18,2);
    matched_total DECIMAL(18,2);
BEGIN
    IF TG_OP = 'DELETE' THEN
        SELECT ABS(COALESCE(amount, 0)) INTO tx_amount
        FROM bank_transactions WHERE id = OLD.bank_transaction_id;

        matched_total := calculate_bank_tx_matched_amount(OLD.bank_transaction_id);

        UPDATE bank_transactions
        SET is_reconciled = (matched_total >= tx_amount),
            reconciled_at = CASE WHEN matched_total >= tx_amount THEN CURRENT_TIMESTAMP ELSE NULL END,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = OLD.bank_transaction_id;
        RETURN OLD;
    ELSE
        SELECT ABS(COALESCE(amount, 0)) INTO tx_amount
        FROM bank_transactions WHERE id = NEW.bank_transaction_id;

        matched_total := calculate_bank_tx_matched_amount(NEW.bank_transaction_id);

        UPDATE bank_transactions
        SET is_reconciled = (matched_total >= tx_amount),
            reconciled_at = CASE WHEN matched_total >= tx_amount THEN CURRENT_TIMESTAMP ELSE NULL END,
            updated_at = CURRENT_TIMESTAMP
        WHERE id = NEW.bank_transaction_id;
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_bank_tx_reconciliation ON bank_transaction_matches;

CREATE TRIGGER trg_update_bank_tx_reconciliation
AFTER INSERT OR UPDATE OR DELETE ON bank_transaction_matches
FOR EACH ROW
EXECUTE FUNCTION update_bank_tx_reconciliation();

-- ============================================
-- VIEW: Payment allocation summary
-- ============================================

CREATE OR REPLACE VIEW v_payment_allocation_summary AS
SELECT
    p.id AS payment_id,
    p.company_id,
    p.customer_id,
    p.amount AS payment_amount,
    p.gross_amount,
    p.tds_amount,
    p.currency,
    p.payment_date,
    p.payment_type,
    COALESCE(SUM(pa.allocated_amount), 0) AS total_allocated,
    p.amount - COALESCE(SUM(pa.allocated_amount), 0) AS unallocated,
    COUNT(pa.id) AS allocation_count,
    p.is_reconciled,
    p.bank_account_id
FROM payments p
LEFT JOIN payment_allocations pa ON pa.payment_id = p.id
GROUP BY p.id;

COMMENT ON VIEW v_payment_allocation_summary IS 'Summary view of payment allocations with unallocated amounts';

-- ============================================
-- VIEW: Invoice payment status
-- ============================================

CREATE OR REPLACE VIEW v_invoice_payment_status AS
SELECT
    i.id AS invoice_id,
    i.company_id,
    i.customer_id,
    i.invoice_number,
    i.total_amount AS invoice_total,
    i.currency,
    i.status AS invoice_status,
    i.due_date,
    COALESCE(SUM(pa.allocated_amount), 0) AS total_paid,
    i.total_amount - COALESCE(SUM(pa.allocated_amount), 0) AS balance_due,
    CASE
        WHEN COALESCE(SUM(pa.allocated_amount), 0) = 0 THEN 'unpaid'
        WHEN COALESCE(SUM(pa.allocated_amount), 0) >= i.total_amount THEN 'paid'
        ELSE 'partial'
    END AS payment_status,
    COUNT(pa.id) AS payment_count,
    MAX(pa.allocation_date) AS last_payment_date
FROM invoices i
LEFT JOIN payment_allocations pa ON pa.invoice_id = i.id
GROUP BY i.id;

COMMENT ON VIEW v_invoice_payment_status IS 'Invoice payment status derived from actual allocations';

-- ============================================
-- BACKFILL: Initialize unallocated_amount for existing payments
-- ============================================

UPDATE payments
SET unallocated_amount = amount
WHERE unallocated_amount IS NULL;

-- ============================================
-- BACKFILL: Auto-create allocations for payments with invoice_id
-- ============================================
-- For existing payments that have an invoice_id, create allocation records

INSERT INTO payment_allocations (
    company_id,
    payment_id,
    invoice_id,
    allocated_amount,
    currency,
    amount_in_inr,
    allocation_date,
    allocation_type,
    tds_allocated,
    notes,
    created_at
)
SELECT
    p.company_id,
    p.id AS payment_id,
    p.invoice_id,
    p.amount AS allocated_amount,
    p.currency,
    CASE WHEN p.currency = 'INR' THEN p.amount ELSE NULL END AS amount_in_inr,
    p.payment_date AS allocation_date,
    'invoice_settlement' AS allocation_type,
    COALESCE(p.tds_amount, 0) AS tds_allocated,
    'Auto-created from existing payment-invoice link' AS notes,
    COALESCE(p.created_at, CURRENT_TIMESTAMP) AS created_at
FROM payments p
WHERE p.invoice_id IS NOT NULL
AND NOT EXISTS (
    SELECT 1 FROM payment_allocations pa
    WHERE pa.payment_id = p.id AND pa.invoice_id = p.invoice_id
);
