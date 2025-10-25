using PharmaCom.Domain.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<OrderStatisticsViewModel> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<SalesStatisticsViewModel> GetSalesStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ProductStatisticsViewModel> GetProductStatisticsAsync();
        Task<CustomerStatisticsViewModel> GetCustomerStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
