using System.Text.Json.Serialization;

namespace Core.Entities.EInvoice
{
    /// <summary>
    /// IRP Schema 1.1 for e-invoice generation.
    /// Supports B2B, B2C, SEZWP, SEZWOP, EXPWP, EXPWOP, DEXP invoice types.
    /// Reference: https://einvoice1.gst.gov.in/Documents/e-invoice_Schema_notified_on_30thJuly.pdf
    /// </summary>
    public class IrpInvoiceSchema
    {
        /// <summary>Version of the schema (currently 1.1)</summary>
        [JsonPropertyName("Version")]
        public string Version { get; set; } = "1.1";

        /// <summary>Transaction details</summary>
        [JsonPropertyName("TranDtls")]
        public TransactionDetails TranDtls { get; set; } = new();

        /// <summary>Document details (invoice number, date, type)</summary>
        [JsonPropertyName("DocDtls")]
        public DocumentDetails DocDtls { get; set; } = new();

        /// <summary>Seller (supplier) details</summary>
        [JsonPropertyName("SellerDtls")]
        public PartyDetails SellerDtls { get; set; } = new();

        /// <summary>Buyer (recipient) details</summary>
        [JsonPropertyName("BuyerDtls")]
        public PartyDetails BuyerDtls { get; set; } = new();

        /// <summary>Dispatch from details (if different from seller)</summary>
        [JsonPropertyName("DispDtls")]
        public DispatchDetails? DispDtls { get; set; }

        /// <summary>Ship to details (if different from buyer)</summary>
        [JsonPropertyName("ShipDtls")]
        public ShipDetails? ShipDtls { get; set; }

        /// <summary>Item list</summary>
        [JsonPropertyName("ItemList")]
        public List<ItemDetails> ItemList { get; set; } = new();

        /// <summary>Value details (totals)</summary>
        [JsonPropertyName("ValDtls")]
        public ValueDetails ValDtls { get; set; } = new();

        /// <summary>Payment details</summary>
        [JsonPropertyName("PayDtls")]
        public PaymentDetails? PayDtls { get; set; }

        /// <summary>Reference details</summary>
        [JsonPropertyName("RefDtls")]
        public ReferenceDetails? RefDtls { get; set; }

        /// <summary>Additional documents</summary>
        [JsonPropertyName("AddlDocDtls")]
        public List<AdditionalDocDetails>? AddlDocDtls { get; set; }

        /// <summary>Export details (for EXPWP, EXPWOP)</summary>
        [JsonPropertyName("ExpDtls")]
        public ExportDetails? ExpDtls { get; set; }

        /// <summary>E-way bill details</summary>
        [JsonPropertyName("EwbDtls")]
        public EwayBillDetails? EwbDtls { get; set; }
    }

    public class TransactionDetails
    {
        /// <summary>Tax scheme (GST)</summary>
        [JsonPropertyName("TaxSch")]
        public string TaxSch { get; set; } = "GST";

        /// <summary>
        /// Supply type code:
        /// B2B, SEZWP, SEZWOP, EXPWP, EXPWOP, DEXP, B2C
        /// </summary>
        [JsonPropertyName("SupTyp")]
        public string SupTyp { get; set; } = "B2B";

        /// <summary>Reverse charge: Y/N</summary>
        [JsonPropertyName("RegRev")]
        public string? RegRev { get; set; }

        /// <summary>E-commerce GSTIN</summary>
        [JsonPropertyName("EcmGstin")]
        public string? EcmGstin { get; set; }

        /// <summary>IGST on Intra: Y/N (for SEZ)</summary>
        [JsonPropertyName("IgstOnIntra")]
        public string? IgstOnIntra { get; set; }
    }

    public class DocumentDetails
    {
        /// <summary>
        /// Document type:
        /// INV=Invoice, CRN=Credit Note, DBN=Debit Note
        /// </summary>
        [JsonPropertyName("Typ")]
        public string Typ { get; set; } = "INV";

        /// <summary>Document number (max 16 chars)</summary>
        [JsonPropertyName("No")]
        public string No { get; set; } = string.Empty;

        /// <summary>Document date (DD/MM/YYYY)</summary>
        [JsonPropertyName("Dt")]
        public string Dt { get; set; } = string.Empty;
    }

    public class PartyDetails
    {
        /// <summary>GSTIN (15 chars) or URP for unregistered</summary>
        [JsonPropertyName("Gstin")]
        public string Gstin { get; set; } = string.Empty;

        /// <summary>Legal name</summary>
        [JsonPropertyName("LglNm")]
        public string LglNm { get; set; } = string.Empty;

        /// <summary>Trade name</summary>
        [JsonPropertyName("TrdNm")]
        public string? TrdNm { get; set; }

        /// <summary>Address line 1</summary>
        [JsonPropertyName("Addr1")]
        public string Addr1 { get; set; } = string.Empty;

        /// <summary>Address line 2</summary>
        [JsonPropertyName("Addr2")]
        public string? Addr2 { get; set; }

        /// <summary>Location (city)</summary>
        [JsonPropertyName("Loc")]
        public string Loc { get; set; } = string.Empty;

        /// <summary>PIN code (6 digits)</summary>
        [JsonPropertyName("Pin")]
        public int Pin { get; set; }

        /// <summary>State code (2 digits, 01-37, 97 for other country)</summary>
        [JsonPropertyName("Stcd")]
        public string Stcd { get; set; } = string.Empty;

        /// <summary>Phone number</summary>
        [JsonPropertyName("Ph")]
        public string? Ph { get; set; }

        /// <summary>Email</summary>
        [JsonPropertyName("Em")]
        public string? Em { get; set; }
    }

    public class DispatchDetails
    {
        [JsonPropertyName("Nm")]
        public string? Nm { get; set; }

        [JsonPropertyName("Addr1")]
        public string? Addr1 { get; set; }

        [JsonPropertyName("Addr2")]
        public string? Addr2 { get; set; }

        [JsonPropertyName("Loc")]
        public string? Loc { get; set; }

        [JsonPropertyName("Pin")]
        public int? Pin { get; set; }

        [JsonPropertyName("Stcd")]
        public string? Stcd { get; set; }
    }

    public class ShipDetails
    {
        [JsonPropertyName("Gstin")]
        public string? Gstin { get; set; }

        [JsonPropertyName("LglNm")]
        public string? LglNm { get; set; }

        [JsonPropertyName("TrdNm")]
        public string? TrdNm { get; set; }

        [JsonPropertyName("Addr1")]
        public string? Addr1 { get; set; }

        [JsonPropertyName("Addr2")]
        public string? Addr2 { get; set; }

        [JsonPropertyName("Loc")]
        public string? Loc { get; set; }

        [JsonPropertyName("Pin")]
        public int? Pin { get; set; }

        [JsonPropertyName("Stcd")]
        public string? Stcd { get; set; }
    }

    public class ItemDetails
    {
        /// <summary>Serial number (1-999)</summary>
        [JsonPropertyName("SlNo")]
        public string SlNo { get; set; } = "1";

        /// <summary>Product description</summary>
        [JsonPropertyName("PrdDesc")]
        public string? PrdDesc { get; set; }

        /// <summary>
        /// Is service: Y/N
        /// </summary>
        [JsonPropertyName("IsServc")]
        public string IsServc { get; set; } = "N";

        /// <summary>HSN code (4-8 digits)</summary>
        [JsonPropertyName("HsnCd")]
        public string HsnCd { get; set; } = string.Empty;

        /// <summary>Barcode</summary>
        [JsonPropertyName("Barcde")]
        public string? Barcde { get; set; }

        /// <summary>Quantity</summary>
        [JsonPropertyName("Qty")]
        public decimal Qty { get; set; }

        /// <summary>Free quantity</summary>
        [JsonPropertyName("FreeQty")]
        public decimal? FreeQty { get; set; }

        /// <summary>Unit (UQC code)</summary>
        [JsonPropertyName("Unit")]
        public string Unit { get; set; } = "OTH";

        /// <summary>Unit price</summary>
        [JsonPropertyName("UnitPrice")]
        public decimal UnitPrice { get; set; }

        /// <summary>Total amount before discount</summary>
        [JsonPropertyName("TotAmt")]
        public decimal TotAmt { get; set; }

        /// <summary>Discount amount</summary>
        [JsonPropertyName("Discount")]
        public decimal Discount { get; set; }

        /// <summary>Pre-tax value (after discount)</summary>
        [JsonPropertyName("PreTaxVal")]
        public decimal? PreTaxVal { get; set; }

        /// <summary>Assessable value (taxable value)</summary>
        [JsonPropertyName("AssAmt")]
        public decimal AssAmt { get; set; }

        /// <summary>GST rate</summary>
        [JsonPropertyName("GstRt")]
        public decimal GstRt { get; set; }

        /// <summary>IGST amount</summary>
        [JsonPropertyName("IgstAmt")]
        public decimal IgstAmt { get; set; }

        /// <summary>CGST amount</summary>
        [JsonPropertyName("CgstAmt")]
        public decimal CgstAmt { get; set; }

        /// <summary>SGST amount</summary>
        [JsonPropertyName("SgstAmt")]
        public decimal SgstAmt { get; set; }

        /// <summary>Cess rate</summary>
        [JsonPropertyName("CesRt")]
        public decimal? CesRt { get; set; }

        /// <summary>Cess amount</summary>
        [JsonPropertyName("CesAmt")]
        public decimal CesAmt { get; set; }

        /// <summary>Cess non-advol amount</summary>
        [JsonPropertyName("CesNonAdvlAmt")]
        public decimal? CesNonAdvlAmt { get; set; }

        /// <summary>State cess rate</summary>
        [JsonPropertyName("StateCesRt")]
        public decimal? StateCesRt { get; set; }

        /// <summary>State cess amount</summary>
        [JsonPropertyName("StateCesAmt")]
        public decimal? StateCesAmt { get; set; }

        /// <summary>State cess non-advol amount</summary>
        [JsonPropertyName("StateCesNonAdvlAmt")]
        public decimal? StateCesNonAdvlAmt { get; set; }

        /// <summary>Other charges</summary>
        [JsonPropertyName("OthChrg")]
        public decimal? OthChrg { get; set; }

        /// <summary>Total item value</summary>
        [JsonPropertyName("TotItemVal")]
        public decimal TotItemVal { get; set; }

        /// <summary>Order line reference</summary>
        [JsonPropertyName("OrdLineRef")]
        public string? OrdLineRef { get; set; }

        /// <summary>Origin country</summary>
        [JsonPropertyName("OrgCntry")]
        public string? OrgCntry { get; set; }

        /// <summary>PO line reference</summary>
        [JsonPropertyName("PrdSlNo")]
        public string? PrdSlNo { get; set; }

        /// <summary>Batch details</summary>
        [JsonPropertyName("BchDtls")]
        public BatchDetails? BchDtls { get; set; }

        /// <summary>Attribute details</summary>
        [JsonPropertyName("AttribDtls")]
        public List<AttributeDetails>? AttribDtls { get; set; }
    }

    public class BatchDetails
    {
        [JsonPropertyName("Nm")]
        public string? Nm { get; set; }

        [JsonPropertyName("ExpDt")]
        public string? ExpDt { get; set; }

        [JsonPropertyName("WrDt")]
        public string? WrDt { get; set; }
    }

    public class AttributeDetails
    {
        [JsonPropertyName("Nm")]
        public string? Nm { get; set; }

        [JsonPropertyName("Val")]
        public string? Val { get; set; }
    }

    public class ValueDetails
    {
        /// <summary>Total assessable value</summary>
        [JsonPropertyName("AssVal")]
        public decimal AssVal { get; set; }

        /// <summary>Total CGST value</summary>
        [JsonPropertyName("CgstVal")]
        public decimal CgstVal { get; set; }

        /// <summary>Total SGST value</summary>
        [JsonPropertyName("SgstVal")]
        public decimal SgstVal { get; set; }

        /// <summary>Total IGST value</summary>
        [JsonPropertyName("IgstVal")]
        public decimal IgstVal { get; set; }

        /// <summary>Total cess value</summary>
        [JsonPropertyName("CesVal")]
        public decimal CesVal { get; set; }

        /// <summary>State cess value</summary>
        [JsonPropertyName("StCesVal")]
        public decimal? StCesVal { get; set; }

        /// <summary>Discount</summary>
        [JsonPropertyName("Discount")]
        public decimal? Discount { get; set; }

        /// <summary>Other charges</summary>
        [JsonPropertyName("OthChrg")]
        public decimal? OthChrg { get; set; }

        /// <summary>Round off amount</summary>
        [JsonPropertyName("RndOffAmt")]
        public decimal? RndOffAmt { get; set; }

        /// <summary>Total invoice value</summary>
        [JsonPropertyName("TotInvVal")]
        public decimal TotInvVal { get; set; }

        /// <summary>Total invoice value in foreign currency</summary>
        [JsonPropertyName("TotInvValFc")]
        public decimal? TotInvValFc { get; set; }
    }

    public class PaymentDetails
    {
        /// <summary>Payee name</summary>
        [JsonPropertyName("Nm")]
        public string? Nm { get; set; }

        /// <summary>Account number</summary>
        [JsonPropertyName("AccDet")]
        public string? AccDet { get; set; }

        /// <summary>Mode of payment</summary>
        [JsonPropertyName("Mode")]
        public string? Mode { get; set; }

        /// <summary>IFSC code</summary>
        [JsonPropertyName("FinInsBr")]
        public string? FinInsBr { get; set; }

        /// <summary>Payment terms</summary>
        [JsonPropertyName("PayTerm")]
        public string? PayTerm { get; set; }

        /// <summary>Payment instruction</summary>
        [JsonPropertyName("PayInstr")]
        public string? PayInstr { get; set; }

        /// <summary>Credit transfer</summary>
        [JsonPropertyName("CrTrn")]
        public string? CrTrn { get; set; }

        /// <summary>Direct debit</summary>
        [JsonPropertyName("DirDr")]
        public string? DirDr { get; set; }

        /// <summary>Credit days</summary>
        [JsonPropertyName("CrDay")]
        public int? CrDay { get; set; }

        /// <summary>Paid amount</summary>
        [JsonPropertyName("PaidAmt")]
        public decimal? PaidAmt { get; set; }

        /// <summary>Payable amount due</summary>
        [JsonPropertyName("PaymtDue")]
        public decimal? PaymtDue { get; set; }
    }

    public class ReferenceDetails
    {
        /// <summary>Invoice remarks</summary>
        [JsonPropertyName("InvRm")]
        public string? InvRm { get; set; }

        /// <summary>Document period start date</summary>
        [JsonPropertyName("DocPerdStDt")]
        public string? DocPerdStDt { get; set; }

        /// <summary>Document period end date</summary>
        [JsonPropertyName("DocPerdEndDt")]
        public string? DocPerdEndDt { get; set; }

        /// <summary>Preceding document details (for credit/debit notes)</summary>
        [JsonPropertyName("PrecDocDtls")]
        public List<PrecedingDocDetails>? PrecDocDtls { get; set; }

        /// <summary>Contract reference details</summary>
        [JsonPropertyName("ContrDtls")]
        public List<ContractDetails>? ContrDtls { get; set; }
    }

    public class PrecedingDocDetails
    {
        [JsonPropertyName("InvNo")]
        public string? InvNo { get; set; }

        [JsonPropertyName("InvDt")]
        public string? InvDt { get; set; }

        [JsonPropertyName("OthRefNo")]
        public string? OthRefNo { get; set; }
    }

    public class ContractDetails
    {
        [JsonPropertyName("RecAdvRefr")]
        public string? RecAdvRefr { get; set; }

        [JsonPropertyName("RecAdvDt")]
        public string? RecAdvDt { get; set; }

        [JsonPropertyName("TendRefr")]
        public string? TendRefr { get; set; }

        [JsonPropertyName("ContrRefr")]
        public string? ContrRefr { get; set; }

        [JsonPropertyName("ExtRefr")]
        public string? ExtRefr { get; set; }

        [JsonPropertyName("ProjRefr")]
        public string? ProjRefr { get; set; }

        [JsonPropertyName("PORefr")]
        public string? PORefr { get; set; }

        [JsonPropertyName("PORefDt")]
        public string? PORefDt { get; set; }
    }

    public class AdditionalDocDetails
    {
        [JsonPropertyName("Url")]
        public string? Url { get; set; }

        [JsonPropertyName("Docs")]
        public string? Docs { get; set; }

        [JsonPropertyName("Info")]
        public string? Info { get; set; }
    }

    public class ExportDetails
    {
        /// <summary>Shipping bill number</summary>
        [JsonPropertyName("ShipBNo")]
        public string? ShipBNo { get; set; }

        /// <summary>Shipping bill date (DD/MM/YYYY)</summary>
        [JsonPropertyName("ShipBDt")]
        public string? ShipBDt { get; set; }

        /// <summary>Port code</summary>
        [JsonPropertyName("Port")]
        public string? Port { get; set; }

        /// <summary>Refund claim: Y/N</summary>
        [JsonPropertyName("RefClm")]
        public string? RefClm { get; set; }

        /// <summary>Foreign currency code</summary>
        [JsonPropertyName("ForCur")]
        public string? ForCur { get; set; }

        /// <summary>Country code</summary>
        [JsonPropertyName("CntCode")]
        public string? CntCode { get; set; }

        /// <summary>Export duty</summary>
        [JsonPropertyName("ExpDuty")]
        public decimal? ExpDuty { get; set; }
    }

    public class EwayBillDetails
    {
        /// <summary>Transporter ID (GSTIN)</summary>
        [JsonPropertyName("TransId")]
        public string? TransId { get; set; }

        /// <summary>Transporter name</summary>
        [JsonPropertyName("TransName")]
        public string? TransName { get; set; }

        /// <summary>
        /// Mode of transport:
        /// 1=Road, 2=Rail, 3=Air, 4=Ship
        /// </summary>
        [JsonPropertyName("TransMode")]
        public string? TransMode { get; set; }

        /// <summary>Distance in KM</summary>
        [JsonPropertyName("Distance")]
        public int? Distance { get; set; }

        /// <summary>Transporter document number</summary>
        [JsonPropertyName("TransDocNo")]
        public string? TransDocNo { get; set; }

        /// <summary>Transporter document date</summary>
        [JsonPropertyName("TransDocDt")]
        public string? TransDocDt { get; set; }

        /// <summary>Vehicle number</summary>
        [JsonPropertyName("VehNo")]
        public string? VehNo { get; set; }

        /// <summary>
        /// Vehicle type:
        /// R=Regular, O=Over Dimensional Cargo
        /// </summary>
        [JsonPropertyName("VehType")]
        public string? VehType { get; set; }
    }

    /// <summary>
    /// Supply type codes for e-invoice
    /// </summary>
    public static class SupplyTypeCodes
    {
        public const string B2B = "B2B";
        public const string B2C = "B2C";
        public const string SEZWP = "SEZWP"; // SEZ with payment
        public const string SEZWOP = "SEZWOP"; // SEZ without payment
        public const string EXPWP = "EXPWP"; // Export with payment
        public const string EXPWOP = "EXPWOP"; // Export without payment (under bond/LUT)
        public const string DEXP = "DEXP"; // Deemed export
    }

    /// <summary>
    /// Document type codes
    /// </summary>
    public static class DocumentTypeCodes
    {
        public const string Invoice = "INV";
        public const string CreditNote = "CRN";
        public const string DebitNote = "DBN";
    }

    /// <summary>
    /// State codes for India (first 2 digits of GSTIN)
    /// </summary>
    public static class IndianStateCodes
    {
        public const string JammuKashmir = "01";
        public const string HimachalPradesh = "02";
        public const string Punjab = "03";
        public const string Chandigarh = "04";
        public const string Uttarakhand = "05";
        public const string Haryana = "06";
        public const string Delhi = "07";
        public const string Rajasthan = "08";
        public const string UttarPradesh = "09";
        public const string Bihar = "10";
        public const string Sikkim = "11";
        public const string ArunachalPradesh = "12";
        public const string Nagaland = "13";
        public const string Manipur = "14";
        public const string Mizoram = "15";
        public const string Tripura = "16";
        public const string Meghalaya = "17";
        public const string Assam = "18";
        public const string WestBengal = "19";
        public const string Jharkhand = "20";
        public const string Odisha = "21";
        public const string Chhattisgarh = "22";
        public const string MadhyaPradesh = "23";
        public const string Gujarat = "24";
        public const string DadraNagarHaveliDamanDiu = "26";
        public const string Maharashtra = "27";
        public const string AndhraPradesh = "28";
        public const string Karnataka = "29";
        public const string Goa = "30";
        public const string Lakshadweep = "31";
        public const string Kerala = "32";
        public const string TamilNadu = "33";
        public const string Puducherry = "34";
        public const string AndamanNicobar = "35";
        public const string Telangana = "36";
        public const string LadakhJK = "37";
        public const string OtherCountry = "97"; // For exports
    }
}
