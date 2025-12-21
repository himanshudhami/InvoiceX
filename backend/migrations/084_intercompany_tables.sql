-- Migration: 084_intercompany_tables.sql
-- Purpose: Create tables for intercompany accounting and consolidation
-- Enables tracking of transactions between group companies for consolidated statements

-- ============================================================================
-- TABLE 1: Company Relationships
-- ============================================================================
-- Tracks parent-subsidiary, affiliate relationships between companies

CREATE TABLE IF NOT EXISTS company_relationships (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    child_company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    relationship_type VARCHAR(50) NOT NULL, -- 'subsidiary', 'associate', 'joint_venture', 'affiliate'
    ownership_percentage NUMERIC(5,2), -- e.g., 100.00 for wholly owned subsidiary
    effective_from DATE NOT NULL,
    effective_to DATE, -- NULL means currently active
    consolidation_method VARCHAR(50), -- 'full', 'proportionate', 'equity_method', 'none'
    functional_currency VARCHAR(3) DEFAULT 'INR',
    is_active BOOLEAN DEFAULT true,
    notes TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    -- Prevent self-referencing and duplicate relationships
    CONSTRAINT chk_no_self_relationship CHECK (parent_company_id != child_company_id),
    CONSTRAINT uq_company_relationship UNIQUE (parent_company_id, child_company_id, effective_from)
);

-- ============================================================================
-- TABLE 2: Intercompany Transactions
-- ============================================================================
-- Tracks all transactions between group companies for reconciliation

CREATE TABLE IF NOT EXISTS intercompany_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE, -- Recording company
    counterparty_company_id UUID NOT NULL REFERENCES companies(id), -- Other company in transaction
    transaction_date DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL, -- '2025-26'

    -- Transaction details
    transaction_type VARCHAR(50) NOT NULL, -- 'invoice', 'payment', 'allocation', 'journal', 'recharge'
    transaction_direction VARCHAR(10) NOT NULL, -- 'receivable' or 'payable'
    source_document_type VARCHAR(50), -- 'invoice', 'payment', 'journal_entry'
    source_document_id UUID,
    source_document_number VARCHAR(100),

    -- Amounts
    amount NUMERIC(18,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    exchange_rate NUMERIC(18,6) DEFAULT 1.0,
    amount_in_inr NUMERIC(18,2),

    -- GST details (if applicable)
    gst_amount NUMERIC(18,2) DEFAULT 0,
    is_gst_applicable BOOLEAN DEFAULT false,

    -- Journal entry references
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Reconciliation status
    is_reconciled BOOLEAN DEFAULT false,
    reconciled_at TIMESTAMP,
    reconciled_by UUID,
    counterparty_transaction_id UUID REFERENCES intercompany_transactions(id), -- Matching entry in counterparty books
    reconciliation_notes TEXT,

    -- Audit
    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    CONSTRAINT chk_ic_no_self_transaction CHECK (company_id != counterparty_company_id),
    CONSTRAINT chk_ic_direction CHECK (transaction_direction IN ('receivable', 'payable'))
);

-- ============================================================================
-- TABLE 3: Intercompany Balances
-- ============================================================================
-- Running balances between company pairs for quick reconciliation

CREATE TABLE IF NOT EXISTS intercompany_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    from_company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    to_company_id UUID NOT NULL REFERENCES companies(id),
    as_of_date DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL,

    -- Balance details
    balance_amount NUMERIC(18,2) NOT NULL DEFAULT 0, -- Positive = receivable, Negative = payable
    currency VARCHAR(3) DEFAULT 'INR',
    balance_in_inr NUMERIC(18,2),

    -- Activity summary
    opening_balance NUMERIC(18,2) DEFAULT 0,
    total_debits NUMERIC(18,2) DEFAULT 0, -- Increases to receivable
    total_credits NUMERIC(18,2) DEFAULT 0, -- Decreases to receivable
    transaction_count INT DEFAULT 0,
    last_transaction_date DATE,

    -- Reconciliation
    is_reconciled BOOLEAN DEFAULT false,
    counterparty_balance NUMERIC(18,2), -- Balance as per counterparty (should be opposite)
    difference NUMERIC(18,2), -- Calculated difference for reconciliation

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_icb_no_self_balance CHECK (from_company_id != to_company_id),
    CONSTRAINT uq_intercompany_balance UNIQUE (from_company_id, to_company_id, as_of_date)
);

-- ============================================================================
-- TABLE 4: Consolidation Eliminations
-- ============================================================================
-- Tracks elimination entries for consolidated financial statements

CREATE TABLE IF NOT EXISTS consolidation_eliminations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consolidation_period DATE NOT NULL, -- Month-end date for consolidation
    financial_year VARCHAR(10) NOT NULL,
    parent_company_id UUID NOT NULL REFERENCES companies(id),

    -- Elimination details
    elimination_type VARCHAR(50) NOT NULL, -- 'intercompany_revenue', 'intercompany_receivable', 'investment', 'dividend', 'unrealized_profit'
    from_company_id UUID REFERENCES companies(id),
    to_company_id UUID REFERENCES companies(id),

    -- Amounts
    elimination_amount NUMERIC(18,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',

    -- Related transactions
    source_transaction_ids UUID[], -- Array of intercompany_transactions.id

    -- Journal entry for elimination
    journal_entry_id UUID REFERENCES journal_entries(id),

    -- Status
    status VARCHAR(20) DEFAULT 'pending', -- 'pending', 'posted', 'reversed'
    posted_at TIMESTAMP,
    posted_by UUID,

    description TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Company relationships
CREATE INDEX IF NOT EXISTS idx_company_rel_parent ON company_relationships(parent_company_id);
CREATE INDEX IF NOT EXISTS idx_company_rel_child ON company_relationships(child_company_id);
CREATE INDEX IF NOT EXISTS idx_company_rel_active ON company_relationships(is_active) WHERE is_active = true;

-- Intercompany transactions
CREATE INDEX IF NOT EXISTS idx_ic_txn_company ON intercompany_transactions(company_id);
CREATE INDEX IF NOT EXISTS idx_ic_txn_counterparty ON intercompany_transactions(counterparty_company_id);
CREATE INDEX IF NOT EXISTS idx_ic_txn_date ON intercompany_transactions(transaction_date);
CREATE INDEX IF NOT EXISTS idx_ic_txn_fy ON intercompany_transactions(financial_year);
CREATE INDEX IF NOT EXISTS idx_ic_txn_source ON intercompany_transactions(source_document_type, source_document_id);
CREATE INDEX IF NOT EXISTS idx_ic_txn_unreconciled ON intercompany_transactions(is_reconciled) WHERE is_reconciled = false;

-- Intercompany balances
CREATE INDEX IF NOT EXISTS idx_icb_companies ON intercompany_balances(from_company_id, to_company_id);
CREATE INDEX IF NOT EXISTS idx_icb_date ON intercompany_balances(as_of_date);
CREATE INDEX IF NOT EXISTS idx_icb_fy ON intercompany_balances(financial_year);

-- Consolidation eliminations
CREATE INDEX IF NOT EXISTS idx_consol_elim_period ON consolidation_eliminations(consolidation_period);
CREATE INDEX IF NOT EXISTS idx_consol_elim_parent ON consolidation_eliminations(parent_company_id);
CREATE INDEX IF NOT EXISTS idx_consol_elim_status ON consolidation_eliminations(status);

-- ============================================================================
-- VIEWS
-- ============================================================================

-- View: Intercompany balance summary with counterparty matching
CREATE OR REPLACE VIEW v_intercompany_reconciliation AS
SELECT
    b1.from_company_id,
    c1.name as from_company_name,
    b1.to_company_id,
    c2.name as to_company_name,
    b1.as_of_date,
    b1.financial_year,
    b1.balance_amount as our_balance,
    COALESCE(b2.balance_amount * -1, 0) as their_balance, -- Opposite sign
    b1.balance_amount - COALESCE(b2.balance_amount * -1, 0) as difference,
    CASE
        WHEN ABS(b1.balance_amount - COALESCE(b2.balance_amount * -1, 0)) < 0.01 THEN 'Matched'
        ELSE 'Unmatched'
    END as reconciliation_status,
    b1.transaction_count,
    b1.last_transaction_date
FROM intercompany_balances b1
JOIN companies c1 ON c1.id = b1.from_company_id
JOIN companies c2 ON c2.id = b1.to_company_id
LEFT JOIN intercompany_balances b2 ON
    b2.from_company_id = b1.to_company_id
    AND b2.to_company_id = b1.from_company_id
    AND b2.as_of_date = b1.as_of_date;

-- View: Active company group structure
CREATE OR REPLACE VIEW v_company_group_structure AS
SELECT
    cr.parent_company_id,
    p.name as parent_name,
    cr.child_company_id,
    c.name as child_name,
    cr.relationship_type,
    cr.ownership_percentage,
    cr.consolidation_method,
    cr.effective_from,
    cr.effective_to
FROM company_relationships cr
JOIN companies p ON p.id = cr.parent_company_id
JOIN companies c ON c.id = cr.child_company_id
WHERE cr.is_active = true
ORDER BY p.name, cr.ownership_percentage DESC;

