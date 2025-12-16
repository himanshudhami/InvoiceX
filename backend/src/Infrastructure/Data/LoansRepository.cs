using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data;

public class LoansRepository : ILoansRepository
{
    private readonly string _connectionString;

    public LoansRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<Loan?> GetByIdAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Loan>("SELECT * FROM loans WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<Loan>> GetAllAsync(Guid? companyId = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (companyId.HasValue)
        {
            return await connection.QueryAsync<Loan>("SELECT * FROM loans WHERE company_id=@companyId ORDER BY created_at DESC", new { companyId = companyId.Value });
        }
        return await connection.QueryAsync<Loan>("SELECT * FROM loans ORDER BY created_at DESC");
    }

    public async Task<(IEnumerable<Loan> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        Dictionary<string, object>? filters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var allowedColumns = new[] {
            "id", "company_id", "loan_name", "lender_name", "loan_type", "asset_id",
            "principal_amount", "interest_rate", "loan_start_date", "loan_end_date",
            "tenure_months", "emi_amount", "outstanding_principal", "interest_type",
            "compounding_frequency", "status", "loan_account_number", "created_at", "updated_at"
        };

        var builder = SqlQueryBuilder
            .From("loans", allowedColumns)
            .SearchAcross(new[] { "loan_name", "lender_name", "loan_account_number" }, searchTerm)
            .ApplyFilters(filters)
            .Paginate(pageNumber, pageSize);

        var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
        var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
        builder.OrderBy(orderBy, sortDescending);

        var (dataSql, parameters) = builder.BuildSelect();
        var (countSql, _) = builder.BuildCount();

        using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
        var items = await multi.ReadAsync<Loan>();
        var total = await multi.ReadSingleAsync<int>();
        return (items, total);
    }

    public async Task<Loan> AddAsync(Loan entity)
    {
        const string sql = @"INSERT INTO loans
        (company_id, loan_name, lender_name, loan_type, asset_id, principal_amount, interest_rate, loan_start_date, loan_end_date, tenure_months, emi_amount, outstanding_principal, interest_type, compounding_frequency, status, loan_account_number, notes, created_at, updated_at)
        VALUES (@CompanyId, @LoanName, @LenderName, @LoanType, @AssetId, @PrincipalAmount, @InterestRate, @LoanStartDate, @LoanEndDate, @TenureMonths, @EmiAmount, @OutstandingPrincipal, @InterestType, @CompoundingFrequency, @Status, @LoanAccountNumber, @Notes, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<Loan>(sql, entity);
    }

    public async Task UpdateAsync(Loan entity)
    {
        const string sql = @"UPDATE loans SET
            loan_name=@LoanName,
            lender_name=@LenderName,
            loan_type=@LoanType,
            asset_id=@AssetId,
            principal_amount=@PrincipalAmount,
            interest_rate=@InterestRate,
            loan_start_date=@LoanStartDate,
            loan_end_date=@LoanEndDate,
            tenure_months=@TenureMonths,
            emi_amount=@EmiAmount,
            outstanding_principal=@OutstandingPrincipal,
            interest_type=@InterestType,
            compounding_frequency=@CompoundingFrequency,
            status=@Status,
            loan_account_number=@LoanAccountNumber,
            notes=@Notes,
            updated_at=NOW()
        WHERE id=@Id;";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM loans WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<LoanEmiSchedule>> GetEmiScheduleAsync(Guid loanId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<LoanEmiSchedule>(
            "SELECT * FROM loan_emi_schedule WHERE loan_id=@loanId ORDER BY emi_number ASC",
            new { loanId });
    }

    public async Task<LoanEmiSchedule?> GetEmiScheduleItemAsync(Guid loanId, int emiNumber)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<LoanEmiSchedule>(
            "SELECT * FROM loan_emi_schedule WHERE loan_id=@loanId AND emi_number=@emiNumber",
            new { loanId, emiNumber });
    }

    public async Task<LoanEmiSchedule> AddEmiScheduleItemAsync(LoanEmiSchedule item)
    {
        const string sql = @"INSERT INTO loan_emi_schedule
        (loan_id, emi_number, due_date, principal_amount, interest_amount, total_emi, outstanding_principal_after, status, paid_date, payment_voucher_id, notes, created_at, updated_at)
        VALUES (@LoanId, @EmiNumber, @DueDate, @PrincipalAmount, @InterestAmount, @TotalEmi, @OutstandingPrincipalAfter, @Status, @PaidDate, @PaymentVoucherId, @Notes, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<LoanEmiSchedule>(sql, item);
    }

    public async Task UpdateEmiScheduleItemAsync(LoanEmiSchedule item)
    {
        const string sql = @"UPDATE loan_emi_schedule SET
            due_date=@DueDate,
            principal_amount=@PrincipalAmount,
            interest_amount=@InterestAmount,
            total_emi=@TotalEmi,
            outstanding_principal_after=@OutstandingPrincipalAfter,
            status=@Status,
            paid_date=@PaidDate,
            payment_voucher_id=@PaymentVoucherId,
            notes=@Notes,
            updated_at=NOW()
        WHERE id=@Id;";
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, item);
    }

    public async Task<IEnumerable<LoanEmiSchedule>> GetPendingEmisAsync(Guid loanId, DateTime? upToDate = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        if (upToDate.HasValue)
        {
            return await connection.QueryAsync<LoanEmiSchedule>(
                "SELECT * FROM loan_emi_schedule WHERE loan_id=@loanId AND status='pending' AND due_date <= @upToDate ORDER BY emi_number ASC",
                new { loanId, upToDate });
        }
        return await connection.QueryAsync<LoanEmiSchedule>(
            "SELECT * FROM loan_emi_schedule WHERE loan_id=@loanId AND status='pending' ORDER BY emi_number ASC",
            new { loanId });
    }

    public async Task<IEnumerable<LoanTransaction>> GetTransactionsAsync(Guid loanId)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<LoanTransaction>(
            "SELECT * FROM loan_transactions WHERE loan_id=@loanId ORDER BY transaction_date DESC, created_at DESC",
            new { loanId });
    }

    public async Task<LoanTransaction> AddTransactionAsync(LoanTransaction transaction)
    {
        const string sql = @"INSERT INTO loan_transactions
        (loan_id, transaction_type, transaction_date, amount, principal_amount, interest_amount, description, payment_method, bank_account_id, voucher_reference, notes, created_at, updated_at)
        VALUES (@LoanId, @TransactionType, @TransactionDate, @Amount, @PrincipalAmount, @InterestAmount, @Description, @PaymentMethod, @BankAccountId, @VoucherReference, @Notes, NOW(), NOW())
        RETURNING *;";
        using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleAsync<LoanTransaction>(sql, transaction);
    }

    public async Task<IEnumerable<LoanTransaction>> GetInterestPaymentsAsync(Guid? loanId, DateTime? fromDate, DateTime? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = "SELECT * FROM loan_transactions WHERE transaction_type IN ('emi_payment', 'interest_accrual') AND interest_amount > 0";
        var parameters = new DynamicParameters();

        if (loanId.HasValue)
        {
            sql += " AND loan_id=@loanId";
            parameters.Add("loanId", loanId.Value);
        }

        if (fromDate.HasValue)
        {
            sql += " AND transaction_date >= @fromDate";
            parameters.Add("fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND transaction_date <= @toDate";
            parameters.Add("toDate", toDate.Value);
        }

        sql += " ORDER BY transaction_date ASC";

        return await connection.QueryAsync<LoanTransaction>(sql, parameters);
    }

    public async Task<decimal> GetTotalInterestPaidAsync(Guid loanId, DateTime? fromDate, DateTime? toDate)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var sql = "SELECT COALESCE(SUM(interest_amount), 0) FROM loan_transactions WHERE loan_id=@loanId AND transaction_type IN ('emi_payment', 'interest_accrual') AND interest_amount > 0";
        var parameters = new DynamicParameters();
        parameters.Add("loanId", loanId);

        if (fromDate.HasValue)
        {
            sql += " AND transaction_date >= @fromDate";
            parameters.Add("fromDate", fromDate.Value);
        }

        if (toDate.HasValue)
        {
            sql += " AND transaction_date <= @toDate";
            parameters.Add("toDate", toDate.Value);
        }

        return await connection.QuerySingleAsync<decimal>(sql, parameters);
    }
}




