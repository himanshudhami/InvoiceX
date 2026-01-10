using Core.Entities;
using Core.Interfaces;
using Dapper;
using Infrastructure.Data.Common;
using Npgsql;

namespace Infrastructure.Data
{
    public class CreditNotesRepository : ICreditNotesRepository
    {
        private readonly string _connectionString;

        public CreditNotesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CreditNotes?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CreditNotes>(
                "SELECT * FROM credit_notes WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<CreditNotes>> GetAllAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CreditNotes>(
                "SELECT * FROM credit_notes ORDER BY credit_note_date DESC");
        }

        public async Task<(IEnumerable<CreditNotes> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            var allowedColumns = new[] {
                "id", "company_id", "party_id", "credit_note_number", "credit_note_date",
                "original_invoice_id", "original_invoice_number", "original_invoice_date",
                "reason", "status", "subtotal", "tax_amount", "discount_amount",
                "total_amount", "currency", "created_at", "updated_at"
            };

            var builder = SqlQueryBuilder
                .From("credit_notes", allowedColumns)
                .SearchAcross(new string[] {
                    "credit_note_number", "original_invoice_number", "reason", "status", "notes"
                }, searchTerm)
                .ApplyFilters(filters)
                .Paginate(pageNumber, pageSize);

            var allowedSet = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
            var orderBy = !string.IsNullOrWhiteSpace(sortBy) && allowedSet.Contains(sortBy!)
                ? sortBy! : "credit_note_date";
            builder.OrderBy(orderBy, sortDescending);

            var (dataSql, parameters) = builder.BuildSelect();
            var (countSql, _) = builder.BuildCount();

            using var multi = await connection.QueryMultipleAsync(dataSql + ";" + countSql, parameters);
            var items = await multi.ReadAsync<CreditNotes>();
            var totalCount = await multi.ReadSingleAsync<int>();
            return (items, totalCount);
        }

        public async Task<CreditNotes> AddAsync(CreditNotes entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO credit_notes
                (company_id, party_id, credit_note_number, credit_note_date,
                 original_invoice_id, original_invoice_number, original_invoice_date,
                 reason, reason_description, status,
                 subtotal, tax_amount, discount_amount, total_amount, currency,
                 notes, terms, issued_at, cancelled_at,
                 invoice_type, supply_type, place_of_supply, reverse_charge,
                 total_cgst, total_sgst, total_igst, total_cess,
                 e_invoice_applicable, irn, irn_generated_at, irn_cancelled_at,
                 qr_code_data, e_invoice_signed_json, e_invoice_status,
                 foreign_currency, exchange_rate, amount_in_inr,
                 itc_reversal_required, itc_reversal_confirmed, itc_reversal_date, itc_reversal_certificate,
                 reported_in_gstr1, gstr1_period, gstr1_filing_date,
                 created_at, updated_at)
                VALUES
                (@CompanyId, @PartyId, @CreditNoteNumber, @CreditNoteDate,
                 @OriginalInvoiceId, @OriginalInvoiceNumber, @OriginalInvoiceDate,
                 @Reason, @ReasonDescription, @Status,
                 @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @Currency,
                 @Notes, @Terms, @IssuedAt, @CancelledAt,
                 @InvoiceType, @SupplyType, @PlaceOfSupply, @ReverseCharge,
                 @TotalCgst, @TotalSgst, @TotalIgst, @TotalCess,
                 @EInvoiceApplicable, @Irn, @IrnGeneratedAt, @IrnCancelledAt,
                 @QrCodeData, @EInvoiceSignedJson::jsonb, @EInvoiceStatus,
                 @ForeignCurrency, @ExchangeRate, @AmountInInr,
                 @ItcReversalRequired, @ItcReversalConfirmed, @ItcReversalDate, @ItcReversalCertificate,
                 @ReportedInGstr1, @Gstr1Period, @Gstr1FilingDate,
                 NOW(), NOW())
                RETURNING *";

            var createdEntity = await connection.QuerySingleAsync<CreditNotes>(sql, entity);
            return createdEntity;
        }

        public async Task UpdateAsync(CreditNotes entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE credit_notes SET
                company_id = @CompanyId,
                party_id = @PartyId,
                credit_note_number = @CreditNoteNumber,
                credit_note_date = @CreditNoteDate,
                original_invoice_id = @OriginalInvoiceId,
                original_invoice_number = @OriginalInvoiceNumber,
                original_invoice_date = @OriginalInvoiceDate,
                reason = @Reason,
                reason_description = @ReasonDescription,
                status = @Status,
                subtotal = @Subtotal,
                tax_amount = @TaxAmount,
                discount_amount = @DiscountAmount,
                total_amount = @TotalAmount,
                currency = @Currency,
                notes = @Notes,
                terms = @Terms,
                issued_at = @IssuedAt,
                cancelled_at = @CancelledAt,
                invoice_type = @InvoiceType,
                supply_type = @SupplyType,
                place_of_supply = @PlaceOfSupply,
                reverse_charge = @ReverseCharge,
                total_cgst = @TotalCgst,
                total_sgst = @TotalSgst,
                total_igst = @TotalIgst,
                total_cess = @TotalCess,
                e_invoice_applicable = @EInvoiceApplicable,
                irn = @Irn,
                irn_generated_at = @IrnGeneratedAt,
                irn_cancelled_at = @IrnCancelledAt,
                qr_code_data = @QrCodeData,
                e_invoice_signed_json = @EInvoiceSignedJson::jsonb,
                e_invoice_status = @EInvoiceStatus,
                foreign_currency = @ForeignCurrency,
                exchange_rate = @ExchangeRate,
                amount_in_inr = @AmountInInr,
                itc_reversal_required = @ItcReversalRequired,
                itc_reversal_confirmed = @ItcReversalConfirmed,
                itc_reversal_date = @ItcReversalDate,
                itc_reversal_certificate = @ItcReversalCertificate,
                reported_in_gstr1 = @ReportedInGstr1,
                gstr1_period = @Gstr1Period,
                gstr1_filing_date = @Gstr1FilingDate,
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM credit_notes WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<CreditNotes>> GetByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CreditNotes>(
                "SELECT * FROM credit_notes WHERE company_id = @companyId ORDER BY credit_note_date DESC",
                new { companyId });
        }

        public async Task<IEnumerable<CreditNotes>> GetByInvoiceIdAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CreditNotes>(
                "SELECT * FROM credit_notes WHERE original_invoice_id = @invoiceId ORDER BY credit_note_date DESC",
                new { invoiceId });
        }

        public async Task<CreditNotes?> GetByNumberAsync(Guid companyId, string creditNoteNumber)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CreditNotes>(
                "SELECT * FROM credit_notes WHERE company_id = @companyId AND credit_note_number = @creditNoteNumber",
                new { companyId, creditNoteNumber });
        }

        public async Task<string> GenerateNextNumberAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            // Get the current fiscal year (April to March in India)
            var today = DateTime.Today;
            var fiscalYearStart = today.Month >= 4
                ? new DateTime(today.Year, 4, 1)
                : new DateTime(today.Year - 1, 4, 1);
            var fiscalYearEnd = fiscalYearStart.AddYears(1).AddDays(-1);

            var fyLabel = $"FY{fiscalYearStart.Year % 100:D2}-{(fiscalYearStart.Year + 1) % 100:D2}";

            // Get the last credit note number for this company and fiscal year
            var lastNumber = await connection.QueryFirstOrDefaultAsync<string>(@"
                SELECT credit_note_number FROM credit_notes
                WHERE company_id = @companyId
                AND credit_note_date >= @fiscalYearStart
                AND credit_note_date <= @fiscalYearEnd
                ORDER BY credit_note_number DESC
                LIMIT 1",
                new { companyId, fiscalYearStart = DateOnly.FromDateTime(fiscalYearStart), fiscalYearEnd = DateOnly.FromDateTime(fiscalYearEnd) });

            var nextSequence = 1;
            if (!string.IsNullOrEmpty(lastNumber))
            {
                // Try to extract sequence number from format CN-FYXX-XX-NNN
                var parts = lastNumber.Split('-');
                if (parts.Length >= 4 && int.TryParse(parts[^1], out var lastSeq))
                {
                    nextSequence = lastSeq + 1;
                }
            }

            return $"CN-{fyLabel}-{nextSequence:D3}";
        }

        public async Task<decimal> GetTotalCreditedAmountForInvoiceAsync(Guid invoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<decimal>(@"
                SELECT COALESCE(SUM(total_amount), 0)
                FROM credit_notes
                WHERE original_invoice_id = @invoiceId
                AND status != 'cancelled'",
                new { invoiceId });
        }
    }
}
