using Application.DTOs.Leave;
using Application.Interfaces.Leave;
using Core.Common;
using Core.Entities.Leave;
using Core.Interfaces;
using Core.Interfaces.Leave;

namespace Application.Services.Leave
{
    public class LeaveService : ILeaveService
    {
        private readonly ILeaveTypeRepository _leaveTypeRepository;
        private readonly IEmployeeLeaveBalanceRepository _balanceRepository;
        private readonly ILeaveApplicationRepository _applicationRepository;
        private readonly IHolidayRepository _holidayRepository;
        private readonly IEmployeesRepository _employeesRepository;

        public LeaveService(
            ILeaveTypeRepository leaveTypeRepository,
            IEmployeeLeaveBalanceRepository balanceRepository,
            ILeaveApplicationRepository applicationRepository,
            IHolidayRepository holidayRepository,
            IEmployeesRepository employeesRepository)
        {
            _leaveTypeRepository = leaveTypeRepository ?? throw new ArgumentNullException(nameof(leaveTypeRepository));
            _balanceRepository = balanceRepository ?? throw new ArgumentNullException(nameof(balanceRepository));
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _holidayRepository = holidayRepository ?? throw new ArgumentNullException(nameof(holidayRepository));
            _employeesRepository = employeesRepository ?? throw new ArgumentNullException(nameof(employeesRepository));
        }

        // ==================== Leave Types ====================

        public async Task<Result<IEnumerable<LeaveTypeDto>>> GetLeaveTypesAsync(Guid companyId, bool activeOnly = true)
        {
            var types = await _leaveTypeRepository.GetAllByCompanyAsync(companyId, activeOnly);
            return Result<IEnumerable<LeaveTypeDto>>.Success(types.Select(MapToLeaveTypeDto));
        }

        public async Task<Result<LeaveTypeDto>> GetLeaveTypeByIdAsync(Guid id)
        {
            var type = await _leaveTypeRepository.GetByIdAsync(id);
            if (type == null)
                return Error.NotFound("Leave type not found");
            return Result<LeaveTypeDto>.Success(MapToLeaveTypeDto(type));
        }

        public async Task<Result<LeaveTypeDto>> CreateLeaveTypeAsync(Guid companyId, CreateLeaveTypeDto dto, string? createdBy = null)
        {
            if (await _leaveTypeRepository.CodeExistsAsync(companyId, dto.Code))
                return Error.Conflict($"Leave type code '{dto.Code}' already exists");

            var entity = new LeaveType
            {
                CompanyId = companyId,
                Name = dto.Name,
                Code = dto.Code.ToUpperInvariant(),
                Description = dto.Description,
                DaysPerYear = dto.DaysPerYear,
                CarryForwardAllowed = dto.CarryForwardAllowed,
                MaxCarryForwardDays = dto.MaxCarryForwardDays,
                EncashmentAllowed = dto.EncashmentAllowed,
                MaxEncashmentDays = dto.MaxEncashmentDays,
                RequiresApproval = dto.RequiresApproval,
                MinDaysNotice = dto.MinDaysNotice,
                MaxConsecutiveDays = dto.MaxConsecutiveDays,
                IsActive = true,
                ColorCode = dto.ColorCode,
                SortOrder = dto.SortOrder,
                CreatedBy = createdBy,
                UpdatedBy = createdBy
            };

            var created = await _leaveTypeRepository.AddAsync(entity);
            return Result<LeaveTypeDto>.Success(MapToLeaveTypeDto(created));
        }

        public async Task<Result<LeaveTypeDto>> UpdateLeaveTypeAsync(Guid id, UpdateLeaveTypeDto dto, string? updatedBy = null)
        {
            var entity = await _leaveTypeRepository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound("Leave type not found");

            if (dto.Code != null && dto.Code.ToUpperInvariant() != entity.Code)
            {
                if (await _leaveTypeRepository.CodeExistsAsync(entity.CompanyId, dto.Code, id))
                    return Error.Conflict($"Leave type code '{dto.Code}' already exists");
                entity.Code = dto.Code.ToUpperInvariant();
            }

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.Description != null) entity.Description = dto.Description;
            if (dto.DaysPerYear.HasValue) entity.DaysPerYear = dto.DaysPerYear.Value;
            if (dto.CarryForwardAllowed.HasValue) entity.CarryForwardAllowed = dto.CarryForwardAllowed.Value;
            if (dto.MaxCarryForwardDays.HasValue) entity.MaxCarryForwardDays = dto.MaxCarryForwardDays.Value;
            if (dto.EncashmentAllowed.HasValue) entity.EncashmentAllowed = dto.EncashmentAllowed.Value;
            if (dto.MaxEncashmentDays.HasValue) entity.MaxEncashmentDays = dto.MaxEncashmentDays.Value;
            if (dto.RequiresApproval.HasValue) entity.RequiresApproval = dto.RequiresApproval.Value;
            if (dto.MinDaysNotice.HasValue) entity.MinDaysNotice = dto.MinDaysNotice.Value;
            if (dto.MaxConsecutiveDays.HasValue) entity.MaxConsecutiveDays = dto.MaxConsecutiveDays.Value;
            if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
            if (dto.ColorCode != null) entity.ColorCode = dto.ColorCode;
            if (dto.SortOrder.HasValue) entity.SortOrder = dto.SortOrder.Value;
            entity.UpdatedBy = updatedBy;

            await _leaveTypeRepository.UpdateAsync(entity);
            return Result<LeaveTypeDto>.Success(MapToLeaveTypeDto(entity));
        }

        public async Task<Result> DeleteLeaveTypeAsync(Guid id)
        {
            var entity = await _leaveTypeRepository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound("Leave type not found");

            await _leaveTypeRepository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Leave Balances ====================

        public async Task<Result<IEnumerable<LeaveBalanceDto>>> GetEmployeeBalancesAsync(Guid employeeId, string? financialYear = null)
        {
            financialYear ??= GetCurrentFinancialYear();
            var balances = await _balanceRepository.GetByEmployeeAsync(employeeId, financialYear);
            var result = new List<LeaveBalanceDto>();

            foreach (var balance in balances)
            {
                var leaveType = await _leaveTypeRepository.GetByIdAsync(balance.LeaveTypeId);
                if (leaveType != null)
                {
                    result.Add(MapToLeaveBalanceDto(balance, leaveType));
                }
            }

            return Result<IEnumerable<LeaveBalanceDto>>.Success(result);
        }

        public async Task<Result> InitializeEmployeeBalancesAsync(Guid employeeId, Guid companyId, string financialYear)
        {
            await _balanceRepository.InitializeBalancesAsync(employeeId, companyId, financialYear);
            return Result.Success();
        }

        public async Task<Result> AdjustBalanceAsync(Guid employeeId, AdjustLeaveBalanceDto dto)
        {
            var balance = await _balanceRepository.GetByEmployeeTypeYearAsync(employeeId, dto.LeaveTypeId, dto.FinancialYear);
            if (balance == null)
                return Error.NotFound("Leave balance not found");

            balance.Adjusted += dto.AdjustmentDays;
            await _balanceRepository.UpdateAsync(balance);
            return Result.Success();
        }

        public async Task<Result> CarryForwardBalancesAsync(Guid employeeId, string fromYear, string toYear)
        {
            await _balanceRepository.CarryForwardBalancesAsync(employeeId, fromYear, toYear);
            return Result.Success();
        }

        // ==================== Leave Applications ====================

        public async Task<Result<LeaveApplicationDetailDto>> ApplyLeaveAsync(Guid employeeId, Guid companyId, ApplyLeaveDto dto)
        {
            // Validate leave type
            var leaveType = await _leaveTypeRepository.GetByIdAsync(dto.LeaveTypeId);
            if (leaveType == null)
                return Error.NotFound("Leave type not found");

            if (!leaveType.IsActive)
                return Error.Validation("This leave type is not active");

            // Validate dates
            if (dto.ToDate < dto.FromDate)
                return Error.Validation("End date cannot be before start date");

            if (dto.FromDate < DateTime.UtcNow.Date && leaveType.MinDaysNotice > 0)
                return Error.Validation("Leave dates cannot be in the past");

            // Check for overlapping leaves
            var overlapping = await _applicationRepository.GetOverlappingAsync(employeeId, dto.FromDate, dto.ToDate);
            if (overlapping.Any())
                return Error.Validation("You already have a leave application for these dates");

            // Calculate leave days
            var calculation = await CalculateLeaveDaysAsync(companyId, dto.FromDate, dto.ToDate);
            if (calculation.IsFailure)
                return Error.Validation(calculation.Error!.Message);

            var totalDays = dto.IsHalfDay ? 0.5m : calculation.Value!.TotalDays;

            // Check balance
            var financialYear = GetFinancialYearForDate(dto.FromDate);
            var balance = await _balanceRepository.GetByEmployeeTypeYearAsync(employeeId, dto.LeaveTypeId, financialYear);
            if (balance == null)
            {
                // Initialize balance if not exists
                await _balanceRepository.InitializeBalancesAsync(employeeId, companyId, financialYear);
                balance = await _balanceRepository.GetByEmployeeTypeYearAsync(employeeId, dto.LeaveTypeId, financialYear);
            }

            if (balance != null && balance.AvailableBalance < totalDays)
                return Error.Validation($"Insufficient leave balance. Available: {balance.AvailableBalance}, Requested: {totalDays}");

            // Check max consecutive days
            if (leaveType.MaxConsecutiveDays.HasValue && totalDays > leaveType.MaxConsecutiveDays.Value)
                return Error.Validation($"Maximum consecutive days allowed is {leaveType.MaxConsecutiveDays.Value}");

            // Create application
            var application = new LeaveApplication
            {
                EmployeeId = employeeId,
                LeaveTypeId = dto.LeaveTypeId,
                CompanyId = companyId,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                TotalDays = totalDays,
                IsHalfDay = dto.IsHalfDay,
                HalfDayType = dto.HalfDayType,
                Reason = dto.Reason,
                Status = leaveType.RequiresApproval ? "pending" : "approved",
                AppliedAt = DateTime.UtcNow,
                EmergencyContact = dto.EmergencyContact,
                HandoverNotes = dto.HandoverNotes,
                AttachmentUrl = dto.AttachmentUrl
            };

            if (!leaveType.RequiresApproval)
            {
                application.ApprovedAt = DateTime.UtcNow;
                // Auto-deduct balance for auto-approved leaves
                await _balanceRepository.IncrementTakenAsync(employeeId, dto.LeaveTypeId, financialYear, totalDays);
            }

            var created = await _applicationRepository.AddAsync(application);
            return await GetLeaveApplicationAsync(created.Id);
        }

        public async Task<Result<LeaveApplicationDetailDto>> GetLeaveApplicationAsync(Guid applicationId)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            var employee = await _employeesRepository.GetByIdAsync(application.EmployeeId);
            var leaveType = await _leaveTypeRepository.GetByIdAsync(application.LeaveTypeId);
            var approver = application.ApprovedBy.HasValue
                ? await _employeesRepository.GetByIdAsync(application.ApprovedBy.Value)
                : null;

            return Result<LeaveApplicationDetailDto>.Success(
                MapToLeaveApplicationDetail(application, employee, leaveType, approver));
        }

        public async Task<Result<IEnumerable<LeaveApplicationSummaryDto>>> GetEmployeeApplicationsAsync(Guid employeeId, string? status = null)
        {
            var applications = await _applicationRepository.GetByEmployeeAsync(employeeId, status);
            var result = new List<LeaveApplicationSummaryDto>();

            foreach (var app in applications)
            {
                var employee = await _employeesRepository.GetByIdAsync(app.EmployeeId);
                var leaveType = await _leaveTypeRepository.GetByIdAsync(app.LeaveTypeId);
                var approver = app.ApprovedBy.HasValue ? await _employeesRepository.GetByIdAsync(app.ApprovedBy.Value) : null;
                result.Add(MapToLeaveApplicationSummary(app, employee, leaveType, approver));
            }

            return Result<IEnumerable<LeaveApplicationSummaryDto>>.Success(result);
        }

        public async Task<Result<IEnumerable<LeaveApplicationSummaryDto>>> GetPendingApprovalsAsync(Guid companyId)
        {
            var applications = await _applicationRepository.GetPendingApprovalAsync(companyId);
            var result = new List<LeaveApplicationSummaryDto>();

            foreach (var app in applications)
            {
                var employee = await _employeesRepository.GetByIdAsync(app.EmployeeId);
                var leaveType = await _leaveTypeRepository.GetByIdAsync(app.LeaveTypeId);
                result.Add(MapToLeaveApplicationSummary(app, employee, leaveType, null));
            }

            return Result<IEnumerable<LeaveApplicationSummaryDto>>.Success(result);
        }

        public async Task<Result<LeaveApplicationDetailDto>> UpdateLeaveApplicationAsync(Guid employeeId, Guid applicationId, UpdateLeaveApplicationDto dto)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            if (application.EmployeeId != employeeId)
                return Error.Forbidden("You can only update your own leave applications");

            if (application.Status != "pending")
                return Error.Validation("Only pending applications can be updated");

            if (dto.FromDate.HasValue) application.FromDate = dto.FromDate.Value;
            if (dto.ToDate.HasValue) application.ToDate = dto.ToDate.Value;
            if (dto.IsHalfDay.HasValue) application.IsHalfDay = dto.IsHalfDay.Value;
            if (dto.HalfDayType != null) application.HalfDayType = dto.HalfDayType;
            if (dto.Reason != null) application.Reason = dto.Reason;
            if (dto.EmergencyContact != null) application.EmergencyContact = dto.EmergencyContact;
            if (dto.HandoverNotes != null) application.HandoverNotes = dto.HandoverNotes;
            if (dto.AttachmentUrl != null) application.AttachmentUrl = dto.AttachmentUrl;

            // Recalculate total days
            var calculation = await CalculateLeaveDaysAsync(application.CompanyId, application.FromDate, application.ToDate);
            if (calculation.IsSuccess)
            {
                application.TotalDays = application.IsHalfDay ? 0.5m : calculation.Value!.TotalDays;
            }

            await _applicationRepository.UpdateAsync(application);
            return await GetLeaveApplicationAsync(applicationId);
        }

        public async Task<Result<LeaveApplicationDetailDto>> ApproveLeaveAsync(Guid applicationId, Guid approvedBy, ApproveLeaveDto dto)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            if (application.Status != "pending")
                return Error.Validation("Only pending applications can be approved");

            await _applicationRepository.UpdateStatusAsync(applicationId, "approved", approvedBy);

            // Deduct from balance
            var financialYear = GetFinancialYearForDate(application.FromDate);
            await _balanceRepository.IncrementTakenAsync(application.EmployeeId, application.LeaveTypeId, financialYear, application.TotalDays);

            return await GetLeaveApplicationAsync(applicationId);
        }

        public async Task<Result<LeaveApplicationDetailDto>> RejectLeaveAsync(Guid applicationId, Guid rejectedBy, RejectLeaveDto dto)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            if (application.Status != "pending")
                return Error.Validation("Only pending applications can be rejected");

            await _applicationRepository.UpdateStatusAsync(applicationId, "rejected", rejectedBy, dto.Reason);
            return await GetLeaveApplicationAsync(applicationId);
        }

        public async Task<Result<LeaveApplicationDetailDto>> CancelLeaveAsync(Guid applicationId, CancelLeaveDto dto)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            if (application.Status != "approved")
                return Error.Validation("Only approved applications can be cancelled");

            await _applicationRepository.UpdateStatusAsync(applicationId, "cancelled", null, dto.Reason);

            // Restore balance
            var financialYear = GetFinancialYearForDate(application.FromDate);
            await _balanceRepository.DecrementTakenAsync(application.EmployeeId, application.LeaveTypeId, financialYear, application.TotalDays);

            return await GetLeaveApplicationAsync(applicationId);
        }

        public async Task<Result> WithdrawLeaveAsync(Guid employeeId, Guid applicationId, string? reason = null)
        {
            var application = await _applicationRepository.GetByIdAsync(applicationId);
            if (application == null)
                return Error.NotFound("Leave application not found");

            if (application.EmployeeId != employeeId)
                return Error.Forbidden("You can only withdraw your own leave applications");

            if (application.Status != "pending")
                return Error.Validation("Only pending applications can be withdrawn");

            await _applicationRepository.UpdateStatusAsync(applicationId, "withdrawn", null, reason);
            return Result.Success();
        }

        public async Task<Result<LeaveCalculationDto>> CalculateLeaveDaysAsync(Guid companyId, DateTime fromDate, DateTime toDate)
        {
            var totalCalendarDays = (toDate - fromDate).Days + 1;
            var holidays = await _holidayRepository.GetByCompanyAndDateRangeAsync(companyId, fromDate, toDate);
            var holidayDates = holidays.Select(h => h.Date).ToList();

            var workingDays = 0;
            var weekends = 0;
            var holidayCount = 0;

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
                var isHoliday = holidayDates.Contains(date.Date);

                if (isWeekend)
                {
                    weekends++;
                }
                else if (isHoliday)
                {
                    holidayCount++;
                }
                else
                {
                    workingDays++;
                }
            }

            return Result<LeaveCalculationDto>.Success(new LeaveCalculationDto
            {
                FromDate = fromDate,
                ToDate = toDate,
                TotalDays = workingDays,
                WorkingDays = workingDays,
                Holidays = holidayCount,
                Weekends = weekends,
                HolidayDates = holidayDates
            });
        }

        // ==================== Holidays ====================

        public async Task<Result<IEnumerable<HolidayDto>>> GetHolidaysAsync(Guid companyId, int year)
        {
            var holidays = await _holidayRepository.GetByCompanyAndYearAsync(companyId, year);
            return Result<IEnumerable<HolidayDto>>.Success(holidays.Select(MapToHolidayDto));
        }

        public async Task<Result<HolidayDto>> GetHolidayByIdAsync(Guid id)
        {
            var holiday = await _holidayRepository.GetByIdAsync(id);
            if (holiday == null)
                return Error.NotFound("Holiday not found");
            return Result<HolidayDto>.Success(MapToHolidayDto(holiday));
        }

        public async Task<Result<HolidayDto>> CreateHolidayAsync(Guid companyId, CreateHolidayDto dto)
        {
            if (await _holidayRepository.ExistsAsync(companyId, dto.Date))
                return Error.Conflict("A holiday already exists on this date");

            var entity = new Holiday
            {
                CompanyId = companyId,
                Name = dto.Name,
                Date = dto.Date.Date,
                Year = dto.Date.Year,
                IsOptional = dto.IsOptional,
                Description = dto.Description
            };

            var created = await _holidayRepository.AddAsync(entity);
            return Result<HolidayDto>.Success(MapToHolidayDto(created));
        }

        public async Task<Result<HolidayDto>> UpdateHolidayAsync(Guid id, UpdateHolidayDto dto)
        {
            var entity = await _holidayRepository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound("Holiday not found");

            if (dto.Date.HasValue && dto.Date.Value.Date != entity.Date)
            {
                if (await _holidayRepository.ExistsAsync(entity.CompanyId, dto.Date.Value, id))
                    return Error.Conflict("A holiday already exists on this date");
                entity.Date = dto.Date.Value.Date;
                entity.Year = dto.Date.Value.Year;
            }

            if (dto.Name != null) entity.Name = dto.Name;
            if (dto.IsOptional.HasValue) entity.IsOptional = dto.IsOptional.Value;
            if (dto.Description != null) entity.Description = dto.Description;

            await _holidayRepository.UpdateAsync(entity);
            return Result<HolidayDto>.Success(MapToHolidayDto(entity));
        }

        public async Task<Result> DeleteHolidayAsync(Guid id)
        {
            var entity = await _holidayRepository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound("Holiday not found");

            await _holidayRepository.DeleteAsync(id);
            return Result.Success();
        }

        // ==================== Portal ====================

        public async Task<Result<LeaveDashboardDto>> GetEmployeeLeaveDashboardAsync(Guid employeeId, Guid companyId)
        {
            var financialYear = GetCurrentFinancialYear();
            var balancesResult = await GetEmployeeBalancesAsync(employeeId, financialYear);
            var applicationsResult = await GetEmployeeApplicationsAsync(employeeId);

            var upcomingLeaves = applicationsResult.IsSuccess
                ? applicationsResult.Value!.Where(a => a.FromDate > DateTime.UtcNow.Date && (a.Status == "approved" || a.Status == "pending")).Take(5).ToList()
                : new List<LeaveApplicationSummaryDto>();

            var pendingApplications = applicationsResult.IsSuccess
                ? applicationsResult.Value!.Where(a => a.Status == "pending").ToList()
                : new List<LeaveApplicationSummaryDto>();

            var holidays = await _holidayRepository.GetByCompanyAndDateRangeAsync(companyId, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddMonths(3));
            var upcomingHolidays = holidays.Select(MapToHolidayDto).ToList();

            return Result<LeaveDashboardDto>.Success(new LeaveDashboardDto
            {
                Balances = balancesResult.IsSuccess ? balancesResult.Value!.ToList() : new List<LeaveBalanceDto>(),
                UpcomingLeaves = upcomingLeaves,
                PendingApplications = pendingApplications,
                UpcomingHolidays = upcomingHolidays
            });
        }

        public async Task<Result<IEnumerable<LeaveCalendarEventDto>>> GetCalendarEventsAsync(Guid companyId, DateTime fromDate, DateTime toDate)
        {
            var events = new List<LeaveCalendarEventDto>();

            // Get approved leaves
            var leaves = await _applicationRepository.GetApprovedForDateRangeAsync(companyId, fromDate, toDate);
            foreach (var leave in leaves)
            {
                var employee = await _employeesRepository.GetByIdAsync(leave.EmployeeId);
                var leaveType = await _leaveTypeRepository.GetByIdAsync(leave.LeaveTypeId);

                events.Add(new LeaveCalendarEventDto
                {
                    Id = leave.Id,
                    Title = $"{employee?.EmployeeName} - {leaveType?.Code}",
                    Start = leave.FromDate,
                    End = leave.ToDate,
                    Color = leaveType?.ColorCode ?? "#3B82F6",
                    Type = "leave",
                    EmployeeName = employee?.EmployeeName,
                    LeaveTypeCode = leaveType?.Code
                });
            }

            // Get holidays
            var holidays = await _holidayRepository.GetByCompanyAndDateRangeAsync(companyId, fromDate, toDate);
            foreach (var holiday in holidays)
            {
                events.Add(new LeaveCalendarEventDto
                {
                    Id = holiday.Id,
                    Title = holiday.Name,
                    Start = holiday.Date,
                    End = holiday.Date,
                    Color = holiday.IsOptional ? "#F59E0B" : "#EF4444",
                    Type = "holiday"
                });
            }

            return Result<IEnumerable<LeaveCalendarEventDto>>.Success(events.OrderBy(e => e.Start));
        }

        // ==================== Helper Methods ====================

        private static string GetCurrentFinancialYear()
        {
            var now = DateTime.UtcNow;
            return now.Month >= 4
                ? $"{now.Year}-{(now.Year + 1) % 100:D2}"
                : $"{now.Year - 1}-{now.Year % 100:D2}";
        }

        private static string GetFinancialYearForDate(DateTime date)
        {
            return date.Month >= 4
                ? $"{date.Year}-{(date.Year + 1) % 100:D2}"
                : $"{date.Year - 1}-{date.Year % 100:D2}";
        }

        private static LeaveTypeDto MapToLeaveTypeDto(LeaveType entity)
        {
            return new LeaveTypeDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Code = entity.Code,
                Description = entity.Description,
                DaysPerYear = entity.DaysPerYear,
                CarryForwardAllowed = entity.CarryForwardAllowed,
                MaxCarryForwardDays = entity.MaxCarryForwardDays,
                EncashmentAllowed = entity.EncashmentAllowed,
                MaxEncashmentDays = entity.MaxEncashmentDays,
                RequiresApproval = entity.RequiresApproval,
                MinDaysNotice = entity.MinDaysNotice,
                MaxConsecutiveDays = entity.MaxConsecutiveDays,
                IsActive = entity.IsActive,
                ColorCode = entity.ColorCode,
                SortOrder = entity.SortOrder
            };
        }

        private static LeaveBalanceDto MapToLeaveBalanceDto(EmployeeLeaveBalance balance, LeaveType leaveType)
        {
            return new LeaveBalanceDto
            {
                Id = balance.Id,
                LeaveTypeId = leaveType.Id,
                LeaveTypeName = leaveType.Name,
                LeaveTypeCode = leaveType.Code,
                ColorCode = leaveType.ColorCode,
                FinancialYear = balance.FinancialYear,
                OpeningBalance = balance.OpeningBalance,
                Accrued = balance.Accrued,
                Taken = balance.Taken,
                CarryForwarded = balance.CarryForwarded,
                Adjusted = balance.Adjusted,
                Encashed = balance.Encashed,
                AvailableBalance = balance.AvailableBalance,
                TotalCredited = balance.TotalCredited
            };
        }

        private static LeaveApplicationSummaryDto MapToLeaveApplicationSummary(
            LeaveApplication app,
            Core.Entities.Employees? employee,
            LeaveType? leaveType,
            Core.Entities.Employees? approver)
        {
            return new LeaveApplicationSummaryDto
            {
                Id = app.Id,
                EmployeeId = app.EmployeeId,
                EmployeeName = employee?.EmployeeName ?? string.Empty,
                EmployeeCode = employee?.EmployeeId,
                LeaveTypeName = leaveType?.Name ?? string.Empty,
                LeaveTypeCode = leaveType?.Code ?? string.Empty,
                LeaveTypeColor = leaveType?.ColorCode ?? "#3B82F6",
                FromDate = app.FromDate,
                ToDate = app.ToDate,
                TotalDays = app.TotalDays,
                IsHalfDay = app.IsHalfDay,
                HalfDayType = app.HalfDayType,
                Status = app.Status,
                AppliedAt = app.AppliedAt,
                ApprovedByName = approver?.EmployeeName,
                ApprovedAt = app.ApprovedAt
            };
        }

        private static LeaveApplicationDetailDto MapToLeaveApplicationDetail(
            LeaveApplication app,
            Core.Entities.Employees? employee,
            LeaveType? leaveType,
            Core.Entities.Employees? approver)
        {
            return new LeaveApplicationDetailDto
            {
                Id = app.Id,
                EmployeeId = app.EmployeeId,
                EmployeeName = employee?.EmployeeName ?? string.Empty,
                EmployeeCode = employee?.EmployeeId,
                Department = employee?.Department,
                LeaveTypeId = app.LeaveTypeId,
                LeaveTypeName = leaveType?.Name ?? string.Empty,
                LeaveTypeCode = leaveType?.Code ?? string.Empty,
                LeaveTypeColor = leaveType?.ColorCode ?? "#3B82F6",
                FromDate = app.FromDate,
                ToDate = app.ToDate,
                TotalDays = app.TotalDays,
                IsHalfDay = app.IsHalfDay,
                HalfDayType = app.HalfDayType,
                Reason = app.Reason,
                Status = app.Status,
                AppliedAt = app.AppliedAt,
                ApprovedBy = app.ApprovedBy,
                ApprovedByName = approver?.EmployeeName,
                ApprovedAt = app.ApprovedAt,
                RejectionReason = app.RejectionReason,
                CancelledAt = app.CancelledAt,
                CancellationReason = app.CancellationReason,
                EmergencyContact = app.EmergencyContact,
                HandoverNotes = app.HandoverNotes,
                AttachmentUrl = app.AttachmentUrl,
                CanEdit = app.CanEdit,
                CanCancel = app.CanCancel,
                CanWithdraw = app.CanWithdraw
            };
        }

        private static HolidayDto MapToHolidayDto(Holiday entity)
        {
            return new HolidayDto
            {
                Id = entity.Id,
                Name = entity.Name,
                Date = entity.Date,
                Year = entity.Year,
                IsOptional = entity.IsOptional,
                Description = entity.Description,
                DayOfWeek = entity.Date.DayOfWeek.ToString()
            };
        }
    }
}
