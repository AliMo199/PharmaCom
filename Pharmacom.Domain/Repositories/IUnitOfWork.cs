using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Repositories
{
    public interface IUnitOfWork: IDisposable
    {
        IAddressRepository Address { get; }
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        ICartItemRepository CartItem { get; }
        IOrderRepository Order { get; }
        ICartRepository Cart { get; }
        IOrderItemRepository OrderItem { get; }
        IPrescriptionRepository Prescription { get; }
        int Save();
    }
}
