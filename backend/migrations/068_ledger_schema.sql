-- Migration: 163_ledger_schema.sql
-- Description: Consolidated ledger tables (COA, JE, Posting Rules, Tally Infrastructure)
-- Architecture: Control Account + Subledger pattern with Tally integration
-- Replaces: 068, 126, 127

-- ============================================================================
-- CHART OF ACCOUNTS
-- ============================================================================

CREATE TABLE IF NOT EXISTS chart_of_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    account_code VARCHAR(20) NOT NULL,
    account_name VARCHAR(255) NOT NULL,
    account_type VARCHAR(30) NOT NULL,
    account_subtype VARCHAR(50),
    parent_account_id UUID REFERENCES chart_of_accounts(id),
    depth_level INTEGER NOT NULL DEFAULT 0,
    full_path TEXT,
    schedule_reference VARCHAR(20),
    gst_treatment VARCHAR(30),

    -- Control account flags (modernization)
    is_control_account BOOLEAN DEFAULT FALSE,
    control_account_type VARCHAR(20),
    is_system_account BOOLEAN DEFAULT FALSE,
    is_bank_account BOOLEAN DEFAULT FALSE,
    linked_bank_account_id UUID REFERENCES bank_accounts(id),
    is_tally_legacy BOOLEAN DEFAULT FALSE,

    -- Balances
    normal_balance VARCHAR(10) NOT NULL,
    opening_balance DECIMAL(18,2) DEFAULT 0,
    current_balance DECIMAL(18,2) DEFAULT 0,

    -- Display
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    description TEXT,

    -- Tally tracking
    tally_ledger_guid VARCHAR(100),
    tally_ledger_name VARCHAR(255),
    tally_group_name VARCHAR(255),
    tally_migration_batch_id UUID,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    CONSTRAINT uq_account_code_company UNIQUE(company_id, account_code),
    CONSTRAINT chk_account_type CHECK (account_type IN ('asset', 'liability', 'equity', 'income', 'expense')),
    CONSTRAINT chk_normal_balance CHECK (normal_balance IN ('debit', 'credit')),
    CONSTRAINT chk_control_account_type CHECK (control_account_type IS NULL OR control_account_type IN (
        'payables', 'receivables', 'bank', 'tds_payable', 'tds_receivable', 'gst_input', 'gst_output', 'loans'
    ))
);

-- ============================================================================
-- JOURNAL ENTRIES
-- ============================================================================

CREATE TABLE IF NOT EXISTS journal_entries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    journal_number VARCHAR(50) NOT NULL,
    journal_date DATE NOT NULL,
    financial_year VARCHAR(10) NOT NULL,
    period_month INTEGER NOT NULL,
    entry_type VARCHAR(30) NOT NULL,
    source_type VARCHAR(50),
    source_id UUID,
    source_number VARCHAR(100),
    description TEXT NOT NULL,
    narration TEXT,
    total_debit DECIMAL(18,2) NOT NULL,
    total_credit DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'draft',
    posted_at TIMESTAMP,
    posted_by UUID,
    approved_by UUID,
    approved_at TIMESTAMP,
    is_reversed BOOLEAN DEFAULT FALSE,
    reversal_of_id UUID REFERENCES journal_entries(id),
    reversed_by_id UUID REFERENCES journal_entries(id),
    reversal_reason TEXT,
    posting_rule_id UUID,
    rule_pack_version VARCHAR(50),

    -- Tally tracking
    tally_voucher_guid VARCHAR(100),
    tally_voucher_number VARCHAR(100),
    tally_voucher_type VARCHAR(50),
    tally_migration_batch_id UUID,

    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    CONSTRAINT chk_journal_balanced CHECK (ABS(total_debit - total_credit) < 0.01),
    CONSTRAINT chk_entry_type CHECK (entry_type IN ('manual', 'auto_post', 'reversal', 'opening', 'closing', 'adjustment')),
    CONSTRAINT chk_status CHECK (status IN ('draft', 'pending_approval', 'posted', 'reversed')),
    CONSTRAINT chk_period_month CHECK (period_month BETWEEN 1 AND 12)
);

-- ============================================================================
-- JOURNAL ENTRY LINES (with subledger for control account pattern)
-- ============================================================================

CREATE TABLE IF NOT EXISTS journal_entry_lines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id UUID NOT NULL REFERENCES journal_entries(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id),
    debit_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    credit_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    currency VARCHAR(3) DEFAULT 'INR',
    exchange_rate DECIMAL(18,6) DEFAULT 1,
    foreign_debit DECIMAL(18,2),
    foreign_credit DECIMAL(18,2),

    -- Subledger reference (enables control account drill-down)
    subledger_type VARCHAR(30),
    subledger_id UUID,

    description TEXT,
    line_number INTEGER NOT NULL,
    reference_type VARCHAR(50),
    reference_id UUID,

    CONSTRAINT chk_debit_or_credit CHECK (
        (debit_amount > 0 AND credit_amount = 0) OR
        (credit_amount > 0 AND debit_amount = 0)
    )
);

-- ============================================================================
-- POSTING RULES
-- ============================================================================

CREATE TABLE IF NOT EXISTS posting_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    rule_code VARCHAR(50) NOT NULL,
    rule_name VARCHAR(200) NOT NULL,
    description TEXT,
    source_type VARCHAR(50) NOT NULL,
    trigger_event VARCHAR(50) NOT NULL,
    conditions_json JSONB,
    posting_template JSONB NOT NULL,
    financial_year VARCHAR(10),
    effective_from DATE NOT NULL DEFAULT CURRENT_DATE,
    effective_to DATE,
    priority INTEGER NOT NULL DEFAULT 100,
    is_active BOOLEAN DEFAULT TRUE,
    is_system_rule BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    created_by UUID,

    CONSTRAINT uq_rule_code_company UNIQUE(company_id, rule_code, financial_year)
);

CREATE TABLE IF NOT EXISTS posting_rule_usage_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    posting_rule_id UUID NOT NULL REFERENCES posting_rules(id),
    journal_entry_id UUID NOT NULL REFERENCES journal_entries(id),
    rule_snapshot JSONB NOT NULL,
    source_type VARCHAR(50) NOT NULL,
    source_id UUID NOT NULL,
    computed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    computed_by VARCHAR(100),
    success BOOLEAN DEFAULT TRUE,
    error_message TEXT
);

CREATE TABLE IF NOT EXISTS account_period_balances (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES chart_of_accounts(id) ON DELETE CASCADE,
    financial_year VARCHAR(10) NOT NULL,
    period_month INTEGER NOT NULL,
    opening_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    period_debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    period_credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    closing_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    transaction_count INTEGER NOT NULL DEFAULT 0,
    last_computed_at TIMESTAMP NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_account_period UNIQUE(account_id, financial_year, period_month)
);

-- ============================================================================
-- TALLY MIGRATION INFRASTRUCTURE
-- ============================================================================

CREATE TABLE IF NOT EXISTS tally_migration_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    batch_number VARCHAR(50) NOT NULL,
    import_type VARCHAR(20) NOT NULL DEFAULT 'full',
    source_file_name VARCHAR(255),
    source_file_size BIGINT,
    source_format VARCHAR(10) NOT NULL DEFAULT 'xml',
    source_checksum VARCHAR(64),
    tally_company_name VARCHAR(255),
    tally_company_guid VARCHAR(100),
    tally_from_date DATE,
    tally_to_date DATE,
    tally_financial_year VARCHAR(20),
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    total_ledgers INT DEFAULT 0,
    imported_ledgers INT DEFAULT 0,
    skipped_ledgers INT DEFAULT 0,
    failed_ledgers INT DEFAULT 0,
    total_stock_items INT DEFAULT 0,
    imported_stock_items INT DEFAULT 0,
    total_vouchers INT DEFAULT 0,
    imported_vouchers INT DEFAULT 0,
    skipped_vouchers INT DEFAULT 0,
    failed_vouchers INT DEFAULT 0,
    voucher_counts JSONB DEFAULT '{}',
    suspense_entries_created INT DEFAULT 0,
    suspense_total_amount DECIMAL(18,2) DEFAULT 0,
    upload_started_at TIMESTAMP WITH TIME ZONE,
    parsing_completed_at TIMESTAMP WITH TIME ZONE,
    import_completed_at TIMESTAMP WITH TIME ZONE,
    error_message TEXT,
    error_details JSONB,
    mapping_config JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_tally_batch_number UNIQUE (company_id, batch_number),
    CONSTRAINT chk_import_type CHECK (import_type IN ('full', 'incremental')),
    CONSTRAINT chk_batch_status CHECK (status IN (
        'pending', 'uploading', 'parsing', 'validating', 'preview',
        'mapping', 'importing', 'posting', 'completed', 'failed', 'rolled_back', 'cancelled'
    ))
);

CREATE TABLE IF NOT EXISTS tally_migration_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    batch_id UUID NOT NULL REFERENCES tally_migration_batches(id) ON DELETE CASCADE,
    record_type VARCHAR(50) NOT NULL,
    tally_guid VARCHAR(100),
    tally_name VARCHAR(255),
    tally_parent_name VARCHAR(255),
    tally_date DATE,
    target_entity VARCHAR(50),
    target_id UUID,
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    skip_reason VARCHAR(100),
    error_message TEXT,
    validation_warnings JSONB DEFAULT '[]',
    raw_data JSONB,
    tally_amount DECIMAL(18,2),
    imported_amount DECIMAL(18,2),
    processing_order INT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS tally_field_mappings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    mapping_type VARCHAR(30) NOT NULL DEFAULT 'ledger_group',
    tally_group_name VARCHAR(255),
    tally_name VARCHAR(255) NOT NULL DEFAULT '',
    target_entity VARCHAR(50) NOT NULL,
    target_account_id UUID REFERENCES chart_of_accounts(id),
    target_account_type VARCHAR(30),
    default_account_code VARCHAR(20),
    target_tag_group VARCHAR(50),
    priority INT DEFAULT 100,
    is_active BOOLEAN DEFAULT true,
    is_system_default BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_by UUID REFERENCES users(id),

    CONSTRAINT uq_tally_mapping UNIQUE (company_id, mapping_type, tally_group_name, tally_name),
    CONSTRAINT chk_target_entity CHECK (target_entity IN (
        'vendors', 'customers', 'bank_accounts', 'chart_of_accounts', 'tags', 'stock_groups', 'suspense', 'skip'
    ))
);

-- Per-ledger mapping for control account pattern (modernization)
CREATE TABLE IF NOT EXISTS tally_ledger_mapping (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID NOT NULL REFERENCES companies(id) ON DELETE CASCADE,
    tally_ledger_name VARCHAR(255) NOT NULL,
    tally_ledger_guid VARCHAR(100),
    tally_parent_group VARCHAR(100),
    control_account_id UUID REFERENCES chart_of_accounts(id),
    party_type VARCHAR(20) CHECK (party_type IN ('vendor', 'customer', 'employee', NULL)),
    party_id UUID,
    legacy_coa_id UUID REFERENCES chart_of_accounts(id),
    opening_balance DECIMAL(18,2) DEFAULT 0,
    opening_balance_date DATE,
    is_active BOOLEAN DEFAULT true,
    last_sync_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT uq_tally_ledger_company UNIQUE(company_id, tally_ledger_name)
);

-- ============================================================================
-- INDEXES
-- ============================================================================

CREATE INDEX IF NOT EXISTS idx_coa_company ON chart_of_accounts(company_id);
CREATE INDEX IF NOT EXISTS idx_coa_parent ON chart_of_accounts(parent_account_id);
CREATE INDEX IF NOT EXISTS idx_coa_type ON chart_of_accounts(account_type);
CREATE INDEX IF NOT EXISTS idx_coa_control ON chart_of_accounts(is_control_account) WHERE is_control_account = true;
CREATE INDEX IF NOT EXISTS idx_coa_tally_guid ON chart_of_accounts(tally_ledger_guid) WHERE tally_ledger_guid IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_je_company ON journal_entries(company_id);
CREATE INDEX IF NOT EXISTS idx_je_date ON journal_entries(journal_date);
CREATE INDEX IF NOT EXISTS idx_je_fy_period ON journal_entries(financial_year, period_month);
CREATE INDEX IF NOT EXISTS idx_je_status ON journal_entries(status);
CREATE INDEX IF NOT EXISTS idx_je_source ON journal_entries(source_type, source_id);
CREATE INDEX IF NOT EXISTS idx_je_company_posted ON journal_entries(company_id, status) WHERE status = 'posted';

CREATE INDEX IF NOT EXISTS idx_jel_entry ON journal_entry_lines(journal_entry_id);
CREATE INDEX IF NOT EXISTS idx_jel_account ON journal_entry_lines(account_id);
CREATE INDEX IF NOT EXISTS idx_jel_subledger ON journal_entry_lines(subledger_type, subledger_id);
CREATE INDEX IF NOT EXISTS idx_jel_account_amounts ON journal_entry_lines(account_id) INCLUDE (debit_amount, credit_amount);

CREATE INDEX IF NOT EXISTS idx_pr_company ON posting_rules(company_id);
CREATE INDEX IF NOT EXISTS idx_pr_source ON posting_rules(source_type, trigger_event);

CREATE INDEX IF NOT EXISTS idx_tally_batches_company ON tally_migration_batches(company_id);
CREATE INDEX IF NOT EXISTS idx_tally_logs_batch ON tally_migration_logs(batch_id);
CREATE INDEX IF NOT EXISTS idx_tally_mappings_company ON tally_field_mappings(company_id);
CREATE INDEX IF NOT EXISTS idx_tlm_company ON tally_ledger_mapping(company_id);
CREATE INDEX IF NOT EXISTS idx_tlm_party ON tally_ledger_mapping(party_type, party_id);
