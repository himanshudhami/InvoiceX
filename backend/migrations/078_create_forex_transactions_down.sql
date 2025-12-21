-- Rollback: Drop forex_transactions table

DROP TRIGGER IF EXISTS trigger_update_forex_transactions_updated_at ON forex_transactions;
DROP FUNCTION IF EXISTS update_forex_transactions_updated_at();
DROP TABLE IF EXISTS forex_transactions;
