-- Seed calculation rule templates and default rules
-- These provide a starting point for companies to configure payroll calculations

-- =====================================================
-- CALCULATION RULE TEMPLATES
-- Pre-built templates that can be copied when creating new rules
-- =====================================================

INSERT INTO calculation_rule_templates (id, name, description, category, component_type, component_code, rule_type, formula_config, default_conditions, is_active, display_order, created_at)
VALUES
-- PF Templates
(
    'a1b2c3d4-1111-4000-8000-000000000001',
    'PF Employee - Ceiling Based',
    'Standard PF employee contribution with ceiling. Calculates 12% of (Basic + DA), capped at Rs.15,000 wage base. This is the statutory minimum approach.',
    'statutory',
    'deduction',
    'PF_EMPLOYEE',
    'percentage',
    '{"rate": 12, "of": "pf_wage", "ceiling": 15000, "description": "12% of PF wage (Basic + DA) capped at Rs.15,000"}'::jsonb,
    '{}'::jsonb,
    true,
    10,
    NOW()
),
(
    'a1b2c3d4-1111-4000-8000-000000000002',
    'PF Employee - Actual Wage',
    'PF employee contribution on actual wages. Calculates 12% of (Basic + DA) without any ceiling. Results in higher contribution for high earners.',
    'statutory',
    'deduction',
    'PF_EMPLOYEE',
    'percentage',
    '{"rate": 12, "of": "pf_wage", "description": "12% of actual PF wage (Basic + DA) - no ceiling applied"}'::jsonb,
    '{}'::jsonb,
    true,
    11,
    NOW()
),
(
    'a1b2c3d4-1111-4000-8000-000000000003',
    'PF Employer - Ceiling Based',
    'Standard PF employer contribution with ceiling. Same as employee contribution.',
    'statutory',
    'employer_contribution',
    'PF_EMPLOYER',
    'percentage',
    '{"rate": 12, "of": "pf_wage", "ceiling": 15000, "description": "12% of PF wage (Basic + DA) capped at Rs.15,000"}'::jsonb,
    '{}'::jsonb,
    true,
    12,
    NOW()
),
(
    'a1b2c3d4-1111-4000-8000-000000000004',
    'PF Employer - Actual Wage',
    'PF employer contribution on actual wages without ceiling.',
    'statutory',
    'employer_contribution',
    'PF_EMPLOYER',
    'percentage',
    '{"rate": 12, "of": "pf_wage", "description": "12% of actual PF wage (Basic + DA) - no ceiling applied"}'::jsonb,
    '{}'::jsonb,
    true,
    13,
    NOW()
),

-- ESI Templates
(
    'a1b2c3d4-2222-4000-8000-000000000001',
    'ESI Employee - Standard',
    'ESI employee contribution at 0.75% of gross earnings. Only applicable when gross salary is Rs.21,000/month or below.',
    'statutory',
    'deduction',
    'ESI_EMPLOYEE',
    'percentage',
    '{"rate": 0.75, "of": "gross_earnings", "description": "0.75% of gross earnings"}'::jsonb,
    '{}'::jsonb,
    true,
    20,
    NOW()
),
(
    'a1b2c3d4-2222-4000-8000-000000000002',
    'ESI Employer - Standard',
    'ESI employer contribution at 3.25% of gross earnings.',
    'statutory',
    'employer_contribution',
    'ESI_EMPLOYER',
    'percentage',
    '{"rate": 3.25, "of": "gross_earnings", "description": "3.25% of gross earnings"}'::jsonb,
    '{}'::jsonb,
    true,
    21,
    NOW()
),

-- Gratuity Templates
(
    'a1b2c3d4-3333-4000-8000-000000000001',
    'Gratuity Provision - Standard',
    'Monthly gratuity provision at 4.81% of basic salary. As per Payment of Gratuity Act, gratuity = (15/26) x Basic x Years, which monthly works out to ~4.81%.',
    'statutory',
    'employer_contribution',
    'GRATUITY',
    'percentage',
    '{"rate": 4.81, "of": "basic", "description": "4.81% of basic salary (monthly provision for gratuity)"}'::jsonb,
    '{}'::jsonb,
    true,
    30,
    NOW()
),

-- HRA Templates
(
    'a1b2c3d4-4444-4000-8000-000000000001',
    'HRA - 50% of Basic (Metro)',
    'HRA calculated as 50% of basic salary. Used for metro cities (Delhi, Mumbai, Chennai, Kolkata).',
    'allowance',
    'earning',
    'HRA',
    'percentage',
    '{"rate": 50, "of": "basic", "description": "50% of basic for metro cities"}'::jsonb,
    '{}'::jsonb,
    true,
    40,
    NOW()
),
(
    'a1b2c3d4-4444-4000-8000-000000000002',
    'HRA - 40% of Basic (Non-Metro)',
    'HRA calculated as 40% of basic salary. Used for non-metro cities.',
    'allowance',
    'earning',
    'HRA',
    'percentage',
    '{"rate": 40, "of": "basic", "description": "40% of basic for non-metro cities"}'::jsonb,
    '{}'::jsonb,
    true,
    41,
    NOW()
),

-- Bonus Templates
(
    'a1b2c3d4-5555-4000-8000-000000000001',
    'Performance Bonus - Percentage',
    'Performance bonus as a percentage of basic salary.',
    'bonus',
    'earning',
    'BONUS',
    'percentage',
    '{"rate": 10, "of": "basic", "description": "10% of basic salary as bonus"}'::jsonb,
    '{}'::jsonb,
    true,
    50,
    NOW()
),
(
    'a1b2c3d4-5555-4000-8000-000000000002',
    'Statutory Bonus - 8.33%',
    'Minimum statutory bonus as per Payment of Bonus Act. 8.33% of (Basic + DA), subject to wage ceiling.',
    'bonus',
    'earning',
    'BONUS',
    'formula',
    '{"expression": "MIN(basic + da, 21000) * 8.33 / 100", "description": "8.33% of (Basic + DA) capped at Rs.21,000"}'::jsonb,
    '{}'::jsonb,
    true,
    51,
    NOW()
),

-- Allowance Templates
(
    'a1b2c3d4-6666-4000-8000-000000000001',
    'Special Allowance - Balance CTC',
    'Special allowance calculated to balance the CTC after other components. Uses formula: CTC - Basic - HRA - PF - Other fixed components.',
    'allowance',
    'earning',
    'SPECIAL_ALLOWANCE',
    'formula',
    '{"expression": "monthly_gross - basic - hra - da - conveyance - medical", "description": "Balance amount after all other components"}'::jsonb,
    '{}'::jsonb,
    true,
    60,
    NOW()
),
(
    'a1b2c3d4-6666-4000-8000-000000000002',
    'Conveyance Allowance - Fixed',
    'Fixed monthly conveyance allowance.',
    'allowance',
    'earning',
    'CONVEYANCE',
    'fixed',
    '{"amount": 1600, "proRata": true, "description": "Fixed Rs.1,600/month, pro-rated for attendance"}'::jsonb,
    '{}'::jsonb,
    true,
    61,
    NOW()
),
(
    'a1b2c3d4-6666-4000-8000-000000000003',
    'Medical Allowance - Fixed',
    'Fixed monthly medical allowance.',
    'allowance',
    'earning',
    'MEDICAL',
    'fixed',
    '{"amount": 1250, "proRata": true, "description": "Fixed Rs.1,250/month, pro-rated for attendance"}'::jsonb,
    '{}'::jsonb,
    true,
    62,
    NOW()
),

-- Professional Tax Templates
(
    'a1b2c3d4-7777-4000-8000-000000000001',
    'PT Karnataka - Standard Slabs',
    'Professional Tax for Karnataka state using standard slabs.',
    'statutory',
    'deduction',
    'PT',
    'slab',
    '{"of": "gross_earnings", "slabs": [{"min": 0, "max": 15000, "value": 0}, {"min": 15001, "max": 999999999, "value": 200}], "description": "Karnataka PT: Rs.200 if gross > Rs.15,000"}'::jsonb,
    '{}'::jsonb,
    true,
    70,
    NOW()
),
(
    'a1b2c3d4-7777-4000-8000-000000000002',
    'PT Maharashtra - Standard Slabs',
    'Professional Tax for Maharashtra state using standard slabs.',
    'statutory',
    'deduction',
    'PT',
    'slab',
    '{"of": "gross_earnings", "slabs": [{"min": 0, "max": 7500, "value": 0}, {"min": 7501, "max": 10000, "value": 175}, {"min": 10001, "max": 999999999, "value": 200}], "description": "Maharashtra PT: Varies by salary slab"}'::jsonb,
    '{}'::jsonb,
    true,
    71,
    NOW()
),

-- LOP Deduction Template
(
    'a1b2c3d4-8888-4000-8000-000000000001',
    'LOP Deduction - Per Day',
    'Loss of Pay deduction calculated per day based on gross salary.',
    'deduction',
    'deduction',
    'LOP_DEDUCTION',
    'formula',
    '{"expression": "(monthly_gross / working_days) * lop_days", "description": "Daily rate x LOP days"}'::jsonb,
    '{}'::jsonb,
    true,
    80,
    NOW()
),

-- Overtime Templates
(
    'a1b2c3d4-9999-4000-8000-000000000001',
    'Overtime - 1.5x Basic',
    'Overtime calculated at 1.5 times the basic hourly rate.',
    'earning',
    'earning',
    'OVERTIME',
    'formula',
    '{"expression": "(basic / (working_days * 8)) * overtime_hours * 1.5", "description": "1.5x hourly rate based on basic"}'::jsonb,
    '{}'::jsonb,
    true,
    90,
    NOW()
),
(
    'a1b2c3d4-9999-4000-8000-000000000002',
    'Overtime - 2x Basic',
    'Overtime calculated at 2 times the basic hourly rate (for holidays/weekends).',
    'earning',
    'earning',
    'OVERTIME',
    'formula',
    '{"expression": "(basic / (working_days * 8)) * overtime_hours * 2", "description": "2x hourly rate for holiday overtime"}'::jsonb,
    '{}'::jsonb,
    true,
    91,
    NOW()
)

ON CONFLICT (id) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    formula_config = EXCLUDED.formula_config;

-- =====================================================
-- Add more formula variables for comprehensive coverage
-- =====================================================
INSERT INTO formula_variables (id, code, display_name, description, data_type, source, source_field, is_system, is_active, created_at, updated_at)
VALUES
    (gen_random_uuid(), 'overtime_hours', 'Overtime Hours', 'Total overtime hours worked', 'decimal', 'payroll', 'overtime_hours', true, true, NOW(), NOW()),
    (gen_random_uuid(), 'arrears', 'Arrears', 'Salary arrears for the month', 'decimal', 'payroll', 'arrears', true, true, NOW(), NOW()),
    (gen_random_uuid(), 'bonus', 'Bonus', 'Monthly bonus amount', 'decimal', 'payroll', 'bonus', true, true, NOW(), NOW()),
    (gen_random_uuid(), 'incentives', 'Incentives', 'Performance incentives', 'decimal', 'payroll', 'incentives', true, true, NOW(), NOW()),
    (gen_random_uuid(), 'reimbursements', 'Reimbursements', 'Total reimbursements for the month', 'decimal', 'payroll', 'reimbursements', true, true, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- =====================================================
-- Note: Company-specific rules should be created via UI
-- The templates above provide a starting point for HR admins
-- =====================================================
