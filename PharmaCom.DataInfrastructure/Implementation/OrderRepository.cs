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
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly ApplicationDBContext _context;

        public OrderRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Address)
                .Include(o => o.Prescription)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Address)
                .Where(o => o.ApplicationUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.ApplicationUser)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderBySessionIdAsync(string sessionId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.SessionId == sessionId);
        }

        public async Task<PagedResult<Order>> GetOrdersPagedAsync(
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
            bool sortDescending = true)
        {
            // Start with base query including essential navigations
            var query = _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.Address)
                .AsQueryable();

            // Search across Id (as string), SessionId, PaymentIntentId
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(searchLower) ||
                    (o.SessionId != null && o.SessionId.ToLower().Contains(searchLower)) ||
                    (o.PaymentIntentId != null && o.PaymentIntentId.ToLower().Contains(searchLower)));
            }

            // USER FILTER
            if (!string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(o => o.ApplicationUserId == userId);
            }

            // STATUS FILTER
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // DATE RANGE FILTER
            if (minDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= minDate.Value);
            }

            if (maxDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= maxDate.Value);
            }

            // AMOUNT RANGE FILTER
            if (minAmount.HasValue && minAmount.Value > 0)
            {
                query = query.Where(o => o.TotalAmount >= minAmount.Value);
            }

            if (maxAmount.HasValue && maxAmount.Value > 0)
            {
                query = query.Where(o => o.TotalAmount <= maxAmount.Value);
            }

            // PRESCRIPTION FILTER 
            if (hasPrescription.HasValue)
            {
                query = query.Where(o => (o.PrescriptionId != null) == hasPrescription.Value);
            }

            // GET TOTAL COUNT (Before pagination)
            var totalCount = await query.CountAsync();

            // SORTING
            query = sortBy?.ToLower() switch
            {
                "totalamount" => sortDescending
                    ? query.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.OrderDate)
                    : query.OrderBy(o => o.TotalAmount).ThenBy(o => o.OrderDate),

                "status" => sortDescending
                    ? query.OrderByDescending(o => o.Status)
                    : query.OrderBy(o => o.Status),

                "orderdate" => sortDescending
                    ? query.OrderByDescending(o => o.OrderDate)
                    : query.OrderBy(o => o.OrderDate),

                _ => query.OrderByDescending(o => o.OrderDate) // Default: Newest first
            };

            // PAGINATION
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // RETURN PAGED RESULT
            return new PagedResult<Order>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }
    }
}
