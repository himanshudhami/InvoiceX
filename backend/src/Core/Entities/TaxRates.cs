namespace Core.Entities
{
    public class TaxRates
    {
public Guid Id { get; set; }
public Guid? CompanyId { get; set; }
public string Name { get; set; } = string.Empty;
public decimal Rate { get; set; }
public bool? IsDefault { get; set; }
public bool? IsActive { get; set; }
public DateTime? CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
}
}