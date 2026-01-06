using System.Text;
using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using Core.Interfaces.Statutory;
using Microsoft.Extensions.Logging;

namespace Application.Services.Statutory
{
    /// <summary>
    /// Service for generating ESI returns for ESIC filing.
    /// </summary>
    public class EsiReturnService : IEsiReturnService
    {
        private readonly IStatutoryPaymentRepository _statutoryPaymentRepository;
        private readonly IPayrollTransactionRepository _payrollTransactionRepository;
        private readonly IPayrollRunRepository _payrollRunRepository;
        private readonly IEmployeePayrollInfoRepository _employeePayrollInfoRepository;
        private readonly ICompaniesRepository _companyRepository;
        private readonly ICompanyStatutoryConfigRepository _statutoryConfigRepository;
        private readonly ILogger<EsiReturnService> _logger;

        public EsiReturnService(
            IStatutoryPaymentRepository statutoryPaymentRepository,
            IPayrollTransactionRepository payrollTransactionRepository,
            IPayrollRunRepository payrollRunRepository,
            IEmployeePayrollInfoRepository employeePayrollInfoRepository,
            ICompaniesRepository companyRepository,
            ICompanyStatutoryConfigRepository statutoryConfigRepository,
            ILogger<EsiReturnService> logger)
        {
            _statutoryPaymentRepository = statutoryPaymentRepository;
            _payrollTransactionRepository = payrollTransactionRepository;
            _payrollRunRepository = payrollRunRepository;
            _employeePayrollInfoRepository = employeePayrollInfoRepository;
            _companyRepository = companyRepository;
            _statutoryConfigRepository = statutoryConfigRepository;
            _logger = logger;
        }

        // ==================== Return Generation ====================

        public async Task<Result<EsiReturnData>> GenerateReturnAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            try
            {
                _logger.LogInformation(
                    "Generating ESI return for company {CompanyId}, period {Month}/{Year}",
                    companyId, periodMonth, periodYear);

                // Get company and statutory config
                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company with ID {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(companyId);
                if (statutoryConfig == null || !statutoryConfig.EsiEnabled)
                    return Error.Validation("ESI is not enabled for this company");

                if (string.IsNullOrWhiteSpace(statutoryConfig.EsiRegistrationNumber))
                    return Error.Validation("ESI registration number not configured for company");

                // Get payroll transactions for the period
                var transactions = await _payrollTransactionRepository.GetByCompanyAndPeriodAsync(
                    companyId, periodMonth, periodYear);

                if (!transactions.Any())
                    return Error.NotFound($"No payroll transactions found for {periodMonth}/{periodYear}");

                // Get employee payroll info for ESI numbers
                var employeeIds = transactions.Select(t => t.EmployeeId).Distinct().ToList();
                var employeePayrollInfos = await _employeePayrollInfoRepository.GetByEmployeeIdsAsync(employeeIds);
                var payrollInfoDict = employeePayrollInfos.ToDictionary(p => p.EmployeeId);

                // Calculate financial year and contribution period
                var financialYear = GetFinancialYear(periodMonth, periodYear);
                var contributionPeriod = GetContributionPeriod(periodMonth, periodYear);
                var dueDate = GetDueDate(periodMonth, periodYear);

                // Build return data
                var returnData = new EsiReturnData
                {
                    CompanyId = companyId,
                    PeriodMonth = periodMonth,
                    PeriodYear = periodYear,
                    FinancialYear = financialYear,
                    ContributionPeriod = contributionPeriod,
                    WageMonth = $"{periodMonth:D2}{periodYear}",
                    EsiCode = statutoryConfig.EsiRegistrationNumber,
                    EmployerName = company.Name,
                    DueDate = dueDate,
                    IsOverdue = DateOnly.FromDateTime(DateTime.Today) > dueDate,
                    DaysOverdue = Math.Max(0, DateOnly.FromDateTime(DateTime.Today).DayNumber - dueDate.DayNumber)
                };

                // Build employee records
                foreach (var transaction in transactions)
                {
                    var payrollInfo = payrollInfoDict.GetValueOrDefault(transaction.EmployeeId);
                    var employee = transaction.Employee;

                    var isCovered = transaction.EsiEmployee > 0 || transaction.EsiEmployer > 0;

                    var employeeRecord = new EsiReturnEmployeeRecord
                    {
                        EmployeeId = transaction.EmployeeId,
                        PayrollTransactionId = transaction.Id,
                        IpNumber = payrollInfo?.EsiNumber ?? string.Empty,
                        EmployeeName = employee?.EmployeeName ?? string.Empty,
                        GrossWages = transaction.GrossEarnings,
                        IsCovered = isCovered,
                        EmployeeContribution = transaction.EsiEmployee,
                        EmployerContribution = transaction.EsiEmployer,
                        TotalContribution = transaction.EsiEmployee + transaction.EsiEmployer,
                        DaysWorked = transaction.PresentDays,
                        AbsentDays = transaction.LopDays,
                        DateOfJoining = payrollInfo?.DateOfJoining,
                        NoContributionReason = !isCovered ? "Gross exceeds ESI ceiling" : null
                    };

                    returnData.EmployeeRecords.Add(employeeRecord);
                }

                // Calculate totals
                returnData.EmployeeCount = returnData.EmployeeRecords.Count;
                returnData.CoveredEmployees = returnData.EmployeeRecords.Count(r => r.IsCovered);
                returnData.TotalGrossWages = returnData.EmployeeRecords.Sum(r => r.GrossWages);
                returnData.TotalCoveredWages = returnData.EmployeeRecords.Where(r => r.IsCovered).Sum(r => r.GrossWages);
                returnData.TotalEmployeeContribution = returnData.EmployeeRecords.Sum(r => r.EmployeeContribution);
                returnData.TotalEmployerContribution = returnData.EmployeeRecords.Sum(r => r.EmployerContribution);
                returnData.TotalContribution = returnData.TotalEmployeeContribution + returnData.TotalEmployerContribution;

                _logger.LogInformation(
                    "Generated ESI return with {EmployeeCount} employees ({CoveredCount} covered), total contribution {Total}",
                    returnData.EmployeeCount, returnData.CoveredEmployees, returnData.TotalContribution);

                return Result<EsiReturnData>.Success(returnData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating ESI return for company {CompanyId}", companyId);
                return Error.Internal($"Error generating ESI return: {ex.Message}");
            }
        }

        public async Task<Result<EsiReturnData>> GenerateReturnFromPayrollRunAsync(Guid payrollRunId)
        {
            var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
            if (payrollRun == null)
                return Error.NotFound($"Payroll run with ID {payrollRunId} not found");

            var result = await GenerateReturnAsync(
                payrollRun.CompanyId,
                payrollRun.PayrollMonth,
                payrollRun.PayrollYear);

            if (result.IsSuccess)
            {
                result.Value.PayrollRunId = payrollRunId;
            }

            return result;
        }

        public async Task<Result<EsiReturnPreview>> PreviewReturnAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var returnResult = await GenerateReturnAsync(companyId, periodMonth, periodYear);
            if (returnResult.IsFailure)
                return Result<EsiReturnPreview>.Failure(returnResult.Error!);

            var returnData = returnResult.Value;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var proposedPaymentDate = today < returnData.DueDate ? returnData.DueDate : today;

            var preview = new EsiReturnPreview
            {
                ReturnData = returnData,
                ProposedPaymentDate = proposedPaymentDate,
                BaseAmount = returnData.TotalContribution,
                InterestAmount = CalculateInterest(returnData.TotalContribution, returnData.DueDate, proposedPaymentDate)
            };

            preview.TotalPayable = preview.BaseAmount + preview.InterestAmount;

            // Validate and add warnings
            ValidateReturnData(returnData, preview);

            return Result<EsiReturnPreview>.Success(preview);
        }

        public async Task<Result<EsiReturnFileResult>> GenerateReturnFileAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var returnResult = await GenerateReturnAsync(companyId, periodMonth, periodYear);
            if (returnResult.IsFailure)
                return Result<EsiReturnFileResult>.Failure(returnResult.Error!);

            var returnData = returnResult.Value;
            var fileContent = GenerateEsiTextContent(returnData);

            var fileName = $"ESI_{returnData.EsiCode}_{periodMonth:D2}{periodYear}.txt";

            return Result<EsiReturnFileResult>.Success(new EsiReturnFileResult
            {
                FileName = fileName,
                FileContent = fileContent,
                FileFormat = "txt",
                RecordCount = returnData.CoveredEmployees,
                TotalAmount = returnData.TotalContribution,
                GeneratedAt = DateTime.UtcNow,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent))
            });
        }

        // ==================== Return Operations ====================

        public async Task<Result<StatutoryPayment>> CreateReturnPaymentAsync(CreateEsiReturnRequest request)
        {
            try
            {
                // Validate no duplicate
                var existing = await _statutoryPaymentRepository.GetByPeriodAndTypeAsync(
                    request.CompanyId, request.PeriodMonth, request.PeriodYear, "ESI");

                if (existing != null && existing.Status != "cancelled")
                    return Error.Conflict($"ESI return already exists for {request.PeriodMonth}/{request.PeriodYear}");

                // Generate return to get amounts
                var returnResult = await GenerateReturnAsync(
                    request.CompanyId, request.PeriodMonth, request.PeriodYear);

                if (returnResult.IsFailure)
                    return Result<StatutoryPayment>.Failure(returnResult.Error!);

                var returnData = returnResult.Value;
                var dueDate = GetDueDate(request.PeriodMonth, request.PeriodYear);
                var interest = CalculateInterest(returnData.TotalContribution, dueDate, request.ProposedPaymentDate);

                var payment = new StatutoryPayment
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    PaymentType = "ESI",
                    FinancialYear = returnData.FinancialYear,
                    PeriodMonth = request.PeriodMonth,
                    PeriodYear = request.PeriodYear,
                    PrincipalAmount = returnData.TotalContribution,
                    InterestAmount = interest,
                    PenaltyAmount = 0, // ESI doesn't have damages like PF
                    LateFee = 0,
                    TotalAmount = returnData.TotalContribution + interest,
                    DueDate = dueDate,
                    Status = "pending",
                    CreatedBy = request.CreatedBy,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _statutoryPaymentRepository.AddAsync(payment);

                _logger.LogInformation(
                    "Created ESI return payment {PaymentId} for {Month}/{Year}",
                    payment.Id, request.PeriodMonth, request.PeriodYear);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ESI return payment");
                return Error.Internal($"Error creating ESI payment: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> RecordPaymentAsync(
            Guid paymentId,
            RecordEsiPaymentRequest request)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Statutory payment with ID {paymentId} not found");

                if (payment.PaymentType != "ESI")
                    return Error.Validation("Payment is not an ESI return payment");

                if (payment.Status == "paid" || payment.Status == "filed")
                    return Error.Conflict("Payment has already been recorded");

                // Update payment details
                payment.PaymentDate = request.PaymentDate;
                payment.PaymentMode = request.PaymentMode;
                payment.BankName = request.BankName;
                payment.BankAccountId = request.BankAccountId;
                payment.BankReference = request.BankReference;
                payment.TotalAmount = request.ActualAmountPaid;
                payment.Status = "paid";
                payment.PaidBy = request.PaidBy;
                payment.PaidAt = DateTime.UtcNow;
                payment.Notes = request.Notes ?? payment.Notes;
                payment.UpdatedAt = DateTime.UtcNow;

                await _statutoryPaymentRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Recorded ESI payment {PaymentId}, amount {Amount}",
                    paymentId, request.ActualAmountPaid);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording ESI payment {PaymentId}", paymentId);
                return Error.Internal($"Error recording payment: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> UpdateChallanNumberAsync(Guid paymentId, string challanNumber)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Statutory payment with ID {paymentId} not found");

                if (payment.PaymentType != "ESI")
                    return Error.Validation("Payment is not an ESI return payment");

                payment.ChallanNumber = challanNumber;
                payment.Status = "filed";
                payment.FiledAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _statutoryPaymentRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Updated challan number {ChallanNumber} for payment {PaymentId}",
                    challanNumber, paymentId);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating challan number for payment {PaymentId}", paymentId);
                return Error.Internal($"Error updating challan number: {ex.Message}");
            }
        }

        // ==================== Retrieval ====================

        public async Task<Result<(IEnumerable<EsiReturnListDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            int pageNumber,
            int pageSize,
            string? financialYear = null,
            string? status = null,
            string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation(
                    "Getting paged ESI returns for Company {CompanyId}, Page {Page}, Size {Size}",
                    companyId, pageNumber, pageSize);

                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(companyId);
                var esiCode = statutoryConfig?.EsiRegistrationNumber ?? string.Empty;

                // Get ESI payments for the company
                IEnumerable<StatutoryPayment> payments;

                if (!string.IsNullOrEmpty(financialYear))
                {
                    payments = await _statutoryPaymentRepository.GetByFinancialYearAsync(companyId, financialYear);
                }
                else
                {
                    var pending = await _statutoryPaymentRepository.GetPendingAsync(companyId);
                    var allPayments = await _statutoryPaymentRepository.GetByFinancialYearAsync(companyId, GetCurrentFinancialYear());
                    payments = pending.Union(allPayments).DistinctBy(p => p.Id);
                }

                // Filter to ESI payments only
                var esiPayments = payments
                    .Where(p => p.PaymentType == "ESI")
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    esiPayments = esiPayments.Where(p => p.Status == status);
                }

                // Apply search filter (search by challan number or reference)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    esiPayments = esiPayments.Where(p =>
                        (p.ReferenceNumber != null && p.ReferenceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.ReceiptNumber != null && p.ReceiptNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = esiPayments.Count();

                // Order by period (most recent first) and apply pagination
                var pagedPayments = esiPayments
                    .OrderByDescending(p => p.PeriodYear)
                    .ThenByDescending(p => p.PeriodMonth)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to DTOs
                var items = pagedPayments.Select(p => new EsiReturnListDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    EsiCode = esiCode,
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    FinancialYear = p.FinancialYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    ContributionPeriod = GetContributionPeriod(p.PeriodMonth, p.PeriodYear),
                    EmployeeCount = 0, // Would need to query payroll transactions for actual count
                    TotalEmployeeContribution = p.PrincipalAmount * 0.1875m, // 0.75 / 4 (emp is ~18.75% of total)
                    TotalEmployerContribution = p.PrincipalAmount * 0.8125m, // 3.25 / 4 (emp is ~81.25% of total)
                    TotalContribution = p.PrincipalAmount,
                    Interest = p.InterestAmount,
                    TotalAmount = p.TotalAmount,
                    DueDate = p.DueDate,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status,
                    ChallanNumber = p.ReceiptNumber,
                    CreatedAt = p.CreatedAt
                });

                return Result<(IEnumerable<EsiReturnListDto> Items, int TotalCount)>.Success((items, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged ESI returns for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting returns: {ex.Message}");
            }
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.Today;
            return GetFinancialYear(today.Month, today.Year);
        }

        public async Task<Result<IEnumerable<PendingEsiReturnDto>>> GetPendingReturnsAsync(
            Guid companyId,
            string? financialYear = null)
        {
            try
            {
                var pendingPayments = await _statutoryPaymentRepository.GetPendingByCompanyAsync(
                    companyId, "ESI", financialYear);

                var pendingDtos = pendingPayments.Select(p => new PendingEsiReturnDto
                {
                    StatutoryPaymentId = p.Id,
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    TotalContribution = p.PrincipalAmount,
                    InterestAmount = p.InterestAmount,
                    TotalAmount = p.TotalAmount,
                    DueDate = p.DueDate,
                    IsOverdue = p.IsOverdue,
                    DaysOverdue = p.DaysOverdue,
                    Status = p.Status
                }).ToList();

                return Result<IEnumerable<PendingEsiReturnDto>>.Success(pendingDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending ESI returns for company {CompanyId}", companyId);
                return Error.Internal($"Error getting pending returns: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<FiledEsiReturnDto>>> GetFiledReturnsAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var filedPayments = await _statutoryPaymentRepository.GetPaidByCompanyAsync(
                    companyId, "ESI", financialYear);

                var filedDtos = filedPayments.Select(p => new FiledEsiReturnDto
                {
                    StatutoryPaymentId = p.Id,
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    TotalContribution = p.PrincipalAmount,
                    InterestAmount = p.InterestAmount,
                    TotalPaid = p.TotalAmount,
                    DueDate = p.DueDate,
                    PaymentDate = p.PaymentDate ?? DateOnly.MinValue,
                    ChallanNumber = p.ChallanNumber,
                    Status = p.Status
                }).ToList();

                return Result<IEnumerable<FiledEsiReturnDto>>.Success(filedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filed ESI returns for company {CompanyId}", companyId);
                return Error.Internal($"Error getting filed returns: {ex.Message}");
            }
        }

        public async Task<Result<EsiReturnDetailDto>> GetReturnByIdAsync(Guid paymentId)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment with ID {paymentId} not found");

                if (payment.PaymentType != "ESI")
                    return Error.Validation("Payment is not an ESI return payment");

                var company = await _companyRepository.GetByIdAsync(payment.CompanyId);
                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(payment.CompanyId);

                // Get return data for employee records
                var returnResult = await GenerateReturnAsync(
                    payment.CompanyId, payment.PeriodMonth, payment.PeriodYear);

                var detail = new EsiReturnDetailDto
                {
                    Id = payment.Id,
                    CompanyId = payment.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    EsiCode = statutoryConfig?.EsiRegistrationNumber ?? string.Empty,
                    PeriodMonth = payment.PeriodMonth,
                    PeriodYear = payment.PeriodYear,
                    FinancialYear = payment.FinancialYear,
                    ContributionPeriod = GetContributionPeriod(payment.PeriodMonth, payment.PeriodYear),
                    TotalContribution = payment.PrincipalAmount,
                    InterestAmount = payment.InterestAmount,
                    TotalPaid = payment.TotalAmount,
                    DueDate = payment.DueDate,
                    PaymentDate = payment.PaymentDate,
                    Status = payment.Status,
                    ChallanNumber = payment.ChallanNumber,
                    PaymentMode = payment.PaymentMode,
                    BankName = payment.BankName,
                    BankReference = payment.BankReference,
                    EmployeeRecords = returnResult.IsSuccess ? returnResult.Value.EmployeeRecords : new List<EsiReturnEmployeeRecord>()
                };

                // Populate totals from return data if available
                if (returnResult.IsSuccess)
                {
                    detail.EmployeeCount = returnResult.Value.EmployeeCount;
                    detail.TotalGrossWages = returnResult.Value.TotalGrossWages;
                    detail.TotalEmployeeContribution = returnResult.Value.TotalEmployeeContribution;
                    detail.TotalEmployerContribution = returnResult.Value.TotalEmployerContribution;
                }

                return Result<EsiReturnDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ESI return details for payment {PaymentId}", paymentId);
                return Error.Internal($"Error getting return details: {ex.Message}");
            }
        }

        // ==================== Utility ====================

        public DateOnly GetDueDate(int periodMonth, int periodYear)
        {
            // ESI due date is 15th of following month
            var followingMonth = periodMonth == 12 ? 1 : periodMonth + 1;
            var followingYear = periodMonth == 12 ? periodYear + 1 : periodYear;
            return new DateOnly(followingYear, followingMonth, 15);
        }

        public decimal CalculateInterest(decimal esiAmount, DateOnly dueDate, DateOnly paymentDate)
        {
            if (paymentDate <= dueDate)
                return 0;

            // Interest rate: 12% per annum (simple interest)
            var daysLate = paymentDate.DayNumber - dueDate.DayNumber;
            var interestRate = 12m / 100m; // Annual rate
            var dailyRate = interestRate / 365m;

            return Math.Round(esiAmount * dailyRate * daysLate, 0, MidpointRounding.AwayFromZero);
        }

        public string GetContributionPeriod(int month, int year)
        {
            // April-September = apr_sep, October-March = oct_mar
            return month >= 4 && month <= 9 ? "apr_sep" : "oct_mar";
        }

        public async Task<Result<EsiReturnSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var (startYear, endYear) = ParseFinancialYear(financialYear);

                var summary = new EsiReturnSummary
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear
                };

                // Get all ESI payments for the financial year
                var payments = await _statutoryPaymentRepository.GetByCompanyAndFyAsync(
                    companyId, "ESI", financialYear);
                var paymentDict = payments.ToDictionary(
                    p => (p.PeriodMonth, p.PeriodYear),
                    p => p);

                // Generate status for each month (April to March)
                for (int i = 0; i < 12; i++)
                {
                    var month = (i + 4 - 1) % 12 + 1;
                    var year = month >= 4 ? startYear : endYear;

                    var monthlyStatus = new MonthlyEsiStatus
                    {
                        Month = month,
                        Year = year,
                        MonthName = new DateTime(year, month, 1).ToString("MMM yyyy"),
                        ContributionPeriod = GetContributionPeriod(month, year),
                        DueDate = GetDueDate(month, year)
                    };

                    // Get deducted ESI from payroll
                    var transactions = await _payrollTransactionRepository.GetByCompanyAndPeriodAsync(
                        companyId, month, year);
                    monthlyStatus.EsiDeducted = transactions.Sum(t => t.EsiEmployee + t.EsiEmployer);
                    monthlyStatus.EmployeeCount = transactions.Count(t => t.EsiEmployee > 0);

                    // Get deposited ESI from statutory payments
                    if (paymentDict.TryGetValue((month, year), out var payment))
                    {
                        monthlyStatus.StatutoryPaymentId = payment.Id;
                        monthlyStatus.EsiDeposited = payment.TotalAmount;
                        monthlyStatus.PaymentDate = payment.PaymentDate;
                        monthlyStatus.ChallanNumber = payment.ChallanNumber;
                        monthlyStatus.Status = payment.Status;
                    }
                    else
                    {
                        monthlyStatus.Status = DateOnly.FromDateTime(DateTime.Today) > monthlyStatus.DueDate
                            ? "overdue"
                            : "pending";
                    }

                    monthlyStatus.Variance = monthlyStatus.EsiDeducted - monthlyStatus.EsiDeposited;

                    summary.MonthlyStatus.Add(monthlyStatus);
                }

                // Calculate totals
                summary.TotalEsiDeducted = summary.MonthlyStatus.Sum(m => m.EsiDeducted);
                summary.TotalEsiDeposited = summary.MonthlyStatus.Sum(m => m.EsiDeposited);
                summary.TotalVariance = summary.TotalEsiDeducted - summary.TotalEsiDeposited;
                summary.PaidCount = summary.MonthlyStatus.Count(m => m.Status == "paid" || m.Status == "filed");
                summary.PendingCount = summary.MonthlyStatus.Count(m => m.Status == "pending");
                summary.OverdueCount = summary.MonthlyStatus.Count(m => m.Status == "overdue");

                return Result<EsiReturnSummary>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ESI summary for company {CompanyId}", companyId);
                return Error.Internal($"Error getting summary: {ex.Message}");
            }
        }

        public async Task<Result<EsiReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var summaryResult = await GetMonthlySummaryAsync(companyId, financialYear);
                if (summaryResult.IsFailure)
                    return Result<EsiReconciliation>.Failure(summaryResult.Error!);

                var summary = summaryResult.Value;

                var reconciliation = new EsiReconciliation
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear,
                    TotalEsiDeducted = summary.TotalEsiDeducted,
                    TotalEsiDeposited = summary.TotalEsiDeposited,
                    Variance = summary.TotalVariance,
                    MonthlyReconciliation = summary.MonthlyStatus.Select(m => new EsiReconciliationItem
                    {
                        Month = m.Month,
                        Year = m.Year,
                        EmployeeCount = m.EmployeeCount,
                        Deducted = m.EsiDeducted,
                        Deposited = m.EsiDeposited,
                        Variance = m.Variance,
                        Status = m.Variance == 0 ? "reconciled" : "mismatch"
                    }).ToList()
                };

                // Identify mismatches
                foreach (var item in reconciliation.MonthlyReconciliation.Where(m => m.Variance != 0))
                {
                    reconciliation.Mismatches.Add(new EsiReconciliationMismatch
                    {
                        MismatchType = item.Variance > 0 ? "under_deposited" : "over_deposited",
                        Description = $"Variance of ₹{Math.Abs(item.Variance):N0} for {new DateTime(item.Year, item.Month, 1):MMM yyyy}",
                        Month = item.Month,
                        Amount = Math.Abs(item.Variance),
                        SuggestedAction = item.Variance > 0
                            ? "File supplementary return for the shortfall"
                            : "Verify calculations and adjust in next month"
                    });
                }

                return Result<EsiReconciliation>.Success(reconciliation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling ESI for company {CompanyId}", companyId);
                return Error.Internal($"Error reconciling: {ex.Message}");
            }
        }

        // ==================== Private Helper Methods ====================

        private static string GetFinancialYear(int month, int year)
        {
            var fyStartYear = month >= 4 ? year : year - 1;
            return $"{fyStartYear}-{(fyStartYear + 1) % 100:D2}";
        }

        private static (int StartYear, int EndYear) ParseFinancialYear(string financialYear)
        {
            var parts = financialYear.Split('-');
            var startYear = int.Parse(parts[0]);
            var endYear = startYear + 1;
            return (startYear, endYear);
        }

        private static void ValidateReturnData(EsiReturnData returnData, EsiReturnPreview preview)
        {
            // Check for missing IP numbers
            var missingIp = returnData.EmployeeRecords.Where(r => r.IsCovered && string.IsNullOrWhiteSpace(r.IpNumber)).ToList();
            if (missingIp.Any())
            {
                preview.HasWarnings = true;
                foreach (var employee in missingIp)
                {
                    preview.Warnings.Add($"Missing IP number for covered employee: {employee.EmployeeName}");
                    preview.ValidationErrors.Add(new EsiValidationError
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = employee.EmployeeName,
                        ErrorCode = "MISSING_IP",
                        ErrorMessage = "IP number is required for ESIC filing",
                        Severity = "error"
                    });
                }
            }

            // Check if overdue
            if (returnData.IsOverdue)
            {
                preview.HasWarnings = true;
                preview.Warnings.Add($"Payment is overdue by {returnData.DaysOverdue} days. Interest will apply.");
            }

            // Check for employees with zero contribution who might be covered
            var zeroCoveredEmployees = returnData.EmployeeRecords
                .Where(r => r.IsCovered && r.TotalContribution == 0)
                .ToList();
            if (zeroCoveredEmployees.Any())
            {
                preview.HasWarnings = true;
                foreach (var employee in zeroCoveredEmployees)
                {
                    preview.Warnings.Add($"Zero contribution for covered employee: {employee.EmployeeName}");
                    preview.ValidationErrors.Add(new EsiValidationError
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeName = employee.EmployeeName,
                        ErrorCode = "ZERO_CONTRIBUTION",
                        ErrorMessage = "Covered employee has zero ESI contribution",
                        Severity = "warning"
                    });
                }
            }
        }

        private static string GenerateEsiTextContent(EsiReturnData returnData)
        {
            // ESIC return file format (pipe-delimited text file)
            var sb = new StringBuilder();

            // Only include covered employees in the file
            foreach (var employee in returnData.EmployeeRecords.Where(r => r.IsCovered))
            {
                // Format: IPNumber|Name|DaysWorked|GrossWages|EmployeeContribution|EmployerContribution
                var line = string.Join("|",
                    employee.IpNumber,
                    CleanName(employee.EmployeeName),
                    employee.DaysWorked.ToString(),
                    employee.GrossWages.ToString("F0"),
                    employee.EmployeeContribution.ToString("F0"),
                    employee.EmployerContribution.ToString("F0"),
                    employee.AbsentDays.ToString(),
                    employee.NoContributionReason ?? ""
                );

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private static string CleanName(string name)
        {
            return name?.Replace("|", " ").Replace("#", " ").Trim() ?? string.Empty;
        }
    }
}
