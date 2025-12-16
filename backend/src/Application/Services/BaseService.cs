using Application.Common;
using Application.Interfaces;
using AutoMapper;
using Core.Common;
using Core.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Base service implementation providing common CRUD operations
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TCreateDto">The DTO type for creation</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type for updates</typeparam>
    /// <typeparam name="TRepository">The repository interface type</typeparam>
    public abstract class BaseService<TEntity, TCreateDto, TUpdateDto, TRepository> 
        : IBaseService<TEntity, TCreateDto, TUpdateDto>
        where TEntity : class
        where TRepository : IRepository<TEntity>
    {
        protected readonly TRepository Repository;
        protected readonly IMapper Mapper;
        protected readonly IValidator<TCreateDto> CreateValidator;
        protected readonly IValidator<TUpdateDto> UpdateValidator;

        protected BaseService(
            TRepository repository,
            IMapper mapper,
            IValidator<TCreateDto> createValidator,
            IValidator<TUpdateDto> updateValidator)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            CreateValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
            UpdateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        }

        /// <inheritdoc />
        public virtual async Task<Result<TEntity>> GetByIdAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = await Repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            return Result<TEntity>.Success(entity);
        }

        /// <inheritdoc />
        public virtual async Task<Result<IEnumerable<TEntity>>> GetAllAsync()
        {
            var entities = await Repository.GetAllAsync();
            return Result<IEnumerable<TEntity>>.Success(entities);
        }

        /// <inheritdoc />
        public virtual async Task<Result<(IEnumerable<TEntity> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            var validation = ServiceExtensions.ValidatePagination(pageNumber, pageSize);
            if (validation.IsFailure)
                return validation.Error!;

            var result = await Repository.GetPagedAsync(
                pageNumber,
                pageSize,
                searchTerm,
                sortBy,
                sortDescending,
                filters);

            return Result<(IEnumerable<TEntity> Items, int TotalCount)>.Success(result);
        }

        /// <inheritdoc />
        public virtual async Task<Result<TEntity>> CreateAsync(TCreateDto dto)
        {
            var validation = await ValidationHelper.ValidateAsync(CreateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var entity = Mapper.Map<TEntity>(dto);
            
            // Set timestamps if entity supports them
            SetCreatedTimestamp(entity);
            
            var createdEntity = await Repository.AddAsync(entity);
            return Result<TEntity>.Success(createdEntity);
        }

        /// <inheritdoc />
        public virtual async Task<Result> UpdateAsync(Guid id, TUpdateDto dto)
        {
            var idValidation = ServiceExtensions.ValidateGuid(id);
            if (idValidation.IsFailure)
                return idValidation.Error!;

            var validation = await ValidationHelper.ValidateAsync(UpdateValidator, dto);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await Repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            Mapper.Map(dto, existingEntity);
            
            // Set updated timestamp if entity supports it
            SetUpdatedTimestamp(existingEntity);

            await Repository.UpdateAsync(existingEntity);
            return Result.Success();
        }

        /// <inheritdoc />
        public virtual async Task<Result> DeleteAsync(Guid id)
        {
            var validation = ServiceExtensions.ValidateGuid(id);
            if (validation.IsFailure)
                return validation.Error!;

            var existingEntity = await Repository.GetByIdAsync(id);
            if (existingEntity == null)
                return Error.NotFound($"{typeof(TEntity).Name} with ID {id} not found");

            await Repository.DeleteAsync(id);
            return Result.Success();
        }

        /// <summary>
        /// Sets the CreatedAt timestamp on the entity if it has such a property
        /// </summary>
        protected virtual void SetCreatedTimestamp(TEntity entity)
        {
            var createdAtProperty = typeof(TEntity).GetProperty("CreatedAt");
            if (createdAtProperty != null && createdAtProperty.PropertyType == typeof(DateTime?) && createdAtProperty.GetValue(entity) == null)
            {
                createdAtProperty.SetValue(entity, DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Sets the UpdatedAt timestamp on the entity if it has such a property
        /// </summary>
        protected virtual void SetUpdatedTimestamp(TEntity entity)
        {
            var updatedAtProperty = typeof(TEntity).GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(DateTime?))
            {
                updatedAtProperty.SetValue(entity, DateTime.UtcNow);
            }
        }
    }
}





