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
        Task<Product?> GetProductWithCategoryAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);

        //Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);

        //Task<IEnumerable<Product>> GetPrescriptionRequiredProductsAsync();
    }
}
