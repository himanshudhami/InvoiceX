namespace Core.Entities
{
    public class Customers
    {
public Guid Id { get; set; }
public Guid? CompanyId { get; set; }
public string Name { get; set; } = string.Empty;
public string? CompanyName { get; set; }
public string? Email { get; set; }
public string? Phone { get; set; }
public string? AddressLine1 { get; set; }
public string? AddressLine2 { get; set; }
public string? City { get; set; }
public string? State { get; set; }
public string? ZipCode { get; set; }
public string? Country { get; set; }
public string? TaxNumber { get; set; }
public string? Notes { get; set; }
public decimal? CreditLimit { get; set; }
public int? PaymentTerms { get; set; }
public bool? IsActive { get; set; }
public DateTime? CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
}
}