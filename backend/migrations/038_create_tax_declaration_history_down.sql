-- Rollback: Drop tax declaration history table

DROP INDEX IF EXISTS idx_declaration_history_declaration_time;
DROP INDEX IF EXISTS idx_declaration_history_changed_by;
DROP INDEX IF EXISTS idx_declaration_history_changed_at;
DROP INDEX IF EXISTS idx_declaration_history_action;
DROP INDEX IF EXISTS idx_declaration_history_declaration;

DROP TABLE IF EXISTS employee_tax_declaration_history;
