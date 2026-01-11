-- Migration: Add ledger account links to loans table for journal entry creation
-- This enables automatic journal entry generation when EMI payments are recorded

-- Add columns to link loans to chart of accounts and bank accounts
ALTER TABLE loans
ADD COLUMN IF NOT EXISTS ledger_account_id UUID REFERENCES chart_of_accounts(id),
ADD COLUMN IF NOT EXISTS interest_expense_account_id UUID REFERENCES chart_of_accounts(id),
ADD COLUMN IF NOT EXISTS bank_account_id UUID REFERENCES bank_accounts(id);

-- Add comments for documentation
COMMENT ON COLUMN loans.ledger_account_id IS 'Chart of Account for the loan liability (e.g., Secured Loan - Bank)';
COMMENT ON COLUMN loans.interest_expense_account_id IS 'Chart of Account for interest expense (e.g., Interest on Borrowings)';
COMMENT ON COLUMN loans.bank_account_id IS 'Bank account from which EMI is debited';

-- Create indexes for faster lookups
CREATE INDEX IF NOT EXISTS idx_loans_ledger_account_id ON loans(ledger_account_id);
CREATE INDEX IF NOT EXISTS idx_loans_interest_expense_account_id ON loans(interest_expense_account_id);
CREATE INDEX IF NOT EXISTS idx_loans_bank_account_id ON loans(bank_account_id);

-- Also add journal_entry_id to loan_transactions to track posted entries
ALTER TABLE loan_transactions
ADD COLUMN IF NOT EXISTS journal_entry_id UUID REFERENCES journal_entries(id);

COMMENT ON COLUMN loan_transactions.journal_entry_id IS 'Reference to the auto-posted journal entry for this transaction';

CREATE INDEX IF NOT EXISTS idx_loan_transactions_journal_entry_id ON loan_transactions(journal_entry_id);
