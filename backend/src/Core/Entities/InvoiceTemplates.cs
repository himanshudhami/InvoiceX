namespace Core.Entities
{
    public class InvoiceTemplates
    {
public Guid Id { get; set; }
public Guid? CompanyId { get; set; }
public string Name { get; set; } = string.Empty;
public string TemplateData { get; set; } = string.Empty;
public string? TemplateKey { get; set; }
public string? PreviewUrl { get; set; }
public string? ConfigSchema { get; set; }
public bool? IsDefault { get; set; }
public DateTime? CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
}
}