namespace Core.Entities
{
    public class Companies
    {
public Guid Id { get; set; }
public string Name { get; set; } = string.Empty;
public string? LogoUrl { get; set; }
public string? AddressLine1 { get; set; }
public string? AddressLine2 { get; set; }
public string? City { get; set; }
public string? State { get; set; }
public string? ZipCode { get; set; }
public string? Country { get; set; }
public string? Email { get; set; }
public string? Phone { get; set; }
public string? Website { get; set; }
public string? TaxNumber { get; set; }
public string? PaymentInstructions { get; set; }
public Guid? InvoiceTemplateId { get; set; }
public string? SignatureType { get; set; }
public string? SignatureData { get; set; }
public string? SignatureName { get; set; }
public string? SignatureFont { get; set; }
public string? SignatureColor { get; set; }
public DateTime? CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
}
}