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
        Task DeleteOrderAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
        Task<Order> GetOrderWithDetailsAsync(int orderId);
        // NEW: Pagination & Search for orders (admin or user panel)
        Task<PagedResult<Order>> GetOrdersPagedAsync
            (
                int pageNumber,
                int pageSize,
                string? searchTerm = null,
                string? userId = null,
                string? status = null,
                DateTime? minDate = null,
                DateTime? maxDate = null,
                decimal? minAmount = null,
                decimal? maxAmount = null,
                bool? hasPrescription = null,
                string sortBy = "OrderDate",
                bool sortDescending = true
            );

        Task<Order> GetOrderBySessionIdAsync(string sessionId);
        Task<bool> CanCancelOrderAsync(int orderId, string userId);
        Task<bool> CancelOrderAsync(int orderId, string userId, string? reason = null);
    }
}
