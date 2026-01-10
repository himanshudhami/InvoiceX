using Application.DTOs.Expense;
using Application.Interfaces.Approval;
using Application.Interfaces.Audit;
using Application.Interfaces.Expense;
using Core.Common;
using Core.Entities.Expense;
using Core.Entities.Ledger;
using Core.Interfaces.Expense;
using Core.Interfaces.Ledger;
using Microsoft.Extensions.Logging;

namespace Application.Services.Expense
{
    /// <summary>
    /// Application service for expense claim operations.
    /// Enhanced for Indian GST compliance with ITC (Input Tax Credit) tracking.
    /// </summary>
    public class ExpenseClaimService : IExpenseClaimService
    {
        private readonly IExpenseClaimRepository _claimRepository;
        private readonly IExpenseCategoryRepository _categoryRepository;
        private readonly IExpenseAttachmentRepository _attachmentRepository;
        private readonly IApprovalWorkflowService _workflowService;
        private readonly IGstInputCreditRepository _gstInputCreditRepository;
        private readonly IExpensePostingService _expensePostingService;
        private readonly IAuditService _auditService;
        private readonly ILogger<ExpenseClaimService> _logger;

        public ExpenseClaimService(
            IExpenseClaimRepository claimRepository,
            IExpenseCategoryRepository categoryRepository,
            IExpenseAttachmentRepository attachmentRepository,
            IApprovalWorkflowService workflowService,
            IGstInputCreditRepository gstInputCreditRepository,
            IExpensePostingService expensePostingService,
            IAuditService auditService,
            ILogger<ExpenseClaimService> logger)
        {
            _claimRepository = claimRepository ?? throw new ArgumentNullException(nameof(claimRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _attachmentRepository = attachmentRepository ?? throw new ArgumentNullException(nameof(attachmentRepository));
            _workflowService = workflowService ?? throw new ArgumentNullException(nameof(workflowService));
            _gstInputCreditRepository = gstInputCreditRepository ?? throw new ArgumentNullException(nameof(gstInputCreditRepository));
            _expensePostingService = expensePostingService ?? throw new ArgumentNullException(nameof(expensePostingService));
            _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExpenseClaimDto>> GetByIdAsync(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Error.Validation("Expense claim ID cannot be empty");
            }

            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound($"Expense claim with ID {id} not found");
            }

            var dto = MapToDto(claim);
            dto.Attachments = (await _attachmentRepository.GetByExpenseAsync(id))
                .Select(MapAttachmentToDto);
            dto.AttachmentCount = dto.Attachments?.Count() ?? 0;

            return Result<ExpenseClaimDto>.Success(dto);
        }

        public async Task<Result<IEnumerable<ExpenseClaimDto>>> GetByEmployeeAsync(Guid employeeId)
        {
            if (employeeId == Guid.Empty)
            {
                return Error.Validation("Employee ID cannot be empty");
            }

            var claims = await _claimRepository.GetByEmployeeAsync(employeeId);
            return Result<IEnumerable<ExpenseClaimDto>>.Success(claims.Select(MapToDto));
        }

        public async Task<Result<(IEnumerable<ExpenseClaimDto> Items, int TotalCount)>> GetPagedAsync(
            Guid companyId,
            ExpenseClaimFilterRequest request)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var pageNumber = Math.Max(1, request.PageNumber);
            var pageSize = Math.Clamp(request.PageSize, 1, 100);

            var (items, totalCount) = await _claimRepository.GetPagedAsync(
                companyId, pageNumber, pageSize,
                request.SearchTerm, request.Status, request.EmployeeId,
                request.CategoryId, request.FromDate, request.ToDate);

            return Result<(IEnumerable<ExpenseClaimDto>, int)>.Success(
                (items.Select(MapToDto), totalCount));
        }

        public async Task<Result<(IEnumerable<ExpenseClaimDto> Items, int TotalCount)>> GetPagedByEmployeeAsync(
            Guid employeeId,
            int pageNumber,
            int pageSize,
            string? status = null)
        {
            if (employeeId == Guid.Empty)
            {
                return Error.Validation("Employee ID cannot be empty");
            }

            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var (items, totalCount) = await _claimRepository.GetPagedByEmployeeAsync(
                employeeId, pageNumber, pageSize, status);

            return Result<(IEnumerable<ExpenseClaimDto>, int)>.Success(
                (items.Select(MapToDto), totalCount));
        }

        public async Task<Result<IEnumerable<ExpenseClaimDto>>> GetPendingForManagerAsync(Guid managerId)
        {
            if (managerId == Guid.Empty)
            {
                return Error.Validation("Manager ID cannot be empty");
            }

            var claims = await _claimRepository.GetPendingForManagerAsync(managerId);
            return Result<IEnumerable<ExpenseClaimDto>>.Success(claims.Select(MapToDto));
        }

        public async Task<Result<ExpenseClaimDto>> CreateAsync(
            Guid companyId, Guid employeeId, CreateExpenseClaimDto dto)
        {
            if (companyId == Guid.Empty || employeeId == Guid.Empty)
            {
                return Error.Validation("Company ID and Employee ID are required");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Error.Validation("Title is required");
            }

            if (dto.Amount <= 0)
            {
                return Error.Validation("Amount must be greater than zero");
            }

            // Validate category exists and belongs to company
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null)
            {
                return Error.NotFound("Expense category not found");
            }
            if (category.CompanyId != companyId)
            {
                return Error.Validation("Category does not belong to this company");
            }
            if (!category.IsActive)
            {
                return Error.Validation("Category is inactive");
            }

            // Validate max amount
            if (category.MaxAmount.HasValue && dto.Amount > category.MaxAmount.Value)
            {
                return Error.Validation($"Amount exceeds maximum allowed ({category.MaxAmount:N2})");
            }

            var claimNumber = await _claimRepository.GenerateClaimNumberAsync(companyId);

            // Calculate GST amounts based on supply type
            var cgstAmount = dto.CgstAmount;
            var sgstAmount = dto.SgstAmount;
            var igstAmount = dto.IgstAmount;
            var totalGst = cgstAmount + sgstAmount + igstAmount;
            var baseAmount = dto.BaseAmount ?? (dto.IsGstApplicable ? dto.Amount - totalGst : dto.Amount);
            var cgstRate = dto.GstRate / 2;
            var sgstRate = dto.GstRate / 2;
            var igstRate = dto.SupplyType == "inter_state" ? dto.GstRate : 0;

            var claim = new ExpenseClaim
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                EmployeeId = employeeId,
                ClaimNumber = claimNumber,
                Title = dto.Title.Trim(),
                Description = dto.Description?.Trim(),
                CategoryId = dto.CategoryId,
                ExpenseDate = dto.ExpenseDate,
                Amount = dto.Amount,
                Currency = dto.Currency ?? "INR",
                Status = ExpenseClaimStatus.Draft,
                // GST fields
                VendorName = dto.VendorName?.Trim(),
                VendorGstin = dto.VendorGstin?.Trim()?.ToUpperInvariant(),
                InvoiceNumber = dto.InvoiceNumber?.Trim(),
                InvoiceDate = dto.InvoiceDate,
                IsGstApplicable = dto.IsGstApplicable,
                SupplyType = dto.SupplyType ?? "intra_state",
                HsnSacCode = dto.HsnSacCode?.Trim(),
                GstRate = dto.GstRate,
                BaseAmount = baseAmount,
                CgstRate = dto.SupplyType == "intra_state" ? cgstRate : 0,
                CgstAmount = cgstAmount,
                SgstRate = dto.SupplyType == "intra_state" ? sgstRate : 0,
                SgstAmount = sgstAmount,
                IgstRate = igstRate,
                IgstAmount = igstAmount,
                TotalGstAmount = totalGst,
                ItcEligible = category.ItcEligible ?? true // From category settings
            };

            var created = await _claimRepository.AddAsync(claim);

            // Audit trail
            await _auditService.AuditCreateAsync(created, created.Id, companyId, created.ClaimNumber);

            _logger.LogInformation(
                "Expense claim created: {ClaimNumber} by employee {EmployeeId}",
                claimNumber, employeeId);

            return Result<ExpenseClaimDto>.Success(MapToDto(created));
        }

        public async Task<Result<ExpenseClaimDto>> UpdateAsync(
            Guid id, Guid employeeId, UpdateExpenseClaimDto dto)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only update your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft)
            {
                return Error.Validation("Only draft claims can be updated");
            }

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return Error.Validation("Title is required");
            }

            if (dto.Amount <= 0)
            {
                return Error.Validation("Amount must be greater than zero");
            }

            // Capture state before update for audit trail
            var oldClaim = new ExpenseClaim
            {
                Id = claim.Id,
                CompanyId = claim.CompanyId,
                EmployeeId = claim.EmployeeId,
                ClaimNumber = claim.ClaimNumber,
                Title = claim.Title,
                Description = claim.Description,
                CategoryId = claim.CategoryId,
                ExpenseDate = claim.ExpenseDate,
                Amount = claim.Amount,
                Currency = claim.Currency,
                Status = claim.Status
            };

            // Validate category if changed
            if (dto.CategoryId != claim.CategoryId)
            {
                var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
                if (category == null || category.CompanyId != claim.CompanyId)
                {
                    return Error.Validation("Invalid category");
                }
                if (!category.IsActive)
                {
                    return Error.Validation("Category is inactive");
                }
                if (category.MaxAmount.HasValue && dto.Amount > category.MaxAmount.Value)
                {
                    return Error.Validation($"Amount exceeds maximum allowed ({category.MaxAmount:N2})");
                }
            }

            // Calculate GST amounts
            var totalGst = dto.CgstAmount + dto.SgstAmount + dto.IgstAmount;
            var baseAmount = dto.BaseAmount ?? (dto.IsGstApplicable ? dto.Amount - totalGst : dto.Amount);

            claim.Title = dto.Title.Trim();
            claim.Description = dto.Description?.Trim();
            claim.CategoryId = dto.CategoryId;
            claim.ExpenseDate = dto.ExpenseDate;
            claim.Amount = dto.Amount;
            claim.Currency = dto.Currency ?? "INR";
            // GST fields
            claim.VendorName = dto.VendorName?.Trim();
            claim.VendorGstin = dto.VendorGstin?.Trim()?.ToUpperInvariant();
            claim.InvoiceNumber = dto.InvoiceNumber?.Trim();
            claim.InvoiceDate = dto.InvoiceDate;
            claim.IsGstApplicable = dto.IsGstApplicable;
            claim.SupplyType = dto.SupplyType ?? "intra_state";
            claim.HsnSacCode = dto.HsnSacCode?.Trim();
            claim.GstRate = dto.GstRate;
            claim.BaseAmount = baseAmount;
            claim.CgstRate = dto.SupplyType == "intra_state" ? dto.GstRate / 2 : 0;
            claim.CgstAmount = dto.CgstAmount;
            claim.SgstRate = dto.SupplyType == "intra_state" ? dto.GstRate / 2 : 0;
            claim.SgstAmount = dto.SgstAmount;
            claim.IgstRate = dto.SupplyType == "inter_state" ? dto.GstRate : 0;
            claim.IgstAmount = dto.IgstAmount;
            claim.TotalGstAmount = totalGst;

            await _claimRepository.UpdateAsync(claim);

            // Audit trail
            await _auditService.AuditUpdateAsync(oldClaim, claim, claim.Id, claim.CompanyId, claim.ClaimNumber);

            // Re-fetch to get updated navigation properties
            var updated = await _claimRepository.GetByIdAsync(id);
            return Result<ExpenseClaimDto>.Success(MapToDto(updated!));
        }

        public async Task<Result<ExpenseClaimDto>> SubmitAsync(Guid id, Guid employeeId)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only submit your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft)
            {
                return Error.Validation("Only draft claims can be submitted");
            }

            // Check if receipt is required
            var category = await _categoryRepository.GetByIdAsync(claim.CategoryId);
            if (category?.RequiresReceipt == true)
            {
                var attachmentCount = await _attachmentRepository.GetCountAsync(id);
                if (attachmentCount == 0)
                {
                    return Error.Validation("This expense category requires at least one receipt attachment");
                }
            }

            claim.SubmittedAt = DateTime.UtcNow;

            // If category requires approval, start the approval workflow
            if (category?.RequiresApproval == true)
            {
                claim.Status = ExpenseClaimStatus.PendingApproval;

                // Create approvable activity wrapper
                var approvableActivity = new ExpenseClaimApprovableActivity(
                    claim,
                    category.Name,
                    onApproved: async () =>
                    {
                        // This is handled by ExpenseStatusHandler
                        await Task.CompletedTask;
                    },
                    onRejected: async (reason, rejectedBy) =>
                    {
                        // This is handled by ExpenseStatusHandler
                        await Task.CompletedTask;
                    },
                    onCancelled: async () =>
                    {
                        // This is handled by ExpenseStatusHandler
                        await Task.CompletedTask;
                    }
                );

                // Start the approval workflow
                var workflowResult = await _workflowService.StartWorkflowAsync(approvableActivity);

                if (workflowResult.IsSuccess && workflowResult.Value != null)
                {
                    claim.ApprovalRequestId = workflowResult.Value.Id;
                    _logger.LogInformation(
                        "Approval workflow started for expense {ClaimNumber}, RequestId: {RequestId}",
                        claim.ClaimNumber, workflowResult.Value.Id);
                }
                else
                {
                    // If no workflow template exists, fall back to direct manager approval
                    _logger.LogWarning(
                        "No approval workflow template found for expense {ClaimNumber}, using direct approval",
                        claim.ClaimNumber);
                }
            }
            else
            {
                // No approval required, auto-approve
                claim.Status = ExpenseClaimStatus.Approved;
                claim.ApprovedAt = DateTime.UtcNow;
            }

            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation(
                "Expense claim submitted: {ClaimNumber}, Status: {Status}",
                claim.ClaimNumber, claim.Status);

            var updated = await _claimRepository.GetByIdAsync(id);
            return Result<ExpenseClaimDto>.Success(MapToDto(updated!));
        }

        public async Task<Result<bool>> CancelAsync(Guid id, Guid employeeId)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only cancel your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft &&
                claim.Status != ExpenseClaimStatus.Submitted &&
                claim.Status != ExpenseClaimStatus.PendingApproval)
            {
                return Error.Validation("Only draft, submitted, or pending claims can be cancelled");
            }

            claim.Status = ExpenseClaimStatus.Cancelled;
            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation(
                "Expense claim cancelled: {ClaimNumber} by employee {EmployeeId}",
                claim.ClaimNumber, employeeId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<ExpenseClaimDto>> ApproveAsync(Guid id, Guid approverId)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.Status != ExpenseClaimStatus.PendingApproval &&
                claim.Status != ExpenseClaimStatus.Submitted)
            {
                return Error.Validation("Only pending claims can be approved");
            }

            claim.Status = ExpenseClaimStatus.Approved;
            claim.ApprovedAt = DateTime.UtcNow;
            claim.ApprovedBy = approverId;

            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation(
                "Expense claim approved: {ClaimNumber} by {ApproverId}",
                claim.ClaimNumber, approverId);

            var updated = await _claimRepository.GetByIdAsync(id);
            return Result<ExpenseClaimDto>.Success(MapToDto(updated!));
        }

        public async Task<Result<ExpenseClaimDto>> RejectAsync(
            Guid id, Guid rejecterId, RejectExpenseClaimDto dto)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.Status != ExpenseClaimStatus.PendingApproval &&
                claim.Status != ExpenseClaimStatus.Submitted)
            {
                return Error.Validation("Only pending claims can be rejected");
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Error.Validation("Rejection reason is required");
            }

            claim.Status = ExpenseClaimStatus.Rejected;
            claim.RejectedAt = DateTime.UtcNow;
            claim.RejectedBy = rejecterId;
            claim.RejectionReason = dto.Reason.Trim();

            await _claimRepository.UpdateAsync(claim);

            _logger.LogInformation(
                "Expense claim rejected: {ClaimNumber} by {RejecterId}",
                claim.ClaimNumber, rejecterId);

            var updated = await _claimRepository.GetByIdAsync(id);
            return Result<ExpenseClaimDto>.Success(MapToDto(updated!));
        }

        public async Task<Result<ExpenseClaimDto>> ReimburseAsync(Guid id, ReimburseExpenseClaimDto dto, Guid? reimbursedBy = null)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.Status != ExpenseClaimStatus.Approved)
            {
                return Error.Validation("Only approved claims can be reimbursed");
            }

            claim.Status = ExpenseClaimStatus.Reimbursed;
            claim.ReimbursedAt = DateTime.UtcNow;
            claim.ReimbursementReference = dto.Reference?.Trim();
            claim.ReimbursementNotes = dto.Notes?.Trim();

            await _claimRepository.UpdateAsync(claim);

            // Add reimbursement proof attachments if provided
            if (dto.ProofAttachmentIds != null && dto.ProofAttachmentIds.Any())
            {
                foreach (var fileStorageId in dto.ProofAttachmentIds)
                {
                    var attachment = new ExpenseAttachment
                    {
                        Id = Guid.NewGuid(),
                        ExpenseId = id,
                        FileStorageId = fileStorageId,
                        Description = "Reimbursement proof",
                        IsPrimary = false,
                        AttachmentType = ExpenseAttachmentType.ReimbursementProof,
                        UploadedBy = reimbursedBy,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _attachmentRepository.AddAsync(attachment);
                }
                _logger.LogInformation(
                    "Added {Count} reimbursement proof attachments for claim: {ClaimNumber}",
                    dto.ProofAttachmentIds.Count, claim.ClaimNumber);
            }

            _logger.LogInformation(
                "Expense claim reimbursed: {ClaimNumber}, Reference: {Reference}",
                claim.ClaimNumber, dto.Reference);

            // Post journal entry for the expense claim
            try
            {
                var journalEntry = await _expensePostingService.PostReimbursementAsync(claim.Id);
                if (journalEntry != null)
                {
                    _logger.LogInformation(
                        "Posted journal entry {JournalNumber} for expense claim: {ClaimNumber}",
                        journalEntry.JournalNumber, claim.ClaimNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Journal posting failed for expense claim: {ClaimNumber}. Reimbursement succeeded but journal entry not created.",
                    claim.ClaimNumber);
                // Don't fail the reimbursement if journal posting fails
            }

            // Create GST Input Credit record if GST is applicable and ITC is eligible
            if (claim.IsGstApplicable && claim.ItcEligible && claim.TotalGstAmount > 0)
            {
                await CreateGstInputCreditAsync(claim);
            }

            var updated = await _claimRepository.GetByIdAsync(id);
            return Result<ExpenseClaimDto>.Success(MapToDto(updated!));
        }

        /// <summary>
        /// Create GST Input Credit record for ITC tracking and GSTR filing.
        /// Per GST Act 2017 Sections 16-21.
        /// </summary>
        private async Task CreateGstInputCreditAsync(ExpenseClaim claim)
        {
            try
            {
                var expenseDate = DateOnly.FromDateTime(claim.ExpenseDate);
                var financialYear = GetFinancialYear(expenseDate);
                var returnPeriod = GetReturnPeriod(expenseDate);

                var gstInputCredit = new GstInputCredit
                {
                    CompanyId = claim.CompanyId,
                    FinancialYear = financialYear,
                    ReturnPeriod = returnPeriod,
                    SourceType = "expense_claim",
                    SourceId = claim.Id,
                    SourceNumber = claim.ClaimNumber,
                    VendorGstin = claim.VendorGstin,
                    VendorName = claim.VendorName,
                    VendorInvoiceNumber = claim.InvoiceNumber,
                    VendorInvoiceDate = claim.InvoiceDate,
                    SupplyType = claim.SupplyType,
                    HsnSacCode = claim.HsnSacCode,
                    TaxableValue = claim.BaseAmount ?? claim.Amount - claim.TotalGstAmount,
                    CgstRate = claim.CgstRate,
                    CgstAmount = claim.CgstAmount,
                    SgstRate = claim.SgstRate,
                    SgstAmount = claim.SgstAmount,
                    IgstRate = claim.IgstRate,
                    IgstAmount = claim.IgstAmount,
                    TotalGst = claim.TotalGstAmount,
                    ItcEligible = claim.ItcEligible,
                    Status = GstInputCreditStatus.Pending
                };

                await _gstInputCreditRepository.AddAsync(gstInputCredit);

                _logger.LogInformation(
                    "GST Input Credit created for expense claim {ClaimNumber}: GST ₹{GstAmount}",
                    claim.ClaimNumber, claim.TotalGstAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to create GST Input Credit for expense claim: {ClaimNumber}",
                    claim.ClaimNumber);
                // Don't fail the reimbursement if ITC creation fails
            }
        }

        /// <summary>
        /// Get financial year in format "2024-25" from a date.
        /// Indian FY starts April 1.
        /// </summary>
        private static string GetFinancialYear(DateOnly date)
        {
            var year = date.Month >= 4 ? date.Year : date.Year - 1;
            return $"{year}-{(year + 1) % 100:D2}";
        }

        /// <summary>
        /// Get GSTR return period in format "Jan-2025" from a date.
        /// </summary>
        private static string GetReturnPeriod(DateOnly date)
        {
            var monthName = date.ToString("MMM");
            return $"{monthName}-{date.Year}";
        }

        public async Task<Result<bool>> DeleteAsync(Guid id, Guid employeeId)
        {
            var claim = await _claimRepository.GetByIdAsync(id);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only delete your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft)
            {
                return Error.Validation("Only draft claims can be deleted");
            }

            // Audit trail before delete
            await _auditService.AuditDeleteAsync(claim, claim.Id, claim.CompanyId, claim.ClaimNumber);

            await _claimRepository.DeleteAsync(id);

            _logger.LogInformation(
                "Expense claim deleted: {ClaimNumber} by employee {EmployeeId}",
                claim.ClaimNumber, employeeId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<ExpenseAttachmentDto>> AddAttachmentAsync(
            Guid expenseId, Guid employeeId, AddExpenseAttachmentDto dto)
        {
            var claim = await _claimRepository.GetByIdAsync(expenseId);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only add attachments to your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft)
            {
                return Error.Validation("Attachments can only be added to draft claims");
            }

            var attachment = new ExpenseAttachment
            {
                Id = Guid.NewGuid(),
                ExpenseId = expenseId,
                FileStorageId = dto.FileStorageId,
                Description = dto.Description?.Trim(),
                IsPrimary = dto.IsPrimary
            };

            var created = await _attachmentRepository.AddAsync(attachment);

            if (dto.IsPrimary)
            {
                await _attachmentRepository.SetPrimaryAsync(expenseId, created.Id);
            }

            return Result<ExpenseAttachmentDto>.Success(MapAttachmentToDto(created));
        }

        public async Task<Result<bool>> RemoveAttachmentAsync(
            Guid expenseId, Guid attachmentId, Guid employeeId)
        {
            var claim = await _claimRepository.GetByIdAsync(expenseId);
            if (claim == null)
            {
                return Error.NotFound("Expense claim not found");
            }

            if (claim.EmployeeId != employeeId)
            {
                return Error.Forbidden("You can only remove attachments from your own expense claims");
            }

            if (claim.Status != ExpenseClaimStatus.Draft)
            {
                return Error.Validation("Attachments can only be removed from draft claims");
            }

            var attachment = await _attachmentRepository.GetByIdAsync(attachmentId);
            if (attachment == null || attachment.ExpenseId != expenseId)
            {
                return Error.NotFound("Attachment not found");
            }

            await _attachmentRepository.DeleteAsync(attachmentId);

            return Result<bool>.Success(true);
        }

        public async Task<Result<IEnumerable<ExpenseAttachmentDto>>> GetAttachmentsAsync(Guid expenseId)
        {
            var attachments = await _attachmentRepository.GetByExpenseAsync(expenseId);
            return Result<IEnumerable<ExpenseAttachmentDto>>.Success(
                attachments.Select(MapAttachmentToDto));
        }

        public async Task<Result<ExpenseSummaryDto>> GetSummaryAsync(
            Guid companyId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (companyId == Guid.Empty)
            {
                return Error.Validation("Company ID cannot be empty");
            }

            var summary = await _claimRepository.GetSummaryAsync(companyId, fromDate, toDate);

            return Result<ExpenseSummaryDto>.Success(new ExpenseSummaryDto
            {
                TotalClaims = summary.TotalClaims,
                DraftClaims = summary.DraftClaims,
                PendingClaims = summary.PendingClaims,
                ApprovedClaims = summary.ApprovedClaims,
                RejectedClaims = summary.RejectedClaims,
                ReimbursedClaims = summary.ReimbursedClaims,
                TotalAmount = summary.TotalAmount,
                PendingAmount = summary.PendingAmount,
                ApprovedAmount = summary.ApprovedAmount,
                ReimbursedAmount = summary.ReimbursedAmount,
                AmountByCategory = summary.AmountByCategory
            });
        }

        /// <summary>
        /// Updates expense claim status from approval workflow callback.
        /// Called by ExpenseStatusHandler when workflow completes.
        /// </summary>
        public async Task<Result> UpdateStatusFromWorkflowAsync(
            Guid expenseId,
            string status,
            Guid actionBy,
            string? reason = null)
        {
            var claim = await _claimRepository.GetByIdAsync(expenseId);
            if (claim == null)
            {
                return Error.NotFound($"Expense claim with ID {expenseId} not found");
            }

            switch (status.ToLowerInvariant())
            {
                case "approved":
                    claim.Status = ExpenseClaimStatus.Approved;
                    claim.ApprovedAt = DateTime.UtcNow;
                    claim.ApprovedBy = actionBy;
                    _logger.LogInformation(
                        "Expense claim {ClaimNumber} approved via workflow by {ApproverId}",
                        claim.ClaimNumber, actionBy);
                    break;

                case "rejected":
                    claim.Status = ExpenseClaimStatus.Rejected;
                    claim.RejectedAt = DateTime.UtcNow;
                    claim.RejectedBy = actionBy;
                    claim.RejectionReason = reason;
                    _logger.LogInformation(
                        "Expense claim {ClaimNumber} rejected via workflow by {RejecterId}: {Reason}",
                        claim.ClaimNumber, actionBy, reason);
                    break;

                case "cancelled":
                    claim.Status = ExpenseClaimStatus.Cancelled;
                    _logger.LogInformation(
                        "Expense claim {ClaimNumber} cancelled via workflow by {CancelledBy}",
                        claim.ClaimNumber, actionBy);
                    break;

                default:
                    return Error.Validation($"Invalid status: {status}");
            }

            await _claimRepository.UpdateAsync(claim);
            return Result.Success();
        }

        private static ExpenseClaimDto MapToDto(ExpenseClaim claim)
        {
            return new ExpenseClaimDto
            {
                Id = claim.Id,
                CompanyId = claim.CompanyId,
                EmployeeId = claim.EmployeeId,
                EmployeeName = claim.EmployeeName,
                ClaimNumber = claim.ClaimNumber,
                Title = claim.Title,
                Description = claim.Description,
                CategoryId = claim.CategoryId,
                CategoryName = claim.CategoryName,
                ExpenseDate = claim.ExpenseDate,
                Amount = claim.Amount,
                Currency = claim.Currency,
                Status = claim.Status,
                StatusDisplayName = ExpenseStatusDisplay.GetDisplayName(claim.Status),
                ApprovalRequestId = claim.ApprovalRequestId,
                SubmittedAt = claim.SubmittedAt,
                ApprovedAt = claim.ApprovedAt,
                ApprovedBy = claim.ApprovedBy,
                ApprovedByName = claim.ApprovedByName,
                RejectedAt = claim.RejectedAt,
                RejectedBy = claim.RejectedBy,
                RejectedByName = claim.RejectedByName,
                RejectionReason = claim.RejectionReason,
                ReimbursedAt = claim.ReimbursedAt,
                ReimbursementReference = claim.ReimbursementReference,
                ReimbursementNotes = claim.ReimbursementNotes,
                // GST fields
                VendorName = claim.VendorName,
                VendorGstin = claim.VendorGstin,
                InvoiceNumber = claim.InvoiceNumber,
                InvoiceDate = claim.InvoiceDate,
                IsGstApplicable = claim.IsGstApplicable,
                SupplyType = claim.SupplyType,
                HsnSacCode = claim.HsnSacCode,
                GstRate = claim.GstRate,
                BaseAmount = claim.BaseAmount,
                CgstRate = claim.CgstRate,
                CgstAmount = claim.CgstAmount,
                SgstRate = claim.SgstRate,
                SgstAmount = claim.SgstAmount,
                IgstRate = claim.IgstRate,
                IgstAmount = claim.IgstAmount,
                TotalGstAmount = claim.TotalGstAmount,
                ItcEligible = claim.ItcEligible,
                ItcClaimed = claim.ItcClaimed,
                ItcClaimedInReturn = claim.ItcClaimedInReturn,
                CreatedAt = claim.CreatedAt,
                UpdatedAt = claim.UpdatedAt
            };
        }

        private static ExpenseAttachmentDto MapAttachmentToDto(ExpenseAttachment attachment)
        {
            return new ExpenseAttachmentDto
            {
                Id = attachment.Id,
                ExpenseId = attachment.ExpenseId,
                FileStorageId = attachment.FileStorageId,
                Description = attachment.Description,
                IsPrimary = attachment.IsPrimary,
                OriginalFilename = attachment.OriginalFilename,
                MimeType = attachment.MimeType,
                FileSize = attachment.FileSize,
                DownloadUrl = attachment.DownloadUrl,
                CreatedAt = attachment.CreatedAt
            };
        }
    }
}
