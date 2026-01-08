using Core.Interfaces;
using Core.Interfaces.Payroll;
using Core.Interfaces.Tax;

namespace Application.Services.Tax
{
    /// <summary>
    /// Service for TDS Return preparation (Form 26Q and Form 24Q).
    /// Aggregates TDS data from contractor payments, salary transactions, etc.
    /// Provides validation and filing support.
    /// </summary>
    public class TdsReturnService : ITdsReturnService
    {
        private readonly IContractorPaymentRepository _contractorPaymentRepo;
        private readonly IPayrollTransactionRepository _payrollTransactionRepo;
        private readonly IEmployeesRepository _employeesRepo;
        private readonly ICompaniesRepository _companiesRepo;
        private readonly ILowerDeductionCertificateRepository _ldcRepo;
        private readonly IEmployeeTaxDeclarationRepository _taxDeclarationRepo;

        // TDS Section mapping for Form 26Q
        private static readonly Dictionary<string, string> TdsSectionNames = new()
        {
            { "194A", "Interest other than securities" },
            { "194C", "Contractors" },
            { "194H", "Commission/Brokerage" },
            { "194I(a)", "Rent - Plant & Machinery" },
            { "194I(b)", "Rent - Land/Building" },
            { "194IB", "Rent by Individual/HUF" },
            { "194J", "Professional/Technical fees" },
            { "194M", "Contractor by Individual/HUF" },
            { "194N", "Cash withdrawal" },
            { "194O", "E-commerce" },
            { "194Q", "Purchase of goods" },
            { "195", "Non-resident payments" }
        };

        // Quarter month ranges (Indian FY: Apr-Mar)
        private static readonly Dictionary<string, (int StartMonth, int EndMonth, int[] Months)> QuarterMonths = new()
        {
            { "Q1", (4, 6, new[] { 4, 5, 6 }) },
            { "Q2", (7, 9, new[] { 7, 8, 9 }) },
            { "Q3", (10, 12, new[] { 10, 11, 12 }) },
            { "Q4", (1, 3, new[] { 1, 2, 3 }) }
        };

        public TdsReturnService(
            IContractorPaymentRepository contractorPaymentRepo,
            IPayrollTransactionRepository payrollTransactionRepo,
            IEmployeesRepository employeesRepo,
            ICompaniesRepository companiesRepo,
            ILowerDeductionCertificateRepository ldcRepo,
            IEmployeeTaxDeclarationRepository taxDeclarationRepo)
        {
            _contractorPaymentRepo = contractorPaymentRepo;
            _payrollTransactionRepo = payrollTransactionRepo;
            _employeesRepo = employeesRepo;
            _companiesRepo = companiesRepo;
            _ldcRepo = ldcRepo;
            _taxDeclarationRepo = taxDeclarationRepo;
        }

        // ==================== Form 26Q (Non-Salary) ====================

        public async Task<Form26QData> GenerateForm26QAsync(Guid companyId, string financialYear, string quarter)
        {
            var company = await _companiesRepo.GetByIdAsync(companyId);
            if (company == null)
                throw new ArgumentException($"Company {companyId} not found");

            var (startYear, endYear) = ParseFinancialYear(financialYear);
            var quarterInfo = QuarterMonths[quarter];
            var quarterYear = quarter == "Q4" ? endYear : startYear;

            // Get contractor payments for the quarter
            var allPayments = await _contractorPaymentRepo.GetByCompanyIdAsync(companyId);
            var quarterPayments = allPayments.Where(p =>
                IsInQuarter(p.PaymentMonth, p.PaymentYear, quarterInfo.Months, quarterYear)).ToList();

            // Build Form 26Q data
            var data = new Form26QData
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                Quarter = quarter,
                Deductor = BuildDeductorDetails(company),
                ResponsiblePerson = new ResponsiblePersonDetails(), // TODO: Get from company settings
                Challans = new List<TdsChallanDetail>(), // TODO: Get from challan repository
                DeducteeRecords = new List<Form26QDeducteeRecord>()
            };

            int serialNo = 1;
            foreach (var payment in quarterPayments.Where(p => p.TdsAmount > 0))
            {
                // ContractorPayment now links to parties table - PAN and Name stored on payment
                var record = new Form26QDeducteeRecord
                {
                    SerialNumber = serialNo++,
                    DeducteePan = payment.ContractorPan ?? "PANAPPLIED",
                    DeducteeName = payment.PartyName ?? "Unknown",
                    TdsSection = payment.TdsSection ?? "194J",
                    PaymentDate = DateOnly.FromDateTime(new DateTime(payment.PaymentYear, payment.PaymentMonth, 1)),
                    GrossAmount = payment.GrossAmount,
                    TdsRate = payment.TdsRate,
                    TdsAmount = payment.TdsAmount,
                    SourceType = "contractor_payment",
                    SourceId = payment.Id
                };

                // Check for LDC
                if (!string.IsNullOrEmpty(payment.ContractorPan))
                {
                    var ldc = await _ldcRepo.GetValidCertificateAsync(
                        companyId,
                        payment.ContractorPan,
                        payment.TdsSection ?? "194J",
                        DateOnly.FromDateTime(new DateTime(payment.PaymentYear, payment.PaymentMonth, 1)));

                    if (ldc != null)
                    {
                        record.ReasonForLowerDeduction = ldc.CertificateType == "nil" ? "A" : "B";
                        record.CertificateNumber = ldc.CertificateNumber;
                    }
                }

                data.DeducteeRecords.Add(record);
            }

            // Calculate totals
            data.TotalTdsDeducted = data.DeducteeRecords.Sum(r => r.TdsAmount);
            data.TotalDeductees = data.DeducteeRecords.Select(r => r.DeducteePan).Distinct().Count();

            return data;
        }

        public async Task<TdsReturnValidationResult> ValidateForm26QAsync(Guid companyId, string financialYear, string quarter)
        {
            var data = await GenerateForm26QAsync(companyId, financialYear, quarter);

            var result = new TdsReturnValidationResult
            {
                FormType = "26Q",
                FinancialYear = financialYear,
                Quarter = quarter,
                Errors = new List<ValidationError>(),
                Warnings = new List<ValidationWarning>()
            };

            // Validate deductor TAN
            if (string.IsNullOrEmpty(data.Deductor.Tan))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "MISSING_TAN",
                    Message = "Deductor TAN is mandatory",
                    Field = "Deductor.Tan",
                    Severity = "error"
                });
            }

            // Validate deductee records
            foreach (var record in data.DeducteeRecords)
            {
                // Check PAN
                if (string.IsNullOrEmpty(record.DeducteePan) || record.DeducteePan == "PANAPPLIED")
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "MISSING_PAN",
                        Message = $"PAN missing for {record.DeducteeName}. Higher TDS rate (20%) should apply.",
                        RecordIdentifier = record.SerialNumber.ToString()
                    });
                }

                // Check TDS rate
                if (record.TdsRate == 0 && record.TdsAmount == 0)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "ZERO_TDS",
                        Message = $"Zero TDS for {record.DeducteeName}. Verify if exempt or lower rate applies.",
                        RecordIdentifier = record.SerialNumber.ToString()
                    });
                }
            }

            // Calculate summary
            result.Summary = new ValidationSummary
            {
                TotalRecords = data.DeducteeRecords.Count,
                ValidRecords = data.DeducteeRecords.Count - result.Errors.Count(e => e.Severity == "error"),
                InvalidRecords = result.Errors.Count(e => e.Severity == "error"),
                RecordsWithWarnings = result.Warnings.Count,
                TotalTdsDeducted = data.TotalTdsDeducted,
                TotalTdsDeposited = data.TotalTdsDeposited,
                Variance = data.TotalTdsDeducted - data.TotalTdsDeposited
            };

            result.IsValid = !result.Errors.Any();

            return result;
        }

        public async Task<Form26QSummary> GetForm26QSummaryAsync(Guid companyId, string financialYear, string quarter)
        {
            var data = await GenerateForm26QAsync(companyId, financialYear, quarter);

            var summary = new Form26QSummary
            {
                FinancialYear = financialYear,
                Quarter = quarter,
                TotalGrossAmount = data.DeducteeRecords.Sum(r => r.GrossAmount),
                TotalTdsDeducted = data.TotalTdsDeducted,
                TotalTdsDeposited = data.TotalTdsDeposited,
                Variance = data.TotalTdsDeducted - data.TotalTdsDeposited,
                TotalTransactions = data.DeducteeRecords.Count,
                UniqueDeductees = data.TotalDeductees
            };

            // Section breakdown
            summary.SectionBreakdown = data.DeducteeRecords
                .GroupBy(r => r.TdsSection)
                .Select(g => new TdsSectionSummary26Q
                {
                    SectionCode = g.Key,
                    SectionName = TdsSectionNames.GetValueOrDefault(g.Key, g.Key),
                    GrossAmount = g.Sum(r => r.GrossAmount),
                    TdsAmount = g.Sum(r => r.TdsAmount),
                    TransactionCount = g.Count(),
                    DeducteeCount = g.Select(r => r.DeducteePan).Distinct().Count()
                })
                .ToList();

            // Month breakdown
            var quarterInfo = QuarterMonths[quarter];
            var (startYear, endYear) = ParseFinancialYear(financialYear);
            var quarterYear = quarter == "Q4" ? endYear : startYear;

            summary.MonthBreakdown = quarterInfo.Months
                .Select(m => new TdsMonthSummary
                {
                    Month = m,
                    Year = quarterYear,
                    MonthName = new DateTime(quarterYear, m, 1).ToString("MMM yyyy"),
                    GrossAmount = data.DeducteeRecords
                        .Where(r => r.PaymentDate.Month == m && r.PaymentDate.Year == quarterYear)
                        .Sum(r => r.GrossAmount),
                    TdsDeducted = data.DeducteeRecords
                        .Where(r => r.PaymentDate.Month == m && r.PaymentDate.Year == quarterYear)
                        .Sum(r => r.TdsAmount),
                    TransactionCount = data.DeducteeRecords
                        .Count(r => r.PaymentDate.Month == m && r.PaymentDate.Year == quarterYear)
                })
                .ToList();

            return summary;
        }

        // ==================== Form 24Q (Salary) ====================

        public async Task<Form24QData> GenerateForm24QAsync(Guid companyId, string financialYear, string quarter)
        {
            var company = await _companiesRepo.GetByIdAsync(companyId);
            if (company == null)
                throw new ArgumentException($"Company {companyId} not found");

            var (startYear, endYear) = ParseFinancialYear(financialYear);
            var quarterInfo = QuarterMonths[quarter];
            var quarterYear = quarter == "Q4" ? endYear : startYear;

            // Get payroll transactions for the quarter (filter by employee's company)
            var allTransactions = await _payrollTransactionRepo.GetAllAsync();
            var allEmployees = await _employeesRepo.GetAllAsync();
            var companyEmployeeIds = allEmployees.Where(e => e.CompanyId == companyId).Select(e => e.Id).ToHashSet();
            var quarterTransactions = allTransactions.Where(t =>
                companyEmployeeIds.Contains(t.EmployeeId) &&
                IsInQuarter(t.PayrollMonth, t.PayrollYear, quarterInfo.Months, quarterYear) &&
                t.PayrollType == "employee").ToList();

            // Build Form 24Q data
            var data = new Form24QData
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                Quarter = quarter,
                Deductor = BuildDeductorDetails(company),
                ResponsiblePerson = new ResponsiblePersonDetails(),
                Challans = new List<TdsChallanDetail>(),
                EmployeeRecords = new List<Form24QEmployeeRecord>()
            };

            // Group by employee
            var employeeGroups = quarterTransactions.GroupBy(t => t.EmployeeId);
            int serialNo = 1;

            foreach (var group in employeeGroups)
            {
                var employee = await _employeesRepo.GetByIdAsync(group.Key);
                if (employee == null) continue;

                var record = new Form24QEmployeeRecord
                {
                    SerialNumber = serialNo++,
                    EmployeeId = group.Key,
                    EmployeePan = employee.PanNumber ?? "PANAPPLIED",
                    EmployeeName = employee.EmployeeName,
                    EmployeeCode = employee.EmployeeId ?? "",
                    DateOfJoining = employee.HireDate.HasValue ? DateOnly.FromDateTime(employee.HireDate.Value) : DateOnly.MinValue,
                    DateOfLeaving = null, // Not tracked in current entity
                    GrossSalary = group.Sum(t => t.GrossEarnings),
                    TdsDeducted = group.Sum(t => t.TdsDeducted),
                    MonthlyDetails = new List<MonthlySalaryTds>()
                };

                // Monthly breakdown
                foreach (var month in quarterInfo.Months)
                {
                    var monthTransactions = group.Where(t => t.PayrollMonth == month && t.PayrollYear == quarterYear);
                    record.MonthlyDetails.Add(new MonthlySalaryTds
                    {
                        Month = month,
                        Year = quarterYear,
                        GrossSalary = monthTransactions.Sum(t => t.GrossEarnings),
                        TdsDeducted = monthTransactions.Sum(t => t.TdsDeducted)
                    });
                }

                data.EmployeeRecords.Add(record);
            }

            // Calculate totals
            data.TotalSalary = data.EmployeeRecords.Sum(r => r.GrossSalary);
            data.TotalTdsDeducted = data.EmployeeRecords.Sum(r => r.TdsDeducted);
            data.TotalEmployees = data.EmployeeRecords.Count;

            return data;
        }

        public async Task<TdsReturnValidationResult> ValidateForm24QAsync(Guid companyId, string financialYear, string quarter)
        {
            var data = await GenerateForm24QAsync(companyId, financialYear, quarter);

            var result = new TdsReturnValidationResult
            {
                FormType = "24Q",
                FinancialYear = financialYear,
                Quarter = quarter,
                Errors = new List<ValidationError>(),
                Warnings = new List<ValidationWarning>()
            };

            // Validate deductor TAN
            if (string.IsNullOrEmpty(data.Deductor.Tan))
            {
                result.Errors.Add(new ValidationError
                {
                    Code = "MISSING_TAN",
                    Message = "Deductor TAN is mandatory",
                    Field = "Deductor.Tan",
                    Severity = "error"
                });
            }

            // Validate employee records
            foreach (var record in data.EmployeeRecords)
            {
                if (string.IsNullOrEmpty(record.EmployeePan) || record.EmployeePan == "PANAPPLIED")
                {
                    result.Errors.Add(new ValidationError
                    {
                        Code = "MISSING_PAN",
                        Message = $"PAN missing for employee {record.EmployeeName}",
                        RecordIdentifier = record.EmployeeCode,
                        Severity = "error"
                    });
                }

                if (record.DateOfJoining == DateOnly.MinValue)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Code = "MISSING_DOJ",
                        Message = $"Date of joining missing for {record.EmployeeName}",
                        RecordIdentifier = record.EmployeeCode
                    });
                }
            }

            result.Summary = new ValidationSummary
            {
                TotalRecords = data.EmployeeRecords.Count,
                ValidRecords = data.EmployeeRecords.Count - result.Errors.Count,
                InvalidRecords = result.Errors.Count,
                RecordsWithWarnings = result.Warnings.Count,
                TotalTdsDeducted = data.TotalTdsDeducted,
                TotalTdsDeposited = data.TotalTdsDeposited,
                Variance = data.TotalTdsDeducted - data.TotalTdsDeposited
            };

            result.IsValid = !result.Errors.Any();

            return result;
        }

        public async Task<Form24QSummary> GetForm24QSummaryAsync(Guid companyId, string financialYear, string quarter)
        {
            var data = await GenerateForm24QAsync(companyId, financialYear, quarter);

            var summary = new Form24QSummary
            {
                FinancialYear = financialYear,
                Quarter = quarter,
                TotalGrossSalary = data.TotalSalary,
                TotalTdsDeducted = data.TotalTdsDeducted,
                TotalTdsDeposited = data.TotalTdsDeposited,
                Variance = data.TotalTdsDeducted - data.TotalTdsDeposited,
                TotalEmployees = data.TotalEmployees,
                EmployeesWithTds = data.EmployeeRecords.Count(r => r.TdsDeducted > 0)
            };

            // Month breakdown
            var quarterInfo = QuarterMonths[quarter];
            var (startYear, endYear) = ParseFinancialYear(financialYear);
            var quarterYear = quarter == "Q4" ? endYear : startYear;

            summary.MonthBreakdown = quarterInfo.Months
                .Select(m => new TdsMonthSummary
                {
                    Month = m,
                    Year = quarterYear,
                    MonthName = new DateTime(quarterYear, m, 1).ToString("MMM yyyy"),
                    GrossAmount = data.EmployeeRecords
                        .SelectMany(r => r.MonthlyDetails)
                        .Where(md => md.Month == m && md.Year == quarterYear)
                        .Sum(md => md.GrossSalary),
                    TdsDeducted = data.EmployeeRecords
                        .SelectMany(r => r.MonthlyDetails)
                        .Where(md => md.Month == m && md.Year == quarterYear)
                        .Sum(md => md.TdsDeducted),
                    TransactionCount = data.EmployeeRecords.Count
                })
                .ToList();

            return summary;
        }

        public async Task<Form24QAnnexureII> GenerateForm24QAnnexureIIAsync(Guid companyId, string financialYear)
        {
            // Annexure II is only for Q4 - contains annual salary details
            var company = await _companiesRepo.GetByIdAsync(companyId);
            if (company == null)
                throw new ArgumentException($"Company {companyId} not found");

            var (startYear, endYear) = ParseFinancialYear(financialYear);

            // Get all salary transactions for the financial year (filter by employee's company)
            var allTransactions = await _payrollTransactionRepo.GetAllAsync();
            var allEmployees = await _employeesRepo.GetAllAsync();
            var companyEmployeeIds = allEmployees.Where(e => e.CompanyId == companyId).Select(e => e.Id).ToHashSet();
            var fyTransactions = allTransactions.Where(t =>
                companyEmployeeIds.Contains(t.EmployeeId) &&
                t.PayrollType == "employee" &&
                IsInFinancialYear(t.PayrollMonth, t.PayrollYear, startYear, endYear)).ToList();

            var annexure = new Form24QAnnexureII
            {
                CompanyId = companyId,
                FinancialYear = financialYear,
                EmployeeRecords = new List<AnnexureIIEmployeeRecord>()
            };

            // Group by employee
            var employeeGroups = fyTransactions.GroupBy(t => t.EmployeeId);

            foreach (var group in employeeGroups)
            {
                var employee = await _employeesRepo.GetByIdAsync(group.Key);
                if (employee == null) continue;

                var totalGross = group.Sum(t => t.GrossEarnings);
                var totalTds = group.Sum(t => t.TdsDeducted);

                // Get tax declaration for Column 388A data
                var taxDeclaration = await _taxDeclarationRepo.GetByEmployeeAndYearAsync(group.Key, financialYear);

                // Calculate Column 388A totals
                decimal otherTdsInterest = taxDeclaration?.OtherTdsInterest ?? 0;
                decimal otherTdsDividend = taxDeclaration?.OtherTdsDividend ?? 0;
                decimal otherTdsOther = (taxDeclaration?.OtherTdsCommission ?? 0) +
                                        (taxDeclaration?.OtherTdsRent ?? 0) +
                                        (taxDeclaration?.OtherTdsProfessional ?? 0) +
                                        (taxDeclaration?.OtherTdsOthers ?? 0);
                decimal tcsCredit = (taxDeclaration?.TcsForeignRemittance ?? 0) +
                                    (taxDeclaration?.TcsOverseasTour ?? 0) +
                                    (taxDeclaration?.TcsVehiclePurchase ?? 0) +
                                    (taxDeclaration?.TcsOthers ?? 0);
                decimal totalColumn388A = otherTdsInterest + otherTdsDividend + otherTdsOther + tcsCredit;

                var record = new AnnexureIIEmployeeRecord
                {
                    EmployeeId = group.Key,
                    EmployeePan = employee.PanNumber ?? "PANAPPLIED",
                    EmployeeName = employee.EmployeeName,
                    EmployeeCode = employee.EmployeeId ?? "",
                    TaxRegime = taxDeclaration?.TaxRegime ?? "new",

                    // Income
                    GrossSalary = totalGross,
                    TotalSalary = totalGross,
                    GrossTotal = totalGross,
                    OtherIncome = taxDeclaration?.OtherIncomeAnnual ?? 0,

                    // Deductions (simplified - actual should come from declarations)
                    StandardDeduction = Math.Min(75000, totalGross),

                    // Tax
                    TaxableIncome = Math.Max(0, totalGross - 75000),
                    TdsDeducted = totalTds,

                    // Column 388A - Other TDS/TCS Credits (per CBDT Feb 2025)
                    OtherTdsInterest = otherTdsInterest,
                    OtherTdsDividend = otherTdsDividend,
                    OtherTdsOther = otherTdsOther,
                    TcsCredit = tcsCredit,
                    TotalColumn388A = totalColumn388A,
                    NetTaxPayable = Math.Max(0, totalTds - totalColumn388A)
                };

                annexure.EmployeeRecords.Add(record);
            }

            return annexure;
        }

        // ==================== Challan Reconciliation ====================

        public Task<IEnumerable<TdsChallanDetail>> GetChallanDetailsAsync(
            Guid companyId,
            string financialYear,
            string quarter,
            string? formType = null)
        {
            // TODO: Implement challan repository integration
            return Task.FromResult<IEnumerable<TdsChallanDetail>>(new List<TdsChallanDetail>());
        }

        public async Task<ChallanReconciliationResult> ReconcileChallansAsync(
            Guid companyId,
            string financialYear,
            string quarter)
        {
            var form26Q = await GenerateForm26QAsync(companyId, financialYear, quarter);
            var form24Q = await GenerateForm24QAsync(companyId, financialYear, quarter);

            var totalDeducted = form26Q.TotalTdsDeducted + form24Q.TotalTdsDeducted;
            var challans = await GetChallanDetailsAsync(companyId, financialYear, quarter);
            var totalDeposited = challans.Sum(c => c.TdsAmount);

            return new ChallanReconciliationResult
            {
                IsReconciled = Math.Abs(totalDeducted - totalDeposited) < 1,
                TotalTdsDeducted = totalDeducted,
                TotalTdsDeposited = totalDeposited,
                Variance = totalDeducted - totalDeposited,
                Mismatches = totalDeducted != totalDeposited
                    ? new List<ChallanMismatch>
                    {
                        new()
                        {
                            MismatchType = "AMOUNT_VARIANCE",
                            Description = $"TDS deducted ({totalDeducted:N0}) doesn't match deposited ({totalDeposited:N0})",
                            ExpectedAmount = totalDeducted,
                            ActualAmount = totalDeposited
                        }
                    }
                    : new List<ChallanMismatch>()
            };
        }

        // ==================== Due Date Tracking ====================

        public Task<IEnumerable<TdsReturnDueDate>> GetDueDatesAsync(string financialYear)
        {
            var (startYear, endYear) = ParseFinancialYear(financialYear);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var dueDates = new List<TdsReturnDueDate>
            {
                // Form 26Q due dates
                new() { FormType = "26Q", FinancialYear = financialYear, Quarter = "Q1",
                    DueDate = new DateOnly(startYear, 7, 31) },
                new() { FormType = "26Q", FinancialYear = financialYear, Quarter = "Q2",
                    DueDate = new DateOnly(startYear, 10, 31) },
                new() { FormType = "26Q", FinancialYear = financialYear, Quarter = "Q3",
                    DueDate = new DateOnly(endYear, 1, 31) },
                new() { FormType = "26Q", FinancialYear = financialYear, Quarter = "Q4",
                    DueDate = new DateOnly(endYear, 5, 31) },

                // Form 24Q due dates
                new() { FormType = "24Q", FinancialYear = financialYear, Quarter = "Q1",
                    DueDate = new DateOnly(startYear, 7, 31) },
                new() { FormType = "24Q", FinancialYear = financialYear, Quarter = "Q2",
                    DueDate = new DateOnly(startYear, 10, 31) },
                new() { FormType = "24Q", FinancialYear = financialYear, Quarter = "Q3",
                    DueDate = new DateOnly(endYear, 1, 31) },
                new() { FormType = "24Q", FinancialYear = financialYear, Quarter = "Q4",
                    DueDate = new DateOnly(endYear, 5, 31) }
            };

            foreach (var dd in dueDates)
            {
                dd.IsOverdue = today > dd.DueDate;
                dd.DaysUntilDue = dd.DueDate.DayNumber - today.DayNumber;
            }

            return Task.FromResult<IEnumerable<TdsReturnDueDate>>(dueDates);
        }

        public async Task<IEnumerable<PendingTdsReturn>> GetPendingReturnsAsync(Guid companyId)
        {
            var currentFy = GetCurrentFinancialYear();
            var dueDates = await GetDueDatesAsync(currentFy);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var pending = new List<PendingTdsReturn>();

            foreach (var dd in dueDates.Where(d => d.DueDate <= today.AddDays(30)))
            {
                // TODO: Check filing history to see if already filed
                var summary = dd.FormType == "26Q"
                    ? await GetForm26QSummaryAsync(companyId, dd.FinancialYear, dd.Quarter)
                    : null;

                var summary24Q = dd.FormType == "24Q"
                    ? await GetForm24QSummaryAsync(companyId, dd.FinancialYear, dd.Quarter)
                    : null;

                pending.Add(new PendingTdsReturn
                {
                    FormType = dd.FormType,
                    FinancialYear = dd.FinancialYear,
                    Quarter = dd.Quarter,
                    DueDate = dd.DueDate,
                    IsOverdue = dd.IsOverdue,
                    DaysOverdue = dd.IsOverdue ? -dd.DaysUntilDue : 0,
                    EstimatedTdsAmount = summary?.TotalTdsDeducted ?? summary24Q?.TotalTdsDeducted ?? 0,
                    RecordCount = summary?.TotalTransactions ?? summary24Q?.TotalEmployees ?? 0
                });
            }

            return pending;
        }

        // ==================== Filing Status ====================

        public Task MarkReturnFiledAsync(MarkReturnFiledRequest request)
        {
            // TODO: Implement filing history storage
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TdsReturnFilingHistory>> GetFilingHistoryAsync(
            Guid companyId,
            string? financialYear = null)
        {
            // TODO: Implement filing history retrieval
            return Task.FromResult<IEnumerable<TdsReturnFilingHistory>>(new List<TdsReturnFilingHistory>());
        }

        // ==================== Helper Methods ====================

        private DeductorDetails BuildDeductorDetails(Core.Entities.Companies company)
        {
            return new DeductorDetails
            {
                Tan = company.TaxNumber ?? "", // TAN stored in TaxNumber field
                Pan = company.PanNumber ?? "",
                Name = company.Name,
                City = company.City ?? "",
                State = company.State ?? "",
                Pincode = company.ZipCode ?? "",
                Phone = company.Phone ?? "",
                Email = company.Email ?? ""
            };
        }

        private static (int StartYear, int EndYear) ParseFinancialYear(string financialYear)
        {
            // Format: "2024-25"
            var parts = financialYear.Split('-');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid financial year format: {financialYear}. Expected format: '2024-25'");

            var startYear = int.Parse(parts[0]);
            var endYear = startYear + 1;
            return (startYear, endYear);
        }

        private static bool IsInQuarter(int month, int year, int[] quarterMonths, int quarterYear)
        {
            return quarterMonths.Contains(month) && year == quarterYear;
        }

        private static bool IsInFinancialYear(int month, int year, int startYear, int endYear)
        {
            // FY runs Apr to Mar
            if (year == startYear && month >= 4) return true;
            if (year == endYear && month <= 3) return true;
            return false;
        }

        private static string GetCurrentFinancialYear()
        {
            var today = DateTime.Today;
            var year = today.Month >= 4 ? today.Year : today.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }
    }
}
