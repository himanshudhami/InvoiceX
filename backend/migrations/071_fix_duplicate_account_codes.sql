-- Migration: Fix duplicate account code 5200 in chart of accounts seed function
-- Issue: Both "Contractor Payments" and "Employee Benefit Expenses" were using code '5200'
-- Fix: Renumber Cost of Revenue children to 5010, 5020, 5030

-- Step 1: Delete any partially created accounts for companies that hit the duplicate error
-- This removes accounts that were created before the error occurred
DELETE FROM chart_of_accounts
WHERE company_id IN (
    SELECT DISTINCT company_id
    FROM chart_of_accounts
    GROUP BY company_id
    HAVING COUNT(*) < 80  -- Less than expected full set means partial creation
);

-- Step 2: Fix any existing accounts with the old codes (5100, 5200, 5300 under Cost of Revenue)
-- Update the child accounts under Cost of Revenue (parent code 5000)
UPDATE chart_of_accounts child
SET account_code = CASE child.account_code
    WHEN '5100' THEN '5010'
    WHEN '5200' THEN '5020'
    WHEN '5300' THEN '5030'
    ELSE child.account_code
END,
sort_order = CASE child.account_code
    WHEN '5100' THEN 5010
    WHEN '5200' THEN 5020
    WHEN '5300' THEN 5030
    ELSE child.sort_order
END
WHERE child.parent_account_id IN (
    SELECT id FROM chart_of_accounts WHERE account_code = '5000'
)
AND child.account_code IN ('5100', '5200', '5300');

-- Step 3: Drop and recreate the function with fixed account codes
DROP FUNCTION IF EXISTS create_default_chart_of_accounts(UUID, UUID);

CREATE OR REPLACE FUNCTION create_default_chart_of_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER := 0;
    v_parent_id UUID;
BEGIN
    -- ========================================
    -- ASSETS (1000-1999)
    -- ========================================

    -- 1000: Current Assets (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1000', 'Current Assets', 'asset', 'current_asset', 'debit', 1000, true, 'II(A)', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    -- 1100: Cash and Cash Equivalents
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1100', 'Cash and Cash Equivalents', 'asset', 'current_asset', v_parent_id, 1, 'debit', 1100, true, 'II(A)(a)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1100'),
           2, 'debit', ord, 'II(A)(a)', p_created_by
    FROM (VALUES
        ('1110', 'Petty Cash', 1110),
        ('1111', 'Cash in Hand', 1111),
        ('1112', 'Bank Accounts - Current', 1112),
        ('1113', 'Bank Accounts - Savings', 1113),
        ('1114', 'Fixed Deposits (< 3 months)', 1114)
    ) AS t(code, name, ord);
    v_count := v_count + 5;

    -- 1120: Trade Receivables (Control Account)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1120', 'Trade Receivables', 'asset', 'current_asset', v_parent_id, 1, 'debit', 1120, true, true, 'II(A)(b)', p_created_by);
    v_count := v_count + 1;

    -- 1130: TDS Receivable
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1130', 'TDS Receivable', 'asset', 'current_asset', v_parent_id, 1, 'debit', 1130, true, 'II(A)(c)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1130'),
           2, 'debit', ord, 'II(A)(c)', p_created_by
    FROM (VALUES
        ('1131', 'TDS Receivable - 194J', 1131),
        ('1132', 'TDS Receivable - 194C', 1132),
        ('1133', 'TDS Receivable - 194H', 1133),
        ('1134', 'TDS Receivable - Other', 1134)
    ) AS t(code, name, ord);
    v_count := v_count + 4;

    -- 1140: GST Input Credit
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, gst_treatment, schedule_reference, created_by)
    VALUES (p_company_id, '1140', 'GST Input Credit', 'asset', 'current_asset', v_parent_id, 1, 'debit', 1140, true, 'taxable', 'II(A)(d)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1140'),
           2, 'debit', ord, 'taxable', 'II(A)(d)', p_created_by
    FROM (VALUES
        ('1141', 'CGST Input', 1141),
        ('1142', 'SGST Input', 1142),
        ('1143', 'IGST Input', 1143),
        ('1144', 'GST Cess Input', 1144)
    ) AS t(code, name, ord);
    v_count := v_count + 4;

    -- 1150: Advances
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1150', 'Advances and Prepayments', 'asset', 'current_asset', v_parent_id, 1, 'debit', 1150, 'II(A)(e)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1150'),
           2, 'debit', ord, 'II(A)(e)', p_created_by
    FROM (VALUES
        ('1151', 'Advance to Suppliers', 1151),
        ('1152', 'Prepaid Expenses', 1152),
        ('1153', 'Advance Tax', 1153),
        ('1154', 'Security Deposits', 1154)
    ) AS t(code, name, ord);
    v_count := v_count + 4;

    -- 1500: Non-Current Assets (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1500', 'Non-Current Assets', 'asset', 'non_current_asset', 'debit', 1500, true, 'I', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    -- 1600: Property, Plant & Equipment
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1600', 'Property, Plant and Equipment', 'asset', 'fixed_asset', v_parent_id, 1, 'debit', 1600, 'I(A)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'fixed_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1600'),
           2, 'debit', ord, 'I(A)', p_created_by
    FROM (VALUES
        ('1610', 'Land', 1610),
        ('1620', 'Buildings', 1620),
        ('1630', 'Plant and Machinery', 1630),
        ('1640', 'Furniture and Fixtures', 1640),
        ('1650', 'Office Equipment', 1650),
        ('1660', 'Computers and IT Equipment', 1660),
        ('1670', 'Vehicles', 1670)
    ) AS t(code, name, ord);
    v_count := v_count + 7;

    -- 1700: Accumulated Depreciation (Contra Asset)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1700', 'Accumulated Depreciation', 'asset', 'contra_asset', v_parent_id, 1, 'credit', 1700, 'I(A)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'contra_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1700'),
           2, 'credit', ord, 'I(A)', p_created_by
    FROM (VALUES
        ('1720', 'Acc. Dep. - Buildings', 1720),
        ('1730', 'Acc. Dep. - Plant and Machinery', 1730),
        ('1740', 'Acc. Dep. - Furniture', 1740),
        ('1750', 'Acc. Dep. - Office Equipment', 1750),
        ('1760', 'Acc. Dep. - Computers', 1760),
        ('1770', 'Acc. Dep. - Vehicles', 1770)
    ) AS t(code, name, ord);
    v_count := v_count + 6;

    -- 1800: Intangible Assets
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1800', 'Intangible Assets', 'asset', 'intangible_asset', v_parent_id, 1, 'debit', 1800, 'I(B)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'intangible_asset',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1800'),
           2, 'debit', ord, 'I(B)', p_created_by
    FROM (VALUES
        ('1810', 'Software Licenses', 1810),
        ('1820', 'Patents and Trademarks', 1820),
        ('1830', 'Goodwill', 1830)
    ) AS t(code, name, ord);
    v_count := v_count + 3;

    -- ========================================
    -- LIABILITIES (2000-2999)
    -- ========================================

    -- 2000: Current Liabilities (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2000', 'Current Liabilities', 'liability', 'current_liability', 'credit', 2000, true, 'II', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    -- 2100: Trade Payables (Control Account)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2100', 'Trade Payables', 'liability', 'current_liability', v_parent_id, 1, 'credit', 2100, true, true, 'II(a)', p_created_by);
    v_count := v_count + 1;

    -- 2110: Salary and Wages Payable
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2110', 'Salary and Wages Payable', 'liability', 'current_liability', v_parent_id, 1, 'credit', 2110, true, 'II(b)', p_created_by);
    v_count := v_count + 1;

    -- 2200: Statutory Dues
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2200', 'Statutory Dues', 'liability', 'current_liability', v_parent_id, 1, 'credit', 2200, true, 'II(c)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2200'),
           2, 'credit', ord, 'II(c)', p_created_by
    FROM (VALUES
        ('2211', 'TDS Payable', 2211),
        ('2212', 'TDS Payable - Salary', 2212),
        ('2213', 'TDS Payable - Professional', 2213),
        ('2220', 'PF Payable', 2220),
        ('2221', 'Employee PF Contribution', 2221),
        ('2222', 'Employer PF Contribution', 2222),
        ('2230', 'ESI Payable', 2230),
        ('2240', 'Professional Tax Payable', 2240)
    ) AS t(code, name, ord);
    v_count := v_count + 8;

    -- 2250: GST Payable
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, gst_treatment, schedule_reference, created_by)
    VALUES (p_company_id, '2250', 'GST Payable', 'liability', 'current_liability', v_parent_id, 1, 'credit', 2250, true, 'taxable', 'II(c)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2250'),
           2, 'credit', ord, 'taxable', 'II(c)', p_created_by
    FROM (VALUES
        ('2251', 'CGST Payable', 2251),
        ('2252', 'SGST Payable', 2252),
        ('2253', 'IGST Payable', 2253),
        ('2254', 'GST Cess Payable', 2254)
    ) AS t(code, name, ord);
    v_count := v_count + 4;

    -- 2300: Other Current Liabilities
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '2300', 'Other Current Liabilities', 'liability', 'current_liability', v_parent_id, 1, 'credit', 2300, 'II(d)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2300'),
           2, 'credit', ord, 'II(d)', p_created_by
    FROM (VALUES
        ('2310', 'Advance from Customers', 2310),
        ('2320', 'Accrued Expenses', 2320),
        ('2330', 'Provisions', 2330)
    ) AS t(code, name, ord);
    v_count := v_count + 3;

    -- 2500: Non-Current Liabilities (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2500', 'Non-Current Liabilities', 'liability', 'non_current_liability', 'credit', 2500, true, 'I', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'non_current_liability', v_parent_id, 1, 'credit', ord, 'I', p_created_by
    FROM (VALUES
        ('2510', 'Long-Term Borrowings', 2510),
        ('2520', 'Deferred Tax Liabilities', 2520),
        ('2530', 'Long-Term Provisions', 2530)
    ) AS t(code, name, ord);
    v_count := v_count + 3;

    -- ========================================
    -- EQUITY (3000-3999)
    -- ========================================

    -- 3000: Equity (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '3000', 'Equity', 'equity', 'equity', 'credit', 3000, true, 'I', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'equity', 'equity', v_parent_id, 1, 'credit', ord, 'I', p_created_by
    FROM (VALUES
        ('3100', 'Share Capital', 3100),
        ('3200', 'Reserves and Surplus', 3200),
        ('3210', 'Retained Earnings', 3210),
        ('3220', 'Current Year Profit/Loss', 3220),
        ('3300', 'Other Equity', 3300)
    ) AS t(code, name, ord);
    v_count := v_count + 5;

    -- ========================================
    -- INCOME (4000-4999)
    -- ========================================

    -- 4000: Revenue from Operations (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4000', 'Revenue from Operations', 'income', 'operating_income', 'credit', 4000, true, 'I', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    -- 4100: Sales Revenue
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4100', 'Sales Revenue', 'income', 'operating_income', v_parent_id, 1, 'credit', 4100, true, 'I(a)', p_created_by);
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'income', 'operating_income',
           (SELECT id FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '4100'),
           2, 'credit', ord, gst, 'I(a)', p_created_by
    FROM (VALUES
        ('4110', 'Domestic Sales - Services', 4110, 'taxable'),
        ('4120', 'Domestic Sales - Products', 4120, 'taxable'),
        ('4130', 'Export Sales - Services', 4130, 'exempt'),
        ('4140', 'Export Sales - Products', 4140, 'exempt')
    ) AS t(code, name, ord, gst);
    v_count := v_count + 4;

    -- 4200: Other Operating Revenue
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '4200', 'Other Operating Revenue', 'income', 'operating_income', v_parent_id, 1, 'credit', 4200, 'I(b)', p_created_by);
    v_count := v_count + 1;

    -- 4500: Other Income
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4500', 'Other Income', 'income', 'other_income', 'credit', 4500, true, 'II', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'income', 'other_income', v_parent_id, 1, 'credit', ord, 'II', p_created_by
    FROM (VALUES
        ('4510', 'Interest Income', 4510),
        ('4520', 'Dividend Income', 4520),
        ('4530', 'Rental Income', 4530),
        ('4540', 'Foreign Exchange Gain', 4540),
        ('4550', 'Miscellaneous Income', 4550)
    ) AS t(code, name, ord);
    v_count := v_count + 5;

    -- ========================================
    -- EXPENSES (5000-5999)
    -- ========================================

    -- 5000: Cost of Services/Goods (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5000', 'Cost of Revenue', 'expense', 'cost_of_revenue', 'debit', 5000, true, 'III', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    -- FIXED: Using 5010, 5020, 5030 instead of 5100, 5200, 5300 to avoid duplicate with Employee Benefits (5200)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'cost_of_revenue', v_parent_id, 1, 'debit', ord, 'III', p_created_by
    FROM (VALUES
        ('5010', 'Direct Labor', 5010),
        ('5020', 'Contractor Payments', 5020),
        ('5030', 'Subcontracting Expenses', 5030)
    ) AS t(code, name, ord);
    v_count := v_count + 3;

    -- 5200: Employee Benefits (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5200', 'Employee Benefit Expenses', 'expense', 'operating_expense', 'debit', 5200, true, 'IV', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'operating_expense', v_parent_id, 1, 'debit', ord, 'IV', p_created_by
    FROM (VALUES
        ('5210', 'Salaries and Wages', 5210),
        ('5220', 'Employer PF Contribution', 5220),
        ('5230', 'Employer ESI Contribution', 5230),
        ('5240', 'Staff Welfare', 5240),
        ('5250', 'Gratuity Expense', 5250),
        ('5260', 'Bonus and Incentives', 5260)
    ) AS t(code, name, ord);
    v_count := v_count + 6;

    -- 5400: Depreciation (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5400', 'Depreciation and Amortization', 'expense', 'depreciation', 'debit', 5400, true, 'V', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'depreciation', v_parent_id, 1, 'debit', ord, 'V', p_created_by
    FROM (VALUES
        ('5410', 'Depreciation - Buildings', 5410),
        ('5420', 'Depreciation - Plant & Machinery', 5420),
        ('5430', 'Depreciation - Furniture', 5430),
        ('5440', 'Depreciation - Computers', 5440),
        ('5450', 'Depreciation - Vehicles', 5450),
        ('5460', 'Amortization - Intangibles', 5460)
    ) AS t(code, name, ord);
    v_count := v_count + 6;

    -- 5500: Other Expenses (Parent)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5500', 'Other Expenses', 'expense', 'operating_expense', 'debit', 5500, true, 'VI', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'operating_expense', v_parent_id, 1, 'debit', ord, 'VI', p_created_by
    FROM (VALUES
        ('5510', 'Rent Expense', 5510),
        ('5520', 'Utilities', 5520),
        ('5530', 'Communication Expenses', 5530),
        ('5540', 'Travel and Conveyance', 5540),
        ('5550', 'Professional Fees', 5550),
        ('5560', 'Office Expenses', 5560),
        ('5570', 'Insurance', 5570),
        ('5580', 'Bank Charges', 5580),
        ('5590', 'Subscriptions and Software', 5590)
    ) AS t(code, name, ord);
    v_count := v_count + 9;

    -- 5600: Finance Costs
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5600', 'Finance Costs', 'expense', 'finance_cost', 'debit', 5600, true, 'VII', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'finance_cost', v_parent_id, 1, 'debit', ord, 'VII', p_created_by
    FROM (VALUES
        ('5610', 'Interest on Borrowings', 5610),
        ('5620', 'Interest on Delayed Payments', 5620),
        ('5630', 'Foreign Exchange Loss', 5630)
    ) AS t(code, name, ord);
    v_count := v_count + 3;

    -- 5700: Tax Expense
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5700', 'Tax Expense', 'expense', 'tax_expense', 'debit', 5700, true, 'VIII', p_created_by)
    RETURNING id INTO v_parent_id;
    v_count := v_count + 1;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'tax_expense', v_parent_id, 1, 'debit', ord, 'VIII', p_created_by
    FROM (VALUES
        ('5710', 'Current Tax', 5710),
        ('5720', 'Deferred Tax', 5720)
    ) AS t(code, name, ord);
    v_count := v_count + 2;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION create_default_chart_of_accounts(UUID, UUID) IS 'Creates default Indian Schedule III chart of accounts for a company (fixed duplicate 5200 issue)';
