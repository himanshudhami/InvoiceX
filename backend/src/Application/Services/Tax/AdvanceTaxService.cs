using Application.DTOs.Tax;
using Application.Interfaces.Tax;
using Core.Common;
using Core.Entities.Tax;
using Core.Interfaces;
using Core.Interfaces.Tax;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service for Advance Tax (Section 207) calculations and operations.
    /// Implements corporate advance tax as per Income Tax Act.
    /// </summary>
    public class AdvanceTaxService : IAdvanceTaxService
    {
        private readonly IAdvanceTaxRepository _repository;
        private readonly ICompaniesRepository _companiesRepository;

        // Quarterly cumulative percentages per Section 211
        private static readonly decimal[] QuarterlyCumulativePercentages = { 15m, 45m, 75m, 100m };

        // Corporate tax rates
        private static readonly Dictionary<string, (decimal Rate, decimal Surcharge)> TaxRegimes = new()
        {
            { "normal", (25.00m, 7.00m) },      // Normal rate for turnover > 400 Cr
            { "115BAA", (22.00m, 10.00m) },     // Section 115BAA (no exemptions)
            { "115BAB", (15.00m, 10.00m) },     // Section 115BAB (new manufacturing)
            { "small", (25.00m, 7.00m) }        // Turnover < 400 Cr
        };

        private const decimal CessRate = 4.00m; // Health & Education Cess

        public AdvanceTaxService(
            IAdvanceTaxRepository repository,
            ICompaniesRepository companiesRepository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _companiesRepository = companiesRepository ?? throw new ArgumentNullException(nameof(companiesRepository));
        }

        // ==================== Assessment Operations ====================

        public async Task<Result<AdvanceTaxAssessmentDto>> ComputeAssessmentAsync(
            CreateAdvanceTaxAssessmentDto request,
            Guid userId)
        {
            // Validate company
            var company = await _companiesRepository.GetByIdAsync(request.CompanyId);
            if (company == null)
                return Error.NotFound($"Company {request.CompanyId} not found");

            // Check if assessment already exists
            var existing = await _repository.GetAssessmentByCompanyAndFYAsync(request.CompanyId, request.FinancialYear);
            if (existing != null)
                return Error.Conflict($"Assessment already exists for FY {request.FinancialYear}");

            // Fetch YTD actuals from ledger
            var fyDates = GetFYDateRange(request.FinancialYear);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var ytdThroughDate = today > fyDates.EndDate ? fyDates.EndDate : today;

            var ytd = await _repository.GetYtdFinancialsFromLedgerAsync(
                request.CompanyId,
                fyDates.StartDate,
                ytdThroughDate);

            var ytdRevenue = ytd.YtdIncome;
            var ytdExpenses = ytd.YtdExpenses;

            // Calculate trend-based projections for remaining months
            var monthsCovered = GetMonthsBetween(fyDates.StartDate, ytdThroughDate);
            var remainingMonths = 12 - monthsCovered;

            var avgMonthlyRevenue = monthsCovered > 0 ? ytdRevenue / monthsCovered : 0;
            var avgMonthlyExpenses = monthsCovered > 0 ? ytdExpenses / monthsCovered : 0;

            // Use provided values or calculate from trend
            var projectedAdditionalRevenue = request.ProjectedRevenue.HasValue
                ? request.ProjectedRevenue.Value - ytdRevenue
                : avgMonthlyRevenue * remainingMonths;
            var projectedAdditionalExpenses = request.ProjectedExpenses.HasValue
                ? request.ProjectedExpenses.Value - ytdExpenses
                : avgMonthlyExpenses * remainingMonths;

            // Ensure non-negative projections
            projectedAdditionalRevenue = Math.Max(0, projectedAdditionalRevenue);
            projectedAdditionalExpenses = Math.Max(0, projectedAdditionalExpenses);

            // Full year = YTD + Projected Additional
            var projectedRevenue = ytdRevenue + projectedAdditionalRevenue;
            var projectedExpenses = ytdExpenses + projectedAdditionalExpenses;
            var projectedDepreciation = request.ProjectedDepreciation ?? 0m;
            var projectedOtherIncome = request.ProjectedOtherIncome ?? 0m;

            var profitBeforeTax = projectedRevenue + projectedOtherIncome - projectedExpenses - projectedDepreciation;

            // Book to Taxable Reconciliation
            // Initially, book profit = profit before tax, adjustments are 0
            var bookProfit = profitBeforeTax;
            var totalAdditions = 0m; // User will add disallowances later
            var totalDeductions = 0m; // User will add deductions later
            var taxableIncome = Math.Max(0, bookProfit + totalAdditions - totalDeductions);

            // Get tax rates for regime
            var (taxRate, surchargeRate) = TaxRegimes.GetValueOrDefault(request.TaxRegime, TaxRegimes["normal"]);

            // Compute tax
            var baseTax = taxableIncome * taxRate / 100m;
            var surcharge = baseTax * surchargeRate / 100m;
            var taxPlusSurcharge = baseTax + surcharge;
            var cess = taxPlusSurcharge * CessRate / 100m;
            var totalTaxLiability = baseTax + surcharge + cess;

            // Apply credits - auto-fetch from TDS/TCS modules if not provided
            var tdsReceivable = request.TdsReceivable ??
                await _repository.GetTdsReceivableAsync(request.CompanyId, request.FinancialYear);
            var tcsCredit = request.TcsCredit ??
                await _repository.GetTcsCreditAsync(request.CompanyId, request.FinancialYear);
            var matCredit = request.MatCredit ?? 0m;
            var netTaxPayable = Math.Max(0, totalTaxLiability - tdsReceivable - tcsCredit - matCredit);

            // Create assessment
            var assessment = new AdvanceTaxAssessment
            {
                CompanyId = request.CompanyId,
                FinancialYear = request.FinancialYear,
                AssessmentYear = GetAssessmentYear(request.FinancialYear),
                Status = "draft",

                // YTD actuals (locked)
                YtdRevenue = ytdRevenue,
                YtdExpenses = ytdExpenses,
                YtdThroughDate = ytdThroughDate,

                // Projected additional (editable)
                ProjectedAdditionalRevenue = projectedAdditionalRevenue,
                ProjectedAdditionalExpenses = projectedAdditionalExpenses,

                // Full year projections (computed)
                ProjectedRevenue = projectedRevenue,
                ProjectedExpenses = projectedExpenses,
                ProjectedDepreciation = projectedDepreciation,
                ProjectedOtherIncome = projectedOtherIncome,
                ProjectedProfitBeforeTax = profitBeforeTax,

                // Book to Taxable Reconciliation
                BookProfit = bookProfit,
                TotalAdditions = totalAdditions,
                TotalDeductions = totalDeductions,

                TaxableIncome = taxableIncome,
                TaxRegime = request.TaxRegime,
                TaxRate = taxRate,
                SurchargeRate = surchargeRate,
                CessRate = CessRate,

                BaseTax = baseTax,
                Surcharge = surcharge,
                Cess = cess,
                TotalTaxLiability = totalTaxLiability,

                TdsReceivable = tdsReceivable,
                TcsCredit = tcsCredit,
                MatCredit = matCredit,
                NetTaxPayable = netTaxPayable,

                Notes = request.Notes,
                CreatedBy = userId
            };

            var created = await _repository.CreateAssessmentAsync(assessment);

            // Create quarterly schedules
            var schedules = CreateQuarterlySchedules(created.Id, request.FinancialYear, netTaxPayable);
            await _repository.CreateSchedulesAsync(schedules);

            return await GetAssessmentByIdAsync(created.Id);
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> GetAssessmentByIdAsync(Guid id)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(id);
            if (assessment == null)
                return Error.NotFound($"Assessment {id} not found");

            var schedules = await _repository.GetSchedulesByAssessmentAsync(id);
            var payments = await _repository.GetPaymentsByAssessmentAsync(id);
            var company = await _companiesRepository.GetByIdAsync(assessment.CompanyId);

            return MapToDto(assessment, schedules, payments, company?.Name);
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> GetAssessmentAsync(Guid companyId, string financialYear)
        {
            var assessment = await _repository.GetAssessmentByCompanyAndFYAsync(companyId, financialYear);
            if (assessment == null)
                return Error.NotFound($"No assessment found for FY {financialYear}");

            return await GetAssessmentByIdAsync(assessment.Id);
        }

        public async Task<Result<IEnumerable<AdvanceTaxAssessmentDto>>> GetAssessmentsByCompanyAsync(Guid companyId)
        {
            var assessments = await _repository.GetAssessmentsByCompanyAsync(companyId);
            var company = await _companiesRepository.GetByIdAsync(companyId);

            var result = new List<AdvanceTaxAssessmentDto>();
            foreach (var assessment in assessments)
            {
                var schedules = await _repository.GetSchedulesByAssessmentAsync(assessment.Id);
                var payments = await _repository.GetPaymentsByAssessmentAsync(assessment.Id);
                result.Add(MapToDto(assessment, schedules, payments, company?.Name));
            }

            return result;
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> UpdateAssessmentAsync(
            Guid id,
            UpdateAdvanceTaxAssessmentDto request,
            Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(id);
            if (assessment == null)
                return Error.NotFound($"Assessment {id} not found");

            if (assessment.Status == "finalized")
                return Error.Validation("Cannot update a finalized assessment");

            // Update projected additional values (YTD remains locked)
            assessment.ProjectedAdditionalRevenue = request.ProjectedAdditionalRevenue;
            assessment.ProjectedAdditionalExpenses = request.ProjectedAdditionalExpenses;

            // Recompute full year projections
            var projectedRevenue = assessment.YtdRevenue + request.ProjectedAdditionalRevenue;
            var projectedExpenses = assessment.YtdExpenses + request.ProjectedAdditionalExpenses;

            var profitBeforeTax = projectedRevenue + request.ProjectedOtherIncome
                                - projectedExpenses - request.ProjectedDepreciation;

            // Book to Taxable Reconciliation
            var bookProfit = profitBeforeTax;

            // Additions (expenses disallowed)
            assessment.AddBookDepreciation = request.AddBookDepreciation;
            assessment.AddDisallowed40A3 = request.AddDisallowed40A3;
            assessment.AddDisallowed40A7 = request.AddDisallowed40A7;
            assessment.AddDisallowed43B = request.AddDisallowed43B;
            assessment.AddOtherDisallowances = request.AddOtherDisallowances;
            var totalAdditions = request.AddBookDepreciation + request.AddDisallowed40A3
                               + request.AddDisallowed40A7 + request.AddDisallowed43B
                               + request.AddOtherDisallowances;
            assessment.TotalAdditions = totalAdditions;

            // Deductions
            assessment.LessItDepreciation = request.LessItDepreciation;
            assessment.LessDeductions80C = request.LessDeductions80C;
            assessment.LessDeductions80D = request.LessDeductions80D;
            assessment.LessOtherDeductions = request.LessOtherDeductions;
            var totalDeductions = request.LessItDepreciation + request.LessDeductions80C
                                + request.LessDeductions80D + request.LessOtherDeductions;
            assessment.TotalDeductions = totalDeductions;

            // Taxable Income = Book Profit + Additions - Deductions
            var taxableIncome = Math.Max(0, bookProfit + totalAdditions - totalDeductions);

            var (taxRate, surchargeRate) = TaxRegimes.GetValueOrDefault(request.TaxRegime, TaxRegimes["normal"]);

            var baseTax = taxableIncome * taxRate / 100m;
            var surcharge = baseTax * surchargeRate / 100m;
            var cess = (baseTax + surcharge) * CessRate / 100m;
            var totalTaxLiability = baseTax + surcharge + cess;

            var netTaxPayable = Math.Max(0, totalTaxLiability - request.TdsReceivable - request.TcsCredit - request.MatCredit);

            // Update assessment
            assessment.ProjectedRevenue = projectedRevenue;
            assessment.ProjectedExpenses = projectedExpenses;
            assessment.ProjectedDepreciation = request.ProjectedDepreciation;
            assessment.ProjectedOtherIncome = request.ProjectedOtherIncome;
            assessment.ProjectedProfitBeforeTax = profitBeforeTax;
            assessment.BookProfit = bookProfit;

            assessment.TaxableIncome = taxableIncome;
            assessment.TaxRegime = request.TaxRegime;
            assessment.TaxRate = taxRate;
            assessment.SurchargeRate = surchargeRate;

            assessment.BaseTax = baseTax;
            assessment.Surcharge = surcharge;
            assessment.Cess = cess;
            assessment.TotalTaxLiability = totalTaxLiability;

            assessment.TdsReceivable = request.TdsReceivable;
            assessment.TcsCredit = request.TcsCredit;
            assessment.MatCredit = request.MatCredit;
            assessment.NetTaxPayable = netTaxPayable;

            assessment.Notes = request.Notes;

            await _repository.UpdateAssessmentAsync(assessment);

            // Recalculate schedules
            await RecalculateSchedulesInternalAsync(assessment);

            return await GetAssessmentByIdAsync(id);
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> ActivateAssessmentAsync(Guid id, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(id);
            if (assessment == null)
                return Error.NotFound($"Assessment {id} not found");

            if (assessment.Status != "draft")
                return Error.Validation("Only draft assessments can be activated");

            assessment.Status = "active";
            await _repository.UpdateAssessmentAsync(assessment);

            return await GetAssessmentByIdAsync(id);
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> FinalizeAssessmentAsync(Guid id, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(id);
            if (assessment == null)
                return Error.NotFound($"Assessment {id} not found");

            // Calculate final interest
            var interest234B = await Calculate234BInterestInternalAsync(assessment);
            var interest234C = await Calculate234CInterestInternalAsync(assessment);

            assessment.Interest234B = interest234B;
            assessment.Interest234C = interest234C;
            assessment.TotalInterest = interest234B + interest234C;
            assessment.Status = "finalized";

            await _repository.UpdateAssessmentAsync(assessment);

            return await GetAssessmentByIdAsync(id);
        }

        public async Task<Result<bool>> DeleteAssessmentAsync(Guid id)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(id);
            if (assessment == null)
                return Error.NotFound($"Assessment {id} not found");

            if (assessment.Status != "draft")
                return Error.Validation("Only draft assessments can be deleted");

            await _repository.DeleteAssessmentAsync(id);
            return true;
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> RefreshYtdAsync(RefreshYtdRequest request, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(request.AssessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {request.AssessmentId} not found");

            if (assessment.Status == "finalized")
                return Error.Validation("Cannot refresh YTD for a finalized assessment");

            // Fetch fresh YTD from ledger
            var fyDates = GetFYDateRange(assessment.FinancialYear);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var ytdThroughDate = today > fyDates.EndDate ? fyDates.EndDate : today;

            var ytd = await _repository.GetYtdFinancialsFromLedgerAsync(
                assessment.CompanyId,
                fyDates.StartDate,
                ytdThroughDate);

            // Update YTD values
            assessment.YtdRevenue = ytd.YtdIncome;
            assessment.YtdExpenses = ytd.YtdExpenses;
            assessment.YtdThroughDate = ytdThroughDate;

            // Optionally auto-project from trend
            if (request.AutoProjectFromTrend)
            {
                var monthsCovered = GetMonthsBetween(fyDates.StartDate, ytdThroughDate);
                var remainingMonths = 12 - monthsCovered;

                if (monthsCovered > 0 && remainingMonths > 0)
                {
                    var avgMonthlyRevenue = ytd.YtdIncome / monthsCovered;
                    var avgMonthlyExpenses = ytd.YtdExpenses / monthsCovered;

                    assessment.ProjectedAdditionalRevenue = avgMonthlyRevenue * remainingMonths;
                    assessment.ProjectedAdditionalExpenses = avgMonthlyExpenses * remainingMonths;
                }
            }

            // Recompute full year projections
            assessment.ProjectedRevenue = assessment.YtdRevenue + assessment.ProjectedAdditionalRevenue;
            assessment.ProjectedExpenses = assessment.YtdExpenses + assessment.ProjectedAdditionalExpenses;

            var profitBeforeTax = assessment.ProjectedRevenue + assessment.ProjectedOtherIncome
                                - assessment.ProjectedExpenses - assessment.ProjectedDepreciation;
            assessment.ProjectedProfitBeforeTax = profitBeforeTax;
            assessment.TaxableIncome = Math.Max(0, profitBeforeTax);

            // Recompute tax
            var (taxRate, surchargeRate) = TaxRegimes.GetValueOrDefault(assessment.TaxRegime, TaxRegimes["normal"]);
            assessment.TaxRate = taxRate;
            assessment.SurchargeRate = surchargeRate;

            var baseTax = assessment.TaxableIncome * taxRate / 100m;
            var surcharge = baseTax * surchargeRate / 100m;
            var cess = (baseTax + surcharge) * CessRate / 100m;

            assessment.BaseTax = baseTax;
            assessment.Surcharge = surcharge;
            assessment.Cess = cess;
            assessment.TotalTaxLiability = baseTax + surcharge + cess;
            assessment.NetTaxPayable = Math.Max(0, assessment.TotalTaxLiability
                - assessment.TdsReceivable - assessment.TcsCredit - assessment.MatCredit);

            await _repository.UpdateAssessmentAsync(assessment);

            // Recalculate schedules
            await RecalculateSchedulesInternalAsync(assessment);

            return await GetAssessmentByIdAsync(request.AssessmentId);
        }

        public async Task<Result<YtdFinancialsDto>> GetYtdFinancialsPreviewAsync(Guid companyId, string financialYear)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company {companyId} not found");

            var fyDates = GetFYDateRange(financialYear);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var ytdThroughDate = today > fyDates.EndDate ? fyDates.EndDate : today;

            var ytd = await _repository.GetYtdFinancialsFromLedgerAsync(
                companyId,
                fyDates.StartDate,
                ytdThroughDate);

            var monthsCovered = GetMonthsBetween(fyDates.StartDate, ytdThroughDate);
            var remainingMonths = 12 - monthsCovered;

            var avgMonthlyRevenue = monthsCovered > 0 ? ytd.YtdIncome / monthsCovered : 0;
            var avgMonthlyExpenses = monthsCovered > 0 ? ytd.YtdExpenses / monthsCovered : 0;

            return new YtdFinancialsDto
            {
                YtdRevenue = ytd.YtdIncome,
                YtdExpenses = ytd.YtdExpenses,
                ThroughDate = ytdThroughDate,
                MonthsCovered = monthsCovered,
                AvgMonthlyRevenue = avgMonthlyRevenue,
                AvgMonthlyExpenses = avgMonthlyExpenses,
                RemainingMonths = remainingMonths,
                SuggestedAdditionalRevenue = avgMonthlyRevenue * remainingMonths,
                SuggestedAdditionalExpenses = avgMonthlyExpenses * remainingMonths
            };
        }

        public async Task<Result<AdvanceTaxAssessmentDto>> RefreshTdsTcsAsync(Guid assessmentId, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            if (assessment.Status == "finalized")
                return Error.Validation("Cannot refresh TDS/TCS for a finalized assessment");

            // Fetch fresh values from TDS/TCS modules
            var tdsReceivable = await _repository.GetTdsReceivableAsync(assessment.CompanyId, assessment.FinancialYear);
            var tcsCredit = await _repository.GetTcsCreditAsync(assessment.CompanyId, assessment.FinancialYear);

            // Update assessment
            assessment.TdsReceivable = tdsReceivable;
            assessment.TcsCredit = tcsCredit;

            // Recalculate net tax payable
            assessment.NetTaxPayable = Math.Max(0,
                assessment.TotalTaxLiability - tdsReceivable - tcsCredit
                - assessment.MatCredit - assessment.AdvanceTaxAlreadyPaid);

            await _repository.UpdateAssessmentAsync(assessment);

            // Recalculate schedules with new net payable
            await RecalculateSchedulesInternalAsync(assessment);

            return await GetAssessmentByIdAsync(assessmentId);
        }

        public async Task<Result<TdsTcsPreviewDto>> GetTdsTcsPreviewAsync(Guid companyId, string financialYear)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company {companyId} not found");

            // Fetch values from modules
            var tdsReceivable = await _repository.GetTdsReceivableAsync(companyId, financialYear);
            var tcsCredit = await _repository.GetTcsCreditAsync(companyId, financialYear);

            // Get current assessment values if exists
            var assessment = await _repository.GetAssessmentByCompanyAndFYAsync(companyId, financialYear);
            var currentTds = assessment?.TdsReceivable ?? 0m;
            var currentTcs = assessment?.TcsCredit ?? 0m;

            return new TdsTcsPreviewDto
            {
                TdsReceivable = tdsReceivable,
                TcsCredit = tcsCredit,
                CurrentTdsInAssessment = currentTds,
                CurrentTcsInAssessment = currentTcs,
                TdsDifference = tdsReceivable - currentTds,
                TcsDifference = tcsCredit - currentTcs
            };
        }

        // ==================== Schedule Operations ====================

        public async Task<Result<IEnumerable<AdvanceTaxScheduleDto>>> GetPaymentScheduleAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            var schedules = await _repository.GetSchedulesByAssessmentAsync(assessmentId);
            return schedules.Select(s => MapScheduleToDto(s)).ToList();
        }

        public async Task<Result<IEnumerable<AdvanceTaxScheduleDto>>> RecalculateSchedulesAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            await RecalculateSchedulesInternalAsync(assessment);
            return await GetPaymentScheduleAsync(assessmentId);
        }

        // ==================== Payment Operations ====================

        public async Task<Result<AdvanceTaxPaymentDto>> RecordPaymentAsync(
            RecordAdvanceTaxPaymentDto request,
            Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(request.AssessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {request.AssessmentId} not found");

            var payment = new AdvanceTaxPayment
            {
                AssessmentId = request.AssessmentId,
                ScheduleId = request.ScheduleId,
                PaymentDate = request.PaymentDate,
                Amount = request.Amount,
                ChallanNumber = request.ChallanNumber,
                BsrCode = request.BsrCode,
                Cin = request.Cin,
                BankAccountId = request.BankAccountId,
                Notes = request.Notes,
                Status = "completed",
                CreatedBy = userId
            };

            // TODO: Create journal entry if requested
            // This would debit Advance Tax Asset and credit Bank

            var created = await _repository.CreatePaymentAsync(payment);

            // Update assessment's advance tax paid
            assessment.AdvanceTaxAlreadyPaid += request.Amount;
            assessment.NetTaxPayable = Math.Max(0,
                assessment.TotalTaxLiability - assessment.TdsReceivable - assessment.TcsCredit
                - assessment.MatCredit - assessment.AdvanceTaxAlreadyPaid);
            await _repository.UpdateAssessmentAsync(assessment);

            // Recalculate schedules
            await RecalculateSchedulesInternalAsync(assessment);

            return MapPaymentToDto(created);
        }

        public async Task<Result<IEnumerable<AdvanceTaxPaymentDto>>> GetPaymentsAsync(Guid assessmentId)
        {
            var payments = await _repository.GetPaymentsByAssessmentAsync(assessmentId);
            return payments.Select(MapPaymentToDto).ToList();
        }

        public async Task<Result<bool>> DeletePaymentAsync(Guid paymentId)
        {
            var payment = await _repository.GetPaymentByIdAsync(paymentId);
            if (payment == null)
                return Error.NotFound($"Payment {paymentId} not found");

            // Update assessment
            var assessment = await _repository.GetAssessmentByIdAsync(payment.AssessmentId);
            if (assessment != null)
            {
                assessment.AdvanceTaxAlreadyPaid -= payment.Amount;
                assessment.NetTaxPayable = Math.Max(0,
                    assessment.TotalTaxLiability - assessment.TdsReceivable - assessment.TcsCredit
                    - assessment.MatCredit - assessment.AdvanceTaxAlreadyPaid);
                await _repository.UpdateAssessmentAsync(assessment);
                await RecalculateSchedulesInternalAsync(assessment);
            }

            await _repository.DeletePaymentAsync(paymentId);
            return true;
        }

        // ==================== Interest Calculations ====================

        public async Task<Result<decimal>> Calculate234BInterestAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            return await Calculate234BInterestInternalAsync(assessment);
        }

        public async Task<Result<decimal>> Calculate234CInterestAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            return await Calculate234CInterestInternalAsync(assessment);
        }

        public async Task<Result<InterestCalculationDto>> GetInterestBreakdownAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            var schedules = await _repository.GetSchedulesByAssessmentAsync(assessmentId);
            var payments = await _repository.GetPaymentsByAssessmentAsync(assessmentId);

            var totalPaid = payments.Sum(p => p.Amount);

            // 234B calculation
            var assessedTax = assessment.TotalTaxLiability - assessment.TdsReceivable - assessment.TcsCredit - assessment.MatCredit;
            var minRequired = assessedTax * 0.9m; // 90% of assessed tax
            var shortfall234B = Math.Max(0, minRequired - totalPaid);
            var months234B = 12; // Simplified - actual would depend on filing date
            var interest234B = shortfall234B * 0.01m * months234B;

            // 234C calculation per quarter
            var quarterlyBreakdown = new List<Interest234CQuarterDto>();
            decimal totalInterest234C = 0;

            foreach (var schedule in schedules.OrderBy(s => s.Quarter))
            {
                if (schedule.ShortfallAmount > 0)
                {
                    var months = GetMonthsForQuarter(schedule.Quarter);
                    var quarterInterest = schedule.ShortfallAmount * 0.01m * months;
                    totalInterest234C += quarterInterest;

                    quarterlyBreakdown.Add(new Interest234CQuarterDto
                    {
                        Quarter = schedule.Quarter,
                        RequiredCumulative = schedule.CumulativeTaxDue,
                        ActualCumulative = schedule.CumulativeTaxPaid,
                        Shortfall = schedule.ShortfallAmount,
                        Months = months,
                        Interest = quarterInterest
                    });
                }
            }

            return new InterestCalculationDto
            {
                AssessedTax = assessedTax,
                AdvanceTaxPaid = totalPaid,
                ShortfallFor234B = shortfall234B,
                Months234B = months234B,
                Interest234B = interest234B,
                QuarterlyBreakdown = quarterlyBreakdown,
                TotalInterest234C = totalInterest234C,
                TotalInterest = interest234B + totalInterest234C
            };
        }

        // ==================== Scenario Analysis ====================

        public async Task<Result<AdvanceTaxScenarioDto>> RunScenarioAsync(RunScenarioDto request, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(request.AssessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {request.AssessmentId} not found");

            // Calculate adjusted figures
            var adjustedRevenue = assessment.ProjectedRevenue + request.RevenueAdjustment;
            var adjustedExpenses = assessment.ProjectedExpenses + request.ExpenseAdjustment + request.PayrollChange;
            var adjustedDepreciation = assessment.ProjectedDepreciation + request.CapexImpact;
            var adjustedPBT = adjustedRevenue + assessment.ProjectedOtherIncome - adjustedExpenses - adjustedDepreciation;
            var adjustedTaxableIncome = Math.Max(0, adjustedPBT + request.OtherAdjustments);

            // Compute adjusted tax
            var (taxRate, surchargeRate) = TaxRegimes.GetValueOrDefault(assessment.TaxRegime, TaxRegimes["normal"]);
            var baseTax = adjustedTaxableIncome * taxRate / 100m;
            var surcharge = baseTax * surchargeRate / 100m;
            var cess = (baseTax + surcharge) * CessRate / 100m;
            var adjustedTaxLiability = baseTax + surcharge + cess;

            var varianceFromBase = adjustedTaxLiability - assessment.TotalTaxLiability;

            var scenario = new AdvanceTaxScenario
            {
                AssessmentId = request.AssessmentId,
                ScenarioName = request.ScenarioName,
                RevenueAdjustment = request.RevenueAdjustment,
                ExpenseAdjustment = request.ExpenseAdjustment,
                CapexImpact = request.CapexImpact,
                PayrollChange = request.PayrollChange,
                OtherAdjustments = request.OtherAdjustments,
                AdjustedTaxableIncome = adjustedTaxableIncome,
                AdjustedTaxLiability = adjustedTaxLiability,
                VarianceFromBase = varianceFromBase,
                Assumptions = request.Assumptions,
                Notes = request.Notes,
                CreatedBy = userId
            };

            var created = await _repository.CreateScenarioAsync(scenario);
            return MapScenarioToDto(created);
        }

        public async Task<Result<IEnumerable<AdvanceTaxScenarioDto>>> GetScenariosAsync(Guid assessmentId)
        {
            var scenarios = await _repository.GetScenariosByAssessmentAsync(assessmentId);
            return scenarios.Select(MapScenarioToDto).ToList();
        }

        public async Task<Result<bool>> DeleteScenarioAsync(Guid scenarioId)
        {
            var scenario = await _repository.GetScenarioByIdAsync(scenarioId);
            if (scenario == null)
                return Error.NotFound($"Scenario {scenarioId} not found");

            await _repository.DeleteScenarioAsync(scenarioId);
            return true;
        }

        // ==================== Dashboard & Reports ====================

        public async Task<Result<AdvanceTaxTrackerDto>> GetTrackerAsync(Guid companyId, string financialYear)
        {
            var assessment = await _repository.GetAssessmentByCompanyAndFYAsync(companyId, financialYear);

            var tracker = new AdvanceTaxTrackerDto
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                AssessmentYear = GetAssessmentYear(financialYear)
            };

            if (assessment == null)
            {
                tracker.AssessmentStatus = "not_created";
                return tracker;
            }

            var schedules = await _repository.GetSchedulesByAssessmentAsync(assessment.Id);
            var payments = await _repository.GetPaymentsByAssessmentAsync(assessment.Id);

            var totalPaid = payments.Sum(p => p.Amount);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Find current/next quarter
            var nextDue = schedules
                .Where(s => s.PaymentStatus != "paid" && s.DueDate >= today)
                .OrderBy(s => s.DueDate)
                .FirstOrDefault();

            var currentQuarter = GetCurrentQuarter();

            tracker.AssessmentId = assessment.Id;
            tracker.AssessmentStatus = assessment.Status;
            tracker.TotalTaxLiability = assessment.TotalTaxLiability;
            tracker.NetTaxPayable = assessment.NetTaxPayable;
            tracker.TotalAdvanceTaxPaid = totalPaid;
            tracker.RemainingTaxPayable = Math.Max(0, assessment.NetTaxPayable - totalPaid);
            tracker.PaymentPercentage = assessment.NetTaxPayable > 0
                ? (totalPaid / assessment.NetTaxPayable) * 100m
                : 100m;
            tracker.CurrentQuarter = currentQuarter;
            tracker.NextDueDate = nextDue?.DueDate;
            tracker.NextQuarterAmount = nextDue?.TaxPayableThisQuarter ?? 0;
            tracker.DaysUntilNextDue = nextDue != null
                ? (nextDue.DueDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days
                : 0;
            tracker.Interest234B = assessment.Interest234B;
            tracker.Interest234C = assessment.Interest234C;
            tracker.TotalInterest = assessment.TotalInterest;
            tracker.Schedules = schedules.Select(s => MapScheduleToDto(s)).ToList();

            return tracker;
        }

        public async Task<Result<TaxComputationDto>> GetTaxComputationAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            return new TaxComputationDto
            {
                TaxableIncome = assessment.TaxableIncome,
                TaxRegime = assessment.TaxRegime,
                TaxRate = assessment.TaxRate,
                BaseTax = assessment.BaseTax,
                SurchargeRate = assessment.SurchargeRate,
                Surcharge = assessment.Surcharge,
                CessRate = assessment.CessRate,
                Cess = assessment.Cess,
                GrossTax = assessment.TotalTaxLiability,
                TdsCredit = assessment.TdsReceivable,
                TcsCredit = assessment.TcsCredit,
                MatCredit = assessment.MatCredit,
                TotalCredits = assessment.TdsReceivable + assessment.TcsCredit + assessment.MatCredit,
                NetTaxPayable = assessment.NetTaxPayable
            };
        }

        public async Task<Result<IEnumerable<AdvanceTaxAssessmentDto>>> GetPendingPaymentsAsync(Guid? companyId = null)
        {
            var assessments = await _repository.GetAssessmentsWithPendingPaymentsAsync(companyId);
            var result = new List<AdvanceTaxAssessmentDto>();

            foreach (var assessment in assessments)
            {
                var schedules = await _repository.GetSchedulesByAssessmentAsync(assessment.Id);
                var payments = await _repository.GetPaymentsByAssessmentAsync(assessment.Id);
                var company = await _companiesRepository.GetByIdAsync(assessment.CompanyId);
                result.Add(MapToDto(assessment, schedules, payments, company?.Name));
            }

            return result;
        }

        // ==================== Helper Methods ====================

        private List<AdvanceTaxSchedule> CreateQuarterlySchedules(Guid assessmentId, string financialYear, decimal netTaxPayable)
        {
            var schedules = new List<AdvanceTaxSchedule>();
            var fyStartYear = int.Parse(financialYear.Split('-')[0]);

            // Due dates: 15-Jun, 15-Sep, 15-Dec, 15-Mar
            var dueDates = new[]
            {
                new DateOnly(fyStartYear, 6, 15),      // Q1
                new DateOnly(fyStartYear, 9, 15),      // Q2
                new DateOnly(fyStartYear, 12, 15),    // Q3
                new DateOnly(fyStartYear + 1, 3, 15)   // Q4
            };

            decimal previousCumulative = 0;
            for (int q = 0; q < 4; q++)
            {
                var cumulativePercent = QuarterlyCumulativePercentages[q];
                var cumulativeDue = netTaxPayable * cumulativePercent / 100m;
                var thisQuarterDue = cumulativeDue - previousCumulative;

                schedules.Add(new AdvanceTaxSchedule
                {
                    AssessmentId = assessmentId,
                    Quarter = q + 1,
                    DueDate = dueDates[q],
                    CumulativePercentage = cumulativePercent,
                    CumulativeTaxDue = cumulativeDue,
                    TaxPayableThisQuarter = thisQuarterDue,
                    PaymentStatus = "pending"
                });

                previousCumulative = cumulativeDue;
            }

            return schedules;
        }

        private async Task RecalculateSchedulesInternalAsync(AdvanceTaxAssessment assessment)
        {
            var schedules = (await _repository.GetSchedulesByAssessmentAsync(assessment.Id)).ToList();
            var payments = (await _repository.GetPaymentsByAssessmentAsync(assessment.Id)).ToList();

            var today = DateOnly.FromDateTime(DateTime.Today);
            decimal cumulativePaid = 0;
            decimal previousCumulativeDue = 0;

            foreach (var schedule in schedules.OrderBy(s => s.Quarter))
            {
                // Recalculate due amounts
                var cumulativePercent = QuarterlyCumulativePercentages[schedule.Quarter - 1];
                var cumulativeDue = assessment.NetTaxPayable * cumulativePercent / 100m;
                schedule.CumulativePercentage = cumulativePercent;
                schedule.CumulativeTaxDue = cumulativeDue;
                schedule.TaxPayableThisQuarter = cumulativeDue - previousCumulativeDue;

                // Calculate payments for this quarter
                var quarterPayments = payments
                    .Where(p => p.ScheduleId == schedule.Id ||
                               (p.ScheduleId == null && p.PaymentDate <= schedule.DueDate &&
                                p.PaymentDate > (schedule.Quarter > 1 ? schedules[schedule.Quarter - 2].DueDate : DateOnly.MinValue)))
                    .Sum(p => p.Amount);

                cumulativePaid += quarterPayments;
                schedule.TaxPaidThisQuarter = quarterPayments;
                schedule.CumulativeTaxPaid = cumulativePaid;
                schedule.ShortfallAmount = Math.Max(0, cumulativeDue - cumulativePaid);

                // Calculate 234C interest if shortfall and past due
                if (schedule.ShortfallAmount > 0 && schedule.DueDate < today)
                {
                    var months = GetMonthsForQuarter(schedule.Quarter);
                    schedule.Interest234C = schedule.ShortfallAmount * 0.01m * months;
                }

                // Update status
                if (schedule.CumulativeTaxPaid >= schedule.CumulativeTaxDue)
                    schedule.PaymentStatus = "paid";
                else if (schedule.DueDate < today)
                    schedule.PaymentStatus = "overdue";
                else if (schedule.TaxPaidThisQuarter > 0)
                    schedule.PaymentStatus = "partial";
                else
                    schedule.PaymentStatus = "pending";

                await _repository.UpdateScheduleAsync(schedule);
                previousCumulativeDue = cumulativeDue;
            }
        }

        private async Task<decimal> Calculate234BInterestInternalAsync(AdvanceTaxAssessment assessment)
        {
            var payments = await _repository.GetPaymentsByAssessmentAsync(assessment.Id);
            var totalPaid = payments.Sum(p => p.Amount);

            // 234B applies if advance tax paid < 90% of assessed tax
            var assessedTax = assessment.TotalTaxLiability - assessment.TdsReceivable - assessment.TcsCredit - assessment.MatCredit;
            var minRequired = assessedTax * 0.9m;

            if (totalPaid >= minRequired)
                return 0;

            var shortfall = minRequired - totalPaid;
            // 1% per month from April to March (or filing date)
            var months = 12;
            return shortfall * 0.01m * months;
        }

        private async Task<decimal> Calculate234CInterestInternalAsync(AdvanceTaxAssessment assessment)
        {
            var schedules = await _repository.GetSchedulesByAssessmentAsync(assessment.Id);
            return schedules.Sum(s => s.Interest234C);
        }

        private static int GetMonthsForQuarter(int quarter)
        {
            // Interest period from due date to next due date
            return quarter switch
            {
                1 => 3, // Jun to Sep
                2 => 3, // Sep to Dec
                3 => 3, // Dec to Mar
                4 => 0, // No 234C for Q4
                _ => 0
            };
        }

        private static int GetCurrentQuarter()
        {
            var month = DateTime.Today.Month;
            return month switch
            {
                >= 4 and <= 6 => 1,
                >= 7 and <= 9 => 2,
                >= 10 and <= 12 => 3,
                _ => 4
            };
        }

        private static int GetMonthsBetween(DateOnly startDate, DateOnly endDate)
        {
            // Calculate months covered (inclusive of partial months)
            var months = (endDate.Year - startDate.Year) * 12 + (endDate.Month - startDate.Month) + 1;
            return Math.Max(0, Math.Min(12, months));
        }

        private static string GetAssessmentYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var startYear))
            {
                return $"{startYear + 1}-{(startYear + 2) % 100:D2}";
            }
            return financialYear;
        }

        private static (DateOnly StartDate, DateOnly EndDate) GetFYDateRange(string financialYear)
        {
            // Parse "2024-25" or "2024-2025" format
            var parts = financialYear.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var startYear))
            {
                var startDate = new DateOnly(startYear, 4, 1);  // Apr 1
                var endDate = new DateOnly(startYear + 1, 3, 31); // Mar 31
                return (startDate, endDate);
            }
            // Fallback to current FY if parse fails
            var today = DateTime.Today;
            var currentFYStart = today.Month >= 4
                ? new DateOnly(today.Year, 4, 1)
                : new DateOnly(today.Year - 1, 4, 1);
            var currentFYEnd = currentFYStart.AddYears(1).AddDays(-1);
            return (currentFYStart, currentFYEnd);
        }

        // ==================== Mapping Methods ====================

        private static AdvanceTaxAssessmentDto MapToDto(
            AdvanceTaxAssessment entity,
            IEnumerable<AdvanceTaxSchedule> schedules,
            IEnumerable<AdvanceTaxPayment> payments,
            string? companyName)
        {
            return new AdvanceTaxAssessmentDto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                CompanyName = companyName,
                FinancialYear = entity.FinancialYear,
                AssessmentYear = entity.AssessmentYear,
                Status = entity.Status,

                // YTD actuals
                YtdRevenue = entity.YtdRevenue,
                YtdExpenses = entity.YtdExpenses,
                YtdThroughDate = entity.YtdThroughDate,

                // Projected additional
                ProjectedAdditionalRevenue = entity.ProjectedAdditionalRevenue,
                ProjectedAdditionalExpenses = entity.ProjectedAdditionalExpenses,

                // Full year projections
                ProjectedRevenue = entity.ProjectedRevenue,
                ProjectedExpenses = entity.ProjectedExpenses,
                ProjectedDepreciation = entity.ProjectedDepreciation,
                ProjectedOtherIncome = entity.ProjectedOtherIncome,
                ProjectedProfitBeforeTax = entity.ProjectedProfitBeforeTax,

                // Book to Taxable Reconciliation
                BookProfit = entity.BookProfit,
                AddBookDepreciation = entity.AddBookDepreciation,
                AddDisallowed40A3 = entity.AddDisallowed40A3,
                AddDisallowed40A7 = entity.AddDisallowed40A7,
                AddDisallowed43B = entity.AddDisallowed43B,
                AddOtherDisallowances = entity.AddOtherDisallowances,
                TotalAdditions = entity.TotalAdditions,
                LessItDepreciation = entity.LessItDepreciation,
                LessDeductions80C = entity.LessDeductions80C,
                LessDeductions80D = entity.LessDeductions80D,
                LessOtherDeductions = entity.LessOtherDeductions,
                TotalDeductions = entity.TotalDeductions,

                TaxableIncome = entity.TaxableIncome,
                TaxRegime = entity.TaxRegime,
                TaxRate = entity.TaxRate,
                SurchargeRate = entity.SurchargeRate,
                CessRate = entity.CessRate,

                BaseTax = entity.BaseTax,
                Surcharge = entity.Surcharge,
                Cess = entity.Cess,
                TotalTaxLiability = entity.TotalTaxLiability,

                TdsReceivable = entity.TdsReceivable,
                TcsCredit = entity.TcsCredit,
                AdvanceTaxAlreadyPaid = entity.AdvanceTaxAlreadyPaid,
                MatCredit = entity.MatCredit,
                NetTaxPayable = entity.NetTaxPayable,

                Interest234B = entity.Interest234B,
                Interest234C = entity.Interest234C,
                TotalInterest = entity.TotalInterest,

                ComputationDetails = entity.ComputationDetails,
                Assumptions = entity.Assumptions,
                Notes = entity.Notes,

                Schedules = schedules.Select(MapScheduleToDto).ToList(),
                Payments = payments.Select(MapPaymentToDto).ToList(),

                RevisionCount = entity.RevisionCount,
                LastRevisionDate = entity.LastRevisionDate,
                LastRevisionQuarter = entity.LastRevisionQuarter,

                // MAT fields
                IsMatApplicable = entity.IsMatApplicable,
                MatBookProfit = entity.MatBookProfit,
                MatRate = entity.MatRate,
                MatOnBookProfit = entity.MatOnBookProfit,
                MatSurcharge = entity.MatSurcharge,
                MatCess = entity.MatCess,
                TotalMat = entity.TotalMat,
                MatCreditAvailable = entity.MatCreditAvailable,
                MatCreditToUtilize = entity.MatCreditToUtilize,
                MatCreditCreatedThisYear = entity.MatCreditCreatedThisYear,
                TaxPayableAfterMat = entity.TaxPayableAfterMat,

                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static AdvanceTaxScheduleDto MapScheduleToDto(AdvanceTaxSchedule entity)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return new AdvanceTaxScheduleDto
            {
                Id = entity.Id,
                AssessmentId = entity.AssessmentId,
                Quarter = entity.Quarter,
                QuarterLabel = $"Q{entity.Quarter}",
                DueDate = entity.DueDate,
                CumulativePercentage = entity.CumulativePercentage,
                CumulativeTaxDue = entity.CumulativeTaxDue,
                TaxPayableThisQuarter = entity.TaxPayableThisQuarter,
                TaxPaidThisQuarter = entity.TaxPaidThisQuarter,
                CumulativeTaxPaid = entity.CumulativeTaxPaid,
                ShortfallAmount = entity.ShortfallAmount,
                Interest234C = entity.Interest234C,
                PaymentStatus = entity.PaymentStatus,
                IsOverdue = entity.DueDate < today && entity.PaymentStatus != "paid",
                DaysUntilDue = (entity.DueDate.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days
            };
        }

        private static AdvanceTaxPaymentDto MapPaymentToDto(AdvanceTaxPayment entity)
        {
            return new AdvanceTaxPaymentDto
            {
                Id = entity.Id,
                AssessmentId = entity.AssessmentId,
                ScheduleId = entity.ScheduleId,
                PaymentDate = entity.PaymentDate,
                Amount = entity.Amount,
                ChallanNumber = entity.ChallanNumber,
                BsrCode = entity.BsrCode,
                Cin = entity.Cin,
                BankAccountId = entity.BankAccountId,
                JournalEntryId = entity.JournalEntryId,
                Status = entity.Status,
                Notes = entity.Notes,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        private static AdvanceTaxScenarioDto MapScenarioToDto(AdvanceTaxScenario entity)
        {
            return new AdvanceTaxScenarioDto
            {
                Id = entity.Id,
                AssessmentId = entity.AssessmentId,
                ScenarioName = entity.ScenarioName,
                RevenueAdjustment = entity.RevenueAdjustment,
                ExpenseAdjustment = entity.ExpenseAdjustment,
                CapexImpact = entity.CapexImpact,
                PayrollChange = entity.PayrollChange,
                OtherAdjustments = entity.OtherAdjustments,
                AdjustedTaxableIncome = entity.AdjustedTaxableIncome,
                AdjustedTaxLiability = entity.AdjustedTaxLiability,
                VarianceFromBase = entity.VarianceFromBase,
                Assumptions = entity.Assumptions,
                Notes = entity.Notes,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        private static AdvanceTaxRevisionDto MapRevisionToDto(AdvanceTaxRevision entity)
        {
            return new AdvanceTaxRevisionDto
            {
                Id = entity.Id,
                AssessmentId = entity.AssessmentId,
                RevisionNumber = entity.RevisionNumber,
                RevisionQuarter = entity.RevisionQuarter,
                RevisionDate = entity.RevisionDate,
                PreviousProjectedRevenue = entity.PreviousProjectedRevenue,
                PreviousProjectedExpenses = entity.PreviousProjectedExpenses,
                PreviousTaxableIncome = entity.PreviousTaxableIncome,
                PreviousTotalTaxLiability = entity.PreviousTotalTaxLiability,
                PreviousNetTaxPayable = entity.PreviousNetTaxPayable,
                RevisedProjectedRevenue = entity.RevisedProjectedRevenue,
                RevisedProjectedExpenses = entity.RevisedProjectedExpenses,
                RevisedTaxableIncome = entity.RevisedTaxableIncome,
                RevisedTotalTaxLiability = entity.RevisedTotalTaxLiability,
                RevisedNetTaxPayable = entity.RevisedNetTaxPayable,
                RevenueVariance = entity.RevenueVariance,
                ExpenseVariance = entity.ExpenseVariance,
                TaxableIncomeVariance = entity.TaxableIncomeVariance,
                TaxLiabilityVariance = entity.TaxLiabilityVariance,
                NetPayableVariance = entity.NetPayableVariance,
                RevisionReason = entity.RevisionReason,
                Notes = entity.Notes,
                RevisedBy = entity.RevisedBy,
                CreatedAt = entity.CreatedAt
            };
        }

        // ==================== Revision Operations ====================

        public async Task<Result<AdvanceTaxRevisionDto>> CreateRevisionAsync(CreateRevisionDto request, Guid userId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(request.AssessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {request.AssessmentId} not found");

            if (assessment.Status == "finalized")
                return Error.Validation("Cannot revise a finalized assessment");

            // Capture previous state
            var revision = new AdvanceTaxRevision
            {
                AssessmentId = assessment.Id,
                RevisionQuarter = request.RevisionQuarter,
                RevisionDate = DateOnly.FromDateTime(DateTime.Today),
                PreviousProjectedRevenue = assessment.ProjectedRevenue,
                PreviousProjectedExpenses = assessment.ProjectedExpenses,
                PreviousTaxableIncome = assessment.TaxableIncome,
                PreviousTotalTaxLiability = assessment.TotalTaxLiability,
                PreviousNetTaxPayable = assessment.NetTaxPayable,
                RevisionReason = request.RevisionReason,
                Notes = request.Notes,
                RevisedBy = userId
            };

            // Apply the update using existing UpdateAssessmentAsync logic
            var updateRequest = new UpdateAdvanceTaxAssessmentDto
            {
                ProjectedAdditionalRevenue = request.ProjectedAdditionalRevenue,
                ProjectedAdditionalExpenses = request.ProjectedAdditionalExpenses,
                ProjectedDepreciation = request.ProjectedDepreciation,
                ProjectedOtherIncome = request.ProjectedOtherIncome,
                AddBookDepreciation = request.AddBookDepreciation,
                AddDisallowed40A3 = request.AddDisallowed40A3,
                AddDisallowed40A7 = request.AddDisallowed40A7,
                AddDisallowed43B = request.AddDisallowed43B,
                AddOtherDisallowances = request.AddOtherDisallowances,
                LessItDepreciation = request.LessItDepreciation,
                LessDeductions80C = request.LessDeductions80C,
                LessDeductions80D = request.LessDeductions80D,
                LessOtherDeductions = request.LessOtherDeductions,
                TaxRegime = request.TaxRegime,
                TdsReceivable = request.TdsReceivable,
                TcsCredit = request.TcsCredit,
                MatCredit = request.MatCredit,
                Notes = request.Notes
            };

            var updateResult = await UpdateAssessmentAsync(assessment.Id, updateRequest, userId);
            if (updateResult.IsFailure)
                return Error.Internal(updateResult.Error!.Message);

            // Get updated assessment to capture new state
            var updatedAssessment = await _repository.GetAssessmentByIdAsync(assessment.Id);

            // Set revised values in revision
            revision.RevisedProjectedRevenue = updatedAssessment!.ProjectedRevenue;
            revision.RevisedProjectedExpenses = updatedAssessment.ProjectedExpenses;
            revision.RevisedTaxableIncome = updatedAssessment.TaxableIncome;
            revision.RevisedTotalTaxLiability = updatedAssessment.TotalTaxLiability;
            revision.RevisedNetTaxPayable = updatedAssessment.NetTaxPayable;

            // Create revision record
            var createdRevision = await _repository.CreateRevisionAsync(revision);

            // Update assessment revision tracking
            updatedAssessment.RevisionCount = await _repository.GetRevisionCountAsync(assessment.Id);
            updatedAssessment.LastRevisionDate = DateOnly.FromDateTime(DateTime.Today);
            updatedAssessment.LastRevisionQuarter = request.RevisionQuarter;
            await _repository.UpdateAssessmentAsync(updatedAssessment);

            return MapRevisionToDto(createdRevision);
        }

        public async Task<Result<IEnumerable<AdvanceTaxRevisionDto>>> GetRevisionsAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            var revisions = await _repository.GetRevisionsByAssessmentAsync(assessmentId);
            return revisions.Select(MapRevisionToDto).ToList();
        }

        public async Task<Result<RevisionStatusDto>> GetRevisionStatusAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            var currentQuarter = GetCurrentQuarter();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var (fyStart, fyEnd) = GetFYDateRange(assessment.FinancialYear);

            // Get YTD actuals to compare with projections
            var ytdActual = assessment.YtdRevenue - assessment.YtdExpenses;
            var projectedToDate = assessment.ProjectedProfitBeforeTax *
                (GetMonthsBetween(fyStart, today) / 12m);

            var variance = ytdActual - projectedToDate;
            var variancePercentage = projectedToDate != 0 ? (variance / projectedToDate) * 100m : 0;

            // Determine if revision is recommended
            var revisionRecommended = false;
            string? revisionPrompt = null;

            // Recommend revision if:
            // 1. Variance exceeds 10%
            // 2. We're past a quarter due date and haven't revised in that quarter
            if (Math.Abs(variancePercentage) > 10)
            {
                revisionRecommended = true;
                revisionPrompt = $"Actual profit variance of {variancePercentage:F1}% from projections. Consider revising estimates.";
            }
            else if (assessment.LastRevisionQuarter == null || assessment.LastRevisionQuarter < currentQuarter)
            {
                if (currentQuarter > 1) // After Q1
                {
                    revisionRecommended = true;
                    revisionPrompt = $"Q{currentQuarter} due date approaching. Review and revise projections if needed.";
                }
            }

            return new RevisionStatusDto
            {
                CurrentQuarter = currentQuarter,
                RevisionRecommended = revisionRecommended,
                RevisionPrompt = revisionPrompt,
                LastRevisionDate = assessment.LastRevisionDate,
                TotalRevisions = assessment.RevisionCount,
                ActualVsProjectedVariance = variance,
                VariancePercentage = variancePercentage
            };
        }

        // ==================== MAT Credit Operations ====================

        private const decimal MatRate = 15.00m; // MAT rate is 15% of book profit

        public async Task<Result<MatComputationDto>> GetMatComputationAsync(Guid assessmentId)
        {
            var assessment = await _repository.GetAssessmentByIdAsync(assessmentId);
            if (assessment == null)
                return Error.NotFound($"Assessment {assessmentId} not found");

            // Get available MAT credits
            var availableCredit = await _repository.GetTotalAvailableMatCreditAsync(
                assessment.CompanyId, assessment.FinancialYear);

            return ComputeMatInternal(assessment, availableCredit);
        }

        public async Task<Result<MatCreditSummaryDto>> GetMatCreditSummaryAsync(Guid companyId, string financialYear)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company {companyId} not found");

            var availableCredits = await _repository.GetAvailableMatCreditsAsync(companyId, financialYear);
            var creditDtos = availableCredits.Select(MapMatCreditToDto).ToList();

            // Calculate credits expiring in next 2 years
            var twoYearsLater = GetFYYearPlusTwoYears(financialYear);
            var expiringSoon = creditDtos.Where(c => string.Compare(c.ExpiryYear, twoYearsLater) <= 0).ToList();

            return new MatCreditSummaryDto
            {
                CompanyId = companyId,
                CurrentFinancialYear = financialYear,
                TotalCreditAvailable = creditDtos.Sum(c => c.MatCreditBalance),
                YearsWithCredit = creditDtos.Count,
                AvailableCredits = creditDtos,
                ExpiringSoonAmount = expiringSoon.Sum(c => c.MatCreditBalance),
                ExpiringSoonCount = expiringSoon.Count
            };
        }

        public async Task<Result<IEnumerable<MatCreditRegisterDto>>> GetMatCreditsAsync(Guid companyId)
        {
            var company = await _companiesRepository.GetByIdAsync(companyId);
            if (company == null)
                return Error.NotFound($"Company {companyId} not found");

            var credits = await _repository.GetMatCreditsByCompanyAsync(companyId);
            return credits.Select(MapMatCreditToDto).ToList();
        }

        public async Task<Result<IEnumerable<MatCreditUtilizationDto>>> GetMatCreditUtilizationsAsync(Guid matCreditId)
        {
            var matCredit = await _repository.GetMatCreditByIdAsync(matCreditId);
            if (matCredit == null)
                return Error.NotFound($"MAT credit {matCreditId} not found");

            var utilizations = await _repository.GetMatCreditUtilizationsAsync(matCreditId);
            return utilizations.Select(MapMatCreditUtilizationToDto).ToList();
        }

        /// <summary>
        /// Compute MAT for an assessment and update assessment fields
        /// </summary>
        private async Task ComputeAndApplyMatAsync(AdvanceTaxAssessment assessment)
        {
            var availableCredit = await _repository.GetTotalAvailableMatCreditAsync(
                assessment.CompanyId, assessment.FinancialYear);

            var matComputation = ComputeMatInternal(assessment, availableCredit);

            // Update assessment with MAT values
            assessment.IsMatApplicable = matComputation.IsMatApplicable;
            assessment.MatBookProfit = matComputation.BookProfit;
            assessment.MatRate = matComputation.MatRate;
            assessment.MatOnBookProfit = matComputation.MatOnBookProfit;
            assessment.MatSurcharge = matComputation.MatSurcharge;
            assessment.MatCess = matComputation.MatCess;
            assessment.TotalMat = matComputation.TotalMat;
            assessment.MatCreditAvailable = matComputation.MatCreditAvailable;
            assessment.MatCreditToUtilize = matComputation.MatCreditToUtilize;
            assessment.MatCreditCreatedThisYear = matComputation.MatCreditCreatedThisYear;
            assessment.TaxPayableAfterMat = matComputation.FinalTaxPayable;

            // If MAT applies, update net tax payable
            if (assessment.IsMatApplicable)
            {
                // MAT is the tax payable (normal tax is lower)
                // No credits to utilize when MAT applies
                assessment.NetTaxPayable = assessment.TotalMat - assessment.TdsReceivable -
                    assessment.TcsCredit - assessment.AdvanceTaxAlreadyPaid;
            }
            else
            {
                // Normal tax applies, utilize MAT credit if available
                assessment.NetTaxPayable = assessment.TotalTaxLiability -
                    assessment.TdsReceivable - assessment.TcsCredit - assessment.MatCredit -
                    assessment.MatCreditToUtilize - assessment.AdvanceTaxAlreadyPaid;
            }

            assessment.NetTaxPayable = Math.Max(0, assessment.NetTaxPayable);
        }

        private MatComputationDto ComputeMatInternal(AdvanceTaxAssessment assessment, decimal availableCredit)
        {
            // Book profit for MAT (as per Section 115JB)
            // For simplicity, using book profit from reconciliation
            // In full implementation, this would have specific MAT adjustments
            var bookProfit = assessment.BookProfit;

            // MAT Calculation
            var matOnBookProfit = bookProfit * MatRate / 100m;

            // Surcharge on MAT (same rules as normal tax)
            var (_, surchargeRate) = TaxRegimes.GetValueOrDefault(assessment.TaxRegime, (25m, 7m));
            var matSurcharge = bookProfit > 10000000 ? matOnBookProfit * surchargeRate / 100m : 0; // Surcharge if income > 1 Cr

            // Cess on MAT
            var matCess = (matOnBookProfit + matSurcharge) * CessRate / 100m;

            var totalMat = matOnBookProfit + matSurcharge + matCess;

            // Normal tax comparison
            var normalTax = assessment.TotalTaxLiability;

            // Determine applicability
            var isMatApplicable = totalMat > normalTax;

            decimal matCreditCreated = 0;
            decimal matCreditToUtilize = 0;
            decimal finalTaxPayable;
            string applicabilityReason;

            if (isMatApplicable)
            {
                // MAT is higher - pay MAT and create credit for the difference
                matCreditCreated = totalMat - normalTax;
                finalTaxPayable = totalMat;
                applicabilityReason = $"MAT ({totalMat:C}) > Normal Tax ({normalTax:C}). MAT Credit of {matCreditCreated:C} created.";
            }
            else
            {
                // Normal tax is higher - pay normal tax and can utilize MAT credits
                var excessOverMat = normalTax - totalMat;
                matCreditToUtilize = Math.Min(availableCredit, excessOverMat);
                finalTaxPayable = normalTax - matCreditToUtilize;
                applicabilityReason = $"Normal Tax ({normalTax:C}) > MAT ({totalMat:C}). ";
                if (matCreditToUtilize > 0)
                    applicabilityReason += $"Utilizing MAT Credit of {matCreditToUtilize:C}.";
                else if (availableCredit == 0)
                    applicabilityReason += "No MAT Credit available.";
            }

            return new MatComputationDto
            {
                AssessmentId = assessment.Id,
                FinancialYear = assessment.FinancialYear,
                BookProfit = bookProfit,
                MatRate = MatRate,
                MatOnBookProfit = matOnBookProfit,
                MatSurcharge = matSurcharge,
                MatSurchargeRate = surchargeRate,
                MatCess = matCess,
                MatCessRate = CessRate,
                TotalMat = totalMat,
                NormalTax = normalTax,
                IsMatApplicable = isMatApplicable,
                TaxDifference = totalMat - normalTax,
                MatCreditCreatedThisYear = matCreditCreated,
                MatCreditAvailable = availableCredit,
                MatCreditToUtilize = matCreditToUtilize,
                FinalTaxPayable = finalTaxPayable,
                MatApplicabilityReason = applicabilityReason
            };
        }

        /// <summary>
        /// Create or update MAT credit register entry when assessment is finalized
        /// </summary>
        private async Task CreateOrUpdateMatCreditEntryAsync(AdvanceTaxAssessment assessment, Guid userId)
        {
            if (!assessment.IsMatApplicable || assessment.MatCreditCreatedThisYear <= 0)
                return;

            var existingCredit = await _repository.GetMatCreditByCompanyAndFYAsync(
                assessment.CompanyId, assessment.FinancialYear);

            var expiryYear = GetExpiryYear(assessment.FinancialYear, 15);

            if (existingCredit != null)
            {
                // Update existing
                existingCredit.BookProfit = assessment.MatBookProfit;
                existingCredit.MatRate = assessment.MatRate;
                existingCredit.MatOnBookProfit = assessment.MatOnBookProfit;
                existingCredit.MatSurcharge = assessment.MatSurcharge;
                existingCredit.MatCess = assessment.MatCess;
                existingCredit.TotalMat = assessment.TotalMat;
                existingCredit.NormalTax = assessment.TotalTaxLiability;
                existingCredit.MatCreditCreated = assessment.MatCreditCreatedThisYear;
                existingCredit.MatCreditBalance = existingCredit.MatCreditCreated - existingCredit.MatCreditUtilized;

                await _repository.UpdateMatCreditAsync(existingCredit);
            }
            else
            {
                // Create new
                var matCredit = new MatCreditRegister
                {
                    CompanyId = assessment.CompanyId,
                    FinancialYear = assessment.FinancialYear,
                    AssessmentYear = assessment.AssessmentYear,
                    BookProfit = assessment.MatBookProfit,
                    MatRate = assessment.MatRate,
                    MatOnBookProfit = assessment.MatOnBookProfit,
                    MatSurcharge = assessment.MatSurcharge,
                    MatCess = assessment.MatCess,
                    TotalMat = assessment.TotalMat,
                    NormalTax = assessment.TotalTaxLiability,
                    MatCreditCreated = assessment.MatCreditCreatedThisYear,
                    MatCreditUtilized = 0,
                    MatCreditBalance = assessment.MatCreditCreatedThisYear,
                    ExpiryYear = expiryYear,
                    Status = "active",
                    CreatedBy = userId
                };

                await _repository.CreateMatCreditAsync(matCredit);
            }
        }

        /// <summary>
        /// Record MAT credit utilization when assessment uses available credits
        /// </summary>
        private async Task RecordMatCreditUtilizationAsync(AdvanceTaxAssessment assessment)
        {
            if (assessment.MatCreditToUtilize <= 0)
                return;

            var availableCredits = (await _repository.GetAvailableMatCreditsAsync(
                assessment.CompanyId, assessment.FinancialYear)).ToList();

            var remainingToUtilize = assessment.MatCreditToUtilize;

            // FIFO utilization (oldest credits first)
            foreach (var credit in availableCredits.OrderBy(c => c.FinancialYear))
            {
                if (remainingToUtilize <= 0)
                    break;

                var utilizeFromThis = Math.Min(credit.MatCreditBalance, remainingToUtilize);

                // Record utilization
                var utilization = new MatCreditUtilization
                {
                    MatCreditId = credit.Id,
                    UtilizationYear = assessment.FinancialYear,
                    AssessmentId = assessment.Id,
                    AmountUtilized = utilizeFromThis,
                    BalanceAfter = credit.MatCreditBalance - utilizeFromThis
                };

                await _repository.CreateMatCreditUtilizationAsync(utilization);

                // Update credit balance
                credit.MatCreditUtilized += utilizeFromThis;
                credit.MatCreditBalance = credit.MatCreditCreated - credit.MatCreditUtilized;

                if (credit.MatCreditBalance <= 0)
                    credit.Status = "fully_utilized";

                await _repository.UpdateMatCreditAsync(credit);

                remainingToUtilize -= utilizeFromThis;
            }
        }

        private static MatCreditRegisterDto MapMatCreditToDto(MatCreditRegister entity)
        {
            return new MatCreditRegisterDto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                FinancialYear = entity.FinancialYear,
                AssessmentYear = entity.AssessmentYear,
                BookProfit = entity.BookProfit,
                MatRate = entity.MatRate,
                MatOnBookProfit = entity.MatOnBookProfit,
                MatSurcharge = entity.MatSurcharge,
                MatCess = entity.MatCess,
                TotalMat = entity.TotalMat,
                NormalTax = entity.NormalTax,
                MatCreditCreated = entity.MatCreditCreated,
                MatCreditUtilized = entity.MatCreditUtilized,
                MatCreditBalance = entity.MatCreditBalance,
                ExpiryYear = entity.ExpiryYear,
                IsExpired = entity.IsExpired,
                Status = entity.Status,
                Notes = entity.Notes,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static MatCreditUtilizationDto MapMatCreditUtilizationToDto(MatCreditUtilization entity)
        {
            return new MatCreditUtilizationDto
            {
                Id = entity.Id,
                MatCreditId = entity.MatCreditId,
                UtilizationYear = entity.UtilizationYear,
                AssessmentId = entity.AssessmentId,
                AmountUtilized = entity.AmountUtilized,
                BalanceAfter = entity.BalanceAfter,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedAt
            };
        }

        private static string GetExpiryYear(string financialYear, int yearsToAdd)
        {
            var parts = financialYear.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out var startYear))
            {
                var expiryStartYear = startYear + yearsToAdd;
                return $"{expiryStartYear}-{(expiryStartYear + 1) % 100:D2}";
            }
            return financialYear;
        }

        private static string GetFYYearPlusTwoYears(string financialYear)
        {
            return GetExpiryYear(financialYear, 2);
        }
    }
}
