-- Rollback: Drop TDS Receivable table

DROP INDEX IF EXISTS idx_tds_recv_company_fy;
DROP INDEX IF EXISTS idx_tds_recv_customer;
DROP INDEX IF EXISTS idx_tds_recv_status;
DROP INDEX IF EXISTS idx_tds_recv_quarter;
DROP INDEX IF EXISTS idx_tds_recv_payment;
DROP INDEX IF EXISTS idx_tds_recv_section;
DROP INDEX IF EXISTS idx_tds_recv_matched;
DROP INDEX IF EXISTS idx_tds_recv_unmatched;

DROP TABLE IF EXISTS tds_receivable;

DO $$
BEGIN
    RAISE NOTICE 'Dropped tds_receivable table';
END $$;
