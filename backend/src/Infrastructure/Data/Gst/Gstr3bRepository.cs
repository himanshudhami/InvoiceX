using Core.Entities.Gst;
using Core.Interfaces.Gst;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Gst
{
    /// <summary>
    /// Repository implementation for GSTR-3B filings
    /// </summary>
    public class Gstr3bRepository : IGstr3bRepository
    {
        private readonly string _connectionString;

        public Gstr3bRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Filing CRUD ====================

        public async Task<Gstr3bFiling?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr3bFiling>(
                @"SELECT id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date,
                    table_3_1 as Table31Json, table_3_2 as Table32Json,
                    table_4 as Table4Json, table_5 as Table5Json, table_6_1 as Table61Json,
                    previous_period_variance as PreviousPeriodVarianceJson,
                    notes, created_at, updated_at
                  FROM gstr3b_filings WHERE id = @id",
                new { id });
        }

        public async Task<Gstr3bFiling?> GetByPeriodAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr3bFiling>(
                @"SELECT id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date,
                    table_3_1 as Table31Json, table_3_2 as Table32Json,
                    table_4 as Table4Json, table_5 as Table5Json, table_6_1 as Table61Json,
                    previous_period_variance as PreviousPeriodVarianceJson,
                    notes, created_at, updated_at
                  FROM gstr3b_filings
                  WHERE company_id = @companyId AND return_period = @returnPeriod",
                new { companyId, returnPeriod });
        }

        public async Task<IEnumerable<Gstr3bFiling>> GetByCompanyAsync(Guid companyId, string? financialYear = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"SELECT id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date, notes, created_at, updated_at
                  FROM gstr3b_filings
                  WHERE company_id = @companyId";

            if (!string.IsNullOrEmpty(financialYear))
            {
                sql += " AND financial_year = @financialYear";
            }
            sql += " ORDER BY return_period DESC";

            return await connection.QueryAsync<Gstr3bFiling>(sql, new { companyId, financialYear });
        }

        public async Task<Gstr3bFiling> AddAsync(Gstr3bFiling filing)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO gstr3b_filings (
                    id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date,
                    table_3_1, table_3_2, table_4, table_5, table_6_1,
                    previous_period_variance, notes, created_at, updated_at
                )
                VALUES (
                    COALESCE(@Id, gen_random_uuid()), @CompanyId, @Gstin, @ReturnPeriod, @FinancialYear,
                    @Status, @GeneratedAt, @GeneratedBy, @ReviewedAt, @ReviewedBy,
                    @FiledAt, @FiledBy, @Arn, @FilingDate,
                    @Table31Json::jsonb, @Table32Json::jsonb, @Table4Json::jsonb, @Table5Json::jsonb, @Table61Json::jsonb,
                    @PreviousPeriodVarianceJson::jsonb, @Notes, NOW(), NOW()
                )
                RETURNING id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date,
                    table_3_1 as Table31Json, table_3_2 as Table32Json,
                    table_4 as Table4Json, table_5 as Table5Json, table_6_1 as Table61Json,
                    previous_period_variance as PreviousPeriodVarianceJson,
                    notes, created_at, updated_at";

            return await connection.QuerySingleAsync<Gstr3bFiling>(sql, filing);
        }

        public async Task UpdateAsync(Gstr3bFiling filing)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE gstr3b_filings SET
                    gstin = @Gstin,
                    return_period = @ReturnPeriod,
                    financial_year = @FinancialYear,
                    status = @Status,
                    generated_at = @GeneratedAt,
                    generated_by = @GeneratedBy,
                    reviewed_at = @ReviewedAt,
                    reviewed_by = @ReviewedBy,
                    filed_at = @FiledAt,
                    filed_by = @FiledBy,
                    arn = @Arn,
                    filing_date = @FilingDate,
                    table_3_1 = @Table31Json::jsonb,
                    table_3_2 = @Table32Json::jsonb,
                    table_4 = @Table4Json::jsonb,
                    table_5 = @Table5Json::jsonb,
                    table_6_1 = @Table61Json::jsonb,
                    previous_period_variance = @PreviousPeriodVarianceJson::jsonb,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, filing);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM gstr3b_filings WHERE id = @id",
                new { id });
        }

        // ==================== Line Items ====================

        public async Task<IEnumerable<Gstr3bLineItem>> GetLineItemsAsync(Guid filingId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr3bLineItem>(
                @"SELECT id, filing_id, table_code, row_order, description,
                    taxable_value, igst, cgst, sgst, cess,
                    source_count, source_type, source_ids as SourceIdsJson,
                    computation_notes, created_at
                  FROM gstr3b_line_items
                  WHERE filing_id = @filingId
                  ORDER BY table_code, row_order",
                new { filingId });
        }

        public async Task<IEnumerable<Gstr3bLineItem>> GetLineItemsByTableAsync(Guid filingId, string tableCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr3bLineItem>(
                @"SELECT id, filing_id, table_code, row_order, description,
                    taxable_value, igst, cgst, sgst, cess,
                    source_count, source_type, source_ids as SourceIdsJson,
                    computation_notes, created_at
                  FROM gstr3b_line_items
                  WHERE filing_id = @filingId AND table_code LIKE @tableCode
                  ORDER BY row_order",
                new { filingId, tableCode = tableCode + "%" });
        }

        public async Task BulkInsertLineItemsAsync(IEnumerable<Gstr3bLineItem> lineItems)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var item in lineItems)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO gstr3b_line_items (
                        id, filing_id, table_code, row_order, description,
                        taxable_value, igst, cgst, sgst, cess,
                        source_count, source_type, source_ids, computation_notes, created_at
                    )
                    VALUES (
                        COALESCE(@Id, gen_random_uuid()), @FilingId, @TableCode, @RowOrder, @Description,
                        @TaxableValue, @Igst, @Cgst, @Sgst, @Cess,
                        @SourceCount, @SourceType, @SourceIdsJson::jsonb, @ComputationNotes, NOW()
                    )",
                    item);
            }
        }

        public async Task DeleteLineItemsByFilingAsync(Guid filingId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM gstr3b_line_items WHERE filing_id = @filingId",
                new { filingId });
        }

        // ==================== Source Documents ====================

        public async Task<IEnumerable<Gstr3bSourceDocument>> GetSourceDocumentsAsync(Guid lineItemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr3bSourceDocument>(
                @"SELECT id, line_item_id, source_type, source_id, source_number, source_date,
                    taxable_value, igst, cgst, sgst, cess,
                    party_name, party_gstin, created_at
                  FROM gstr3b_source_documents
                  WHERE line_item_id = @lineItemId
                  ORDER BY source_date DESC, source_number",
                new { lineItemId });
        }

        public async Task BulkInsertSourceDocumentsAsync(IEnumerable<Gstr3bSourceDocument> documents)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var doc in documents)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO gstr3b_source_documents (
                        id, line_item_id, source_type, source_id, source_number, source_date,
                        taxable_value, igst, cgst, sgst, cess,
                        party_name, party_gstin, created_at
                    )
                    VALUES (
                        COALESCE(@Id, gen_random_uuid()), @LineItemId, @SourceType, @SourceId, @SourceNumber, @SourceDate,
                        @TaxableValue, @Igst, @Cgst, @Sgst, @Cess,
                        @PartyName, @PartyGstin, NOW()
                    )",
                    doc);
            }
        }

        public async Task DeleteSourceDocumentsByFilingAsync(Guid filingId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"DELETE FROM gstr3b_source_documents
                  WHERE line_item_id IN (SELECT id FROM gstr3b_line_items WHERE filing_id = @filingId)",
                new { filingId });
        }

        // ==================== Status Updates ====================

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE gstr3b_filings SET status = @status, updated_at = NOW() WHERE id = @id",
                new { id, status });
        }

        public async Task MarkAsGeneratedAsync(Guid id, Guid generatedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr3b_filings SET
                    status = 'generated',
                    generated_at = NOW(),
                    generated_by = @generatedBy,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, generatedBy });
        }

        public async Task MarkAsReviewedAsync(Guid id, Guid reviewedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr3b_filings SET
                    status = 'reviewed',
                    reviewed_at = NOW(),
                    reviewed_by = @reviewedBy,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, reviewedBy });
        }

        public async Task MarkAsFiledAsync(Guid id, string arn, DateTime filingDate, Guid filedBy)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr3b_filings SET
                    status = 'filed',
                    arn = @arn,
                    filing_date = @filingDate,
                    filed_at = NOW(),
                    filed_by = @filedBy,
                    updated_at = NOW()
                  WHERE id = @id",
                new { id, arn, filingDate, filedBy });
        }

        // ==================== Table Summary Updates ====================

        public async Task UpdateTableSummaryAsync(Guid id, string tableColumn, string jsonData)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = $"UPDATE gstr3b_filings SET {tableColumn} = @jsonData::jsonb, updated_at = NOW() WHERE id = @id";
            await connection.ExecuteAsync(sql, new { id, jsonData });
        }

        // ==================== Queries ====================

        public async Task<bool> ExistsForPeriodAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<bool>(
                @"SELECT EXISTS(
                    SELECT 1 FROM gstr3b_filings
                    WHERE company_id = @companyId AND return_period = @returnPeriod
                )",
                new { companyId, returnPeriod });
        }

        public async Task<(IEnumerable<Gstr3bFiling> Items, int TotalCount)> GetFilingHistoryAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var whereClauses = new List<string> { "company_id = @companyId" };
            if (!string.IsNullOrEmpty(financialYear))
                whereClauses.Add("financial_year = @financialYear");
            if (!string.IsNullOrEmpty(status))
                whereClauses.Add("status = @status");

            var whereClause = string.Join(" AND ", whereClauses);
            var offset = (pageNumber - 1) * pageSize;

            var dataSql = $@"SELECT id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date, notes, created_at, updated_at
                  FROM gstr3b_filings
                  WHERE {whereClause}
                  ORDER BY return_period DESC
                  LIMIT @pageSize OFFSET @offset";

            var countSql = $"SELECT COUNT(*) FROM gstr3b_filings WHERE {whereClause}";

            var items = await connection.QueryAsync<Gstr3bFiling>(dataSql,
                new { companyId, financialYear, status, pageSize, offset });
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { companyId, financialYear, status });

            return (items, totalCount);
        }

        public async Task<Gstr3bFiling?> GetPreviousPeriodFilingAsync(Guid companyId, string currentPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // Get all filings and find the one just before currentPeriod
            return await connection.QueryFirstOrDefaultAsync<Gstr3bFiling>(
                @"SELECT id, company_id, gstin, return_period, financial_year,
                    status, generated_at, generated_by, reviewed_at, reviewed_by,
                    filed_at, filed_by, arn, filing_date,
                    table_3_1 as Table31Json, table_3_2 as Table32Json,
                    table_4 as Table4Json, table_5 as Table5Json, table_6_1 as Table61Json,
                    notes, created_at, updated_at
                  FROM gstr3b_filings
                  WHERE company_id = @companyId
                    AND return_period < @currentPeriod
                    AND status IN ('generated', 'reviewed', 'filed')
                  ORDER BY return_period DESC
                  LIMIT 1",
                new { companyId, currentPeriod });
        }
    }
}
