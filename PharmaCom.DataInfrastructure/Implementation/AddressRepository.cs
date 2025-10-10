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
    public class AddressRepository : GenericRepository<Address>, IAddressRepository
    {
        private readonly ApplicationDBContext _context;
        public AddressRepository(ApplicationDBContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Address>> GetAddressesByGovernorateAsync(string governorate)
        {
            return await _context.Addresses
                .Where(a => a.Governorate == governorate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Address>> GetAddressesByCityAsync(string city)
        {
            return await _context.Addresses
                .Where(a => a.City == city)
                .ToListAsync();
        }
    }
}
