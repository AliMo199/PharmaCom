using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class CustomerStatisticsViewModel
    {
        public int TotalCustomers { get; set; }
        public int NewCustomersToday { get; set; }
        public int NewCustomersThisWeek { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public Dictionary<string, int> CustomersByRegion { get; set; } = new Dictionary<string, int>();
    }
}
