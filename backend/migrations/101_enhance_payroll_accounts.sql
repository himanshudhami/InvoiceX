-- ============================================================================
-- Migration 101: Enhance Chart of Accounts for Payroll
-- Description: Split PF/ESI Payable into Employee/Employer, add Gratuity/Bonus/LWF Payable
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Function to add account if not exists
-- -----------------------------------------------------------------------------
CREATE OR REPLACE FUNCTION add_payroll_account_if_not_exists(
    p_company_id UUID,
    p_account_code VARCHAR(20),
    p_account_name VARCHAR(200),
    p_account_type VARCHAR(50),
    p_account_subtype VARCHAR(50),
    p_parent_code VARCHAR(20),
    p_normal_balance VARCHAR(10),
    p_sort_order INTEGER,
    p_schedule_reference VARCHAR(50) DEFAULT 'II(a)'
) RETURNS VOID AS $$
DECLARE
    v_parent_id UUID;
BEGIN
    -- Check if account already exists
    IF EXISTS (SELECT 1 FROM chart_of_accounts
               WHERE company_id = p_company_id AND account_code = p_account_code) THEN
        RETURN;
    END IF;

    -- Get parent account ID if specified
    IF p_parent_code IS NOT NULL THEN
        SELECT id INTO v_parent_id
        FROM chart_of_accounts
        WHERE company_id = p_company_id AND account_code = p_parent_code;
    END IF;

    -- Insert new account
    INSERT INTO chart_of_accounts (
        id, company_id, account_code, account_name, account_type,
        account_subtype, parent_account_id, normal_balance, sort_order,
        schedule_reference, is_system_account, is_active,
        opening_balance, current_balance, created_at, updated_at
    ) VALUES (
        gen_random_uuid(), p_company_id, p_account_code, p_account_name,
        p_account_type, p_account_subtype, v_parent_id, p_normal_balance, p_sort_order,
        p_schedule_reference, true, true, 0, 0, NOW(), NOW()
    );
END;
$$ LANGUAGE plpgsql;

-- -----------------------------------------------------------------------------
-- 2. Add new payroll-related accounts for all companies
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    company_rec RECORD;
BEGIN
    FOR company_rec IN SELECT id FROM companies LOOP

        -- Split PF Payable into Employee and Employer portions
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2221', 'Employee PF Contribution Payable',
            'liability', 'current_liability', '2220', 'credit', 2221
        );

        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2222', 'Employer PF Contribution Payable',
            'liability', 'current_liability', '2220', 'credit', 2222
        );

        -- Split ESI Payable into Employee and Employer portions
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2231', 'Employee ESI Contribution Payable',
            'liability', 'current_liability', '2230', 'credit', 2231
        );

        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2232', 'Employer ESI Contribution Payable',
            'liability', 'current_liability', '2230', 'credit', 2232
        );

        -- Add LWF Payable (Labour Welfare Fund)
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2245', 'LWF Payable',
            'liability', 'current_liability', '2200', 'credit', 2245
        );

        -- Add Gratuity Payable (separate from expense provision)
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2250', 'Gratuity Payable',
            'liability', 'current_liability', '2200', 'credit', 2250
        );

        -- Add Bonus Payable
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2260', 'Bonus Payable',
            'liability', 'current_liability', '2200', 'credit', 2260
        );

        -- Add Reimbursements Payable
        PERFORM add_payroll_account_if_not_exists(
            company_rec.id, '2270', 'Reimbursements Payable',
            'liability', 'current_liability', '2200', 'credit', 2270
        );

    END LOOP;
END $$;

-- -----------------------------------------------------------------------------
-- 3. Clean up function
-- -----------------------------------------------------------------------------
DROP FUNCTION IF EXISTS add_payroll_account_if_not_exists;
