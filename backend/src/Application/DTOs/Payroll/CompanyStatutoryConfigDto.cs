namespace Application.DTOs.Payroll;

public class CompanyStatutoryConfigDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    // PF Configuration
    public bool PfEnabled { get; set; }
    public string? PfEstablishmentCode { get; set; }
    public decimal PfEmployeeRate { get; set; }
    public decimal PfEmployerRate { get; set; }
    public decimal PfWageCeiling { get; set; }

    // ESI Configuration
    public bool EsiEnabled { get; set; }
    public string? EsiCode { get; set; }
    public decimal EsiEmployeeRate { get; set; }
    public decimal EsiEmployerRate { get; set; }
    public decimal EsiWageCeiling { get; set; }

    // PT Configuration
    public bool PtEnabled { get; set; }
    public string? PtState { get; set; }
    public string? PtRegistrationNumber { get; set; }

    // LWF Configuration
    public bool LwfEnabled { get; set; }
    public decimal LwfEmployeeAmount { get; set; }
    public decimal LwfEmployerAmount { get; set; }

    // Gratuity Configuration
    public bool GratuityEnabled { get; set; }
    public decimal GratuityRate { get; set; }

    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public string? CompanyName { get; set; }
}

public class CreateCompanyStatutoryConfigDto
{
    public Guid CompanyId { get; set; }

    // PF Configuration
    public bool PfEnabled { get; set; } = true;
    public string? PfEstablishmentCode { get; set; }
    public decimal PfEmployeeRate { get; set; } = 12.00m;
    public decimal PfEmployerRate { get; set; } = 12.00m;
    public decimal PfWageCeiling { get; set; } = 15000.00m;

    // ESI Configuration
    public bool EsiEnabled { get; set; } = false;
    public string? EsiCode { get; set; }
    public decimal EsiEmployeeRate { get; set; } = 0.75m;
    public decimal EsiEmployerRate { get; set; } = 3.25m;
    public decimal EsiWageCeiling { get; set; } = 21000.00m;

    // PT Configuration
    public bool PtEnabled { get; set; } = true;
    public string? PtState { get; set; }
    public string? PtRegistrationNumber { get; set; }

    // LWF Configuration
    public bool LwfEnabled { get; set; } = false;
    public decimal LwfEmployeeAmount { get; set; }
    public decimal LwfEmployerAmount { get; set; }

    // Gratuity Configuration
    public bool GratuityEnabled { get; set; } = false;
    public decimal GratuityRate { get; set; } = 4.81m;
}

public class UpdateCompanyStatutoryConfigDto
{
    // PF Configuration
    public bool? PfEnabled { get; set; }
    public string? PfEstablishmentCode { get; set; }
    public decimal? PfEmployeeRate { get; set; }
    public decimal? PfEmployerRate { get; set; }
    public decimal? PfWageCeiling { get; set; }

    // ESI Configuration
    public bool? EsiEnabled { get; set; }
    public string? EsiCode { get; set; }
    public decimal? EsiEmployeeRate { get; set; }
    public decimal? EsiEmployerRate { get; set; }
    public decimal? EsiWageCeiling { get; set; }

    // PT Configuration
    public bool? PtEnabled { get; set; }
    public string? PtState { get; set; }
    public string? PtRegistrationNumber { get; set; }

    // LWF Configuration
    public bool? LwfEnabled { get; set; }
    public decimal? LwfEmployeeAmount { get; set; }
    public decimal? LwfEmployerAmount { get; set; }

    // Gratuity Configuration
    public bool? GratuityEnabled { get; set; }
    public decimal? GratuityRate { get; set; }

    public bool? IsActive { get; set; }
}
