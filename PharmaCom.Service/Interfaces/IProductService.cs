using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<IEnumerable<Product>> GetAllProductsWithCategoryAsync();
        Task<PagedResult<Product>> GetProductsPagedAsync
            (
                int pageNumber,
                int pageSize,
                string? searchTerm = null,
                int? categoryId = null,
                string? brand = null,
                string? form = null,
                bool? isRxRequired = null,
                decimal? minPrice = null,
                decimal? maxPrice = null,
                string sortBy = "Name",
                bool sortDescending = false
            );
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductWithCategoryAsync(int id);
        Task<Product> CreateProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int id);
        Task<bool> ProductExistsAsync(int id);

        // Helpers
        Task<IEnumerable<string>> GetAllBrandsAsync();
        Task<IEnumerable<string>> GetAllFormsAsync();
    }
}
