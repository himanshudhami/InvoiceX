using Core.Entities;
using Core.Interfaces;
using Dapper;
using Npgsql;

namespace Infrastructure.Data
{
    public class VendorPaymentAllocationRepository : IVendorPaymentAllocationRepository
    {
        private readonly string _connectionString;

        public VendorPaymentAllocationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<VendorPaymentAllocation?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<VendorPaymentAllocation>(
                "SELECT * FROM vendor_payment_allocations WHERE id = @id",
                new { id });
        }

        public async Task<VendorPaymentAllocation> AddAsync(VendorPaymentAllocation entity)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"INSERT INTO vendor_payment_allocations (
                    id, company_id, vendor_payment_id, vendor_invoice_id,
                    allocated_amount, tds_allocated, currency, amount_in_inr, exchange_rate,
                    allocation_date, allocation_type, tally_bill_ref,
                    notes, created_by, created_at, updated_at
                )
                VALUES (
                    @Id, @CompanyId, @VendorPaymentId, @VendorInvoiceId,
                    @AllocatedAmount, @TdsAllocated, @Currency, @AmountInInr, @ExchangeRate,
                    @AllocationDate, @AllocationType, @TallyBillRef,
                    @Notes, @CreatedBy, NOW(), NOW()
                )
                RETURNING *";

            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            return await connection.QuerySingleAsync<VendorPaymentAllocation>(sql, entity);
        }

        public async Task<IEnumerable<VendorPaymentAllocation>> AddBulkAsync(IEnumerable<VendorPaymentAllocation> allocations)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var results = new List<VendorPaymentAllocation>();
                var sql = @"INSERT INTO vendor_payment_allocations (
                        id, company_id, vendor_payment_id, vendor_invoice_id,
                        allocated_amount, tds_allocated, currency, amount_in_inr, exchange_rate,
                        allocation_date, allocation_type, tally_bill_ref,
                        notes, created_by, created_at, updated_at
                    )
                    VALUES (
                        @Id, @CompanyId, @VendorPaymentId, @VendorInvoiceId,
                        @AllocatedAmount, @TdsAllocated, @Currency, @AmountInInr, @ExchangeRate,
                        @AllocationDate, @AllocationType, @TallyBillRef,
                        @Notes, @CreatedBy, NOW(), NOW()
                    )
                    RETURNING *";

                foreach (var allocation in allocations)
                {
                    if (allocation.Id == Guid.Empty)
                        allocation.Id = Guid.NewGuid();

                    var created = await connection.QuerySingleAsync<VendorPaymentAllocation>(sql, allocation, transaction);
                    results.Add(created);
                }

                await transaction.CommitAsync();
                return results;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<IEnumerable<VendorPaymentAllocation>> GetByPaymentIdAsync(Guid vendorPaymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPaymentAllocation>(
                "SELECT * FROM vendor_payment_allocations WHERE vendor_payment_id = @vendorPaymentId",
                new { vendorPaymentId });
        }

        public async Task<IEnumerable<VendorPaymentAllocation>> GetByInvoiceIdAsync(Guid vendorInvoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<VendorPaymentAllocation>(
                "SELECT * FROM vendor_payment_allocations WHERE vendor_invoice_id = @vendorInvoiceId",
                new { vendorInvoiceId });
        }

        public async Task<decimal> GetTotalAllocatedForInvoiceAsync(Guid vendorInvoiceId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(allocated_amount), 0) FROM vendor_payment_allocations WHERE vendor_invoice_id = @vendorInvoiceId",
                new { vendorInvoiceId });
        }

        public async Task DeleteByPaymentIdAsync(Guid vendorPaymentId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM vendor_payment_allocations WHERE vendor_payment_id = @vendorPaymentId",
                new { vendorPaymentId });
        }
    }
}
