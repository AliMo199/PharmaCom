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

        public async Task<List<Product>?> GetAllProductWithCategoryAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
        }

        public async Task<Product?> GetProductWithCategoryAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        //public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm)
        //{
        //    return await _context.Products
        //        .Include(p => p.Category)
        //        .Where(p => p.Name.Contains(searchTerm) ||
        //                   p.Brand.Contains(searchTerm) ||
        //                   p.Description.Contains(searchTerm))
        //        .ToListAsync();
        //}

        //public async Task<IEnumerable<Product>> GetPrescriptionRequiredProductsAsync()
        //{
        //    return await _context.Products
        //        .Include(p => p.Category)
        //        .Where(p => p.IsRxRequired)
        //        .ToListAsync();
        //}
    }
}
