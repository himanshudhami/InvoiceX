-- ============================================================================
-- Migration 106: Add Journal Entry Linkage to Contractor Payments
-- Description: Track accrual and disbursement journal entries for contractor payments
-- Author: System
-- Date: 2024-12
-- ============================================================================

-- -----------------------------------------------------------------------------
-- 1. Add journal entry linkage columns to contractor_payments
-- -----------------------------------------------------------------------------

-- Accrual journal entry (created on approval)
-- Records expense recognition: Dr. Professional Fees, Cr. TDS Payable + Contractor Payable
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS accrual_journal_entry_id UUID REFERENCES journal_entries(id);

COMMENT ON COLUMN contractor_payments.accrual_journal_entry_id IS
    'Journal entry created on approval - expense recognition (Dr. Professional Fees, Cr. TDS Payable + Contractor Payable)';

-- Disbursement journal entry (created on payment)
-- Records liability settlement: Dr. Contractor Payable, Cr. Bank
ALTER TABLE contractor_payments
ADD COLUMN IF NOT EXISTS disbursement_journal_entry_id UUID REFERENCES journal_entries(id);

COMMENT ON COLUMN contractor_payments.disbursement_journal_entry_id IS
    'Journal entry created on payment - liability settlement (Dr. Contractor Payable, Cr. Bank)';

-- -----------------------------------------------------------------------------
-- 2. Create indexes for efficient lookups
-- -----------------------------------------------------------------------------

CREATE INDEX IF NOT EXISTS idx_contractor_payments_accrual_journal
ON contractor_payments(accrual_journal_entry_id)
WHERE accrual_journal_entry_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_contractor_payments_disbursement_journal
ON contractor_payments(disbursement_journal_entry_id)
WHERE disbursement_journal_entry_id IS NOT NULL;

-- -----------------------------------------------------------------------------
-- 3. Add TDS Payable - 194C account if not exists (for contractor TDS at 1-2%)
-- This is separate from 194J (professional fees at 10%)
-- -----------------------------------------------------------------------------

-- Check if we need to add TDS Payable - 194C (contractors) account
-- 2213 = TDS Payable - Professional (already exists for 194J)
-- We'll add 2214 = TDS Payable - Contractor (for 194C)

INSERT INTO chart_of_accounts (
    company_id, account_code, account_name, account_type, account_subtype,
    depth_level, normal_balance, is_active, description, created_at
)
SELECT
    c.id,
    '2214',
    'TDS Payable - Contractor',
    'liability',
    'current_liability',
    2,
    'credit',
    true,
    'TDS payable under Section 194C for contractor payments (1-2%)',
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '2214'
);

-- Add Contractor Payable account (subaccount of Trade Payables 2100)
INSERT INTO chart_of_accounts (
    company_id, account_code, account_name, account_type, account_subtype,
    depth_level, normal_balance, is_active, description, created_at
)
SELECT
    c.id,
    '2101',
    'Contractor Payable',
    'liability',
    'current_liability',
    2,
    'credit',
    true,
    'Amount payable to contractors for professional/technical services',
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '2101'
);

-- Add Input GST account if not exists (for claiming ITC on contractor invoices)
INSERT INTO chart_of_accounts (
    company_id, account_code, account_name, account_type, account_subtype,
    depth_level, normal_balance, is_active, description, created_at
)
SELECT
    c.id,
    '1210',
    'Input GST Receivable',
    'asset',
    'current_asset',
    2,
    'debit',
    true,
    'Input GST credit on purchases and expenses',
    NOW()
FROM companies c
WHERE NOT EXISTS (
    SELECT 1 FROM chart_of_accounts coa
    WHERE coa.company_id = c.id AND coa.account_code = '1210'
);
