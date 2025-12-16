namespace Core.Entities
{
    public class Products
    {
        public Guid Id { get; set; }
        public Guid? CompanyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Sku { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public decimal? TaxRate { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // GST Compliance fields
        public string? HsnSacCode { get; set; }
        public bool IsService { get; set; } = true;
        public decimal DefaultGstRate { get; set; } = 18;
        public decimal CessRate { get; set; }
    }
}