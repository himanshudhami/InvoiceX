-- Rollback: Revert PT Karnataka rule to old slab-based version

UPDATE calculation_rules
SET
    rule_type = 'slab',
    formula_config = '{
        "slabs": [
            {"min": 0, "max": 24999, "value": 0},
            {"min": 25000, "max": 999999999, "value": 200}
        ],
        "baseField": "monthly_gross"
    }'::jsonb,
    name = 'PT Karnataka - Standard Slabs',
    description = NULL,
    updated_at = NOW()
WHERE id = 'b1546b8a-52cb-4d1c-95d9-e6828f694b69'
  AND company_id = '43e030dc-d522-49e0-819a-31744383b2e2';

-- Remove formula variables
DELETE FROM formula_variables WHERE code IN ('payroll_month', 'payroll_year');

-- Revert template
UPDATE calculation_rule_templates
SET
    formula_config = '{"of": "gross_earnings", "slabs": [{"min": 0, "max": 15000, "value": 0}, {"min": 15001, "max": 999999999, "value": 200}], "description": "Karnataka PT: Rs.200 if gross > Rs.15,000"}'::jsonb,
    name = 'PT Karnataka - Standard Slabs',
    description = 'Professional Tax for Karnataka state using standard slabs.'
WHERE id = 'a1b2c3d4-7777-4000-8000-000000000001';
