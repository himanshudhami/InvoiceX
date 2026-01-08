-- Fix Tally import issues
-- Run this on InvoiceApp2 database

BEGIN;

-- Fix 1: Set all Tally-imported vendors to active
UPDATE vendors
SET is_active = true
WHERE tally_ledger_guid IS NOT NULL
AND (is_active IS NULL OR is_active = false);

-- Fix 2: Set all Tally-imported customers to active
UPDATE customers
SET is_active = true
WHERE tally_ledger_guid IS NOT NULL
AND (is_active IS NULL OR is_active = false);

-- Fix 3: Set all manually created vendors (no tally guid) to active too
UPDATE vendors
SET is_active = true
WHERE is_active IS NULL OR is_active = false;

COMMIT;

-- ============================================
-- DIAGNOSTICS
-- ============================================

-- 1. Vendor counts
SELECT 'Total Vendors' as metric, COUNT(*) as count FROM vendors;
SELECT 'Active Vendors' as metric, COUNT(*) as count FROM vendors WHERE is_active = true;
SELECT 'Tally Vendors' as metric, COUNT(*) as count FROM vendors WHERE tally_ledger_guid IS NOT NULL;

-- 2. Check for payments pointing to UNKNOWN vendor
SELECT
    'Payments to UNKNOWN vendor' as issue,
    COUNT(*) as count
FROM vendor_payments vp
JOIN vendors v ON vp.vendor_id = v.id
WHERE v.name = 'UNKNOWN-TALLY-VENDOR';

-- 3. Trial balance check
SELECT
    'Trial Balance' as check_type,
    ROUND(SUM(debit_amount)::numeric, 2) as total_debit,
    ROUND(SUM(credit_amount)::numeric, 2) as total_credit,
    ROUND((SUM(debit_amount) - SUM(credit_amount))::numeric, 2) as difference
FROM journal_entry_lines jel
JOIN journal_entries je ON jel.journal_entry_id = je.id
WHERE je.tally_voucher_guid IS NOT NULL;

-- 4. Migration log summary
SELECT
    record_type,
    status,
    COUNT(*) as count
FROM tally_migration_logs
GROUP BY record_type, status
ORDER BY record_type, status;

-- 5. Payments that might have wrong vendor
SELECT
    'Sample payments with issues' as info,
    vp.id,
    vp.amount,
    vp.tally_voucher_number,
    v.name as vendor_name
FROM vendor_payments vp
LEFT JOIN vendors v ON vp.vendor_id = v.id
WHERE vp.tally_voucher_guid IS NOT NULL
ORDER BY vp.payment_date DESC
LIMIT 10;
