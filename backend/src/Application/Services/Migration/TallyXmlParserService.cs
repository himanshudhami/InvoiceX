using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Application.DTOs.Migration;
using Application.Interfaces.Migration;
using Core.Common;
using Microsoft.Extensions.Logging;

namespace Application.Services.Migration
{
    /// <summary>
    /// Parser for Tally XML export files
    /// Handles both UTF-8 and UTF-16 encoded files from Tally
    /// </summary>
    public class TallyXmlParserService : ITallyParserService
    {
        private readonly ILogger<TallyXmlParserService> _logger;

        public string Format => "xml";

        public TallyXmlParserService(ILogger<TallyXmlParserService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool CanParse(string fileName, string? contentType = null)
        {
            if (string.IsNullOrEmpty(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension == ".xml")
                return true;

            if (contentType != null)
            {
                return contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public async Task<Result<TallyParsedDataDto>> ParseAsync(Stream fileStream, string fileName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new TallyParsedDataDto
            {
                FileName = fileName,
                Format = "xml"
            };

            try
            {
                _logger.LogInformation("Starting to parse Tally XML file: {FileName}", fileName);

                // Get file size
                result.FileSize = fileStream.Length;

                // Detect encoding and read content
                var (content, encoding) = await DetectEncodingAndReadAsync(fileStream);
                _logger.LogInformation("Detected encoding: {Encoding}", encoding.EncodingName);

                // Load XML document from string
                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    MaxCharactersFromEntities = 1024 * 1024 * 100, // 100MB limit
                    Async = true // Required for XDocument.LoadAsync
                };

                XDocument doc;
                using (var stringReader = new StringReader(content))
                using (var reader = XmlReader.Create(stringReader, settings))
                {
                    doc = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);
                }

                // Parse envelope structure
                var envelope = doc.Element("ENVELOPE");
                if (envelope == null)
                {
                    // Try alternate structure (direct TALLYMESSAGE)
                    var tallyMessage = doc.Element("TALLYMESSAGE");
                    if (tallyMessage != null)
                    {
                        ParseTallyMessage(tallyMessage, result);
                    }
                    else
                    {
                        return Error.Validation("Invalid Tally XML format: Missing ENVELOPE or TALLYMESSAGE element");
                    }
                }
                else
                {
                    // Standard Tally export format
                    var body = envelope.Element("BODY");
                    var importData = body?.Element("IMPORTDATA") ?? body?.Element("DATA");

                    if (importData != null)
                    {
                        // Extract company info from static variables
                        var staticVars = importData.Element("REQUESTDESC")?.Element("STATICVARIABLES");
                        if (staticVars != null)
                        {
                            result.Masters.TallyCompanyName = GetElementValue(staticVars, "SVCURRENTCOMPANY");
                        }

                        // Parse request data
                        var requestData = importData.Element("REQUESTDATA");
                        if (requestData != null)
                        {
                            var tallyMessages = requestData.Elements("TALLYMESSAGE");
                            foreach (var msg in tallyMessages)
                            {
                                ParseTallyMessage(msg, result);
                            }
                        }
                    }
                    else
                    {
                        // Try body directly
                        var tallyMessages = body?.Elements("TALLYMESSAGE") ?? envelope.Elements("TALLYMESSAGE");
                        foreach (var msg in tallyMessages)
                        {
                            ParseTallyMessage(msg, result);
                        }
                    }
                }

                // Calculate summaries
                CalculateSummaries(result);

                // Validate parsed data
                ValidateParsedData(result);

                stopwatch.Stop();
                result.ParseDurationMs = (int)stopwatch.ElapsedMilliseconds;
                result.ParsedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Parsed Tally XML: {Ledgers} ledgers, {StockItems} stock items, {Vouchers} vouchers in {Duration}ms",
                    result.Masters.Ledgers.Count,
                    result.Masters.StockItems.Count,
                    result.Vouchers.Vouchers.Count,
                    result.ParseDurationMs);

                return Result<TallyParsedDataDto>.Success(result);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "XML parsing error in file {FileName}", fileName);
                return Error.Validation($"Invalid XML format: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Tally XML file {FileName}", fileName);
                return Error.Internal($"Failed to parse file: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects encoding (UTF-8, UTF-16 LE/BE) and reads the content
        /// </summary>
        private async Task<(string Content, Encoding Encoding)> DetectEncodingAndReadAsync(Stream stream)
        {
            // Reset stream position
            stream.Position = 0;

            // Read BOM to detect encoding
            var bom = new byte[4];
            await stream.ReadAsync(bom, 0, 4);
            stream.Position = 0;

            Encoding encoding;

            // Check for BOM
            if (bom[0] == 0xFF && bom[1] == 0xFE)
            {
                // UTF-16 LE (Little Endian) - common in Tally exports
                encoding = Encoding.Unicode;
                _logger.LogInformation("Detected UTF-16 LE encoding (BOM: FF FE)");
            }
            else if (bom[0] == 0xFE && bom[1] == 0xFF)
            {
                // UTF-16 BE (Big Endian)
                encoding = Encoding.BigEndianUnicode;
                _logger.LogInformation("Detected UTF-16 BE encoding (BOM: FE FF)");
            }
            else if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
            {
                // UTF-8 with BOM
                encoding = Encoding.UTF8;
                _logger.LogInformation("Detected UTF-8 encoding (BOM: EF BB BF)");
            }
            else
            {
                // Default to UTF-8
                encoding = Encoding.UTF8;
                _logger.LogInformation("No BOM detected, defaulting to UTF-8");
            }

            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync();

            // Sanitize invalid XML characters (common in Tally exports)
            content = SanitizeXmlContent(content);

            return (content, encoding);
        }

        /// <summary>
        /// Removes invalid XML characters that Tally sometimes includes in exports.
        /// Handles both literal invalid characters AND XML entity references to invalid characters.
        /// Valid XML chars: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]
        /// </summary>
        private string SanitizeXmlContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Remove XML numeric character references to invalid characters
            // Matches &#0; through &#31; (decimal) and &#x0; through &#x1F; (hex)
            // But preserves &#9; &#10; &#13; (tab, LF, CR) and their hex equivalents
            content = System.Text.RegularExpressions.Regex.Replace(
                content,
                @"&#(x?)([0-9a-fA-F]+);?",
                match =>
                {
                    var isHex = !string.IsNullOrEmpty(match.Groups[1].Value);
                    var numStr = match.Groups[2].Value;

                    int charCode;
                    if (isHex)
                    {
                        if (!int.TryParse(numStr, System.Globalization.NumberStyles.HexNumber, null, out charCode))
                            return match.Value; // Keep if can't parse
                    }
                    else
                    {
                        if (!int.TryParse(numStr, out charCode))
                            return match.Value; // Keep if can't parse
                    }

                    // Check if it's a valid XML character
                    if (charCode == 0x9 || charCode == 0xA || charCode == 0xD ||
                        (charCode >= 0x20 && charCode <= 0xD7FF) ||
                        (charCode >= 0xE000 && charCode <= 0xFFFD))
                    {
                        return match.Value; // Keep valid references
                    }

                    _logger.LogDebug("Removing invalid XML entity reference: {Entity}", match.Value);
                    return ""; // Remove invalid references
                });

            // Then remove any literal invalid characters
            var sb = new StringBuilder(content.Length);
            var invalidCharsRemoved = 0;

            foreach (var c in content)
            {
                // Valid XML 1.0 characters
                if (c == 0x9 || c == 0xA || c == 0xD ||
                    (c >= 0x20 && c <= 0xD7FF) ||
                    (c >= 0xE000 && c <= 0xFFFD))
                {
                    sb.Append(c);
                }
                else
                {
                    invalidCharsRemoved++;
                }
            }

            if (invalidCharsRemoved > 0)
            {
                _logger.LogWarning("Removed {Count} invalid literal XML characters from Tally export", invalidCharsRemoved);
            }

            return sb.ToString();
        }

        private void ParseTallyMessage(XElement message, TallyParsedDataDto result)
        {
            // Parse Company
            foreach (var company in message.Elements("COMPANY"))
            {
                result.Masters.TallyCompanyName = GetAttributeOrElement(company, "NAME");
                result.Masters.TallyCompanyGuid = GetAttributeOrElement(company, "GUID");

                var booksFrom = GetElementValue(company, "BOOKSFROM");
                var booksTo = GetElementValue(company, "BOOKSTO");

                result.Masters.FinancialYearFrom = booksFrom;
                result.Masters.FinancialYearTo = booksTo;
            }

            // Parse Groups (Ledger groups hierarchy)
            foreach (var group in message.Elements("GROUP"))
            {
                // Groups are parent categories for ledgers
                // We can store these for reference but don't need separate entity
                var groupName = GetAttributeOrElement(group, "NAME");
                var parentGroup = GetElementValue(group, "PARENT");
                _logger.LogDebug("Found group: {GroupName} under {Parent}", groupName, parentGroup);
            }

            // Parse Ledgers
            foreach (var ledger in message.Elements("LEDGER"))
            {
                var ledgerDto = ParseLedger(ledger);
                if (ledgerDto != null)
                    result.Masters.Ledgers.Add(ledgerDto);
            }

            // Parse Stock Groups
            foreach (var stockGroup in message.Elements("STOCKGROUP"))
            {
                var dto = ParseStockGroup(stockGroup);
                if (dto != null)
                    result.Masters.StockGroups.Add(dto);
            }

            // Parse Stock Items
            foreach (var stockItem in message.Elements("STOCKITEM"))
            {
                var dto = ParseStockItem(stockItem);
                if (dto != null)
                    result.Masters.StockItems.Add(dto);
            }

            // Parse Godowns
            foreach (var godown in message.Elements("GODOWN"))
            {
                var dto = ParseGodown(godown);
                if (dto != null)
                    result.Masters.Godowns.Add(dto);
            }

            // Parse Units
            foreach (var unit in message.Elements("UNIT"))
            {
                var dto = ParseUnit(unit);
                if (dto != null)
                    result.Masters.Units.Add(dto);
            }

            // Parse Cost Centers
            foreach (var costCenter in message.Elements("COSTCENTRE"))
            {
                var dto = ParseCostCenter(costCenter);
                if (dto != null)
                    result.Masters.CostCenters.Add(dto);
            }

            // Parse Cost Categories
            foreach (var costCategory in message.Elements("COSTCATEGORY"))
            {
                var dto = ParseCostCategory(costCategory);
                if (dto != null)
                    result.Masters.CostCategories.Add(dto);
            }

            // Parse Currencies
            foreach (var currency in message.Elements("CURRENCY"))
            {
                var dto = ParseCurrency(currency);
                if (dto != null)
                    result.Masters.Currencies.Add(dto);
            }

            // Parse Voucher Types
            foreach (var voucherType in message.Elements("VOUCHERTYPE"))
            {
                var dto = ParseVoucherType(voucherType);
                if (dto != null)
                    result.Masters.VoucherTypes.Add(dto);
            }

            // Parse Vouchers
            foreach (var voucher in message.Elements("VOUCHER"))
            {
                var dto = ParseVoucher(voucher);
                if (dto != null)
                    result.Vouchers.Vouchers.Add(dto);
            }
        }

        private TallyLedgerDto? ParseLedger(XElement element)
        {
            try
            {
                var dto = new TallyLedgerDto
                {
                    // GUID can be in GUID element or REMOTEID attribute
                    Guid = GetElementValue(element, "GUID") ?? GetAttributeOrElement(element, "REMOTEID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    Alias = GetElementValue(element, "MAILINGNAME"),
                    LedgerGroup = GetElementValue(element, "PARENT"),
                    IsBillWiseOn = GetBoolValue(element, "ISBILLWISEON"),
                    IsRevenue = GetBoolValue(element, "ISREVENUE"),

                    // Balance
                    OpeningBalance = ParseAmount(GetElementValue(element, "OPENINGBALANCE")),
                    ClosingBalance = ParseAmount(GetElementValue(element, "CLOSINGBALANCE")),

                    // Contact - Tally uses OLDADDRESS.LIST for address lines
                    Address = ParseOldAddressList(element),
                    StateName = GetElementValue(element, "PRIORSTATENAME")
                             ?? GetElementValue(element, "OLDLEDSTATENAME")
                             ?? GetElementValue(element, "LEDSTATENAME")
                             ?? GetElementValue(element, "STATENAME"),
                    CountryName = GetElementValue(element, "COUNTRYNAME") ?? GetElementValue(element, "COUNTRYOFRESIDENCE"),
                    Pincode = GetElementValue(element, "OLDPINCODE") ?? GetElementValue(element, "PINCODE"),
                    Email = GetElementValue(element, "EMAIL") ?? GetElementValue(element, "EMAILCC") ?? GetElementValue(element, "LEDGEREMAIL"),
                    PhoneNumber = GetElementValue(element, "LEDGERPHONE") ?? GetElementValue(element, "PHONENUMBER"),
                    MobileNumber = GetElementValue(element, "LEDGERMOBILE") ?? GetElementValue(element, "MOBILENUMBER"),

                    // GST - Tally uses PARTYGSTIN for GSTIN
                    Gstin = GetElementValue(element, "PARTYGSTIN") ?? GetElementValue(element, "GSTIN"),
                    GstRegistrationType = GetElementValue(element, "GSTREGISTRATIONTYPE"),
                    StateCode = GetElementValue(element, "STATECODE"),
                    PartyGstin = GetElementValue(element, "PARTYGSTIN"),

                    // Bank - Tally uses BANKDETAILS for account number and IFSCODE for IFSC
                    BankAccountNumber = GetElementValue(element, "BANKDETAILS")
                                     ?? GetElementValue(element, "BANKACCOUNT")
                                     ?? GetElementValue(element, "BANKACCOUNTNUMBER"),
                    IfscCode = GetElementValue(element, "IFSCODE") ?? GetElementValue(element, "IFSCCODE"),
                    BankBranchName = GetElementValue(element, "BANKBRANCHNAME"),

                    // PAN - Tally uses INCOMETAXNUMBER for PAN
                    PanNumber = GetElementValue(element, "INCOMETAXNUMBER") ?? GetElementValue(element, "PANNUMBER"),

                    // Credit
                    CreditLimit = ParseDecimalOrNull(GetElementValue(element, "CREDITLIMIT")),
                    CreditDays = ParseIntOrNull(GetElementValue(element, "CREDITDAYS"))
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ledger: {Name}", GetAttributeOrElement(element, "NAME"));
                return null;
            }
        }

        /// <summary>
        /// Parses OLDADDRESS.LIST element which contains address lines
        /// </summary>
        private string? ParseOldAddressList(XElement element)
        {
            // Try OLDADDRESS.LIST first (common in Tally exports)
            var addressList = element.Element("OLDADDRESS.LIST");
            if (addressList != null)
            {
                var addressLines = addressList.Elements("OLDADDRESS")
                    .Select(e => e.Value.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                if (addressLines.Any())
                    return string.Join(", ", addressLines);
            }

            // Try ADDRESS.LIST
            var addressList2 = element.Element("ADDRESS.LIST");
            if (addressList2 != null)
            {
                var addressLines = addressList2.Elements("ADDRESS")
                    .Select(e => e.Value.Trim())
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                if (addressLines.Any())
                    return string.Join(", ", addressLines);
            }

            // Fallback to single ADDRESS element
            return GetElementValue(element, "ADDRESS");
        }

        private TallyStockGroupDto? ParseStockGroup(XElement element)
        {
            try
            {
                return new TallyStockGroupDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    Alias = GetElementValue(element, "ALIAS"),
                    IsAddable = GetBoolValue(element, "ISADDABLE", true),
                    BaseUnits = GetElementValue(element, "BASEUNITS"),
                    AdditionalUnits = GetElementValue(element, "ADDITIONALUNITS")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse stock group");
                return null;
            }
        }

        private TallyStockItemDto? ParseStockItem(XElement element)
        {
            try
            {
                return new TallyStockItemDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    Alias = GetElementValue(element, "ALIAS"),
                    PartNumber = GetElementValue(element, "PARTNUMBER"),
                    Description = GetElementValue(element, "DESCRIPTION"),
                    StockGroup = GetElementValue(element, "PARENT"),
                    Category = GetElementValue(element, "CATEGORY"),
                    BaseUnits = GetElementValue(element, "BASEUNITS"),
                    AdditionalUnits = GetElementValue(element, "ADDITIONALUNITS"),
                    Conversion = ParseDecimalOrNull(GetElementValue(element, "CONVERSION")),

                    // Opening
                    OpeningQuantity = ParseQuantity(GetElementValue(element, "OPENINGBALANCE")),
                    OpeningRate = ParseRate(GetElementValue(element, "OPENINGRATE")),
                    OpeningValue = ParseAmount(GetElementValue(element, "OPENINGVALUE")),

                    // Closing
                    ClosingQuantity = ParseQuantity(GetElementValue(element, "CLOSINGBALANCE")),
                    ClosingRate = ParseRate(GetElementValue(element, "CLOSINGRATE")),
                    ClosingValue = ParseAmount(GetElementValue(element, "CLOSINGVALUE")),

                    // GST
                    GstApplicable = GetElementValue(element, "GSTAPPLICABLE"),
                    HsnCode = GetElementValue(element, "HSNCODE"),
                    SacCode = GetElementValue(element, "SACCODE"),
                    GstRate = ParseDecimalOrNull(GetElementValue(element, "GSTRATE")),
                    IgstRate = ParseDecimalOrNull(GetElementValue(element, "IGSTRATE")),
                    CgstRate = ParseDecimalOrNull(GetElementValue(element, "CGSTRATE")),
                    SgstRate = ParseDecimalOrNull(GetElementValue(element, "SGSTRATE")),
                    CessRate = ParseDecimalOrNull(GetElementValue(element, "CESSRATE")),

                    // Tracking
                    IsBatchEnabled = GetBoolValue(element, "HASMFGDATE") || GetBoolValue(element, "ISBATCHENABLE"),
                    IsPerishable = GetBoolValue(element, "ISPERISHABLE"),
                    HasExpiryDate = GetBoolValue(element, "HASEXPIRYDATE"),

                    // Valuation
                    CostingMethod = GetElementValue(element, "COSTINGMETHOD"),
                    StandardCost = ParseDecimalOrNull(GetElementValue(element, "STANDARDCOST")),
                    StandardPrice = ParseDecimalOrNull(GetElementValue(element, "STANDARDPRICE")),
                    ReorderLevel = ParseDecimalOrNull(GetElementValue(element, "REORDERLEVEL")),
                    MinimumOrderQty = ParseDecimalOrNull(GetElementValue(element, "MINORDERQTY"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse stock item");
                return null;
            }
        }

        private TallyGodownDto? ParseGodown(XElement element)
        {
            try
            {
                return new TallyGodownDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    Address = GetElementValue(element, "ADDRESS"),
                    IsInternal = GetBoolValue(element, "ISINTERNAL", true),
                    HasNoStock = GetBoolValue(element, "HASNOSTOCK")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse godown");
                return null;
            }
        }

        private TallyUnitDto? ParseUnit(XElement element)
        {
            try
            {
                return new TallyUnitDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Symbol = GetElementValue(element, "SYMBOL") ?? GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    IsSimpleUnit = GetBoolValue(element, "ISSIMPLEUNIT", true),
                    BaseUnits = GetElementValue(element, "BASEUNITS"),
                    AdditionalUnits = GetElementValue(element, "ADDITIONALUNITS"),
                    Conversion = ParseDecimalOrNull(GetElementValue(element, "CONVERSION")),
                    DecimalPlaces = ParseIntOrNull(GetElementValue(element, "DECIMALPLACES"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse unit");
                return null;
            }
        }

        private TallyCostCenterDto? ParseCostCenter(XElement element)
        {
            try
            {
                return new TallyCostCenterDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    Category = GetElementValue(element, "CATEGORY"),
                    RevenueItem = GetElementValue(element, "REVENUEITEM"),
                    EmailId = GetElementValue(element, "EMAILID")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost center");
                return null;
            }
        }

        private TallyCostCategoryDto? ParseCostCategory(XElement element)
        {
            try
            {
                return new TallyCostCategoryDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    AllocateRevenue = GetBoolValue(element, "ALLOCATEREVENUE"),
                    AllocateNonRevenue = GetBoolValue(element, "ALLOCATENONREVENUE")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost category");
                return null;
            }
        }

        private TallyCurrencyDto? ParseCurrency(XElement element)
        {
            try
            {
                return new TallyCurrencyDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Symbol = GetElementValue(element, "SYMBOL") ?? string.Empty,
                    FormalName = GetElementValue(element, "FORMALNAME"),
                    IsoCode = GetElementValue(element, "MAILINGNAME") ?? GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    DecimalPlaces = ParseIntOrNull(GetElementValue(element, "DECIMALPLACES")) ?? 2,
                    ExchangeRate = ParseDecimalOrNull(GetElementValue(element, "EXCHANGERATE"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse currency");
                return null;
            }
        }

        private TallyVoucherTypeDto? ParseVoucherType(XElement element)
        {
            try
            {
                return new TallyVoucherTypeDto
                {
                    Guid = GetAttributeOrElement(element, "GUID") ?? string.Empty,
                    Name = GetAttributeOrElement(element, "NAME") ?? string.Empty,
                    Parent = GetElementValue(element, "PARENT"),
                    NumberingMethod = GetElementValue(element, "NUMBERINGMETHOD"),
                    UseForPoS = GetBoolValue(element, "USEFORPOS"),
                    UseForJobwork = GetBoolValue(element, "USEFORJOBWORK"),
                    IsActive = GetBoolValue(element, "ISACTIVE", true)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse voucher type");
                return null;
            }
        }

        private TallyVoucherDto? ParseVoucher(XElement element)
        {
            try
            {
                // GUID can be in GUID element, or REMOTEID attribute
                var guid = GetElementValue(element, "GUID")
                        ?? element.Attribute("REMOTEID")?.Value
                        ?? string.Empty;

                // Voucher type can be in VCHTYPE attribute, VOUCHERTYPENAME element, or VOUCHERTYPENAME attribute
                var voucherType = element.Attribute("VCHTYPE")?.Value
                               ?? GetElementValue(element, "VOUCHERTYPENAME")
                               ?? element.Attribute("VOUCHERTYPENAME")?.Value
                               ?? string.Empty;

                var dto = new TallyVoucherDto
                {
                    Guid = guid,
                    VoucherNumber = GetElementValue(element, "VOUCHERNUMBER") ?? string.Empty,
                    VoucherType = voucherType,
                    VoucherTypeName = GetElementValue(element, "VOUCHERTYPENAME") ?? voucherType,
                    Date = ParseTallyDate(GetElementValue(element, "DATE")),
                    ReferenceNumber = GetElementValue(element, "REFERENCE"),
                    ReferenceDate = GetElementValue(element, "REFERENCEDATE"),
                    Narration = GetElementValue(element, "NARRATION"),

                    // Party
                    PartyLedgerName = GetElementValue(element, "PARTYLEDGERNAME"),
                    PartyLedgerGuid = GetElementValue(element, "PARTYLEDGERGUID"),

                    // Amount - can be in AMOUNT element or calculated from ledger entries
                    Amount = Math.Abs(ParseAmount(GetElementValue(element, "AMOUNT"))),
                    Currency = GetElementValue(element, "CURRENCYNAME"),
                    ExchangeRate = ParseDecimalOrNull(GetElementValue(element, "EXCHANGERATE")),

                    // GST
                    PlaceOfSupply = GetElementValue(element, "PLACEOFSUPPLY"),
                    IsReverseCharge = GetBoolValue(element, "ISREVERSECHARGEAPPLICABLE"),
                    GstinOfParty = GetElementValue(element, "PARTYGSTIN"),

                    // E-Invoice
                    EInvoiceIrn = GetElementValue(element, "IRN"),
                    EWayBillNumber = GetElementValue(element, "EWAYBILLNO"),

                    // Flags
                    IsCancelled = GetBoolValue(element, "ISCANCELLED"),
                    IsOptional = GetBoolValue(element, "ISOPTIONAL"),
                    IsPostDated = GetBoolValue(element, "ISPOSTDATED")
                };

                // Parse ledger entries from ALLLEDGERENTRIES.LIST or LEDGERENTRIES.LIST
                foreach (var entry in element.Elements("ALLLEDGERENTRIES.LIST").Concat(element.Elements("LEDGERENTRIES.LIST")))
                {
                    var ledgerEntry = ParseLedgerEntry(entry);
                    if (ledgerEntry != null)
                        dto.LedgerEntries.Add(ledgerEntry);
                }

                // If no ledger entries found in LIST elements, check for direct LEDGERNAME/AMOUNT pairs
                // This happens in simpler Tally exports
                if (dto.LedgerEntries.Count == 0)
                {
                    var directEntries = ParseDirectLedgerEntries(element);
                    dto.LedgerEntries.AddRange(directEntries);
                }

                // Parse inventory entries
                foreach (var entry in element.Elements("ALLINVENTORYENTRIES.LIST").Concat(element.Elements("INVENTORYENTRIES.LIST")))
                {
                    var invEntry = ParseInventoryEntry(entry);
                    if (invEntry != null)
                        dto.InventoryEntries.Add(invEntry);
                }

                // Parse bill allocations at voucher level
                foreach (var billAlloc in element.Elements("BILLALLOCATIONS.LIST"))
                {
                    var alloc = ParseBillAllocation(billAlloc);
                    if (alloc != null)
                        dto.BillAllocations.Add(alloc);
                }

                // Parse cost allocations at voucher level
                foreach (var costAlloc in element.Elements("CATEGORYALLOCATIONS.LIST"))
                {
                    var allocs = ParseCostAllocations(costAlloc);
                    dto.CostAllocations.AddRange(allocs);
                }

                // If amount is still 0, calculate from ledger entries (take absolute of credits)
                if (dto.Amount == 0 && dto.LedgerEntries.Any())
                {
                    // Take the sum of credit entries (negative amounts)
                    dto.Amount = Math.Abs(dto.LedgerEntries.Where(e => e.Amount < 0).Sum(e => e.Amount));
                    if (dto.Amount == 0)
                    {
                        // Or take the sum of debit entries
                        dto.Amount = Math.Abs(dto.LedgerEntries.Where(e => e.Amount > 0).Sum(e => e.Amount));
                    }
                }

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse voucher");
                return null;
            }
        }

        /// <summary>
        /// Parses direct LEDGERNAME/AMOUNT elements that appear as siblings in simpler Tally exports
        /// </summary>
        private List<TallyLedgerEntryDto> ParseDirectLedgerEntries(XElement voucherElement)
        {
            var entries = new List<TallyLedgerEntryDto>();

            // Get all LEDGERNAME elements and pair with corresponding AMOUNT elements
            var ledgerNames = voucherElement.Elements("LEDGERNAME").ToList();
            var amounts = voucherElement.Elements("AMOUNT").ToList();

            // Simple case: one ledger name and one amount
            if (ledgerNames.Count == 1 && amounts.Count >= 1)
            {
                entries.Add(new TallyLedgerEntryDto
                {
                    LedgerName = ledgerNames[0].Value.Trim(),
                    Amount = ParseAmount(amounts[0].Value)
                });

                // If there are additional amounts, they might be for the party ledger
                if (amounts.Count > 1)
                {
                    var partyLedgerName = GetElementValue(voucherElement, "PARTYLEDGERNAME");
                    if (!string.IsNullOrEmpty(partyLedgerName))
                    {
                        entries.Add(new TallyLedgerEntryDto
                        {
                            LedgerName = partyLedgerName,
                            Amount = ParseAmount(amounts[1].Value)
                        });
                    }
                }
            }
            else if (ledgerNames.Count > 1)
            {
                // Multiple ledger names - pair them with amounts by index
                for (int i = 0; i < ledgerNames.Count && i < amounts.Count; i++)
                {
                    entries.Add(new TallyLedgerEntryDto
                    {
                        LedgerName = ledgerNames[i].Value.Trim(),
                        Amount = ParseAmount(amounts[i].Value)
                    });
                }
            }
            else if (ledgerNames.Count == 0 && amounts.Count > 0)
            {
                // No LEDGERNAME but has AMOUNT - use PARTYLEDGERNAME
                var partyLedgerName = GetElementValue(voucherElement, "PARTYLEDGERNAME");
                if (!string.IsNullOrEmpty(partyLedgerName))
                {
                    entries.Add(new TallyLedgerEntryDto
                    {
                        LedgerName = partyLedgerName,
                        Amount = ParseAmount(amounts[0].Value)
                    });
                }
            }

            return entries;
        }

        private TallyLedgerEntryDto? ParseLedgerEntry(XElement element)
        {
            try
            {
                var entry = new TallyLedgerEntryDto
                {
                    LedgerName = GetElementValue(element, "LEDGERNAME") ?? string.Empty,
                    LedgerGuid = GetElementValue(element, "LEDGERGUID"),
                    Amount = ParseAmount(GetElementValue(element, "AMOUNT")),

                    // GST breakdown
                    CgstAmount = ParseDecimalOrNull(GetElementValue(element, "CGSTAMOUNT")),
                    SgstAmount = ParseDecimalOrNull(GetElementValue(element, "SGSTAMOUNT")),
                    IgstAmount = ParseDecimalOrNull(GetElementValue(element, "IGSTAMOUNT")),
                    CessAmount = ParseDecimalOrNull(GetElementValue(element, "CESSAMOUNT")),

                    // TDS
                    TdsAmount = ParseDecimalOrNull(GetElementValue(element, "TDSAMOUNT")),
                    TdsSection = GetElementValue(element, "TDSSECTION"),
                    TdsRate = ParseDecimalOrNull(GetElementValue(element, "TDSRATE"))
                };

                // Parse nested bill allocations
                foreach (var billAlloc in element.Elements("BILLALLOCATIONS.LIST"))
                {
                    var alloc = ParseBillAllocation(billAlloc);
                    if (alloc != null)
                        entry.BillAllocations.Add(alloc);
                }

                // Parse nested cost allocations
                foreach (var costAlloc in element.Elements("CATEGORYALLOCATIONS.LIST"))
                {
                    var allocs = ParseCostAllocations(costAlloc);
                    entry.CostAllocations.AddRange(allocs);
                }

                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ledger entry");
                return null;
            }
        }

        private TallyInventoryEntryDto? ParseInventoryEntry(XElement element)
        {
            try
            {
                var entry = new TallyInventoryEntryDto
                {
                    StockItemName = GetElementValue(element, "STOCKITEMNAME") ?? string.Empty,
                    StockItemGuid = GetElementValue(element, "STOCKITEMGUID"),
                    Quantity = ParseQuantity(GetElementValue(element, "ACTUALQTY") ?? GetElementValue(element, "BILLEDQTY")),
                    Unit = ExtractUnit(GetElementValue(element, "ACTUALQTY")),
                    ActualQuantity = ParseQuantity(GetElementValue(element, "ACTUALQTY")),
                    BilledQuantity = ParseQuantity(GetElementValue(element, "BILLEDQTY")),
                    Rate = ParseRate(GetElementValue(element, "RATE")),
                    Amount = Math.Abs(ParseAmount(GetElementValue(element, "AMOUNT"))),
                    Discount = ParseDecimalOrNull(GetElementValue(element, "DISCOUNT")),
                    DiscountType = GetElementValue(element, "DISCOUNTTYPE"),
                    GodownName = GetElementValue(element, "GODOWNNAME"),
                    GodownGuid = GetElementValue(element, "GODOWNGUID"),
                    DestinationGodownName = GetElementValue(element, "DESTINATIONGODOWNNAME"),
                    DestinationGodownGuid = GetElementValue(element, "DESTINATIONGODOWNGUID"),

                    // GST
                    HsnCode = GetElementValue(element, "HSNCODE"),
                    GstRate = ParseDecimalOrNull(GetElementValue(element, "GSTRATE")),
                    CgstAmount = ParseDecimalOrNull(GetElementValue(element, "CGSTAMOUNT")),
                    SgstAmount = ParseDecimalOrNull(GetElementValue(element, "SGSTAMOUNT")),
                    IgstAmount = ParseDecimalOrNull(GetElementValue(element, "IGSTAMOUNT")),
                    CessAmount = ParseDecimalOrNull(GetElementValue(element, "CESSAMOUNT")),

                    // Order
                    OrderNumber = GetElementValue(element, "ORDERNUMBER"),
                    OrderDate = ParseTallyDateOrNull(GetElementValue(element, "ORDERDATE"))
                };

                // Parse batch allocations
                foreach (var batch in element.Elements("BATCHALLOCATIONS.LIST"))
                {
                    var batchAlloc = ParseBatchAllocation(batch);
                    if (batchAlloc != null)
                        entry.BatchAllocations.Add(batchAlloc);
                }

                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse inventory entry");
                return null;
            }
        }

        private TallyBillAllocationDto? ParseBillAllocation(XElement element)
        {
            try
            {
                return new TallyBillAllocationDto
                {
                    Name = GetElementValue(element, "NAME") ?? string.Empty,
                    BillType = GetElementValue(element, "BILLTYPE") ?? string.Empty,
                    Amount = ParseAmount(GetElementValue(element, "AMOUNT")),
                    BillDate = ParseTallyDateOrNull(GetElementValue(element, "BILLDATE")),
                    DueDate = ParseTallyDateOrNull(GetElementValue(element, "DUEDATE")),
                    BillCreditPeriod = GetElementValue(element, "BILLCREDITPERIOD")
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse bill allocation");
                return null;
            }
        }

        private List<TallyCostAllocationDto> ParseCostAllocations(XElement categoryElement)
        {
            var result = new List<TallyCostAllocationDto>();
            try
            {
                var categoryName = GetElementValue(categoryElement, "CATEGORY");

                foreach (var costCenter in categoryElement.Elements("COSTCENTREALLOCATIONS.LIST"))
                {
                    result.Add(new TallyCostAllocationDto
                    {
                        CostCenterName = GetElementValue(costCenter, "NAME") ?? string.Empty,
                        CostCenterGuid = GetElementValue(costCenter, "GUID"),
                        CostCategoryName = categoryName,
                        Amount = ParseAmount(GetElementValue(costCenter, "AMOUNT"))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse cost allocations");
            }
            return result;
        }

        private TallyBatchAllocationDto? ParseBatchAllocation(XElement element)
        {
            try
            {
                return new TallyBatchAllocationDto
                {
                    BatchName = GetElementValue(element, "BATCHNAME") ?? GetElementValue(element, "NAME") ?? string.Empty,
                    BatchGuid = GetElementValue(element, "BATCHGUID"),
                    GodownName = GetElementValue(element, "GODOWNNAME"),
                    Quantity = ParseQuantity(GetElementValue(element, "QUANTITY")),
                    Rate = ParseRate(GetElementValue(element, "RATE")),
                    Amount = ParseAmount(GetElementValue(element, "AMOUNT")),
                    ManufacturingDate = ParseTallyDateOrNull(GetElementValue(element, "MFGDATE")),
                    ExpiryDate = ParseTallyDateOrNull(GetElementValue(element, "EXPIRYDATE"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse batch allocation");
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
            var voucherTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Sales", "sales" },
                { "Purchase", "purchase" },
                { "Receipt", "receipt" },
                { "Payment", "payment" },
                { "Journal", "journal" },
                { "Contra", "contra" },
                { "Credit Note", "credit_note" },
                { "Debit Note", "debit_note" },
                { "Stock Journal", "stock_journal" },
                { "Physical Stock", "physical_stock" },
                { "Delivery Note", "delivery_note" },
                { "Receipt Note", "receipt_note" }
            };

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

                // Track date range
                if (vouchers.MinDate == null || v.Date < vouchers.MinDate)
                    vouchers.MinDate = v.Date;
                if (vouchers.MaxDate == null || v.Date > vouchers.MaxDate)
                    vouchers.MaxDate = v.Date;
            }
        }

        private void ValidateParsedData(TallyParsedDataDto result)
        {
            // Check for required data
            if (result.Masters.Ledgers.Count == 0 && result.Vouchers.Vouchers.Count == 0)
            {
                result.ValidationIssues.Add(new TallyValidationIssueDto
                {
                    Severity = "warning",
                    Code = "EMPTY_DATA",
                    Message = "No ledgers or vouchers found in the file"
                });
            }

            // Check for ledgers without GUID
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

            // Check for vouchers without ledger entries
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

            // Check for unbalanced vouchers (debits != credits)
            foreach (var voucher in result.Vouchers.Vouchers)
            {
                var totalAmount = voucher.LedgerEntries.Sum(e => e.Amount);
                if (Math.Abs(totalAmount) > 0.01m) // Allow small rounding differences
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
        private static string? GetElementValue(XElement parent, string name)
        {
            var value = parent.Element(name)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string? GetAttributeOrElement(XElement element, string name)
        {
            var attr = element.Attribute(name)?.Value;
            if (!string.IsNullOrWhiteSpace(attr))
                return attr.Trim();
            return GetElementValue(element, name);
        }

        private static bool GetBoolValue(XElement parent, string name, bool defaultValue = false)
        {
            var value = GetElementValue(parent, name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                   value == "1";
        }

        private static decimal ParseAmount(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            // Remove currency symbols and spaces
            value = value.Replace("₹", "").Replace("Rs.", "").Replace("Rs", "").Trim();

            // Handle Dr/Cr suffix - Tally exports sometimes use this format
            bool isDebit = value.EndsWith(" Dr", StringComparison.OrdinalIgnoreCase);
            bool isCredit = value.EndsWith(" Cr", StringComparison.OrdinalIgnoreCase);

            if (isDebit || isCredit)
                value = value.Substring(0, value.Length - 3).Trim();

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                // Convention: positive = debit, negative = credit
                // Apply sign based on Dr/Cr suffix if present
                if (isCredit)
                    return -Math.Abs(result);  // Credits are negative
                if (isDebit)
                    return Math.Abs(result);   // Debits are positive
                // If no suffix, preserve original sign from Tally
                return result;
            }

            return 0;
        }

        private static decimal? ParseDecimalOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return result;

            return null;
        }

        private static int? ParseIntOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value.Trim(), out var result))
                return result;

            return null;
        }

        private static decimal ParseQuantity(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            // Quantity format: "10 Nos" or "5.5 Kgs" or just "10"
            var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && decimal.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return Math.Abs(result);

            return 0;
        }

        private static string? ExtractUnit(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var parts = value.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? parts[1] : null;
        }

        private static decimal ParseRate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            // Rate format: "100/Nos" or "50.5/Kgs" or just "100"
            var slashIndex = value.IndexOf('/');
            var ratePart = slashIndex > 0 ? value.Substring(0, slashIndex) : value;

            if (decimal.TryParse(ratePart.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                return Math.Abs(result);

            return 0;
        }

        private static DateOnly ParseTallyDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DateOnly.FromDateTime(DateTime.Today);

            // Tally date format: YYYYMMDD
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

            // Try standard date parsing
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
