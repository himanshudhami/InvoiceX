using Core.Entities.Ledger;
using Core.Interfaces.Ledger;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Ledger
{
    public class GstInputCreditRepository : IGstInputCreditRepository
    {
        private readonly string _connectionString;

        private static readonly string[] AllColumns = new[]
        {
            "id", "company_id", "financial_year", "return_period",
            "source_type", "source_id", "source_number",
            "vendor_gstin", "vendor_name", "vendor_invoice_number", "vendor_invoice_date",
            "place_of_supply", "supply_type", "hsn_sac_code", "taxable_value",
            "cgst_rate", "cgst_amount", "sgst_rate", "sgst_amount",
            "igst_rate", "igst_amount", "cess_rate", "cess_amount", "total_gst",
            "itc_eligible", "ineligible_reason",
            "matched_with_gstr2b", "gstr2b_match_date", "gstr2b_mismatch_reason",
            "status", "claimed_in_gstr3b", "gstr3b_filing_period", "claimed_at", "claimed_by",
            "is_reversed", "reversal_amount", "reversal_reason", "reversal_date",
            "created_at", "updated_at"
        };

        private static readonly string[] SearchableColumns = new[]
        {
            "vendor_name", "vendor_gstin", "vendor_invoice_number", "source_number"
        };

        public GstInputCreditRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<GstInputCredit?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<GstInputCredit>(
                "SELECT * FROM gst_input_credit WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<GstInputCredit>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                "SELECT * FROM gst_input_credit ORDER BY created_at DESC");
        }

        public async Task<GstInputCredit> AddAsync(GstInputCredit entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO gst_input_credit (
                    company_id, financial_year, return_period,
                    source_type, source_id, source_number,
                    vendor_gstin, vendor_name, vendor_invoice_number, vendor_invoice_date,
                    place_of_supply, supply_type, hsn_sac_code, taxable_value,
                    cgst_rate, cgst_amount, sgst_rate, sgst_amount,
                    igst_rate, igst_amount, cess_rate, cess_amount, total_gst,
                    itc_eligible, ineligible_reason,
                    status, created_at, updated_at
                )
                VALUES (
                    @CompanyId, @FinancialYear, @ReturnPeriod,
                    @SourceType, @SourceId, @SourceNumber,
                    @VendorGstin, @VendorName, @VendorInvoiceNumber, @VendorInvoiceDate,
                    @PlaceOfSupply, @SupplyType, @HsnSacCode, @TaxableValue,
                    @CgstRate, @CgstAmount, @SgstRate, @SgstAmount,
                    @IgstRate, @IgstAmount, @CessRate, @CessAmount, @TotalGst,
                    @ItcEligible, @IneligibleReason,
                    @Status, NOW(), NOW()
                )
                RETURNING *";

            return await connection.QuerySingleAsync<GstInputCredit>(sql, entity);
        }

        public async Task UpdateAsync(GstInputCredit entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE gst_input_credit SET
                    vendor_gstin = @VendorGstin,
                    vendor_name = @VendorName,
                    vendor_invoice_number = @VendorInvoiceNumber,
                    vendor_invoice_date = @VendorInvoiceDate,
                    place_of_supply = @PlaceOfSupply,
                    supply_type = @SupplyType,
                    hsn_sac_code = @HsnSacCode,
                    taxable_value = @TaxableValue,
                    cgst_rate = @CgstRate, cgst_amount = @CgstAmount,
                    sgst_rate = @SgstRate, sgst_amount = @SgstAmount,
                    igst_rate = @IgstRate, igst_amount = @IgstAmount,
                    cess_rate = @CessRate, cess_amount = @CessAmount,
                    total_gst = @TotalGst,
                    itc_eligible = @ItcEligible,
                    ineligible_reason = @IneligibleReason,
                    matched_with_gstr2b = @MatchedWithGstr2B,
                    gstr2b_match_date = @Gstr2BMatchDate,
                    gstr2b_mismatch_reason = @Gstr2BMismatchReason,
                    status = @Status,
                    claimed_in_gstr3b = @ClaimedInGstr3B,
                    gstr3b_filing_period = @Gstr3BFilingPeriod,
                    claimed_at = @ClaimedAt,
                    claimed_by = @ClaimedBy,
                    is_reversed = @IsReversed,
                    reversal_amount = @ReversalAmount,
                    reversal_reason = @ReversalReason,
                    reversal_date = @ReversalDate,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM gst_input_credit WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<GstInputCredit>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                "SELECT * FROM gst_input_credit WHERE company_id = @companyId ORDER BY created_at DESC",
                new { companyId });
        }

        public async Task<IEnumerable<GstInputCredit>> GetByReturnPeriodAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                @"SELECT * FROM gst_input_credit
                  WHERE company_id = @companyId AND return_period = @returnPeriod
                  ORDER BY created_at DESC",
                new { companyId, returnPeriod });
        }

        public async Task<IEnumerable<GstInputCredit>> GetByFinancialYearAsync(Guid companyId, string financialYear)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                @"SELECT * FROM gst_input_credit
                  WHERE company_id = @companyId AND financial_year = @financialYear
                  ORDER BY return_period, created_at DESC",
                new { companyId, financialYear });
        }

        public async Task<GstInputCredit?> GetBySourceAsync(string sourceType, Guid sourceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<GstInputCredit>(
                "SELECT * FROM gst_input_credit WHERE source_type = @sourceType AND source_id = @sourceId",
                new { sourceType, sourceId });
        }

        public async Task<IEnumerable<GstInputCredit>> GetBySourceTypeAsync(Guid companyId, string sourceType)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                @"SELECT * FROM gst_input_credit
                  WHERE company_id = @companyId AND source_type = @sourceType
                  ORDER BY created_at DESC",
                new { companyId, sourceType });
        }

        public async Task<IEnumerable<GstInputCredit>> GetPendingAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                @"SELECT * FROM gst_input_credit
                  WHERE company_id = @companyId AND status = 'pending' AND itc_eligible = TRUE
                  ORDER BY vendor_invoice_date",
                new { companyId });
        }

        public async Task<IEnumerable<GstInputCredit>> GetClaimedAsync(Guid companyId, string? returnPeriod = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT * FROM gst_input_credit
                        WHERE company_id = @companyId AND status = 'claimed'";

            if (!string.IsNullOrEmpty(returnPeriod))
            {
                sql += " AND gstr3b_filing_period = @returnPeriod";
            }

            sql += " ORDER BY claimed_at DESC";

            return await connection.QueryAsync<GstInputCredit>(sql, new { companyId, returnPeriod });
        }

        public async Task<IEnumerable<GstInputCredit>> GetUnmatchedAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<GstInputCredit>(
                @"SELECT * FROM gst_input_credit
                  WHERE company_id = @companyId AND matched_with_gstr2b = FALSE AND itc_eligible = TRUE
                  ORDER BY vendor_invoice_date",
                new { companyId });
        }

        public async Task<(decimal TotalCgst, decimal TotalSgst, decimal TotalIgst, decimal TotalCess)> GetItcSummaryAsync(
            Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT
                    COALESCE(SUM(cgst_amount), 0) as total_cgst,
                    COALESCE(SUM(sgst_amount), 0) as total_sgst,
                    COALESCE(SUM(igst_amount), 0) as total_igst,
                    COALESCE(SUM(cess_amount), 0) as total_cess
                  FROM gst_input_credit
                  WHERE company_id = @companyId
                    AND gstr3b_filing_period = @returnPeriod
                    AND status = 'claimed'",
                new { companyId, returnPeriod });

            return (result?.total_cgst ?? 0, result?.total_sgst ?? 0, result?.total_igst ?? 0, result?.total_cess ?? 0);
        }

        public async Task<(decimal TotalCgst, decimal TotalSgst, decimal TotalIgst, decimal TotalCess)> GetPendingItcSummaryAsync(
            Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT
                    COALESCE(SUM(cgst_amount), 0) as total_cgst,
                    COALESCE(SUM(sgst_amount), 0) as total_sgst,
                    COALESCE(SUM(igst_amount), 0) as total_igst,
                    COALESCE(SUM(cess_amount), 0) as total_cess
                  FROM gst_input_credit
                  WHERE company_id = @companyId
                    AND status = 'pending'
                    AND itc_eligible = TRUE",
                new { companyId });

            return (result?.total_cgst ?? 0, result?.total_sgst ?? 0, result?.total_igst ?? 0, result?.total_cess ?? 0);
        }

        public async Task MarkAsClaimedAsync(IEnumerable<Guid> ids, string returnPeriod, string claimedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gst_input_credit SET
                    status = 'claimed',
                    claimed_in_gstr3b = TRUE,
                    gstr3b_filing_period = @returnPeriod,
                    claimed_at = NOW(),
                    claimed_by = @claimedBy,
                    updated_at = NOW()
                  WHERE id = ANY(@ids)",
                new { ids = ids.ToArray(), returnPeriod, claimedBy });
        }

        public async Task MarkAsMatchedAsync(Guid id, DateTime matchDate)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gst_input_credit SET
                    matched_with_gstr2b = TRUE,
                    gstr2b_match_date = @matchDate,
                    gstr2b_mismatch_reason = NULL,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, matchDate });
        }

        public async Task ReverseItcAsync(Guid id, decimal amount, string reason)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gst_input_credit SET
                    is_reversed = TRUE,
                    reversal_amount = @amount,
                    reversal_reason = @reason,
                    reversal_date = CURRENT_DATE,
                    status = 'reversed',
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, amount, reason });
        }

        public async Task<(IEnumerable<GstInputCredit> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var conditions = new List<string>();
            var parameters = new DynamicParameters();

            if (filters != null)
            {
                foreach (var filter in filters)
                {
                    var paramName = filter.Key.Replace(".", "_");
                    conditions.Add($"{filter.Key} = @{paramName}");
                    parameters.Add(paramName, filter.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchConditions = SearchableColumns.Select(col => $"{col} ILIKE @searchTerm");
                conditions.Add($"({string.Join(" OR ", searchConditions)})");
                parameters.Add("searchTerm", $"%{searchTerm}%");
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            var allowedSet = new HashSet<string>(AllColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "created_at";
            var sortDirection = sortDescending ? "DESC" : "ASC";

            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"
                SELECT * FROM gst_input_credit
                {whereClause}
                ORDER BY {orderBy} {sortDirection}
                LIMIT @pageSize OFFSET @offset";

            var countSql = $@"
                SELECT COUNT(*) FROM gst_input_credit
                {whereClause}";

            parameters.Add("pageSize", pageSize);
            parameters.Add("offset", offset);

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<GstInputCredit>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }
    }
}
