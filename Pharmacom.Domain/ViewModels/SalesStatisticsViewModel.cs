using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class SalesStatisticsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, decimal> RevenueTrend { get; set; } = new Dictionary<string, decimal>();
    }
}
