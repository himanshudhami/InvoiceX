// Loan management types

export interface Loan {
  id: string;
  companyId: string;
  loanName: string;
  lenderName: string;
  loanType: 'secured' | 'unsecured' | 'asset_financing';
  assetId?: string;
  principalAmount: number;
  interestRate: number;
  loanStartDate: string;
  loanEndDate?: string;
  tenureMonths: number;
  emiAmount: number;
  outstandingPrincipal: number;
  interestType: 'fixed' | 'floating' | 'reducing';
  compoundingFrequency: 'monthly' | 'quarterly' | 'annually';
  status: 'active' | 'closed' | 'foreclosed' | 'defaulted';
  loanAccountNumber?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
  // Ledger account links for journal entry creation
  ledgerAccountId?: string;
  interestExpenseAccountId?: string;
  bankAccountId?: string;
}

export interface LoanEmiSchedule {
  id: string;
  loanId: string;
  emiNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  totalEmi: number;
  outstandingPrincipalAfter: number;
  status: 'pending' | 'paid' | 'overdue' | 'skipped';
  paidDate?: string;
  paymentVoucherId?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface LoanTransaction {
  id: string;
  loanId: string;
  transactionType: 'disbursement' | 'emi_payment' | 'prepayment' | 'foreclosure' | 'interest_accrual' | 'interest_capitalization';
  transactionDate: string;
  amount: number;
  principalAmount: number;
  interestAmount: number;
  description?: string;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateLoanDto {
  companyId: string;
  loanName: string;
  lenderName: string;
  loanType?: 'secured' | 'unsecured' | 'asset_financing';
  assetId?: string;
  principalAmount: number;
  interestRate: number;
  loanStartDate: string;
  loanEndDate?: string;
  tenureMonths: number;
  interestType?: 'fixed' | 'floating' | 'reducing';
  compoundingFrequency?: 'monthly' | 'quarterly' | 'annually';
  loanAccountNumber?: string;
  notes?: string;
  // Ledger account links for journal entry creation
  ledgerAccountId?: string;
  interestExpenseAccountId?: string;
  bankAccountId?: string;
}

export interface UpdateLoanDto extends Partial<CreateLoanDto> {
  emiAmount?: number;
  status?: 'active' | 'closed' | 'foreclosed' | 'defaulted';
}

export interface LoanScheduleDto {
  loanId: string;
  loanName: string;
  principalAmount: number;
  interestRate: number;
  tenureMonths: number;
  emiAmount: number;
  scheduleItems: LoanEmiScheduleItemDto[];
}

export interface LoanEmiScheduleItemDto {
  id: string;
  emiNumber: number;
  dueDate: string;
  principalAmount: number;
  interestAmount: number;
  totalEmi: number;
  outstandingPrincipalAfter: number;
  status: string;
  paidDate?: string;
}

export interface CreateEmiPaymentDto {
  paymentDate: string;
  amount: number;
  principalAmount: number;
  interestAmount: number;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  emiNumber?: number;
}

export interface PrepaymentDto {
  prepaymentDate: string;
  amount: number;
  paymentMethod?: 'bank_transfer' | 'cheque' | 'cash' | 'online' | 'other';
  bankAccountId?: string;
  voucherReference?: string;
  notes?: string;
  reduceEmi?: boolean;
}
