-- 057_create_approval_workflow_tables_down.sql
-- Rollback: Remove approval workflow tables

-- Drop triggers first
DROP TRIGGER IF EXISTS trg_approval_templates_updated ON approval_workflow_templates;
DROP TRIGGER IF EXISTS trg_workflow_steps_updated ON approval_workflow_steps;
DROP TRIGGER IF EXISTS trg_approval_requests_updated ON approval_requests;

-- Drop function
DROP FUNCTION IF EXISTS update_approval_updated_at();

-- Drop indexes
DROP INDEX IF EXISTS idx_approval_templates_default;
DROP INDEX IF EXISTS idx_approval_templates_company;
DROP INDEX IF EXISTS idx_approval_templates_activity;
DROP INDEX IF EXISTS idx_workflow_steps_template;
DROP INDEX IF EXISTS idx_approval_requests_activity;
DROP INDEX IF EXISTS idx_approval_requests_status;
DROP INDEX IF EXISTS idx_approval_requests_requestor;
DROP INDEX IF EXISTS idx_approval_requests_company;
DROP INDEX IF EXISTS idx_approval_request_steps_request;
DROP INDEX IF EXISTS idx_approval_request_steps_pending;
DROP INDEX IF EXISTS idx_approval_request_steps_assigned;

-- Drop tables in reverse dependency order
DROP TABLE IF EXISTS approval_request_steps;
DROP TABLE IF EXISTS approval_requests;
DROP TABLE IF EXISTS approval_workflow_steps;
DROP TABLE IF EXISTS approval_workflow_templates;
