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
        public async Task<IEnumerable<Prescription>> GetPrescriptionsByUserIdAsync(string userId)
        {
            return await _context.Prescriptions
                .Where(p => p.UploadedByUserId == userId)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }

        // ✅ Add this new method
        public async Task<IEnumerable<Prescription>> GetUnassignedPrescriptionsByUserIdAsync(string userId)
        {
            return await _context.Prescriptions
                .Where(p => p.UploadedByUserId == userId && p.OrderId == null)
                .OrderByDescending(p => p.UploadDate)
                .ToListAsync();
        }
    }
}
