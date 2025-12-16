-- Rollback: Remove ESI eligibility period tracking

DROP INDEX IF EXISTS idx_esi_eligibility_active;
DROP INDEX IF EXISTS idx_esi_eligibility_period;
DROP INDEX IF EXISTS idx_esi_eligibility_company;
DROP INDEX IF EXISTS idx_esi_eligibility_employee;

DROP TABLE IF EXISTS esi_eligibility_periods;
