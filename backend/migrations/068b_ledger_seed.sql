-- Migration: 165_ledger_seed.sql
-- Description: COA seeding, default mappings, and posting rules
-- Replaces: 069, 070, 157

-- ============================================================================
-- CREATE DEFAULT COA (Indian Schedule III)
-- ============================================================================

CREATE OR REPLACE FUNCTION create_default_chart_of_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER := 0;
    v_parent_1000 UUID; v_parent_1100 UUID; v_parent_1130 UUID; v_parent_1140 UUID;
    v_parent_1150 UUID; v_parent_1500 UUID; v_parent_1600 UUID; v_parent_1700 UUID;
    v_parent_1800 UUID; v_parent_2000 UUID; v_parent_2200 UUID; v_parent_2250 UUID;
    v_parent_2300 UUID; v_parent_2500 UUID; v_parent_3000 UUID; v_parent_4000 UUID;
    v_parent_4100 UUID; v_parent_4500 UUID; v_parent_5000 UUID; v_parent_5200 UUID;
    v_parent_5400 UUID; v_parent_5500 UUID; v_parent_5600 UUID; v_parent_5700 UUID;
BEGIN
    -- ASSETS
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1000', 'Current Assets', 'asset', 'current_asset', 'debit', 1000, true, 'II(A)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1000;
    SELECT id INTO v_parent_1000 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1000';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1100', 'Cash and Cash Equivalents', 'asset', 'current_asset', v_parent_1000, 1, 'debit', 1100, true, 'II(A)(a)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1100;
    SELECT id INTO v_parent_1100 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1100';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset', v_parent_1100, 2, 'debit', ord, 'II(A)(a)', p_created_by
    FROM (VALUES ('1110','Petty Cash',1110),('1111','Cash in Hand',1111),('1112','Bank Accounts - Current',1112),('1113','Bank Accounts - Savings',1113),('1114','Fixed Deposits (< 3 months)',1114)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- Trade Receivables (Control Account)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1120', 'Trade Receivables', 'asset', 'current_asset', v_parent_1000, 1, 'debit', 1120, true, 'receivables', true, 'II(A)(b)', p_created_by)
    ON CONFLICT DO NOTHING;

    -- TDS Receivable
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1130', 'TDS Receivable', 'asset', 'current_asset', v_parent_1000, 1, 'debit', 1130, true, 'tds_receivable', true, 'II(A)(c)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1130;
    SELECT id INTO v_parent_1130 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1130';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset', v_parent_1130, 2, 'debit', ord, 'II(A)(c)', p_created_by
    FROM (VALUES ('1131','TDS Receivable - 194J',1131),('1132','TDS Receivable - 194C',1132),('1133','TDS Receivable - 194H',1133),('1134','TDS Receivable - Other',1134)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- GST Input
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, is_system_account, gst_treatment, schedule_reference, created_by)
    VALUES (p_company_id, '1140', 'GST Input Credit', 'asset', 'current_asset', v_parent_1000, 1, 'debit', 1140, true, 'gst_input', true, 'taxable', 'II(A)(d)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1140;
    SELECT id INTO v_parent_1140 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1140';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset', v_parent_1140, 2, 'debit', ord, 'taxable', 'II(A)(d)', p_created_by
    FROM (VALUES ('1141','CGST Input',1141),('1142','SGST Input',1142),('1143','IGST Input',1143),('1144','GST Cess Input',1144)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- Advances
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1150', 'Advances and Prepayments', 'asset', 'current_asset', v_parent_1000, 1, 'debit', 1150, 'II(A)(e)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1150;
    SELECT id INTO v_parent_1150 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1150';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset', v_parent_1150, 2, 'debit', ord, 'II(A)(e)', p_created_by
    FROM (VALUES ('1151','Advance to Suppliers',1151),('1152','Prepaid Expenses',1152),('1153','Advance Tax',1153),('1154','Security Deposits',1154)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- Non-Current Assets
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '1500', 'Non-Current Assets', 'asset', 'non_current_asset', 'debit', 1500, true, 'I', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1500;
    SELECT id INTO v_parent_1500 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1500';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1600', 'Property, Plant and Equipment', 'asset', 'fixed_asset', v_parent_1500, 1, 'debit', 1600, 'I(A)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1600;
    SELECT id INTO v_parent_1600 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1600';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'fixed_asset', v_parent_1600, 2, 'debit', ord, 'I(A)', p_created_by
    FROM (VALUES ('1610','Land',1610),('1620','Buildings',1620),('1630','Plant and Machinery',1630),('1640','Furniture and Fixtures',1640),('1650','Office Equipment',1650),('1660','Computers and IT Equipment',1660),('1670','Vehicles',1670)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1700', 'Accumulated Depreciation', 'asset', 'contra_asset', v_parent_1500, 1, 'credit', 1700, 'I(A)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1700;
    SELECT id INTO v_parent_1700 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1700';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'contra_asset', v_parent_1700, 2, 'credit', ord, 'I(A)', p_created_by
    FROM (VALUES ('1720','Acc. Dep. - Buildings',1720),('1730','Acc. Dep. - Plant and Machinery',1730),('1740','Acc. Dep. - Furniture',1740),('1750','Acc. Dep. - Office Equipment',1750),('1760','Acc. Dep. - Computers',1760),('1770','Acc. Dep. - Vehicles',1770)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '1800', 'Intangible Assets', 'asset', 'intangible_asset', v_parent_1500, 1, 'debit', 1800, 'I(B)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_1800;
    SELECT id INTO v_parent_1800 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '1800';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'asset', 'intangible_asset', v_parent_1800, 2, 'debit', ord, 'I(B)', p_created_by
    FROM (VALUES ('1810','Software Licenses',1810),('1820','Patents and Trademarks',1820),('1830','Goodwill',1830)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- LIABILITIES
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2000', 'Current Liabilities', 'liability', 'current_liability', 'credit', 2000, true, 'II', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_2000;
    SELECT id INTO v_parent_2000 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2000';

    -- Trade Payables (Control Account)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2100', 'Trade Payables', 'liability', 'current_liability', v_parent_2000, 1, 'credit', 2100, true, 'payables', true, 'II(a)', p_created_by)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2110', 'Salary and Wages Payable', 'liability', 'current_liability', v_parent_2000, 1, 'credit', 2110, true, 'II(b)', p_created_by)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2200', 'Statutory Dues', 'liability', 'current_liability', v_parent_2000, 1, 'credit', 2200, true, 'II(c)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_2200;
    SELECT id INTO v_parent_2200 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2200';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability', v_parent_2200, 2, 'credit', ord, is_ctrl, ctrl_type, 'II(c)', p_created_by
    FROM (VALUES ('2211','TDS Payable',2211,true,'tds_payable'),('2212','TDS Payable - Salary',2212,false,NULL),('2213','TDS Payable - Professional',2213,false,NULL),('2220','PF Payable',2220,false,NULL),('2221','Employee PF Contribution',2221,false,NULL),('2222','Employer PF Contribution',2222,false,NULL),('2230','ESI Payable',2230,false,NULL),('2240','Professional Tax Payable',2240,false,NULL)) AS t(code,name,ord,is_ctrl,ctrl_type)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, is_system_account, gst_treatment, schedule_reference, created_by)
    VALUES (p_company_id, '2250', 'GST Payable', 'liability', 'current_liability', v_parent_2000, 1, 'credit', 2250, true, 'gst_output', true, 'taxable', 'II(c)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_2250;
    SELECT id INTO v_parent_2250 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2250';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability', v_parent_2250, 2, 'credit', ord, 'taxable', 'II(c)', p_created_by
    FROM (VALUES ('2251','CGST Payable',2251),('2252','SGST Payable',2252),('2253','IGST Payable',2253),('2254','GST Cess Payable',2254)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '2300', 'Other Current Liabilities', 'liability', 'current_liability', v_parent_2000, 1, 'credit', 2300, 'II(d)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_2300;
    SELECT id INTO v_parent_2300 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2300';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability', v_parent_2300, 2, 'credit', ord, 'II(d)', p_created_by
    FROM (VALUES ('2310','Advance from Customers',2310),('2320','Accrued Expenses',2320),('2330','Provisions',2330)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '2500', 'Non-Current Liabilities', 'liability', 'non_current_liability', 'credit', 2500, true, 'I', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_2500;
    SELECT id INTO v_parent_2500 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '2500';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_control_account, control_account_type, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'liability', 'non_current_liability', v_parent_2500, 1, 'credit', ord, is_ctrl, ctrl_type, 'I', p_created_by
    FROM (VALUES ('2510','Long-Term Borrowings',2510,true,'loans'),('2520','Deferred Tax Liabilities',2520,false,NULL),('2530','Long-Term Provisions',2530,false,NULL)) AS t(code,name,ord,is_ctrl,ctrl_type)
    ON CONFLICT DO NOTHING;

    -- EQUITY
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '3000', 'Equity', 'equity', 'equity', 'credit', 3000, true, 'I', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_3000;
    SELECT id INTO v_parent_3000 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '3000';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'equity', 'equity', v_parent_3000, 1, 'credit', ord, 'I', p_created_by
    FROM (VALUES ('3100','Share Capital',3100),('3200','Reserves and Surplus',3200),('3210','Retained Earnings',3210),('3220','Current Year Profit/Loss',3220),('3300','Other Equity',3300)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- INCOME
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4000', 'Revenue from Operations', 'income', 'operating_income', 'credit', 4000, true, 'I', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_4000;
    SELECT id INTO v_parent_4000 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '4000';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4100', 'Sales Revenue', 'income', 'operating_income', v_parent_4000, 1, 'credit', 4100, true, 'I(a)', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_4100;
    SELECT id INTO v_parent_4100 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '4100';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'income', 'operating_income', v_parent_4100, 2, 'credit', ord, gst, 'I(a)', p_created_by
    FROM (VALUES ('4110','Domestic Sales - Services',4110,'taxable'),('4120','Domestic Sales - Products',4120,'taxable'),('4130','Export Sales - Services',4130,'exempt'),('4140','Export Sales - Products',4140,'exempt')) AS t(code,name,ord,gst)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    VALUES (p_company_id, '4200', 'Other Operating Revenue', 'income', 'operating_income', v_parent_4000, 1, 'credit', 4200, 'I(b)', p_created_by)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '4500', 'Other Income', 'income', 'other_income', 'credit', 4500, true, 'II', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_4500;
    SELECT id INTO v_parent_4500 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '4500';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'income', 'other_income', v_parent_4500, 1, 'credit', ord, 'II', p_created_by
    FROM (VALUES ('4510','Interest Income',4510),('4520','Dividend Income',4520),('4530','Rental Income',4530),('4540','Foreign Exchange Gain',4540),('4550','Miscellaneous Income',4550)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- EXPENSES
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5000', 'Cost of Revenue', 'expense', 'cost_of_revenue', 'debit', 5000, true, 'III', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5000;
    SELECT id INTO v_parent_5000 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5000';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'cost_of_revenue', v_parent_5000, 1, 'debit', ord, 'III', p_created_by
    FROM (VALUES ('5010','Direct Labor',5010),('5020','Contractor Payments',5020),('5030','Subcontracting Expenses',5030)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5200', 'Employee Benefit Expenses', 'expense', 'operating_expense', 'debit', 5200, true, 'IV', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5200;
    SELECT id INTO v_parent_5200 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5200';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'operating_expense', v_parent_5200, 1, 'debit', ord, 'IV', p_created_by
    FROM (VALUES ('5210','Salaries and Wages',5210),('5220','Employer PF Contribution',5220),('5230','Employer ESI Contribution',5230),('5240','Staff Welfare',5240),('5250','Gratuity Expense',5250),('5260','Bonus and Incentives',5260)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5400', 'Depreciation and Amortization', 'expense', 'depreciation', 'debit', 5400, true, 'V', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5400;
    SELECT id INTO v_parent_5400 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5400';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'depreciation', v_parent_5400, 1, 'debit', ord, 'V', p_created_by
    FROM (VALUES ('5410','Depreciation - Buildings',5410),('5420','Depreciation - Plant & Machinery',5420),('5430','Depreciation - Furniture',5430),('5440','Depreciation - Computers',5440),('5450','Depreciation - Vehicles',5450),('5460','Amortization - Intangibles',5460)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5500', 'Other Expenses', 'expense', 'operating_expense', 'debit', 5500, true, 'VI', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5500;
    SELECT id INTO v_parent_5500 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5500';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'operating_expense', v_parent_5500, 1, 'debit', ord, 'VI', p_created_by
    FROM (VALUES ('5510','Rent Expense',5510),('5520','Utilities',5520),('5530','Communication Expenses',5530),('5540','Travel and Conveyance',5540),('5550','Professional Fees',5550),('5560','Office Expenses',5560),('5570','Insurance',5570),('5580','Bank Charges',5580),('5590','Subscriptions and Software',5590)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5600', 'Finance Costs', 'expense', 'finance_cost', 'debit', 5600, true, 'VII', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5600;
    SELECT id INTO v_parent_5600 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5600';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'finance_cost', v_parent_5600, 1, 'debit', ord, 'VII', p_created_by
    FROM (VALUES ('5610','Interest on Borrowings',5610),('5620','Interest on Delayed Payments',5620),('5630','Foreign Exchange Loss',5630)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, schedule_reference, created_by)
    VALUES (p_company_id, '5700', 'Tax Expense', 'expense', 'tax_expense', 'debit', 5700, true, 'VIII', p_created_by)
    ON CONFLICT DO NOTHING RETURNING id INTO v_parent_5700;
    SELECT id INTO v_parent_5700 FROM chart_of_accounts WHERE company_id = p_company_id AND account_code = '5700';

    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, parent_account_id, depth_level, normal_balance, sort_order, schedule_reference, created_by)
    SELECT p_company_id, code, name, 'expense', 'tax_expense', v_parent_5700, 1, 'debit', ord, 'VIII', p_created_by
    FROM (VALUES ('5710','Current Tax',5710),('5720','Deferred Tax',5720)) AS t(code,name,ord)
    ON CONFLICT DO NOTHING;

    -- SUSPENSE ACCOUNTS
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype, normal_balance, sort_order, is_system_account, created_by)
    SELECT p_company_id, code, name, acct_type, subtype, normal_bal, ord, true, p_created_by
    FROM (VALUES ('9990','Tally Import Suspense - Assets','asset','current_asset','debit',9990),('9991','Tally Import Suspense - Liabilities','liability','current_liability','credit',9991),('9992','Tally Import Suspense - Income','income','other_income','credit',9992),('9993','Tally Import Suspense - Expenses','expense','operating_expense','debit',9993),('9994','Tally Import Suspense - Equity','equity','equity','credit',9994)) AS t(code,name,acct_type,subtype,normal_bal,ord)
    ON CONFLICT DO NOTHING;

    SELECT COUNT(*) INTO v_count FROM chart_of_accounts WHERE company_id = p_company_id;
    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- SEED TALLY DEFAULT MAPPINGS
-- ============================================================================

CREATE OR REPLACE FUNCTION seed_tally_default_mappings(p_company_id UUID)
RETURNS void AS $$
BEGIN
    INSERT INTO tally_field_mappings (company_id, mapping_type, tally_group_name, tally_name, target_entity, target_account_type, is_system_default, priority)
    VALUES
        (p_company_id, 'ledger_group', 'Sundry Creditors', '', 'vendors', NULL, true, 10),
        (p_company_id, 'ledger_group', 'CONSULTANTS', '', 'vendors', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Consultants', '', 'vendors', NULL, true, 10),
        (p_company_id, 'ledger_group', 'CONTRACTORS', '', 'vendors', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Contractors', '', 'vendors', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Sundry Debtors', '', 'customers', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Bank Accounts', '', 'bank_accounts', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Bank OD A/c', '', 'bank_accounts', NULL, true, 10),
        (p_company_id, 'ledger_group', 'Cash-in-hand', '', 'chart_of_accounts', 'asset', true, 10),
        (p_company_id, 'ledger_group', 'Purchase Accounts', '', 'chart_of_accounts', 'expense', true, 20),
        (p_company_id, 'ledger_group', 'Sales Accounts', '', 'chart_of_accounts', 'income', true, 20),
        (p_company_id, 'ledger_group', 'Direct Expenses', '', 'chart_of_accounts', 'expense', true, 20),
        (p_company_id, 'ledger_group', 'Indirect Expenses', '', 'chart_of_accounts', 'expense', true, 20),
        (p_company_id, 'ledger_group', 'Direct Incomes', '', 'chart_of_accounts', 'income', true, 20),
        (p_company_id, 'ledger_group', 'Indirect Incomes', '', 'chart_of_accounts', 'income', true, 20),
        (p_company_id, 'ledger_group', 'Duties & Taxes', '', 'chart_of_accounts', 'liability', true, 20),
        (p_company_id, 'ledger_group', 'Fixed Assets', '', 'chart_of_accounts', 'asset', true, 20),
        (p_company_id, 'ledger_group', 'Capital Account', '', 'chart_of_accounts', 'equity', true, 20),
        (p_company_id, 'ledger_group', 'Suspense A/c', '', 'suspense', NULL, true, 100)
    ON CONFLICT ON CONSTRAINT uq_tally_mapping DO NOTHING;
END;
$$ LANGUAGE plpgsql;

-- ============================================================================
-- SEED DEFAULT POSTING RULES
-- ============================================================================

CREATE OR REPLACE FUNCTION seed_default_posting_rules(p_company_id UUID)
RETURNS void AS $$
BEGIN
    INSERT INTO posting_rules (company_id, rule_code, rule_name, source_type, trigger_event, conditions_json, posting_template, priority, is_active, is_system_rule)
    VALUES
        (p_company_id, 'INV_DEFAULT', 'Default Sales Invoice', 'invoice', 'on_finalize', '{}',
         '{"descriptionTemplate":"Sales Invoice - {source_number}","lines":[{"side":"debit","accountCode":"1120","amountField":"total_amount","subledgerType":"customer","subledgerField":"customer_id"},{"side":"credit","accountCode":"4110","amountField":"subtotal"},{"side":"credit","accountCode":"2251","amountField":"total_cgst","skipIfZero":true},{"side":"credit","accountCode":"2252","amountField":"total_sgst","skipIfZero":true},{"side":"credit","accountCode":"2253","amountField":"total_igst","skipIfZero":true}]}'::jsonb, 100, true, true),
        (p_company_id, 'PMT_DEFAULT', 'Default Payment Received', 'payment', 'on_finalize', '{}',
         '{"descriptionTemplate":"Payment - {source_number}","lines":[{"side":"debit","accountCode":"1112","amountField":"amount"},{"side":"credit","accountCode":"1120","amountField":"amount","subledgerType":"customer","subledgerField":"customer_id"}]}'::jsonb, 100, true, true),
        (p_company_id, 'VINV_DEFAULT', 'Default Vendor Invoice', 'vendor_invoice', 'on_finalize', '{}',
         '{"descriptionTemplate":"Vendor Invoice - {source_number}","lines":[{"side":"debit","accountCode":"5020","amountField":"subtotal"},{"side":"debit","accountCode":"1141","amountField":"total_cgst","skipIfZero":true},{"side":"debit","accountCode":"1142","amountField":"total_sgst","skipIfZero":true},{"side":"debit","accountCode":"1143","amountField":"total_igst","skipIfZero":true},{"side":"credit","accountCode":"2100","amountField":"total_amount","subledgerType":"vendor","subledgerField":"vendor_id"}]}'::jsonb, 100, true, true),
        (p_company_id, 'VPMT_DEFAULT', 'Default Vendor Payment', 'vendor_payment', 'on_finalize', '{}',
         '{"descriptionTemplate":"Vendor Payment - {source_number}","lines":[{"side":"debit","accountCode":"2100","amountField":"amount","subledgerType":"vendor","subledgerField":"vendor_id"},{"side":"credit","accountCode":"1112","amountField":"amount"}]}'::jsonb, 100, true, true)
    ON CONFLICT (company_id, rule_code, financial_year) DO NOTHING;
END;
$$ LANGUAGE plpgsql;
