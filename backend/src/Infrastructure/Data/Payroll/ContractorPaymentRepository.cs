using Core.Entities.Payroll;
using Core.Interfaces.Payroll;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Payroll
{
    public class ContractorPaymentRepository : IContractorPaymentRepository
    {
        private readonly string _connectionString;
        private static readonly string[] AllowedColumns = new[]
        {
            "id", "employee_id", "company_id", "payment_month", "payment_year", "invoice_number",
            "contract_reference", "gross_amount", "tds_rate", "tds_amount", "other_deductions",
            "net_payable", "gst_applicable", "gst_rate", "gst_amount", "total_invoice_amount",
            "status", "payment_date", "payment_method", "payment_reference", "description",
            "remarks", "created_at", "updated_at", "created_by", "updated_by"
        };

        public ContractorPaymentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<ContractorPayment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ContractorPayment>(
                "SELECT * FROM contractor_payments WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<ContractorPayment>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                "SELECT * FROM contractor_payments ORDER BY payment_year DESC, payment_month DESC");
        }

        public async Task<IEnumerable<ContractorPayment>> GetByEmployeeIdAsync(Guid employeeId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                "SELECT * FROM contractor_payments WHERE employee_id = @employeeId ORDER BY payment_year DESC, payment_month DESC",
                new { employeeId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<ContractorPayment>(
                "SELECT * FROM contractor_payments WHERE company_id = @companyId ORDER BY payment_year DESC, payment_month DESC",
                new { companyId });
        }

        public async Task<ContractorPayment?> GetByEmployeeAndMonthAsync(Guid employeeId, int paymentMonth, int paymentYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ContractorPayment>(
                "SELECT * FROM contractor_payments WHERE employee_id = @employeeId AND payment_month = @paymentMonth AND payment_year = @paymentYear",
                new { employeeId, paymentMonth, paymentYear });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByMonthYearAsync(int paymentMonth, int paymentYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM contractor_payments WHERE payment_month = @paymentMonth AND payment_year = @paymentYear";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            sql += " ORDER BY created_at";

            return await connection.QueryAsync<ContractorPayment>(sql, new { paymentMonth, paymentYear, companyId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByStatusAsync(string status, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM contractor_payments WHERE status = @status";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            sql += " ORDER BY payment_year DESC, payment_month DESC";

            return await connection.QueryAsync<ContractorPayment>(sql, new { status, companyId });
        }

        public async Task<IEnumerable<ContractorPayment>> GetByFinancialYearAsync(Guid employeeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            return await connection.QueryAsync<ContractorPayment>(
                @"SELECT * FROM contractor_payments
                  WHERE employee_id = @employeeId
                    AND ((payment_year = @startYear AND payment_month >= 4)
                         OR (payment_year = @endYear AND payment_month <= 3))
                  ORDER BY payment_year, payment_month",
                new { employeeId, startYear, endYear });
        }

        public async Task<(IEnumerable<ContractorPayment> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("contractor_payments", AllowedColumns)
                .SearchAcross(new[] { "invoice_number", "contract_reference", "status", "description", "remarks" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<ContractorPayment>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<ContractorPayment> AddAsync(ContractorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO contractor_payments
                (employee_id, company_id, payment_month, payment_year, invoice_number,
                 contract_reference, gross_amount, tds_rate, tds_amount, other_deductions,
                 net_payable, gst_applicable, gst_rate, gst_amount, total_invoice_amount,
                 status, payment_date, payment_method, payment_reference, description,
                 remarks, created_at, updated_at, created_by, updated_by)
                VALUES
                (@EmployeeId, @CompanyId, @PaymentMonth, @PaymentYear, @InvoiceNumber,
                 @ContractReference, @GrossAmount, @TdsRate, @TdsAmount, @OtherDeductions,
                 @NetPayable, @GstApplicable, @GstRate, @GstAmount, @TotalInvoiceAmount,
                 @Status, @PaymentDate, @PaymentMethod, @PaymentReference, @Description,
                 @Remarks, NOW(), NOW(), @CreatedBy, @UpdatedBy)
                RETURNING *";

            return await connection.QuerySingleAsync<ContractorPayment>(sql, entity);
        }

        public async Task UpdateAsync(ContractorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE contractor_payments SET
                employee_id = @EmployeeId,
                company_id = @CompanyId,
                payment_month = @PaymentMonth,
                payment_year = @PaymentYear,
                invoice_number = @InvoiceNumber,
                contract_reference = @ContractReference,
                gross_amount = @GrossAmount,
                tds_rate = @TdsRate,
                tds_amount = @TdsAmount,
                other_deductions = @OtherDeductions,
                net_payable = @NetPayable,
                gst_applicable = @GstApplicable,
                gst_rate = @GstRate,
                gst_amount = @GstAmount,
                total_invoice_amount = @TotalInvoiceAmount,
                status = @Status,
                payment_date = @PaymentDate,
                payment_method = @PaymentMethod,
                payment_reference = @PaymentReference,
                description = @Description,
                remarks = @Remarks,
                updated_at = NOW(),
                updated_by = @UpdatedBy
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM contractor_payments WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<ContractorPayment>> BulkAddAsync(IEnumerable<ContractorPayment> entities)
        {
            var results = new List<ContractorPayment>();
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var entity in entities)
            {
                var sql = @"INSERT INTO contractor_payments
                    (employee_id, company_id, payment_month, payment_year, invoice_number,
                     contract_reference, gross_amount, tds_rate, tds_amount, other_deductions,
                     net_payable, gst_applicable, gst_rate, gst_amount, total_invoice_amount,
                     status, payment_date, payment_method, payment_reference, description,
                     remarks, created_at, updated_at, created_by, updated_by)
                    VALUES
                    (@EmployeeId, @CompanyId, @PaymentMonth, @PaymentYear, @InvoiceNumber,
                     @ContractReference, @GrossAmount, @TdsRate, @TdsAmount, @OtherDeductions,
                     @NetPayable, @GstApplicable, @GstRate, @GstAmount, @TotalInvoiceAmount,
                     @Status, @PaymentDate, @PaymentMethod, @PaymentReference, @Description,
                     @Remarks, NOW(), NOW(), @CreatedBy, @UpdatedBy)
                    RETURNING *";
                var created = await connection.QuerySingleAsync<ContractorPayment>(sql, entity);
                results.Add(created);
            }

            return results;
        }

        public async Task<bool> ExistsForEmployeeAndMonthAsync(Guid employeeId, int paymentMonth, int paymentYear, Guid? excludeId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM contractor_payments WHERE employee_id = @employeeId AND payment_month = @paymentMonth AND payment_year = @paymentYear AND id != @excludeId"
                : "SELECT COUNT(*) FROM contractor_payments WHERE employee_id = @employeeId AND payment_month = @paymentMonth AND payment_year = @paymentYear";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { employeeId, paymentMonth, paymentYear, excludeId });
            return count > 0;
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE contractor_payments SET
                status = @status,
                payment_date = CASE WHEN @status = 'paid' THEN NOW() ELSE payment_date END,
                updated_at = NOW()
                WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, status });
        }

        public async Task<Dictionary<string, decimal>> GetMonthlySummaryAsync(int paymentMonth, int paymentYear, Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT
                COALESCE(SUM(gross_amount), 0) AS TotalGross,
                COALESCE(SUM(tds_amount), 0) AS TotalTds,
                COALESCE(SUM(gst_amount), 0) AS TotalGst,
                COALESCE(SUM(net_payable), 0) AS TotalNet,
                COALESCE(SUM(total_invoice_amount), 0) AS TotalInvoice,
                COUNT(*) AS ContractorCount
                FROM contractor_payments
                WHERE payment_month = @paymentMonth AND payment_year = @paymentYear";

            if (companyId.HasValue)
            {
                sql += " AND company_id = @companyId";
            }

            var result = await connection.QueryFirstAsync(sql, new { paymentMonth, paymentYear, companyId });
            return new Dictionary<string, decimal>
            {
                ["TotalGross"] = result.totalgross,
                ["TotalTds"] = result.totaltds,
                ["TotalGst"] = result.totalgst,
                ["TotalNet"] = result.totalnet,
                ["TotalInvoice"] = result.totalinvoice,
                ["ContractorCount"] = result.contractorcount
            };
        }

        public async Task<Dictionary<string, decimal>> GetYtdSummaryAsync(Guid employeeId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = 2000 + int.Parse(parts[1]);

            var sql = @"SELECT
                COALESCE(SUM(gross_amount), 0) AS YtdGross,
                COALESCE(SUM(tds_amount), 0) AS YtdTds,
                COALESCE(SUM(gst_amount), 0) AS YtdGst,
                COALESCE(SUM(net_payable), 0) AS YtdNet,
                COUNT(*) AS PaymentCount
                FROM contractor_payments
                WHERE employee_id = @employeeId
                  AND ((payment_year = @startYear AND payment_month >= 4)
                       OR (payment_year = @endYear AND payment_month <= 3))";

            var result = await connection.QueryFirstAsync(sql, new { employeeId, startYear, endYear });
            return new Dictionary<string, decimal>
            {
                ["YtdGross"] = result.ytdgross,
                ["YtdTds"] = result.ytdtds,
                ["YtdGst"] = result.ytdgst,
                ["YtdNet"] = result.ytdnet,
                ["PaymentCount"] = result.paymentcount
            };
        }
    }
}
