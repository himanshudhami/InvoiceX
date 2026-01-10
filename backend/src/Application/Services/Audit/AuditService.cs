using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces.Audit;
using Core.Entities.Audit;
using Core.Interfaces.Audit;
using Microsoft.Extensions.Logging;

namespace Application.Services.Audit
{
    /// <summary>
    /// Service for recording MCA-compliant audit trail entries.
    /// All operations are non-blocking - audit failures are logged but don't affect main operations.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditTrailRepository _repository;
        private readonly IAuditContext _context;
        private readonly ILogger<AuditService> _logger;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        // Entity type mapping from CLR types to audit entity type strings
        private static readonly Dictionary<string, string> EntityTypeMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Invoice", AuditEntityTypes.Invoice },
            { "Payment", AuditEntityTypes.Payment },
            { "Vendor", AuditEntityTypes.Vendor },
            { "VendorInvoice", AuditEntityTypes.VendorInvoice },
            { "VendorPayment", AuditEntityTypes.VendorPayment },
            { "JournalEntry", AuditEntityTypes.JournalEntry },
            { "Customer", AuditEntityTypes.Customer },
            { "BankAccount", AuditEntityTypes.BankAccount },
            { "BankTransaction", AuditEntityTypes.BankTransaction },
            { "Employee", AuditEntityTypes.Employee },
            { "Asset", AuditEntityTypes.Asset },
            { "ChartOfAccount", AuditEntityTypes.ChartOfAccount },
            { "Party", AuditEntityTypes.Party },
            { "CreditNote", AuditEntityTypes.CreditNote },
            { "Quote", AuditEntityTypes.Quote },
            { "Product", AuditEntityTypes.Product },
            { "ExpenseClaim", AuditEntityTypes.ExpenseClaim },
            { "PayrollRun", AuditEntityTypes.PayrollRun }
        };

        public AuditService(
            IAuditTrailRepository repository,
            IAuditContext context,
            ILogger<AuditService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task AuditCreateAsync<T>(T entity, Guid entityId, Guid companyId, string? displayName = null) where T : class
        {
            try
            {
                if (!_context.HasActor)
                {
                    _logger.LogWarning("Skipping audit for CREATE - no actor in context");
                    return;
                }

                var entityType = GetEntityType<T>();
                var entry = CreateBaseEntry(entityType, entityId, companyId, AuditOperations.Create, displayName);
                entry.NewValues = SerializeEntity(entity);

                await _repository.AddAsync(entry);
                _logger.LogDebug("Audited CREATE for {EntityType} {EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                // Non-blocking: log error but don't throw
                _logger.LogError(ex, "Failed to audit CREATE for {EntityType} {EntityId}", typeof(T).Name, entityId);
            }
        }

        public async Task AuditUpdateAsync<T>(T oldEntity, T newEntity, Guid entityId, Guid companyId, string? displayName = null) where T : class
        {
            try
            {
                if (!_context.HasActor)
                {
                    _logger.LogWarning("Skipping audit for UPDATE - no actor in context");
                    return;
                }

                var changedFields = GetChangedFields((object)oldEntity, (object)newEntity);

                // Only audit if there are actual changes
                if (changedFields.Length == 0)
                {
                    _logger.LogDebug("Skipping audit for UPDATE - no changes detected for {EntityType} {EntityId}",
                        typeof(T).Name, entityId);
                    return;
                }

                var entityType = GetEntityType<T>();
                var entry = CreateBaseEntry(entityType, entityId, companyId, AuditOperations.Update, displayName);
                entry.OldValues = SerializeEntity(oldEntity);
                entry.NewValues = SerializeEntity(newEntity);
                entry.ChangedFields = changedFields;

                await _repository.AddAsync(entry);
                _logger.LogDebug("Audited UPDATE for {EntityType} {EntityId} - changed fields: {ChangedFields}",
                    entityType, entityId, string.Join(", ", changedFields));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to audit UPDATE for {EntityType} {EntityId}", typeof(T).Name, entityId);
            }
        }

        public async Task AuditDeleteAsync<T>(T entity, Guid entityId, Guid companyId, string? displayName = null) where T : class
        {
            try
            {
                if (!_context.HasActor)
                {
                    _logger.LogWarning("Skipping audit for DELETE - no actor in context");
                    return;
                }

                var entityType = GetEntityType<T>();
                var entry = CreateBaseEntry(entityType, entityId, companyId, AuditOperations.Delete, displayName);
                entry.OldValues = SerializeEntity(entity);

                await _repository.AddAsync(entry);
                _logger.LogDebug("Audited DELETE for {EntityType} {EntityId}", entityType, entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to audit DELETE for {EntityType} {EntityId}", typeof(T).Name, entityId);
            }
        }

        public async Task AuditAsync(
            string entityType,
            Guid entityId,
            Guid companyId,
            string operation,
            object? oldValues,
            object? newValues,
            string? displayName = null)
        {
            try
            {
                if (!_context.HasActor)
                {
                    _logger.LogWarning("Skipping audit - no actor in context");
                    return;
                }

                var entry = CreateBaseEntry(entityType, entityId, companyId, operation, displayName);
                entry.OldValues = oldValues != null ? SerializeEntity(oldValues) : null;
                entry.NewValues = newValues != null ? SerializeEntity(newValues) : null;

                if (operation == AuditOperations.Update && oldValues != null && newValues != null)
                {
                    entry.ChangedFields = GetChangedFields(oldValues, newValues);
                }

                await _repository.AddAsync(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to audit {Operation} for {EntityType} {EntityId}", operation, entityType, entityId);
            }
        }

        private AuditTrail CreateBaseEntry(string entityType, Guid entityId, Guid companyId, string operation, string? displayName)
        {
            var entry = new AuditTrail
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                EntityType = entityType,
                EntityId = entityId,
                EntityDisplayName = displayName,
                Operation = operation,
                ActorId = _context.ActorId!.Value,
                ActorName = _context.ActorName,
                ActorEmail = _context.ActorEmail,
                ActorIp = _context.ActorIp,
                UserAgent = _context.UserAgent,
                CorrelationId = _context.CorrelationId,
                RequestPath = _context.RequestPath,
                RequestMethod = _context.RequestMethod,
                CreatedAt = DateTime.UtcNow
            };

            // Calculate checksum for integrity (MCA compliance)
            entry.Checksum = CalculateChecksum(entry);

            return entry;
        }

        private static string GetEntityType<T>()
        {
            var typeName = typeof(T).Name;

            // Try exact match, then try without trailing 's' (handles Invoices -> Invoice)
            if (EntityTypeMap.TryGetValue(typeName, out var mappedType))
                return mappedType;

            if (typeName.EndsWith('s') && EntityTypeMap.TryGetValue(typeName[..^1], out mappedType))
                return mappedType;

            return typeName.ToLowerInvariant();
        }

        private static string SerializeEntity(object entity)
        {
            return JsonSerializer.Serialize(entity, SerializerOptions);
        }

        private static string[] GetChangedFields(object oldEntity, object newEntity)
        {
            if (oldEntity.GetType() != newEntity.GetType())
                return Array.Empty<string>();

            var changes = new List<string>();

            foreach (var prop in oldEntity.GetType().GetProperties())
            {
                // Skip navigation properties, collections, complex types, and read-only properties
                if (!prop.CanWrite ||
                    (prop.PropertyType.IsClass && prop.PropertyType != typeof(string) && !prop.PropertyType.IsArray))
                    continue;

                try
                {
                    var oldValue = prop.GetValue(oldEntity);
                    var newValue = prop.GetValue(newEntity);

                    if (!Equals(oldValue, newValue))
                        changes.Add(prop.Name);
                }
                catch
                {
                    // Skip properties that throw on access
                }
            }

            return changes.ToArray();
        }

        private static string CalculateChecksum(AuditTrail entry)
        {
            // Create a hash of key fields for tamper detection
            var content = $"{entry.EntityType}|{entry.EntityId}|{entry.Operation}|{entry.OldValues ?? ""}|{entry.NewValues ?? ""}|{entry.CreatedAt:O}";
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
