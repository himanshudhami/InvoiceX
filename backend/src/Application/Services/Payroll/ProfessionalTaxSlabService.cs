using Application.DTOs.Payroll;
using Application.Interfaces.Payroll;
using Core.Common;
using Core.Entities.Payroll;
using Core.Interfaces.Payroll;

namespace Application.Services.Payroll;

/// <summary>
/// Service for managing Professional Tax slabs
/// </summary>
public class ProfessionalTaxSlabService : IProfessionalTaxSlabService
{
    private readonly IProfessionalTaxSlabRepository _repository;

    public ProfessionalTaxSlabService(IProfessionalTaxSlabRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> GetAllAsync(string? state = null)
    {
        IEnumerable<ProfessionalTaxSlab> slabs;

        if (!string.IsNullOrWhiteSpace(state))
        {
            slabs = await _repository.GetByStateAsync(state);
        }
        else
        {
            slabs = await _repository.GetAllAsync();
        }

        var dtos = slabs.Select(MapToDto);
        return Result<IEnumerable<ProfessionalTaxSlabDto>>.Success(dtos);
    }

    public async Task<Result<ProfessionalTaxSlabDto>> GetByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Error.Validation("ID cannot be empty");

        var slab = await _repository.GetByIdAsync(id);
        if (slab == null)
            return Error.NotFound($"Professional Tax slab with ID {id} not found");

        return Result<ProfessionalTaxSlabDto>.Success(MapToDto(slab));
    }

    public async Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> GetByStateAsync(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Error.Validation("State is required");

        var slabs = await _repository.GetByStateAsync(state);
        var dtos = slabs.Select(MapToDto);
        return Result<IEnumerable<ProfessionalTaxSlabDto>>.Success(dtos);
    }

    public async Task<Result<ProfessionalTaxSlabDto?>> GetSlabForIncomeAsync(decimal monthlyIncome, string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return Error.Validation("State is required");

        if (monthlyIncome < 0)
            return Error.Validation("Monthly income cannot be negative");

        var slab = await _repository.GetSlabForIncomeAsync(monthlyIncome, state);
        return Result<ProfessionalTaxSlabDto?>.Success(slab != null ? MapToDto(slab) : null);
    }

    public async Task<Result<IEnumerable<string>>> GetDistinctStatesAsync()
    {
        var states = await _repository.GetDistinctStatesAsync();
        return Result<IEnumerable<string>>.Success(states);
    }

    public async Task<Result<ProfessionalTaxSlabDto>> CreateAsync(CreateProfessionalTaxSlabDto dto)
    {
        // Validate
        var validationResult = ValidateCreateDto(dto);
        if (validationResult.IsFailure)
            return Error.Validation(validationResult.Error!.Message);

        // Check for overlapping slabs
        var overlaps = await _repository.ExistsForStateAndRangeAsync(
            dto.State,
            dto.MinMonthlyIncome,
            dto.MaxMonthlyIncome);

        if (overlaps)
            return Error.Conflict($"A PT slab already exists for {dto.State} in the specified income range");

        var entity = new ProfessionalTaxSlab
        {
            State = dto.State.Trim(),
            MinMonthlyIncome = dto.MinMonthlyIncome,
            MaxMonthlyIncome = dto.MaxMonthlyIncome,
            MonthlyTax = dto.MonthlyTax,
            FebruaryTax = dto.FebruaryTax,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsActive = dto.IsActive
        };

        var created = await _repository.AddAsync(entity);
        return Result<ProfessionalTaxSlabDto>.Success(MapToDto(created));
    }

    public async Task<Result<bool>> UpdateAsync(Guid id, UpdateProfessionalTaxSlabDto dto)
    {
        if (id == Guid.Empty)
            return Error.Validation("ID cannot be empty");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound($"Professional Tax slab with ID {id} not found");

        // Validate
        var validationResult = ValidateUpdateDto(dto);
        if (validationResult.IsFailure)
            return Error.Validation(validationResult.Error!.Message);

        // Check for overlapping slabs (excluding current slab)
        var overlaps = await _repository.ExistsForStateAndRangeAsync(
            dto.State,
            dto.MinMonthlyIncome,
            dto.MaxMonthlyIncome,
            id);

        if (overlaps)
            return Error.Conflict($"A PT slab already exists for {dto.State} in the specified income range");

        existing.State = dto.State.Trim();
        existing.MinMonthlyIncome = dto.MinMonthlyIncome;
        existing.MaxMonthlyIncome = dto.MaxMonthlyIncome;
        existing.MonthlyTax = dto.MonthlyTax;
        existing.FebruaryTax = dto.FebruaryTax;
        existing.EffectiveFrom = dto.EffectiveFrom;
        existing.EffectiveTo = dto.EffectiveTo;
        existing.IsActive = dto.IsActive;

        await _repository.UpdateAsync(existing);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(Guid id)
    {
        if (id == Guid.Empty)
            return Error.Validation("ID cannot be empty");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return Error.NotFound($"Professional Tax slab with ID {id} not found");

        await _repository.DeleteAsync(id);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IEnumerable<ProfessionalTaxSlabDto>>> BulkCreateAsync(IEnumerable<CreateProfessionalTaxSlabDto> dtos)
    {
        var entities = new List<ProfessionalTaxSlab>();

        foreach (var dto in dtos)
        {
            var validationResult = ValidateCreateDto(dto);
            if (validationResult.IsFailure)
                return Error.Validation($"Validation failed for slab: {validationResult.Error!.Message}");

            entities.Add(new ProfessionalTaxSlab
            {
                State = dto.State.Trim(),
                MinMonthlyIncome = dto.MinMonthlyIncome,
                MaxMonthlyIncome = dto.MaxMonthlyIncome,
                MonthlyTax = dto.MonthlyTax,
                FebruaryTax = dto.FebruaryTax,
                EffectiveFrom = dto.EffectiveFrom,
                EffectiveTo = dto.EffectiveTo,
                IsActive = dto.IsActive
            });
        }

        var created = await _repository.BulkAddAsync(entities);
        return Result<IEnumerable<ProfessionalTaxSlabDto>>.Success(created.Select(MapToDto));
    }

    private static Result<bool> ValidateCreateDto(CreateProfessionalTaxSlabDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.State))
            return Error.Validation("State is required");

        if (dto.MinMonthlyIncome < 0)
            return Error.Validation("Minimum income cannot be negative");

        if (dto.MaxMonthlyIncome.HasValue && dto.MaxMonthlyIncome.Value <= dto.MinMonthlyIncome)
            return Error.Validation("Maximum income must be greater than minimum income");

        if (dto.MonthlyTax < 0)
            return Error.Validation("Monthly tax cannot be negative");

        if (dto.FebruaryTax.HasValue && dto.FebruaryTax.Value < 0)
            return Error.Validation("February tax cannot be negative");

        return Result<bool>.Success(true);
    }

    private static Result<bool> ValidateUpdateDto(UpdateProfessionalTaxSlabDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.State))
            return Error.Validation("State is required");

        if (dto.MinMonthlyIncome < 0)
            return Error.Validation("Minimum income cannot be negative");

        if (dto.MaxMonthlyIncome.HasValue && dto.MaxMonthlyIncome.Value <= dto.MinMonthlyIncome)
            return Error.Validation("Maximum income must be greater than minimum income");

        if (dto.MonthlyTax < 0)
            return Error.Validation("Monthly tax cannot be negative");

        if (dto.FebruaryTax.HasValue && dto.FebruaryTax.Value < 0)
            return Error.Validation("February tax cannot be negative");

        return Result<bool>.Success(true);
    }

    private static ProfessionalTaxSlabDto MapToDto(ProfessionalTaxSlab entity)
    {
        return new ProfessionalTaxSlabDto
        {
            Id = entity.Id,
            State = entity.State,
            MinMonthlyIncome = entity.MinMonthlyIncome,
            MaxMonthlyIncome = entity.MaxMonthlyIncome,
            MonthlyTax = entity.MonthlyTax,
            FebruaryTax = entity.FebruaryTax,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
