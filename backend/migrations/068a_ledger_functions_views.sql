-- Migration: 164_ledger_functions_views.sql
-- Description: Ledger functions, views, and triggers
-- Replaces: 158, 159, 160

-- ============================================================================
-- UTILITY FUNCTIONS
-- ============================================================================

CREATE OR REPLACE FUNCTION generate_journal_number(p_company_id UUID, p_financial_year VARCHAR)
RETURNS VARCHAR(50) AS $$
DECLARE
    next_number INTEGER;
BEGIN
    SELECT COALESCE(MAX(CAST(SUBSTRING(journal_number FROM 'JV-[0-9]+-([0-9]+)') AS INTEGER)), 0) + 1
    INTO next_number
    FROM journal_entries
    WHERE company_id = p_company_id AND financial_year = p_financial_year;

    RETURN 'JV-' || REPLACE(p_financial_year, '-', '') || '-' || LPAD(next_number::TEXT, 6, '0');
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION get_fy_period_month(p_date DATE)
RETURNS INTEGER AS $$
BEGIN
    RETURN CASE WHEN EXTRACT(MONTH FROM p_date) >= 4
        THEN EXTRACT(MONTH FROM p_date) - 3
        ELSE EXTRACT(MONTH FROM p_date) + 9 END;
END;
$$ LANGUAGE plpgsql IMMUTABLE;

CREATE OR REPLACE FUNCTION get_financial_year(payment_date DATE)
RETURNS VARCHAR(10) AS $$
DECLARE start_year INT;
BEGIN
    start_year := CASE WHEN EXTRACT(MONTH FROM payment_date) >= 4
        THEN EXTRACT(YEAR FROM payment_date)
        ELSE EXTRACT(YEAR FROM payment_date) - 1 END;
    RETURN start_year::TEXT || '-' || SUBSTRING((start_year + 1)::TEXT FROM 3);
END;
$$ LANGUAGE plpgsql IMMUTABLE;

-- ============================================================================
-- BALANCE FUNCTIONS
-- ============================================================================

CREATE OR REPLACE FUNCTION update_balance_on_post()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.status = 'posted' AND (OLD.status IS NULL OR OLD.status != 'posted') THEN
        UPDATE chart_of_accounts coa
        SET current_balance = current_balance + (
            SELECT CASE WHEN coa.normal_balance = 'debit'
                THEN jel.debit_amount - jel.credit_amount
                ELSE jel.credit_amount - jel.debit_amount END
            FROM journal_entry_lines jel
            WHERE jel.journal_entry_id = NEW.id AND jel.account_id = coa.id
        ), updated_at = NOW()
        WHERE id IN (SELECT account_id FROM journal_entry_lines WHERE journal_entry_id = NEW.id);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION recalculate_account_balances(p_company_id UUID DEFAULT NULL)
RETURNS TABLE(accounts_updated INTEGER) AS $$
DECLARE v_count INTEGER;
BEGIN
    UPDATE chart_of_accounts coa
    SET current_balance = COALESCE((
        SELECT CASE WHEN coa.normal_balance = 'debit'
            THEN SUM(jel.debit_amount) - SUM(jel.credit_amount)
            ELSE SUM(jel.credit_amount) - SUM(jel.debit_amount) END
        FROM journal_entry_lines jel
        JOIN journal_entries je ON jel.journal_entry_id = je.id
        WHERE jel.account_id = coa.id AND je.status = 'posted' AND je.company_id = coa.company_id
    ), 0) + coa.opening_balance, updated_at = NOW()
    WHERE (p_company_id IS NULL OR coa.company_id = p_company_id);
    GET DIAGNOSTICS v_count = ROW_COUNT;
    RETURN QUERY SELECT v_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- VIEWS
-- ============================================================================

-- Trial Balance (computed from JE lines, excludes legacy accounts)
CREATE OR REPLACE VIEW v_trial_balance AS
WITH account_balances AS (
    SELECT
        coa.company_id, coa.id AS account_id, coa.account_code, coa.account_name,
        coa.account_type, coa.account_subtype, coa.normal_balance, coa.depth_level,
        coa.parent_account_id, coa.sort_order, coa.is_control_account, coa.control_account_type,
        COALESCE(-coa.opening_balance, 0) AS opening_balance,
        COALESCE(SUM(jel.debit_amount), 0) AS total_debit,
        COALESCE(SUM(jel.credit_amount), 0) AS total_credit
    FROM chart_of_accounts coa
    LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
    LEFT JOIN journal_entries je ON jel.journal_entry_id = je.id
        AND je.status = 'posted' AND je.company_id = coa.company_id
    WHERE coa.is_active = true AND coa.is_tally_legacy = false
    GROUP BY coa.company_id, coa.id, coa.account_code, coa.account_name,
        coa.account_type, coa.account_subtype, coa.normal_balance, coa.depth_level,
        coa.parent_account_id, coa.sort_order, coa.is_control_account,
        coa.control_account_type, coa.opening_balance
)
SELECT company_id, account_id, account_code, account_name, account_type, account_subtype,
    normal_balance, depth_level, parent_account_id, is_control_account, control_account_type,
    GREATEST(total_debit - total_credit - opening_balance, 0) AS debit_balance,
    GREATEST(total_credit - total_debit + opening_balance, 0) AS credit_balance,
    total_debit - total_credit - opening_balance AS current_balance
FROM account_balances
ORDER BY sort_order, account_code;

-- Account Ledger
CREATE OR REPLACE VIEW v_account_ledger AS
SELECT je.company_id, jel.account_id, coa.account_code, coa.account_name,
    je.id AS journal_entry_id, je.journal_number, je.journal_date, je.financial_year,
    je.period_month, je.entry_type, je.source_type, je.source_number,
    je.description AS journal_description, jel.description AS line_description,
    jel.debit_amount, jel.credit_amount, jel.subledger_type, jel.subledger_id,
    je.status, je.posted_at
FROM journal_entry_lines jel
JOIN journal_entries je ON je.id = jel.journal_entry_id
JOIN chart_of_accounts coa ON coa.id = jel.account_id
WHERE je.status = 'posted'
ORDER BY je.journal_date, je.journal_number, jel.line_number;

-- Subledger Balance (party-wise drill-down for control accounts)
CREATE OR REPLACE VIEW v_subledger_balance AS
SELECT je.company_id, jel.account_id, coa.account_code, coa.account_name,
    coa.control_account_type, jel.subledger_type, jel.subledger_id,
    SUM(jel.debit_amount) AS total_debit, SUM(jel.credit_amount) AS total_credit,
    SUM(jel.debit_amount - jel.credit_amount) AS balance, COUNT(*) AS transaction_count
FROM journal_entry_lines jel
JOIN journal_entries je ON je.id = jel.journal_entry_id
JOIN chart_of_accounts coa ON coa.id = jel.account_id
WHERE je.status = 'posted' AND coa.is_control_account = true AND jel.subledger_id IS NOT NULL
GROUP BY je.company_id, jel.account_id, coa.account_code, coa.account_name,
    coa.control_account_type, jel.subledger_type, jel.subledger_id;

-- Income Statement
CREATE OR REPLACE VIEW v_income_statement AS
SELECT coa.company_id, je.financial_year, coa.account_type, coa.account_subtype,
    coa.id AS account_id, coa.account_code, coa.account_name,
    SUM(jel.credit_amount - jel.debit_amount) AS net_amount
FROM chart_of_accounts coa
LEFT JOIN journal_entry_lines jel ON jel.account_id = coa.id
LEFT JOIN journal_entries je ON je.id = jel.journal_entry_id AND je.status = 'posted'
WHERE coa.account_type IN ('income', 'expense') AND coa.is_active = true AND coa.is_tally_legacy = false
GROUP BY coa.company_id, je.financial_year, coa.account_type, coa.account_subtype,
    coa.id, coa.account_code, coa.account_name
ORDER BY coa.account_type DESC, coa.sort_order, coa.account_code;

-- Balance Sheet
CREATE OR REPLACE VIEW v_balance_sheet AS
SELECT coa.company_id, coa.account_type, coa.account_subtype, coa.schedule_reference,
    coa.id AS account_id, coa.account_code, coa.account_name, coa.current_balance,
    coa.depth_level, coa.parent_account_id, coa.is_control_account, coa.control_account_type
FROM chart_of_accounts coa
WHERE coa.account_type IN ('asset', 'liability', 'equity')
    AND coa.is_active = true AND coa.is_tally_legacy = false
ORDER BY CASE coa.account_type WHEN 'asset' THEN 1 WHEN 'liability' THEN 2 WHEN 'equity' THEN 3 END,
    coa.sort_order, coa.account_code;

-- ============================================================================
-- TRIGGERS
-- ============================================================================

CREATE OR REPLACE FUNCTION update_updated_at() RETURNS TRIGGER AS $$
BEGIN NEW.updated_at = CURRENT_TIMESTAMP; RETURN NEW; END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_update_balance_on_post ON journal_entries;
CREATE TRIGGER trg_update_balance_on_post AFTER UPDATE ON journal_entries
    FOR EACH ROW EXECUTE FUNCTION update_balance_on_post();

DROP TRIGGER IF EXISTS trg_coa_updated_at ON chart_of_accounts;
CREATE TRIGGER trg_coa_updated_at BEFORE UPDATE ON chart_of_accounts
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS trg_je_updated_at ON journal_entries;
CREATE TRIGGER trg_je_updated_at BEFORE UPDATE ON journal_entries
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS trg_pr_updated_at ON posting_rules;
CREATE TRIGGER trg_pr_updated_at BEFORE UPDATE ON posting_rules
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS trg_tally_batches_updated_at ON tally_migration_batches;
CREATE TRIGGER trg_tally_batches_updated_at BEFORE UPDATE ON tally_migration_batches
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS trg_tally_mappings_updated_at ON tally_field_mappings;
CREATE TRIGGER trg_tally_mappings_updated_at BEFORE UPDATE ON tally_field_mappings
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();

DROP TRIGGER IF EXISTS trg_tally_ledger_mapping_updated_at ON tally_ledger_mapping;
CREATE TRIGGER trg_tally_ledger_mapping_updated_at BEFORE UPDATE ON tally_ledger_mapping
    FOR EACH ROW EXECUTE FUNCTION update_updated_at();
