using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.Repositories
{
    public interface IPrescriptionRepository : IGenericRepository<Prescription>
    {
        Task<IEnumerable<Prescription>> GetPrescriptionsByOrderIdAsync(int orderId);
        Task<Prescription?> GetPrescriptionWithOrderAsync(int id);
    }
}
