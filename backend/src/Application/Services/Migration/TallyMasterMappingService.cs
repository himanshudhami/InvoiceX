using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Core.Entities;
using Core.Entities.Inventory;
using Core.Entities.Ledger;
using Core.Entities.Migration;
using Core.Entities.Tags;
using Core.Interfaces;
using Core.Interfaces.Inventory;
using Core.Interfaces.Ledger;
using Core.Interfaces.Migration;
using Core.Interfaces.Tags;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Service for mapping and importing Tally masters to our entities.
    /// Uses the unified Party model for customers and vendors.
    /// Uses tag-driven TDS detection instead of hard-coded vendor types.
    /// </summary>
    public class TallyMasterMappingService : ITallyMasterMappingService
    {
        private readonly ILogger<TallyMasterMappingService> _logger;
        private readonly ITallyMigrationLogRepository _logRepository;
        private readonly ITallyFieldMappingRepository _mappingRepository;
        private readonly IChartOfAccountRepository _coaRepository;
        private readonly IPartyRepository _partyRepository;
        private readonly ITdsTagRuleRepository _tdsTagRuleRepository;
        private readonly IBankAccountRepository _bankAccountRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockGroupRepository _stockGroupRepository;
        private readonly IWarehouseRepository _warehouseRepository;
        private readonly IUnitOfMeasureRepository _unitRepository;
        private readonly ITagRepository _tagRepository;

        public TallyMasterMappingService(
            ILogger<TallyMasterMappingService> logger,
            ITallyMigrationLogRepository logRepository,
            ITallyFieldMappingRepository mappingRepository,
            IChartOfAccountRepository coaRepository,
            IPartyRepository partyRepository,
            ITdsTagRuleRepository tdsTagRuleRepository,
            IBankAccountRepository bankAccountRepository,
            IStockItemRepository stockItemRepository,
            IStockGroupRepository stockGroupRepository,
            IWarehouseRepository warehouseRepository,
            IUnitOfMeasureRepository unitRepository,
            ITagRepository tagRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _mappingRepository = mappingRepository ?? throw new ArgumentNullException(nameof(mappingRepository));
            _coaRepository = coaRepository ?? throw new ArgumentNullException(nameof(coaRepository));
            _partyRepository = partyRepository ?? throw new ArgumentNullException(nameof(partyRepository));
            _tdsTagRuleRepository = tdsTagRuleRepository ?? throw new ArgumentNullException(nameof(tdsTagRuleRepository));
            _bankAccountRepository = bankAccountRepository ?? throw new ArgumentNullException(nameof(bankAccountRepository));
            _stockItemRepository = stockItemRepository ?? throw new ArgumentNullException(nameof(stockItemRepository));
            _stockGroupRepository = stockGroupRepository ?? throw new ArgumentNullException(nameof(stockGroupRepository));
            _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
            _unitRepository = unitRepository ?? throw new ArgumentNullException(nameof(unitRepository));
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        }

        public async Task<Result<TallyMasterImportResultDto>> ImportMastersAsync(
            Guid batchId,
            Guid companyId,
            TallyMastersSummaryDto masters,
            TallyMappingConfigDto? mappingConfig = null,
            CancellationToken cancellationToken = default)
        {
            var result = new TallyMasterImportResultDto();

            try
            {
                _logger.LogInformation("Starting master import for batch {BatchId}", batchId);

                // Ensure default mappings exist
                if (!await _mappingRepository.HasDefaultMappingsAsync(companyId))
                {
                    await _mappingRepository.SeedDefaultMappingsAsync(companyId);
                }

                // Import in dependency order
                // 1. Units of Measure (no dependencies)
                var unitsResult = await ImportUnitsAsync(batchId, companyId, masters.Units, cancellationToken);
                result.Units = unitsResult.Value!;

                // 2. Stock Groups (self-referential hierarchy)
                var stockGroupsResult = await ImportStockGroupsAsync(batchId, companyId, masters.StockGroups, cancellationToken);
                result.StockGroups = stockGroupsResult.Value!;

                // 3. Godowns/Warehouses (self-referential hierarchy)
                var godownsResult = await ImportGodownsAsync(batchId, companyId, masters.Godowns, cancellationToken);
                result.Godowns = godownsResult.Value!;

                // 4. Cost Centers as Tags (self-referential hierarchy)
                var costCentersResult = await ImportCostCentersAsync(batchId, companyId, masters.CostCenters, masters.CostCategories, cancellationToken);
                result.CostCenters = costCentersResult.Value!;

                // 5. Ledgers (depends on nothing for creation, but links are resolved later)
                var ledgersResult = await ImportLedgersAsync(batchId, companyId, masters.Ledgers, cancellationToken);
                result.Ledgers = ledgersResult.Value!;

                // 6. Stock Items (depends on stock groups, units, godowns)
                var stockItemsResult = await ImportStockItemsAsync(batchId, companyId, masters.StockItems, cancellationToken);
                result.StockItems = stockItemsResult.Value!;

                // Calculate totals
                result.TotalImported = result.Units.Imported + result.StockGroups.Imported +
                                        result.Godowns.Imported + result.CostCenters.Imported +
                                        result.Ledgers.Imported + result.StockItems.Imported;

                result.TotalFailed = result.Units.Failed + result.StockGroups.Failed +
                                      result.Godowns.Failed + result.CostCenters.Failed +
                                      result.Ledgers.Failed + result.StockItems.Failed;

                result.TotalSuspense = result.Ledgers.Suspense;

                _logger.LogInformation(
                    "Master import completed for batch {BatchId}: {Imported} imported, {Failed} failed, {Suspense} suspense",
                    batchId, result.TotalImported, result.TotalFailed, result.TotalSuspense);

                return Result<TallyMasterImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master import failed for batch {BatchId}", batchId);
                return Error.Internal($"Master import failed: {ex.Message}");
            }
        }

        public async Task<Result<TallyImportCountsDto>> ImportUnitsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyUnitDto> units,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = units.Count };
            var processingOrder = 0;

            foreach (var unit in units)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _unitRepository.GetByTallyGuidAsync(companyId, unit.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogMigration(batchId, "unit", unit.Guid, unit.Name, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Create new unit
                    var newUnit = new UnitOfMeasure
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = unit.Name,
                        Symbol = unit.Symbol,
                        DecimalPlaces = unit.DecimalPlaces ?? 2,
                        TallyUnitGuid = unit.Guid,
                        TallyUnitName = unit.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitRepository.AddAsync(newUnit);
                    unit.TargetId = newUnit.Id;
                    counts.Imported++;

                    await LogMigration(batchId, "unit", unit.Guid, unit.Name, "success", null, newUnit.Id, processingOrder);
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import unit {Name}", unit.Name);
                    await LogMigration(batchId, "unit", unit.Guid, unit.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportStockGroupsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyStockGroupDto> stockGroups,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = stockGroups.Count };
            var processingOrder = 0;
            var createdGroups = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            // Sort by hierarchy level (parents first)
            var sortedGroups = SortByHierarchy(stockGroups, g => g.Name, g => g.Parent);

            foreach (var group in sortedGroups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _stockGroupRepository.GetByTallyGuidAsync(companyId, group.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        createdGroups[group.Name] = existing.Id;
                        await LogMigration(batchId, "stock_group", group.Guid, group.Name, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve parent
                    Guid? parentId = null;
                    if (!string.IsNullOrEmpty(group.Parent))
                    {
                        if (createdGroups.TryGetValue(group.Parent, out var pid))
                            parentId = pid;
                        else
                        {
                            var parentGroup = await _stockGroupRepository.GetByNameAsync(companyId, group.Parent);
                            parentId = parentGroup?.Id;
                        }
                    }

                    var newGroup = new StockGroup
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = group.Name,
                        ParentStockGroupId = parentId,
                        TallyStockGroupGuid = group.Guid,
                        TallyStockGroupName = group.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _stockGroupRepository.AddAsync(newGroup);
                    group.TargetId = newGroup.Id;
                    createdGroups[group.Name] = newGroup.Id;
                    counts.Imported++;

                    await LogMigration(batchId, "stock_group", group.Guid, group.Name, "success", null, newGroup.Id, processingOrder);
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import stock group {Name}", group.Name);
                    await LogMigration(batchId, "stock_group", group.Guid, group.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportStockItemsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyStockItemDto> stockItems,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = stockItems.Count };
            var processingOrder = 0;

            foreach (var item in stockItems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _stockItemRepository.GetByTallyGuidAsync(companyId, item.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        await LogMigration(batchId, "stock_item", item.Guid, item.Name, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve stock group
                    Guid? stockGroupId = null;
                    if (!string.IsNullOrEmpty(item.StockGroup))
                    {
                        var group = await _stockGroupRepository.GetByNameAsync(companyId, item.StockGroup);
                        stockGroupId = group?.Id;
                    }

                    // Resolve unit
                    Guid? unitId = null;
                    if (!string.IsNullOrEmpty(item.BaseUnits))
                    {
                        var unit = await _unitRepository.GetByNameAsync(companyId, item.BaseUnits);
                        unitId = unit?.Id;
                    }

                    var newItem = new StockItem
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = item.Name,
                        Sku = item.PartNumber ?? GenerateSku(item.Name),
                        Description = item.Description,
                        StockGroupId = stockGroupId,
                        BaseUnitId = unitId ?? Guid.Empty,
                        HsnSacCode = item.HsnCode ?? item.SacCode,
                        GstRate = item.GstRate ?? 18m,
                        OpeningQuantity = item.OpeningQuantity,
                        CostPrice = item.OpeningRate,
                        OpeningValue = item.OpeningValue,
                        ReorderLevel = item.ReorderLevel,
                        ValuationMethod = MapCostingMethod(item.CostingMethod),
                        IsBatchEnabled = item.IsBatchEnabled,
                        TallyStockItemGuid = item.Guid,
                        TallyStockItemName = item.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _stockItemRepository.AddAsync(newItem);
                    item.TargetId = newItem.Id;
                    counts.Imported++;

                    await LogMigration(batchId, "stock_item", item.Guid, item.Name, "success", null, newItem.Id, processingOrder);
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import stock item {Name}", item.Name);
                    await LogMigration(batchId, "stock_item", item.Guid, item.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportGodownsAsync(
            Guid batchId,
            Guid companyId,
            List<TallyGodownDto> godowns,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = godowns.Count };
            var processingOrder = 0;
            var createdWarehouses = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            // Sort by hierarchy
            var sortedGodowns = SortByHierarchy(godowns, g => g.Name, g => g.Parent);

            foreach (var godown in sortedGodowns)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _warehouseRepository.GetByTallyGuidAsync(companyId, godown.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        createdWarehouses[godown.Name] = existing.Id;
                        await LogMigration(batchId, "godown", godown.Guid, godown.Name, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve parent
                    Guid? parentId = null;
                    if (!string.IsNullOrEmpty(godown.Parent))
                    {
                        if (createdWarehouses.TryGetValue(godown.Parent, out var pid))
                            parentId = pid;
                        else
                        {
                            var parent = await _warehouseRepository.GetByNameAsync(companyId, godown.Parent);
                            parentId = parent?.Id;
                        }
                    }

                    var warehouse = new Warehouse
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = godown.Name,
                        Code = GenerateWarehouseCode(godown.Name),
                        Address = godown.Address,
                        ParentWarehouseId = parentId,
                        IsActive = !godown.HasNoStock,
                        TallyGodownGuid = godown.Guid,
                        TallyGodownName = godown.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _warehouseRepository.AddAsync(warehouse);
                    godown.TargetId = warehouse.Id;
                    createdWarehouses[godown.Name] = warehouse.Id;
                    counts.Imported++;

                    await LogMigration(batchId, "godown", godown.Guid, godown.Name, "success", null, warehouse.Id, processingOrder);
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import godown {Name}", godown.Name);
                    await LogMigration(batchId, "godown", godown.Guid, godown.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportCostCentersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyCostCenterDto> costCenters,
            List<TallyCostCategoryDto> costCategories,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = costCenters.Count };
            var processingOrder = 0;
            var createdTags = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            // Sort by hierarchy
            var sortedCenters = SortByHierarchy(costCenters, c => c.Name, c => c.Parent);

            foreach (var center in sortedCenters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Check if already exists
                    var existing = await _tagRepository.GetByTallyCostCenterGuidAsync(companyId, center.Guid);
                    if (existing != null)
                    {
                        counts.Skipped++;
                        createdTags[center.Name] = existing.Id;
                        await LogMigration(batchId, "cost_center", center.Guid, center.Name, "skipped", "Already exists", existing.Id, processingOrder);
                        continue;
                    }

                    // Resolve parent
                    Guid? parentId = null;
                    if (!string.IsNullOrEmpty(center.Parent))
                    {
                        if (createdTags.TryGetValue(center.Parent, out var pid))
                            parentId = pid;
                        else
                        {
                            var parent = await _tagRepository.GetByNameAsync(companyId, center.Parent, null);
                            parentId = parent?.Id;
                        }
                    }

                    // Determine tag group from cost category mapping
                    var tagGroup = "cost_center";
                    if (!string.IsNullOrEmpty(center.Category))
                    {
                        var categoryMapping = await _mappingRepository.GetMappingForCostCategoryAsync(companyId, center.Category);
                        if (categoryMapping != null && !string.IsNullOrEmpty(categoryMapping.TargetTagGroup))
                        {
                            tagGroup = categoryMapping.TargetTagGroup;
                        }
                    }

                    var tag = new Tag
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = companyId,
                        Name = center.Name,
                        TagGroup = tagGroup,
                        ParentTagId = parentId,
                        IsActive = true,
                        TallyCostCenterGuid = center.Guid,
                        TallyCostCenterName = center.Name,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _tagRepository.AddAsync(tag);
                    center.TargetId = tag.Id;
                    center.TargetTagGroup = tagGroup;
                    createdTags[center.Name] = tag.Id;
                    counts.Imported++;

                    await LogMigration(batchId, "cost_center", center.Guid, center.Name, "success", null, tag.Id, processingOrder);
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import cost center {Name}", center.Name);
                    await LogMigration(batchId, "cost_center", center.Guid, center.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        public async Task<Result<TallyImportCountsDto>> ImportLedgersAsync(
            Guid batchId,
            Guid companyId,
            List<TallyLedgerDto> ledgers,
            CancellationToken cancellationToken = default)
        {
            var counts = new TallyImportCountsDto { Total = ledgers.Count };
            var processingOrder = 0;

            foreach (var ledger in ledgers)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processingOrder++;

                try
                {
                    // Determine target entity based on mapping
                    var targetEntity = await _mappingRepository.GetTargetEntityAsync(
                        companyId,
                        ledger.LedgerGroup ?? string.Empty,
                        ledger.Name);

                    ledger.TargetEntity = targetEntity;

                    switch (targetEntity.ToLower())
                    {
                        case "customers":
                            await ImportLedgerAsCustomer(batchId, companyId, ledger, processingOrder, counts);
                            break;

                        case "vendors":
                            await ImportLedgerAsVendor(batchId, companyId, ledger, processingOrder, counts);
                            break;

                        case "bank_accounts":
                            await ImportLedgerAsBankAccount(batchId, companyId, ledger, processingOrder, counts);
                            break;

                        case "suspense":
                            await ImportLedgerAsSuspense(batchId, companyId, ledger, processingOrder, counts);
                            break;

                        default:
                            // Import as chart of account
                            await ImportLedgerAsChartOfAccount(batchId, companyId, ledger, processingOrder, counts, targetEntity);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    counts.Failed++;
                    _logger.LogWarning(ex, "Failed to import ledger {Name}", ledger.Name);
                    await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "failed", ex.Message, null, processingOrder);
                }
            }

            return Result<TallyImportCountsDto>.Success(counts);
        }

        private async Task ImportLedgerAsCustomer(Guid batchId, Guid companyId, TallyLedgerDto ledger, int order, TallyImportCountsDto counts)
        {
            // Check by Tally GUID first using the unified Party model
            var existing = await _partyRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
            if (existing != null)
            {
                // If exists but not marked as customer, enable customer role
                if (!existing.IsCustomer)
                {
                    existing.IsCustomer = true;
                    await _partyRepository.UpdateAsync(existing);
                    _logger.LogInformation("Enabled customer role for existing party {PartyId}", existing.Id);
                }
                counts.Skipped++;
                ledger.TargetId = existing.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Already exists as party (by GUID)", existing.Id, order);
                return;
            }

            // Check by name (deduplication for parties created without Tally GUID)
            var existingByName = await _partyRepository.GetByNameAsync(companyId, ledger.Name);
            if (existingByName != null)
            {
                // Link existing party to Tally by updating TallyGuid and enable customer role
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                existingByName.IsCustomer = true;
                await _partyRepository.UpdateAsync(existingByName);

                counts.Skipped++;
                ledger.TargetId = existingByName.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Linked to existing party (by name)", existingByName.Id, order);
                return;
            }

            // Create new Party with customer role
            var party = new Party
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = ledger.Name,
                DisplayName = ledger.Name,
                LegalName = ledger.Name,
                IsCustomer = true,
                IsVendor = false,
                IsEmployee = false,
                Email = ledger.Email,
                Phone = ledger.PhoneNumber,
                Mobile = ledger.MobileNumber,
                AddressLine1 = ledger.Address,
                City = string.Empty,
                State = ledger.StateName,
                StateCode = ledger.StateCode,
                Pincode = ledger.Pincode,
                Country = ledger.CountryName ?? "India",
                PanNumber = ledger.PanNumber,
                Gstin = ledger.Gstin ?? ledger.PartyGstin,
                IsGstRegistered = !string.IsNullOrEmpty(ledger.Gstin ?? ledger.PartyGstin),
                PartyType = DeterminePartyType(ledger),
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdParty = await _partyRepository.AddAsync(party);
            ledger.TargetId = createdParty.Id;

            // Create customer profile with additional details
            var customerProfile = new PartyCustomerProfile
            {
                Id = Guid.NewGuid(),
                PartyId = createdParty.Id,
                CompanyId = companyId,
                CustomerType = "b2b",
                CreditLimit = ledger.CreditLimit,
                PaymentTermsDays = ledger.CreditDays,
                EInvoiceApplicable = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _partyRepository.AddCustomerProfileAsync(customerProfile);

            // Auto-tag based on Tally group
            await AutoTagPartyFromTallyGroup(createdParty.Id, companyId, ledger.LedgerGroup, batchId);

            counts.Imported++;

            // Also create corresponding AR account
            await CreateReceivableAccount(batchId, companyId, ledger, createdParty.Id, order);

            await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "success", null, createdParty.Id, order, "parties");
        }

        private async Task ImportLedgerAsVendor(Guid batchId, Guid companyId, TallyLedgerDto ledger, int order, TallyImportCountsDto counts)
        {
            // Check by Tally GUID first using the unified Party model
            var existing = await _partyRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
            if (existing != null)
            {
                // If exists but not marked as vendor, enable vendor role
                if (!existing.IsVendor)
                {
                    existing.IsVendor = true;
                    await _partyRepository.UpdateAsync(existing);
                    _logger.LogInformation("Enabled vendor role for existing party {PartyId}", existing.Id);
                }
                counts.Skipped++;
                ledger.TargetId = existing.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Already exists as party (by GUID)", existing.Id, order);
                return;
            }

            // Check by name (deduplication for parties created without Tally GUID)
            var existingByName = await _partyRepository.GetByNameAsync(companyId, ledger.Name);
            if (existingByName != null)
            {
                // Link existing party to Tally by updating TallyGuid and enable vendor role
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                existingByName.IsVendor = true;
                await _partyRepository.UpdateAsync(existingByName);

                counts.Skipped++;
                ledger.TargetId = existingByName.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Linked to existing party (by name)", existingByName.Id, order);
                return;
            }

            // Create new Party with vendor role
            var party = new Party
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = ledger.Name,
                DisplayName = ledger.Name,
                LegalName = ledger.Name,
                IsCustomer = false,
                IsVendor = true,
                IsEmployee = false,
                Email = ledger.Email,
                Phone = ledger.PhoneNumber,
                Mobile = ledger.MobileNumber,
                AddressLine1 = ledger.Address,
                City = string.Empty,
                State = ledger.StateName,
                StateCode = ledger.StateCode,
                Pincode = ledger.Pincode,
                Country = ledger.CountryName ?? "India",
                PanNumber = ledger.PanNumber,
                Gstin = ledger.Gstin ?? ledger.PartyGstin,
                IsGstRegistered = !string.IsNullOrEmpty(ledger.Gstin ?? ledger.PartyGstin),
                PartyType = DeterminePartyType(ledger),
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdParty = await _partyRepository.AddAsync(party);
            ledger.TargetId = createdParty.Id;

            // Auto-tag based on Tally group (e.g., CONSULTANTS → "Consultant" tag)
            await AutoTagPartyFromTallyGroup(createdParty.Id, companyId, ledger.LedgerGroup, batchId);

            // Detect TDS configuration based on tags and Tally group
            var tdsConfig = await DetectTdsConfiguration(companyId, createdParty.Id, ledger.LedgerGroup);

            // Create vendor profile with TDS settings
            var vendorProfile = new PartyVendorProfile
            {
                Id = Guid.NewGuid(),
                PartyId = createdParty.Id,
                CompanyId = companyId,
                VendorType = "b2b",
                TdsApplicable = tdsConfig != null,
                DefaultTdsSection = tdsConfig?.TdsSection,
                DefaultTdsRate = tdsConfig?.TdsRate,
                BankAccountNumber = ledger.BankAccountNumber,
                BankIfscCode = ledger.IfscCode,
                BankName = ledger.BankBranchName,
                CreditLimit = ledger.CreditLimit,
                PaymentTermsDays = ledger.CreditDays,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _partyRepository.AddVendorProfileAsync(vendorProfile);

            counts.Imported++;

            // Also create corresponding AP account
            await CreatePayableAccount(batchId, companyId, ledger, createdParty.Id, order);

            _logger.LogInformation(
                "Imported vendor {Name} from Tally group {Group} with TDS: {TdsSection} ({TdsRate}%)",
                ledger.Name, ledger.LedgerGroup, tdsConfig?.TdsSection ?? "None", tdsConfig?.TdsRate ?? 0);

            await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "success", null, createdParty.Id, order, "parties");
        }

        private async Task ImportLedgerAsBankAccount(Guid batchId, Guid companyId, TallyLedgerDto ledger, int order, TallyImportCountsDto counts)
        {
            // Check by Tally GUID first
            var existing = await _bankAccountRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
            if (existing != null)
            {
                counts.Skipped++;
                ledger.TargetId = existing.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Already exists as bank account (by GUID)", existing.Id, order);
                return;
            }

            // Check by account number (deduplication for bank accounts created without Tally GUID)
            if (!string.IsNullOrEmpty(ledger.BankAccountNumber))
            {
                var existingByNumber = await _bankAccountRepository.GetByAccountNumberAsync(companyId, ledger.BankAccountNumber);
                if (existingByNumber != null)
                {
                    // Link existing bank account to Tally
                    existingByNumber.TallyLedgerGuid = ledger.Guid;
                    existingByNumber.TallyLedgerName = ledger.Name;
                    existingByNumber.TallyMigrationBatchId = batchId;
                    await _bankAccountRepository.UpdateAsync(existingByNumber);

                    counts.Skipped++;
                    ledger.TargetId = existingByNumber.Id;
                    await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Linked to existing bank account (by account number)", existingByNumber.Id, order);
                    return;
                }
            }

            // Check by name as fallback
            var existingByName = await _bankAccountRepository.GetByNameAsync(companyId, ledger.Name);
            if (existingByName != null)
            {
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyMigrationBatchId = batchId;
                await _bankAccountRepository.UpdateAsync(existingByName);

                counts.Skipped++;
                ledger.TargetId = existingByName.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Linked to existing bank account (by name)", existingByName.Id, order);
                return;
            }

            var bankAccount = new BankAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                AccountName = ledger.Name,
                AccountNumber = ledger.BankAccountNumber,
                BankName = ledger.BankBranchName ?? ledger.Name ?? "Unknown Bank", // Required field - fallback to ledger name
                IfscCode = ledger.IfscCode,
                CurrentBalance = ledger.ClosingBalance,
                OpeningBalance = ledger.OpeningBalance,
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bankAccountRepository.AddAsync(bankAccount);
            ledger.TargetId = bankAccount.Id;
            counts.Imported++;

            // Also create corresponding GL account
            await CreateBankGlAccount(batchId, companyId, ledger, bankAccount.Id, order);

            await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "success", null, bankAccount.Id, order, "bank_accounts");
        }

        private async Task ImportLedgerAsChartOfAccount(Guid batchId, Guid companyId, TallyLedgerDto ledger, int order, TallyImportCountsDto counts, string targetEntity)
        {
            // Check by Tally GUID first
            var existing = await _coaRepository.GetByTallyGuidAsync(companyId, ledger.Guid);
            if (existing != null)
            {
                counts.Skipped++;
                ledger.TargetId = existing.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Already exists as GL account (by GUID)", existing.Id, order);
                return;
            }

            // Check by account name (deduplication for accounts created without Tally GUID)
            var existingByName = await _coaRepository.GetByNameAsync(companyId, ledger.Name);
            if (existingByName != null)
            {
                // Link existing account to Tally by updating TallyGuid
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                await _coaRepository.UpdateAsync(existingByName);

                counts.Skipped++;
                ledger.TargetId = existingByName.Id;
                await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "skipped", "Linked to existing GL account (by name)", existingByName.Id, order);
                return;
            }

            var accountType = DetermineAccountType(ledger.LedgerGroup, targetEntity);
            var accountCode = await GenerateAccountCode(companyId, accountType, ledger.Guid);

            var account = new ChartOfAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                AccountCode = accountCode,
                AccountName = ledger.Name,
                AccountType = accountType,
                NormalBalance = GetNormalBalance(accountType),
                OpeningBalance = ledger.OpeningBalance,
                CurrentBalance = ledger.ClosingBalance,
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _coaRepository.AddAsync(account);
            ledger.TargetId = account.Id;
            counts.Imported++;

            await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "success", null, account.Id, order, "chart_of_accounts");
        }

        private async Task ImportLedgerAsSuspense(Guid batchId, Guid companyId, TallyLedgerDto ledger, int order, TallyImportCountsDto counts)
        {
            // Find or create suspense account
            var suspenseAccount = await _coaRepository.GetSuspenseAccountAsync(companyId, "unmapped");

            ledger.TargetId = suspenseAccount?.Id;
            counts.Suspense++;

            await LogMigration(batchId, "ledger", ledger.Guid, ledger.Name, "mapped_to_suspense",
                $"Unmapped ledger group: {ledger.LedgerGroup}", suspenseAccount?.Id, order, "suspense");
        }

        private async Task CreateReceivableAccount(Guid batchId, Guid companyId, TallyLedgerDto ledger, Guid customerId, int order)
        {
            var accountName = $"Trade Receivable - {ledger.Name}";

            // Check if account already exists by name (deduplication)
            var existingByName = await _coaRepository.GetByNameAsync(companyId, accountName);
            if (existingByName != null)
            {
                // Link existing account to Tally
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                existingByName.UpdatedAt = DateTime.UtcNow;
                await _coaRepository.UpdateAsync(existingByName);
                return;
            }

            var accountCode = await GenerateAccountCode(companyId, "asset", ledger.Guid + "_AR");
            var account = new ChartOfAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                AccountCode = accountCode,
                AccountName = accountName,
                AccountType = "asset",
                NormalBalance = "debit",
                AccountSubtype = "trade_receivables",
                OpeningBalance = ledger.OpeningBalance,
                CurrentBalance = ledger.ClosingBalance,
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _coaRepository.AddAsync(account);
        }

        private async Task CreatePayableAccount(Guid batchId, Guid companyId, TallyLedgerDto ledger, Guid vendorId, int order)
        {
            var accountName = $"Trade Payable - {ledger.Name}";

            // Check if account already exists by name (deduplication)
            var existingByName = await _coaRepository.GetByNameAsync(companyId, accountName);
            if (existingByName != null)
            {
                // Link existing account to Tally
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                existingByName.UpdatedAt = DateTime.UtcNow;
                await _coaRepository.UpdateAsync(existingByName);
                return;
            }

            var accountCode = await GenerateAccountCode(companyId, "liability", ledger.Guid + "_AP");
            var account = new ChartOfAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                AccountCode = accountCode,
                AccountName = accountName,
                AccountType = "liability",
                NormalBalance = "credit",
                AccountSubtype = "trade_payables",
                OpeningBalance = ledger.OpeningBalance,
                CurrentBalance = ledger.ClosingBalance,
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _coaRepository.AddAsync(account);
        }

        private async Task CreateBankGlAccount(Guid batchId, Guid companyId, TallyLedgerDto ledger, Guid bankAccountId, int order)
        {
            var accountName = $"Bank - {ledger.Name}";

            // Check if account already exists by name (deduplication)
            var existingByName = await _coaRepository.GetByNameAsync(companyId, accountName);
            if (existingByName != null)
            {
                // Link existing account to Tally
                existingByName.TallyLedgerGuid = ledger.Guid;
                existingByName.TallyLedgerName = ledger.Name;
                existingByName.TallyGroupName = ledger.LedgerGroup;
                existingByName.TallyMigrationBatchId = batchId;
                existingByName.UpdatedAt = DateTime.UtcNow;
                await _coaRepository.UpdateAsync(existingByName);
                return;
            }

            var accountCode = await GenerateAccountCode(companyId, "asset", ledger.Guid + "_BANK");
            var account = new ChartOfAccount
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                AccountCode = accountCode,
                AccountName = accountName,
                AccountType = "asset",
                NormalBalance = "debit",
                AccountSubtype = "bank",
                OpeningBalance = ledger.OpeningBalance,
                CurrentBalance = ledger.ClosingBalance,
                IsActive = true,
                TallyLedgerGuid = ledger.Guid,
                TallyLedgerName = ledger.Name,
                TallyGroupName = ledger.LedgerGroup,
                TallyMigrationBatchId = batchId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _coaRepository.AddAsync(account);
        }

        private static string GetNormalBalance(string accountType)
        {
            // Standard accounting: Assets/Expenses are debit-normal, Liabilities/Equity/Income are credit-normal
            return accountType switch
            {
                "asset" => "debit",
                "expense" => "debit",
                "liability" => "credit",
                "equity" => "credit",
                "income" => "credit",
                _ => "debit"
            };
        }

        private string DetermineAccountType(string? ledgerGroup, string targetEntity)
        {
            if (string.IsNullOrEmpty(ledgerGroup))
                return "asset";

            var group = ledgerGroup.ToLower();

            // Check targetEntity hint
            if (targetEntity.Contains(":"))
            {
                var type = targetEntity.Split(':')[1];
                if (new[] { "asset", "liability", "equity", "income", "expense" }.Contains(type))
                    return type;
            }

            // Infer from group name
            return group switch
            {
                var g when g.Contains("fixed asset") => "asset",
                var g when g.Contains("current asset") => "asset",
                var g when g.Contains("bank") => "asset",
                var g when g.Contains("cash") => "asset",
                var g when g.Contains("current liabilit") => "liability",
                var g when g.Contains("loans") => "liability",
                var g when g.Contains("duties & taxes") => "liability",
                var g when g.Contains("provisions") => "liability",
                var g when g.Contains("capital") => "equity",
                var g when g.Contains("reserves") => "equity",
                var g when g.Contains("sales") => "income",
                var g when g.Contains("income") => "income",
                var g when g.Contains("revenue") => "income",
                var g when g.Contains("purchase") => "expense",
                var g when g.Contains("expense") => "expense",
                var g when g.Contains("indirect expense") => "expense",
                var g when g.Contains("direct expense") => "expense",
                _ => "asset" // Default
            };
        }

        private Task<string> GenerateAccountCode(Guid companyId, string accountType, string? tallyGuid = null)
        {
            // Use Tally GUID hash to guarantee uniqueness (avoids race conditions)
            // Format: T{type}{hash} - e.g., TL-A1B2C3 for liability
            var typePrefix = accountType switch
            {
                "asset" => "A",
                "liability" => "L",
                "equity" => "E",
                "income" => "I",
                "expense" => "X",
                _ => "O"
            };

            // Generate unique code from Tally GUID or random if not available
            string uniquePart;
            if (!string.IsNullOrEmpty(tallyGuid))
            {
                // Use first 8 chars of Tally GUID hash for uniqueness
                var hash = Math.Abs(tallyGuid.GetHashCode()).ToString("X8");
                uniquePart = hash.Substring(0, Math.Min(8, hash.Length));
            }
            else
            {
                // Fallback: use timestamp + random for uniqueness
                uniquePart = $"{DateTime.UtcNow:HHmmssfff}{Random.Shared.Next(1000, 9999)}";
            }

            return Task.FromResult($"T{typePrefix}-{uniquePart}");
        }

        private static string GenerateSku(string name)
        {
            // Generate a simple SKU from name
            var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ').ToArray());
            var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var sku = string.Join("", words.Select(w => w.Length > 0 ? w[0].ToString().ToUpper() : ""));
            return $"SKU-{sku}-{DateTime.UtcNow:MMdd}";
        }

        private static string GenerateWarehouseCode(string name)
        {
            var cleaned = new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
            return $"WH-{cleaned.Substring(0, Math.Min(6, cleaned.Length)).ToUpper()}";
        }

        private static string MapCostingMethod(string? tallyMethod)
        {
            return tallyMethod?.ToUpper() switch
            {
                "FIFO" => "fifo",
                "LIFO" => "lifo",
                "WEIGHTED AVERAGE" => "weighted_average",
                "AVERAGE" => "weighted_average",
                _ => "weighted_average"
            };
        }

        private static List<T> SortByHierarchy<T>(List<T> items, Func<T, string> getName, Func<T, string?> getParent)
        {
            var result = new List<T>();
            var remaining = new List<T>(items);
            var added = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add root items first (no parent)
            var roots = remaining.Where(i => string.IsNullOrEmpty(getParent(i))).ToList();
            foreach (var root in roots)
            {
                result.Add(root);
                added.Add(getName(root));
                remaining.Remove(root);
            }

            // Keep adding items whose parents are already added
            var maxIterations = items.Count;
            var iteration = 0;
            while (remaining.Count > 0 && iteration < maxIterations)
            {
                iteration++;
                var toAdd = remaining.Where(i =>
                {
                    var parent = getParent(i);
                    return !string.IsNullOrEmpty(parent) && added.Contains(parent);
                }).ToList();

                if (toAdd.Count == 0)
                {
                    // Remaining items have missing parents, add them anyway
                    result.AddRange(remaining);
                    break;
                }

                foreach (var item in toAdd)
                {
                    result.Add(item);
                    added.Add(getName(item));
                    remaining.Remove(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Auto-tag a party based on Tally ledger group name using tag assignments from tally_field_mappings.
        /// Applies both party type tags (e.g., "Vendor:Consultant") and TDS section tags (e.g., "TDS:194J-Professional").
        /// </summary>
        private async Task AutoTagPartyFromTallyGroup(Guid partyId, Guid companyId, string? tallyGroupName, Guid batchId)
        {
            if (string.IsNullOrEmpty(tallyGroupName))
                return;

            try
            {
                // Get tag assignments from tally_field_mappings for this Tally group
                var tagNames = await _mappingRepository.GetTagAssignmentsForGroupAsync(companyId, tallyGroupName);

                if (tagNames.Count == 0)
                {
                    _logger.LogDebug("No tag assignments found for Tally group '{TallyGroup}'", tallyGroupName);
                    return;
                }

                var tagsApplied = new List<string>();

                foreach (var tagName in tagNames)
                {
                    // Determine tag group from the tag name prefix
                    var tagGroup = tagName switch
                    {
                        var n when n.StartsWith("TDS:") => "tds_section",
                        var n when n.StartsWith("Vendor:") => "party_type",
                        var n when n.StartsWith("Customer:") => "party_type",
                        var n when n.StartsWith("MSME:") => "compliance",
                        var n when n.StartsWith("GST:") => "compliance",
                        var n when n.StartsWith("PAN:") => "compliance",
                        _ => "party_type" // Default to party_type
                    };

                    // Find the tag by name and group
                    var tag = await _tagRepository.GetByNameAndGroupAsync(companyId, tagName, tagGroup);

                    if (tag == null)
                    {
                        _logger.LogWarning(
                            "Tag '{TagName}' (group: {TagGroup}) not found for company {CompanyId}. Run seed_tds_system() first.",
                            tagName, tagGroup, companyId);
                        continue;
                    }

                    // Add tag to party
                    await _partyRepository.AddTagAsync(partyId, tag.Id, "migration");
                    tagsApplied.Add(tagName);
                }

                if (tagsApplied.Count > 0)
                {
                    _logger.LogInformation(
                        "Auto-tagged party {PartyId} from Tally group '{TallyGroup}' with tags: [{Tags}]",
                        partyId, tallyGroupName, string.Join(", ", tagsApplied));
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the import
                _logger.LogWarning(ex, "Failed to auto-tag party {PartyId} from Tally group '{TallyGroup}'",
                    partyId, tallyGroupName);
            }
        }

        /// <summary>
        /// Detect TDS configuration for a party based on assigned TDS section tags.
        /// Returns TDS section and rate if a TDS tag is assigned to the party.
        /// Uses tag-driven TDS detection instead of hard-coded vendor types.
        /// </summary>
        private async Task<TdsTagConfig?> DetectTdsConfiguration(Guid companyId, Guid partyId, string? tallyGroupName)
        {
            try
            {
                // Get TDS rule for this party (based on assigned TDS section tags)
                var tdsRule = await _tdsTagRuleRepository.GetRuleForPartyAsync(partyId);

                if (tdsRule != null && tdsRule.TdsSection != "EXEMPT")
                {
                    _logger.LogDebug(
                        "Detected TDS by tag for party {PartyId}: {TdsSection} ({TdsRate}%)",
                        partyId, tdsRule.TdsSection, tdsRule.TdsRateWithPan);

                    return new TdsTagConfig
                    {
                        TdsSection = tdsRule.TdsSection,
                        TdsSectionClause = tdsRule.TdsSectionClause,
                        TdsRate = tdsRule.TdsRateWithPan,
                        TdsRateIndividual = tdsRule.TdsRateIndividual,
                        TdsRateCompany = tdsRule.TdsRateCompany,
                        ThresholdAnnual = tdsRule.ThresholdAnnual,
                        ThresholdSinglePayment = tdsRule.ThresholdSinglePayment
                    };
                }

                // If no TDS tag found, TDS is not applicable
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect TDS configuration for party {PartyId}", partyId);
                return null;
            }
        }

        /// <summary>
        /// TDS configuration detected from tags (internal DTO)
        /// </summary>
        private class TdsTagConfig
        {
            public string TdsSection { get; set; } = string.Empty;
            public string? TdsSectionClause { get; set; }
            public decimal TdsRate { get; set; }
            public decimal? TdsRateIndividual { get; set; }
            public decimal? TdsRateCompany { get; set; }
            public decimal ThresholdAnnual { get; set; }
            public decimal? ThresholdSinglePayment { get; set; }
        }

        /// <summary>
        /// Determine party type (individual, company, firm, etc.) from ledger data.
        /// </summary>
        private static string DeterminePartyType(TallyLedgerDto ledger)
        {
            // Try to infer from name or other fields
            var name = ledger.Name?.ToLower() ?? string.Empty;

            if (name.Contains("ltd") || name.Contains("limited") || name.Contains("pvt") || name.Contains("private"))
                return "company";
            if (name.Contains("llp") || name.Contains("partnership"))
                return "firm";
            if (name.Contains("trust") || name.Contains("foundation"))
                return "trust";
            if (name.Contains("government") || name.Contains("ministry") || name.Contains("dept"))
                return "government";

            // Check if GSTIN indicates company/firm
            if (!string.IsNullOrEmpty(ledger.Gstin) && ledger.Gstin.Length == 15)
            {
                var entityType = ledger.Gstin.Substring(5, 1);
                return entityType switch
                {
                    "C" => "company",
                    "F" => "firm",
                    "A" => "aop",
                    "T" => "trust",
                    "G" => "government",
                    _ => "individual"
                };
            }

            return "individual"; // Default
        }

        private async Task LogMigration(
            Guid batchId,
            string recordType,
            string tallyGuid,
            string tallyName,
            string status,
            string? message,
            Guid? targetId,
            int processingOrder,
            string? targetEntity = null)
        {
            var log = new TallyMigrationLog
            {
                BatchId = batchId,
                RecordType = recordType,
                TallyGuid = tallyGuid,
                TallyName = tallyName,
                Status = status,
                ErrorMessage = message,
                TargetId = targetId,
                TargetEntity = targetEntity ?? recordType,
                ProcessingOrder = processingOrder
            };

            await _logRepository.AddAsync(log);
        }
    }
}
