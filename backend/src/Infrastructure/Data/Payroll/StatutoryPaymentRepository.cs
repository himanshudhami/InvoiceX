using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Payroll
{
    /// <summary>
    /// Repository implementation for statutory payments (TDS/PF/ESI/PT challans)
    /// </summary>
    public class StatutoryPaymentRepository : IStatutoryPaymentRepository
    {
        private readonly string _connectionString;

        public StatutoryPaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<StatutoryPayment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StatutoryPayment>(
                @"SELECT id, company_id, payment_type, reference_number,
                         financial_year, period_month, period_year, quarter,
                         principal_amount, interest_amount, penalty_amount, late_fee, total_amount,
                         payment_date, payment_mode, bank_name, bank_account_id, bank_reference,
                         bsr_code, receipt_number, trrn, challan_number,
                         status, due_date, journal_entry_id,
                         created_at, updated_at, created_by, paid_by, paid_at,
                         verified_by, verified_at, filed_by, filed_at, notes
                  FROM statutory_payments
                  WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  ORDER BY due_date DESC",
                new { companyId });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetPendingAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND status = 'pending'
                  ORDER BY due_date",
                new { companyId });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetOverdueAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND status = 'pending'
                  AND due_date < CURRENT_DATE
                  ORDER BY due_date",
                new { companyId });
        }

        public async Task<StatutoryPayment?> GetByPeriodAsync(
            Guid companyId,
            string paymentType,
            string financialYear,
            int periodMonth)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND payment_type = @paymentType
                  AND financial_year = @financialYear
                  AND period_month = @periodMonth
                  AND status != 'cancelled'",
                new { companyId, paymentType, financialYear, periodMonth });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetByFinancialYearAsync(
            Guid companyId,
            string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND financial_year = @financialYear
                  ORDER BY period_month, payment_type",
                new { companyId, financialYear });
        }

        public async Task<StatutoryPayment> AddAsync(StatutoryPayment payment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            payment.Id = Guid.NewGuid();
            payment.CreatedAt = DateTime.UtcNow;
            payment.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO statutory_payments (
                    id, company_id, payment_type, reference_number,
                    financial_year, period_month, period_year, quarter,
                    principal_amount, interest_amount, penalty_amount, late_fee, total_amount,
                    payment_date, payment_mode, bank_name, bank_account_id, bank_reference,
                    bsr_code, receipt_number, trrn, challan_number,
                    status, due_date, journal_entry_id,
                    created_at, updated_at, created_by, notes
                ) VALUES (
                    @Id, @CompanyId, @PaymentType, @ReferenceNumber,
                    @FinancialYear, @PeriodMonth, @PeriodYear, @Quarter,
                    @PrincipalAmount, @InterestAmount, @PenaltyAmount, @LateFee, @TotalAmount,
                    @PaymentDate, @PaymentMode, @BankName, @BankAccountId, @BankReference,
                    @BsrCode, @ReceiptNumber, @Trrn, @ChallanNumber,
                    @Status, @DueDate, @JournalEntryId,
                    @CreatedAt, @UpdatedAt, @CreatedBy, @Notes
                )",
                payment);

            return payment;
        }

        public async Task UpdateAsync(StatutoryPayment payment)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            payment.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"UPDATE statutory_payments SET
                    reference_number = @ReferenceNumber,
                    principal_amount = @PrincipalAmount,
                    interest_amount = @InterestAmount,
                    penalty_amount = @PenaltyAmount,
                    late_fee = @LateFee,
                    total_amount = @TotalAmount,
                    payment_date = @PaymentDate,
                    payment_mode = @PaymentMode,
                    bank_name = @BankName,
                    bank_account_id = @BankAccountId,
                    bank_reference = @BankReference,
                    bsr_code = @BsrCode,
                    receipt_number = @ReceiptNumber,
                    trrn = @Trrn,
                    challan_number = @ChallanNumber,
                    status = @Status,
                    journal_entry_id = @JournalEntryId,
                    updated_at = @UpdatedAt,
                    paid_by = @PaidBy,
                    paid_at = @PaidAt,
                    verified_by = @VerifiedBy,
                    verified_at = @VerifiedAt,
                    filed_by = @FiledBy,
                    filed_at = @FiledAt,
                    notes = @Notes
                  WHERE id = @Id",
                payment);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE statutory_payments
                  SET status = 'cancelled', updated_at = @updatedAt
                  WHERE id = @id",
                new { id, updatedAt = DateTime.UtcNow });
        }

        public async Task<IEnumerable<PendingStatutoryPaymentView>> GetPendingPaymentsViewAsync(
            Guid companyId)
        {
            return await GetPendingPaymentsViewAsync(companyId, null);
        }

        public async Task<IEnumerable<PendingStatutoryPaymentView>> GetPendingPaymentsViewAsync(
            Guid companyId,
            string? statusFilter)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"SELECT
                            company_id,
                            financial_year,
                            period_month,
                            period_year,
                            payment_type,
                            payment_type_name,
                            payment_category,
                            amount_due,
                            amount_paid,
                            balance_due,
                            due_date,
                            payment_status,
                            days_overdue,
                            statutory_payment_id,
                            reference_number,
                            payment_date,
                            challan_status
                        FROM v_pending_statutory_payments
                        WHERE company_id = @companyId";

            if (!string.IsNullOrEmpty(statusFilter))
            {
                sql += " AND payment_status = @statusFilter";
            }

            sql += " ORDER BY due_date, payment_type";

            return await connection.QueryAsync<PendingStatutoryPaymentView>(
                sql,
                new { companyId, statusFilter });
        }

        public async Task<IEnumerable<StatutoryPaymentType>> GetPaymentTypesAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPaymentType>(
                @"SELECT code, name, category, due_day, grace_period_days,
                         penalty_type, penalty_rate, filing_form,
                         payment_frequency, payable_account_code, remarks
                  FROM statutory_payment_types
                  ORDER BY category, code");
        }

        public async Task<StatutoryPaymentType?> GetPaymentTypeAsync(string code)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StatutoryPaymentType>(
                @"SELECT * FROM statutory_payment_types WHERE code = @code",
                new { code });
        }

        public async Task AddAllocationAsync(StatutoryPaymentAllocation allocation)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            allocation.Id = Guid.NewGuid();
            allocation.CreatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO statutory_payment_allocations (
                    id, statutory_payment_id, payroll_run_id,
                    payroll_transaction_id, contractor_payment_id,
                    amount_allocated, allocation_type, created_at
                ) VALUES (
                    @Id, @StatutoryPaymentId, @PayrollRunId,
                    @PayrollTransactionId, @ContractorPaymentId,
                    @AmountAllocated, @AllocationType, @CreatedAt
                )",
                allocation);
        }

        public async Task<IEnumerable<StatutoryPaymentAllocation>> GetAllocationsAsync(
            Guid statutoryPaymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPaymentAllocation>(
                @"SELECT * FROM statutory_payment_allocations
                  WHERE statutory_payment_id = @statutoryPaymentId",
                new { statutoryPaymentId });
        }

        public async Task<StatutoryPayment?> GetByPeriodAndTypeAsync(
            Guid companyId,
            int periodMonth,
            int periodYear,
            string paymentType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND period_month = @periodMonth
                  AND period_year = @periodYear
                  AND payment_type = @paymentType
                  AND status != 'cancelled'",
                new { companyId, periodMonth, periodYear, paymentType });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetPendingByCompanyAsync(
            Guid companyId,
            string paymentType,
            string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM statutory_payments
                        WHERE company_id = @companyId
                        AND payment_type = @paymentType
                        AND status = 'pending'";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND financial_year = @financialYear";
            }

            sql += " ORDER BY due_date";

            return await connection.QueryAsync<StatutoryPayment>(
                sql,
                new { companyId, paymentType, financialYear });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetPaidByCompanyAsync(
            Guid companyId,
            string paymentType,
            string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND payment_type = @paymentType
                  AND financial_year = @financialYear
                  AND status IN ('paid', 'verified', 'filed')
                  ORDER BY period_month",
                new { companyId, paymentType, financialYear });
        }

        public async Task<IEnumerable<StatutoryPayment>> GetByCompanyAndFyAsync(
            Guid companyId,
            string paymentType,
            string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<StatutoryPayment>(
                @"SELECT * FROM statutory_payments
                  WHERE company_id = @companyId
                  AND payment_type = @paymentType
                  AND financial_year = @financialYear
                  AND status != 'cancelled'
                  ORDER BY period_month",
                new { companyId, paymentType, financialYear });
        }
    }
}
