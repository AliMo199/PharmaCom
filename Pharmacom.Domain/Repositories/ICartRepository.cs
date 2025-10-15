using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Repositories
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart?> GetCartWithItemsAsync(int cartId);
        Task<Cart?> GetCartByUserIdAsync(string userId);
        Task<CartItem?> GetCartItemByProductAsync(int cartId, int productId);
    }
}
