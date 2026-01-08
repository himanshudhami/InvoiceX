using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Parser for Tally JSON export files (TallyPrime format)
    /// </summary>
    public class TallyJsonParserService : ITallyParserService
    {
        private readonly ILogger<TallyJsonParserService> _logger;

        public string Format => "json";

        public TallyJsonParserService(ILogger<TallyJsonParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanParse(string fileName, string? contentType = null)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension == ".json")
                return true;

            if (contentType != null)
            {
                return contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public async Task<Result<TallyParsedDataDto>> ParseAsync(Stream fileStream, string fileName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TallyParsedDataDto
            {
                FileName = fileName,
                Format = "json"
            };

            try
            {
                _logger.LogInformation("Starting to parse Tally JSON file: {FileName}", fileName);

                result.FileSize = fileStream.Length;

                var options = new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip,
                    MaxDepth = 100
                };

                using var doc = await JsonDocument.ParseAsync(fileStream, options);
                var root = doc.RootElement;

                // TallyPrime JSON structure
                // Can be: { "ENVELOPE": { ... } } or { "TALLYMESSAGE": [...] } or direct data

                if (root.TryGetProperty("ENVELOPE", out var envelope))
                {
                    ParseEnvelope(envelope, result);
                }
                else if (root.TryGetProperty("TALLYMESSAGE", out var tallyMessage))
                {
                    ParseTallyMessages(tallyMessage, result);
                }
                else if (root.TryGetProperty("BODY", out var body))
                {
                    ParseBody(body, result);
                }
                else
                {
                    // Try to parse root directly as data
                    ParseDirectData(root, result);
                }

                // Calculate summaries
                CalculateSummaries(result);

                // Validate parsed data
                ValidateParsedData(result);

                stopwatch.Stop();
                result.ParseDurationMs = (int)stopwatch.ElapsedMilliseconds;
                result.ParsedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Parsed Tally JSON: {Ledgers} ledgers, {StockItems} stock items, {Vouchers} vouchers in {Duration}ms",
                    result.Masters.Ledgers.Count,
                    result.Masters.StockItems.Count,
                    result.Vouchers.Vouchers.Count,
                    result.ParseDurationMs);

                return Result<TallyParsedDataDto>.Success(result);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error in file {FileName}", fileName);
                return Error.Validation($"Invalid JSON format: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Tally JSON file {FileName}", fileName);
                return Error.Internal($"Failed to parse file: {ex.Message}");
            }
        }

        private void ParseEnvelope(JsonElement envelope, TallyParsedDataDto result)
        {
            if (envelope.TryGetProperty("BODY", out var body))
            {
                ParseBody(body, result);
            }
        }

        private void ParseBody(JsonElement body, TallyParsedDataDto result)
        {
            if (body.TryGetProperty("IMPORTDATA", out var importData) ||
                body.TryGetProperty("DATA", out importData))
            {
                // Extract company info
                if (importData.TryGetProperty("REQUESTDESC", out var requestDesc) &&
                    requestDesc.TryGetProperty("STATICVARIABLES", out var staticVars))
                {
                    result.Masters.TallyCompanyName = GetStringValue(staticVars, "SVCURRENTCOMPANY");
                }

                // Parse request data
                if (importData.TryGetProperty("REQUESTDATA", out var requestData))
                {
                    if (requestData.TryGetProperty("TALLYMESSAGE", out var tallyMessage))
                    {
                        ParseTallyMessages(tallyMessage, result);
                    }
                }
            }
            else if (body.TryGetProperty("TALLYMESSAGE", out var tallyMessage))
            {
                ParseTallyMessages(tallyMessage, result);
            }
        }

        private void ParseTallyMessages(JsonElement messages, TallyParsedDataDto result)
        {
            if (messages.ValueKind == JsonValueKind.Array)
            {
                foreach (var msg in messages.EnumerateArray())
                {
                    ParseSingleTallyMessage(msg, result);
                }
            }
            else if (messages.ValueKind == JsonValueKind.Object)
            {
                ParseSingleTallyMessage(messages, result);
            }
        }

        private void ParseSingleTallyMessage(JsonElement message, TallyParsedDataDto result)
        {
            // Parse Company
            ParseArrayOrObject(message, "COMPANY", el =>
            {
                result.Masters.TallyCompanyName = GetStringValue(el, "NAME") ?? GetStringValue(el, "@NAME");
                result.Masters.TallyCompanyGuid = GetStringValue(el, "GUID") ?? GetStringValue(el, "@GUID");
                result.Masters.FinancialYearFrom = GetStringValue(el, "BOOKSFROM");
                result.Masters.FinancialYearTo = GetStringValue(el, "BOOKSTO");
            });

            // Parse Ledgers
            ParseArrayOrObject(message, "LEDGER", el =>
            {
                var dto = ParseLedger(el);
                if (dto != null)
                    result.Masters.Ledgers.Add(dto);
            });

            // Parse Stock Groups
            ParseArrayOrObject(message, "STOCKGROUP", el =>
            {
                var dto = ParseStockGroup(el);
                if (dto != null)
                    result.Masters.StockGroups.Add(dto);
            });

            // Parse Stock Items
            ParseArrayOrObject(message, "STOCKITEM", el =>
            {
                var dto = ParseStockItem(el);
                if (dto != null)
                    result.Masters.StockItems.Add(dto);
            });

            // Parse Godowns
            ParseArrayOrObject(message, "GODOWN", el =>
            {
                var dto = ParseGodown(el);
                if (dto != null)
                    result.Masters.Godowns.Add(dto);
            });

            // Parse Units
            ParseArrayOrObject(message, "UNIT", el =>
            {
                var dto = ParseUnit(el);
                if (dto != null)
                    result.Masters.Units.Add(dto);
            });

            // Parse Cost Centers
            ParseArrayOrObject(message, "COSTCENTRE", el =>
            {
                var dto = ParseCostCenter(el);
                if (dto != null)
                    result.Masters.CostCenters.Add(dto);
            });

            // Parse Cost Categories
            ParseArrayOrObject(message, "COSTCATEGORY", el =>
            {
                var dto = ParseCostCategory(el);
                if (dto != null)
                    result.Masters.CostCategories.Add(dto);
            });

            // Parse Currencies
            ParseArrayOrObject(message, "CURRENCY", el =>
            {
                var dto = ParseCurrency(el);
                if (dto != null)
                    result.Masters.Currencies.Add(dto);
            });

            // Parse Voucher Types
            ParseArrayOrObject(message, "VOUCHERTYPE", el =>
            {
                var dto = ParseVoucherType(el);
                if (dto != null)
                    result.Masters.VoucherTypes.Add(dto);
            });

            // Parse Vouchers
            ParseArrayOrObject(message, "VOUCHER", el =>
            {
                var dto = ParseVoucher(el);
                if (dto != null)
                    result.Vouchers.Vouchers.Add(dto);
            });
        }

        private void ParseDirectData(JsonElement root, TallyParsedDataDto result)
        {
            // Try to find data at various locations
            foreach (var prop in new[] { "LEDGER", "LEDGERS", "ledgers" })
            {
                ParseArrayOrObject(root, prop, el =>
                {
                    var dto = ParseLedger(el);
                    if (dto != null)
                        result.Masters.Ledgers.Add(dto);
                });
            }

            foreach (var prop in new[] { "STOCKITEM", "STOCKITEMS", "stockItems" })
            {
                ParseArrayOrObject(root, prop, el =>
                {
                    var dto = ParseStockItem(el);
                    if (dto != null)
                        result.Masters.StockItems.Add(dto);
                });
            }

            foreach (var prop in new[] { "VOUCHER", "VOUCHERS", "vouchers" })
            {
                ParseArrayOrObject(root, prop, el =>
                {
                    var dto = ParseVoucher(el);
                    if (dto != null)
                        result.Vouchers.Vouchers.Add(dto);
                });
            }
        }

        private TallyLedgerDto? ParseLedger(JsonElement element)
        {
            try
            {
                return new TallyLedgerDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    Alias = GetStringValue(element, "ALIAS"),
                    LedgerGroup = GetStringValue(element, "PARENT"),
                    IsBillWiseOn = GetBoolValue(element, "ISBILLWISEON"),
                    IsRevenue = GetBoolValue(element, "ISREVENUE"),

                    OpeningBalance = GetDecimalValue(element, "OPENINGBALANCE"),
                    ClosingBalance = GetDecimalValue(element, "CLOSINGBALANCE"),

                    Address = GetStringValue(element, "ADDRESS"),
                    StateName = GetStringValue(element, "LEDSTATENAME", "STATENAME"),
                    CountryName = GetStringValue(element, "COUNTRYNAME"),
                    Pincode = GetStringValue(element, "PINCODE"),
                    Email = GetStringValue(element, "EMAIL"),
                    PhoneNumber = GetStringValue(element, "PHONENUMBER", "LEDGERPHONE"),
                    MobileNumber = GetStringValue(element, "MOBILENUMBER", "LEDGERMOBILE"),

                    Gstin = GetStringValue(element, "GSTIN", "PARTYGSTIN"),
                    GstRegistrationType = GetStringValue(element, "GSTREGISTRATIONTYPE"),
                    StateCode = GetStringValue(element, "STATECODE"),
                    PartyGstin = GetStringValue(element, "PARTYGSTIN"),

                    BankAccountNumber = GetStringValue(element, "BANKACCOUNT", "BANKACCOUNTNUMBER"),
                    IfscCode = GetStringValue(element, "IFSCODE", "IFSCCODE"),
                    BankBranchName = GetStringValue(element, "BANKBRANCHNAME"),

                    PanNumber = GetStringValue(element, "INCOMETAXNUMBER", "PANNUMBER"),

                    CreditLimit = GetNullableDecimalValue(element, "CREDITLIMIT"),
                    CreditDays = GetNullableIntValue(element, "CREDITDAYS")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ledger from JSON");
                return null;
            }
        }

        private TallyStockGroupDto? ParseStockGroup(JsonElement element)
        {
            try
            {
                return new TallyStockGroupDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    Alias = GetStringValue(element, "ALIAS"),
                    IsAddable = GetBoolValue(element, "ISADDABLE", true),
                    BaseUnits = GetStringValue(element, "BASEUNITS"),
                    AdditionalUnits = GetStringValue(element, "ADDITIONALUNITS")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse stock group from JSON");
                return null;
            }
        }

        private TallyStockItemDto? ParseStockItem(JsonElement element)
        {
            try
            {
                return new TallyStockItemDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    Alias = GetStringValue(element, "ALIAS"),
                    PartNumber = GetStringValue(element, "PARTNUMBER"),
                    Description = GetStringValue(element, "DESCRIPTION"),
                    StockGroup = GetStringValue(element, "PARENT"),
                    Category = GetStringValue(element, "CATEGORY"),
                    BaseUnits = GetStringValue(element, "BASEUNITS"),
                    AdditionalUnits = GetStringValue(element, "ADDITIONALUNITS"),
                    Conversion = GetNullableDecimalValue(element, "CONVERSION"),

                    OpeningQuantity = GetDecimalValue(element, "OPENINGBALANCE"),
                    OpeningRate = GetDecimalValue(element, "OPENINGRATE"),
                    OpeningValue = GetDecimalValue(element, "OPENINGVALUE"),

                    ClosingQuantity = GetDecimalValue(element, "CLOSINGBALANCE"),
                    ClosingRate = GetDecimalValue(element, "CLOSINGRATE"),
                    ClosingValue = GetDecimalValue(element, "CLOSINGVALUE"),

                    GstApplicable = GetStringValue(element, "GSTAPPLICABLE"),
                    HsnCode = GetStringValue(element, "HSNCODE"),
                    SacCode = GetStringValue(element, "SACCODE"),
                    GstRate = GetNullableDecimalValue(element, "GSTRATE"),
                    IgstRate = GetNullableDecimalValue(element, "IGSTRATE"),
                    CgstRate = GetNullableDecimalValue(element, "CGSTRATE"),
                    SgstRate = GetNullableDecimalValue(element, "SGSTRATE"),
                    CessRate = GetNullableDecimalValue(element, "CESSRATE"),

                    IsBatchEnabled = GetBoolValue(element, "HASMFGDATE") || GetBoolValue(element, "ISBATCHENABLE"),
                    IsPerishable = GetBoolValue(element, "ISPERISHABLE"),
                    HasExpiryDate = GetBoolValue(element, "HASEXPIRYDATE"),

                    CostingMethod = GetStringValue(element, "COSTINGMETHOD"),
                    StandardCost = GetNullableDecimalValue(element, "STANDARDCOST"),
                    StandardPrice = GetNullableDecimalValue(element, "STANDARDPRICE"),
                    ReorderLevel = GetNullableDecimalValue(element, "REORDERLEVEL"),
                    MinimumOrderQty = GetNullableDecimalValue(element, "MINORDERQTY")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse stock item from JSON");
                return null;
            }
        }

        private TallyGodownDto? ParseGodown(JsonElement element)
        {
            try
            {
                return new TallyGodownDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    Address = GetStringValue(element, "ADDRESS"),
                    IsInternal = GetBoolValue(element, "ISINTERNAL", true),
                    HasNoStock = GetBoolValue(element, "HASNOSTOCK")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse godown from JSON");
                return null;
            }
        }

        private TallyUnitDto? ParseUnit(JsonElement element)
        {
            try
            {
                return new TallyUnitDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Symbol = GetStringValue(element, "SYMBOL") ?? GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    IsSimpleUnit = GetBoolValue(element, "ISSIMPLEUNIT", true),
                    BaseUnits = GetStringValue(element, "BASEUNITS"),
                    AdditionalUnits = GetStringValue(element, "ADDITIONALUNITS"),
                    Conversion = GetNullableDecimalValue(element, "CONVERSION"),
                    DecimalPlaces = GetNullableIntValue(element, "DECIMALPLACES")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse unit from JSON");
                return null;
            }
        }

        private TallyCostCenterDto? ParseCostCenter(JsonElement element)
        {
            try
            {
                return new TallyCostCenterDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    Category = GetStringValue(element, "CATEGORY"),
                    RevenueItem = GetStringValue(element, "REVENUEITEM"),
                    EmailId = GetStringValue(element, "EMAILID")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost center from JSON");
                return null;
            }
        }

        private TallyCostCategoryDto? ParseCostCategory(JsonElement element)
        {
            try
            {
                return new TallyCostCategoryDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    AllocateRevenue = GetBoolValue(element, "ALLOCATEREVENUE"),
                    AllocateNonRevenue = GetBoolValue(element, "ALLOCATENONREVENUE")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost category from JSON");
                return null;
            }
        }

        private TallyCurrencyDto? ParseCurrency(JsonElement element)
        {
            try
            {
                return new TallyCurrencyDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Symbol = GetStringValue(element, "SYMBOL") ?? string.Empty,
                    FormalName = GetStringValue(element, "FORMALNAME"),
                    IsoCode = GetStringValue(element, "MAILINGNAME") ?? GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    DecimalPlaces = GetNullableIntValue(element, "DECIMALPLACES") ?? 2,
                    ExchangeRate = GetNullableDecimalValue(element, "EXCHANGERATE")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse currency from JSON");
                return null;
            }
        }

        private TallyVoucherTypeDto? ParseVoucherType(JsonElement element)
        {
            try
            {
                return new TallyVoucherTypeDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    Name = GetStringValue(element, "NAME", "@NAME") ?? string.Empty,
                    Parent = GetStringValue(element, "PARENT"),
                    NumberingMethod = GetStringValue(element, "NUMBERINGMETHOD"),
                    UseForPoS = GetBoolValue(element, "USEFORPOS"),
                    UseForJobwork = GetBoolValue(element, "USEFORJOBWORK"),
                    IsActive = GetBoolValue(element, "ISACTIVE", true)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse voucher type from JSON");
                return null;
            }
        }

        private TallyVoucherDto? ParseVoucher(JsonElement element)
        {
            try
            {
                var dto = new TallyVoucherDto
                {
                    Guid = GetStringValue(element, "GUID", "@GUID") ?? string.Empty,
                    VoucherNumber = GetStringValue(element, "VOUCHERNUMBER") ?? string.Empty,
                    VoucherType = GetStringValue(element, "VOUCHERTYPENAME", "@VOUCHERTYPENAME") ?? string.Empty,
                    VoucherTypeName = GetStringValue(element, "VOUCHERTYPENAME"),
                    Date = ParseTallyDate(GetStringValue(element, "DATE")),
                    ReferenceNumber = GetStringValue(element, "REFERENCE"),
                    ReferenceDate = GetStringValue(element, "REFERENCEDATE"),
                    Narration = GetStringValue(element, "NARRATION"),

                    PartyLedgerName = GetStringValue(element, "PARTYLEDGERNAME"),
                    PartyLedgerGuid = GetStringValue(element, "PARTYLEDGERGUID"),

                    Amount = Math.Abs(GetDecimalValue(element, "AMOUNT")),
                    Currency = GetStringValue(element, "CURRENCYNAME"),
                    ExchangeRate = GetNullableDecimalValue(element, "EXCHANGERATE"),

                    PlaceOfSupply = GetStringValue(element, "PLACEOFSUPPLY"),
                    IsReverseCharge = GetBoolValue(element, "ISREVERSECHARGEAPPLICABLE"),
                    GstinOfParty = GetStringValue(element, "PARTYGSTIN"),

                    EInvoiceIrn = GetStringValue(element, "IRN"),
                    EWayBillNumber = GetStringValue(element, "EWAYBILLNO"),

                    IsCancelled = GetBoolValue(element, "ISCANCELLED"),
                    IsOptional = GetBoolValue(element, "ISOPTIONAL"),
                    IsPostDated = GetBoolValue(element, "ISPOSTDATED")
                };

                // Parse ledger entries
                ParseArrayOrObject(element, "ALLLEDGERENTRIES.LIST", entry =>
                {
                    var ledgerEntry = ParseLedgerEntry(entry);
                    if (ledgerEntry != null)
                        dto.LedgerEntries.Add(ledgerEntry);
                });

                ParseArrayOrObject(element, "LEDGERENTRIES.LIST", entry =>
                {
                    var ledgerEntry = ParseLedgerEntry(entry);
                    if (ledgerEntry != null)
                        dto.LedgerEntries.Add(ledgerEntry);
                });

                // Parse inventory entries
                ParseArrayOrObject(element, "ALLINVENTORYENTRIES.LIST", entry =>
                {
                    var invEntry = ParseInventoryEntry(entry);
                    if (invEntry != null)
                        dto.InventoryEntries.Add(invEntry);
                });

                ParseArrayOrObject(element, "INVENTORYENTRIES.LIST", entry =>
                {
                    var invEntry = ParseInventoryEntry(entry);
                    if (invEntry != null)
                        dto.InventoryEntries.Add(invEntry);
                });

                // Parse bill allocations
                ParseArrayOrObject(element, "BILLALLOCATIONS.LIST", alloc =>
                {
                    var billAlloc = ParseBillAllocation(alloc);
                    if (billAlloc != null)
                        dto.BillAllocations.Add(billAlloc);
                });

                // Parse cost allocations
                ParseArrayOrObject(element, "CATEGORYALLOCATIONS.LIST", catAlloc =>
                {
                    var allocs = ParseCostAllocations(catAlloc);
                    dto.CostAllocations.AddRange(allocs);
                });

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse voucher from JSON");
                return null;
            }
        }

        private TallyLedgerEntryDto? ParseLedgerEntry(JsonElement element)
        {
            try
            {
                var entry = new TallyLedgerEntryDto
                {
                    LedgerName = GetStringValue(element, "LEDGERNAME") ?? string.Empty,
                    LedgerGuid = GetStringValue(element, "LEDGERGUID"),
                    Amount = GetDecimalValue(element, "AMOUNT"),

                    CgstAmount = GetNullableDecimalValue(element, "CGSTAMOUNT"),
                    SgstAmount = GetNullableDecimalValue(element, "SGSTAMOUNT"),
                    IgstAmount = GetNullableDecimalValue(element, "IGSTAMOUNT"),
                    CessAmount = GetNullableDecimalValue(element, "CESSAMOUNT"),

                    TdsAmount = GetNullableDecimalValue(element, "TDSAMOUNT"),
                    TdsSection = GetStringValue(element, "TDSSECTION"),
                    TdsRate = GetNullableDecimalValue(element, "TDSRATE")
                };

                // Parse nested bill allocations
                ParseArrayOrObject(element, "BILLALLOCATIONS.LIST", alloc =>
                {
                    var billAlloc = ParseBillAllocation(alloc);
                    if (billAlloc != null)
                        entry.BillAllocations.Add(billAlloc);
                });

                // Parse nested cost allocations
                ParseArrayOrObject(element, "CATEGORYALLOCATIONS.LIST", catAlloc =>
                {
                    var allocs = ParseCostAllocations(catAlloc);
                    entry.CostAllocations.AddRange(allocs);
                });

                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ledger entry from JSON");
                return null;
            }
        }

        private TallyInventoryEntryDto? ParseInventoryEntry(JsonElement element)
        {
            try
            {
                var entry = new TallyInventoryEntryDto
                {
                    StockItemName = GetStringValue(element, "STOCKITEMNAME") ?? string.Empty,
                    StockItemGuid = GetStringValue(element, "STOCKITEMGUID"),
                    Quantity = GetDecimalValue(element, "ACTUALQTY", "BILLEDQTY"),
                    Unit = GetStringValue(element, "UNIT"),
                    ActualQuantity = GetNullableDecimalValue(element, "ACTUALQTY"),
                    BilledQuantity = GetNullableDecimalValue(element, "BILLEDQTY"),
                    Rate = GetDecimalValue(element, "RATE"),
                    Amount = Math.Abs(GetDecimalValue(element, "AMOUNT")),
                    Discount = GetNullableDecimalValue(element, "DISCOUNT"),
                    DiscountType = GetStringValue(element, "DISCOUNTTYPE"),
                    GodownName = GetStringValue(element, "GODOWNNAME"),
                    GodownGuid = GetStringValue(element, "GODOWNGUID"),
                    DestinationGodownName = GetStringValue(element, "DESTINATIONGODOWNNAME"),
                    DestinationGodownGuid = GetStringValue(element, "DESTINATIONGODOWNGUID"),

                    HsnCode = GetStringValue(element, "HSNCODE"),
                    GstRate = GetNullableDecimalValue(element, "GSTRATE"),
                    CgstAmount = GetNullableDecimalValue(element, "CGSTAMOUNT"),
                    SgstAmount = GetNullableDecimalValue(element, "SGSTAMOUNT"),
                    IgstAmount = GetNullableDecimalValue(element, "IGSTAMOUNT"),
                    CessAmount = GetNullableDecimalValue(element, "CESSAMOUNT"),

                    OrderNumber = GetStringValue(element, "ORDERNUMBER"),
                    OrderDate = ParseTallyDateOrNull(GetStringValue(element, "ORDERDATE"))
                };

                // Parse batch allocations
                ParseArrayOrObject(element, "BATCHALLOCATIONS.LIST", batch =>
                {
                    var batchAlloc = ParseBatchAllocation(batch);
                    if (batchAlloc != null)
                        entry.BatchAllocations.Add(batchAlloc);
                });

                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse inventory entry from JSON");
                return null;
            }
        }

        private TallyBillAllocationDto? ParseBillAllocation(JsonElement element)
        {
            try
            {
                return new TallyBillAllocationDto
                {
                    Name = GetStringValue(element, "NAME") ?? string.Empty,
                    BillType = GetStringValue(element, "BILLTYPE") ?? string.Empty,
                    Amount = GetDecimalValue(element, "AMOUNT"),
                    BillDate = ParseTallyDateOrNull(GetStringValue(element, "BILLDATE")),
                    DueDate = ParseTallyDateOrNull(GetStringValue(element, "DUEDATE")),
                    BillCreditPeriod = GetStringValue(element, "BILLCREDITPERIOD")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse bill allocation from JSON");
                return null;
            }
        }

        private List<TallyCostAllocationDto> ParseCostAllocations(JsonElement categoryElement)
        {
            var result = new List<TallyCostAllocationDto>();
            try
            {
                var categoryName = GetStringValue(categoryElement, "CATEGORY");

                ParseArrayOrObject(categoryElement, "COSTCENTREALLOCATIONS.LIST", costCenter =>
                {
                    result.Add(new TallyCostAllocationDto
                    {
                        CostCenterName = GetStringValue(costCenter, "NAME") ?? string.Empty,
                        CostCenterGuid = GetStringValue(costCenter, "GUID"),
                        CostCategoryName = categoryName,
                        Amount = GetDecimalValue(costCenter, "AMOUNT")
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost allocations from JSON");
            }
            return result;
        }

        private TallyBatchAllocationDto? ParseBatchAllocation(JsonElement element)
        {
            try
            {
                return new TallyBatchAllocationDto
                {
                    BatchName = GetStringValue(element, "BATCHNAME", "NAME") ?? string.Empty,
                    BatchGuid = GetStringValue(element, "BATCHGUID"),
                    GodownName = GetStringValue(element, "GODOWNNAME"),
                    Quantity = GetDecimalValue(element, "QUANTITY"),
                    Rate = GetDecimalValue(element, "RATE"),
                    Amount = GetDecimalValue(element, "AMOUNT"),
                    ManufacturingDate = ParseTallyDateOrNull(GetStringValue(element, "MFGDATE")),
                    ExpiryDate = ParseTallyDateOrNull(GetStringValue(element, "EXPIRYDATE"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse batch allocation from JSON");
                return null;
            }
        }

        private void CalculateSummaries(TallyParsedDataDto result)
        {
            // Ledger counts by group
            var ledgerGroups = result.Masters.Ledgers
                .Where(l => !string.IsNullOrEmpty(l.LedgerGroup))
                .GroupBy(l => l.LedgerGroup!)
                .ToDictionary(g => g.Key, g => g.Count());
            result.Masters.LedgerCountsByGroup = ledgerGroups;

            // Voucher summaries
            var vouchers = result.Vouchers;

            foreach (var v in result.Vouchers.Vouchers)
            {
                var type = v.VoucherType?.ToLower() ?? "other";

                if (!vouchers.CountsByVoucherType.ContainsKey(type))
                    vouchers.CountsByVoucherType[type] = 0;
                vouchers.CountsByVoucherType[type]++;

                if (!vouchers.AmountsByVoucherType.ContainsKey(type))
                    vouchers.AmountsByVoucherType[type] = 0;
                vouchers.AmountsByVoucherType[type] += v.Amount;

                switch (v.VoucherType?.ToLower())
                {
                    case "sales":
                        vouchers.SalesCount++;
                        vouchers.TotalSalesAmount += v.Amount;
                        break;
                    case "purchase":
                        vouchers.PurchaseCount++;
                        vouchers.TotalPurchaseAmount += v.Amount;
                        break;
                    case "receipt":
                        vouchers.ReceiptCount++;
                        vouchers.TotalReceiptAmount += v.Amount;
                        break;
                    case "payment":
                        vouchers.PaymentCount++;
                        vouchers.TotalPaymentAmount += v.Amount;
                        break;
                    case "journal":
                        vouchers.JournalCount++;
                        break;
                    case "contra":
                        vouchers.ContraCount++;
                        break;
                    case "credit note":
                        vouchers.CreditNoteCount++;
                        break;
                    case "debit note":
                        vouchers.DebitNoteCount++;
                        break;
                    case "stock journal":
                        vouchers.StockJournalCount++;
                        break;
                    case "physical stock":
                        vouchers.PhysicalStockCount++;
                        break;
                    case "delivery note":
                        vouchers.DeliveryNoteCount++;
                        break;
                    case "receipt note":
                        vouchers.ReceiptNoteCount++;
                        break;
                    default:
                        vouchers.OtherCount++;
                        break;
                }

                if (vouchers.MinDate == null || v.Date < vouchers.MinDate)
                    vouchers.MinDate = v.Date;
                if (vouchers.MaxDate == null || v.Date > vouchers.MaxDate)
                    vouchers.MaxDate = v.Date;
            }
        }

        private void ValidateParsedData(TallyParsedDataDto result)
        {
            if (result.Masters.Ledgers.Count == 0 && result.Vouchers.Vouchers.Count == 0)
            {
                result.ValidationIssues.Add(new TallyValidationIssueDto
                {
                    Severity = "warning",
                    Code = "EMPTY_DATA",
                    Message = "No ledgers or vouchers found in the file"
                });
            }

            var noGuidLedgers = result.Masters.Ledgers.Where(l => string.IsNullOrEmpty(l.Guid)).ToList();
            if (noGuidLedgers.Any())
            {
                result.ValidationIssues.Add(new TallyValidationIssueDto
                {
                    Severity = "warning",
                    Code = "MISSING_GUID",
                    Message = $"{noGuidLedgers.Count} ledger(s) have no GUID and may cause issues with incremental sync"
                });
            }

            var emptyVouchers = result.Vouchers.Vouchers.Where(v => v.LedgerEntries.Count == 0).ToList();
            if (emptyVouchers.Any())
            {
                result.ValidationIssues.Add(new TallyValidationIssueDto
                {
                    Severity = "warning",
                    Code = "EMPTY_VOUCHERS",
                    Message = $"{emptyVouchers.Count} voucher(s) have no ledger entries"
                });
            }

            foreach (var voucher in result.Vouchers.Vouchers)
            {
                var totalAmount = voucher.LedgerEntries.Sum(e => e.Amount);
                if (Math.Abs(totalAmount) > 0.01m)
                {
                    result.ValidationIssues.Add(new TallyValidationIssueDto
                    {
                        Severity = "error",
                        Code = "UNBALANCED_VOUCHER",
                        Message = $"Voucher {voucher.VoucherNumber} is unbalanced by {totalAmount:N2}",
                        RecordType = "voucher",
                        RecordName = voucher.VoucherNumber,
                        RecordGuid = voucher.Guid
                    });
                }
            }
        }

        // Helper methods
        private static void ParseArrayOrObject(JsonElement parent, string propertyName, Action<JsonElement> action)
        {
            if (!parent.TryGetProperty(propertyName, out var prop))
                return;

            if (prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    action(item);
                }
            }
            else if (prop.ValueKind == JsonValueKind.Object)
            {
                action(prop);
            }
        }

        private static string? GetStringValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.String)
                    {
                        var value = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            return value.Trim();
                    }
                    else if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.ToString();
                    }
                }
            }
            return null;
        }

        private static bool GetBoolValue(JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return defaultValue;

            return prop.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => prop.GetString()?.Equals("Yes", StringComparison.OrdinalIgnoreCase) == true ||
                                         prop.GetString()?.Equals("True", StringComparison.OrdinalIgnoreCase) == true ||
                                         prop.GetString() == "1",
                JsonValueKind.Number => prop.GetInt32() != 0,
                _ => defaultValue
            };
        }

        private static decimal GetDecimalValue(JsonElement element, params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                if (element.TryGetProperty(name, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Number)
                    {
                        return prop.GetDecimal();
                    }
                    else if (prop.ValueKind == JsonValueKind.String)
                    {
                        var str = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            // Handle Tally amount format
                            str = str.Replace("₹", "").Replace("Rs.", "").Replace("Rs", "").Trim();
                            if (str.EndsWith(" Dr", StringComparison.OrdinalIgnoreCase) ||
                                str.EndsWith(" Cr", StringComparison.OrdinalIgnoreCase))
                                str = str.Substring(0, str.Length - 3).Trim();

                            if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                                return result;
                        }
                    }
                }
            }
            return 0;
        }

        private static decimal? GetNullableDecimalValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();

            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return null;
                if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                    return result;
            }

            return null;
        }

        private static int? GetNullableIntValue(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;

            if (prop.ValueKind == JsonValueKind.Null)
                return null;

            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetInt32();

            if (prop.ValueKind == JsonValueKind.String)
            {
                var str = prop.GetString();
                if (string.IsNullOrWhiteSpace(str))
                    return null;
                if (int.TryParse(str, out var result))
                    return result;
            }

            return null;
        }

        private static DateOnly ParseTallyDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateOnly.FromDateTime(DateTime.Today);

            // YYYYMMDD format
            if (value.Length == 8 &&
                int.TryParse(value.Substring(0, 4), out var year) &&
                int.TryParse(value.Substring(4, 2), out var month) &&
                int.TryParse(value.Substring(6, 2), out var day))
            {
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch
                {
                    // Invalid date
                }
            }

            if (DateTime.TryParse(value, out var dateTime))
                return DateOnly.FromDateTime(dateTime);

            return DateOnly.FromDateTime(DateTime.Today);
        }

        private static DateOnly? ParseTallyDateOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var date = ParseTallyDate(value);
            return date == DateOnly.FromDateTime(DateTime.Today) ? null : date;
        }
    }
}
