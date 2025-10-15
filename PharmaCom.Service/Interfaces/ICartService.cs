using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PharmaCom.Domain.Models;

namespace PharmaCom.Service.Interfaces
{
    public interface ICartService
    {
        Task<Cart> GetOrCreateUserCartAsync(string userId);
        Task<CartItem> AddToCartAsync(string userId, int productId, int quantity);
        Task<CartItem> UpdateCartItemAsync(string userId, int productId, int quantity);
        Task RemoveFromCartAsync(string userId, int productId);
        Task ClearCartAsync(string userId);
        Task<decimal> CalculateCartTotalAsync(string userId);
        Task<int> GetCartItemsCountAsync(string userId);
    }
}
