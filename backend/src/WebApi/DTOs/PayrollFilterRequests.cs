namespace WebApi.DTOs;

public class BasePayrollFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
}

public class PayrollRunFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public int? PayrollMonth { get; set; }
    public int? PayrollYear { get; set; }
    public string? FinancialYear { get; set; }
    public string? Status { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (PayrollMonth.HasValue) filters["payroll_month"] = PayrollMonth.Value;
        if (PayrollYear.HasValue) filters["payroll_year"] = PayrollYear.Value;
        if (!string.IsNullOrEmpty(FinancialYear)) filters["financial_year"] = FinancialYear;
        if (!string.IsNullOrEmpty(Status)) filters["status"] = Status;
        return filters;
    }
}

public class PayrollTransactionFilterRequest : BasePayrollFilterRequest
{
    public Guid? PayrollRunId { get; set; }
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public int? PayrollMonth { get; set; }
    public int? PayrollYear { get; set; }
    public string? Status { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (PayrollRunId.HasValue) filters["payroll_run_id"] = PayrollRunId.Value;
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (EmployeeId.HasValue) filters["employee_id"] = EmployeeId.Value;
        if (PayrollMonth.HasValue) filters["payroll_month"] = PayrollMonth.Value;
        if (PayrollYear.HasValue) filters["payroll_year"] = PayrollYear.Value;
        if (!string.IsNullOrEmpty(Status)) filters["status"] = Status;
        return filters;
    }
}

public class ContractorPaymentFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? PartyId { get; set; }
    public int? PaymentMonth { get; set; }
    public int? PaymentYear { get; set; }
    public string? Status { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (PartyId.HasValue) filters["party_id"] = PartyId.Value;
        if (PaymentMonth.HasValue) filters["payment_month"] = PaymentMonth.Value;
        if (PaymentYear.HasValue) filters["payment_year"] = PaymentYear.Value;
        if (!string.IsNullOrEmpty(Status)) filters["status"] = Status;
        return filters;
    }
}

public class EmployeePayrollInfoFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? PayrollType { get; set; }
    public bool? IsActive { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (EmployeeId.HasValue) filters["employee_id"] = EmployeeId.Value;
        if (!string.IsNullOrEmpty(PayrollType)) filters["payroll_type"] = PayrollType;
        if (IsActive.HasValue) filters["is_active"] = IsActive.Value;
        return filters;
    }
}

public class EmployeeSalaryStructureFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public bool? IsActive { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (EmployeeId.HasValue) filters["employee_id"] = EmployeeId.Value;
        if (IsActive.HasValue) filters["is_active"] = IsActive.Value;
        return filters;
    }
}

public class EmployeeTaxDeclarationFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? FinancialYear { get; set; }
    public string? Status { get; set; }
    public string? TaxRegime { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (EmployeeId.HasValue) filters["employee_id"] = EmployeeId.Value;
        if (!string.IsNullOrEmpty(FinancialYear)) filters["financial_year"] = FinancialYear;
        if (!string.IsNullOrEmpty(Status)) filters["status"] = Status;
        if (!string.IsNullOrEmpty(TaxRegime)) filters["tax_regime"] = TaxRegime;
        return filters;
    }
}

public class CompanyStatutoryConfigFilterRequest : BasePayrollFilterRequest
{
    public Guid? CompanyId { get; set; }
    public bool? IsActive { get; set; }
    public bool? PfEnabled { get; set; }
    public bool? EsiEnabled { get; set; }
    public bool? PtEnabled { get; set; }

    public Dictionary<string, object> GetFilters()
    {
        var filters = new Dictionary<string, object>();
        if (CompanyId.HasValue) filters["company_id"] = CompanyId.Value;
        if (IsActive.HasValue) filters["is_active"] = IsActive.Value;
        if (PfEnabled.HasValue) filters["pf_enabled"] = PfEnabled.Value;
        if (EsiEnabled.HasValue) filters["esi_enabled"] = EsiEnabled.Value;
        if (PtEnabled.HasValue) filters["pt_enabled"] = PtEnabled.Value;
        return filters;
    }
}
