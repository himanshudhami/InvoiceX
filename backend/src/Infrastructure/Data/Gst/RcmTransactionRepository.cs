using Core.Entities.Gst;
using Core.Interfaces.Gst;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data.Gst
{
    /// <summary>
    /// Repository implementation for RCM (Reverse Charge Mechanism) transactions
    /// </summary>
    public class RcmTransactionRepository : IRcmTransactionRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "financial_year", "return_period",
            "source_type", "source_id", "source_number",
            "vendor_name", "vendor_gstin", "vendor_pan", "vendor_state_code",
            "vendor_invoice_number", "vendor_invoice_date",
            "rcm_category_id", "rcm_category_code", "rcm_notification",
            "place_of_supply", "supply_type", "hsn_sac_code", "description",
            "taxable_value", "cgst_rate", "cgst_amount", "sgst_rate", "sgst_amount",
            "igst_rate", "igst_amount", "cess_rate", "cess_amount", "total_rcm_tax",
            "liability_recognized", "liability_recognized_at", "liability_journal_id",
            "rcm_paid", "rcm_payment_date", "rcm_payment_journal_id", "rcm_payment_reference",
            "itc_eligible", "itc_claimed", "itc_claim_date", "itc_claim_journal_id", "itc_claim_period",
            "itc_blocked", "itc_blocked_reason",
            "gstr3b_period", "gstr3b_table", "gstr3b_filed",
            "status", "notes",
            "created_at", "updated_at", "created_by"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "vendor_name", "vendor_gstin", "vendor_invoice_number", "rcm_category_code", "description"
        };

        public RcmTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<RcmTransaction?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<RcmTransaction>(
                "SELECT * FROM rcm_transactions WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<RcmTransaction>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                "SELECT * FROM rcm_transactions ORDER BY created_at DESC");
        }

        public async Task<(IEnumerable<RcmTransaction> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object?>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var builder = SqlQueryBuilder
                .From("rcm_transactions", AllColumns)
                .SearchAcross(SearchableColumns, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<RcmTransaction>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<RcmTransaction> AddAsync(RcmTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO rcm_transactions (
                    id, company_id, financial_year, return_period,
                    source_type, source_id, source_number,
                    vendor_name, vendor_gstin, vendor_pan, vendor_state_code,
                    vendor_invoice_number, vendor_invoice_date,
                    rcm_category_id, rcm_category_code, rcm_notification,
                    place_of_supply, supply_type, hsn_sac_code, description,
                    taxable_value, cgst_rate, cgst_amount, sgst_rate, sgst_amount,
                    igst_rate, igst_amount, cess_rate, cess_amount, total_rcm_tax,
                    liability_recognized, liability_recognized_at, liability_journal_id,
                    rcm_paid, rcm_payment_date, rcm_payment_journal_id, rcm_payment_reference,
                    itc_eligible, itc_claimed, itc_claim_date, itc_claim_journal_id, itc_claim_period,
                    itc_blocked, itc_blocked_reason,
                    gstr3b_period, gstr3b_table, gstr3b_filed,
                    status, notes,
                    created_at, updated_at, created_by
                )
                VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CompanyId, @FinancialYear, @ReturnPeriod,
                    @SourceType, @SourceId, @SourceNumber,
                    @VendorName, @VendorGstin, @VendorPan, @VendorStateCode,
                    @VendorInvoiceNumber, @VendorInvoiceDate,
                    @RcmCategoryId, @RcmCategoryCode, @RcmNotification,
                    @PlaceOfSupply, @SupplyType, @HsnSacCode, @Description,
                    @TaxableValue, @CgstRate, @CgstAmount, @SgstRate, @SgstAmount,
                    @IgstRate, @IgstAmount, @CessRate, @CessAmount, @TotalRcmTax,
                    @LiabilityRecognized, @LiabilityRecognizedAt, @LiabilityJournalId,
                    @RcmPaid, @RcmPaymentDate, @RcmPaymentJournalId, @RcmPaymentReference,
                    @ItcEligible, @ItcClaimed, @ItcClaimDate, @ItcClaimJournalId, @ItcClaimPeriod,
                    @ItcBlocked, @ItcBlockedReason,
                    @Gstr3bPeriod, @Gstr3bTable, @Gstr3bFiled,
                    @Status, @Notes,
                    NOW(), NOW(), @CreatedBy
                )
                RETURNING *";

            return await connection.QuerySingleAsync<RcmTransaction>(sql, entity);
        }

        public async Task UpdateAsync(RcmTransaction entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE rcm_transactions SET
                    company_id = @CompanyId,
                    financial_year = @FinancialYear,
                    return_period = @ReturnPeriod,
                    source_type = @SourceType,
                    source_id = @SourceId,
                    source_number = @SourceNumber,
                    vendor_name = @VendorName,
                    vendor_gstin = @VendorGstin,
                    vendor_pan = @VendorPan,
                    vendor_state_code = @VendorStateCode,
                    vendor_invoice_number = @VendorInvoiceNumber,
                    vendor_invoice_date = @VendorInvoiceDate,
                    rcm_category_id = @RcmCategoryId,
                    rcm_category_code = @RcmCategoryCode,
                    rcm_notification = @RcmNotification,
                    place_of_supply = @PlaceOfSupply,
                    supply_type = @SupplyType,
                    hsn_sac_code = @HsnSacCode,
                    description = @Description,
                    taxable_value = @TaxableValue,
                    cgst_rate = @CgstRate,
                    cgst_amount = @CgstAmount,
                    sgst_rate = @SgstRate,
                    sgst_amount = @SgstAmount,
                    igst_rate = @IgstRate,
                    igst_amount = @IgstAmount,
                    cess_rate = @CessRate,
                    cess_amount = @CessAmount,
                    total_rcm_tax = @TotalRcmTax,
                    liability_recognized = @LiabilityRecognized,
                    liability_recognized_at = @LiabilityRecognizedAt,
                    liability_journal_id = @LiabilityJournalId,
                    rcm_paid = @RcmPaid,
                    rcm_payment_date = @RcmPaymentDate,
                    rcm_payment_journal_id = @RcmPaymentJournalId,
                    rcm_payment_reference = @RcmPaymentReference,
                    itc_eligible = @ItcEligible,
                    itc_claimed = @ItcClaimed,
                    itc_claim_date = @ItcClaimDate,
                    itc_claim_journal_id = @ItcClaimJournalId,
                    itc_claim_period = @ItcClaimPeriod,
                    itc_blocked = @ItcBlocked,
                    itc_blocked_reason = @ItcBlockedReason,
                    gstr3b_period = @Gstr3bPeriod,
                    gstr3b_table = @Gstr3bTable,
                    gstr3b_filed = @Gstr3bFiled,
                    status = @Status,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM rcm_transactions WHERE id = @id",
                new { id });
        }

        // ==================== Company & Period Queries ====================

        public async Task<IEnumerable<RcmTransaction>> GetByCompanyAsync(Guid companyId, string? returnPeriod = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = "SELECT * FROM rcm_transactions WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(returnPeriod))
            {
                sql += " AND return_period = @returnPeriod";
            }
            sql += " ORDER BY created_at DESC";

            return await connection.QueryAsync<RcmTransaction>(sql, new { companyId, returnPeriod });
        }

        public async Task<IEnumerable<RcmTransaction>> GetByCompanyAndFYAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId AND financial_year = @financialYear
                  ORDER BY created_at DESC",
                new { companyId, financialYear });
        }

        // ==================== Status-based Queries ====================

        public async Task<IEnumerable<RcmTransaction>> GetPendingLiabilityAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId AND liability_recognized = false AND status = 'pending'
                  ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<RcmTransaction>> GetPendingPaymentAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId
                    AND liability_recognized = true
                    AND rcm_paid = false
                    AND status = 'liability_created'
                  ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<RcmTransaction>> GetPaidAwaitingItcClaimAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId
                    AND rcm_paid = true
                    AND itc_claimed = false
                    AND itc_eligible = true
                    AND itc_blocked = false
                    AND status = 'rcm_paid'
                  ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<RcmTransaction>> GetByStatusAsync(Guid companyId, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId AND status = @status
                  ORDER BY created_at DESC",
                new { companyId, status });
        }

        // ==================== Source Document Queries ====================

        public async Task<RcmTransaction?> GetBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE source_type = @sourceType AND source_id = @sourceId",
                new { sourceType, sourceId });
        }

        public async Task<bool> ExistsBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM rcm_transactions
                    WHERE source_type = @sourceType AND source_id = @sourceId
                )",
                new { sourceType, sourceId });
        }

        // ==================== Category Queries ====================

        public async Task<IEnumerable<RcmTransaction>> GetByCategoryAsync(Guid companyId, string categoryCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<RcmTransaction>(
                @"SELECT * FROM rcm_transactions
                  WHERE company_id = @companyId AND rcm_category_code = @categoryCode
                  ORDER BY created_at DESC",
                new { companyId, categoryCode });
        }

        // ==================== Aggregations ====================

        public async Task<decimal> GetTotalRcmLiabilityAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(total_rcm_tax), 0)
                  FROM rcm_transactions
                  WHERE company_id = @companyId
                    AND return_period = @returnPeriod
                    AND liability_recognized = true",
                new { companyId, returnPeriod });
        }

        public async Task<decimal> GetTotalPendingPaymentAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(total_rcm_tax), 0)
                  FROM rcm_transactions
                  WHERE company_id = @companyId
                    AND liability_recognized = true
                    AND rcm_paid = false",
                new { companyId });
        }

        public async Task<RcmPeriodSummary> GetPeriodSummaryAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var summaryQuery = @"
                SELECT
                    @returnPeriod as ReturnPeriod,
                    COALESCE(SUM(taxable_value), 0) as TotalTaxableValue,
                    COALESCE(SUM(cgst_amount), 0) as TotalCgst,
                    COALESCE(SUM(sgst_amount), 0) as TotalSgst,
                    COALESCE(SUM(igst_amount), 0) as TotalIgst,
                    COALESCE(SUM(total_rcm_tax), 0) as TotalRcmTax,
                    COALESCE(SUM(total_rcm_tax) FILTER (WHERE rcm_paid = true), 0) as RcmPaidAmount,
                    COALESCE(SUM(total_rcm_tax) FILTER (WHERE rcm_paid = false AND liability_recognized = true), 0) as RcmPendingAmount,
                    COALESCE(SUM(total_rcm_tax) FILTER (WHERE itc_claimed = true), 0) as ItcClaimedAmount,
                    COUNT(*) as TotalTransactions,
                    COUNT(*) FILTER (WHERE rcm_paid = false AND liability_recognized = true) as PendingTransactions
                FROM rcm_transactions
                WHERE company_id = @companyId AND return_period = @returnPeriod";

            var summary = await connection.QuerySingleAsync<RcmPeriodSummary>(summaryQuery, new { companyId, returnPeriod });

            // Get category breakdown
            var categoryQuery = @"
                SELECT
                    rcm_category_code as CategoryCode,
                    '' as CategoryName,
                    COALESCE(SUM(taxable_value), 0) as TaxableValue,
                    COALESCE(SUM(total_rcm_tax), 0) as RcmTax,
                    COUNT(*) as TransactionCount
                FROM rcm_transactions
                WHERE company_id = @companyId AND return_period = @returnPeriod
                GROUP BY rcm_category_code";

            var categories = await connection.QueryAsync<RcmCategorySummary>(categoryQuery, new { companyId, returnPeriod });
            summary.CategoryBreakdown = categories.ToList();

            return summary;
        }

        // ==================== Status Updates ====================

        public async Task MarkLiabilityRecognizedAsync(Guid id, Guid journalEntryId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE rcm_transactions SET
                    liability_recognized = true,
                    liability_recognized_at = NOW(),
                    liability_journal_id = @journalEntryId,
                    status = 'liability_created',
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, journalEntryId });
        }

        public async Task MarkRcmPaidAsync(Guid id, DateTime paymentDate, Guid journalEntryId, string? paymentReference = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE rcm_transactions SET
                    rcm_paid = true,
                    rcm_payment_date = @paymentDate,
                    rcm_payment_journal_id = @journalEntryId,
                    rcm_payment_reference = @paymentReference,
                    status = 'rcm_paid',
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, paymentDate, journalEntryId, paymentReference });
        }

        public async Task MarkItcClaimedAsync(Guid id, Guid journalEntryId, string claimPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE rcm_transactions SET
                    itc_claimed = true,
                    itc_claim_date = NOW(),
                    itc_claim_journal_id = @journalEntryId,
                    itc_claim_period = @claimPeriod,
                    status = 'itc_claimed',
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, journalEntryId, claimPeriod });
        }

        public async Task MarkItcBlockedAsync(Guid id, string reason)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE rcm_transactions SET
                    itc_blocked = true,
                    itc_blocked_reason = @reason,
                    itc_eligible = false,
                    status = 'itc_blocked',
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, reason });
        }
    }
}
