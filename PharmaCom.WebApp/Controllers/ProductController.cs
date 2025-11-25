using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Service.Interfaces;

namespace PharmaCom.WebApp.Controllers
{
    [Authorize(Roles = "Admin, Pharmacist")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductController(IProductService productService, IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _unitOfWork = unitOfWork;
        }

        // GET: /Product/Index (with pagination and search)
        public async Task<IActionResult> Index(
            int pageNumber = 1,
            string? searchTerm = null,
            int? categoryId = null,
            string? brand = null,
            string? form = null,
            string? isRxRequired = null,  // string? for form binding
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string sortBy = "Name",
            bool sortDescending = false)
        {
            // Convert string isRxRequired to bool? for service
            bool? rxRequired = isRxRequired switch
            {
                "true" => true,
                "false" => false,
                _ => null
            };

            var pagedProducts = await _productService.GetProductsPagedAsync(
                pageNumber,
                25,  // Page size
                searchTerm,
                categoryId,
                brand,
                form,
                rxRequired,
                minPrice,
                maxPrice,
                sortBy,
                sortDescending);

            // Dropdown data
            ViewBag.Categories = await _unitOfWork.Category.GetAllAsync();
            ViewBag.Brands = await _productService.GetAllBrandsAsync();
            ViewBag.Forms = await _productService.GetAllFormsAsync();

            // Preserve filter state
            ViewBag.CurrentSearchTerm = searchTerm;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentBrand = brand;
            ViewBag.CurrentForm = form;
            ViewBag.CurrentIsRxRequired = isRxRequired;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentSortBy = sortBy;
            ViewBag.CurrentSortDescending = sortDescending;

            return View(pagedProducts);
        }

        // GET: /Product/Details
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductWithCategoryAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        // GET: /Product/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _unitOfWork.Category.GetAllAsync(), "Id", "Name");
            return View();
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                await _productService.CreateProductAsync(product);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _unitOfWork.Category.GetAllAsync(), "Id", "Name");
            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = new SelectList(await _unitOfWork.Category.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.Id)
                return BadRequest();

            if (ModelState.IsValid)
            {
                _productService.UpdateProductAsync(product);
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _unitOfWork.Category.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }


        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound();

            await _productService.DeleteProductAsync(id);
            return RedirectToAction("ProductManagement","Dashboard");
        }
    }
}
