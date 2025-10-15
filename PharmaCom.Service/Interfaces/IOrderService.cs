using PharmaCom.Domain.Models;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace PharmaCom.Service.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderFromCartAsync(string userId, int addressId); 
        Task<Session> CreateStripeCheckoutSessionAsync(int orderId, string successUrl, string cancelUrl);
        Task<bool> ProcessStripePaymentSuccessAsync(string sessionId);
        Task UpdateOrderStatusAsync(int orderId, string status);
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
        Task<Order> GetOrderWithDetailsAsync(int orderId);
    }
}
