using Assignment.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmaCom.DataInfrastructure.Implementation;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Service.Interfaces;
using System.Diagnostics;

namespace PharmaCom.WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly int _pageSize = 9;

        public HomeController(ILogger<HomeController> logger, IProductService productService, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _productService = productService;
            _unitOfWork = unitOfWork;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured/popular products for homepage
                var products = await _productService.GetAllProductsAsync();
                var featuredProducts = products.Take(6).ToList();

                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error loading homepage: {ex.Message}");
                return View(new List<PharmaCom.Domain.Models.Product>());
            }
        }

        // GET: Home/Store (Product listing with pagination)
        public async Task<IActionResult> Store(
            int page = 1,
            string? searchTerm = null,
            int? categoryId = null,
            string? brand = null,
            string? form = null,
            bool? rxRequired = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? sortOption = null,
            string? sort = null,
            bool? desc = null)
        {
            try
            {
                // Parse combined sortOption if provided
                string sortBy = "Name";
                bool sortDescending = false;

                if (!string.IsNullOrEmpty(sortOption))
                {
                    var parts = sortOption.Split('-');
                    if (parts.Length == 2)
                    {
                        sortBy = parts[0];
                        bool.TryParse(parts[1], out sortDescending);
                    }
                }
                else if (!string.IsNullOrEmpty(sort))
                {
                    sortBy = sort;
                    sortDescending = desc ?? false;
                }

                // Get paginated products
                var pagedProducts = await _productService.GetProductsPagedAsync(
                    page,
                    12,
                    searchTerm, // No search term (using navbar search)
                    categoryId,
                    brand,
                    form,
                    rxRequired,
                    minPrice,
                    maxPrice,
                    sortBy,
                    sortDescending);

                // Get filter options
                ViewBag.Categories = await _unitOfWork.Category.GetAllAsync();
                ViewBag.Brands = await _productService.GetAllBrandsAsync();
                ViewBag.Forms = await _productService.GetAllFormsAsync();

                ViewBag.SearchTerm = searchTerm;

                // Keep current values
                ViewBag.CurrentCategory = categoryId;
                ViewBag.CurrentBrand = brand;
                ViewBag.CurrentForm = form;
                ViewBag.CurrentRxRequired = rxRequired;
                ViewBag.MinPrice = minPrice;
                ViewBag.MaxPrice = maxPrice;
                ViewBag.SortBy = sortBy;
                ViewBag.CurrentDesc = sortDescending;

                return View(pagedProducts);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading products: " + ex.Message;
                return View(new PagedResult<Product>
                {
                    Items = new List<Product>(),
                    PageNumber = 1,
                    PageSize = 12,
                    TotalCount = 0
                });
            }
        }

        // GET: Home/ShopSingle (Product details)
        public async Task<IActionResult> ShowProduct(int id)
        {
            try
            {
                var product = await _productService.GetProductWithCategoryAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found.";
                    return RedirectToAction(nameof(Store));
                }
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Store));
            }
        }

        // GET: Home/About
        public IActionResult About()
        {
            return View();
        }

        // GET: Home/About 
        public IActionResult Contact()
        {
            return View();
        }

        // POST: Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string firstName, string lastName, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return View();
            }

            try
            {
                // TODO: Implement email sending logic here
                // For now, just show success message

                TempData["SuccessMessage"] = "Thank you for contacting us! We'll get back to you soon.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while sending your message. Please try again.";
                return View();
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
