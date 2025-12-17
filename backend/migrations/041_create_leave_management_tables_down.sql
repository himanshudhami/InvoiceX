-- Rollback Migration: 041_create_leave_management_tables
-- Description: Drops leave management tables

-- Drop tables in reverse order of dependencies
DROP TABLE IF EXISTS leave_applications CASCADE;
DROP TABLE IF EXISTS employee_leave_balances CASCADE;
DROP TABLE IF EXISTS holidays CASCADE;
DROP TABLE IF EXISTS leave_types CASCADE;
