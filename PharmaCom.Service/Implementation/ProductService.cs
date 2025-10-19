using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Implementation
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _unitOfWork.Product.GetAllAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductsWithCategoryAsync()
        {
            return await _unitOfWork.Product.GetAllProductsWithCategoryAsync();
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
            // VALIDATION 

            // Page number must be at least 1
            if (pageNumber < 1)
                pageNumber = 1;

            // Page size must be between 1 and 50 (prevent abuse)
            if (pageSize < 1)
                pageSize = 12; // Default
            else if (pageSize > 50)
                pageSize = 50; // Maximum

            // Trim search term
            searchTerm = searchTerm?.Trim();

            // Validate price range
            if (minPrice.HasValue && minPrice.Value < 0)
                minPrice = 0;

            if (maxPrice.HasValue && maxPrice.Value < 0)
                maxPrice = null;

            // If min > max, swap them
            if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            {
                (minPrice, maxPrice) = (maxPrice, minPrice);
            }

            // Validate sort field
            var validSortFields = new[] { "name", "price", "date" };
            if (!validSortFields.Contains(sortBy?.ToLower()))
                sortBy = "Name"; // default

            // CALL REPOSITORY 
            return await _unitOfWork.Product.GetProductsPagedAsync(
                pageNumber,
                pageSize,
                searchTerm,
                categoryId,
                brand,
                form,
                isRxRequired,
                minPrice,
                maxPrice,
                sortBy,
                sortDescending);
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _unitOfWork.Product.GetByIdAsync(id);
        }
        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _unitOfWork.Product.GetProductWithCategoryAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            await _unitOfWork.Product.AddAsync(product);
            _unitOfWork.Save();
            return product;
        }

        public async Task UpdateProductAsync(Product product)
        {
            _unitOfWork.Product.Update(product);
            _unitOfWork.Save();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Product.GetByIdAsync(id);
            if (product != null)
            {
                _unitOfWork.Product.Remove(product);
                _unitOfWork.Save();
            }
        }

        public async Task<bool> ProductExistsAsync(int id)
        {
            return await _unitOfWork.Product.GetByIdAsync(id) != null;
        }


        // Helpers
        public async Task<IEnumerable<string>> GetAllBrandsAsync()
        {
            return await _unitOfWork.Product.GetAllBrandsAsync();
        }

        public async Task<IEnumerable<string>> GetAllFormsAsync()
        {
            return await _unitOfWork.Product.GetAllFormsAsync();
        }
    }
}
