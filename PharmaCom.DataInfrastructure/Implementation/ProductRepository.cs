using Microsoft.EntityFrameworkCore;
using PharmaCom.DataInfrastructure.Data;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.DataInfrastructure.Implementation
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        private readonly ApplicationDBContext _context;

        public ProductRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllProductsWithCategoryAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        public async Task<PagedResult<Product>> GetProductsPagedAsync(
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
            bool sortDescending = false)
        {
            // Start with base query including Category navigation
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // Search across Name, Brand, Description, and GTIN
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(searchLower) ||
                    p.Brand.ToLower().Contains(searchLower) ||
                    p.Description.ToLower().Contains(searchLower) ||
                    (p.GTIN != null && p.GTIN.ToLower().Contains(searchLower)));
            }

            // CATEGORY FILTER
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // BRAND FILTER
            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(p => p.Brand == brand);
            }

            // FORM FILTER 
            if (!string.IsNullOrWhiteSpace(form))
            {
                query = query.Where(p => p.Form == form);
            }

            // PRESCRIPTION FILTER
            if (isRxRequired.HasValue)
            {
                query = query.Where(p => p.IsRxRequired == isRxRequired.Value);
            }

            // PRICE RANGE FILTER 
            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            // GET TOTAL COUNT (Before pagination)
            var totalCount = await query.CountAsync();

            // SORTING 
            query = sortBy?.ToLower() switch
            {
                "price" => sortDescending
                    ? query.OrderByDescending(p => p.Price).ThenBy(p => p.Name)
                    : query.OrderBy(p => p.Price).ThenBy(p => p.Name),

                "newest" => query.OrderByDescending(p => p.Id),

                "name" => sortDescending
                    ? query.OrderByDescending(p => p.Name)
                    : query.OrderBy(p => p.Name),

                _ => query.OrderBy(p => p.Name) // Default: Name A-Z
            };

            // PAGINATION 
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // RETURN PAGED RESULT
            return new PagedResult<Product>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // Helper: Get all unique brands for dropdown
        public async Task<IEnumerable<string>> GetAllBrandsAsync()
        {
            return await _context.Products
                .Select(p => p.Brand)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        // Helper: Get all unique forms for dropdown
        public async Task<IEnumerable<string>> GetAllFormsAsync()
        {
            return await _context.Products
                .Select(p => p.Form)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }
    }
}
