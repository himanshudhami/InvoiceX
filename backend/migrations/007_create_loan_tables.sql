-- 007_create_loan_tables.sql
-- Creates loan management tables for asset financing and loan tracking

-- Loans table
CREATE TABLE IF NOT EXISTS loans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    company_id UUID REFERENCES companies(id) ON DELETE CASCADE,
    loan_name VARCHAR(200) NOT NULL,
    lender_name VARCHAR(200) NOT NULL,
    loan_type VARCHAR(30) NOT NULL DEFAULT 'secured',
    asset_id UUID REFERENCES assets(id) ON DELETE SET NULL,
    principal_amount NUMERIC(14,2) NOT NULL,
    interest_rate NUMERIC(5,2) NOT NULL,
    loan_start_date DATE NOT NULL,
    loan_end_date DATE,
    tenure_months INTEGER NOT NULL,
    emi_amount NUMERIC(14,2) NOT NULL,
    outstanding_principal NUMERIC(14,2) NOT NULL,
    interest_type VARCHAR(20) NOT NULL DEFAULT 'fixed',
    compounding_frequency VARCHAR(20) NOT NULL DEFAULT 'monthly',
    status VARCHAR(30) NOT NULL DEFAULT 'active',
    loan_account_number VARCHAR(100),
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT loans_type_check CHECK (loan_type IN ('secured','unsecured','asset_financing')),
    CONSTRAINT loans_interest_type_check CHECK (interest_type IN ('fixed','floating','reducing')),
    CONSTRAINT loans_compounding_frequency_check CHECK (compounding_frequency IN ('monthly','quarterly','annually')),
    CONSTRAINT loans_status_check CHECK (status IN ('active','closed','foreclosed','defaulted')),
    CONSTRAINT loans_principal_amount_check CHECK (principal_amount > 0),
    CONSTRAINT loans_interest_rate_check CHECK (interest_rate >= 0 AND interest_rate <= 100),
    CONSTRAINT loans_tenure_months_check CHECK (tenure_months > 0 AND tenure_months <= 360)
);

CREATE INDEX IF NOT EXISTS idx_loans_company_id ON loans(company_id);
CREATE INDEX IF NOT EXISTS idx_loans_asset_id ON loans(asset_id);
CREATE INDEX IF NOT EXISTS idx_loans_status ON loans(status);
CREATE INDEX IF NOT EXISTS idx_loans_company_status ON loans(company_id, status);

-- Loan EMI Schedule table
CREATE TABLE IF NOT EXISTS loan_emi_schedule (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id UUID REFERENCES loans(id) ON DELETE CASCADE,
    emi_number INTEGER NOT NULL,
    due_date DATE NOT NULL,
    principal_amount NUMERIC(14,2) NOT NULL,
    interest_amount NUMERIC(14,2) NOT NULL,
    total_emi NUMERIC(14,2) NOT NULL,
    outstanding_principal_after NUMERIC(14,2) NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'pending',
    paid_date DATE,
    payment_voucher_id UUID,
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT loan_emi_schedule_status_check CHECK (status IN ('pending','paid','overdue','skipped')),
    CONSTRAINT loan_emi_schedule_emi_number_check CHECK (emi_number > 0),
    CONSTRAINT loan_emi_schedule_unique_loan_emi UNIQUE (loan_id, emi_number)
);

CREATE INDEX IF NOT EXISTS idx_loan_emi_schedule_loan_id ON loan_emi_schedule(loan_id);
CREATE INDEX IF NOT EXISTS idx_loan_emi_schedule_due_date ON loan_emi_schedule(due_date);
CREATE INDEX IF NOT EXISTS idx_loan_emi_schedule_status ON loan_emi_schedule(status);
CREATE INDEX IF NOT EXISTS idx_loan_emi_schedule_loan_status ON loan_emi_schedule(loan_id, status);

-- Loan Transactions table
CREATE TABLE IF NOT EXISTS loan_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    loan_id UUID REFERENCES loans(id) ON DELETE CASCADE,
    transaction_type VARCHAR(30) NOT NULL,
    transaction_date DATE NOT NULL,
    amount NUMERIC(14,2) NOT NULL,
    principal_amount NUMERIC(14,2) DEFAULT 0,
    interest_amount NUMERIC(14,2) DEFAULT 0,
    description TEXT,
    payment_method VARCHAR(30),
    bank_account_id UUID,
    voucher_reference VARCHAR(200),
    notes TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT loan_transactions_type_check CHECK (transaction_type IN ('disbursement','emi_payment','prepayment','foreclosure','interest_accrual','interest_capitalization')),
    CONSTRAINT loan_transactions_payment_method_check CHECK (payment_method IN ('bank_transfer','cheque','cash','online','other') OR payment_method IS NULL),
    CONSTRAINT loan_transactions_amount_check CHECK (amount >= 0)
);

CREATE INDEX IF NOT EXISTS idx_loan_transactions_loan_id ON loan_transactions(loan_id);
CREATE INDEX IF NOT EXISTS idx_loan_transactions_type ON loan_transactions(transaction_type);
CREATE INDEX IF NOT EXISTS idx_loan_transactions_date ON loan_transactions(transaction_date);
CREATE INDEX IF NOT EXISTS idx_loan_transactions_loan_type ON loan_transactions(loan_id, transaction_type);





