using Application.Interfaces;
using Application.DTOs.QuoteItems;
using Core.Entities;
using Core.Interfaces;
using Core.Common;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service implementation for QuoteItems operations
    /// </summary>
    public class QuoteItemsService : IQuoteItemsService
    {
        private readonly IQuoteItemsRepository _repository;
        private readonly IMapper _mapper;

        public QuoteItemsService(IQuoteItemsRepository repository, IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <inheritdoc />
        public async Task<Result<QuoteItems>> GetByIdAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Error.NotFound($"Quote item with ID {id} not found");

            return Result<QuoteItems>.Success(entity);
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<QuoteItems>>> GetAllAsync()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return Result<IEnumerable<QuoteItems>>.Success(entities);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve quote items: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<(IEnumerable<QuoteItems> Items, int TotalCount)>> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null)
        {
            try
            {
                var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, sortBy, sortDescending, filters);
                return Result<(IEnumerable<QuoteItems> Items, int TotalCount)>.Success(result);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to retrieve paged quote items: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<QuoteItems>> CreateAsync(CreateQuoteItemsDto dto)
        {
            try
            {
                var entity = _mapper.Map<QuoteItems>(dto);
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;

                var createdEntity = await _repository.AddAsync(entity);
                return Result<QuoteItems>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to create quote item: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> UpdateAsync(Guid id, UpdateQuoteItemsDto dto)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var existingEntity = await _repository.GetByIdAsync(id);
                if (existingEntity == null)
                    return Error.NotFound($"Quote item with ID {id} not found");

                _mapper.Map(dto, existingEntity);
                existingEntity.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existingEntity);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to update quote item: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            if (id == default(Guid))
                return Error.Validation("ID cannot be default value");

            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return Error.NotFound($"Quote item with ID {id} not found");

                await _repository.DeleteAsync(id);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Error.Internal($"Failed to delete quote item: {ex.Message}");
            }
        }
    }
}
