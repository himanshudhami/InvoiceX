-- Migration: General Ledger System
-- Purpose: Implement double-entry accounting with Chart of Accounts, Journal Entries, and Posting Rules
-- Follows Indian Schedule III standard for account classification

-- ============================================
-- CHART OF ACCOUNTS
-- ============================================
-- Hierarchical structure following Indian Schedule III

CREATE TABLE IF NOT EXISTS chart_of_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,

    -- Account identification
    account_code VARCHAR(20) NOT NULL,
    account_name VARCHAR(255) NOT NULL,

    -- Classification (Indian Schedule III)
    account_type VARCHAR(30) NOT NULL, -- asset, liability, equity, income, expense
    account_subtype VARCHAR(50), -- current_asset, fixed_asset, current_liability, etc.

    -- Hierarchy
    parent_account_id UUID REFERENCES chart_of_accounts(id),
    depth_level INTEGER NOT NULL DEFAULT 0,
    full_path TEXT, -- e.g., "1000.1100.1110" for nested accounts

    -- Indian compliance
    schedule_reference VARCHAR(20), -- Schedule III reference (I, II, III, etc.)
    gst_treatment VARCHAR(30), -- taxable, exempt, nil_rated, non_gst

    -- Control flags
    is_control_account BOOLEAN DEFAULT FALSE, -- e.g., Accounts Receivable, Accounts Payable
    is_system_account BOOLEAN DEFAULT FALSE, -- System-managed, cannot be deleted
    is_bank_account BOOLEAN DEFAULT FALSE,
    linked_bank_account_id UUID REFERENCES bank_accounts(id),

    -- Balance tracking
    normal_balance VARCHAR(10) NOT NULL, -- debit, credit
    opening_balance DECIMAL(18,2) DEFAULT 0,
    current_balance DECIMAL(18,2) DEFAULT 0,

    -- Display
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    description TEXT,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    -- Constraints
    CONSTRAINT uq_account_code_company UNIQUE(company_id, account_code),
    CONSTRAINT chk_account_type CHECK (account_type IN ('asset', 'liability', 'equity', 'income', 'expense')),
    CONSTRAINT chk_normal_balance CHECK (normal_balance IN ('debit', 'credit'))
);

COMMENT ON TABLE chart_of_accounts IS 'Chart of Accounts following Indian Schedule III standard';
COMMENT ON COLUMN chart_of_accounts.account_type IS 'Types: asset, liability, equity, income, expense';
COMMENT ON COLUMN chart_of_accounts.normal_balance IS 'Normal balance side: debit or credit';
COMMENT ON COLUMN chart_of_accounts.schedule_reference IS 'Reference to Indian Schedule III section';

-- ============================================
-- JOURNAL ENTRIES
-- ============================================
-- Immutable once posted for audit compliance

CREATE TABLE IF NOT EXISTS journal_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,

    -- Entry identification
    journal_number VARCHAR(50) NOT NULL,
    journal_date DATE NOT NULL,

    -- Period tracking (Indian FY)
    financial_year VARCHAR(10) NOT NULL, -- e.g., '2024-25'
    period_month INTEGER NOT NULL, -- 1-12 (April = 1 for Indian FY)

    -- Entry classification
    entry_type VARCHAR(30) NOT NULL, -- manual, auto_post, reversal, opening, closing, adjustment

    -- Source document linkage
    source_type VARCHAR(50), -- invoice, payment, payroll, expense, bank_transaction, manual
    source_id UUID,
    source_number VARCHAR(100), -- Invoice number, payment ref, etc.

    -- Description
    description TEXT NOT NULL,
    narration TEXT, -- Detailed narration for audit

    -- Totals (must balance)
    total_debit DECIMAL(18,2) NOT NULL,
    total_credit DECIMAL(18,2) NOT NULL,

    -- Status workflow
    status VARCHAR(20) NOT NULL DEFAULT 'draft', -- draft, pending_approval, posted, reversed

    -- Posting info
    posted_at TIMESTAMP,
    posted_by UUID,
    approved_by UUID,
    approved_at TIMESTAMP,

    -- Reversal tracking
    is_reversed BOOLEAN DEFAULT FALSE,
    reversal_of_id UUID REFERENCES journal_entries(id),
    reversed_by_id UUID REFERENCES journal_entries(id),
    reversal_reason TEXT,

    -- Auto-posting metadata
    posting_rule_id UUID,
    rule_pack_version VARCHAR(50),

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    -- Constraints
    CONSTRAINT chk_journal_balanced CHECK (ABS(total_debit - total_credit) < 0.01),
    CONSTRAINT chk_entry_type CHECK (entry_type IN ('manual', 'auto_post', 'reversal', 'opening', 'closing', 'adjustment')),
    CONSTRAINT chk_status CHECK (status IN ('draft', 'pending_approval', 'posted', 'reversed')),
    CONSTRAINT chk_period_month CHECK (period_month BETWEEN 1 AND 12)
);

COMMENT ON TABLE journal_entries IS 'Journal entries for double-entry accounting - immutable once posted';
COMMENT ON COLUMN journal_entries.period_month IS 'Indian FY month: April=1, March=12';
COMMENT ON COLUMN journal_entries.entry_type IS 'Types: manual, auto_post, reversal, opening, closing, adjustment';

-- ============================================
-- JOURNAL ENTRY LINES
-- ============================================

CREATE TABLE IF NOT EXISTS journal_entry_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id UUID NOT NULL REFERENCES journal_entries(id) ON DELETE CASCADE,

    -- Account reference
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id),

    -- Amounts (one must be zero)
    debit_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    credit_amount DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Multi-currency support
    currency VARCHAR(3) DEFAULT 'INR',
    exchange_rate DECIMAL(18,6) DEFAULT 1,
    foreign_debit DECIMAL(18,2),
    foreign_credit DECIMAL(18,2),

    -- Subledger reference (for control accounts)
    subledger_type VARCHAR(30), -- customer, vendor, employee, bank
    subledger_id UUID,

    -- Line details
    description TEXT,
    line_number INTEGER NOT NULL,

    -- Reference
    reference_type VARCHAR(50),
    reference_id UUID,

    -- Constraints
    CONSTRAINT chk_debit_or_credit CHECK (
        (debit_amount > 0 AND credit_amount = 0) OR
        (credit_amount > 0 AND debit_amount = 0)
    )
);

COMMENT ON TABLE journal_entry_lines IS 'Individual debit/credit lines within a journal entry';
COMMENT ON COLUMN journal_entry_lines.subledger_type IS 'Types: customer, vendor, employee, bank';

-- ============================================
-- POSTING RULES
-- ============================================
-- Config-driven automatic journal entry generation

CREATE TABLE IF NOT EXISTS posting_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,

    -- Rule identification
    rule_code VARCHAR(50) NOT NULL,
    rule_name VARCHAR(200) NOT NULL,

    -- Trigger configuration
    source_type VARCHAR(50) NOT NULL, -- invoice, payment, payroll, expense, etc.
    trigger_event VARCHAR(50) NOT NULL, -- on_create, on_finalize, on_approval, on_payment

    -- Conditions (JSONB for flexibility)
    conditions_json JSONB,
    -- Example: {"invoice_type": "domestic", "gst_applicable": true}

    -- Posting template (JSONB)
    posting_template JSONB NOT NULL,
    -- Example: {
    --   "lines": [
    --     {"account_code": "1120", "side": "debit", "amount_field": "total_amount"},
    --     {"account_code": "4110", "side": "credit", "amount_field": "subtotal"}
    --   ]
    -- }

    -- Validity
    financial_year VARCHAR(10),
    effective_from DATE NOT NULL DEFAULT CURRENT_DATE,
    effective_to DATE,

    -- Priority (lower = higher priority)
    priority INTEGER NOT NULL DEFAULT 100,

    -- Status
    is_active BOOLEAN DEFAULT TRUE,
    is_system_rule BOOLEAN DEFAULT FALSE,

    -- Audit
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    -- Constraints
    CONSTRAINT uq_rule_code_company UNIQUE(company_id, rule_code, financial_year)
);

COMMENT ON TABLE posting_rules IS 'Configuration for automatic journal entry generation';
COMMENT ON COLUMN posting_rules.conditions_json IS 'JSON conditions that must be met to apply this rule';
COMMENT ON COLUMN posting_rules.posting_template IS 'JSON template defining debit/credit lines';

-- ============================================
-- POSTING RULE USAGE LOG
-- ============================================
-- Audit trail for rule applications

CREATE TABLE IF NOT EXISTS posting_rule_usage_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    posting_rule_id UUID NOT NULL REFERENCES posting_rules(id),
    journal_entry_id UUID NOT NULL REFERENCES journal_entries(id),

    -- Snapshot of rule at time of use
    rule_snapshot JSONB NOT NULL,

    -- Source document
    source_type VARCHAR(50) NOT NULL,
    source_id UUID NOT NULL,

    -- Execution details
    computed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    computed_by VARCHAR(100),

    -- Result
    success BOOLEAN DEFAULT TRUE,
    error_message TEXT
);

COMMENT ON TABLE posting_rule_usage_log IS 'Audit log for posting rule executions';

-- ============================================
-- ACCOUNT PERIOD BALANCES
-- ============================================
-- Pre-computed balances for reporting performance

CREATE TABLE IF NOT EXISTS account_period_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id) ON DELETE CASCADE,

    -- Period
    financial_year VARCHAR(10) NOT NULL,
    period_month INTEGER NOT NULL, -- 1-12

    -- Balances
    opening_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    period_debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    period_credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    closing_balance DECIMAL(18,2) NOT NULL DEFAULT 0,

    -- Movement count
    transaction_count INTEGER NOT NULL DEFAULT 0,

    -- Last updated
    last_computed_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_account_period UNIQUE(account_id, financial_year, period_month)
);

COMMENT ON TABLE account_period_balances IS 'Pre-computed period balances for fast reporting';

-- ============================================
-- INDEXES
-- ============================================

-- Chart of Accounts
CREATE INDEX IF NOT EXISTS idx_coa_company ON chart_of_accounts(company_id);
CREATE INDEX IF NOT EXISTS idx_coa_parent ON chart_of_accounts(parent_account_id);
CREATE INDEX IF NOT EXISTS idx_coa_type ON chart_of_accounts(account_type);
CREATE INDEX IF NOT EXISTS idx_coa_active ON chart_of_accounts(is_active) WHERE is_active = true;

-- Journal Entries
CREATE INDEX IF NOT EXISTS idx_je_company ON journal_entries(company_id);
CREATE INDEX IF NOT EXISTS idx_je_date ON journal_entries(journal_date);
CREATE INDEX IF NOT EXISTS idx_je_fy_period ON journal_entries(financial_year, period_month);
CREATE INDEX IF NOT EXISTS idx_je_status ON journal_entries(status);
CREATE INDEX IF NOT EXISTS idx_je_source ON journal_entries(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_je_posted ON journal_entries(posted_at) WHERE status = 'posted';

-- Journal Entry Lines
CREATE INDEX IF NOT EXISTS idx_jel_entry ON journal_entry_lines(journal_entry_id);
CREATE INDEX IF NOT EXISTS idx_jel_account ON journal_entry_lines(account_id);
CREATE INDEX IF NOT EXISTS idx_jel_subledger ON journal_entry_lines(subledger_type, subledger_id);

-- Posting Rules
CREATE INDEX IF NOT EXISTS idx_pr_company ON posting_rules(company_id);
CREATE INDEX IF NOT EXISTS idx_pr_source ON posting_rules(source_type, trigger_event);
CREATE INDEX IF NOT EXISTS idx_pr_active ON posting_rules(is_active) WHERE is_active = true;

-- Period Balances
CREATE INDEX IF NOT EXISTS idx_apb_account ON account_period_balances(account_id);
CREATE INDEX IF NOT EXISTS idx_apb_period ON account_period_balances(financial_year, period_month);

-- ============================================
-- FUNCTIONS
-- ============================================

-- Generate journal number
CREATE OR REPLACE FUNCTION generate_journal_number(p_company_id UUID, p_financial_year VARCHAR)
RETURNS VARCHAR(50) AS $$
DECLARE
    next_number INTEGER;
    prefix VARCHAR(20);
BEGIN
    -- Get next sequence number for this company/year
    SELECT COALESCE(MAX(
        CAST(SUBSTRING(journal_number FROM 'JV-[0-9]+-([0-9]+)') AS INTEGER)
    ), 0) + 1
    INTO next_number
    FROM journal_entries
    WHERE company_id = p_company_id
    AND financial_year = p_financial_year;

    RETURN 'JV-' || REPLACE(p_financial_year, '-', '') || '-' || LPAD(next_number::TEXT, 6, '0');
END;
$$ LANGUAGE plpgsql;

-- Get Indian FY period month (April = 1, March = 12)
CREATE OR REPLACE FUNCTION get_fy_period_month(p_date DATE)
RETURNS INTEGER AS $$
BEGIN
    -- April (4) = 1, May (5) = 2, ..., March (3) = 12
    RETURN CASE
        WHEN EXTRACT(MONTH FROM p_date) >= 4 THEN EXTRACT(MONTH FROM p_date) - 3
        ELSE EXTRACT(MONTH FROM p_date) + 9
    END;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- Update account balance after journal posting
CREATE OR REPLACE FUNCTION update_account_balance()
RETURNS TRIGGER AS $$
DECLARE
    v_account chart_of_accounts%ROWTYPE;
    v_net_change DECIMAL(18,2);
BEGIN
    IF TG_OP = 'INSERT' THEN
        -- Get account details
        SELECT * INTO v_account FROM chart_of_accounts WHERE id = NEW.account_id;

        -- Calculate net change based on normal balance
        IF v_account.normal_balance = 'debit' THEN
            v_net_change := NEW.debit_amount - NEW.credit_amount;
        ELSE
            v_net_change := NEW.credit_amount - NEW.debit_amount;
        END IF;

        -- Update current balance
        UPDATE chart_of_accounts
        SET current_balance = current_balance + v_net_change,
            updated_at = NOW()
        WHERE id = NEW.account_id;

    ELSIF TG_OP = 'DELETE' THEN
        -- Reverse the effect
        SELECT * INTO v_account FROM chart_of_accounts WHERE id = OLD.account_id;

        IF v_account.normal_balance = 'debit' THEN
            v_net_change := OLD.credit_amount - OLD.debit_amount;
        ELSE
            v_net_change := OLD.debit_amount - OLD.credit_amount;
        END IF;

        UPDATE chart_of_accounts
        SET current_balance = current_balance + v_net_change,
            updated_at = NOW()
        WHERE id = OLD.account_id;
    END IF;

    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Trigger for balance updates (only for posted entries)
CREATE OR REPLACE FUNCTION update_balance_on_post()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'posted' AND (OLD.status IS NULL OR OLD.status != 'posted') THEN
        -- Update all account balances for this journal entry
        UPDATE chart_of_accounts coa
        SET current_balance = current_balance + (
            SELECT
                CASE
                    WHEN coa.normal_balance = 'debit' THEN jel.debit_amount - jel.credit_amount
                    ELSE jel.credit_amount - jel.debit_amount
                END
            FROM journal_entry_lines jel
            WHERE jel.journal_entry_id = NEW.id
            AND jel.account_id = coa.id
        ),
        updated_at = NOW()
        WHERE id IN (SELECT account_id FROM journal_entry_lines WHERE journal_entry_id = NEW.id);
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_balance_on_post ON journal_entries;
CREATE TRIGGER trg_update_balance_on_post
AFTER UPDATE ON journal_entries
FOR EACH ROW
EXECUTE FUNCTION update_balance_on_post();

-- ============================================
-- VIEWS
-- ============================================

-- Trial Balance View
CREATE OR REPLACE VIEW v_trial_balance AS
SELECT
    coa.company_id,
    coa.id AS account_id,
    coa.account_code,
    coa.account_name,
    coa.account_type,
    coa.account_subtype,
    coa.normal_balance,
    coa.depth_level,
    coa.parent_account_id,
    CASE
        WHEN coa.normal_balance = 'debit' THEN GREATEST(coa.current_balance, 0)
        ELSE GREATEST(-coa.current_balance, 0)
    END AS debit_balance,
    CASE
        WHEN coa.normal_balance = 'credit' THEN GREATEST(coa.current_balance, 0)
        ELSE GREATEST(-coa.current_balance, 0)
    END AS credit_balance,
    coa.current_balance
FROM chart_of_accounts coa
WHERE coa.is_active = true
ORDER BY coa.sort_order, coa.account_code;

COMMENT ON VIEW v_trial_balance IS 'Trial balance showing debit/credit balances for all accounts';

-- Account Ledger View
CREATE OR REPLACE VIEW v_account_ledger AS
SELECT
    je.company_id,
    jel.account_id,
    coa.account_code,
    coa.account_name,
    je.id AS journal_entry_id,
    je.journal_number,
    je.journal_date,
    je.financial_year,
    je.period_month,
    je.entry_type,
    je.source_type,
    je.source_number,
    je.description AS journal_description,
    jel.description AS line_description,
    jel.debit_amount,
    jel.credit_amount,
    jel.subledger_type,
    jel.subledger_id,
    je.status,
    je.posted_at
FROM journal_entry_lines jel
JOIN journal_entries je ON je.id = jel.journal_entry_id
JOIN chart_of_accounts coa ON coa.id = jel.account_id
WHERE je.status = 'posted'
ORDER BY je.journal_date, je.journal_number, jel.line_number;

COMMENT ON VIEW v_account_ledger IS 'Detailed ledger view for any account';

-- Income Statement View (for P&L)
CREATE OR REPLACE VIEW v_income_statement AS
SELECT
    coa.company_id,
    je.financial_year,
    coa.account_type,
    coa.account_subtype,
    coa.id AS account_id,
    coa.account_code,
    coa.account_name,
    SUM(jel.credit_amount - jel.debit_amount) AS net_amount
FROM chart_of_accounts coa
LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id AND je.status = 'posted'
WHERE coa.account_type IN ('income', 'expense')
AND coa.is_active = true
GROUP BY coa.company_id, je.financial_year, coa.account_type, coa.account_subtype,
         coa.id, coa.account_code, coa.account_name
ORDER BY coa.account_type DESC, coa.sort_order, coa.account_code;

COMMENT ON VIEW v_income_statement IS 'Income statement data grouped by account';

-- Balance Sheet View
CREATE OR REPLACE VIEW v_balance_sheet AS
SELECT
    coa.company_id,
    coa.account_type,
    coa.account_subtype,
    coa.schedule_reference,
    coa.id AS account_id,
    coa.account_code,
    coa.account_name,
    coa.current_balance,
    coa.depth_level,
    coa.parent_account_id
FROM chart_of_accounts coa
WHERE coa.account_type IN ('asset', 'liability', 'equity')
AND coa.is_active = true
ORDER BY
    CASE coa.account_type
        WHEN 'asset' THEN 1
        WHEN 'liability' THEN 2
        WHEN 'equity' THEN 3
    END,
    coa.sort_order,
    coa.account_code;

COMMENT ON VIEW v_balance_sheet IS 'Balance sheet data following Schedule III structure';
