using System;

namespace Core.Entities;

public class Assets
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ModelId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string Status { get; set; } = "available";
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? Vendor { get; set; }
    public string? PurchaseType { get; set; }
    public string? InvoiceReference { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? InServiceDate { get; set; }
    public DateTime? DepreciationStartDate { get; set; }
    public DateTime? WarrantyExpiration { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string? Currency { get; set; }
    public string? DepreciationMethod { get; set; }
    public int? UsefulLifeMonths { get; set; }
    public decimal? SalvageValue { get; set; }
    public decimal? ResidualBookValue { get; set; }
    public string? CustomProperties { get; set; } // JSONB
    public string? Notes { get; set; }
    // Loan-related fields
    public Guid? LinkedLoanId { get; set; }
    public decimal? DownPaymentAmount { get; set; }
    public decimal? GstAmount { get; set; }
    public decimal? GstRate { get; set; }
    public bool? ItcEligible { get; set; }
    public decimal? TdsOnInterest { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}




