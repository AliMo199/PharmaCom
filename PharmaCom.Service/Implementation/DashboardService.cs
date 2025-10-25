using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Domain.ViewModels;
using PharmaCom.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.Service.Implementation
{
    public class DashboardService : IDashboardService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var dashboard = new DashboardViewModel
            {
                OrderStatistics = await GetOrderStatisticsAsync(startDate, endDate),
                SalesStatistics = await GetSalesStatisticsAsync(startDate, endDate),
                ProductStatistics = await GetProductStatisticsAsync(),
                CustomerStatistics = await GetCustomerStatisticsAsync(startDate, endDate),
                RecentOrders = await GetRecentOrdersAsync(10),
                TopSellingProducts = await GetTopSellingProductsAsync(startDate, endDate, 5)
            };

            return dashboard;
        }

        public async Task<OrderStatisticsViewModel> GetOrderStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var allOrders = await _unitOfWork.Order.FindAsync(o => o.OrderDate >= startDate && o.OrderDate <= endDate);
            var orderList = allOrders.ToList();

            var today = DateTime.UtcNow.Date;
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var statistics = new OrderStatisticsViewModel
            {
                TotalOrders = orderList.Count,
                PendingOrders = orderList.Count(o => o.Status == ST.Pending || o.Status == ST.PaymentReceived),
                CompletedOrders = orderList.Count(o => o.Status == ST.Completed),
                CancelledOrders = orderList.Count(o => o.Status == ST.Rejected),
                OrdersToday = orderList.Count(o => o.OrderDate.Date == today),
                OrdersThisWeek = orderList.Count(o => o.OrderDate >= startOfWeek),
                OrdersThisMonth = orderList.Count(o => o.OrderDate >= startOfMonth)
            };

            return statistics;
        }

        public async Task<SalesStatisticsViewModel> GetSalesStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var completedOrders = await _unitOfWork.Order.FindAsync(o =>
                o.OrderDate >= startDate &&
                o.OrderDate <= endDate &&
                (o.Status == ST.Completed || o.Status == ST.PaymentReceived));

            var orderList = completedOrders.ToList();

            var today = DateTime.UtcNow.Date;
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var totalRevenue = orderList.Sum(o => o.TotalAmount);
            var averageOrderValue = orderList.Any() ? totalRevenue / orderList.Count : 0;

            var statistics = new SalesStatisticsViewModel
            {
                TotalRevenue = totalRevenue,
                RevenueToday = orderList.Where(o => o.OrderDate.Date == today).Sum(o => o.TotalAmount),
                RevenueThisWeek = orderList.Where(o => o.OrderDate >= startOfWeek).Sum(o => o.TotalAmount),
                RevenueThisMonth = orderList.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.TotalAmount),
                AverageOrderValue = averageOrderValue,
                RevenueTrend = GetRevenueByDay(orderList, startDate.Value, endDate.Value)
            };

            return statistics;
        }

        public async Task<ProductStatisticsViewModel> GetProductStatisticsAsync()
        {
            var products = await _unitOfWork.Product.GetAllProductsWithCategoryAsync();
            var categories = await _unitOfWork.Category.GetAllAsync();

            var statistics = new ProductStatisticsViewModel
            {
                TotalProducts = products.Count,
                RxRequiredProducts = products.Count(p => p.IsRxRequired),
                // Note: If you don't have stock tracking yet, this will be 0
                OutOfStockProducts = 0,
                ProductsByCategory = categories.ToDictionary(
                    c => c.Name,
                    c => products.Count(p => p.CategoryId == c.Id)
                )
            };

            return statistics;
        }

        public async Task<CustomerStatisticsViewModel> GetCustomerStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var allUsers = await _userManager.Users.ToListAsync();

            var today = DateTime.UtcNow.Date;
            var startOfWeek = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek).Date;
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            // For this example, we'll use account creation date to track "new" customers
            // In a real system, you might use first order date
            var statistics = new CustomerStatisticsViewModel
            {
                TotalCustomers = allUsers.Count,
                NewCustomersToday = 0, // You would need creation date for this
                NewCustomersThisWeek = 0,
                NewCustomersThisMonth = 0,
            };

            // For region statistics, we'll use addresses from orders
            var orders = await _unitOfWork.Order.FindAsync(o => true);
            var ordersByRegion = new Dictionary<string, int>();

            foreach (var order in orders)
            {
                var address = await _unitOfWork.Address.GetByIdAsync(order.AddressId);
                if (address != null)
                {
                    if (!ordersByRegion.ContainsKey(address.Governorate))
                    {
                        ordersByRegion[address.Governorate] = 0;
                    }
                    ordersByRegion[address.Governorate]++;
                }
            }

            statistics.CustomersByRegion = ordersByRegion;

            return statistics;
        }

        private async Task<List<RecentOrderViewModel>> GetRecentOrdersAsync(int count)
        {
            var recentOrders = await _unitOfWork.Order.GetOrdersPagedAsync(
                pageNumber: 1,
                pageSize: count,
                sortBy: "OrderDate",
                sortDescending: true
            );

            var result = new List<RecentOrderViewModel>();

            foreach (var order in recentOrders.Items)
            {
                var user = await _userManager.FindByIdAsync(order.ApplicationUserId);
                var userName = user?.UserName ?? "Unknown";

                result.Add(new RecentOrderViewModel
                {
                    OrderId = order.Id,
                    CustomerName = userName,
                    OrderDate = order.OrderDate,
                    Status = order.Status,
                    TotalAmount = order.TotalAmount,
                    HasPrescription = order.PrescriptionId.HasValue
                });
            }

            return result;
        }

        private async Task<List<TopSellingProductViewModel>> GetTopSellingProductsAsync(DateTime? startDate, DateTime? endDate, int count)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            // Get completed orders in date range
            var orders = await _unitOfWork.Order.FindAsync(o =>
                o.OrderDate >= startDate &&
                o.OrderDate <= endDate &&
                (o.Status == ST.Completed || o.Status == ST.PaymentReceived));

            // Get all order items for these orders
            var orderItemsByProduct = new Dictionary<int, (int Quantity, decimal Revenue)>();

            foreach (var order in orders)
            {
                var orderDetails = await _unitOfWork.Order.GetOrderWithDetailsAsync(order.Id);
                if (orderDetails?.OrderItems == null) continue;

                foreach (var item in orderDetails.OrderItems)
                {
                    if (!orderItemsByProduct.ContainsKey(item.ProductId))
                    {
                        orderItemsByProduct[item.ProductId] = (0, 0);
                    }

                    var price = item.Product?.Price ?? 0;
                    var currentValues = orderItemsByProduct[item.ProductId];
                    orderItemsByProduct[item.ProductId] = (
                        currentValues.Quantity + item.Quantity,
                        currentValues.Revenue + (price * item.Quantity)
                    );
                }
            }

            // Get the top selling products
            var topProducts = orderItemsByProduct
                .OrderByDescending(kv => kv.Value.Revenue)
                .Take(count)
                .ToList();

            var result = new List<TopSellingProductViewModel>();

            foreach (var entry in topProducts)
            {
                var product = await _unitOfWork.Product.GetProductWithCategoryAsync(entry.Key);
                if (product == null) continue;

                result.Add(new TopSellingProductViewModel
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Quantity = entry.Value.Quantity,
                    Revenue = entry.Value.Revenue,
                    Category = product.Category?.Name ?? "Unknown"
                });
            }

            return result;
        }

        private Dictionary<string, decimal> GetRevenueByDay(List<Order> orders, DateTime startDate, DateTime endDate)
        {
            var result = new Dictionary<string, decimal>();

            // Initialize all dates in range with zero revenue
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                result[date.ToString("yyyy-MM-dd")] = 0;
            }

            // Sum revenue by date
            foreach (var order in orders)
            {
                var dateKey = order.OrderDate.ToString("yyyy-MM-dd");
                if (result.ContainsKey(dateKey))
                {
                    result[dateKey] += order.TotalAmount;
                }
            }

            return result;
        }
    }
}
