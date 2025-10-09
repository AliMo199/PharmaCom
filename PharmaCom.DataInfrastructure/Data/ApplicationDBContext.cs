using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.DataInfrastructure.Data
{
    public class ApplicationDBContext : IdentityDbContext<ApplicationUser>
    {
    }
}
