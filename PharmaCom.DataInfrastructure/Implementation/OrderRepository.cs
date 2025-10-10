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
                //.Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        //public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        //{
        //    return await _context.Orders
        //        .Include(o => o.OrderItems)
        //        .Include(o => o.Address)
        //        .Where(o => o.ApplicationUserId == userId)
        //        .OrderByDescending(o => o.OrderDate)
        //        .ToListAsync();
        //}

        //public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
        //{
        //    return await _context.Orders
        //        .Include(o => o.OrderItems)
        //        .Include(o => o.ApplicationUser)
        //        .Where(o => o.Status == status)
        //        .OrderByDescending(o => o.OrderDate)
        //        .ToListAsync();
        //}

        //public async Task<IEnumerable<Order>> GetRecentOrdersAsync(int count)
        //{
        //    return await _context.Orders
        //        .Include(o => o.OrderItems)
        //        .Include(o => o.ApplicationUser)
        //        .OrderByDescending(o => o.OrderDate)
        //        .Take(count)
        //        .ToListAsync();
        //}
    }
}
