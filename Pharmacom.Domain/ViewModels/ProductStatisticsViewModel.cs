using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class ProductStatisticsViewModel
    {
        public int TotalProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int RxRequiredProducts { get; set; }
        public Dictionary<string, int> ProductsByCategory { get; set; } = new Dictionary<string, int>();
    }
}
