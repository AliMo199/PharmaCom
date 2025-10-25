using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Domain.ViewModels
{
    public class DashboardViewModel
    {
        public OrderStatisticsViewModel OrderStatistics { get; set; }
        public SalesStatisticsViewModel SalesStatistics { get; set; }
        public ProductStatisticsViewModel ProductStatistics { get; set; }
        public CustomerStatisticsViewModel CustomerStatistics { get; set; }
        public List<RecentOrderViewModel> RecentOrders { get; set; }
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; }

        public DashboardViewModel()
        {
            OrderStatistics = new OrderStatisticsViewModel();
            SalesStatistics = new SalesStatisticsViewModel();
            ProductStatistics = new ProductStatisticsViewModel();
            CustomerStatistics = new CustomerStatisticsViewModel();
            RecentOrders = new List<RecentOrderViewModel>();
            TopSellingProducts = new List<TopSellingProductViewModel>();
        }
    }
}
