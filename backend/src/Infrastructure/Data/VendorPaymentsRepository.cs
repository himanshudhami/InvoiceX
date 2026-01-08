using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class VendorPaymentsRepository : IVendorPaymentsRepository
    {
        private readonly string _connectionString;

        public VendorPaymentsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<VendorPayment?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE id = @id",
                new { id });
        }

        public async Task<VendorPayment?> GetByIdWithAllocationsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM vendor_payments WHERE id = @id;
                SELECT * FROM vendor_payment_allocations WHERE vendor_payment_id = @id;";

            using var multi = await connection.QueryMultipleAsync(sql, new { id });
            var payment = await multi.ReadFirstOrDefaultAsync<VendorPayment>();
            if (payment != null)
            {
                payment.Allocations = (await multi.ReadAsync<VendorPaymentAllocation>()).ToList();
            }
            return payment;
        }

        public async Task<IEnumerable<VendorPayment>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                "SELECT * FROM vendor_payments ORDER BY payment_date DESC");
        }

        public async Task<IEnumerable<VendorPayment>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE company_id = @companyId ORDER BY payment_date DESC",
                new { companyId });
        }

        public async Task<IEnumerable<VendorPayment>> GetByVendorIdAsync(Guid partyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE party_id = @partyId ORDER BY payment_date DESC",
                new { partyId });
        }

        public async Task<(IEnumerable<VendorPayment> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "party_id", "payment_number", "payment_date",
                "gross_amount", "tds_amount", "net_amount", "payment_method", "status",
                "bank_account_id", "reference_number", "cheque_number", "cheque_date",
                "utr_number", "is_posted", "is_reconciled", "financial_year",
                "tds_deposited", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("vendor_payments", allowedColumns)
                .SearchAcross(new[] { "payment_number", "reference_number", "cheque_number", "utr_number", "notes" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "payment_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<VendorPayment>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<IEnumerable<VendorPayment>> GetPendingApprovalAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE company_id = @companyId AND status = 'pending_approval' ORDER BY payment_date",
                new { companyId });
        }

        public async Task<IEnumerable<VendorPayment>> GetUnreconciledAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                @"SELECT * FROM vendor_payments
                WHERE company_id = @companyId
                AND status NOT IN ('draft', 'cancelled', 'failed')
                AND is_reconciled = FALSE
                ORDER BY payment_date",
                new { companyId });
        }

        public async Task<IEnumerable<VendorPayment>> GetTdsPaymentsAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                @"SELECT * FROM vendor_payments
                WHERE company_id = @companyId
                AND financial_year = @financialYear
                AND tds_amount > 0
                ORDER BY payment_date",
                new { companyId, financialYear });
        }

        public async Task<IEnumerable<VendorPayment>> GetPendingTdsDepositAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPayment>(
                @"SELECT * FROM vendor_payments
                WHERE company_id = @companyId
                AND tds_amount > 0
                AND tds_deposited = FALSE
                AND status NOT IN ('draft', 'cancelled', 'failed')
                ORDER BY payment_date",
                new { companyId });
        }

        public async Task<VendorPayment?> GetByTallyGuidAsync(string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE tally_voucher_guid = @tallyVoucherGuid",
                new { tallyVoucherGuid });
        }

        public async Task<VendorPayment?> GetByTallyGuidAsync(Guid companyId, string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorPayment>(
                "SELECT * FROM vendor_payments WHERE company_id = @companyId AND tally_voucher_guid = @tallyVoucherGuid",
                new { companyId, tallyVoucherGuid });
        }

        public async Task<VendorPayment> AddAsync(VendorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendor_payments (
                company_id, party_id, payment_date, amount, gross_amount, amount_in_inr,
                currency, payment_method, reference_number, cheque_number, cheque_date,
                notes, description, payment_type, status,
                tds_applicable, tds_section, tds_rate, tds_amount,
                tds_deposited, tds_challan_number, tds_deposit_date,
                financial_year, bank_account_id,
                is_posted, posted_journal_id, posted_at,
                is_reconciled, bank_transaction_id, reconciled_at,
                approved_by, approved_at,
                tally_voucher_guid, tally_voucher_number, tally_migration_batch_id,
                created_at, updated_at
            ) VALUES (
                @CompanyId, @PartyId, @PaymentDate, @Amount, @GrossAmount, @AmountInInr,
                @Currency, @PaymentMethod, @ReferenceNumber, @ChequeNumber, @ChequeDate,
                @Notes, @Description, @PaymentType, @Status,
                @TdsApplicable, @TdsSection, @TdsRate, @TdsAmount,
                @TdsDeposited, @TdsChallanNumber, @TdsDepositDate,
                @FinancialYear, @BankAccountId,
                @IsPosted, @PostedJournalId, @PostedAt,
                @IsReconciled, @BankTransactionId, @ReconciledAt,
                @ApprovedBy, @ApprovedAt,
                @TallyVoucherGuid, @TallyVoucherNumber, @TallyMigrationBatchId,
                NOW(), NOW()
            ) RETURNING *";

            return await connection.QuerySingleAsync<VendorPayment>(sql, entity);
        }

        public async Task UpdateAsync(VendorPayment entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE vendor_payments SET
                company_id = @CompanyId,
                party_id = @PartyId,
                payment_date = @PaymentDate,
                amount = @Amount,
                gross_amount = @GrossAmount,
                amount_in_inr = @AmountInInr,
                currency = @Currency,
                payment_method = @PaymentMethod,
                reference_number = @ReferenceNumber,
                cheque_number = @ChequeNumber,
                cheque_date = @ChequeDate,
                notes = @Notes,
                description = @Description,
                payment_type = @PaymentType,
                status = @Status,
                tds_applicable = @TdsApplicable,
                tds_section = @TdsSection,
                tds_rate = @TdsRate,
                tds_amount = @TdsAmount,
                tds_deposited = @TdsDeposited,
                tds_challan_number = @TdsChallanNumber,
                tds_deposit_date = @TdsDepositDate,
                financial_year = @FinancialYear,
                bank_account_id = @BankAccountId,
                is_posted = @IsPosted,
                posted_journal_id = @PostedJournalId,
                posted_at = @PostedAt,
                is_reconciled = @IsReconciled,
                bank_transaction_id = @BankTransactionId,
                reconciled_at = @ReconciledAt,
                approved_by = @ApprovedBy,
                approved_at = @ApprovedAt,
                tally_voucher_guid = @TallyVoucherGuid,
                tally_voucher_number = @TallyVoucherNumber,
                tally_migration_batch_id = @TallyMigrationBatchId,
                updated_at = NOW()
            WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM vendor_payments WHERE id = @id", new { id });
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE vendor_payments SET status = @status, updated_at = NOW() WHERE id = @id",
                new { id, status });
        }

        public async Task MarkAsPostedAsync(Guid id, Guid journalId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE vendor_payments SET
                    is_posted = TRUE,
                    posted_journal_id = @journalId,
                    posted_at = NOW(),
                    updated_at = NOW()
                WHERE id = @id",
                new { id, journalId });
        }

        public async Task MarkAsReconciledAsync(Guid id, Guid bankTransactionId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE vendor_payments SET
                    is_reconciled = TRUE,
                    reconciled_bank_transaction_id = @bankTransactionId,
                    reconciled_at = NOW(),
                    updated_at = NOW()
                WHERE id = @id",
                new { id, bankTransactionId });
        }

        public async Task MarkTdsDepositedAsync(Guid id, string challanNumber, DateOnly depositDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE vendor_payments SET
                    tds_deposited = TRUE,
                    tds_challan_number = @challanNumber,
                    tds_deposit_date = @depositDate,
                    updated_at = NOW()
                WHERE id = @id",
                new { id, challanNumber, depositDate });
        }

        // Allocation methods
        public async Task<IEnumerable<VendorPaymentAllocation>> GetAllocationsAsync(Guid vendorPaymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPaymentAllocation>(
                "SELECT * FROM vendor_payment_allocations WHERE vendor_payment_id = @vendorPaymentId",
                new { vendorPaymentId });
        }

        public async Task<IEnumerable<VendorPaymentAllocation>> GetAllocationsByInvoiceAsync(Guid vendorInvoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPaymentAllocation>(
                "SELECT * FROM vendor_payment_allocations WHERE vendor_invoice_id = @vendorInvoiceId",
                new { vendorInvoiceId });
        }

        public async Task<VendorPaymentAllocation> AddAllocationAsync(VendorPaymentAllocation allocation)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendor_payment_allocations (
                vendor_payment_id, vendor_invoice_id, allocated_amount, tds_allocated,
                allocation_type, tally_bill_ref, notes, created_at, updated_at
            ) VALUES (
                @VendorPaymentId, @VendorInvoiceId, @AllocatedAmount, @TdsAllocated,
                @AllocationType, @TallyBillRef, @Notes, NOW(), NOW()
            ) RETURNING *";

            return await connection.QuerySingleAsync<VendorPaymentAllocation>(sql, allocation);
        }

        public async Task UpdateAllocationAsync(VendorPaymentAllocation allocation)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE vendor_payment_allocations SET
                vendor_payment_id = @VendorPaymentId,
                vendor_invoice_id = @VendorInvoiceId,
                allocated_amount = @AllocatedAmount,
                tds_allocated = @TdsAllocated,
                allocation_type = @AllocationType,
                tally_bill_ref = @TallyBillRef,
                notes = @Notes,
                updated_at = NOW()
            WHERE id = @Id";
            await connection.ExecuteAsync(sql, allocation);
        }

        public async Task DeleteAllocationAsync(Guid allocationId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM vendor_payment_allocations WHERE id = @allocationId",
                new { allocationId });
        }

        public async Task DeleteAllocationsByPaymentIdAsync(Guid vendorPaymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM vendor_payment_allocations WHERE vendor_payment_id = @vendorPaymentId",
                new { vendorPaymentId });
        }

        // Report methods
        public async Task<decimal> GetTotalPaidToVendorAsync(Guid partyId, DateOnly? fromDate = null, DateOnly? toDate = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(net_amount), 0)
                FROM vendor_payments
                WHERE party_id = @partyId
                AND status NOT IN ('draft', 'cancelled', 'failed')";

            if (fromDate.HasValue)
                sql += " AND payment_date >= @fromDate";
            if (toDate.HasValue)
                sql += " AND payment_date <= @toDate";

            return await connection.QuerySingleAsync<decimal>(sql, new { partyId, fromDate, toDate });
        }

        public async Task<decimal> GetTotalTdsDeductedAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
                SELECT COALESCE(SUM(tds_amount), 0)
                FROM vendor_payments
                WHERE company_id = @companyId
                AND financial_year = @financialYear
                AND status NOT IN ('draft', 'cancelled', 'failed')";

            return await connection.QuerySingleAsync<decimal>(sql, new { companyId, financialYear });
        }

        public async Task<IEnumerable<VendorPayment>> BulkAddAsync(IEnumerable<VendorPayment> entities)
        {
            var results = new List<VendorPayment>();
            foreach (var entity in entities)
            {
                results.Add(await AddAsync(entity));
            }
            return results;
        }
    }
}
