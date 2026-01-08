-- Migration: 133_add_indian_accounting_mappings.sql
-- Description: Add comprehensive Tally ledger group mappings for Indian accounting standards
-- Based on: Tally ERP9/TallyPrime documentation, Indian tax laws (GST, TDS), ICAI guidelines
-- Sources:
--   - https://help.tallysolutions.com/tally-prime/payroll-masters/payroll-create-payable-ledgers-tally/
--   - https://help.tallysolutions.com/article/Tally.ERP9/Creating_Masters/Accounts_Info/Intro_Groups.htm
--   - https://tallyerp9book.com/tallyprimebook/ledger-in-tallyprime/creating-gst-ledger-cgst-sgst-igst-under-duty-and-taxes-group-in-tallyprime.html
--   - https://cleartax.in/s/section-194j

-- ============================================================================
-- UPDATE seed_tally_default_mappings FUNCTION
-- Add comprehensive Indian accounting ledger group mappings
-- ============================================================================

CREATE OR REPLACE FUNCTION seed_tally_default_mappings(p_company_id UUID)
RETURNS void AS $$
BEGIN
    -- ========================================================================
    -- PARTY MAPPINGS (Priority 10 - Highest)
    -- These create Party records (vendors/customers)
    -- ========================================================================

    -- Sundry Creditors -> Vendors (standard trade payables)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- Sundry Debtors -> Customers (standard trade receivables)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'customers';

    -- Bank Accounts -> Bank Accounts
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank Accounts', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    -- Bank OD A/c -> Bank Accounts (overdraft accounts)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OD A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    -- Bank OCC A/c -> Bank Accounts (cash credit accounts)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bank OCC A/c', '', 'bank_accounts', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'bank_accounts';

    -- ========================================================================
    -- TDS-APPLICABLE VENDOR CATEGORIES (Priority 10)
    -- Section 194J, 194C, 194H, 194I, 194A per Indian Income Tax Act
    -- ========================================================================

    -- CONSULTANTS -> Vendors (TDS 194J @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONSULTANTS', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Consultants', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- CONTRACTORS -> Vendors (TDS 194C @ 1-2%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CONTRACTORS', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Contractors', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- PROFESSIONAL FEES -> Vendors (TDS 194J @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PROFESSIONAL FEES', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Professional Fees', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- RENT PAYABLE -> Vendors (TDS 194I @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'RENT PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Rent Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- COMMISSION PAYABLE -> Vendors (TDS 194H @ 5%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'COMMISSION PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Commission Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- BROKERAGE PAYABLE -> Vendors (TDS 194H @ 5%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'BROKERAGE PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Brokerage Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- INTEREST PAYABLE -> Vendors (TDS 194A @ 10%)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'INTEREST PAYABLE', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Interest Payable', '', 'vendors', true, 10)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'vendors';

    -- ========================================================================
    -- GST ACCOUNTS (Priority 15) - Per Indian GST Law
    -- Duties & Taxes group -> chart_of_accounts (liability)
    -- Source: https://tallyerp9book.com - "Tax Ledgers should be created under Duties and Taxes group"
    -- ========================================================================

    -- Duties & Taxes (primary group for all tax accounts)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Duties & Taxes', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- CGST, SGST, IGST (Central, State, Integrated GST)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'CGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'SGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'IGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'UTGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Input GST (asset - recoverable from government)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input CGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input SGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Input IGST', '', 'chart_of_accounts', 'asset', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Output GST (liability - payable to government)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output CGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output SGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Output IGST', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- TDS PAYABLE ACCOUNTS (Priority 15) - Per Indian Income Tax Act
    -- These are statutory liabilities under Duties & Taxes
    -- Source: https://cleartax.in/s/section-194j
    -- ========================================================================

    -- TDS Payable accounts (liability - to be deposited with government)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Salary', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Contract', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Consulting', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Rent', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Interest', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Commission', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'TDS on Professional', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- PAYROLL ACCOUNTS (Priority 15) - Per Indian labor laws (EPF, ESI Act)
    -- Source: https://help.tallysolutions.com/tally-prime/payroll-masters/payroll-create-payable-ledgers-tally/
    -- ========================================================================

    -- Salary/Payroll Expense Accounts -> Indirect Expenses
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salaries', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary Account', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- PF Employer Contribution -> Indirect Expenses (employer's expense)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Employer Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers PF Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- EPS Contribution (Employees' Pension Scheme @ 8.33%) -> Indirect Expenses
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers EPS Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'EPS Contribution', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- EDLI Contribution (Employees' Deposit Linked Insurance) -> Current Liabilities
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers EDLI Contribution', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'EDLI Contribution', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- PF Admin Charges -> Current Liabilities (payable to EPFO)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employers PF Admin Charges', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Admin Charges', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Employee Bonus -> Indirect Expenses
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Employee Bonus', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Bonus', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- Stipend/Internship -> Indirect Expenses
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Internship-Stipend', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stipend', '', 'chart_of_accounts', 'expense', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- Full & Final Settlement -> Current Liabilities
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Full & Final Settlement', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Salary Payable, PF Payable -> Current Liabilities
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Salary Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'PF Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'ESI Payable', '', 'chart_of_accounts', 'liability', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- ========================================================================
    -- CAPITAL ACCOUNT (Priority 15) - Directors/Partners/Proprietors
    -- Per Indian Companies Act & Partnership Act
    -- ========================================================================

    -- Capital Account -> Equity (primary group for owner's capital)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Capital Account', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    -- Reserves & Surplus -> Equity (retained earnings, reserves)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Reserves & Surplus', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    -- Share Capital -> Equity
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Share Capital', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    -- Partners Capital A/c -> Equity
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Partners Capital', '', 'chart_of_accounts', 'equity', true, 15)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'equity';

    -- ========================================================================
    -- SYSTEM/SKIP ACCOUNTS (Priority 100 - Lowest)
    -- ========================================================================

    -- Profit & Loss A/c -> Skip (system-generated)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Profit & Loss A/c', '', 'skip', true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'skip';

    -- Primary Accounts -> Skip (system accounts)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Primary', '', 'skip', true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'skip';

    -- ========================================================================
    -- STANDARD CHART OF ACCOUNTS MAPPINGS (Priority 20)
    -- ========================================================================

    -- Cash-in-hand -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Cash-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Purchase Accounts -> Expense
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Purchase Accounts', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- Sales Accounts -> Income
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Sales Accounts', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    -- Direct Expenses -> Expense
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- Indirect Expenses -> Expense
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Expenses', '', 'chart_of_accounts', 'expense', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'expense';

    -- Direct Incomes -> Income
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Direct Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    -- Indirect Incomes -> Income
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Indirect Incomes', '', 'chart_of_accounts', 'income', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'income';

    -- Fixed Assets -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Fixed Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Investments -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Investments', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Current Assets -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Assets', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Current Liabilities -> Liability
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Current Liabilities', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Loans (Liability) -> Liability
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans (Liability)', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Unsecured Loans -> Liability (directors/partners loans)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Unsecured Loans', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Secured Loans -> Liability
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Secured Loans', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Loans & Advances (Asset) -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Loans & Advances (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Stock-in-hand -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Stock-in-hand', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'asset';

    -- Provisions -> Liability
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Provisions', '', 'chart_of_accounts', 'liability', true, 20)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO UPDATE SET target_entity = 'chart_of_accounts', target_account_type = 'liability';

    -- Suspense A/c -> Suspense (fallback)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Suspense A/c', '', 'suspense', true, 100)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

    -- Branch / Divisions -> Asset
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Branch / Divisions', '', 'chart_of_accounts', 'asset', true, 30)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

    -- Misc. Expenses (Asset) -> Asset (deferred expenses)
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES (p_company_id, 'ledger_group', 'Misc. Expenses (Asset)', '', 'chart_of_accounts', 'asset', true, 20)
    ON CONFLICT (company_id, mapping_type, tally_group_name, tally_name) DO NOTHING;

END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- NOTE: Call seed_tally_default_mappings(company_id) manually after creating a company
-- Example: SELECT seed_tally_default_mappings('your-company-uuid');
-- ============================================================================

COMMENT ON FUNCTION seed_tally_default_mappings IS 'Seeds comprehensive Tally ledger group mappings based on Indian accounting standards (GST, TDS, EPF, Companies Act)';
