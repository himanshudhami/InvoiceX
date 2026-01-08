using System.Text;
using Application.Services.Migration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace UnitTests.Migration
{
    public class TallyXmlParserServiceTests
    {
        private readonly Mock<ILogger<TallyXmlParserService>> _mockLogger;
        private readonly TallyXmlParserService _parser;

        public TallyXmlParserServiceTests()
        {
            _mockLogger = new Mock<ILogger<TallyXmlParserService>>();
            _parser = new TallyXmlParserService(_mockLogger.Object);
        }

        [Fact]
        public void CanParse_WithXmlExtension_ReturnsTrue()
        {
            // Act
            var result = _parser.CanParse("Master.xml");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanParse_WithNonXmlExtension_ReturnsFalse()
        {
            // Act
            var result = _parser.CanParse("Master.json");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ParseAsync_WithUtf8Xml_ParsesSuccessfully()
        {
            // Arrange - Using actual Tally XML structure with TALLYMESSAGE under BODY
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <LEDGER NAME=""Test Ledger"">
                <GUID>test-guid-123</GUID>
                <PARENT>Sundry Debtors</PARENT>
                <OPENINGBALANCE>10000.00</OPENINGBALANCE>
            </LEDGER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess, $"Parse failed: {result.Error?.Message}");
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.Masters.Ledgers);
            Assert.Equal("Test Ledger", result.Value.Masters.Ledgers[0].Name);
            Assert.Equal("test-guid-123", result.Value.Masters.Ledgers[0].Guid);
            Assert.Equal("Sundry Debtors", result.Value.Masters.Ledgers[0].LedgerGroup);
            Assert.Equal(10000.00m, result.Value.Masters.Ledgers[0].OpeningBalance);
        }

        [Fact]
        public async Task ParseAsync_WithUtf16LEXml_ParsesSuccessfully()
        {
            // Arrange - UTF-16 LE encoded XML
            var xml = @"<?xml version=""1.0"" encoding=""UTF-16""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <LEDGER NAME=""UTF16 Ledger"">
                <GUID>utf16-guid-456</GUID>
                <PARENT>Sundry Creditors</PARENT>
                <PARTYGSTIN>29AAACX1234A1ZT</PARTYGSTIN>
                <INCOMETAXNUMBER>AABCT1234F</INCOMETAXNUMBER>
            </LEDGER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            // Create UTF-16 LE encoded bytes with BOM
            var encoding = Encoding.Unicode; // UTF-16 LE
            var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(xml)).ToArray();
            using var stream = new MemoryStream(bytes);

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.Masters.Ledgers);
            Assert.Equal("UTF16 Ledger", result.Value.Masters.Ledgers[0].Name);
            Assert.Equal("utf16-guid-456", result.Value.Masters.Ledgers[0].Guid);
            Assert.Equal("29AAACX1234A1ZT", result.Value.Masters.Ledgers[0].Gstin);
            Assert.Equal("AABCT1234F", result.Value.Masters.Ledgers[0].PanNumber);
        }

        [Fact]
        public async Task ParseAsync_WithOldAddressList_ParsesAddress()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <LEDGER NAME=""Address Test"">
                <GUID>address-guid-789</GUID>
                <PARENT>Sundry Debtors</PARENT>
                <OLDADDRESS.LIST>
                    <OLDADDRESS>123 Main Street</OLDADDRESS>
                    <OLDADDRESS>Floor 4</OLDADDRESS>
                    <OLDADDRESS>Bangalore</OLDADDRESS>
                </OLDADDRESS.LIST>
                <OLDPINCODE>560001</OLDPINCODE>
                <PRIORSTATENAME>Karnataka</PRIORSTATENAME>
            </LEDGER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.Masters.Ledgers);
            var ledger = result.Value.Masters.Ledgers[0];
            Assert.Contains("123 Main Street", ledger.Address);
            Assert.Contains("Floor 4", ledger.Address);
            Assert.Contains("Bangalore", ledger.Address);
            Assert.Equal("560001", ledger.Pincode);
            Assert.Equal("Karnataka", ledger.StateName);
        }

        [Fact]
        public async Task ParseAsync_WithVoucher_ParsesVoucherDetails()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <VOUCHER REMOTEID=""voucher-guid-001"" VCHTYPE=""Payment"">
                <DATE>20251001</DATE>
                <GUID>voucher-guid-001</GUID>
                <VOUCHERTYPENAME>Payment</VOUCHERTYPENAME>
                <VOUCHERNUMBER>228</VOUCHERNUMBER>
                <PARTYLEDGERNAME>Test Party</PARTYLEDGERNAME>
                <NARRATION>Test payment</NARRATION>
                <LEDGERNAME>Test Party</LEDGERNAME>
                <AMOUNT>-100000.00</AMOUNT>
            </VOUCHER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Single(result.Value.Vouchers.Vouchers);
            var voucher = result.Value.Vouchers.Vouchers[0];
            Assert.Equal("voucher-guid-001", voucher.Guid);
            Assert.Equal("Payment", voucher.VoucherType);
            Assert.Equal("228", voucher.VoucherNumber);
            Assert.Equal("Test Party", voucher.PartyLedgerName);
            Assert.Equal("Test payment", voucher.Narration);
            Assert.Equal(new DateOnly(2025, 10, 1), voucher.Date);
            Assert.True(voucher.LedgerEntries.Count > 0);
        }

        [Fact]
        public async Task ParseAsync_WithVoucherTypeAttribute_UsesVchTypeAttribute()
        {
            // Arrange - Voucher type comes from VCHTYPE attribute
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <VOUCHER REMOTEID=""sale-guid-002"" VCHTYPE=""Sales"">
                <DATE>20251015</DATE>
                <VOUCHERNUMBER>INV-001</VOUCHERNUMBER>
                <PARTYLEDGERNAME>Customer ABC</PARTYLEDGERNAME>
                <AMOUNT>-50000.00</AMOUNT>
            </VOUCHER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            var voucher = result.Value!.Vouchers.Vouchers[0];
            Assert.Equal("Sales", voucher.VoucherType);
            Assert.Equal("sale-guid-002", voucher.Guid);
        }

        [Fact]
        public async Task ParseAsync_WithBankDetails_ParsesBankInfo()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <LEDGER NAME=""Bank Ledger"">
                <GUID>bank-guid-123</GUID>
                <PARENT>Bank Accounts</PARENT>
                <BANKDETAILS>912020011392251</BANKDETAILS>
                <IFSCODE>UTIB0000333</IFSCODE>
            </LEDGER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            var ledger = result.Value!.Masters.Ledgers[0];
            Assert.Equal("912020011392251", ledger.BankAccountNumber);
            Assert.Equal("UTIB0000333", ledger.IfscCode);
        }

        [Fact]
        public async Task ParseAsync_CalculatesSummaries()
        {
            // Arrange
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <LEDGER NAME=""Ledger 1"">
                <GUID>guid-1</GUID>
                <PARENT>Sundry Debtors</PARENT>
            </LEDGER>
            <LEDGER NAME=""Ledger 2"">
                <GUID>guid-2</GUID>
                <PARENT>Sundry Debtors</PARENT>
            </LEDGER>
            <LEDGER NAME=""Ledger 3"">
                <GUID>guid-3</GUID>
                <PARENT>Sundry Creditors</PARENT>
            </LEDGER>
            <VOUCHER VCHTYPE=""Sales"" REMOTEID=""v1"">
                <DATE>20251001</DATE>
                <VOUCHERNUMBER>S1</VOUCHERNUMBER>
                <AMOUNT>10000.00</AMOUNT>
            </VOUCHER>
            <VOUCHER VCHTYPE=""Payment"" REMOTEID=""v2"">
                <DATE>20251002</DATE>
                <VOUCHERNUMBER>P1</VOUCHERNUMBER>
                <AMOUNT>5000.00</AMOUNT>
            </VOUCHER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(3, result.Value!.Masters.Ledgers.Count);
            Assert.Equal(2, result.Value.Vouchers.Vouchers.Count);
            Assert.Equal(1, result.Value.Vouchers.SalesCount);
            Assert.Equal(1, result.Value.Vouchers.PaymentCount);
            Assert.True(result.Value.Masters.LedgerCountsByGroup.ContainsKey("Sundry Debtors"));
            Assert.Equal(2, result.Value.Masters.LedgerCountsByGroup["Sundry Debtors"]);
        }

        [Fact]
        public async Task ParseAsync_WithInvalidXml_ReturnsError()
        {
            // Arrange
            var invalidXml = "This is not XML";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidXml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Error);
        }

        [Fact]
        public async Task ParseAsync_WithDirectTallyMessage_ParsesSuccessfully()
        {
            // Arrange - Some Tally exports don't have ENVELOPE wrapper
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<TALLYMESSAGE>
    <LEDGER NAME=""Direct Ledger"">
        <GUID>direct-guid-001</GUID>
        <PARENT>Capital Account</PARENT>
    </LEDGER>
</TALLYMESSAGE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "test.xml");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!.Masters.Ledgers);
            Assert.Equal("Direct Ledger", result.Value.Masters.Ledgers[0].Name);
        }

        [Fact]
        public async Task ParseAsync_WithNestedImportDataStructure_ParsesSuccessfully()
        {
            // Arrange - Full Tally export structure with IMPORTDATA > REQUESTDATA > TALLYMESSAGE
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <HEADER>
        <TALLYREQUEST>Export Data</TALLYREQUEST>
    </HEADER>
    <BODY>
        <IMPORTDATA>
            <REQUESTDESC>
                <STATICVARIABLES>
                    <SVCURRENTCOMPANY>Test Company Pvt Ltd</SVCURRENTCOMPANY>
                </STATICVARIABLES>
            </REQUESTDESC>
            <REQUESTDATA>
                <TALLYMESSAGE>
                    <LEDGER NAME=""ABC Enterprises"" REMOTEID=""abc-guid-001"">
                        <GUID>abc-guid-001</GUID>
                        <PARENT>Sundry Creditors</PARENT>
                        <OPENINGBALANCE>-50000.00</OPENINGBALANCE>
                        <GSTREGISTRATIONTYPE>Regular</GSTREGISTRATIONTYPE>
                        <PARTYGSTIN>27AABCT1234A1Z5</PARTYGSTIN>
                        <INCOMETAXNUMBER>AABCT1234F</INCOMETAXNUMBER>
                        <OLDADDRESS.LIST>
                            <OLDADDRESS>101 Business Park</OLDADDRESS>
                            <OLDADDRESS>Mumbai</OLDADDRESS>
                        </OLDADDRESS.LIST>
                        <OLDPINCODE>400001</OLDPINCODE>
                        <PRIORSTATENAME>Maharashtra</PRIORSTATENAME>
                    </LEDGER>
                    <LEDGER NAME=""HDFC Bank Current"" REMOTEID=""hdfc-guid-002"">
                        <GUID>hdfc-guid-002</GUID>
                        <PARENT>Bank Accounts</PARENT>
                        <OPENINGBALANCE>250000.00</OPENINGBALANCE>
                        <BANKDETAILS>50100012345678</BANKDETAILS>
                        <IFSCODE>HDFC0001234</IFSCODE>
                    </LEDGER>
                </TALLYMESSAGE>
            </REQUESTDATA>
        </IMPORTDATA>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "master.xml");

            // Assert
            Assert.True(result.IsSuccess, $"Parse failed: {result.Error?.Message}");
            Assert.NotNull(result.Value);
            Assert.Equal("Test Company Pvt Ltd", result.Value.Masters.TallyCompanyName);
            Assert.Equal(2, result.Value.Masters.Ledgers.Count);

            // Verify first ledger (Sundry Creditor)
            var creditor = result.Value.Masters.Ledgers.First(l => l.Name == "ABC Enterprises");
            Assert.Equal("abc-guid-001", creditor.Guid);
            Assert.Equal("Sundry Creditors", creditor.LedgerGroup);
            Assert.Equal(-50000.00m, creditor.OpeningBalance);
            Assert.Equal("27AABCT1234A1Z5", creditor.Gstin);
            Assert.Equal("AABCT1234F", creditor.PanNumber);
            Assert.Contains("101 Business Park", creditor.Address);
            Assert.Contains("Mumbai", creditor.Address);
            Assert.Equal("400001", creditor.Pincode);
            Assert.Equal("Maharashtra", creditor.StateName);

            // Verify second ledger (Bank Account)
            var bank = result.Value.Masters.Ledgers.First(l => l.Name == "HDFC Bank Current");
            Assert.Equal("hdfc-guid-002", bank.Guid);
            Assert.Equal("Bank Accounts", bank.LedgerGroup);
            Assert.Equal(250000.00m, bank.OpeningBalance);
            Assert.Equal("50100012345678", bank.BankAccountNumber);
            Assert.Equal("HDFC0001234", bank.IfscCode);
        }

        [Fact]
        public async Task ParseAsync_WithAllLedgerEntriesList_ParsesVoucherWithEntries()
        {
            // Arrange - Voucher with ALLLEDGERENTRIES.LIST structure
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <TALLYMESSAGE>
            <VOUCHER REMOTEID=""sale-001"" VCHTYPE=""Sales"">
                <DATE>20251115</DATE>
                <GUID>sale-001</GUID>
                <VOUCHERTYPENAME>Sales</VOUCHERTYPENAME>
                <VOUCHERNUMBER>INV/2025/001</VOUCHERNUMBER>
                <PARTYLEDGERNAME>Customer XYZ</PARTYLEDGERNAME>
                <NARRATION>Goods sold as per order</NARRATION>
                <ALLLEDGERENTRIES.LIST>
                    <LEDGERNAME>Customer XYZ</LEDGERNAME>
                    <LEDGERGUID>cust-xyz-guid</LEDGERGUID>
                    <AMOUNT>-11800.00</AMOUNT>
                </ALLLEDGERENTRIES.LIST>
                <ALLLEDGERENTRIES.LIST>
                    <LEDGERNAME>Sales Account</LEDGERNAME>
                    <LEDGERGUID>sales-acc-guid</LEDGERGUID>
                    <AMOUNT>10000.00</AMOUNT>
                </ALLLEDGERENTRIES.LIST>
                <ALLLEDGERENTRIES.LIST>
                    <LEDGERNAME>CGST Output</LEDGERNAME>
                    <AMOUNT>900.00</AMOUNT>
                    <CGSTAMOUNT>900.00</CGSTAMOUNT>
                </ALLLEDGERENTRIES.LIST>
                <ALLLEDGERENTRIES.LIST>
                    <LEDGERNAME>SGST Output</LEDGERNAME>
                    <AMOUNT>900.00</AMOUNT>
                    <SGSTAMOUNT>900.00</SGSTAMOUNT>
                </ALLLEDGERENTRIES.LIST>
            </VOUCHER>
        </TALLYMESSAGE>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "transactions.xml");

            // Assert
            Assert.True(result.IsSuccess, $"Parse failed: {result.Error?.Message}");
            Assert.Single(result.Value!.Vouchers.Vouchers);

            var voucher = result.Value.Vouchers.Vouchers[0];
            Assert.Equal("sale-001", voucher.Guid);
            Assert.Equal("Sales", voucher.VoucherType);
            Assert.Equal("INV/2025/001", voucher.VoucherNumber);
            Assert.Equal("Customer XYZ", voucher.PartyLedgerName);
            Assert.Equal("Goods sold as per order", voucher.Narration);
            Assert.Equal(new DateOnly(2025, 11, 15), voucher.Date);

            // Verify ledger entries
            Assert.Equal(4, voucher.LedgerEntries.Count);

            var customerEntry = voucher.LedgerEntries.First(e => e.LedgerName == "Customer XYZ");
            Assert.Equal(-11800.00m, customerEntry.Amount);
            Assert.Equal("cust-xyz-guid", customerEntry.LedgerGuid);

            var cgstEntry = voucher.LedgerEntries.First(e => e.LedgerName == "CGST Output");
            Assert.Equal(900.00m, cgstEntry.CgstAmount);
        }

        [Fact]
        public async Task ParseAsync_FullIntegration_ParsesMastersAndVouchers()
        {
            // Arrange - Combined masters and vouchers in one file
            var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ENVELOPE>
    <BODY>
        <IMPORTDATA>
            <REQUESTDESC>
                <STATICVARIABLES>
                    <SVCURRENTCOMPANY>Integration Test Company</SVCURRENTCOMPANY>
                </STATICVARIABLES>
            </REQUESTDESC>
            <REQUESTDATA>
                <TALLYMESSAGE>
                    <LEDGER NAME=""Customer A"">
                        <GUID>cust-a-001</GUID>
                        <PARENT>Sundry Debtors</PARENT>
                        <OPENINGBALANCE>-25000.00</OPENINGBALANCE>
                        <PARTYGSTIN>29AABCA1234B1ZC</PARTYGSTIN>
                    </LEDGER>
                    <LEDGER NAME=""Vendor B"">
                        <GUID>vend-b-001</GUID>
                        <PARENT>Sundry Creditors</PARENT>
                        <OPENINGBALANCE>15000.00</OPENINGBALANCE>
                    </LEDGER>
                    <VOUCHER REMOTEID=""pmt-001"" VCHTYPE=""Payment"">
                        <DATE>20251201</DATE>
                        <VOUCHERNUMBER>PAY/001</VOUCHERNUMBER>
                        <PARTYLEDGERNAME>Vendor B</PARTYLEDGERNAME>
                        <NARRATION>Payment for goods</NARRATION>
                        <ALLLEDGERENTRIES.LIST>
                            <LEDGERNAME>Vendor B</LEDGERNAME>
                            <AMOUNT>-15000.00</AMOUNT>
                        </ALLLEDGERENTRIES.LIST>
                        <ALLLEDGERENTRIES.LIST>
                            <LEDGERNAME>HDFC Bank</LEDGERNAME>
                            <AMOUNT>15000.00</AMOUNT>
                        </ALLLEDGERENTRIES.LIST>
                    </VOUCHER>
                    <VOUCHER REMOTEID=""rcpt-001"" VCHTYPE=""Receipt"">
                        <DATE>20251202</DATE>
                        <VOUCHERNUMBER>RCP/001</VOUCHERNUMBER>
                        <PARTYLEDGERNAME>Customer A</PARTYLEDGERNAME>
                        <NARRATION>Received payment</NARRATION>
                        <ALLLEDGERENTRIES.LIST>
                            <LEDGERNAME>Customer A</LEDGERNAME>
                            <AMOUNT>25000.00</AMOUNT>
                        </ALLLEDGERENTRIES.LIST>
                        <ALLLEDGERENTRIES.LIST>
                            <LEDGERNAME>HDFC Bank</LEDGERNAME>
                            <AMOUNT>-25000.00</AMOUNT>
                        </ALLLEDGERENTRIES.LIST>
                    </VOUCHER>
                </TALLYMESSAGE>
            </REQUESTDATA>
        </IMPORTDATA>
    </BODY>
</ENVELOPE>";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            // Act
            var result = await _parser.ParseAsync(stream, "combined.xml");

            // Assert
            Assert.True(result.IsSuccess, $"Parse failed: {result.Error?.Message}");
            Assert.Equal("Integration Test Company", result.Value!.Masters.TallyCompanyName);

            // Masters
            Assert.Equal(2, result.Value.Masters.Ledgers.Count);
            Assert.Contains(result.Value.Masters.Ledgers, l => l.Name == "Customer A" && l.LedgerGroup == "Sundry Debtors");
            Assert.Contains(result.Value.Masters.Ledgers, l => l.Name == "Vendor B" && l.LedgerGroup == "Sundry Creditors");

            // Vouchers
            Assert.Equal(2, result.Value.Vouchers.Vouchers.Count);
            Assert.Equal(1, result.Value.Vouchers.PaymentCount);
            Assert.Equal(1, result.Value.Vouchers.ReceiptCount);

            // Verify Payment voucher
            var payment = result.Value.Vouchers.Vouchers.First(v => v.VoucherType == "Payment");
            Assert.Equal("pmt-001", payment.Guid);
            Assert.Equal(2, payment.LedgerEntries.Count);

            // Verify Receipt voucher
            var receipt = result.Value.Vouchers.Vouchers.First(v => v.VoucherType == "Receipt");
            Assert.Equal("rcpt-001", receipt.Guid);
            Assert.Equal(2, receipt.LedgerEntries.Count);

            // Verify ledger counts by group
            Assert.True(result.Value.Masters.LedgerCountsByGroup.ContainsKey("Sundry Debtors"));
            Assert.Equal(1, result.Value.Masters.LedgerCountsByGroup["Sundry Debtors"]);
            Assert.True(result.Value.Masters.LedgerCountsByGroup.ContainsKey("Sundry Creditors"));
            Assert.Equal(1, result.Value.Masters.LedgerCountsByGroup["Sundry Creditors"]);
        }
    }
}
