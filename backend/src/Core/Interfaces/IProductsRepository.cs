using Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IProductsRepository
    {
        Task<Products?> GetByIdAsync(Guid id);
        Task<IEnumerable<Products>> GetAllAsync();
        Task<(IEnumerable<Products> Items, int TotalCount)> GetPagedAsync(
            int pageNumber, 
            int pageSize, 
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            Dictionary<string, object>? filters = null);
        Task<Products> AddAsync(Products entity);
        Task UpdateAsync(Products entity);
        Task DeleteAsync(Guid id);
    }
}