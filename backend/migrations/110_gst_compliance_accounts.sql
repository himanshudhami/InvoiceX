-- Migration: GST Compliance Enhancement Accounts
-- Purpose: Add UTGST, RCM, ITC Blocked, and GST TDS/TCS accounts
-- Following: Indian GST Act, Notification 13/2017 (RCM), Section 17(5) (ITC Blocked)

-- ============================================
-- Add new GST accounts to existing companies
-- ============================================

-- Function to add GST compliance accounts to a company
CREATE OR REPLACE FUNCTION add_gst_compliance_accounts(p_company_id UUID, p_created_by UUID DEFAULT NULL)
RETURNS INTEGER AS $$
DECLARE
    v_count INTEGER := 0;
    v_gst_input_parent_id UUID;
    v_gst_output_parent_id UUID;
    v_tds_receivable_parent_id UUID;
    v_statutory_dues_parent_id UUID;
BEGIN
    -- Get parent account IDs
    SELECT id INTO v_gst_input_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '1140';

    SELECT id INTO v_gst_output_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '2250';

    SELECT id INTO v_tds_receivable_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '1130';

    SELECT id INTO v_statutory_dues_parent_id FROM chart_of_accounts
    WHERE company_id = p_company_id AND account_code = '2200';

    -- ========================================
    -- GST INPUT ACCOUNTS (Assets)
    -- ========================================

    -- 1145: UTGST Input (for Union Territory transactions)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '1145', 'UTGST Input', 'asset', 'current_asset',
        v_gst_input_parent_id, 2, 'debit', 1145, 'taxable', 'II(A)(d)',
        'Union Territory GST Input Credit - for UT transactions (Delhi, Chandigarh, etc.)', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- 1146-1148: RCM ITC Claimable (Reverse Charge)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'asset', 'current_asset',
           v_gst_input_parent_id, 2, 'debit', ord, 'taxable', 'II(A)(d)', descr, p_created_by
    FROM (VALUES
        ('1146', 'RCM CGST ITC Claimable', 1146, 'CGST Input Tax Credit claimable after RCM payment - Section 9(3)'),
        ('1147', 'RCM SGST ITC Claimable', 1147, 'SGST Input Tax Credit claimable after RCM payment - Section 9(3)'),
        ('1148', 'RCM IGST ITC Claimable', 1148, 'IGST Input Tax Credit claimable after RCM payment - Section 9(3)')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 3;

    -- 1149: ITC Blocked (Section 17(5))
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '1149', 'ITC Blocked - Section 17(5)', 'asset', 'current_asset',
        v_gst_input_parent_id, 2, 'debit', 1149, 'blocked', 'II(A)(d)',
        'Blocked Input Tax Credit per Section 17(5) - motor vehicles, food, club membership, etc.', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- 1155: GST TDS Receivable (Section 51)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '1155', 'GST TDS Receivable', 'asset', 'current_asset',
        v_tds_receivable_parent_id, 2, 'debit', 1155, 'taxable', 'II(A)(c)',
        'GST TDS deducted by government departments - Section 51 CGST Act', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- 1156: GST TCS Receivable (Section 52)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '1156', 'GST TCS Receivable', 'asset', 'current_asset',
        v_tds_receivable_parent_id, 2, 'debit', 1156, 'taxable', 'II(A)(c)',
        'GST TCS collected by e-commerce operators - Section 52 CGST Act', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- ========================================
    -- GST OUTPUT/PAYABLE ACCOUNTS (Liabilities)
    -- ========================================

    -- 2255: UTGST Payable
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '2255', 'UTGST Payable', 'liability', 'current_liability',
        v_gst_output_parent_id, 2, 'credit', 2255, 'taxable', 'II(c)',
        'Union Territory GST Output Payable - for UT transactions', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- 2256-2259: RCM GST Payable (Reverse Charge Mechanism)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    SELECT p_company_id, code, name, 'liability', 'current_liability',
           v_statutory_dues_parent_id, 2, 'credit', ord, 'taxable', 'II(c)', descr, p_created_by
    FROM (VALUES
        ('2256', 'RCM CGST Payable', 2256, 'CGST payable under Reverse Charge - Section 9(3), Notification 13/2017'),
        ('2257', 'RCM SGST Payable', 2257, 'SGST payable under Reverse Charge - Section 9(3), Notification 13/2017'),
        ('2258', 'RCM IGST Payable', 2258, 'IGST payable under Reverse Charge - Section 5(3) IGST Act'),
        ('2259', 'RCM UTGST Payable', 2259, 'UTGST payable under Reverse Charge - for Union Territories')
    ) AS t(code, name, ord, descr)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 4;

    -- 2260: GST TDS Payable (Section 51)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '2260', 'GST TDS Payable', 'liability', 'current_liability',
        v_statutory_dues_parent_id, 2, 'credit', 2260, 'taxable', 'II(c)',
        'GST TDS to be deducted on payments to vendors - Section 51 CGST Act (Govt depts)', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    -- 2261: GST TCS Payable (Section 52)
    INSERT INTO chart_of_accounts (company_id, account_code, account_name, account_type, account_subtype,
        parent_account_id, depth_level, normal_balance, sort_order, gst_treatment, schedule_reference,
        description, created_by)
    VALUES (p_company_id, '2261', 'GST TCS Payable', 'liability', 'current_liability',
        v_statutory_dues_parent_id, 2, 'credit', 2261, 'taxable', 'II(c)',
        'GST TCS to collect as e-commerce operator - Section 52 CGST Act', p_created_by)
    ON CONFLICT (company_id, account_code) DO NOTHING;
    v_count := v_count + 1;

    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- Apply to all existing companies
-- ============================================
DO $$
DECLARE
    company_record RECORD;
    accounts_added INTEGER;
BEGIN
    FOR company_record IN SELECT id FROM companies LOOP
        SELECT add_gst_compliance_accounts(company_record.id) INTO accounts_added;
        RAISE NOTICE 'Added % GST compliance accounts to company %', accounts_added, company_record.id;
    END LOOP;
END $$;

-- ============================================
-- Update the main create_default_chart_of_accounts function
-- to include these accounts for new companies
-- ============================================

-- Create a trigger to automatically add GST compliance accounts for new companies
CREATE OR REPLACE FUNCTION trigger_add_gst_compliance_accounts()
RETURNS TRIGGER AS $$
BEGIN
    -- Called after create_default_chart_of_accounts runs
    -- Check if GST accounts already exist (idempotent)
    IF NOT EXISTS (SELECT 1 FROM chart_of_accounts WHERE company_id = NEW.id AND account_code = '1145') THEN
        PERFORM add_gst_compliance_accounts(NEW.id, NEW.created_by);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Note: The trigger would be created in application code after company creation
-- or the main create_default_chart_of_accounts function should be updated

COMMENT ON FUNCTION add_gst_compliance_accounts IS 'Adds GST compliance accounts including UTGST, RCM, ITC Blocked per Indian GST Act FY 2024-25';
