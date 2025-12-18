-- Update PT Karnataka rule for Xcdify with 2024 slabs and February adjustment
-- New slabs effective 2024:
--   - Gross ≤ Rs.24,999: Rs.0 (exempt)
--   - Gross ≥ Rs.25,000: Rs.200/month (Apr-Jan), Rs.300 in February
--   - Annual cap: Rs.2,500 (11 × 200 + 300)

UPDATE calculation_rules
SET
    rule_type = 'formula',
    formula_config = '{
        "expression": "IF(gross_earnings < 25000, 0, IF(payroll_month = 2, 300, 200))",
        "description": "Karnataka PT 2024: Rs.0 if gross < 25,000; Rs.200/month (Apr-Jan); Rs.300 in February to cap annual PT at Rs.2,500"
    }'::jsonb,
    name = 'PT Karnataka - 2024 Slabs with Feb Adjustment',
    description = 'Professional Tax for Karnataka as per 2024 rules. If monthly gross ≤ Rs.24,999: exempt. If gross ≥ Rs.25,000: Rs.200/month (11 months) + Rs.300 in February = Rs.2,500 annual cap.',
    updated_at = NOW()
WHERE id = 'b1546b8a-52cb-4d1c-95d9-e6828f694b69'
  AND company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- Also add payroll_month and payroll_year to formula_variables for documentation
INSERT INTO formula_variables (id, code, display_name, description, data_type, source, source_field, is_system, is_active, created_at, updated_at)
VALUES
    (gen_random_uuid(), 'payroll_month', 'Payroll Month', 'The month of payroll (1-12). Useful for month-specific calculations like PT February adjustment.', 'integer', 'payroll', 'payroll_month', true, true, NOW(), NOW()),
    (gen_random_uuid(), 'payroll_year', 'Payroll Year', 'The year of payroll (e.g., 2024). Useful for year-specific calculations.', 'integer', 'payroll', 'payroll_year', true, true, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- Update the PT Karnataka template as well for future use
UPDATE calculation_rule_templates
SET
    formula_config = '{
        "expression": "IF(gross_earnings < 25000, 0, IF(payroll_month = 2, 300, 200))",
        "description": "Karnataka PT 2024: Rs.0 if gross < 25,000; Rs.200/month; Rs.300 in Feb"
    }'::jsonb,
    name = 'PT Karnataka - 2024 Slabs',
    description = 'Professional Tax for Karnataka (2024). Gross ≤ Rs.24,999: exempt. Gross ≥ Rs.25,000: Rs.200/month + Rs.300 in February = Rs.2,500 annual cap.'
WHERE id = 'a1b2c3d4-7777-4000-8000-000000000001';
