-- Migration: Replace employee_id with party_id in contractor_payments
-- This links contractor payments directly to parties (unified vendor model)

-- Step 1: Add party_id column
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS party_id UUID REFERENCES parties(id);

-- Step 2: Create index for performance
CREATE INDEX IF NOT EXISTS idx_contractor_payments_party_id
ON contractor_payments(party_id) WHERE party_id IS NOT NULL;

-- Step 3: Drop employee_id FK constraint
ALTER TABLE contractor_payments
DROP CONSTRAINT IF EXISTS contractor_payments_employee_id_fkey;

-- Step 4: Make employee_id nullable (will be dropped after code migration)
ALTER TABLE contractor_payments
ALTER COLUMN employee_id DROP NOT NULL;

-- Step 5: Drop the unique constraint that includes employee_id (if exists)
ALTER TABLE contractor_payments
DROP CONSTRAINT IF EXISTS contractor_payments_employee_id_payment_month_payment_year_key;

-- Note: NO unique constraint on (party_id, payment_month, payment_year)
-- A contractor can receive multiple payments in the same month
-- Duplicate prevention is handled via tally_voucher_guid for imports

-- Step 7: Remove employee_id column entirely (fresh DB, no backward compat needed)
ALTER TABLE contractor_payments DROP COLUMN IF EXISTS employee_id;

-- Step 8: Make party_id NOT NULL (required field)
ALTER TABLE contractor_payments
ALTER COLUMN party_id SET NOT NULL;

-- Step 9: Delete contractor records from employees table
-- (contractors are now managed via parties, not employees)
DELETE FROM employees WHERE employment_type = 'contractor';
