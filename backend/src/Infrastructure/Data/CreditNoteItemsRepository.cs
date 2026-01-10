using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    public class CreditNoteItemsRepository : ICreditNoteItemsRepository
    {
        private readonly string _connectionString;

        public CreditNoteItemsRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CreditNoteItems?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<CreditNoteItems>(
                "SELECT * FROM credit_note_items WHERE id = @id",
                new { id });
        }

        public async Task<IEnumerable<CreditNoteItems>> GetByCreditNoteIdAsync(Guid creditNoteId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<CreditNoteItems>(
                "SELECT * FROM credit_note_items WHERE credit_note_id = @creditNoteId ORDER BY sort_order, id",
                new { creditNoteId });
        }

        public async Task<CreditNoteItems> AddAsync(CreditNoteItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO credit_note_items
                (credit_note_id, original_invoice_item_id, product_id,
                 description, quantity, unit_price, tax_rate, discount_rate, line_total, sort_order,
                 hsn_sac_code, is_service,
                 cgst_rate, cgst_amount, sgst_rate, sgst_amount,
                 igst_rate, igst_amount, cess_rate, cess_amount,
                 created_at, updated_at)
                VALUES
                (@CreditNoteId, @OriginalInvoiceItemId, @ProductId,
                 @Description, @Quantity, @UnitPrice, @TaxRate, @DiscountRate, @LineTotal, @SortOrder,
                 @HsnSacCode, @IsService,
                 @CgstRate, @CgstAmount, @SgstRate, @SgstAmount,
                 @IgstRate, @IgstAmount, @CessRate, @CessAmount,
                 NOW(), NOW())
                RETURNING *";

            return await connection.QuerySingleAsync<CreditNoteItems>(sql, entity);
        }

        public async Task UpdateAsync(CreditNoteItems entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"UPDATE credit_note_items SET
                credit_note_id = @CreditNoteId,
                original_invoice_item_id = @OriginalInvoiceItemId,
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
                updated_at = NOW()
                WHERE id = @Id";
            await connection.ExecuteAsync(sql, entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM credit_note_items WHERE id = @id",
                new { id });
        }

        public async Task DeleteByCreditNoteIdAsync(Guid creditNoteId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM credit_note_items WHERE credit_note_id = @creditNoteId",
                new { creditNoteId });
        }
    }
}
