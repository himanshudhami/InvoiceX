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
    /// Service for generating PF ECR (Electronic Challan-cum-Return) for EPFO filing.
    /// </summary>
    public class PfEcrService : IPfEcrService
    {
        private readonly IStatutoryPaymentRepository _statutoryPaymentRepository;
        private readonly IPayrollTransactionRepository _payrollTransactionRepository;
        private readonly IPayrollRunRepository _payrollRunRepository;
        private readonly IEmployeePayrollInfoRepository _employeePayrollInfoRepository;
        private readonly ICompaniesRepository _companyRepository;
        private readonly ICompanyStatutoryConfigRepository _statutoryConfigRepository;
        private readonly ILogger<PfEcrService> _logger;

        public PfEcrService(
            IStatutoryPaymentRepository statutoryPaymentRepository,
            IPayrollTransactionRepository payrollTransactionRepository,
            IPayrollRunRepository payrollRunRepository,
            IEmployeePayrollInfoRepository employeePayrollInfoRepository,
            ICompaniesRepository companyRepository,
            ICompanyStatutoryConfigRepository statutoryConfigRepository,
            ILogger<PfEcrService> logger)
        {
            _statutoryPaymentRepository = statutoryPaymentRepository;
            _payrollTransactionRepository = payrollTransactionRepository;
            _payrollRunRepository = payrollRunRepository;
            _employeePayrollInfoRepository = employeePayrollInfoRepository;
            _companyRepository = companyRepository;
            _statutoryConfigRepository = statutoryConfigRepository;
            _logger = logger;
        }

        // ==================== ECR Generation ====================

        public async Task<Result<PfEcrData>> GenerateEcrAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            try
            {
                _logger.LogInformation(
                    "Generating PF ECR for company {CompanyId}, period {Month}/{Year}",
                    companyId, periodMonth, periodYear);

                // Get company and statutory config
                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company with ID {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(companyId);
                if (statutoryConfig == null || !statutoryConfig.PfEnabled)
                    return Error.Validation("PF is not enabled for this company");

                if (string.IsNullOrWhiteSpace(statutoryConfig.PfRegistrationNumber))
                    return Error.Validation("PF registration number not configured for company");

                // Get payroll transactions for the period
                var transactions = await _payrollTransactionRepository.GetByCompanyAndPeriodAsync(
                    companyId, periodMonth, periodYear);

                var pfTransactions = transactions
                    .Where(t => t.PfEmployee > 0 || t.PfEmployer > 0)
                    .ToList();

                if (!pfTransactions.Any())
                    return Error.NotFound($"No PF-applicable payroll transactions found for {periodMonth}/{periodYear}");

                // Get employee payroll info for UAN
                var employeeIds = pfTransactions.Select(t => t.EmployeeId).Distinct().ToList();
                var employeePayrollInfos = await _employeePayrollInfoRepository.GetByEmployeeIdsAsync(employeeIds);
                var payrollInfoDict = employeePayrollInfos.ToDictionary(p => p.EmployeeId);

                // Calculate financial year
                var financialYear = GetFinancialYear(periodMonth, periodYear);
                var dueDate = GetDueDate(periodMonth, periodYear);

                // Build ECR data
                var ecrData = new PfEcrData
                {
                    CompanyId = companyId,
                    PeriodMonth = periodMonth,
                    PeriodYear = periodYear,
                    FinancialYear = financialYear,
                    WageMonth = $"{periodMonth:D2}{periodYear}",
                    EstablishmentCode = statutoryConfig.PfRegistrationNumber,
                    EstablishmentName = company.Name,
                    DueDate = dueDate,
                    IsOverdue = DateOnly.FromDateTime(DateTime.Today) > dueDate,
                    DaysOverdue = Math.Max(0, DateOnly.FromDateTime(DateTime.Today).DayNumber - dueDate.DayNumber)
                };

                // Build member records
                foreach (var transaction in pfTransactions)
                {
                    var payrollInfo = payrollInfoDict.GetValueOrDefault(transaction.EmployeeId);
                    var employee = transaction.Employee;

                    var memberRecord = new PfEcrMemberRecord
                    {
                        EmployeeId = transaction.EmployeeId,
                        PayrollTransactionId = transaction.Id,
                        Uan = payrollInfo?.Uan ?? string.Empty,
                        MemberName = employee?.EmployeeName ?? string.Empty,
                        MemberId = payrollInfo?.PfAccountNumber,
                        GrossWages = transaction.GrossEarnings,
                        EpfWages = transaction.BasicEarned + transaction.DaEarned,
                        EpsWages = Math.Min(transaction.BasicEarned + transaction.DaEarned, 15000),
                        EdliWages = Math.Min(transaction.BasicEarned + transaction.DaEarned, 15000),
                        EmployeeEpfContribution = transaction.PfEmployee,
                        EmployerEpfContribution = transaction.PfEmployer - GetEpsContribution(transaction),
                        EmployerEpsContribution = GetEpsContribution(transaction),
                        TotalContribution = transaction.PfEmployee + transaction.PfEmployer,
                        NcpDays = transaction.LopDays,
                        DateOfJoining = payrollInfo?.DateOfJoining
                    };

                    ecrData.MemberRecords.Add(memberRecord);
                }

                // Calculate totals
                ecrData.MemberCount = ecrData.MemberRecords.Count;
                ecrData.TotalEpfWages = ecrData.MemberRecords.Sum(m => m.EpfWages);
                ecrData.TotalEpsWages = ecrData.MemberRecords.Sum(m => m.EpsWages);
                ecrData.TotalEdliWages = ecrData.MemberRecords.Sum(m => m.EdliWages);
                ecrData.TotalEmployeeContribution = ecrData.MemberRecords.Sum(m => m.EmployeeEpfContribution);
                ecrData.TotalEmployerEpfContribution = ecrData.MemberRecords.Sum(m => m.EmployerEpfContribution);
                ecrData.TotalEmployerEpsContribution = ecrData.MemberRecords.Sum(m => m.EmployerEpsContribution);
                ecrData.TotalAdminCharges = pfTransactions.Sum(t => t.PfAdminCharges);
                ecrData.TotalEdliCharges = pfTransactions.Sum(t => t.PfEdli);
                ecrData.TotalContribution = ecrData.TotalEmployeeContribution +
                                            ecrData.TotalEmployerEpfContribution +
                                            ecrData.TotalEmployerEpsContribution +
                                            ecrData.TotalAdminCharges +
                                            ecrData.TotalEdliCharges;

                _logger.LogInformation(
                    "Generated ECR with {MemberCount} members, total contribution {Total}",
                    ecrData.MemberCount, ecrData.TotalContribution);

                return Result<PfEcrData>.Success(ecrData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PF ECR for company {CompanyId}", companyId);
                return Error.Internal($"Error generating ECR: {ex.Message}");
            }
        }

        public async Task<Result<PfEcrData>> GenerateEcrFromPayrollRunAsync(Guid payrollRunId)
        {
            var payrollRun = await _payrollRunRepository.GetByIdAsync(payrollRunId);
            if (payrollRun == null)
                return Error.NotFound($"Payroll run with ID {payrollRunId} not found");

            var result = await GenerateEcrAsync(
                payrollRun.CompanyId,
                payrollRun.PayrollMonth,
                payrollRun.PayrollYear);

            if (result.IsSuccess)
            {
                result.Value.PayrollRunId = payrollRunId;
            }

            return result;
        }

        public async Task<Result<PfEcrPreview>> PreviewEcrAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var ecrResult = await GenerateEcrAsync(companyId, periodMonth, periodYear);
            if (ecrResult.IsFailure)
                return Result<PfEcrPreview>.Failure(ecrResult.Error!);

            var ecrData = ecrResult.Value;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var proposedPaymentDate = today < ecrData.DueDate ? ecrData.DueDate : today;

            var preview = new PfEcrPreview
            {
                EcrData = ecrData,
                ProposedPaymentDate = proposedPaymentDate,
                BaseAmount = ecrData.TotalContribution,
                InterestAmount = CalculateInterest(ecrData.TotalContribution, ecrData.DueDate, proposedPaymentDate),
                DamagesAmount = CalculateDamages(ecrData.TotalContribution, ecrData.DueDate, proposedPaymentDate)
            };

            preview.TotalPayable = preview.BaseAmount + preview.InterestAmount + preview.DamagesAmount;

            // Validate and add warnings
            ValidateEcrData(ecrData, preview);

            return Result<PfEcrPreview>.Success(preview);
        }

        public async Task<Result<PfEcrFileResult>> GenerateEcrFileAsync(
            Guid companyId,
            int periodMonth,
            int periodYear)
        {
            var ecrResult = await GenerateEcrAsync(companyId, periodMonth, periodYear);
            if (ecrResult.IsFailure)
                return Result<PfEcrFileResult>.Failure(ecrResult.Error!);

            var ecrData = ecrResult.Value;
            var fileContent = GenerateEcrTextContent(ecrData);

            var fileName = $"ECR_{ecrData.EstablishmentCode}_{periodMonth:D2}{periodYear}.txt";

            return Result<PfEcrFileResult>.Success(new PfEcrFileResult
            {
                FileName = fileName,
                FileContent = fileContent,
                FileFormat = "txt",
                RecordCount = ecrData.MemberCount,
                TotalAmount = ecrData.TotalContribution,
                GeneratedAt = DateTime.UtcNow,
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileContent))
            });
        }

        // ==================== ECR Operations ====================

        public async Task<Result<StatutoryPayment>> CreateEcrPaymentAsync(CreatePfEcrRequest request)
        {
            try
            {
                // Validate no duplicate
                var existing = await _statutoryPaymentRepository.GetByPeriodAndTypeAsync(
                    request.CompanyId, request.PeriodMonth, request.PeriodYear, "PF");

                if (existing != null && existing.Status != "cancelled")
                    return Error.Conflict($"PF ECR already exists for {request.PeriodMonth}/{request.PeriodYear}");

                // Generate ECR to get amounts
                var ecrResult = await GenerateEcrAsync(
                    request.CompanyId, request.PeriodMonth, request.PeriodYear);

                if (ecrResult.IsFailure)
                    return Result<StatutoryPayment>.Failure(ecrResult.Error!);

                var ecrData = ecrResult.Value;
                var dueDate = GetDueDate(request.PeriodMonth, request.PeriodYear);
                var interest = CalculateInterest(ecrData.TotalContribution, dueDate, request.ProposedPaymentDate);
                var damages = CalculateDamages(ecrData.TotalContribution, dueDate, request.ProposedPaymentDate);

                var payment = new StatutoryPayment
                {
                    Id = Guid.NewGuid(),
                    CompanyId = request.CompanyId,
                    PaymentType = "PF",
                    FinancialYear = ecrData.FinancialYear,
                    PeriodMonth = request.PeriodMonth,
                    PeriodYear = request.PeriodYear,
                    PrincipalAmount = ecrData.TotalContribution,
                    InterestAmount = interest,
                    PenaltyAmount = damages,
                    LateFee = 0,
                    TotalAmount = ecrData.TotalContribution + interest + damages,
                    DueDate = dueDate,
                    Status = "pending",
                    CreatedBy = request.CreatedBy,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _statutoryPaymentRepository.AddAsync(payment);

                _logger.LogInformation(
                    "Created PF ECR payment {PaymentId} for {Month}/{Year}",
                    payment.Id, request.PeriodMonth, request.PeriodYear);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PF ECR payment");
                return Error.Internal($"Error creating ECR payment: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> RecordEcrPaymentAsync(
            Guid paymentId,
            RecordPfEcrPaymentRequest request)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Statutory payment with ID {paymentId} not found");

                if (payment.PaymentType != "PF")
                    return Error.Validation("Payment is not a PF ECR payment");

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
                    "Recorded PF ECR payment {PaymentId}, amount {Amount}",
                    paymentId, request.ActualAmountPaid);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording PF ECR payment {PaymentId}", paymentId);
                return Error.Internal($"Error recording payment: {ex.Message}");
            }
        }

        public async Task<Result<StatutoryPayment>> UpdateTrrnAsync(Guid paymentId, string trrn)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Statutory payment with ID {paymentId} not found");

                if (payment.PaymentType != "PF")
                    return Error.Validation("Payment is not a PF ECR payment");

                payment.Trrn = trrn;
                payment.Status = "filed";
                payment.FiledAt = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await _statutoryPaymentRepository.UpdateAsync(payment);

                _logger.LogInformation(
                    "Updated TRRN {Trrn} for payment {PaymentId}",
                    trrn, paymentId);

                return Result<StatutoryPayment>.Success(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating TRRN for payment {PaymentId}", paymentId);
                return Error.Internal($"Error updating TRRN: {ex.Message}");
            }
        }

        // ==================== Retrieval ====================

        public async Task<Result<(IEnumerable<PfEcrListDto> Items, int TotalCount)>> GetPagedAsync(
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
                    "Getting paged PF ECRs for Company {CompanyId}, Page {Page}, Size {Size}",
                    companyId, pageNumber, pageSize);

                var company = await _companyRepository.GetByIdAsync(companyId);
                if (company == null)
                    return Error.NotFound($"Company {companyId} not found");

                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(companyId);
                var establishmentCode = statutoryConfig?.PfRegistrationNumber ?? string.Empty;

                // Get PF payments for the company
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

                // Filter to PF payments only
                var pfPayments = payments
                    .Where(p => p.PaymentType == "PF")
                    .AsQueryable();

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    pfPayments = pfPayments.Where(p => p.Status == status);
                }

                // Apply search filter (search by TRRN or reference)
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    pfPayments = pfPayments.Where(p =>
                        (p.ReferenceNumber != null && p.ReferenceNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.ReceiptNumber != null && p.ReceiptNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
                }

                var totalCount = pfPayments.Count();

                // Order by period (most recent first) and apply pagination
                var pagedPayments = pfPayments
                    .OrderByDescending(p => p.PeriodYear)
                    .ThenByDescending(p => p.PeriodMonth)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Map to DTOs
                var items = pagedPayments.Select(p => new PfEcrListDto
                {
                    Id = p.Id,
                    CompanyId = p.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    EstablishmentCode = establishmentCode,
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    FinancialYear = p.FinancialYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    MemberCount = 0, // Would need to query payroll transactions for actual count
                    TotalEmployeeContribution = p.PrincipalAmount * 0.5m, // Approximation
                    TotalEmployerContribution = p.PrincipalAmount * 0.5m,
                    TotalAdminCharges = 0,
                    TotalContribution = p.PrincipalAmount,
                    Interest = p.InterestAmount,
                    Damages = p.PenaltyAmount,
                    TotalAmount = p.TotalAmount,
                    DueDate = p.DueDate,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status,
                    Trrn = p.ReceiptNumber,
                    CreatedAt = p.CreatedAt
                });

                return Result<(IEnumerable<PfEcrListDto> Items, int TotalCount)>.Success((items, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paged PF ECRs for Company {CompanyId}", companyId);
                return Error.Internal($"Error getting ECRs: {ex.Message}");
            }
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.Today;
            return GetFinancialYear(today.Month, today.Year);
        }

        public async Task<Result<IEnumerable<PendingEcrDto>>> GetPendingEcrAsync(
            Guid companyId,
            string? financialYear = null)
        {
            try
            {
                var pendingPayments = await _statutoryPaymentRepository.GetPendingByCompanyAsync(
                    companyId, "PF", financialYear);

                var pendingDtos = pendingPayments.Select(p => new PendingEcrDto
                {
                    StatutoryPaymentId = p.Id,
                    PeriodMonth = p.PeriodMonth,
                    PeriodYear = p.PeriodYear,
                    MonthName = new DateTime(p.PeriodYear, p.PeriodMonth, 1).ToString("MMM yyyy"),
                    TotalContribution = p.PrincipalAmount,
                    InterestAmount = p.InterestAmount,
                    DamagesAmount = p.PenaltyAmount,
                    TotalAmount = p.TotalAmount,
                    DueDate = p.DueDate,
                    IsOverdue = p.IsOverdue,
                    DaysOverdue = p.DaysOverdue,
                    Status = p.Status
                }).ToList();

                return Result<IEnumerable<PendingEcrDto>>.Success(pendingDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending ECRs for company {CompanyId}", companyId);
                return Error.Internal($"Error getting pending ECRs: {ex.Message}");
            }
        }

        public async Task<Result<IEnumerable<FiledEcrDto>>> GetFiledEcrAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var filedPayments = await _statutoryPaymentRepository.GetPaidByCompanyAsync(
                    companyId, "PF", financialYear);

                var filedDtos = filedPayments.Select(p => new FiledEcrDto
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
                    Trrn = p.Trrn,
                    Status = p.Status
                }).ToList();

                return Result<IEnumerable<FiledEcrDto>>.Success(filedDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filed ECRs for company {CompanyId}", companyId);
                return Error.Internal($"Error getting filed ECRs: {ex.Message}");
            }
        }

        public async Task<Result<PfEcrDetailDto>> GetEcrByIdAsync(Guid paymentId)
        {
            try
            {
                var payment = await _statutoryPaymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                    return Error.NotFound($"Payment with ID {paymentId} not found");

                if (payment.PaymentType != "PF")
                    return Error.Validation("Payment is not a PF ECR payment");

                var company = await _companyRepository.GetByIdAsync(payment.CompanyId);
                var statutoryConfig = await _statutoryConfigRepository.GetByCompanyIdAsync(payment.CompanyId);

                // Get ECR data for member records
                var ecrResult = await GenerateEcrAsync(
                    payment.CompanyId, payment.PeriodMonth, payment.PeriodYear);

                var detail = new PfEcrDetailDto
                {
                    Id = payment.Id,
                    CompanyId = payment.CompanyId,
                    CompanyName = company?.Name ?? string.Empty,
                    EstablishmentCode = statutoryConfig?.PfRegistrationNumber ?? string.Empty,
                    PeriodMonth = payment.PeriodMonth,
                    PeriodYear = payment.PeriodYear,
                    FinancialYear = payment.FinancialYear,
                    TotalContribution = payment.PrincipalAmount,
                    InterestAmount = payment.InterestAmount,
                    DamagesAmount = payment.PenaltyAmount,
                    TotalPaid = payment.TotalAmount,
                    DueDate = payment.DueDate,
                    PaymentDate = payment.PaymentDate,
                    Status = payment.Status,
                    Trrn = payment.Trrn,
                    PaymentMode = payment.PaymentMode,
                    BankName = payment.BankName,
                    BankReference = payment.BankReference,
                    MemberRecords = ecrResult.IsSuccess ? ecrResult.Value.MemberRecords : new List<PfEcrMemberRecord>()
                };

                // Populate totals from ECR data if available
                if (ecrResult.IsSuccess)
                {
                    detail.MemberCount = ecrResult.Value.MemberCount;
                    detail.TotalEpfWages = ecrResult.Value.TotalEpfWages;
                    detail.TotalEpsWages = ecrResult.Value.TotalEpsWages;
                    detail.TotalEmployeeContribution = ecrResult.Value.TotalEmployeeContribution;
                    detail.TotalEmployerContribution = ecrResult.Value.TotalEmployerEpfContribution +
                                                        ecrResult.Value.TotalEmployerEpsContribution;
                    detail.TotalAdminCharges = ecrResult.Value.TotalAdminCharges;
                    detail.TotalEdliCharges = ecrResult.Value.TotalEdliCharges;
                }

                return Result<PfEcrDetailDto>.Success(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ECR details for payment {PaymentId}", paymentId);
                return Error.Internal($"Error getting ECR details: {ex.Message}");
            }
        }

        // ==================== Utility ====================

        public DateOnly GetDueDate(int periodMonth, int periodYear)
        {
            // PF due date is 15th of following month
            var followingMonth = periodMonth == 12 ? 1 : periodMonth + 1;
            var followingYear = periodMonth == 12 ? periodYear + 1 : periodYear;
            return new DateOnly(followingYear, followingMonth, 15);
        }

        public decimal CalculateInterest(decimal pfAmount, DateOnly dueDate, DateOnly paymentDate)
        {
            if (paymentDate <= dueDate)
                return 0;

            // Interest rate: 1% per month (simple interest) under Section 7Q
            var daysLate = paymentDate.DayNumber - dueDate.DayNumber;
            var monthsLate = (int)Math.Ceiling(daysLate / 30.0);

            return Math.Round(pfAmount * monthsLate * 1m / 100, 0, MidpointRounding.AwayFromZero);
        }

        public decimal CalculateDamages(decimal pfAmount, DateOnly dueDate, DateOnly paymentDate)
        {
            if (paymentDate <= dueDate)
                return 0;

            // Damages under Section 14B based on delay period
            var daysLate = paymentDate.DayNumber - dueDate.DayNumber;

            // Damage rates:
            // Up to 2 months: 5%
            // 2-4 months: 10%
            // 4-6 months: 15%
            // Above 6 months: 25%
            var damageRate = daysLate switch
            {
                <= 60 => 5m,
                <= 120 => 10m,
                <= 180 => 15m,
                _ => 25m
            };

            return Math.Round(pfAmount * damageRate / 100, 0, MidpointRounding.AwayFromZero);
        }

        public async Task<Result<PfEcrSummary>> GetMonthlySummaryAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var (startYear, endYear) = ParseFinancialYear(financialYear);

                var summary = new PfEcrSummary
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear
                };

                // Get all PF payments for the financial year
                var payments = await _statutoryPaymentRepository.GetByCompanyAndFyAsync(
                    companyId, "PF", financialYear);
                var paymentDict = payments.ToDictionary(
                    p => (p.PeriodMonth, p.PeriodYear),
                    p => p);

                // Generate status for each month (April to March)
                for (int i = 0; i < 12; i++)
                {
                    var month = (i + 4 - 1) % 12 + 1; // April = 4, May = 5, ..., March = 3
                    var year = month >= 4 ? startYear : endYear;

                    var monthlyStatus = new MonthlyPfStatus
                    {
                        Month = month,
                        Year = year,
                        MonthName = new DateTime(year, month, 1).ToString("MMM yyyy"),
                        DueDate = GetDueDate(month, year)
                    };

                    // Get deducted PF from payroll
                    var transactions = await _payrollTransactionRepository.GetByCompanyAndPeriodAsync(
                        companyId, month, year);
                    monthlyStatus.PfDeducted = transactions.Sum(t => t.PfEmployee + t.PfEmployer);
                    monthlyStatus.MemberCount = transactions.Count(t => t.PfEmployee > 0);

                    // Get deposited PF from statutory payments
                    if (paymentDict.TryGetValue((month, year), out var payment))
                    {
                        monthlyStatus.StatutoryPaymentId = payment.Id;
                        monthlyStatus.PfDeposited = payment.TotalAmount;
                        monthlyStatus.PaymentDate = payment.PaymentDate;
                        monthlyStatus.Trrn = payment.Trrn;
                        monthlyStatus.Status = payment.Status;
                    }
                    else
                    {
                        monthlyStatus.Status = DateOnly.FromDateTime(DateTime.Today) > monthlyStatus.DueDate
                            ? "overdue"
                            : "pending";
                    }

                    monthlyStatus.Variance = monthlyStatus.PfDeducted - monthlyStatus.PfDeposited;

                    summary.MonthlyStatus.Add(monthlyStatus);
                }

                // Calculate totals
                summary.TotalPfDeducted = summary.MonthlyStatus.Sum(m => m.PfDeducted);
                summary.TotalPfDeposited = summary.MonthlyStatus.Sum(m => m.PfDeposited);
                summary.TotalVariance = summary.TotalPfDeducted - summary.TotalPfDeposited;
                summary.PaidCount = summary.MonthlyStatus.Count(m => m.Status == "paid" || m.Status == "filed");
                summary.PendingCount = summary.MonthlyStatus.Count(m => m.Status == "pending");
                summary.OverdueCount = summary.MonthlyStatus.Count(m => m.Status == "overdue");

                return Result<PfEcrSummary>.Success(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PF ECR summary for company {CompanyId}", companyId);
                return Error.Internal($"Error getting summary: {ex.Message}");
            }
        }

        public async Task<Result<PfReconciliation>> ReconcileAsync(
            Guid companyId,
            string financialYear)
        {
            try
            {
                var summaryResult = await GetMonthlySummaryAsync(companyId, financialYear);
                if (summaryResult.IsFailure)
                    return Result<PfReconciliation>.Failure(summaryResult.Error!);

                var summary = summaryResult.Value;

                var reconciliation = new PfReconciliation
                {
                    CompanyId = companyId,
                    FinancialYear = financialYear,
                    TotalPfDeducted = summary.TotalPfDeducted,
                    TotalPfDeposited = summary.TotalPfDeposited,
                    Variance = summary.TotalVariance,
                    MonthlyReconciliation = summary.MonthlyStatus.Select(m => new PfReconciliationItem
                    {
                        Month = m.Month,
                        Year = m.Year,
                        MemberCount = m.MemberCount,
                        Deducted = m.PfDeducted,
                        Deposited = m.PfDeposited,
                        Variance = m.Variance,
                        Status = m.Variance == 0 ? "reconciled" : "mismatch"
                    }).ToList()
                };

                // Identify mismatches
                foreach (var item in reconciliation.MonthlyReconciliation.Where(m => m.Variance != 0))
                {
                    reconciliation.Mismatches.Add(new PfReconciliationMismatch
                    {
                        MismatchType = item.Variance > 0 ? "under_deposited" : "over_deposited",
                        Description = $"Variance of ₹{Math.Abs(item.Variance):N0} for {new DateTime(item.Year, item.Month, 1):MMM yyyy}",
                        Month = item.Month,
                        Amount = Math.Abs(item.Variance),
                        SuggestedAction = item.Variance > 0
                            ? "File supplementary ECR for the shortfall"
                            : "Verify calculations and adjust in next month"
                    });
                }

                return Result<PfReconciliation>.Success(reconciliation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling PF for company {CompanyId}", companyId);
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

        private static decimal GetEpsContribution(PayrollTransaction transaction)
        {
            // EPS is 8.33% of PF wage base capped at ₹15,000
            var epsWageBase = Math.Min(transaction.BasicEarned + transaction.DaEarned, 15000);
            return Math.Round(epsWageBase * 8.33m / 100, 0, MidpointRounding.AwayFromZero);
        }

        private static void ValidateEcrData(PfEcrData ecrData, PfEcrPreview preview)
        {
            // Check for missing UANs
            var missingUan = ecrData.MemberRecords.Where(m => string.IsNullOrWhiteSpace(m.Uan)).ToList();
            if (missingUan.Any())
            {
                preview.HasWarnings = true;
                foreach (var member in missingUan)
                {
                    preview.Warnings.Add($"Missing UAN for employee: {member.MemberName}");
                    preview.ValidationErrors.Add(new EcrValidationError
                    {
                        EmployeeId = member.EmployeeId,
                        EmployeeName = member.MemberName,
                        ErrorCode = "MISSING_UAN",
                        ErrorMessage = "UAN is required for EPFO filing",
                        Severity = "error"
                    });
                }
            }

            // Check for zero contributions
            var zeroContribution = ecrData.MemberRecords.Where(m => m.TotalContribution == 0).ToList();
            if (zeroContribution.Any())
            {
                preview.HasWarnings = true;
                foreach (var member in zeroContribution)
                {
                    preview.Warnings.Add($"Zero contribution for employee: {member.MemberName}");
                    preview.ValidationErrors.Add(new EcrValidationError
                    {
                        EmployeeId = member.EmployeeId,
                        EmployeeName = member.MemberName,
                        ErrorCode = "ZERO_CONTRIBUTION",
                        ErrorMessage = "Employee has zero PF contribution",
                        Severity = "warning"
                    });
                }
            }

            // Check if overdue
            if (ecrData.IsOverdue)
            {
                preview.HasWarnings = true;
                preview.Warnings.Add($"Payment is overdue by {ecrData.DaysOverdue} days. Interest and damages will apply.");
            }
        }

        private static string GenerateEcrTextContent(PfEcrData ecrData)
        {
            // EPFO ECR file format (pipe-delimited text file)
            var sb = new StringBuilder();

            // Header line is not required for ECR - EPFO portal handles it
            // Each line is a member record in the format:
            // UAN#Member Name#Gross Wages#EPF Wages#EPS Wages#EDLI Wages#
            // EPF Contribution(Employee)#EPS Contribution#EPF Contribution(Employer)#
            // NCP Days#Refund of Advances

            foreach (var member in ecrData.MemberRecords)
            {
                // Format: UAN#Name#GrossWages#EPFWages#EPSWages#EDLIWages#EEContribution#EPSContribution#ERContribution#NCPDays#RefundAdvances
                var line = string.Join("#",
                    member.Uan,
                    CleanName(member.MemberName),
                    member.GrossWages.ToString("F0"),
                    member.EpfWages.ToString("F0"),
                    member.EpsWages.ToString("F0"),
                    member.EdliWages.ToString("F0"),
                    member.EmployeeEpfContribution.ToString("F0"),
                    member.EmployerEpsContribution.ToString("F0"),
                    member.EmployerEpfContribution.ToString("F0"),
                    member.NcpDays.ToString(),
                    "0" // Refund of advances - typically 0
                );

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        private static string CleanName(string name)
        {
            // Remove special characters that might break the file format
            return name?.Replace("#", " ").Replace("|", " ").Trim() ?? string.Empty;
        }
    }
}
