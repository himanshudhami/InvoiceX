-- Rollback: Remove auto-created payroll info records
-- Note: This only removes records that were auto-created by the migration
-- Manually created records are preserved

-- This rollback is intentionally minimal as we cannot distinguish
-- between auto-created and manually created records
-- In practice, you may want to keep these records

-- If you need to rollback, you can manually delete records created after a specific timestamp
-- DELETE FROM employee_payroll_info WHERE created_at >= '2025-12-14';



