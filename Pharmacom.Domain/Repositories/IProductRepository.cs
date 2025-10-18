using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Repositories
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<List<Product>> GetAllProductsWithCategoryAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task<PagedResult<Product>> GetProductsPagedAsync
            (
                int pageNumber,
                int pageSize,
                string? searchTerm = null,          // Search in Name, Brand, Description, GTIN
                int? categoryId = null,             // Filter by category
                string? brand = null,               // Filter by specific brand
                string? form = null,                // Filter by form (Tablet, Capsule, etc.)
                bool? isRxRequired = null,          // Filter by prescription requirement
                decimal? minPrice = null,           // Price range minimum
                decimal? maxPrice = null,           // Price range maximum
                string sortBy = "Name",             // Sort field: Name, Price, Newest
                bool sortDescending = false         // Sort direction
            );
        Task<Product?> GetProductWithCategoryAsync(int id);

        // Helper methods for dropdowns
        Task<IEnumerable<string>> GetAllBrandsAsync();
        Task<IEnumerable<string>> GetAllFormsAsync();
    }
}
