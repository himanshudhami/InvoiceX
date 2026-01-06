using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;
using Infrastructure.Data.Common;

namespace Infrastructure.Data
{
    public class VendorInvoicesRepository : IVendorInvoicesRepository
    {
        private readonly string _connectionString;

        public VendorInvoicesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<VendorInvoice?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices WHERE id = @id",
                new { id });
        }

        public async Task<VendorInvoice?> GetByIdWithItemsAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var sql = @"
                SELECT * FROM vendor_invoices WHERE id = @id;
                SELECT * FROM vendor_invoice_items WHERE vendor_invoice_id = @id ORDER BY sort_order;";

            using var multi = await connection.QueryMultipleAsync(sql, new { id });
            var invoice = await multi.ReadFirstOrDefaultAsync<VendorInvoice>();
            if (invoice != null)
            {
                invoice.Items = (await multi.ReadAsync<VendorInvoiceItem>()).ToList();
            }
            return invoice;
        }

        public async Task<IEnumerable<VendorInvoice>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices ORDER BY invoice_date DESC");
        }

        public async Task<IEnumerable<VendorInvoice>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices WHERE company_id = @companyId ORDER BY invoice_date DESC",
                new { companyId });
        }

        public async Task<IEnumerable<VendorInvoice>> GetByVendorIdAsync(Guid vendorId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices WHERE vendor_id = @vendorId ORDER BY invoice_date DESC",
                new { vendorId });
        }

        public async Task<(IEnumerable<VendorInvoice> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var allowedColumns = new[] {
                "id", "company_id", "vendor_id", "invoice_number", "internal_reference",
                "invoice_date", "due_date", "received_date", "status",
                "subtotal", "tax_amount", "discount_amount", "total_amount", "paid_amount",
                "currency", "invoice_type", "supply_type", "reverse_charge", "rcm_applicable",
                "total_cgst", "total_sgst", "total_igst", "total_cess",
                "itc_eligible", "itc_claimed_amount", "matched_with_gstr2b",
                "tds_applicable", "tds_section", "tds_rate", "tds_amount",
                "is_posted", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("vendor_invoices", allowedColumns)
                .SearchAcross(new[] { "invoice_number", "internal_reference", "po_number", "notes" }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!) ? sortBy! : "invoice_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<VendorInvoice>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<IEnumerable<VendorInvoice>> GetPendingApprovalAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices WHERE company_id = @companyId AND status = 'pending_approval' ORDER BY invoice_date",
                new { companyId });
        }

        public async Task<IEnumerable<VendorInvoice>> GetUnpaidAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                @"SELECT * FROM vendor_invoices
                WHERE company_id = @companyId
                AND status NOT IN ('draft', 'cancelled', 'paid')
                AND (total_amount - COALESCE(paid_amount, 0)) > 0.01
                ORDER BY due_date",
                new { companyId });
        }

        public async Task<IEnumerable<VendorInvoice>> GetOverdueAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                @"SELECT * FROM vendor_invoices
                WHERE company_id = @companyId
                AND status NOT IN ('draft', 'cancelled', 'paid')
                AND due_date < CURRENT_DATE
                AND (total_amount - COALESCE(paid_amount, 0)) > 0.01
                ORDER BY due_date",
                new { companyId });
        }

        public async Task<IEnumerable<VendorInvoice>> GetItcEligibleAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                @"SELECT * FROM vendor_invoices
                WHERE company_id = @companyId
                AND itc_eligible = TRUE
                AND status NOT IN ('draft', 'cancelled')
                ORDER BY invoice_date DESC",
                new { companyId });
        }

        public async Task<IEnumerable<VendorInvoice>> GetUnmatchedWithGstr2BAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoice>(
                @"SELECT * FROM vendor_invoices
                WHERE company_id = @companyId
                AND itc_eligible = TRUE
                AND matched_with_gstr2b = FALSE
                AND status NOT IN ('draft', 'cancelled')
                ORDER BY invoice_date DESC",
                new { companyId });
        }

        public async Task<VendorInvoice?> GetByTallyGuidAsync(string tallyVoucherGuid)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorInvoice>(
                "SELECT * FROM vendor_invoices WHERE tally_voucher_guid = @tallyVoucherGuid",
                new { tallyVoucherGuid });
        }

        public async Task<VendorInvoice> AddAsync(VendorInvoice entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendor_invoices (
                company_id, vendor_id, invoice_number, internal_reference,
                invoice_date, due_date, received_date, status,
                subtotal, tax_amount, discount_amount, total_amount, paid_amount,
                currency, notes, po_number,
                invoice_type, supply_type, place_of_supply, reverse_charge, rcm_applicable,
                total_cgst, total_sgst, total_igst, total_cess,
                itc_eligible, itc_claimed_amount, itc_ineligible_reason,
                matched_with_gstr2b, gstr2b_period,
                tds_applicable, tds_section, tds_rate, tds_amount,
                bill_of_entry_number, bill_of_entry_date, port_code,
                foreign_currency_amount, foreign_currency, exchange_rate,
                is_posted, posted_journal_id, posted_at, expense_account_id,
                approved_by, approved_at, approval_notes,
                tally_voucher_guid, tally_voucher_number, tally_migration_batch_id,
                created_at, updated_at
            ) VALUES (
                @CompanyId, @VendorId, @InvoiceNumber, @InternalReference,
                @InvoiceDate, @DueDate, @ReceivedDate, @Status,
                @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @PaidAmount,
                @Currency, @Notes, @PoNumber,
                @InvoiceType, @SupplyType, @PlaceOfSupply, @ReverseCharge, @RcmApplicable,
                @TotalCgst, @TotalSgst, @TotalIgst, @TotalCess,
                @ItcEligible, @ItcClaimedAmount, @ItcIneligibleReason,
                @MatchedWithGstr2B, @Gstr2BPeriod,
                @TdsApplicable, @TdsSection, @TdsRate, @TdsAmount,
                @BillOfEntryNumber, @BillOfEntryDate, @PortCode,
                @ForeignCurrencyAmount, @ForeignCurrency, @ExchangeRate,
                @IsPosted, @PostedJournalId, @PostedAt, @ExpenseAccountId,
                @ApprovedBy, @ApprovedAt, @ApprovalNotes,
                @TallyVoucherGuid, @TallyVoucherNumber, @TallyMigrationBatchId,
                NOW(), NOW()
            ) RETURNING *";

            return await connection.QuerySingleAsync<VendorInvoice>(sql, entity);
        }

        public async Task UpdateAsync(VendorInvoice entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE vendor_invoices SET
                company_id = @CompanyId,
                vendor_id = @VendorId,
                invoice_number = @InvoiceNumber,
                internal_reference = @InternalReference,
                invoice_date = @InvoiceDate,
                due_date = @DueDate,
                received_date = @ReceivedDate,
                status = @Status,
                subtotal = @Subtotal,
                tax_amount = @TaxAmount,
                discount_amount = @DiscountAmount,
                total_amount = @TotalAmount,
                paid_amount = @PaidAmount,
                currency = @Currency,
                notes = @Notes,
                po_number = @PoNumber,
                invoice_type = @InvoiceType,
                supply_type = @SupplyType,
                place_of_supply = @PlaceOfSupply,
                reverse_charge = @ReverseCharge,
                rcm_applicable = @RcmApplicable,
                total_cgst = @TotalCgst,
                total_sgst = @TotalSgst,
                total_igst = @TotalIgst,
                total_cess = @TotalCess,
                itc_eligible = @ItcEligible,
                itc_claimed_amount = @ItcClaimedAmount,
                itc_ineligible_reason = @ItcIneligibleReason,
                matched_with_gstr2b = @MatchedWithGstr2B,
                gstr2b_period = @Gstr2BPeriod,
                tds_applicable = @TdsApplicable,
                tds_section = @TdsSection,
                tds_rate = @TdsRate,
                tds_amount = @TdsAmount,
                bill_of_entry_number = @BillOfEntryNumber,
                bill_of_entry_date = @BillOfEntryDate,
                port_code = @PortCode,
                foreign_currency_amount = @ForeignCurrencyAmount,
                foreign_currency = @ForeignCurrency,
                exchange_rate = @ExchangeRate,
                is_posted = @IsPosted,
                posted_journal_id = @PostedJournalId,
                posted_at = @PostedAt,
                expense_account_id = @ExpenseAccountId,
                approved_by = @ApprovedBy,
                approved_at = @ApprovedAt,
                approval_notes = @ApprovalNotes,
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
            await connection.ExecuteAsync("DELETE FROM vendor_invoices WHERE id = @id", new { id });
        }

        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE vendor_invoices SET status = @status, updated_at = NOW() WHERE id = @id",
                new { id, status });
        }

        public async Task MarkAsPostedAsync(Guid id, Guid journalId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE vendor_invoices SET
                    is_posted = TRUE,
                    posted_journal_id = @journalId,
                    posted_at = NOW(),
                    updated_at = NOW()
                WHERE id = @id",
                new { id, journalId });
        }

        // Item methods
        public async Task<IEnumerable<VendorInvoiceItem>> GetItemsAsync(Guid vendorInvoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorInvoiceItem>(
                "SELECT * FROM vendor_invoice_items WHERE vendor_invoice_id = @vendorInvoiceId ORDER BY sort_order",
                new { vendorInvoiceId });
        }

        public async Task<VendorInvoiceItem> AddItemAsync(VendorInvoiceItem item)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendor_invoice_items (
                vendor_invoice_id, product_id, description, quantity, unit_price,
                tax_rate, discount_rate, line_total, sort_order,
                hsn_sac_code, is_service,
                cgst_rate, cgst_amount, sgst_rate, sgst_amount,
                igst_rate, igst_amount, cess_rate, cess_amount,
                itc_eligible, itc_category, itc_ineligible_reason,
                expense_account_id, cost_center_id,
                created_at, updated_at
            ) VALUES (
                @VendorInvoiceId, @ProductId, @Description, @Quantity, @UnitPrice,
                @TaxRate, @DiscountRate, @LineTotal, @SortOrder,
                @HsnSacCode, @IsService,
                @CgstRate, @CgstAmount, @SgstRate, @SgstAmount,
                @IgstRate, @IgstAmount, @CessRate, @CessAmount,
                @ItcEligible, @ItcCategory, @ItcIneligibleReason,
                @ExpenseAccountId, @CostCenterId,
                NOW(), NOW()
            ) RETURNING *";

            return await connection.QuerySingleAsync<VendorInvoiceItem>(sql, item);
        }

        public async Task UpdateItemAsync(VendorInvoiceItem item)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE vendor_invoice_items SET
                product_id = @ProductId,
                description = @Description,
                quantity = @Quantity,
                unit_price = @UnitPrice,
                tax_rate = @TaxRate,
                discount_rate = @DiscountRate,
                line_total = @LineTotal,
                sort_order = @SortOrder,
                hsn_sac_code = @HsnSacCode,
                is_service = @IsService,
                cgst_rate = @CgstRate,
                cgst_amount = @CgstAmount,
                sgst_rate = @SgstRate,
                sgst_amount = @SgstAmount,
                igst_rate = @IgstRate,
                igst_amount = @IgstAmount,
                cess_rate = @CessRate,
                cess_amount = @CessAmount,
                itc_eligible = @ItcEligible,
                itc_category = @ItcCategory,
                itc_ineligible_reason = @ItcIneligibleReason,
                expense_account_id = @ExpenseAccountId,
                cost_center_id = @CostCenterId,
                updated_at = NOW()
            WHERE id = @Id";
            await connection.ExecuteAsync(sql, item);
        }

        public async Task DeleteItemAsync(Guid itemId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM vendor_invoice_items WHERE id = @itemId", new { itemId });
        }

        public async Task DeleteItemsByInvoiceIdAsync(Guid vendorInvoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM vendor_invoice_items WHERE vendor_invoice_id = @vendorInvoiceId",
                new { vendorInvoiceId });
        }

        public async Task<IEnumerable<VendorInvoice>> BulkAddAsync(IEnumerable<VendorInvoice> entities)
        {
            var results = new List<VendorInvoice>();
            foreach (var entity in entities)
            {
                results.Add(await AddAsync(entity));
            }
            return results;
        }
    }
}
