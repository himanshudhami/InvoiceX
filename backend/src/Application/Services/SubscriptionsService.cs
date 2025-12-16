using Application.Common;
using Application.DTOs.Subscriptions;
using Application.Interfaces;
using Application.Validators.Subscriptions;
using AutoMapper;
using Core.Common;
using Core.Entities;
using Core.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SubscriptionsEntity = Core.Entities.Subscriptions;

namespace Application.Services;

public class SubscriptionsService : ISubscriptionsService
{
    private readonly ISubscriptionsRepository _repository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateSubscriptionDto> _createValidator;
    private readonly IValidator<UpdateSubscriptionDto> _updateValidator;
    private readonly IValidator<CreateSubscriptionAssignmentDto> _createAssignmentValidator;
    private readonly IValidator<RevokeSubscriptionAssignmentDto> _revokeAssignmentValidator;

    public SubscriptionsService(
        ISubscriptionsRepository repository, 
        IMapper mapper,
        IValidator<CreateSubscriptionDto> createValidator,
        IValidator<UpdateSubscriptionDto> updateValidator,
        IValidator<CreateSubscriptionAssignmentDto> createAssignmentValidator,
        IValidator<RevokeSubscriptionAssignmentDto> revokeAssignmentValidator)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _createAssignmentValidator = createAssignmentValidator ?? throw new ArgumentNullException(nameof(createAssignmentValidator));
        _revokeAssignmentValidator = revokeAssignmentValidator ?? throw new ArgumentNullException(nameof(revokeAssignmentValidator));
    }

    public async Task<Result<SubscriptionsEntity>> GetByIdAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return Error.NotFound($"Subscription {id} not found");

        return Result<SubscriptionsEntity>.Success(entity);
    }

    public async Task<Result<(IEnumerable<SubscriptionsEntity> Items, int TotalCount)>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, string? sortBy = null, bool sortDescending = false, Dictionary<string, object>? filters = null)
    {
        var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
        if (validation.IsFailure)
            return validation.Error!;

        var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
        return Result<(IEnumerable<SubscriptionsEntity> Items, int TotalCount)>.Success(result);
    }

    public async Task<Result<SubscriptionsEntity>> CreateAsync(CreateSubscriptionDto dto)
    {
        var validation = await ValidationHelper.ValidateAsync(_createValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var entity = _mapper.Map<SubscriptionsEntity>(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var created = await _repository.AddAsync(entity);
        return Result<SubscriptionsEntity>.Success(created);
    }

    public async Task<Result> UpdateAsync(Guid id, UpdateSubscriptionDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_updateValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Subscription not found");

        _mapper.Map(dto, existing);
        existing.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existing);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Guid id)
    {
        var validation = ServiceExtensions.ValidateGuid(id);
        if (validation.IsFailure)
            return validation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Subscription not found");

        await _repository.DeleteAsync(id);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<SubscriptionAssignments>>> GetAssignmentsAsync(Guid subscriptionId)
    {
        var validation = ServiceExtensions.ValidateGuid(subscriptionId, "SubscriptionId");
        if (validation.IsFailure)
            return validation.Error!;

        var items = await _repository.GetAssignmentsAsync(subscriptionId);
        return Result<IEnumerable<SubscriptionAssignments>>.Success(items);
    }

    public async Task<Result<SubscriptionAssignments>> AddAssignmentAsync(Guid subscriptionId, CreateSubscriptionAssignmentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(subscriptionId, "SubscriptionId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_createAssignmentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        var sub = await _repository.GetByIdAsync(subscriptionId);
        if (sub == null)
            return Error.NotFound("Subscription not found");

        var assignment = new SubscriptionAssignments
        {
            SubscriptionId = subscriptionId,
            CompanyId = dto.CompanyId,
            EmployeeId = dto.EmployeeId,
            TargetType = dto.TargetType,
            SeatIdentifier = dto.SeatIdentifier,
            Role = dto.Role,
            AssignedOn = dto.AssignedOn ?? DateTime.UtcNow.Date,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repository.AddAssignmentAsync(assignment);
        return Result<SubscriptionAssignments>.Success(created);
    }

    public async Task<Result> RevokeAssignmentAsync(Guid assignmentId, RevokeSubscriptionAssignmentDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(assignmentId, "AssignmentId");
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var validation = await ValidationHelper.ValidateAsync(_revokeAssignmentValidator, dto);
        if (validation.IsFailure)
            return validation.Error!;

        await _repository.RevokeAssignmentAsync(assignmentId, dto.RevokedOn ?? DateTime.UtcNow.Date);
        return Result.Success();
    }

    public async Task<Result> PauseSubscriptionAsync(Guid id, PauseSubscriptionDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Subscription not found");

        if (existing.Status != "active")
            return Error.Validation("Only active subscriptions can be paused");

        await _repository.PauseSubscriptionAsync(id, dto.PausedOn ?? DateTime.UtcNow.Date);
        return Result.Success();
    }

    public async Task<Result> ResumeSubscriptionAsync(Guid id, ResumeSubscriptionDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Subscription not found");

        if (existing.Status != "on_hold")
            return Error.Validation("Only paused subscriptions can be resumed");

        await _repository.ResumeSubscriptionAsync(id, dto.ResumedOn ?? DateTime.UtcNow.Date);
        return Result.Success();
    }

    public async Task<Result> CancelSubscriptionAsync(Guid id, CancelSubscriptionDto dto)
    {
        var idValidation = ServiceExtensions.ValidateGuid(id);
        if (idValidation.IsFailure)
            return idValidation.Error!;

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound("Subscription not found");

        if (existing.Status == "cancelled")
            return Error.Validation("Subscription is already cancelled");

        await _repository.CancelSubscriptionAsync(id, dto.CancelledOn ?? DateTime.UtcNow.Date);
        return Result.Success();
    }
}
