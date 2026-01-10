using Core.Entities.Gst;
using Core.Interfaces.Gst;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.Gst
{
    /// <summary>
    /// Repository implementation for GSTR-2B operations
    /// </summary>
    public class Gstr2bRepository : IGstr2bRepository
    {
        private readonly string _connectionString;

        public Gstr2bRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ==================== Imports ====================

        public async Task<Gstr2bImport?> GetImportByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr2bImport>(
                @"SELECT id, company_id AS CompanyId, return_period AS ReturnPeriod, gstin AS Gstin,
                         import_source AS ImportSource, file_name AS FileName, file_hash AS FileHash,
                         import_status AS ImportStatus, error_message AS ErrorMessage,
                         total_invoices AS TotalInvoices, matched_invoices AS MatchedInvoices,
                         unmatched_invoices AS UnmatchedInvoices, partially_matched_invoices AS PartiallyMatchedInvoices,
                         total_itc_igst AS TotalItcIgst, total_itc_cgst AS TotalItcCgst,
                         total_itc_sgst AS TotalItcSgst, total_itc_cess AS TotalItcCess,
                         matched_itc_amount AS MatchedItcAmount, raw_json AS RawJson,
                         imported_by AS ImportedBy, imported_at AS ImportedAt, processed_at AS ProcessedAt,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM gstr2b_imports WHERE id = @id", new { id });
        }

        public async Task<Gstr2bImport?> GetImportByPeriodAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr2bImport>(
                @"SELECT id, company_id AS CompanyId, return_period AS ReturnPeriod, gstin AS Gstin,
                         import_source AS ImportSource, file_name AS FileName, file_hash AS FileHash,
                         import_status AS ImportStatus, error_message AS ErrorMessage,
                         total_invoices AS TotalInvoices, matched_invoices AS MatchedInvoices,
                         unmatched_invoices AS UnmatchedInvoices, partially_matched_invoices AS PartiallyMatchedInvoices,
                         total_itc_igst AS TotalItcIgst, total_itc_cgst AS TotalItcCgst,
                         total_itc_sgst AS TotalItcSgst, total_itc_cess AS TotalItcCess,
                         matched_itc_amount AS MatchedItcAmount,
                         imported_by AS ImportedBy, imported_at AS ImportedAt, processed_at AS ProcessedAt,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM gstr2b_imports
                  WHERE company_id = @companyId AND return_period = @returnPeriod
                  ORDER BY created_at DESC LIMIT 1",
                new { companyId, returnPeriod });
        }

        public async Task<Gstr2bImport?> GetImportByHashAsync(Guid companyId, string returnPeriod, string fileHash)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr2bImport>(
                @"SELECT id, company_id AS CompanyId, return_period AS ReturnPeriod
                  FROM gstr2b_imports
                  WHERE company_id = @companyId AND return_period = @returnPeriod AND file_hash = @fileHash",
                new { companyId, returnPeriod, fileHash });
        }

        public async Task<IEnumerable<Gstr2bImport>> GetImportsByCompanyAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bImport>(
                @"SELECT id, company_id AS CompanyId, return_period AS ReturnPeriod, gstin AS Gstin,
                         import_source AS ImportSource, file_name AS FileName,
                         import_status AS ImportStatus,
                         total_invoices AS TotalInvoices, matched_invoices AS MatchedInvoices,
                         unmatched_invoices AS UnmatchedInvoices,
                         imported_at AS ImportedAt, processed_at AS ProcessedAt
                  FROM gstr2b_imports WHERE company_id = @companyId
                  ORDER BY return_period DESC, created_at DESC",
                new { companyId });
        }

        public async Task<(IEnumerable<Gstr2bImport> Items, int TotalCount)> GetImportsPagedAsync(
            Guid companyId, int pageNumber, int pageSize, string? status = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;

            var whereClause = "WHERE company_id = @companyId";
            if (!string.IsNullOrEmpty(status))
                whereClause += " AND import_status = @status";

            var countSql = $"SELECT COUNT(*) FROM gstr2b_imports {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql, new { companyId, status });

            var sql = $@"SELECT id, company_id AS CompanyId, return_period AS ReturnPeriod, gstin AS Gstin,
                                import_source AS ImportSource, file_name AS FileName,
                                import_status AS ImportStatus,
                                total_invoices AS TotalInvoices, matched_invoices AS MatchedInvoices,
                                unmatched_invoices AS UnmatchedInvoices, partially_matched_invoices AS PartiallyMatchedInvoices,
                                total_itc_igst AS TotalItcIgst, total_itc_cgst AS TotalItcCgst,
                                total_itc_sgst AS TotalItcSgst, total_itc_cess AS TotalItcCess,
                                imported_at AS ImportedAt, processed_at AS ProcessedAt,
                                created_at AS CreatedAt
                         FROM gstr2b_imports {whereClause}
                         ORDER BY return_period DESC, created_at DESC
                         LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<Gstr2bImport>(sql, new { companyId, status, pageSize, offset });
            return (items, totalCount);
        }

        public async Task<Gstr2bImport> AddImportAsync(Gstr2bImport import)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            import.Id = Guid.NewGuid();
            import.CreatedAt = DateTime.UtcNow;
            import.UpdatedAt = DateTime.UtcNow;
            import.ImportedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO gstr2b_imports
                  (id, company_id, return_period, gstin, import_source, file_name, file_hash,
                   import_status, raw_json, imported_by, imported_at, created_at, updated_at)
                  VALUES (@Id, @CompanyId, @ReturnPeriod, @Gstin, @ImportSource, @FileName, @FileHash,
                          @ImportStatus, @RawJson::jsonb, @ImportedBy, @ImportedAt, @CreatedAt, @UpdatedAt)",
                import);
            return import;
        }

        public async Task UpdateImportAsync(Gstr2bImport import)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            import.UpdatedAt = DateTime.UtcNow;
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_imports SET
                  import_status = @ImportStatus, error_message = @ErrorMessage,
                  total_invoices = @TotalInvoices, matched_invoices = @MatchedInvoices,
                  unmatched_invoices = @UnmatchedInvoices, partially_matched_invoices = @PartiallyMatchedInvoices,
                  total_itc_igst = @TotalItcIgst, total_itc_cgst = @TotalItcCgst,
                  total_itc_sgst = @TotalItcSgst, total_itc_cess = @TotalItcCess,
                  matched_itc_amount = @MatchedItcAmount, processed_at = @ProcessedAt,
                  updated_at = @UpdatedAt
                  WHERE id = @Id", import);
        }

        public async Task UpdateImportStatusAsync(Guid importId, string status, string? errorMessage = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_imports SET import_status = @status, error_message = @errorMessage,
                  processed_at = CASE WHEN @status = 'completed' THEN CURRENT_TIMESTAMP ELSE processed_at END,
                  updated_at = CURRENT_TIMESTAMP
                  WHERE id = @importId",
                new { importId, status, errorMessage });
        }

        public async Task UpdateImportSummaryAsync(Guid importId, int total, int matched, int unmatched, int partial)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_imports SET
                  total_invoices = @total, matched_invoices = @matched,
                  unmatched_invoices = @unmatched, partially_matched_invoices = @partial,
                  updated_at = CURRENT_TIMESTAMP
                  WHERE id = @importId",
                new { importId, total, matched, unmatched, partial });
        }

        public async Task DeleteImportAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM gstr2b_imports WHERE id = @id", new { id });
        }

        // ==================== Invoices ====================

        public async Task<Gstr2bInvoice?> GetInvoiceByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr2bInvoice>(
                @"SELECT id, import_id AS ImportId, company_id AS CompanyId, return_period AS ReturnPeriod,
                         supplier_gstin AS SupplierGstin, supplier_name AS SupplierName, supplier_trade_name AS SupplierTradeName,
                         invoice_number AS InvoiceNumber, invoice_date AS InvoiceDate, invoice_type AS InvoiceType,
                         document_type AS DocumentType, taxable_value AS TaxableValue,
                         igst_amount AS IgstAmount, cgst_amount AS CgstAmount, sgst_amount AS SgstAmount, cess_amount AS CessAmount,
                         total_invoice_value AS TotalInvoiceValue,
                         itc_eligible AS ItcEligible, itc_igst AS ItcIgst, itc_cgst AS ItcCgst, itc_sgst AS ItcSgst, itc_cess AS ItcCess,
                         place_of_supply AS PlaceOfSupply, supply_type AS SupplyType, reverse_charge AS ReverseCharge,
                         match_status AS MatchStatus, matched_vendor_invoice_id AS MatchedVendorInvoiceId,
                         match_confidence AS MatchConfidence, match_details AS MatchDetails, match_discrepancies AS MatchDiscrepancies,
                         action_status AS ActionStatus, action_by AS ActionBy, action_at AS ActionAt, action_notes AS ActionNotes,
                         created_at AS CreatedAt, updated_at AS UpdatedAt
                  FROM gstr2b_invoices WHERE id = @id", new { id });
        }

        public async Task<IEnumerable<Gstr2bInvoice>> GetInvoicesByImportAsync(Guid importId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bInvoice>(
                @"SELECT id, import_id AS ImportId, company_id AS CompanyId,
                         supplier_gstin AS SupplierGstin, supplier_name AS SupplierName,
                         invoice_number AS InvoiceNumber, invoice_date AS InvoiceDate, invoice_type AS InvoiceType,
                         taxable_value AS TaxableValue, igst_amount AS IgstAmount, cgst_amount AS CgstAmount,
                         sgst_amount AS SgstAmount, cess_amount AS CessAmount,
                         itc_igst AS ItcIgst, itc_cgst AS ItcCgst, itc_sgst AS ItcSgst, itc_cess AS ItcCess,
                         match_status AS MatchStatus, matched_vendor_invoice_id AS MatchedVendorInvoiceId,
                         match_confidence AS MatchConfidence, action_status AS ActionStatus
                  FROM gstr2b_invoices WHERE import_id = @importId
                  ORDER BY supplier_gstin, invoice_date",
                new { importId });
        }

        public async Task<(IEnumerable<Gstr2bInvoice> Items, int TotalCount)> GetInvoicesPagedAsync(
            Guid importId, int pageNumber, int pageSize,
            string? matchStatus = null, string? invoiceType = null, string? searchTerm = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var offset = (pageNumber - 1) * pageSize;

            var whereClause = "WHERE import_id = @importId";
            if (!string.IsNullOrEmpty(matchStatus))
                whereClause += " AND match_status = @matchStatus";
            if (!string.IsNullOrEmpty(invoiceType))
                whereClause += " AND invoice_type = @invoiceType";
            if (!string.IsNullOrEmpty(searchTerm))
                whereClause += " AND (supplier_gstin ILIKE @search OR supplier_name ILIKE @search OR invoice_number ILIKE @search)";

            var search = $"%{searchTerm}%";

            var countSql = $"SELECT COUNT(*) FROM gstr2b_invoices {whereClause}";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql,
                new { importId, matchStatus, invoiceType, search });

            var sql = $@"SELECT id, import_id AS ImportId, company_id AS CompanyId,
                                supplier_gstin AS SupplierGstin, supplier_name AS SupplierName,
                                invoice_number AS InvoiceNumber, invoice_date AS InvoiceDate, invoice_type AS InvoiceType,
                                taxable_value AS TaxableValue, igst_amount AS IgstAmount, cgst_amount AS CgstAmount,
                                sgst_amount AS SgstAmount, cess_amount AS CessAmount,
                                itc_igst AS ItcIgst, itc_cgst AS ItcCgst, itc_sgst AS ItcSgst, itc_cess AS ItcCess,
                                match_status AS MatchStatus, matched_vendor_invoice_id AS MatchedVendorInvoiceId,
                                match_confidence AS MatchConfidence, action_status AS ActionStatus
                         FROM gstr2b_invoices {whereClause}
                         ORDER BY supplier_gstin, invoice_date
                         LIMIT @pageSize OFFSET @offset";

            var items = await connection.QueryAsync<Gstr2bInvoice>(sql,
                new { importId, matchStatus, invoiceType, search, pageSize, offset });
            return (items, totalCount);
        }

        public async Task<IEnumerable<Gstr2bInvoice>> GetUnmatchedInvoicesAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bInvoice>(
                @"SELECT id, import_id AS ImportId, company_id AS CompanyId,
                         supplier_gstin AS SupplierGstin, supplier_name AS SupplierName,
                         invoice_number AS InvoiceNumber, invoice_date AS InvoiceDate, invoice_type AS InvoiceType,
                         taxable_value AS TaxableValue, igst_amount AS IgstAmount, cgst_amount AS CgstAmount,
                         sgst_amount AS SgstAmount, cess_amount AS CessAmount,
                         itc_igst AS ItcIgst, itc_cgst AS ItcCgst, itc_sgst AS ItcSgst, itc_cess AS ItcCess,
                         match_status AS MatchStatus, action_status AS ActionStatus
                  FROM gstr2b_invoices
                  WHERE company_id = @companyId AND return_period = @returnPeriod
                    AND match_status IN ('unmatched', 'partial_match')
                  ORDER BY supplier_gstin, invoice_date",
                new { companyId, returnPeriod });
        }

        public async Task<IEnumerable<Gstr2bInvoice>> GetInvoicesByMatchStatusAsync(Guid importId, string matchStatus)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bInvoice>(
                @"SELECT id, supplier_gstin AS SupplierGstin, supplier_name AS SupplierName,
                         invoice_number AS InvoiceNumber, invoice_date AS InvoiceDate,
                         taxable_value AS TaxableValue,
                         itc_igst AS ItcIgst, itc_cgst AS ItcCgst, itc_sgst AS ItcSgst, itc_cess AS ItcCess,
                         match_status AS MatchStatus, match_confidence AS MatchConfidence
                  FROM gstr2b_invoices WHERE import_id = @importId AND match_status = @matchStatus",
                new { importId, matchStatus });
        }

        public async Task<Gstr2bInvoice> AddInvoiceAsync(Gstr2bInvoice invoice)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            invoice.Id = Guid.NewGuid();
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await connection.ExecuteAsync(
                @"INSERT INTO gstr2b_invoices
                  (id, import_id, company_id, return_period, supplier_gstin, supplier_name, supplier_trade_name,
                   invoice_number, invoice_date, invoice_type, document_type, taxable_value,
                   igst_amount, cgst_amount, sgst_amount, cess_amount, total_invoice_value,
                   itc_eligible, itc_igst, itc_cgst, itc_sgst, itc_cess,
                   place_of_supply, supply_type, reverse_charge, match_status, raw_json, created_at, updated_at)
                  VALUES (@Id, @ImportId, @CompanyId, @ReturnPeriod, @SupplierGstin, @SupplierName, @SupplierTradeName,
                          @InvoiceNumber, @InvoiceDate, @InvoiceType, @DocumentType, @TaxableValue,
                          @IgstAmount, @CgstAmount, @SgstAmount, @CessAmount, @TotalInvoiceValue,
                          @ItcEligible, @ItcIgst, @ItcCgst, @ItcSgst, @ItcCess,
                          @PlaceOfSupply, @SupplyType, @ReverseCharge, @MatchStatus, @RawJson::jsonb, @CreatedAt, @UpdatedAt)",
                invoice);
            return invoice;
        }

        public async Task BulkInsertInvoicesAsync(IEnumerable<Gstr2bInvoice> invoices)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = await connection.BeginTransactionAsync();
            try
            {
                foreach (var invoice in invoices)
                {
                    invoice.Id = Guid.NewGuid();
                    invoice.CreatedAt = DateTime.UtcNow;
                    invoice.UpdatedAt = DateTime.UtcNow;

                    await connection.ExecuteAsync(
                        @"INSERT INTO gstr2b_invoices
                          (id, import_id, company_id, return_period, supplier_gstin, supplier_name, supplier_trade_name,
                           invoice_number, invoice_date, invoice_type, document_type, taxable_value,
                           igst_amount, cgst_amount, sgst_amount, cess_amount, total_invoice_value,
                           itc_eligible, itc_igst, itc_cgst, itc_sgst, itc_cess,
                           place_of_supply, supply_type, reverse_charge, match_status, raw_json, created_at, updated_at)
                          VALUES (@Id, @ImportId, @CompanyId, @ReturnPeriod, @SupplierGstin, @SupplierName, @SupplierTradeName,
                                  @InvoiceNumber, @InvoiceDate, @InvoiceType, @DocumentType, @TaxableValue,
                                  @IgstAmount, @CgstAmount, @SgstAmount, @CessAmount, @TotalInvoiceValue,
                                  @ItcEligible, @ItcIgst, @ItcCgst, @ItcSgst, @ItcCess,
                                  @PlaceOfSupply, @SupplyType, @ReverseCharge, @MatchStatus, @RawJson::jsonb, @CreatedAt, @UpdatedAt)",
                        invoice, transaction);
                }
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task UpdateInvoiceAsync(Gstr2bInvoice invoice)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            invoice.UpdatedAt = DateTime.UtcNow;
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_invoices SET
                  match_status = @MatchStatus, matched_vendor_invoice_id = @MatchedVendorInvoiceId,
                  match_confidence = @MatchConfidence, match_details = @MatchDetails::jsonb,
                  match_discrepancies = @MatchDiscrepancies::jsonb,
                  action_status = @ActionStatus, action_by = @ActionBy, action_at = @ActionAt, action_notes = @ActionNotes,
                  updated_at = @UpdatedAt
                  WHERE id = @Id", invoice);
        }

        public async Task UpdateInvoiceMatchAsync(Guid invoiceId, string matchStatus, Guid? matchedVendorInvoiceId,
            int? matchConfidence, string? matchDetails, string? matchDiscrepancies)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_invoices SET
                  match_status = @matchStatus, matched_vendor_invoice_id = @matchedVendorInvoiceId,
                  match_confidence = @matchConfidence, match_details = @matchDetails::jsonb,
                  match_discrepancies = @matchDiscrepancies::jsonb, updated_at = CURRENT_TIMESTAMP
                  WHERE id = @invoiceId",
                new { invoiceId, matchStatus, matchedVendorInvoiceId, matchConfidence, matchDetails, matchDiscrepancies });
        }

        public async Task UpdateInvoiceActionAsync(Guid invoiceId, string actionStatus, Guid userId, string? notes)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE gstr2b_invoices SET
                  action_status = @actionStatus, action_by = @userId, action_at = CURRENT_TIMESTAMP,
                  action_notes = @notes, updated_at = CURRENT_TIMESTAMP
                  WHERE id = @invoiceId",
                new { invoiceId, actionStatus, userId, notes });
        }

        public async Task DeleteInvoicesByImportAsync(Guid importId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM gstr2b_invoices WHERE import_id = @importId", new { importId });
        }

        // ==================== Reconciliation Rules ====================

        public async Task<IEnumerable<Gstr2bReconciliationRule>> GetReconciliationRulesAsync(Guid? companyId = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bReconciliationRule>(
                @"SELECT id, company_id AS CompanyId, rule_name AS RuleName, rule_code AS RuleCode,
                         priority AS Priority, is_active AS IsActive,
                         match_gstin AS MatchGstin, match_invoice_number AS MatchInvoiceNumber,
                         match_invoice_date AS MatchInvoiceDate, match_amount AS MatchAmount,
                         invoice_number_fuzzy_threshold AS InvoiceNumberFuzzyThreshold,
                         date_tolerance_days AS DateToleranceDays, amount_tolerance_percentage AS AmountTolerancePercentage,
                         amount_tolerance_absolute AS AmountToleranceAbsolute,
                         confidence_score AS ConfidenceScore, description AS Description
                  FROM gstr2b_reconciliation_rules
                  WHERE is_active = true AND (company_id IS NULL OR company_id = @companyId)
                  ORDER BY priority",
                new { companyId });
        }

        public async Task<Gstr2bReconciliationRule?> GetReconciliationRuleByCodeAsync(string ruleCode)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<Gstr2bReconciliationRule>(
                @"SELECT id, rule_name AS RuleName, rule_code AS RuleCode, confidence_score AS ConfidenceScore
                  FROM gstr2b_reconciliation_rules WHERE rule_code = @ruleCode",
                new { ruleCode });
        }

        // ==================== Summary Queries ====================

        public async Task<Gstr2bReconciliationSummary> GetReconciliationSummaryAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var summary = await connection.QueryFirstOrDefaultAsync<Gstr2bReconciliationSummary>(
                @"SELECT
                    @returnPeriod AS ReturnPeriod,
                    COUNT(*) AS TotalInvoices,
                    COUNT(*) FILTER (WHERE match_status = 'matched') AS MatchedInvoices,
                    COUNT(*) FILTER (WHERE match_status = 'partial_match') AS PartialMatchInvoices,
                    COUNT(*) FILTER (WHERE match_status = 'unmatched') AS UnmatchedInvoices,
                    COUNT(*) FILTER (WHERE action_status = 'accepted') AS AcceptedInvoices,
                    COUNT(*) FILTER (WHERE action_status = 'rejected') AS RejectedInvoices,
                    COUNT(*) FILTER (WHERE action_status = 'pending_review') AS PendingReviewInvoices,
                    COALESCE(SUM(taxable_value), 0) AS TotalTaxableValue,
                    COALESCE(SUM(taxable_value) FILTER (WHERE match_status = 'matched'), 0) AS MatchedTaxableValue,
                    COALESCE(SUM(taxable_value) FILTER (WHERE match_status = 'unmatched'), 0) AS UnmatchedTaxableValue,
                    COALESCE(SUM(itc_igst + itc_cgst + itc_sgst + itc_cess), 0) AS TotalItcAvailable,
                    COALESCE(SUM(itc_igst + itc_cgst + itc_sgst + itc_cess) FILTER (WHERE match_status = 'matched'), 0) AS MatchedItc,
                    COALESCE(SUM(itc_igst + itc_cgst + itc_sgst + itc_cess) FILTER (WHERE match_status = 'unmatched'), 0) AS UnmatchedItc
                  FROM gstr2b_invoices
                  WHERE company_id = @companyId AND return_period = @returnPeriod",
                new { companyId, returnPeriod });

            return summary ?? new Gstr2bReconciliationSummary { ReturnPeriod = returnPeriod };
        }

        public async Task<IEnumerable<Gstr2bSupplierSummary>> GetSupplierWiseSummaryAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<Gstr2bSupplierSummary>(
                @"SELECT
                    supplier_gstin AS SupplierGstin,
                    MAX(supplier_name) AS SupplierName,
                    COUNT(*) AS InvoiceCount,
                    COUNT(*) FILTER (WHERE match_status = 'matched') AS MatchedCount,
                    COUNT(*) FILTER (WHERE match_status = 'unmatched') AS UnmatchedCount,
                    COALESCE(SUM(taxable_value), 0) AS TotalTaxableValue,
                    COALESCE(SUM(itc_igst + itc_cgst + itc_sgst + itc_cess), 0) AS TotalItc
                  FROM gstr2b_invoices
                  WHERE company_id = @companyId AND return_period = @returnPeriod
                  GROUP BY supplier_gstin
                  ORDER BY TotalTaxableValue DESC",
                new { companyId, returnPeriod });
        }

        public async Task<Gstr2bItcSummary> GetItcSummaryAsync(Guid companyId, string returnPeriod)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var summary = await connection.QueryFirstOrDefaultAsync<Gstr2bItcSummary>(
                @"SELECT
                    @returnPeriod AS ReturnPeriod,
                    COALESCE(SUM(itc_igst), 0) AS Gstr2bItcIgst,
                    COALESCE(SUM(itc_cgst), 0) AS Gstr2bItcCgst,
                    COALESCE(SUM(itc_sgst), 0) AS Gstr2bItcSgst,
                    COALESCE(SUM(itc_cess), 0) AS Gstr2bItcCess,
                    COALESCE(SUM(itc_igst + itc_cgst + itc_sgst + itc_cess), 0) AS Gstr2bItcTotal
                  FROM gstr2b_invoices
                  WHERE company_id = @companyId AND return_period = @returnPeriod",
                new { companyId, returnPeriod });

            return summary ?? new Gstr2bItcSummary { ReturnPeriod = returnPeriod };
        }
    }
}
