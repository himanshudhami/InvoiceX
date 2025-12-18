using Application.DTOs.Portal;
using Application.Interfaces;
using Core.Common;
using Core.Entities;
using Core.Entities.Payroll;
using Core.Interfaces;
using Core.Interfaces.Payroll;
using System.Globalization;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for Employee Portal operations.
    /// All operations are scoped to the authenticated employee.
    /// </summary>
    public class EmployeePortalService : IEmployeePortalService
    {
        private readonly IEmployeesRepository _employeesRepository;
        private readonly IEmployeeSalaryTransactionsRepository _payslipRepository;
        private readonly IAssetsRepository _assetsRepository;
        private readonly IEmployeeTaxDeclarationRepository _taxDeclarationRepository;
        private readonly ISubscriptionsRepository _subscriptionsRepository;

        private static readonly string[] MonthNames = new[]
        {
            "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };

        public EmployeePortalService(
            IEmployeesRepository employeesRepository,
            IEmployeeSalaryTransactionsRepository payslipRepository,
            IAssetsRepository assetsRepository,
            IEmployeeTaxDeclarationRepository taxDeclarationRepository,
            ISubscriptionsRepository subscriptionsRepository)
        {
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
            _payslipRepository = payslipRepository ?? throw new ArgumentNullException(nameof(payslipRepository));
            _assetsRepository = assetsRepository ?? throw new ArgumentNullException(nameof(assetsRepository));
            _taxDeclarationRepository = taxDeclarationRepository ?? throw new ArgumentNullException(nameof(taxDeclarationRepository));
            _subscriptionsRepository = subscriptionsRepository ?? throw new ArgumentNullException(nameof(subscriptionsRepository));
        }

        // ==================== Profile ====================

        public async Task<Result<EmployeeProfileDto>> GetMyProfileAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound("Employee not found");

            return Result<EmployeeProfileDto>.Success(MapToProfileDto(employee));
        }

        // ==================== Dashboard ====================

        public async Task<Result<PortalDashboardDto>> GetDashboardAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            if (employee == null)
                return Error.NotFound("Employee not found");

            // Get latest payslip
            var payslips = await _payslipRepository.GetByEmployeeIdAsync(employeeId);
            var latestPayslip = payslips
                .Where(p => p.Status == "paid" || p.Status == "processed")
                .OrderByDescending(p => p.SalaryYear)
                .ThenByDescending(p => p.SalaryMonth)
                .FirstOrDefault();

            // Get assigned assets count
            var assetAssignments = await _assetsRepository.GetAssignmentsByEmployeeAsync(employeeId);
            var activeAssets = assetAssignments.Where(a => a.IsActive).Count();

            // Get active subscriptions count
            var subscriptionAssignments = await _subscriptionsRepository.GetAssignmentsByEmployeeAsync(employeeId);
            var activeSubscriptions = subscriptionAssignments.Where(s => s.RevokedOn == null).Count();

            // Get current tax declaration
            var currentFY = GetCurrentFinancialYear();
            var taxDeclaration = await _taxDeclarationRepository.GetByEmployeeAndYearAsync(employeeId, currentFY);

            var dashboard = new PortalDashboardDto
            {
                Profile = MapToProfileDto(employee),
                LatestPayslip = latestPayslip != null ? MapToPayslipSummary(latestPayslip) : null,
                AssignedAssetsCount = activeAssets,
                ActiveSubscriptionsCount = activeSubscriptions,
                CurrentTaxDeclaration = taxDeclaration != null ? MapToTaxDeclarationSummary(taxDeclaration) : null,
                QuickActions = GetQuickActions(taxDeclaration),
                Notifications = new List<NotificationDto>() // Future: implement notifications
            };

            return Result<PortalDashboardDto>.Success(dashboard);
        }

        // ==================== Payslips ====================

        public async Task<Result<IEnumerable<PayslipSummaryDto>>> GetMyPayslipsAsync(Guid employeeId, int? year = null)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var payslips = await _payslipRepository.GetByEmployeeIdAsync(employeeId);

            if (year.HasValue)
            {
                payslips = payslips.Where(p => p.SalaryYear == year.Value);
            }

            var summaries = payslips
                .OrderByDescending(p => p.SalaryYear)
                .ThenByDescending(p => p.SalaryMonth)
                .Select(MapToPayslipSummary)
                .ToList();

            return Result<IEnumerable<PayslipSummaryDto>>.Success(summaries);
        }

        public async Task<Result<PayslipDetailDto>> GetPayslipDetailAsync(Guid employeeId, Guid payslipId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (payslipId == default)
                return Error.Validation("Payslip ID cannot be empty");

            var payslip = await _payslipRepository.GetByIdAsync(payslipId);
            if (payslip == null)
                return Error.NotFound("Payslip not found");

            // Security check: ensure payslip belongs to the employee
            if (payslip.EmployeeId != employeeId)
                return Error.Forbidden("Access denied to this payslip");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            return Result<PayslipDetailDto>.Success(MapToPayslipDetail(payslip, employee));
        }

        public async Task<Result<PayslipDetailDto>> GetPayslipByMonthAsync(Guid employeeId, int month, int year)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (month < 1 || month > 12)
                return Error.Validation("Month must be between 1 and 12");
            if (year < 2000 || year > 2100)
                return Error.Validation("Invalid year");

            var payslip = await _payslipRepository.GetByEmployeeAndMonthAsync(employeeId, month, year);
            if (payslip == null)
                return Error.NotFound($"Payslip not found for {MonthNames[month - 1]} {year}");

            var employee = await _employeesRepository.GetByIdAsync(employeeId);
            return Result<PayslipDetailDto>.Success(MapToPayslipDetail(payslip, employee));
        }

        // ==================== Assets ====================

        public async Task<Result<IEnumerable<MyAssetDto>>> GetMyAssetsAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var assignments = await _assetsRepository.GetAssignmentsByEmployeeAsync(employeeId);
            var activeAssignments = assignments.Where(a => a.IsActive).ToList();

            var assets = new List<MyAssetDto>();
            foreach (var assignment in activeAssignments)
            {
                var asset = await _assetsRepository.GetByIdAsync(assignment.AssetId);
                if (asset != null)
                {
                    assets.Add(MapToMyAsset(assignment, asset));
                }
            }

            return Result<IEnumerable<MyAssetDto>>.Success(assets);
        }

        public async Task<Result<IEnumerable<MyAssetDto>>> GetMyAssetHistoryAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var assignments = await _assetsRepository.GetAssignmentsByEmployeeAsync(employeeId);

            var assets = new List<MyAssetDto>();
            foreach (var assignment in assignments.OrderByDescending(a => a.AssignedOn))
            {
                var asset = await _assetsRepository.GetByIdAsync(assignment.AssetId);
                if (asset != null)
                {
                    assets.Add(MapToMyAsset(assignment, asset));
                }
            }

            return Result<IEnumerable<MyAssetDto>>.Success(assets);
        }

        // ==================== Tax Declarations ====================

        public async Task<Result<IEnumerable<TaxDeclarationSummaryDto>>> GetMyTaxDeclarationsAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var declarations = await _taxDeclarationRepository.GetByEmployeeIdAsync(employeeId);

            var summaries = declarations
                .OrderByDescending(d => d.FinancialYear)
                .Select(MapToTaxDeclarationSummary)
                .ToList();

            return Result<IEnumerable<TaxDeclarationSummaryDto>>.Success(summaries);
        }

        public async Task<Result<TaxDeclarationDetailDto>> GetTaxDeclarationDetailAsync(Guid employeeId, Guid declarationId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (declarationId == default)
                return Error.Validation("Declaration ID cannot be empty");

            var declaration = await _taxDeclarationRepository.GetByIdAsync(declarationId);
            if (declaration == null)
                return Error.NotFound("Tax declaration not found");

            // Security check: ensure declaration belongs to the employee
            if (declaration.EmployeeId != employeeId)
                return Error.Forbidden("Access denied to this tax declaration");

            return Result<TaxDeclarationDetailDto>.Success(MapToTaxDeclarationDetail(declaration));
        }

        public async Task<Result<TaxDeclarationDetailDto>> GetTaxDeclarationByYearAsync(Guid employeeId, string financialYear)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (string.IsNullOrWhiteSpace(financialYear))
                return Error.Validation("Financial year is required");

            var declaration = await _taxDeclarationRepository.GetByEmployeeAndYearAsync(employeeId, financialYear);
            if (declaration == null)
                return Error.NotFound($"Tax declaration not found for {financialYear}");

            return Result<TaxDeclarationDetailDto>.Success(MapToTaxDeclarationDetail(declaration));
        }

        public async Task<Result<TaxDeclarationDetailDto>> UpdateTaxDeclarationAsync(Guid employeeId, Guid declarationId, UpdateTaxDeclarationDto dto)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (declarationId == default)
                return Error.Validation("Declaration ID cannot be empty");

            var declaration = await _taxDeclarationRepository.GetByIdAsync(declarationId);
            if (declaration == null)
                return Error.NotFound("Tax declaration not found");

            // Security check: ensure declaration belongs to the employee
            if (declaration.EmployeeId != employeeId)
                return Error.Forbidden("Access denied to this tax declaration");

            // Can only update draft or rejected declarations
            if (declaration.Status != "draft" && declaration.Status != "rejected")
                return Error.Validation($"Cannot update tax declaration with status '{declaration.Status}'");

            // Apply updates
            ApplyTaxDeclarationUpdates(declaration, dto);
            declaration.UpdatedAt = DateTime.UtcNow;

            // If it was rejected and being revised, clear rejection and increment revision count
            if (declaration.Status == "rejected")
            {
                await _taxDeclarationRepository.ClearRejectionAsync(declarationId);
                await _taxDeclarationRepository.IncrementRevisionCountAsync(declarationId);
                declaration.Status = "draft";
            }

            await _taxDeclarationRepository.UpdateAsync(declaration);

            // Refresh and return
            declaration = await _taxDeclarationRepository.GetByIdAsync(declarationId);
            return Result<TaxDeclarationDetailDto>.Success(MapToTaxDeclarationDetail(declaration!));
        }

        public async Task<Result<TaxDeclarationDetailDto>> SubmitTaxDeclarationAsync(Guid employeeId, Guid declarationId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");
            if (declarationId == default)
                return Error.Validation("Declaration ID cannot be empty");

            var declaration = await _taxDeclarationRepository.GetByIdAsync(declarationId);
            if (declaration == null)
                return Error.NotFound("Tax declaration not found");

            // Security check: ensure declaration belongs to the employee
            if (declaration.EmployeeId != employeeId)
                return Error.Forbidden("Access denied to this tax declaration");

            // Can only submit draft declarations
            if (declaration.Status != "draft")
                return Error.Validation($"Cannot submit tax declaration with status '{declaration.Status}'");

            await _taxDeclarationRepository.UpdateStatusAsync(declarationId, "submitted");
            declaration.Status = "submitted";
            declaration.SubmittedAt = DateTime.UtcNow;

            return Result<TaxDeclarationDetailDto>.Success(MapToTaxDeclarationDetail(declaration));
        }

        // ==================== Subscriptions ====================

        public async Task<Result<IEnumerable<MySubscriptionDto>>> GetMySubscriptionsAsync(Guid employeeId)
        {
            if (employeeId == default)
                return Error.Validation("Employee ID cannot be empty");

            var assignments = await _subscriptionsRepository.GetAssignmentsByEmployeeAsync(employeeId);

            var subscriptions = new List<MySubscriptionDto>();
            foreach (var assignment in assignments)
            {
                var subscription = await _subscriptionsRepository.GetByIdAsync(assignment.SubscriptionId);
                if (subscription != null)
                {
                    subscriptions.Add(MapToMySubscription(assignment, subscription));
                }
            }

            return Result<IEnumerable<MySubscriptionDto>>.Success(subscriptions);
        }

        // ==================== Helper Methods ====================

        private static string GetCurrentFinancialYear()
        {
            var now = DateTime.UtcNow;
            // Financial year in India is April to March
            if (now.Month >= 4)
                return $"{now.Year}-{(now.Year + 1) % 100:D2}";
            else
                return $"{now.Year - 1}-{now.Year % 100:D2}";
        }

        private static string MaskPan(string? pan)
        {
            if (string.IsNullOrWhiteSpace(pan) || pan.Length < 4)
                return "****";
            return $"XXXXXX{pan[^4..]}";
        }

        private static string MaskBankAccount(string? account)
        {
            if (string.IsNullOrWhiteSpace(account) || account.Length < 4)
                return "****";
            return $"****{account[^4..]}";
        }

        private static EmployeeProfileDto MapToProfileDto(Employees employee)
        {
            return new EmployeeProfileDto
            {
                Id = employee.Id,
                EmployeeName = employee.EmployeeName,
                Email = employee.Email,
                Phone = employee.Phone,
                EmployeeId = employee.EmployeeId,
                Department = employee.Department,
                Designation = employee.Designation,
                HireDate = employee.HireDate,
                Status = employee.Status,
                AddressLine1 = employee.AddressLine1,
                AddressLine2 = employee.AddressLine2,
                City = employee.City,
                State = employee.State,
                ZipCode = employee.ZipCode,
                Country = employee.Country,
                Company = employee.Company,
                CompanyId = employee.CompanyId,
                MaskedPanNumber = MaskPan(employee.PanNumber),
                MaskedBankAccountNumber = MaskBankAccount(employee.BankAccountNumber),
                BankName = employee.BankName,
                IfscCode = employee.IfscCode
            };
        }

        private static PayslipSummaryDto MapToPayslipSummary(EmployeeSalaryTransactions payslip)
        {
            var totalDeductions = payslip.PfEmployee + payslip.Pt + payslip.IncomeTax + payslip.OtherDeductions;
            return new PayslipSummaryDto
            {
                Id = payslip.Id,
                SalaryMonth = payslip.SalaryMonth,
                SalaryYear = payslip.SalaryYear,
                MonthName = MonthNames[payslip.SalaryMonth - 1],
                GrossSalary = payslip.GrossSalary,
                TotalDeductions = totalDeductions,
                NetSalary = payslip.NetSalary,
                PaymentDate = payslip.PaymentDate,
                Status = payslip.Status,
                Currency = payslip.Currency
            };
        }

        private static PayslipDetailDto MapToPayslipDetail(EmployeeSalaryTransactions payslip, Employees? employee)
        {
            var totalDeductions = payslip.PfEmployee + payslip.Pt + payslip.IncomeTax + payslip.OtherDeductions;

            // Build earnings array
            var earnings = new List<PayslipComponentDto>();
            if (payslip.BasicSalary > 0) earnings.Add(new PayslipComponentDto { Name = "Basic Salary", Amount = payslip.BasicSalary });
            if (payslip.Hra > 0) earnings.Add(new PayslipComponentDto { Name = "HRA", Amount = payslip.Hra });
            if (payslip.Conveyance > 0) earnings.Add(new PayslipComponentDto { Name = "Conveyance", Amount = payslip.Conveyance });
            if (payslip.MedicalAllowance > 0) earnings.Add(new PayslipComponentDto { Name = "Medical Allowance", Amount = payslip.MedicalAllowance });
            if (payslip.SpecialAllowance > 0) earnings.Add(new PayslipComponentDto { Name = "Special Allowance", Amount = payslip.SpecialAllowance });
            if (payslip.Lta > 0) earnings.Add(new PayslipComponentDto { Name = "LTA", Amount = payslip.Lta });
            if (payslip.OtherAllowances > 0) earnings.Add(new PayslipComponentDto { Name = "Other Allowances", Amount = payslip.OtherAllowances });

            // Build deductions array
            var deductions = new List<PayslipComponentDto>();
            if (payslip.PfEmployee > 0) deductions.Add(new PayslipComponentDto { Name = "PF (Employee)", Amount = payslip.PfEmployee });
            if (payslip.Pt > 0) deductions.Add(new PayslipComponentDto { Name = "Professional Tax", Amount = payslip.Pt });
            if (payslip.IncomeTax > 0) deductions.Add(new PayslipComponentDto { Name = "Income Tax (TDS)", Amount = payslip.IncomeTax });
            if (payslip.OtherDeductions > 0) deductions.Add(new PayslipComponentDto { Name = "Other Deductions", Amount = payslip.OtherDeductions });

            return new PayslipDetailDto
            {
                Id = payslip.Id,
                SalaryMonth = payslip.SalaryMonth,
                SalaryYear = payslip.SalaryYear,
                MonthName = MonthNames[payslip.SalaryMonth - 1],
                BasicSalary = payslip.BasicSalary,
                Hra = payslip.Hra,
                Conveyance = payslip.Conveyance,
                MedicalAllowance = payslip.MedicalAllowance,
                SpecialAllowance = payslip.SpecialAllowance,
                Lta = payslip.Lta,
                OtherAllowances = payslip.OtherAllowances,
                GrossSalary = payslip.GrossSalary,
                PfEmployee = payslip.PfEmployee,
                PfEmployer = payslip.PfEmployer,
                Pt = payslip.Pt,
                IncomeTax = payslip.IncomeTax,
                OtherDeductions = payslip.OtherDeductions,
                TotalDeductions = totalDeductions,
                NetSalary = payslip.NetSalary,
                PaymentDate = payslip.PaymentDate,
                PaymentMethod = payslip.PaymentMethod,
                PaymentReference = payslip.PaymentReference,
                Status = payslip.Status,
                Currency = payslip.Currency,
                Remarks = payslip.Remarks,
                EmployeeName = employee?.EmployeeName ?? string.Empty,
                EmployeeId = employee?.EmployeeId,
                Designation = employee?.Designation,
                Department = employee?.Department,
                Company = employee?.Company,
                MaskedBankAccountNumber = MaskBankAccount(employee?.BankAccountNumber),
                MaskedPanNumber = MaskPan(employee?.PanNumber),
                BankName = employee?.BankName,
                Earnings = earnings,
                Deductions = deductions
            };
        }

        private static MyAssetDto MapToMyAsset(AssetAssignments assignment, Core.Entities.Assets asset)
        {
            return new MyAssetDto
            {
                AssignmentId = assignment.Id,
                AssetId = asset.Id,
                AssetTag = asset.AssetTag,
                Name = asset.Name,
                AssetType = asset.AssetType,
                SerialNumber = asset.SerialNumber,
                Description = asset.Description,
                AssignedOn = assignment.AssignedOn,
                ConditionOut = assignment.ConditionOut,
                Notes = assignment.Notes,
                WarrantyExpiration = asset.WarrantyExpiration
            };
        }

        private static MySubscriptionDto MapToMySubscription(SubscriptionAssignments assignment, Core.Entities.Subscriptions subscription)
        {
            return new MySubscriptionDto
            {
                AssignmentId = assignment.Id,
                SubscriptionId = subscription.Id,
                Name = subscription.Name,
                Description = subscription.Notes,
                Category = subscription.Category,
                Vendor = subscription.Vendor,
                AssignedOn = assignment.AssignedOn,
                RevokedOn = assignment.RevokedOn,
                Notes = assignment.Notes,
                IsActive = assignment.RevokedOn == null && subscription.Status == "active"
            };
        }

        private static TaxDeclarationSummaryDto MapToTaxDeclarationSummary(EmployeeTaxDeclaration declaration)
        {
            var total80C = declaration.Sec80cPpf + declaration.Sec80cElss + declaration.Sec80cLifeInsurance +
                           declaration.Sec80cHomeLoanPrincipal + declaration.Sec80cChildrenTuition +
                           declaration.Sec80cNsc + declaration.Sec80cSukanyaSamriddhi +
                           declaration.Sec80cFixedDeposit + declaration.Sec80cOthers;

            var total80D = declaration.Sec80dSelfFamily + declaration.Sec80dParents + declaration.Sec80dPreventiveCheckup;

            var totalOther = declaration.Sec80ccdNps + declaration.Sec80eEducationLoan +
                            declaration.Sec24HomeLoanInterest + declaration.Sec80gDonations +
                            declaration.Sec80ttaSavingsInterest;

            return new TaxDeclarationSummaryDto
            {
                Id = declaration.Id,
                FinancialYear = declaration.FinancialYear,
                TaxRegime = declaration.TaxRegime,
                Total80CDeductions = total80C,
                Total80DDeductions = total80D,
                TotalOtherDeductions = totalOther,
                GrandTotalDeductions = total80C + total80D + totalOther,
                Status = declaration.Status,
                SubmittedAt = declaration.SubmittedAt,
                VerifiedAt = declaration.VerifiedAt,
                RejectionReason = declaration.RejectionReason,
                RevisionCount = declaration.RevisionCount
            };
        }

        private static TaxDeclarationDetailDto MapToTaxDeclarationDetail(EmployeeTaxDeclaration declaration)
        {
            var summary = MapToTaxDeclarationSummary(declaration);

            return new TaxDeclarationDetailDto
            {
                Id = declaration.Id,
                FinancialYear = declaration.FinancialYear,
                TaxRegime = declaration.TaxRegime,
                Sec80cPpf = declaration.Sec80cPpf,
                Sec80cElss = declaration.Sec80cElss,
                Sec80cLifeInsurance = declaration.Sec80cLifeInsurance,
                Sec80cHomeLoanPrincipal = declaration.Sec80cHomeLoanPrincipal,
                Sec80cChildrenTuition = declaration.Sec80cChildrenTuition,
                Sec80cNsc = declaration.Sec80cNsc,
                Sec80cSukanyaSamriddhi = declaration.Sec80cSukanyaSamriddhi,
                Sec80cFixedDeposit = declaration.Sec80cFixedDeposit,
                Sec80cOthers = declaration.Sec80cOthers,
                Sec80ccdNps = declaration.Sec80ccdNps,
                Sec80dSelfFamily = declaration.Sec80dSelfFamily,
                Sec80dParents = declaration.Sec80dParents,
                Sec80dPreventiveCheckup = declaration.Sec80dPreventiveCheckup,
                Sec80dSelfSeniorCitizen = declaration.Sec80dSelfSeniorCitizen,
                Sec80dParentsSeniorCitizen = declaration.Sec80dParentsSeniorCitizen,
                Sec80eEducationLoan = declaration.Sec80eEducationLoan,
                Sec24HomeLoanInterest = declaration.Sec24HomeLoanInterest,
                Sec80gDonations = declaration.Sec80gDonations,
                Sec80ttaSavingsInterest = declaration.Sec80ttaSavingsInterest,
                HraRentPaidAnnual = declaration.HraRentPaidAnnual,
                HraMetroCity = declaration.HraMetroCity,
                HraLandlordPan = declaration.HraLandlordPan,
                HraLandlordName = declaration.HraLandlordName,
                OtherIncomeAnnual = declaration.OtherIncomeAnnual,
                PrevEmployerIncome = declaration.PrevEmployerIncome,
                PrevEmployerTds = declaration.PrevEmployerTds,
                PrevEmployerPf = declaration.PrevEmployerPf,
                PrevEmployerPt = declaration.PrevEmployerPt,
                Status = declaration.Status,
                SubmittedAt = declaration.SubmittedAt,
                VerifiedAt = declaration.VerifiedAt,
                VerifiedBy = declaration.VerifiedBy,
                RejectedAt = declaration.RejectedAt,
                RejectedBy = declaration.RejectedBy,
                RejectionReason = declaration.RejectionReason,
                RevisionCount = declaration.RevisionCount,
                ProofDocuments = declaration.ProofDocuments,
                Total80CDeductions = summary.Total80CDeductions,
                Total80DDeductions = summary.Total80DDeductions,
                TotalOtherDeductions = summary.TotalOtherDeductions,
                GrandTotalDeductions = summary.GrandTotalDeductions
            };
        }

        private static void ApplyTaxDeclarationUpdates(EmployeeTaxDeclaration declaration, UpdateTaxDeclarationDto dto)
        {
            if (dto.TaxRegime != null) declaration.TaxRegime = dto.TaxRegime;

            // Section 80C
            if (dto.Sec80cPpf.HasValue) declaration.Sec80cPpf = dto.Sec80cPpf.Value;
            if (dto.Sec80cElss.HasValue) declaration.Sec80cElss = dto.Sec80cElss.Value;
            if (dto.Sec80cLifeInsurance.HasValue) declaration.Sec80cLifeInsurance = dto.Sec80cLifeInsurance.Value;
            if (dto.Sec80cHomeLoanPrincipal.HasValue) declaration.Sec80cHomeLoanPrincipal = dto.Sec80cHomeLoanPrincipal.Value;
            if (dto.Sec80cChildrenTuition.HasValue) declaration.Sec80cChildrenTuition = dto.Sec80cChildrenTuition.Value;
            if (dto.Sec80cNsc.HasValue) declaration.Sec80cNsc = dto.Sec80cNsc.Value;
            if (dto.Sec80cSukanyaSamriddhi.HasValue) declaration.Sec80cSukanyaSamriddhi = dto.Sec80cSukanyaSamriddhi.Value;
            if (dto.Sec80cFixedDeposit.HasValue) declaration.Sec80cFixedDeposit = dto.Sec80cFixedDeposit.Value;
            if (dto.Sec80cOthers.HasValue) declaration.Sec80cOthers = dto.Sec80cOthers.Value;

            // Section 80CCD(1B)
            if (dto.Sec80ccdNps.HasValue) declaration.Sec80ccdNps = dto.Sec80ccdNps.Value;

            // Section 80D
            if (dto.Sec80dSelfFamily.HasValue) declaration.Sec80dSelfFamily = dto.Sec80dSelfFamily.Value;
            if (dto.Sec80dParents.HasValue) declaration.Sec80dParents = dto.Sec80dParents.Value;
            if (dto.Sec80dPreventiveCheckup.HasValue) declaration.Sec80dPreventiveCheckup = dto.Sec80dPreventiveCheckup.Value;
            if (dto.Sec80dSelfSeniorCitizen.HasValue) declaration.Sec80dSelfSeniorCitizen = dto.Sec80dSelfSeniorCitizen.Value;
            if (dto.Sec80dParentsSeniorCitizen.HasValue) declaration.Sec80dParentsSeniorCitizen = dto.Sec80dParentsSeniorCitizen.Value;

            // Section 80E
            if (dto.Sec80eEducationLoan.HasValue) declaration.Sec80eEducationLoan = dto.Sec80eEducationLoan.Value;

            // Section 24
            if (dto.Sec24HomeLoanInterest.HasValue) declaration.Sec24HomeLoanInterest = dto.Sec24HomeLoanInterest.Value;

            // Section 80G
            if (dto.Sec80gDonations.HasValue) declaration.Sec80gDonations = dto.Sec80gDonations.Value;

            // Section 80TTA
            if (dto.Sec80ttaSavingsInterest.HasValue) declaration.Sec80ttaSavingsInterest = dto.Sec80ttaSavingsInterest.Value;

            // HRA
            if (dto.HraRentPaidAnnual.HasValue) declaration.HraRentPaidAnnual = dto.HraRentPaidAnnual.Value;
            if (dto.HraMetroCity.HasValue) declaration.HraMetroCity = dto.HraMetroCity.Value;
            if (dto.HraLandlordPan != null) declaration.HraLandlordPan = dto.HraLandlordPan;
            if (dto.HraLandlordName != null) declaration.HraLandlordName = dto.HraLandlordName;

            // Other Income
            if (dto.OtherIncomeAnnual.HasValue) declaration.OtherIncomeAnnual = dto.OtherIncomeAnnual.Value;

            // Previous Employer
            if (dto.PrevEmployerIncome.HasValue) declaration.PrevEmployerIncome = dto.PrevEmployerIncome.Value;
            if (dto.PrevEmployerTds.HasValue) declaration.PrevEmployerTds = dto.PrevEmployerTds.Value;
            if (dto.PrevEmployerPf.HasValue) declaration.PrevEmployerPf = dto.PrevEmployerPf.Value;
            if (dto.PrevEmployerPt.HasValue) declaration.PrevEmployerPt = dto.PrevEmployerPt.Value;

            // Proof documents
            if (dto.ProofDocuments != null) declaration.ProofDocuments = dto.ProofDocuments;
        }

        private static List<QuickActionDto> GetQuickActions(EmployeeTaxDeclaration? taxDeclaration)
        {
            var actions = new List<QuickActionDto>();

            // Check if tax declaration needs action
            if (taxDeclaration == null)
            {
                actions.Add(new QuickActionDto
                {
                    Title = "Submit Tax Declaration",
                    Description = "You haven't submitted your tax declaration for this financial year",
                    ActionType = "submit_declaration",
                    ActionUrl = "/portal/tax-declarations",
                    IsUrgent = true
                });
            }
            else if (taxDeclaration.Status == "draft")
            {
                actions.Add(new QuickActionDto
                {
                    Title = "Complete Tax Declaration",
                    Description = "Your tax declaration is in draft. Complete and submit for verification.",
                    ActionType = "complete_declaration",
                    ActionUrl = $"/portal/tax-declarations/{taxDeclaration.Id}",
                    IsUrgent = true
                });
            }
            else if (taxDeclaration.Status == "rejected")
            {
                actions.Add(new QuickActionDto
                {
                    Title = "Revise Tax Declaration",
                    Description = $"Your tax declaration was rejected: {taxDeclaration.RejectionReason}",
                    ActionType = "revise_declaration",
                    ActionUrl = $"/portal/tax-declarations/{taxDeclaration.Id}",
                    IsUrgent = true
                });
            }

            return actions;
        }
    }
}
