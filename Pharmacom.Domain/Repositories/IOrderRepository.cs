using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Repositories
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetOrderWithDetailsAsync(int orderId);

        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);

        Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);

        Task<Order?> GetOrderBySessionIdAsync(string sessionId);

        // NEW: Complete Pagination & Search (ALL CASES COVERED)
        Task<PagedResult<Order>> GetOrdersPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm = null,          // Search in Id, SessionId, PaymentIntentId
            string? userId = null,              // Filter by user
            string? status = null,              // Filter by status
            DateTime? minDate = null,           // Order date range minimum
            DateTime? maxDate = null,           // Order date range maximum
            decimal? minAmount = null,          // Total amount range minimum
            decimal? maxAmount = null,          // Total amount range maximum
            bool? hasPrescription = null,       // Filter by has prescription
            string sortBy = "OrderDate",        // Sort field: OrderDate, TotalAmount, Status
            bool sortDescending = true);        // Sort direction (default descending for newest)
    }
}
