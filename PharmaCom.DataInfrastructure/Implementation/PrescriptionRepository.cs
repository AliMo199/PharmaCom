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
    public class PrescriptionRepository : GenericRepository<Prescription>, IPrescriptionRepository
    {
        private readonly ApplicationDBContext _context;

        public PrescriptionRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Prescription>> GetPrescriptionsByOrderIdAsync(int orderId)
        {
            return await _context.Prescriptions
                .Include(p => p.Order)
                .Where(p => p.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<Prescription?> GetPrescriptionWithOrderAsync(int id)
        {
            return await _context.Prescriptions
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
