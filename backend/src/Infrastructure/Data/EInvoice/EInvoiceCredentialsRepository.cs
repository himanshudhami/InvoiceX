using Core.Entities.EInvoice;
using Core.Interfaces.EInvoice;
using Dapper;
using Npgsql;

namespace Infrastructure.Data.EInvoice
{
    public class EInvoiceCredentialsRepository : IEInvoiceCredentialsRepository
    {
        private readonly string _connectionString;

        public EInvoiceCredentialsRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<EInvoiceCredentials?> GetByIdAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EInvoiceCredentials>(
                @"SELECT id, company_id AS CompanyId, gsp_provider AS GspProvider,
                         environment AS Environment, client_id AS ClientId,
                         client_secret AS ClientSecret, username AS Username,
                         password AS Password, auth_token AS AuthToken,
                         token_expiry AS TokenExpiry, sek AS Sek,
                         auto_generate_irn AS AutoGenerateIrn,
                         auto_cancel_on_void AS AutoCancelOnVoid,
                         generate_eway_bill AS GenerateEwayBill,
                         einvoice_threshold AS EinvoiceThreshold,
                         is_active AS IsActive, created_at AS CreatedAt,
                         updated_at AS UpdatedAt
                  FROM einvoice_credentials WHERE id = @Id",
                new { Id = id });
        }

        public async Task<EInvoiceCredentials?> GetByCompanyIdAsync(Guid companyId, string environment = "production")
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<EInvoiceCredentials>(
                @"SELECT id, company_id AS CompanyId, gsp_provider AS GspProvider,
                         environment AS Environment, client_id AS ClientId,
                         client_secret AS ClientSecret, username AS Username,
                         password AS Password, auth_token AS AuthToken,
                         token_expiry AS TokenExpiry, sek AS Sek,
                         auto_generate_irn AS AutoGenerateIrn,
                         auto_cancel_on_void AS AutoCancelOnVoid,
                         generate_eway_bill AS GenerateEwayBill,
                         einvoice_threshold AS EinvoiceThreshold,
                         is_active AS IsActive, created_at AS CreatedAt,
                         updated_at AS UpdatedAt
                  FROM einvoice_credentials
                  WHERE company_id = @CompanyId AND environment = @Environment AND is_active = true",
                new { CompanyId = companyId, Environment = environment });
        }

        public async Task<IEnumerable<EInvoiceCredentials>> GetAllByCompanyIdAsync(Guid companyId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QueryAsync<EInvoiceCredentials>(
                @"SELECT id, company_id AS CompanyId, gsp_provider AS GspProvider,
                         environment AS Environment, client_id AS ClientId,
                         client_secret AS ClientSecret, username AS Username,
                         password AS Password, auth_token AS AuthToken,
                         token_expiry AS TokenExpiry, sek AS Sek,
                         auto_generate_irn AS AutoGenerateIrn,
                         auto_cancel_on_void AS AutoCancelOnVoid,
                         generate_eway_bill AS GenerateEwayBill,
                         einvoice_threshold AS EinvoiceThreshold,
                         is_active AS IsActive, created_at AS CreatedAt,
                         updated_at AS UpdatedAt
                  FROM einvoice_credentials
                  WHERE company_id = @CompanyId
                  ORDER BY environment, created_at DESC",
                new { CompanyId = companyId });
        }

        public async Task<EInvoiceCredentials> AddAsync(EInvoiceCredentials credentials)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var id = await connection.ExecuteScalarAsync<Guid>(
                @"INSERT INTO einvoice_credentials
                  (company_id, gsp_provider, environment, client_id, client_secret,
                   username, password, auth_token, token_expiry, sek,
                   auto_generate_irn, auto_cancel_on_void, generate_eway_bill,
                   einvoice_threshold, is_active)
                  VALUES
                  (@CompanyId, @GspProvider, @Environment, @ClientId, @ClientSecret,
                   @Username, @Password, @AuthToken, @TokenExpiry, @Sek,
                   @AutoGenerateIrn, @AutoCancelOnVoid, @GenerateEwayBill,
                   @EinvoiceThreshold, @IsActive)
                  RETURNING id",
                credentials);

            credentials.Id = id;
            return credentials;
        }

        public async Task UpdateAsync(EInvoiceCredentials credentials)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_credentials SET
                    gsp_provider = @GspProvider,
                    environment = @Environment,
                    client_id = @ClientId,
                    client_secret = @ClientSecret,
                    username = @Username,
                    password = @Password,
                    auto_generate_irn = @AutoGenerateIrn,
                    auto_cancel_on_void = @AutoCancelOnVoid,
                    generate_eway_bill = @GenerateEwayBill,
                    einvoice_threshold = @EinvoiceThreshold,
                    is_active = @IsActive,
                    updated_at = NOW()
                  WHERE id = @Id",
                credentials);
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "DELETE FROM einvoice_credentials WHERE id = @Id",
                new { Id = id });
        }

        public async Task UpdateTokenAsync(Guid id, string authToken, DateTime tokenExpiry, string? sek = null)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE einvoice_credentials SET
                    auth_token = @AuthToken,
                    token_expiry = @TokenExpiry,
                    sek = @Sek,
                    updated_at = NOW()
                  WHERE id = @Id",
                new { Id = id, AuthToken = authToken, TokenExpiry = tokenExpiry, Sek = sek });
        }
    }
}
