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
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        private readonly ApplicationDBContext _context;
        public CartRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartWithItemsAsync(int cartId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Category)
                //.Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(c => c.Id == cartId);
        }

        //public async Task<Cart?> GetCartByUserIdAsync(int userId)
        //{
        //    return await _context.Carts
        //        .Include(c => c.Items)
        //            .ThenInclude(i => i.Product)
        //                .ThenInclude(p => p.Category)
        //        .Include(c => c.ApplicationUser)
        //        .FirstOrDefaultAsync(c => c.ApplicationUserId == userId);
        //}
    }
}
