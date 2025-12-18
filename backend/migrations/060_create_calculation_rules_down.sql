-- Rollback: Drop calculation rules engine tables
-- Must drop in reverse order due to foreign key constraints

-- Drop indexes first
DROP INDEX IF EXISTS idx_rule_conditions_rule;
DROP INDEX IF EXISTS idx_calculation_rules_active;
DROP INDEX IF EXISTS idx_calculation_rules_component;
DROP INDEX IF EXISTS idx_calculation_rules_company;

-- Drop tables in order of dependencies
DROP TABLE IF EXISTS calculation_rule_conditions;
DROP TABLE IF EXISTS calculation_rule_templates;
DROP TABLE IF EXISTS calculation_rules;
DROP TABLE IF EXISTS formula_variables;
