using PharmaCom.DataInfrastructure.Data;
using PharmaCom.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.DataInfrastructure.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _context;
        public IAddressRepository Address { get; private set; }
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public ICartItemRepository CartItem { get; private set; }
        public IOrderRepository Order { get; private set; }
        public ICartRepository Cart { get; private set; }
        public IOrderItemRepository OrderItem { get; private set; }
        public IPrescriptionRepository Prescription { get; private set; }
        public UnitOfWork(ApplicationDBContext context)
        {
            _context = context;
            Address = new AddressRepository(_context);
            Category = new CategoryRepository(_context);
            Product = new ProductRepository(_context);
            CartItem = new CartItemRepository(_context);
            Order = new OrderRepository(_context);
            Cart = new CartRepository(_context);
            OrderItem = new OrderItemRepository(_context);
            Prescription = new PrescriptionRepository(_context);
        }
        public int Save()
        {
            return _context.SaveChanges();
        }
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
