namespace Application.DTOs.Gst
{
    /// <summary>
    /// Complete GSTR-3B filing data
    /// </summary>
    public class Gstr3bFilingDto
    {
        public Guid Id { get; set; }
        public Guid CompanyId { get; set; }
        public string Gstin { get; set; } = string.Empty;
        public string ReturnPeriod { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Status { get; set; } = "draft";

        // Timestamps
        public DateTime? GeneratedAt { get; set; }
        public Guid? GeneratedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public Guid? ReviewedBy { get; set; }
        public DateTime? FiledAt { get; set; }
        public Guid? FiledBy { get; set; }

        // Filing details
        public string? Arn { get; set; }
        public DateTime? FilingDate { get; set; }

        // Tables
        public Gstr3bTable31Dto? Table31 { get; set; }
        public Gstr3bTable4Dto? Table4 { get; set; }
        public Gstr3bTable5Dto? Table5 { get; set; }

        // Variance
        public Gstr3bVarianceSummaryDto? Variance { get; set; }

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to generate GSTR-3B filing pack
    /// </summary>
    public class GenerateGstr3bRequestDto
    {
        public Guid CompanyId { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public bool Regenerate { get; set; } = false;
    }

    // ==================== Table 3.1: Outward Supplies ====================

    /// <summary>
    /// Table 3.1 - Details of outward supplies and inward supplies liable to reverse charge
    /// </summary>
    public class Gstr3bTable31Dto
    {
        /// <summary>
        /// 3.1(a) - Outward taxable supplies (other than zero rated, nil rated and exempted)
        /// </summary>
        public Gstr3bRowDto OutwardTaxable { get; set; } = new();

        /// <summary>
        /// 3.1(b) - Outward taxable supplies (zero rated)
        /// </summary>
        public Gstr3bRowDto OutwardZeroRated { get; set; } = new();

        /// <summary>
        /// 3.1(c) - Other outward supplies (Nil rated, exempted)
        /// </summary>
        public Gstr3bRowDto OtherOutward { get; set; } = new();

        /// <summary>
        /// 3.1(d) - Inward supplies (liable to reverse charge)
        /// </summary>
        public Gstr3bRowDto InwardRcm { get; set; } = new();

        /// <summary>
        /// 3.1(e) - Non-GST outward supplies
        /// </summary>
        public Gstr3bRowDto NonGst { get; set; } = new();

        /// <summary>
        /// Total of all rows
        /// </summary>
        public Gstr3bRowDto Total => new()
        {
            TaxableValue = OutwardTaxable.TaxableValue + OutwardZeroRated.TaxableValue +
                          OtherOutward.TaxableValue + InwardRcm.TaxableValue + NonGst.TaxableValue,
            Igst = OutwardTaxable.Igst + OutwardZeroRated.Igst + InwardRcm.Igst,
            Cgst = OutwardTaxable.Cgst + InwardRcm.Cgst,
            Sgst = OutwardTaxable.Sgst + InwardRcm.Sgst,
            Cess = OutwardTaxable.Cess + OutwardZeroRated.Cess + InwardRcm.Cess
        };
    }

    // ==================== Table 4: ITC ====================

    /// <summary>
    /// Table 4 - Eligible ITC
    /// </summary>
    public class Gstr3bTable4Dto
    {
        /// <summary>
        /// Table 4(A) - ITC Available (whether in full or part)
        /// </summary>
        public Gstr3bItcAvailableDto ItcAvailable { get; set; } = new();

        /// <summary>
        /// Table 4(B) - ITC Reversed
        /// </summary>
        public Gstr3bItcReversedDto ItcReversed { get; set; } = new();

        /// <summary>
        /// Table 4(C) - Net ITC Available (A - B)
        /// </summary>
        public Gstr3bItcRowDto NetItcAvailable => new()
        {
            Igst = ItcAvailable.Total.Igst - ItcReversed.Total.Igst,
            Cgst = ItcAvailable.Total.Cgst - ItcReversed.Total.Cgst,
            Sgst = ItcAvailable.Total.Sgst - ItcReversed.Total.Sgst,
            Cess = ItcAvailable.Total.Cess - ItcReversed.Total.Cess
        };

        /// <summary>
        /// Table 4(D) - Ineligible ITC
        /// </summary>
        public Gstr3bItcIneligibleDto ItcIneligible { get; set; } = new();
    }

    /// <summary>
    /// Table 4(A) - ITC Available
    /// </summary>
    public class Gstr3bItcAvailableDto
    {
        /// <summary>
        /// 4(A)(1) - Import of goods
        /// </summary>
        public Gstr3bItcRowDto ImportGoods { get; set; } = new();

        /// <summary>
        /// 4(A)(2) - Import of services
        /// </summary>
        public Gstr3bItcRowDto ImportServices { get; set; } = new();

        /// <summary>
        /// 4(A)(3) - Inward supplies liable to reverse charge (other than 1 & 2 above)
        /// </summary>
        public Gstr3bItcRowDto RcmInward { get; set; } = new();

        /// <summary>
        /// 4(A)(4) - Inward supplies from ISD
        /// </summary>
        public Gstr3bItcRowDto IsdInward { get; set; } = new();

        /// <summary>
        /// 4(A)(5) - All other ITC
        /// </summary>
        public Gstr3bItcRowDto AllOtherItc { get; set; } = new();

        /// <summary>
        /// Total ITC Available
        /// </summary>
        public Gstr3bItcRowDto Total => new()
        {
            Igst = ImportGoods.Igst + ImportServices.Igst + RcmInward.Igst + IsdInward.Igst + AllOtherItc.Igst,
            Cgst = ImportGoods.Cgst + ImportServices.Cgst + RcmInward.Cgst + IsdInward.Cgst + AllOtherItc.Cgst,
            Sgst = ImportGoods.Sgst + ImportServices.Sgst + RcmInward.Sgst + IsdInward.Sgst + AllOtherItc.Sgst,
            Cess = ImportGoods.Cess + ImportServices.Cess + RcmInward.Cess + IsdInward.Cess + AllOtherItc.Cess
        };
    }

    /// <summary>
    /// Table 4(B) - ITC Reversed
    /// </summary>
    public class Gstr3bItcReversedDto
    {
        /// <summary>
        /// 4(B)(1) - As per rules 42 & 43 of CGST Rules
        /// </summary>
        public Gstr3bItcRowDto Rule42_43 { get; set; } = new();

        /// <summary>
        /// 4(B)(2) - Others
        /// </summary>
        public Gstr3bItcRowDto Others { get; set; } = new();

        /// <summary>
        /// Total ITC Reversed
        /// </summary>
        public Gstr3bItcRowDto Total => new()
        {
            Igst = Rule42_43.Igst + Others.Igst,
            Cgst = Rule42_43.Cgst + Others.Cgst,
            Sgst = Rule42_43.Sgst + Others.Sgst,
            Cess = Rule42_43.Cess + Others.Cess
        };
    }

    /// <summary>
    /// Table 4(D) - Ineligible ITC
    /// </summary>
    public class Gstr3bItcIneligibleDto
    {
        /// <summary>
        /// 4(D)(1) - As per section 17(5)
        /// </summary>
        public Gstr3bItcRowDto Section17_5 { get; set; } = new();

        /// <summary>
        /// 4(D)(2) - Others
        /// </summary>
        public Gstr3bItcRowDto Others { get; set; } = new();

        /// <summary>
        /// Total Ineligible ITC
        /// </summary>
        public Gstr3bItcRowDto Total => new()
        {
            Igst = Section17_5.Igst + Others.Igst,
            Cgst = Section17_5.Cgst + Others.Cgst,
            Sgst = Section17_5.Sgst + Others.Sgst,
            Cess = Section17_5.Cess + Others.Cess
        };
    }

    // ==================== Table 5: Exempt Supplies ====================

    /// <summary>
    /// Table 5 - Values of exempt, nil-rated and non-GST inward supplies
    /// </summary>
    public class Gstr3bTable5Dto
    {
        /// <summary>
        /// 5(a) - From a supplier under composition scheme, exempt, nil rated
        /// </summary>
        public Gstr3bExemptRowDto InterStateSupplies { get; set; } = new();

        /// <summary>
        /// 5(b) - Non GST supplies
        /// </summary>
        public Gstr3bExemptRowDto IntraStateSupplies { get; set; } = new();
    }

    // ==================== Row DTOs ====================

    /// <summary>
    /// Standard row for Table 3.1 (with taxable value and all tax components)
    /// </summary>
    public class Gstr3bRowDto
    {
        public decimal TaxableValue { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }
        public int SourceCount { get; set; }

        public decimal TotalGst => Igst + Cgst + Sgst + Cess;
    }

    /// <summary>
    /// ITC row for Table 4 (no taxable value)
    /// </summary>
    public class Gstr3bItcRowDto
    {
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }
        public int SourceCount { get; set; }

        public decimal TotalItc => Igst + Cgst + Sgst + Cess;
    }

    /// <summary>
    /// Exempt supply row for Table 5
    /// </summary>
    public class Gstr3bExemptRowDto
    {
        public decimal CompositionTaxablePersons { get; set; }
        public decimal NilRated { get; set; }
        public decimal Exempt { get; set; }
        public decimal NonGst { get; set; }

        public decimal Total => CompositionTaxablePersons + NilRated + Exempt + NonGst;
    }

    // ==================== Line Item & Source Document DTOs ====================

    /// <summary>
    /// Line item with drill-down capability
    /// </summary>
    public class Gstr3bLineItemDto
    {
        public Guid Id { get; set; }
        public string TableCode { get; set; } = string.Empty;
        public int RowOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal TaxableValue { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }
        public int SourceCount { get; set; }
        public string? SourceType { get; set; }
        public string? ComputationNotes { get; set; }
    }

    /// <summary>
    /// Source document for drill-down
    /// </summary>
    public class Gstr3bSourceDocumentDto
    {
        public Guid Id { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public Guid SourceId { get; set; }
        public string? SourceNumber { get; set; }
        public DateTime? SourceDate { get; set; }
        public decimal TaxableValue { get; set; }
        public decimal Igst { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Cess { get; set; }
        public string? PartyName { get; set; }
        public string? PartyGstin { get; set; }
    }

    // ==================== Variance DTOs ====================

    /// <summary>
    /// Variance summary compared to previous period
    /// </summary>
    public class Gstr3bVarianceSummaryDto
    {
        public string PreviousPeriod { get; set; } = string.Empty;
        public List<Gstr3bVarianceItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Individual variance item
    /// </summary>
    public class Gstr3bVarianceItemDto
    {
        public string Field { get; set; } = string.Empty;
        public string TableCode { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal Variance => CurrentValue - PreviousValue;
        public decimal VariancePercent => PreviousValue != 0
            ? Math.Round((Variance / PreviousValue) * 100, 2)
            : (CurrentValue != 0 ? 100 : 0);
        public string? Explanation { get; set; }
    }

    // ==================== Filing Workflow DTOs ====================

    /// <summary>
    /// Request to mark filing as reviewed
    /// </summary>
    public class ReviewGstr3bRequestDto
    {
        public Guid FilingId { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Request to mark filing as filed
    /// </summary>
    public class FileGstr3bRequestDto
    {
        public Guid FilingId { get; set; }
        public string Arn { get; set; } = string.Empty;
        public DateTime FilingDate { get; set; }
    }

    // ==================== Filing History ====================

    /// <summary>
    /// Filing history summary item
    /// </summary>
    public class Gstr3bFilingHistoryDto
    {
        public Guid Id { get; set; }
        public string ReturnPeriod { get; set; } = string.Empty;
        public string FinancialYear { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? GeneratedAt { get; set; }
        public DateTime? FiledAt { get; set; }
        public string? Arn { get; set; }

        // Quick summaries
        public decimal TotalOutwardTax { get; set; }
        public decimal TotalItcClaimed { get; set; }
        public decimal NetTaxPayable { get; set; }
    }
}
