-- 007_create_loan_tables_down.sql
-- Rolls back the loan tables created by 007_create_loan_tables.sql

DROP TABLE IF EXISTS loan_transactions CASCADE;
DROP TABLE IF EXISTS loan_emi_schedule CASCADE;
DROP TABLE IF EXISTS loans CASCADE;



