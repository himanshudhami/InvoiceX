using Application.DTOs.Payroll;
using Core.Interfaces.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for Professional Tax (PT) calculations
/// PT varies by state in India. Key states:
/// - Karnataka: ₹200/month (if salary > ₹15,000)
/// - Maharashtra: ₹200/month (Feb), ₹175/month (other months) for salary > ₹10,000
/// - Tamil Nadu: ₹0 (no PT)
/// - Gujarat: ₹200/month max
/// - Delhi: No PT
/// </summary>
public class ProfessionalTaxCalculationService
{
    private readonly IProfessionalTaxSlabRepository _ptSlabRepository;

    public ProfessionalTaxCalculationService(IProfessionalTaxSlabRepository ptSlabRepository)
    {
        _ptSlabRepository = ptSlabRepository;
    }

    /// <summary>
    /// Calculate PT based on state and gross salary
    /// </summary>
    public async Task<PtCalculationDto> CalculateAsync(
        decimal grossSalary,
        string state,
        int paymentMonth,
        bool isPtApplicable)
    {
        var result = new PtCalculationDto
        {
            State = state,
            GrossSalary = grossSalary,
            PaymentMonth = paymentMonth
        };

        if (!isPtApplicable || string.IsNullOrEmpty(state))
        {
            return result;
        }

        // Get the applicable PT slab from database
        var slab = await _ptSlabRepository.GetSlabForIncomeAsync(grossSalary, state);

        if (slab != null)
        {
            result.TaxAmount = slab.MonthlyTax;
        }
        else
        {
            // Fallback to hardcoded values if not found in database
            result.TaxAmount = CalculateDefaultPt(grossSalary, state, paymentMonth);
        }

        return result;
    }

    /// <summary>
    /// Calculate PT synchronously using default rules
    /// </summary>
    public PtCalculationDto Calculate(
        decimal grossSalary,
        string state,
        int paymentMonth,
        bool isPtApplicable)
    {
        var result = new PtCalculationDto
        {
            State = state,
            GrossSalary = grossSalary,
            PaymentMonth = paymentMonth
        };

        if (!isPtApplicable || string.IsNullOrEmpty(state))
        {
            return result;
        }

        result.TaxAmount = CalculateDefaultPt(grossSalary, state, paymentMonth);
        return result;
    }

    /// <summary>
    /// Default PT calculation based on common Indian state rules
    /// </summary>
    private decimal CalculateDefaultPt(decimal grossSalary, string state, int paymentMonth)
    {
        var stateLower = state.ToLowerInvariant();

        return stateLower switch
        {
            "karnataka" => CalculateKarnatakaPt(grossSalary),
            "maharashtra" => CalculateMaharashtraPt(grossSalary, paymentMonth),
            "tamil nadu" or "tamilnadu" => 0, // No PT in Tamil Nadu
            "delhi" => 0, // No PT in Delhi
            "gujarat" => CalculateGujaratPt(grossSalary),
            "west bengal" or "westbengal" => CalculateWestBengalPt(grossSalary),
            "andhra pradesh" or "andhrapradesh" => CalculateAndhraPt(grossSalary),
            "telangana" => CalculateTelanganaPt(grossSalary),
            "kerala" => CalculateKeralaPt(grossSalary),
            "madhya pradesh" or "madhyapradesh" => CalculateMadhyaPradeshPt(grossSalary),
            _ => 200m // Default for other states
        };
    }

    private decimal CalculateKarnatakaPt(decimal grossSalary)
    {
        // Karnataka PT slabs (monthly)
        if (grossSalary <= 15000) return 0;
        return 200m;
    }

    private decimal CalculateMaharashtraPt(decimal grossSalary, int paymentMonth)
    {
        // Maharashtra PT slabs (monthly)
        if (grossSalary <= 7500) return 0;
        if (grossSalary <= 10000) return paymentMonth == 2 ? 0 : 175m;
        return paymentMonth == 2 ? 300m : 200m; // February has higher PT
    }

    private decimal CalculateGujaratPt(decimal grossSalary)
    {
        // Gujarat PT slabs (monthly)
        if (grossSalary <= 5999) return 0;
        if (grossSalary <= 8999) return 80m;
        if (grossSalary <= 11999) return 150m;
        return 200m;
    }

    private decimal CalculateWestBengalPt(decimal grossSalary)
    {
        // West Bengal PT slabs (monthly)
        if (grossSalary <= 10000) return 0;
        if (grossSalary <= 15000) return 110m;
        if (grossSalary <= 25000) return 130m;
        if (grossSalary <= 40000) return 150m;
        return 200m;
    }

    private decimal CalculateAndhraPt(decimal grossSalary)
    {
        // Andhra Pradesh PT slabs (monthly)
        if (grossSalary <= 15000) return 0;
        if (grossSalary <= 20000) return 150m;
        return 200m;
    }

    private decimal CalculateTelanganaPt(decimal grossSalary)
    {
        // Telangana PT slabs (monthly)
        if (grossSalary <= 15000) return 0;
        if (grossSalary <= 20000) return 150m;
        return 200m;
    }

    private decimal CalculateKeralaPt(decimal grossSalary)
    {
        // Kerala PT slabs (half-yearly, divided by 6 for monthly)
        if (grossSalary <= 11999) return 0;
        if (grossSalary <= 17999) return 120m / 6;
        if (grossSalary <= 29999) return 180m / 6;
        if (grossSalary <= 41999) return 300m / 6;
        if (grossSalary <= 59999) return 450m / 6;
        return 600m / 6;
    }

    private decimal CalculateMadhyaPradeshPt(decimal grossSalary)
    {
        // Madhya Pradesh PT slabs (monthly)
        if (grossSalary <= 18750) return 0;
        if (grossSalary <= 25000) return 125m;
        return 208m;
    }

    /// <summary>
    /// Calculate annual PT for budgeting
    /// Note: Some states have different rates for February
    /// </summary>
    public decimal CalculateAnnualPt(decimal monthlyGross, string state, bool isPtApplicable)
    {
        if (!isPtApplicable)
            return 0;

        var total = 0m;
        for (int month = 1; month <= 12; month++)
        {
            total += CalculateDefaultPt(monthlyGross, state, month);
        }

        return total;
    }
}
