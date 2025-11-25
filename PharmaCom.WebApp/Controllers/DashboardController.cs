using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PharmaCom.Domain.Repositories;
using PharmaCom.Domain.Static;
using PharmaCom.Service.Interfaces;

namespace PharmaCom.WebApp.Controllers
{
    [Authorize (Roles ="Admin, Pharmacist")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardController(
            IDashboardService dashboardService,
            IOrderService orderService,
            IProductService productService,
            IUnitOfWork unitOfWork)
        {
            _dashboardService = dashboardService;
            _orderService = orderService;
            _productService = productService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            var dashboardData = await _dashboardService.GetDashboardDataAsync(startDate, endDate);
            return View(dashboardData);
        }

        public async Task<IActionResult> OrderManagement(
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = null,
            string status = null,
            string sortBy = "OrderDate",
            bool sortDescending = true)
        {
            var orders = await _orderService.GetOrdersPagedAsync(
                pageNumber,
                pageSize,
                searchTerm,
                null,
                status,
                null,
                null,
                null,
                null,
                null,
                sortBy,
                sortDescending);

            ViewBag.StatusList = ST.Statuses;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDescending = sortDescending;

            return View(orders);
        }

        public async Task<IActionResult> ProductManagement(
            int pageNumber = 1,
            int pageSize = 10,
            string searchTerm = null,
            int? categoryId = null,
            string sortBy = "Name",
            bool sortDescending = false)
        {
            var products = await _productService.GetProductsPagedAsync(
                pageNumber,
                pageSize,
                searchTerm,
                categoryId,
                null,
                null,
                null,
                null,
                null,
                sortBy,
                sortDescending);

            ViewBag.Categories = await _unitOfWork.Category.GetAllAsync();
            ViewBag.CurrentPage = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDescending = sortDescending;

            return View(products);
        }

        public async Task<IActionResult> SalesReport(DateTime? startDate = null, DateTime? endDate = null)
        {
            startDate ??= DateTime.UtcNow.AddDays(-30);
            endDate ??= DateTime.UtcNow;

            var salesStatistics = await _dashboardService.GetSalesStatisticsAsync(startDate, endDate);
            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");

            return View(salesStatistics);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderData(DateTime? startDate = null, DateTime? endDate = null)
        {
            var orderStats = await _dashboardService.GetOrderStatisticsAsync(startDate, endDate);
            return Json(orderStats);
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesData(DateTime? startDate = null, DateTime? endDate = null)
        {
            var salesStats = await _dashboardService.GetSalesStatisticsAsync(startDate, endDate);
            return Json(salesStats);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, status);
                return Json(new { success = true, message = $"Order status updated to {status}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
